namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.History;

/// <summary>Handler for <see cref="GetIniHistoryCountQuery" />.</summary>
public sealed class GetIniHistoryCountHandler : IQueryHandler<GetIniHistoryCountQuery, ErrorOr<int>>
{
    private readonly IInstallationRepository _installations;

    private readonly ILogger<GetIniHistoryCountHandler> _logger;

    private readonly IIniSnapshotRepository _snapshots;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="logger">Logger for failures.</param>
    /// <param name="snapshots">The snapshot repository.</param>
    public GetIniHistoryCountHandler(IInstallationRepository installations, ILogger<GetIniHistoryCountHandler> logger, IIniSnapshotRepository snapshots)
    {
        _installations = installations;
        _logger        = logger;
        _snapshots     = snapshots;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<int>> Handle(GetIniHistoryCountQuery query, CancellationToken cancellationToken)
    {
        GameInstallation? installation = await _installations.GetByIdAsync(query.InstallationId, cancellationToken);

        if (installation is null)
        {
            return IniErrors.InstallationNotFound(query.InstallationId);
        }

        try
        {
            return await _snapshots.CountHistoricalAsync(installation.Id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, message: "Counting INI history failed for {InstallationId}", installation.Id);

            return IniErrors.StoreFailed(ex.Message);
        }
    }
}
