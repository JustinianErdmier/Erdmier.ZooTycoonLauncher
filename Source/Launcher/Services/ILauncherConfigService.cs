using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Loads and saves the launcher's persisted JSON configuration in <c> %AppData%\ZooTycoonLauncher\launcher.config</c>.</summary>
public interface ILauncherConfigService
{
    /// <summary>Path to the JSON config file on the disk. Stable for the lifetime of the service.</summary>
    string ConfigFilePath { get; }

    /// <summary>Reads the config from disk. Returns a fresh <see cref="LauncherConfig" /> with default values if the file does not exist, is empty, or fails to parse.</summary>
    Task<LauncherConfig> LoadAsync();

    /// <summary>Writes the config to disk atomically (temp file and rename). Creates the parent directory if necessary.</summary>
    Task SaveAsync(LauncherConfig config);
}
