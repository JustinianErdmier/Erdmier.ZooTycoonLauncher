using System;
using System.Collections.Generic;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Persisted launcher-specific preferences, stored as JSON in <c>%AppData%\ZooTycoonLauncher\launcher.config</c>.</summary>
public sealed class LauncherConfig
{
    /// <summary>All registered Zoo Tycoon installations, in the order they were added.</summary>
    public List<Installation> Installations { get; set; } = [];

    /// <summary>
    ///     The <see cref="Installation.Id" /> of the most recently opened installation.
    ///     <see langword="null" /> if no installation has been opened yet.
    /// </summary>
    public Guid? LastOpenedInstallationId { get; set; }

    /// <summary>Controls how the launcher selects the active installation on startup.</summary>
    public LaunchBehaviour LaunchBehaviour { get; set; } = LaunchBehaviour.OpenLastUsed;

    /// <summary>Whether the launcher should minimise to the taskbar when the game is launched. Reserved for a future milestone; currently no UI reads this.</summary>
    public bool MinimiseOnLaunch { get; set; }
}
