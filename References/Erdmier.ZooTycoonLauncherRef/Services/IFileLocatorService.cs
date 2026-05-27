using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Stateless validator that confirms <c>zoo.exe</c> and <c>zoo.ini</c> exist within a given directory.</summary>
public interface IFileLocatorService
{
    /// <summary>Confirms that <c>zoo.exe</c> and <c>zoo.ini</c> exist within the given directory.</summary>
    /// <param name="directoryPath">Absolute path to the directory to probe.</param>
    Task<LocatorResult> LocateFilesAsync(string directoryPath);
}
