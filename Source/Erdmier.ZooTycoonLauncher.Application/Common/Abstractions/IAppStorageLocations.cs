namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>
/// Resolves on-disk paths the launcher reads and writes (SDD §4.7).
/// </summary>
/// <remarks>
/// All paths are absolute. Implementations may compute paths from <c>%LOCALAPPDATA%</c> or, in tests,
/// from a temp directory; the application layer must not depend on either resolution strategy.
/// </remarks>
public interface IAppStorageLocations
{
    /// <summary>The application data root — usually <c>%LOCALAPPDATA%\ZooTycoonLauncher\</c>.</summary>
    string AppDataRoot { get; }

    /// <summary>The folder holding launcher and per-installation databases.</summary>
    string DataRoot { get; }

    /// <summary>The folder holding logs.</summary>
    string LogsRoot { get; }

    /// <summary>The fully qualified path to <c>Launcher.db</c>.</summary>
    string LauncherDatabasePath { get; }

    /// <summary>The fully qualified path to the rolling launcher log file.</summary>
    string LauncherLogPath { get; }

    /// <summary>The fully qualified path to a particular installation's database.</summary>
    /// <param name="installationId">The installation's id.</param>
    string InstallationDatabasePath(Guid installationId);

    /// <summary>The fully qualified path to a particular installation's log file.</summary>
    /// <param name="installationId">The installation's id.</param>
    string InstallationLogPath(Guid installationId);
}
