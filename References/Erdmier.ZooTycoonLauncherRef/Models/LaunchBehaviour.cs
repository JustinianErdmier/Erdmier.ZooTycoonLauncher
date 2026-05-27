namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Controls how the launcher selects the active installation on startup.</summary>
public enum LaunchBehaviour
{
    /// <summary>Automatically opens the most recently used installation without prompting.</summary>
    OpenLastUsed,

    /// <summary>Shows the installation picker on every launch so the user can choose explicitly.</summary>
    PromptToChoose
}
