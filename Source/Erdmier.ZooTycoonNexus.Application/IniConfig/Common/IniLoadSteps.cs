namespace Erdmier.ZooTycoonNexus.Application.IniConfig.Common;

/// <summary>The read-and-reconcile prologue shared by every operation that loads an installation's <c>zoo.ini</c> and reconciles <c>Current</c> with it.</summary>
public static class IniLoadSteps
{
    /// <summary>Reads <c>zoo.ini</c> after removing orphaned temp files; read failures and a missing file are returned as errors.</summary>
    /// <param name="files">The <c>zoo.ini</c> file store.</param>
    /// <param name="logger">Logger for read failures.</param>
    /// <param name="installation">The installation whose file is read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content, <c>Ini.Missing</c> or <c>Ini.ReadFailed</c>.</returns>
    public static async Task<ErrorOr<IniFileContent>> ReadAsync(IIniFileStore files, ILogger logger, GameInstallation installation, CancellationToken cancellationToken)
    {
        if (!installation.HasIni)
        {
            return IniErrors.Missing(installation.Path);
        }

        IniFileContent? content;

        try
        {
            await files.DeleteOrphanedTempFilesAsync(installation.Path, cancellationToken);

            content = await files.ReadAsync(installation.Path, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, message: "Reading zoo.ini failed for {InstallationId}", installation.Id);

            return IniErrors.ReadFailed(ex.Message);
        }

        return content is null ? IniErrors.Missing(installation.Path) : content;
    }

    /// <summary>
    ///     Reconciles <c>Current</c> with the text read from disk inside an open transaction. It does not log: callers call <see cref="LogReconciliation" /> once the
    ///     transaction has committed, so a rolled-back reconciliation is never reported as done.
    /// </summary>
    /// <param name="transaction">The open snapshot transaction.</param>
    /// <param name="reconciler">The tiered-drift reconciler.</param>
    /// <param name="diskText">The text read from disk.</param>
    /// <param name="nowUtc">The UTC time to stamp snapshots with.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reconciliation.</returns>
    public static Task<IniReconciliation> ReconcileAsync(IIniSnapshotTransaction transaction,
                                                         IniReconciler           reconciler,
                                                         string                  diskText,
                                                         DateTime                nowUtc,
                                                         CancellationToken       cancellationToken)
        => reconciler.ReconcileAsync(transaction, diskText, nowUtc, cancellationToken);

    /// <summary>Logs a committed reconciliation at information level when its outcome is not <see cref="IniReconciliationOutcome.Unchanged" />.</summary>
    /// <param name="logger">Logger for the outcome.</param>
    /// <param name="installation">The installation that was reconciled.</param>
    /// <param name="reconciliation">The committed reconciliation.</param>
    public static void LogReconciliation(ILogger logger, GameInstallation installation, IniReconciliation reconciliation)
    {
        if (reconciliation.Outcome == IniReconciliationOutcome.Unchanged)
        {
            return;
        }

        logger.LogInformation(message: "INI snapshot reconciled for {InstallationId}: {Outcome} ({ChangedKeyCount} changed keys)",
                              installation.Id,
                              reconciliation.Outcome,
                              reconciliation.ChangedKeyCount);
    }
}
