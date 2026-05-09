using System;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>
///     Orchestrates the launcher's startup flow: evaluates the installation list, validates the active installation,
///     parses <c>zoo.ini</c>, ensures the original backup exists, and persists the last-opened installation.
/// </summary>
public interface IStartupService
{
    /// <summary>Runs the full startup flow and returns a populated <see cref="StartupResult" />.</summary>
    Task<StartupResult> InitializeAsync();

    /// <summary>Validates <paramref name="directoryPath" />, registers it as a new installation, and opens it.</summary>
    /// <param name="directoryPath">Absolute path to the directory chosen manually by the user.</param>
    Task<StartupResult> ApplyManualDirectoryAsync(string directoryPath);

    /// <summary>Opens a specific registered installation by its <see cref="Installation.Id" />, parsing its INI and updating <see cref="Installation.LastOpened" />.</summary>
    /// <param name="id">The <see cref="Installation.Id" /> of the installation to open.</param>
    Task<StartupResult> OpenInstallationByIdAsync(Guid id);
}
