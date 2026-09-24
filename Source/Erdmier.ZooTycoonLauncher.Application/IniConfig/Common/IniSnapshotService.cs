namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;

/// <summary>
///     The real <see cref="IIniSnapshotService" />: reads <c>zoo.ini</c>, then reconciles the <c>Current</c> snapshot with it in one transaction (first import, tiered drift). Read
///     failures and snapshot-store failures are returned as errors so a bad file or database degrades the installation to Cannot Play instead of failing the boot.
/// </summary>
public sealed class IniSnapshotService : IIniSnapshotService
{
    private readonly TimeProvider _clock;

    private readonly IIniFileStore _files;

    private readonly ILogger<IniSnapshotService> _logger;

    private readonly IniReconciler _reconciler;

    private readonly IIniSnapshotRepository _snapshots;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="clock">Time provider for UTC timestamps.</param>
    /// <param name="files">The <c>zoo.ini</c> file store.</param>
    /// <param name="logger">Logger for outcomes and failures.</param>
    /// <param name="reconciler">The tiered-drift reconciler.</param>
    /// <param name="snapshots">The snapshot repository.</param>
    public IniSnapshotService(TimeProvider                clock,
                              IIniFileStore               files,
                              ILogger<IniSnapshotService> logger,
                              IniReconciler               reconciler,
                              IIniSnapshotRepository      snapshots)
    {
        _clock      = clock;
        _files      = files;
        _logger     = logger;
        _reconciler = reconciler;
        _snapshots  = snapshots;
    }

    /// <inheritdoc />
    public Task<ErrorOr<Success>> CaptureOriginalAsync(GameInstallation installation, CancellationToken cancellationToken)
        => SynchroniseAsync(installation, cancellationToken);

    /// <inheritdoc />
    public async Task<ErrorOr<Success>> SynchroniseAsync(GameInstallation installation, CancellationToken cancellationToken)
    {
        if (!installation.HasIni)
        {
            return Result.Success;
        }

        ErrorOr<IniConfigResult> loaded = await LoadAsync(installation, cancellationToken);

        return loaded.IsError ? loaded.Errors : Result.Success;
    }

    /// <inheritdoc />
    public async Task<ErrorOr<IniConfigResult>> LoadAsync(GameInstallation installation, CancellationToken cancellationToken)
    {
        if (!installation.HasIni)
        {
            return IniErrors.Missing(installation.Path);
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

        try
        {
            await using IIniSnapshotTransaction transaction = await _snapshots.BeginAsync(installation.Id, cancellationToken);

            IniReconciliation reconciliation = await _reconciler.ReconcileAsync(transaction,
                                                                                content.Text,
                                                                                _clock.GetUtcNow()
                                                                                      .UtcDateTime,
                                                                                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            if (reconciliation.Outcome != IniReconciliationOutcome.Unchanged)
            {
                _logger.LogInformation(message: "INI snapshot reconciled for {InstallationId}: {Outcome}", installation.Id, reconciliation.Outcome);
            }

            return new IniConfigResult(reconciliation.Values, content.LastWriteUtc);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, message: "The INI snapshot store failed for {InstallationId}", installation.Id);

            return IniErrors.StoreFailed(ex.Message);
        }
    }
}
