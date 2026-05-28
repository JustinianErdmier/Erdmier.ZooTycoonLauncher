namespace Erdmier.ZooTycoonLauncher.Application.Installations.Locate;

/// <summary>Handler for <see cref="LocateZooTycoonQuery" />.</summary>
public sealed class LocateZooTycoonHandler : IQueryHandler<LocateZooTycoonQuery, ErrorOr<LocatedDirectory>>
{
    private readonly IInstallationLocator _locator;
    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="locator">The locator.</param>
    /// <param name="settings">Settings repository (consulted for the persisted last-known directory; that field is not modelled in <see cref="LauncherSettings" /> today, so the handler passes <see langword="null" /> for the persisted candidate).</param>
    public LocateZooTycoonHandler(IInstallationLocator locator, ILauncherSettingsRepository settings)
    {
        _locator = locator;
        _settings = settings;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<LocatedDirectory>> Handle(LocateZooTycoonQuery query, CancellationToken cancellationToken)
    {
        // The SDD treats the persisted last-known directory as a future addition to LauncherSettings. Until that field lands,
        // pass null and rely on the Program Files + registry trail. The locator is forward-compatible — any future addition is
        // a one-line change here.
        _ = await _settings.GetAsync(cancellationToken);

        LocatedDirectory located = await _locator.LocateAsync(persistedLastKnownPath: null, cancellationToken);

        return located;
    }
}
