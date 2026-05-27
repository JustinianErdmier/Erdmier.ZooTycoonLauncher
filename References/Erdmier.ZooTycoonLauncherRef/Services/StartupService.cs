using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IStartupService" />
public sealed class StartupService : IStartupService
{
    private readonly ILauncherConfigService _config;

    private readonly IInstallationService _installations;

    private readonly IFileLocatorService _locator;

    private readonly IIniParserService _parser;

    private readonly IVersioningService _versioning;

    /// <summary>Initialises a new instance of <see cref="StartupService" />.</summary>
    /// <param name="config">Launcher config service used to load and persist the installation list.</param>
    /// <param name="installations">Installation service that owns CRUD and auto-discovery for registered installations.</param>
    /// <param name="locator">Stateless validator used to confirm <c>zoo.exe</c>/<c>zoo.ini</c> for an installation directory.</param>
    /// <param name="parser">INI parser that materialises <c>zoo.ini</c> into a <see cref="ZooIniModel" />.</param>
    /// <param name="versioning">Versioning service responsible for the <c>zoo.ini.original</c> snapshot.</param>
    public StartupService(ILauncherConfigService config,
                          IInstallationService   installations,
                          IFileLocatorService    locator,
                          IIniParserService      parser,
                          IVersioningService     versioning)
    {
        _config        = config;
        _installations = installations;
        _locator       = locator;
        _parser        = parser;
        _versioning    = versioning;
    }

    /// <inheritdoc />
    public async Task<StartupResult> InitializeAsync()
    {
        LauncherConfig config = await _config.LoadAsync();

        if (config.Installations.Count == 0)
        {
            return config.LaunchBehaviour == LaunchBehaviour.PromptToChoose
                       ? AwaitingSelection(config)
                       : await DiscoverAndOpenAsync(config);
        }

        if (config.LaunchBehaviour == LaunchBehaviour.PromptToChoose)
        {
            await _installations.RevalidateAllAsync();
            config = await _config.LoadAsync();

            return AwaitingSelection(config);
        }

        // OpenLastUsed with existing installations.
        await _installations.RevalidateAllAsync();
        config = await _config.LoadAsync();

        List<Installation> invalid = config.Installations.FindAll(i => !i.IsValid);
        List<Installation> valid   = config.Installations.FindAll(i => i.IsValid);

        if (valid.Count == 0)
        {
            return new StartupResult(StartupStatus.AllInstallationsInvalid,
                                     GameDirectory: null,
                                     ExePath: null,
                                     IniPath: null,
                                     Model: null,
                                     config,
                                     Warning: "All registered installations are invalid.",
                                     ActiveInstallation: null,
                                     InvalidInstallations: invalid);
        }

        Installation? lastOpened = config.LastOpenedInstallationId.HasValue
                                       ? valid.Find(i => i.Id == config.LastOpenedInstallationId.Value)
                                       : null;

        Installation target = lastOpened ?? valid[0];

        return await OpenInstallationAsync(target, config, invalid);
    }

    /// <inheritdoc />
    public async Task<StartupResult> ApplyManualDirectoryAsync(string directoryPath)
    {
        bool valid = await _installations.ValidateAsync(directoryPath);

        if (!valid)
        {
            LauncherConfig cfg = await _config.LoadAsync();

            return NoDirectory(cfg, "The selected directory does not contain zoo.exe.");
        }

        Installation   registered = await _installations.AddAsync(directoryPath);
        LauncherConfig config     = await _config.LoadAsync();
        config.LastOpenedInstallationId = registered.Id;
        await _config.SaveAsync(config);

        return await OpenInstallationAsync(registered, config, invalidInstallations: []);
    }

    /// <inheritdoc />
    public async Task<StartupResult> OpenInstallationByIdAsync(Guid id)
    {
        LauncherConfig config       = await _config.LoadAsync();
        Installation?  installation = config.Installations.Find(i => i.Id == id);

        if (installation is null)
        {
            return NoDirectory(config, "Installation not found.");
        }

        return await OpenInstallationAsync(installation, config, invalidInstallations: []);
    }

