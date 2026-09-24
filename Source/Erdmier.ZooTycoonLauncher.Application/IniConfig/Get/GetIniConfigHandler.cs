namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Get;

/// <summary>Handler for <see cref="GetIniConfigQuery" />.</summary>
public sealed class GetIniConfigHandler : IQueryHandler<GetIniConfigQuery, ErrorOr<IniConfigResult>>
{
    private readonly IInstallationRepository _installations;

    private readonly IIniSnapshotService _snapshots;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="snapshots">INI snapshot service.</param>
    public GetIniConfigHandler(IInstallationRepository installations, IIniSnapshotService snapshots)
    {
        _installations = installations;
        _snapshots     = snapshots;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<IniConfigResult>> Handle(GetIniConfigQuery query, CancellationToken cancellationToken)
    {
        GameInstallation? installation = await _installations.GetByIdAsync(query.InstallationId, cancellationToken);

        if (installation is null)
        {
            return IniErrors.InstallationNotFound(query.InstallationId);
        }

        return await _snapshots.LoadAsync(installation, cancellationToken);
    }
}
