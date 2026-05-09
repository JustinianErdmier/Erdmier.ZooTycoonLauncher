namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Strongly typed representation of the <c>[UI]</c> section of <c>zoo.ini</c>. Covers audio, gameplay (cash), interface, and miscellaneous runtime-state keys.</summary>
public class UiSettings
{
    /// <summary>Denomination by which cash is awarded. Valid range: <c>100</c>–<c>1,000,000</c>.</summary>
    public int CashIncrement { get; set; } = 5_000;

    /// <summary>Audio attenuation value for completed exhibit notifications. Read-only runtime state managed by the game.</summary>
    public int? CompletedExhibitAttenuation { get; set; }

    /// <summary>Maximum character length for editable text fields. Read-only runtime state managed by the game.</summary>
    public int? DefaultEditCharLimit { get; set; }

    /// <summary>Help system verbosity. <c>0</c> = Off, <c>1</c> = Standard, <c>2</c> = Verbose.</summary>
    public int HelpType { get; set; } = 1;

    /// <summary>Horizontal keyboard scroll speed. Valid range: <c>1</c>–<c>200</c>.</summary>
    public int KeyScrollX { get; set; } = 64;

    /// <summary>Vertical keyboard scroll speed. Valid range: <c>1</c>–<c>200</c>.</summary>
    public int KeyScrollY { get; set; } = 64;

    /// <summary>Last recorded window X-position. Read-only runtime state written by the game on exit.</summary>
    public int? LastWindowX { get; set; }

    /// <summary>Last recorded window Y-position. Read-only runtime state written by the game on exit.</summary>
    public int? LastWindowY { get; set; }

    /// <summary>Maximum cash the player can hold. Valid range: <c>0</c>–<c>10,000,000</c>.</summary>
    public int MaxCash { get; set; } = 500_000;

    /// <summary>Path to the main menu music file, relative to the game directory.</summary>
    public string MenuMusic { get; set; } = "sounds/mainmenu.wav";

    /// <summary>Attenuation applied to menu music. Higher values are quieter. Valid range: <c>0</c>–<c>10000</c>.</summary>
    public int MenuMusicAttenuation { get; set; } = 1500;

    /// <summary>Show in-game notification messages.</summary>
    public bool MessageDisplay { get; set; } = true;

    /// <summary>Minimum cash the player can hold. Valid range: <c>0</c>–<c>10,000,000</c>.</summary>
    public int MinCash { get; set; } = 10_000;

    /// <summary>Minimum interval in seconds between repeated notification messages. Valid range: <c>0</c>–<c>3600</c>.</summary>
    public int MinimumMessageInterval { get; set; } = 60;

    /// <summary>Delay before mouse-edge scrolling begins. Valid range: <c>0</c>–<c>10</c>.</summary>
    public int MouseScrollDelay { get; set; } = 1;

    /// <summary>Pixel distance from the screen edge at which mouse-edge scrolling activates. Valid range: <c>0</c>–<c>50</c>.</summary>
    public int MouseScrollThreshold { get; set; } = 1;

    /// <summary>Horizontal mouse-edge scroll speed. Valid range: <c>1</c>–<c>200</c>.</summary>
    public int MouseScrollX { get; set; } = 27;

    /// <summary>Vertical mouse-edge scroll speed. Valid range: <c>1</c>–<c>200</c>.</summary>
    public int MouseScrollY { get; set; } = 27;

    /// <summary>Volume for the first intro movie. <c>0</c> = full volume, <c>-10000</c> = silent.</summary>
    public int MovieVolume1 { get; set; } = -1000;

    /// <summary>Volume for the second intro movie. <c>0</c> = full volume, <c>-10000</c> = silent.</summary>
    public int MovieVolume2 { get; set; } = -1000;

    /// <summary>Suppress main menu background music. <see langword="true" /> = music disabled (note inverted logic).</summary>
    public bool NoMenuMusic { get; set; }

    /// <summary>Play the first intro movie on startup.</summary>
    public bool PlayMovie { get; set; }

    /// <summary>Play the second intro movie on startup.</summary>
    public bool PlaySecondMovie { get; set; }

    /// <summary>Internal counter used by the loading screen progress bar. Read-only runtime state managed by the game.</summary>
    public int? ProgressCalls { get; set; }

    /// <summary>Records whether the Marine Mania expansion tutorial has been started. Read-only runtime state managed by the game.</summary>
    public bool StartedAquaTutorial { get; set; }

    /// <summary>Records whether the Dinosaur Digs expansion tutorial has been started. Read-only runtime state managed by the game.</summary>
    public bool StartedDinoTutorial { get; set; }

    /// <summary>Records whether the main campaign tutorial has been started. Read-only runtime state managed by the game.</summary>
    public bool StartedFirstTutorial { get; set; }

    /// <summary>Cash available at the start of a new game. Valid range: <c>0</c>–<c>10,000,000</c>.</summary>
    public int StartingCash { get; set; } = 70_000;

    /// <summary>Delay in seconds before tooltips appear. Valid range: <c>0</c>–<c>60</c>.</summary>
    public int TooltipDelay { get; set; } = 1;

    /// <summary>Duration in milliseconds that tooltips remain visible. Valid range: <c>0</c>–<c>30000</c>.</summary>
    public int TooltipDuration { get; set; } = 3_000;

    /// <summary>Use monochrome cursors. Recommended if the cursor flickers or disappears.</summary>
    public bool UseAlternateCursors { get; set; }

    /// <summary>Additional attenuation applied globally to all game audio. Valid range: <c>0</c>–<c>10000</c>.</summary>
    public int UserAttenuation { get; set; }
}