    /// <summary>Runs auto-discovery and, on success, registers the discovered directory and opens it.</summary>
    private async Task<StartupResult> DiscoverAndOpenAsync(LauncherConfig config)
    {
        LocatorResult discovered = await _installations.DiscoverAsync();

        if (!discovered.ExeFound)
        {
            return NoDirectory(config, "Could not locate Zoo Tycoon. Use Manage Installations to add one.");
        }

        Installation   registered = await _installations.AddAsync(discovered.GameDirectory!);
        LauncherConfig updated    = await _config.LoadAsync();
        updated.LastOpenedInstallationId = registered.Id;
        await _config.SaveAsync(updated);

        return await OpenInstallationAsync(registered, updated, invalidInstallations: []);
    }

    /// <summary>Validates the installation, parses its <c>zoo.ini</c>, refreshes <see cref="Installation.LastOpened" />, and produces a <see cref="StartupResult" />.</summary>
    private async Task<StartupResult> OpenInstallationAsync(Installation                installation,
                                                            LauncherConfig              config,
                                                            IReadOnlyList<Installation> invalidInstallations)
    {
        LocatorResult located = await _locator.LocateFilesAsync(installation.GameDirectory);

        installation.LastOpened         = DateTime.UtcNow;
        config.LastOpenedInstallationId = installation.Id;
        await _config.SaveAsync(config);

        ZooIniModel? model        = null;
        string?      parseWarning = null;

        if (located.IniFound)
        {
            try
            {
                model = await _parser.ReadAsync(located.IniPath!);
                await _versioning.EnsureOriginalBackupAsync(located.IniPath!);
            }
            catch (Exception ex)
            {
                return new StartupResult(StartupStatus.IniParseFailed,
                                         installation.GameDirectory,
                                         located.ExePath,
                                         located.IniPath,
                                         Model: null,
                                         config,
                                         $"Failed to read zoo.ini: {ex.Message}",
                                         installation,
                                         invalidInstallations);
            }
        }
        else
        {
            parseWarning = "zoo.ini not found in the game directory. Settings cannot be edited until it is created.";
        }

        StartupStatus status = (located.ExeFound, located.IniFound) switch
        {
            (true, true)  => StartupStatus.Ready,
            (true, false) => StartupStatus.IniMissing,
            (false, true) => StartupStatus.ExeMissing,
            var _         => StartupStatus.GameDirectoryUnknown
        };

        string? warning = status switch
        {
            StartupStatus.IniMissing => parseWarning,
            StartupStatus.ExeMissing => "zoo.exe not found in the game directory. Launching is disabled.",
            var _                    => null
        };

        return new StartupResult(status,
                                 installation.GameDirectory,
                                 located.ExePath,
                                 located.IniPath,
                                 model,
                                 config,
                                 warning,
                                 installation,
                                 invalidInstallations);
    }

    /// <summary>Builds an <see cref="StartupStatus.AwaitingUserSelection" /> result indicating the VM must show the picker.</summary>
    private static StartupResult AwaitingSelection(LauncherConfig config)
        => new(StartupStatus.AwaitingUserSelection,
               GameDirectory: null,
               ExePath: null,
               IniPath: null,
               Model: null,
               config,
               Warning: null,
               ActiveInstallation: null,
               InvalidInstallations: []);

    /// <summary>Builds a <see cref="StartupStatus.GameDirectoryUnknown" /> result with the supplied user-facing warning.</summary>
    private static StartupResult NoDirectory(LauncherConfig config, string warning)
        => new(StartupStatus.GameDirectoryUnknown,
               GameDirectory: null,
               ExePath: null,
               IniPath: null,
               Model: null,
               config,
               warning,
               ActiveInstallation: null,
               InvalidInstallations: []);
}
