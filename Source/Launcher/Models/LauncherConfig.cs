namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Persisted launcher-specific preferences, stored as JSON in <c> %AppData%\ZooTycoonLauncher\launcher.config</c>.</summary>
public sealed class LauncherConfig
{
    /// <summary>The directory containing <c> zoo.exe </c>, persisted across sessions so subsequent launches skip auto-discovery.</summary>
    public string? GameDirectory { get; set; }

    /// <summary>Whether the launcher should minimise to the taskbar when the game is launched. Reserved for a future milestone; currently no UI reads this.</summary>
    public bool MinimiseOnLaunch { get; init; }
}
