using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IInstallationService" />
public sealed class InstallationService : IInstallationService
{
    private static readonly string[] DefaultInstallPaths =
    [
        @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
        @"C:\Program Files\Microsoft Games\Zoo Tycoon"
    ];

    private static readonly (string SubKey, string ValueName)[] RegistryProbes =
    [
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0",             "Install Path"),
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0",             "Install_Path"),
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0",             "InstallPath"),
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0",             "Path"),
        (@"SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0", "Install Path"),
        (@"SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0", "Install_Path"),
        (@"SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0", "InstallPath"),
        (@"SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0", "Path")
    ];

    private readonly ILauncherConfigService _config;

    private readonly IFileSystem _fileSystem;

    private readonly IFileLocatorService _locator;

    private readonly IRegistryReader _registry;

    /// <summary>Initialises a new instance of <see cref="InstallationService" />.</summary>
    /// <param name="config">Launcher config service used to load and persist the installation list.</param>
    /// <param name="locator">Stateless directory validator used to confirm <c>zoo.exe</c>/<c>zoo.ini</c> exist in candidate directories.</param>
    /// <param name="fileSystem">File-system abstraction used to probe the hard-coded default install paths.</param>
    /// <param name="registry">Registry reader used to enumerate Zoo Tycoon's recorded install paths.</param>
    public InstallationService(ILauncherConfigService config,
                               IFileLocatorService    locator,
                               IFileSystem            fileSystem,
                               IRegistryReader        registry)
    {
        _config     = config;
        _locator    = locator;
        _fileSystem = fileSystem;
        _registry   = registry;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(string gameDirectory)
    {
        LocatorResult result = await _locator.LocateFilesAsync(gameDirectory);

        return result.ExeFound;
    }

    /// <inheritdoc />
    public async Task RevalidateAllAsync()
    {
        LauncherConfig config  = await _config.LoadAsync();
        bool           changed = false;

        foreach (Installation installation in config.Installations)
        {
            bool valid = await ValidateAsync(installation.GameDirectory);

            if (installation.IsValid == valid)
            {
                continue;
            }

            installation.IsValid = valid;
            changed              = true;
        }

        if (changed)
        {
            await _config.SaveAsync(config);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Installation>> GetAllAsync()
    {
        LauncherConfig config = await _config.LoadAsync();

        return config.Installations;
    }

    /// <inheritdoc />
    public async Task<Installation> AddAsync(string gameDirectory, string? name = null)
    {
        bool valid = await ValidateAsync(gameDirectory);

        if (!valid)
        {
            throw new InvalidOperationException($"The directory '{gameDirectory}' does not contain zoo.exe.");
        }

        LauncherConfig config       = await _config.LoadAsync();
        var            installation = new Installation { GameDirectory = gameDirectory, Name = name };

        config.Installations.Add(installation);
        await _config.SaveAsync(config);

        return installation;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(Guid id)
    {
        LauncherConfig config = await _config.LoadAsync();

        config.Installations.RemoveAll(i => i.Id == id);

        if (config.LastOpenedInstallationId == id)
        {
            config.LastOpenedInstallationId = null;
        }

        await _config.SaveAsync(config);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Guid id, string? name = null, string? gameDirectory = null)
    {
        LauncherConfig config       = await _config.LoadAsync();
        Installation?  installation = config.Installations.Find(i => i.Id == id);

        if (installation is null)
        {
            return;
        }

        if (name is not null)
        {
            installation.Name = name;
        }

        if (gameDirectory is not null)
        {
            installation.GameDirectory = gameDirectory;
            installation.IsValid       = await ValidateAsync(gameDirectory);
        }

        await _config.SaveAsync(config);
    }

    /// <inheritdoc />
    public async Task SetLastOpenedAsync(Guid id)
    {
        LauncherConfig config = await _config.LoadAsync();
        config.LastOpenedInstallationId = id;
        await _config.SaveAsync(config);
    }

    /// <inheritdoc />
    public async Task<LocatorResult> DiscoverAsync()
    {
        foreach (string path in DefaultInstallPaths)
        {
            if (!_fileSystem.Directory.Exists(path))
            {
                continue;
            }

            LocatorResult result = await _locator.LocateFilesAsync(path);

            if (result.ExeFound)
            {
                return result;
            }
        }

        foreach (string directory in EnumerateRegistryCandidates())
        {
            LocatorResult result = await _locator.LocateFilesAsync(directory);

            if (result.ExeFound)
            {
                return result;
            }
        }

        return new LocatorResult(ExeFound: false, IniFound: false, ExePath: null, IniPath: null, GameDirectory: null);
    }

    /// <summary>Yields each registry-probed directory that exists on disk, in probe order.</summary>
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
