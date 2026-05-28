namespace Erdmier.ZooTycoonLauncher.Application.Installations.GetById;

/// <summary>Handler for <see cref="GetInstallationByIdQuery" />.</summary>
public sealed class GetInstallationByIdHandler : IQueryHandler<GetInstallationByIdQuery, ErrorOr<InstallationSummary>>
{
    private readonly IInstallationRepository _installations;

    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="settings">Launcher settings repository.</param>
    public GetInstallationByIdHandler(IInstallationRepository installations, ILauncherSettingsRepository settings)
    {
        _installations = installations;
        _settings      = settings;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<InstallationSummary>> Handle(GetInstallationByIdQuery query, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(query.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", $"No installation with id {query.InstallationId}.");
        }

        LauncherSettings settings = await _settings.GetAsync(cancellationToken);

        return new InstallationSummary(row.Id,
                                       row.Name,
                                       row.Path,
                                       row.Validity,
                                       settings.DefaultInstallationId == row.Id,
                                       row.AddedUtc,
                                       row.ModifiedUtc,
                                       row.LastPlayedUtc,
                                       row.LastOpenedUtc);
    }
}
