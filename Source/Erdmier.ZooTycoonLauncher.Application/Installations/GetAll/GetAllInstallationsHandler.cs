namespace Erdmier.ZooTycoonLauncher.Application.Installations.GetAll;

/// <summary>Handler for <see cref="GetAllInstallationsQuery" />.</summary>
public sealed class GetAllInstallationsHandler : IQueryHandler<GetAllInstallationsQuery, ErrorOr<IReadOnlyList<InstallationSummary>>>
{
    private readonly IInstallationRepository _installations;
    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="settings">Launcher settings repository.</param>
    public GetAllInstallationsHandler(IInstallationRepository installations, ILauncherSettingsRepository settings)
    {
        _installations = installations;
        _settings = settings;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<IReadOnlyList<InstallationSummary>>> Handle(GetAllInstallationsQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<GameInstallation> rows = await _installations.GetAllAsync(cancellationToken);
        LauncherSettings settings = await _settings.GetAsync(cancellationToken);

        IReadOnlyList<InstallationSummary> summaries = rows.Select(row => new InstallationSummary(
                                                               Id:            row.Id,
                                                               Name:          row.Name,
                                                               Path:          row.Path,
                                                               Validity:      row.Validity,
                                                               IsDefault:     settings.DefaultInstallationId == row.Id,
                                                               AddedUtc:      row.AddedUtc,
                                                               ModifiedUtc:   row.ModifiedUtc,
                                                               LastPlayedUtc: row.LastPlayedUtc,
                                                               LastOpenedUtc: row.LastOpenedUtc))
                                                           .ToList();

        return ErrorOrFactory.From(summaries);
    }
}
