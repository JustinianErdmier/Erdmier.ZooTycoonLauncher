namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Outcome of <see cref="Services.IStartupService.InitializeAsync" />, capturing the located paths, parsed model, and any user-facing warning.</summary>
public sealed record StartupResult(StartupStatus  Status,
                                   string?        GameDirectory,
                                   string?        ExePath,
                                   string?        IniPath,
                                   ZooIniModel?   Model,
                                   LauncherConfig Config,
                                   string?        Warning);

/// <summary>The category of the startup outcome, used by <see cref="ViewModels.MainWindowViewModel" /> to decide which UI affordances to enable.</summary>
public enum StartupStatus
{
    /// <summary>Both <c> zoo.exe </c> and <c> zoo.ini </c> were found and the INI parsed successfully. The UI is enabled.</summary>
    Ready,

    /// <summary>Auto-discovery failed entirely. The user must locate the installation manually.</summary>
    GameDirectoryUnknown,

    /// <summary><c> zoo.exe </c> was located but <c> zoo.ini </c> is missing. Settings tabs are disabled; Launch is enabled.</summary>
    IniMissing,

    /// <summary><c> zoo.ini </c> parsed successfully but <c> zoo.exe </c> is missing. Settings tabs are enabled; Launch is disabled.</summary>
    ExeMissing,

    /// <summary><c> zoo.ini </c> exists but could not be read or parsed. Settings tabs are disabled.</summary>
    IniParseFailed
}
