namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Save;

/// <summary>
///     Handler for <see cref="SaveIniCommand" />. Follows SDD §8.2 inside one transaction, but builds the new file from the text on disk rather than from the stored blob, after
///     first reconciling any external drift — so values the game (or a hand edit) wrote since the editor loaded are never overwritten unless the user edited that key.
/// </summary>
public sealed class SaveIniHandler : ICommandHandler<SaveIniCommand, ErrorOr<IniConfigResult>>
{
    private readonly TimeProvider _clock;

    private readonly IIniFileStore _files;

    private readonly IInstallationRepository _installations;

    private readonly ILogger<SaveIniHandler> _logger;

    private readonly IniReconciler _reconciler;

    private readonly IIniSnapshotRepository _snapshots;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="clock">Time provider for UTC timestamps.</param>
    /// <param name="files">The <c>zoo.ini</c> file store.</param>
    /// <param name="installations">Installation repository.</param>
    /// <param name="logger">Logger for read, write and snapshot-store failures.</param>
    /// <param name="reconciler">The tiered-drift reconciler.</param>
    /// <param name="snapshots">The snapshot repository.</param>
    public SaveIniHandler(TimeProvider            clock,
                          IIniFileStore           files,
                          IInstallationRepository installations,
                          ILogger<SaveIniHandler> logger,
                          IniReconciler           reconciler,
                          IIniSnapshotRepository  snapshots)
    {
        _clock         = clock;
        _files         = files;
        _installations = installations;
        _logger        = logger;
        _reconciler    = reconciler;
        _snapshots     = snapshots;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<IniConfigResult>> Handle(SaveIniCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? installation = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (installation is null)
        {
            return IniErrors.InstallationNotFound(command.InstallationId);
        }

        IniFileContent? content;

        try
        {
            content = await _files.ReadAsync(installation.Path, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, message: "Reading zoo.ini failed for {InstallationId}", installation.Id);

            return IniErrors.ReadFailed(ex.Message);
        }

        if (content is null)
        {
            return IniErrors.Missing(installation.Path);
        }

        DateTime nowUtc = _clock.GetUtcNow()
                                .UtcDateTime;

        // The validator guarantees every key is recognised; resolving before BeginAsync means this unreachable throw cannot be mislabelled StoreFailed by either
        // catch below. Normalising to the registry id gives inserted keys the registry's casing.
        Dictionary<IniKeyId, IniKeySpec> specs = [];

        foreach (IniKeyId id in command.Edits.Keys)
        {
            specs[id] = ZooIniDefaults.TryGet(id, out IniKeySpec? found) ? found : throw new InvalidOperationException($"Unrecognised key {id}.");
        }

        IIniSnapshotTransaction transaction;

        try
        {
            transaction = await _snapshots.BeginAsync(installation.Id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, message: "The INI snapshot store failed for {InstallationId}", installation.Id);

            return IniErrors.StoreFailed(ex.Message);
        }

        await using (transaction)
        {
            Dictionary<IniKeyId, string>  edits = [];
            Dictionary<IniKeyId, string?> values;

            try
            {
                IniReconciliation reconciliation = await _reconciler.ReconcileAsync(transaction, content.Text, nowUtc, cancellationToken);

                foreach ((IniKeyId id, string value) in command.Edits)
                {
                    IniKeySpec spec = specs[id];

                    if (!spec.AreEquivalent(value, reconciliation.Values.GetValueOrDefault(spec.Id)))
                    {
                        edits[spec.Id] = value;
                    }
                }

                if (edits.Count == 0)
                {
                    await transaction.CommitAsync(cancellationToken);

                    return new IniConfigResult(reconciliation.Values, content.LastWriteUtc);
                }

                await transaction.ArchiveCurrentAsync(IniSnapshotTrigger.LauncherGui, nowUtc, cancellationToken);

                values = new Dictionary<IniKeyId, string?>(reconciliation.Values);

                foreach ((IniKeyId id, string value) in edits)
                {
                    values[id] = value;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, message: "The INI snapshot store failed for {InstallationId}", installation.Id);

                return IniErrors.StoreFailed(ex.Message);
            }

            IniDocument document = IniDocument.Parse(content.Text);

            foreach ((IniKeyId id, string value) in edits)
            {
                document.SetValue(id, value);
            }

            string text = document.Render();

            DateTime lastWriteUtc;

            try
            {
                lastWriteUtc = await _files.WriteAsync(installation.Path, text, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Returning disposes the transaction uncommitted, rolling back the archive: the file and the database both keep their previous state.
                _logger.LogWarning(ex, message: "Writing zoo.ini failed for {InstallationId}", installation.Id);

                return IniErrors.WriteFailed(ex.Message);
            }

            List<IniValueChange> changes = edits.Select(edit => new IniValueChange(edit.Key, edit.Value, IniValueSource.LauncherGui))
                                                .ToList();

            // The file is now the source of truth, so its write is not rolled back even if the snapshot cannot be recorded: use CancellationToken.None rather than the
            // caller's token, and swallow (rather than fault the command on) a failure here. The next synchronise sees the disk differs from Current and adopts it as Manual
            // drift (SDD §7.7).
            try
            {
                await transaction.UpdateCurrentAsync(text, changes, nowUtc, CancellationToken.None);
                await transaction.CommitAsync(CancellationToken.None);

                // Disposing here (rather than leaving it to the outer await using) means a disposal failure after a successful commit is caught and logged by this
                // block instead of escaping as "could not be saved" for a file that was, in fact, written. The outer await using then no-ops against the now-idempotent
                // dispose.
                await transaction.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                                  message: "zoo.ini was saved for {InstallationId} but its snapshot could not be recorded; the next synchronise adopts the file",
                                  installation.Id);
            }

            return new IniConfigResult(values, lastWriteUtc);
        }
    }
}
