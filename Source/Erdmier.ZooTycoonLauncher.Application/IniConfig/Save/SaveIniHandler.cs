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
    /// <param name="logger">Logger for write failures.</param>
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
            return IniErrors.ReadFailed(ex.Message);
        }

        if (content is null)
        {
            return IniErrors.Missing(installation.Path);
        }

        DateTime nowUtc = _clock.GetUtcNow()
                                .UtcDateTime;

        await using IIniSnapshotTransaction transaction = await _snapshots.BeginAsync(installation.Id, cancellationToken);

        IniReconciliation reconciliation = await _reconciler.ReconcileAsync(transaction, content.Text, nowUtc, cancellationToken);

        Dictionary<IniKeyId, string> edits = [];

        foreach ((IniKeyId id, string value) in command.Edits)
        {
            // The validator guarantees the key is recognised; normalising to the registry id gives inserted keys the registry's casing.
            IniKeySpec spec = ZooIniDefaults.TryGet(id, out IniKeySpec? found) ? found : throw new InvalidOperationException($"Unrecognised key {id}.");

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

        await transaction.UpdateCurrentAsync(text, changes, nowUtc, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        Dictionary<IniKeyId, string?> values = new(reconciliation.Values);

        foreach ((IniKeyId id, string value) in edits)
        {
            values[id] = value;
        }

        return new IniConfigResult(values, lastWriteUtc);
    }
}
