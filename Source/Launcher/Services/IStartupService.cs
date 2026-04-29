using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Orchestrates the launcher's startup flow: load config, locate game files, parse <c> zoo.ini </c>, ensure the original backup exists, and persist the discovered game directory.</summary>
public interface IStartupService
{
    /// <summary>Runs the full auto-discovery startup sequence and returns a populated <see cref="StartupResult" />.</summary>
    Task<StartupResult> InitializeAsync();

    /// <summary>Re-runs the parse / backup / persist phases against a directory chosen manually by the user.</summary>
    Task<StartupResult> ApplyManualDirectoryAsync(string directoryPath);
}
