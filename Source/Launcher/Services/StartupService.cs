using System;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IStartupService" />
public sealed class StartupService : IStartupService
{
    private readonly ILauncherConfigService _config;
    private readonly IFileLocatorService _locator;
    private readonly IIniParserService _parser;
    private readonly IVersioningService _versioning;

    public StartupService(
        ILauncherConfigService config,
        IFileLocatorService locator,
        IIniParserService parser,
        IVersioningService versioning)
    {
        _config = config;
        _locator = locator;
        _parser = parser;
        _versioning = versioning;
    }

    /// <inheritdoc />
    public async Task<StartupResult> InitializeAsync()
    {
        var config = await _config.LoadAsync();
        var locator = await _locator.LocateFilesAsync();
        return await CompleteAsync(config, locator);
    }

    /// <inheritdoc />
    public async Task<StartupResult> ApplyManualDirectoryAsync(string directoryPath)
    {
        var config = await _config.LoadAsync();
        var locator = await _locator.LocateFilesAsync(directoryPath);
        return await CompleteAsync(config, locator);
    }

    private async Task<StartupResult> CompleteAsync(LauncherConfig config, LocatorResult locator)
    {
        if (!locator.ExeFound && !locator.IniFound)
        {
            return new StartupResult(
                Status: StartupStatus.GameDirectoryUnknown,
                GameDirectory: null,
                ExePath: null,
                IniPath: null,
                Model: null,
                Config: config,
                Warning: "Could not locate Zoo Tycoon. Use File → Locate Manually…");
        }

        ZooIniModel? model = null;
        string? parseWarning = null;
        if (locator.IniFound)
        {
            try
            {
                model = await _parser.ReadAsync(locator.IniPath!);
                await _versioning.EnsureOriginalBackupAsync(locator.IniPath!);
            }
            catch (Exception ex)
            {
                return new StartupResult(
                    Status: StartupStatus.IniParseFailed,
                    GameDirectory: locator.GameDirectory,
                    ExePath: locator.ExePath,
                    IniPath: locator.IniPath,
                    Model: null,
                    Config: config,
                    Warning: $"Failed to read zoo.ini: {ex.Message}");
            }
        }
        else
        {
            parseWarning = "zoo.ini not found in the game directory. Settings cannot be edited until it is created.";
        }

        if (locator.GameDirectory is not null && !string.Equals(locator.GameDirectory, config.GameDirectory, StringComparison.OrdinalIgnoreCase))
        {
            config.GameDirectory = locator.GameDirectory;
            await _config.SaveAsync(config);
        }

        var status = (locator.ExeFound, locator.IniFound) switch
        {
            (true, true)  => StartupStatus.Ready,
            (true, false) => StartupStatus.IniMissing,
            (false, true) => StartupStatus.ExeMissing,
            _             => StartupStatus.GameDirectoryUnknown
        };

        var warning = status switch
        {
            StartupStatus.Ready       => null,
            StartupStatus.IniMissing  => parseWarning,
            StartupStatus.ExeMissing  => "zoo.exe not found in the game directory. Launching is disabled.",
            _                         => null
        };

        return new StartupResult(
            Status: status,
            GameDirectory: locator.GameDirectory,
            ExePath: locator.ExePath,
            IniPath: locator.IniPath,
            Model: model,
            Config: config,
            Warning: warning);
    }
}
