using System.IO.Abstractions;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IFileLocatorService" />
public sealed class FileLocatorService : IFileLocatorService
{
    private const string ExeFileName = "zoo.exe";

    private const string IniFileName = "zoo.ini";

    private readonly IFileSystem _fileSystem;

    /// <summary>Initialises a new instance of <see cref="FileLocatorService" />.</summary>
    /// <param name="fileSystem">File-system abstraction used to probe the supplied directory.</param>
    public FileLocatorService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public Task<LocatorResult> LocateFilesAsync(string directoryPath)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(directoryPath)
                || !_fileSystem.Directory.Exists(directoryPath))
            {
                return new LocatorResult(ExeFound: false, IniFound: false, ExePath: null, IniPath: null, GameDirectory: null);
            }

            string exePath = _fileSystem.Path.Combine(directoryPath, ExeFileName);
            string iniPath = _fileSystem.Path.Combine(directoryPath, IniFileName);

            bool exeFound = _fileSystem.File.Exists(exePath);
            bool iniFound = _fileSystem.File.Exists(iniPath);

            return new LocatorResult(exeFound,
                                     iniFound,
                                     exeFound ? exePath : null,
                                     iniFound ? iniPath : null,
                                     exeFound || iniFound ? directoryPath : null);
        });
    }
}
