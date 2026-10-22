namespace Erdmier.ZooTycoonLauncher.Application.Settings.Save;

/// <summary>Saves the launcher-wide options; the default installation is never changed by this command.</summary>
/// <param name="StartupPreference">What the launcher opens at start-up.</param>
/// <param name="CloseAfterGameLaunch">Whether the launcher closes after a successful Launch Game.</param>
public sealed record SaveLauncherOptionsCommand(LauncherStartupPreference StartupPreference, bool CloseAfterGameLaunch) : ICommand<ErrorOr<Success>>;
