namespace Erdmier.ZooTycoonLauncher.Application.Settings.Common;

/// <summary>The launcher-wide options edited in the Options dialogue.</summary>
/// <param name="StartupPreference">What the launcher opens at start-up.</param>
/// <param name="CloseAfterGameLaunch">Whether the launcher closes after a successful Launch Game.</param>
public sealed record LauncherOptions(LauncherStartupPreference StartupPreference, bool CloseAfterGameLaunch);
