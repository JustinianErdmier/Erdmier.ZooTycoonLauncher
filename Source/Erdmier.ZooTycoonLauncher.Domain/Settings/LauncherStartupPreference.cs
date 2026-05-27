namespace Erdmier.ZooTycoonLauncher.Domain.Settings;

/// <summary>
/// Governs which installation, if any, the launcher resolves on start (SDD §3.1, §7.1).
/// </summary>
public sealed class LauncherStartupPreference : SmartEnum<LauncherStartupPreference>
{
    /// <summary>Open <see cref="LauncherSettings.DefaultInstallationId" /> on start (the standard behaviour).</summary>
    public static readonly LauncherStartupPreference DefaultInstallation    = new("DefaultInstallation",    1);

    /// <summary>Open the installation with the most recent <c>LastPlayedUtc</c>; fall back to default if none.</summary>
    public static readonly LauncherStartupPreference LastPlayedInstallation = new("LastPlayedInstallation", 2);

    /// <summary>Open the installation with the most recent <c>LastOpenedUtc</c>; fall back to default if none.</summary>
    public static readonly LauncherStartupPreference LastOpenedInstallation = new("LastOpenedInstallation", 3);

    /// <summary>Open no installation; present the <c>OpenGameInstallation</c> state.</summary>
    public static readonly LauncherStartupPreference NoInstallation         = new("NoInstallation",         4);

    private LauncherStartupPreference(string name, int id) : base(name, id) { }
}
