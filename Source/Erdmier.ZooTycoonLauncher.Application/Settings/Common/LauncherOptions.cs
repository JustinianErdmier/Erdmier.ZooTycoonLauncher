namespace Erdmier.ZooTycoonLauncher.Application.Settings.Common;

/// <summary>The launcher-wide options edited in the Options dialogue.</summary>
/// <param name="StartupPreference">What the launcher opens at start-up.</param>
/// <param name="CloseAfterGameLaunch">Whether the launcher closes after a successful Launch Game.</param>
/// <param name="EditorStyle">The style of the INI editor's tabs.</param>
public sealed record LauncherOptions(LauncherStartupPreference StartupPreference, bool CloseAfterGameLaunch, IniEditorStyle EditorStyle);
