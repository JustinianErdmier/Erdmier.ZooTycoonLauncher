namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.History;

/// <summary>
///     Handler for <see cref="GetIniHistoryQuery" />: reads <c>zoo.ini</c>, reconciles <c>Current</c> with it in one transaction, then compares every stored snapshot against the
///     reconciled values.
/// </summary>
public sealed class GetIniHistoryHandler : IQueryHandler<GetIniHistoryQuery, ErrorOr<IniHistoryResult>>
{
    private readonly TimeProvider _clock;

    private readonly IIniFileStore _files;

    private readonly IInstallationRepository _installations;

    private readonly ILogger<GetIniHistoryHandler> _logger;

    private readonly IniReconciler _reconciler;

    private readonly IIniSnapshotRepository _snapshots;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="clock">Time provider for UTC timestamps.</param>
    /// <param name="files">The <c>zoo.ini</c> file store.</param>
    /// <param name="installations">Installation repository.</param>
    /// <param name="logger">Logger for failures.</param>
    /// <param name="reconciler">The tiered-drift reconciler.</param>
    /// <param name="snapshots">The snapshot repository.</param>
    public GetIniHistoryHandler(TimeProvider                  clock,
                                IIniFileStore                 files,
                                IInstallationRepository       installations,
                                ILogger<GetIniHistoryHandler> logger,
                                IniReconciler                 reconciler,
                                IIniSnapshotRepository        snapshots)
    {
        _clock         = clock;
        _files         = files;
        _installations = installations;
        _logger        = logger;
        _reconciler    = reconciler;
        _snapshots     = snapshots;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<IniHistoryResult>> Handle(GetIniHistoryQuery query, CancellationToken cancellationToken)
    {
        GameInstallation? installation = await _installations.GetByIdAsync(query.InstallationId, cancellationToken);

        if (installation is null)
        {
            return IniErrors.InstallationNotFound(query.InstallationId);
        }

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

            IReadOnlyList<IniSnapshot> historical = await transaction.ListHistoricalAsync(cancellationToken);

            IniSnapshot? original = await transaction.GetOriginalAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            IniLoadSteps.LogReconciliation(_logger, installation, reconciliation);

            List<IniHistoryEntry> entries = historical.Select(snapshot => ToEntry(snapshot, reconciliation.Values))
                                                      .ToList();

            if (original is not null)
            {
                entries.Add(ToEntry(original, reconciliation.Values));
            }

            return new IniHistoryResult(entries);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, message: "The INI snapshot store failed for {InstallationId}", installation.Id);

            return IniErrors.StoreFailed(ex.Message);
        }
    }

    private static IniHistoryEntry ToEntry(IniSnapshot snapshot, IReadOnlyDictionary<IniKeyId, string?> reconciled)
        => new(snapshot.Id,
               snapshot.Kind,
               snapshot.Trigger,
               snapshot.CapturedUtc,
               IniSnapshotComparer.Compare(reconciled, IniSnapshotComparer.ValuesOf(snapshot)));
}
