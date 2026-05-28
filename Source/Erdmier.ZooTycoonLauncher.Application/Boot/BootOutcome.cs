namespace Erdmier.ZooTycoonLauncher.Application.Boot;

/// <summary>Terminal state determined by <see cref="BootHandler" /> after running the SDD §7.1.1 state machine.</summary>
public enum BootOutcome
{
    /// <summary>The active installation is valid; the launcher is ready to play.</summary>
    ReadyToPlay,

    /// <summary>The active installation is invalid, or synchronisation failed; the launcher cannot launch the game.</summary>
    CannotPlay,

    /// <summary>No suitable installation was found; the user must add one.</summary>
    NoGameInstallationFound,

    /// <summary>The startup preference is <c>NoInstallation</c>; the user must select an installation manually.</summary>
    OpenGameInstallation
}
