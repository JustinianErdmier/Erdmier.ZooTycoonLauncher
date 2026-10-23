namespace Erdmier.ZooTycoonNexus.Application.IniConfig.Common;

/// <summary>
///     The shared SDD §8.2 write pipeline. Inside one transaction it reads <c>zoo.ini</c>, reconciles any external drift, asks an <see cref="IniEditSelector" /> which keys to
///     write, archives <c>Current</c>, merges the edits onto the text on disk (not the stored blob) and records the result — so values the game (or a hand edit) wrote since the
///     editor loaded are never overwritten unless the selector chose that key.
/// </summary>
public sealed class IniWritePipeline
{
    private readonly TimeProvider _clock;

    private readonly IIniFileStore _files;

    private readonly IInstallationRepository _installations;

    private readonly ILogger<IniWritePipeline> _logger;

    private readonly IInstallationWriteProbe _probe;

    private readonly IniReconciler _reconciler;

    private readonly IIniSnapshotRepository _snapshots;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="clock">Time provider for UTC timestamps.</param>
    /// <param name="files">The <c>zoo.ini</c> file store.</param>
    /// <param name="installations">Installation repository.</param>
    /// <param name="logger">Logger for read, write and snapshot-store failures.</param>
    /// <param name="probe">Classifies an access-denied write failure.</param>
    /// <param name="reconciler">The tiered-drift reconciler.</param>
    /// <param name="snapshots">The snapshot repository.</param>
    public IniWritePipeline(TimeProvider              clock,
                            IIniFileStore             files,
                            IInstallationRepository   installations,
                            ILogger<IniWritePipeline> logger,
                            IInstallationWriteProbe   probe,
                            IniReconciler             reconciler,
                            IIniSnapshotRepository    snapshots)
    {
        _clock         = clock;
        _files         = files;
        _installations = installations;
        _logger        = logger;
        _probe         = probe;
        _reconciler    = reconciler;
        _snapshots     = snapshots;
    }

    /// <summary>Runs the write pipeline for one installation.</summary>
    /// <param name="installationId">The installation whose <c>zoo.ini</c> is written.</param>
    /// <param name="selectEdits">Chooses the keys to write from the reconciled values; an error it returns aborts the write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The effective values, the file's last-write time and the historical snapshot count, or the first failure.</returns>
    public async Task<ErrorOr<IniConfigResult>> WriteAsync(Guid installationId, IniEditSelector selectEdits, CancellationToken cancellationToken)
    {
        GameInstallation? installation = await _installations.GetByIdAsync(installationId, cancellationToken);

        if (installation is null)
        {
            return IniErrors.InstallationNotFound(installationId);
        }

        IniFileContent? content;

        try
        {
            await _files.DeleteOrphanedTempFilesAsync(installation.Path, cancellationToken);

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
            IReadOnlyDictionary<IniKeyId, string> edits;
            Dictionary<IniKeyId, string?>         values;
            int                                   historicalCount;
            IniReconciliation                     reconciliation;

            try
            {
                reconciliation = await IniLoadSteps.ReconcileAsync(transaction, _reconciler, content.Text, nowUtc, cancellationToken);

                ErrorOr<IReadOnlyDictionary<IniKeyId, string>> selection = await selectEdits(transaction, reconciliation.Values, cancellationToken);

                if (selection.IsError)
                {
                    return selection.Errors;
                }

                edits = selection.Value;

                if (edits.Count == 0)
                {
                    historicalCount = await transaction.CountHistoricalAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    IniLoadSteps.LogReconciliation(_logger, installation, reconciliation);

                    return new IniConfigResult(reconciliation.Values, content.LastWriteUtc, historicalCount);
                }

                await transaction.ArchiveCurrentAsync(IniSnapshotTrigger.LauncherGui, nowUtc, cancellationToken);

                historicalCount = await transaction.CountHistoricalAsync(cancellationToken);

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
            catch (UnauthorizedAccessException ex)
            {
                // Returning disposes the transaction uncommitted, rolling back the archive: the file and the database both keep their previous state.
                _logger.LogWarning(ex, message: "Writing zoo.ini was denied for {InstallationId}", installation.Id);

                InstallationWriteAccess access = await _probe.ProbeAsync(installation.Path, cancellationToken);

                return IniReadOnlyText.For(access) is null ? IniErrors.WriteFailed(ex.Message) : IniErrors.ReadOnly(access);
            }
            catch (IOException ex)
            {
                // As above: the transaction is disposed uncommitted.
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

                IniLoadSteps.LogReconciliation(_logger, installation, reconciliation);

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

            return new IniConfigResult(values, lastWriteUtc, historicalCount);
        }
    }
}
