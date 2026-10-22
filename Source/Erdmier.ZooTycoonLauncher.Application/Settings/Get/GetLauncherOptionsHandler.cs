namespace Erdmier.ZooTycoonLauncher.Application.Settings.Get;

/// <summary>Handler for <see cref="GetLauncherOptionsQuery" />.</summary>
/// <remarks>A repository exception propagates to the caller, which reports it to the user.</remarks>
public sealed class GetLauncherOptionsHandler : IQueryHandler<GetLauncherOptionsQuery, ErrorOr<LauncherOptions>>
{
    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="settings">The launcher settings repository.</param>
    public GetLauncherOptionsHandler(ILauncherSettingsRepository settings) => _settings = settings;

    /// <inheritdoc />
    public async ValueTask<ErrorOr<LauncherOptions>> Handle(GetLauncherOptionsQuery query, CancellationToken cancellationToken)
    {
        LauncherSettings settings = await _settings.GetAsync(cancellationToken);

        return new LauncherOptions(settings.LauncherStartupPreference, settings.CloseAfterGameLaunch);
    }
}
