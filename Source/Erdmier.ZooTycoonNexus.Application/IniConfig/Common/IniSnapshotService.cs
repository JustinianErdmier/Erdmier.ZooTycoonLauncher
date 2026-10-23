namespace Erdmier.ZooTycoonNexus.Application.IniConfig.Common;

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
        ErrorOr<IniFileContent> read = await IniLoadSteps.ReadAsync(_files, _logger, installation, cancellationToken);

        if (read.IsError)
        {
            return read.Errors;
        }

        IniFileContent content = read.Value;

        try
        {
            await using IIniSnapshotTransaction transaction = await _snapshots.BeginAsync(installation.Id, cancellationToken);

            IniReconciliation reconciliation = await IniLoadSteps.ReconcileAsync(transaction,
                                                                                 _reconciler,
                                                                                 content.Text,
                                                                                 _clock.GetUtcNow()
                                                                                       .UtcDateTime,
                                                                                 cancellationToken);

            int historicalCount = await transaction.CountHistoricalAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            IniLoadSteps.LogReconciliation(_logger, installation, reconciliation);

            return new IniConfigResult(reconciliation.Values, content.LastWriteUtc, historicalCount);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, message: "The INI snapshot store failed for {InstallationId}", installation.Id);

            return IniErrors.StoreFailed(ex.Message);
        }
    }
}
