using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IFileLocatorService" />
public sealed class FileLocatorService : IFileLocatorService
{
    private const string ExeFileName = "zoo.exe";

    private const string IniFileName = "zoo.ini";

    private static readonly string[] DefaultInstallPaths = [@"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon", @"C:\Program Files\Microsoft Games\Zoo Tycoon"];

    private static readonly (string SubKey, string ValueName)[] RegistryProbes =
    [
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0", "Install Path"),
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0", "Install_Path"),
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0", "InstallPath"),
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0", "Path"),
        (@"SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0", "Install Path"),
        (@"SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0", "Install_Path"),
        (@"SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0", "InstallPath"),
        (@"SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0", "Path")
    ];

    private readonly ILauncherConfigService _config;

    private readonly IFileSystem _fileSystem;

    private readonly IRegistryReader _registry;

    public FileLocatorService(IFileSystem fileSystem, IRegistryReader registry, ILauncherConfigService config)
    {
        _fileSystem = fileSystem;
        _registry   = registry;
        _config     = config;
    }

    /// <inheritdoc />
    public async Task<LocatorResult> LocateFilesAsync()
    {
        LauncherConfig config = await _config.LoadAsync();

        if (!string.IsNullOrWhiteSpace(config.GameDirectory))
        {
            LocatorResult fromConfig = await LocateFilesAsync(config.GameDirectory);

            if (fromConfig.ExeFound)
            {
                return fromConfig;
            }
        }

        foreach (string path in DefaultInstallPaths)
        {
            if (!_fileSystem.Directory.Exists(path))
            {
                continue;
            }

            LocatorResult fromDefault = await LocateFilesAsync(path);

            if (fromDefault.ExeFound)
            {
                return fromDefault;
            }
        }

        foreach (string directory in EnumerateRegistryCandidates())
        {
            LocatorResult fromRegistry = await LocateFilesAsync(directory);

            if (fromRegistry.ExeFound)
            {
                return fromRegistry;
            }
        }

        return new LocatorResult(ExeFound: false, IniFound: false, ExePath: null, IniPath: null, GameDirectory: null);
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

    private IEnumerable<string> EnumerateRegistryCandidates()
    {
        foreach ((string subKey, string valueName) in RegistryProbes)
        {
            string? value = _registry.ReadHklmString(subKey, valueName);

            if (!string.IsNullOrWhiteSpace(value)
                && _fileSystem.Directory.Exists(value))
            {
                yield return value;
            }
        }
    }
}
