namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Strongly typed representation of the <c>[user]</c> section of <c>zoo.ini</c>, covering display and performance settings.</summary>
public class UserSettings
{
    /// <summary>Target frame rate cap (FPS). Valid range: <c>15</c>–<c>120</c>.</summary>
    public int DrawRate { get; set; } = 60;

    /// <summary>Run the game in fullscreen (<see langword="true" />) or windowed (<see langword="false" />) mode.</summary>
    public bool Fullscreen { get; set; } = true;

    /// <summary>Path to the most recently opened save game file. Read-only runtime state managed by the game.</summary>
    public string? LastFile { get; set; }

    /// <summary>Vertical resolution in pixels.</summary>
    public int ScreenHeight { get; set; } = 600;

    /// <summary>Horizontal resolution in pixels.</summary>
    public int ScreenWidth { get; set; } = 800;

    /// <summary>Records whether the user entity warning dialogue has been shown. Read-only runtime state managed by the game.</summary>
    public bool ShowUserEntityWarning { get; set; }

    /// <summary>Game logic update rate (ticks per second). Valid range: <c>1</c>–<c>60</c>.</summary>
    public int UpdateRate { get; set; } = 15;
}
