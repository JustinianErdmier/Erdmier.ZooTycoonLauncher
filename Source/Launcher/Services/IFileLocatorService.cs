using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Locates <c>zoo.exe</c> and <c>zoo.ini</c> on the host file system.</summary>
public interface IFileLocatorService
{
    /// <summary>
    ///     Attempts to locate <c>zoo.exe</c> and <c>zoo.ini</c> automatically by checking the persisted launcher config, common installation paths, and the Windows Registry, in
    ///     that order.
    /// </summary>
    Task<LocatorResult> LocateFilesAsync();

    /// <summary>Verifies that the supplied directory contains <c>zoo.exe</c> (and optionally <c>zoo.ini</c>) and returns a <see cref="LocatorResult" /> reflecting what was found.</summary>
    /// <param name="directoryPath"> A directory chosen manually by the user via the folder-picker dialogue. </param>
    Task<LocatorResult> LocateFilesAsync(string directoryPath);
}
