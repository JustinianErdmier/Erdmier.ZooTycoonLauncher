# Multi-Installation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rework the launcher config, startup flow, and UI to support registering, naming, switching between, and validating multiple Zoo Tycoon installations.

**Architecture:** `InstallationService` becomes the single authority for the installations list and auto-discovery; `FileLocatorService` is stripped to a stateless directory validator; `StartupService` is rewritten around the §5.4 branching tree. Three new dialog pairs (picker, manage, invalid-alert) handle all installation UI, opened via `IDialogService` injected into `MainWindowViewModel`. A dedicated installation panel sits between the menu bar and the tab strip.

**Tech Stack:** C# 13, .NET 10, Avalonia 11, CommunityToolkit.Mvvm 8 (partial-property source generators), Microsoft.Extensions.DependencyInjection, System.IO.Abstractions, Classic.Avalonia.Theme.

---

## File Map

| Action | Path |
|--------|------|
| Create | `Source/Launcher/Models/LaunchBehaviour.cs` |
| Create | `Source/Launcher/Models/Installation.cs` |
| Rewrite | `Source/Launcher/Models/LauncherConfig.cs` |
| Extend | `Source/Launcher/Models/StartupResult.cs` |
| Create | `Source/Launcher/Services/IInstallationService.cs` |
| Create | `Source/Launcher/Services/InstallationService.cs` |
| Rewrite | `Source/Launcher/Services/IFileLocatorService.cs` |
| Rewrite | `Source/Launcher/Services/FileLocatorService.cs` |
| Extend | `Source/Launcher/Services/IStartupService.cs` |
| Rewrite | `Source/Launcher/Services/StartupService.cs` |
| Create | `Source/Launcher/Services/IDialogService.cs` |
| Create | `Source/Launcher/Services/AvaloniaDialogService.cs` |
| Create | `Source/Launcher/ViewModels/InstallationPickerViewModel.cs` |
| Create | `Source/Launcher/Views/InstallationPickerView.axaml` |
| Create | `Source/Launcher/Views/InstallationPickerView.axaml.cs` |
| Create | `Source/Launcher/ViewModels/ManageInstallationsViewModel.cs` |
| Create | `Source/Launcher/Views/ManageInstallationsView.axaml` |
| Create | `Source/Launcher/Views/ManageInstallationsView.axaml.cs` |
| Create | `Source/Launcher/ViewModels/InvalidInstallationsViewModel.cs` |
| Create | `Source/Launcher/Views/InvalidInstallationsView.axaml` |
| Create | `Source/Launcher/Views/InvalidInstallationsView.axaml.cs` |
| Create | `Source/Launcher/Views/InputDialogView.axaml` |
| Create | `Source/Launcher/Views/InputDialogView.axaml.cs` |
| Rewrite | `Source/Launcher/App.axaml.cs` |
| Rewrite | `Source/Launcher/ViewModels/MainWindowViewModel.cs` |
| Extend | `Source/Launcher/Views/MainWindow.axaml` |
| Extend | `Source/Launcher/Views/MainWindow.axaml.cs` |

---

### Task 1: New models — `LaunchBehaviour` and `Installation`

**Files:**
- Create: `Source/Launcher/Models/LaunchBehaviour.cs`
- Create: `Source/Launcher/Models/Installation.cs`

- [ ] **Step 1: Create `LaunchBehaviour.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Controls how the launcher selects the active installation on startup.</summary>
public enum LaunchBehaviour
{
    /// <summary>Automatically opens the most recently used installation without prompting.</summary>
    OpenLastUsed,

    /// <summary>Shows the installation picker on every launch so the user can choose explicitly.</summary>
    PromptToChoose
}
```

- [ ] **Step 2: Create `Installation.cs`**

```csharp
using System;

using JetBrains.Annotations;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>A registered Zoo Tycoon installation: a directory containing both <c>zoo.exe</c> and <c>zoo.ini</c>.</summary>
[UsedImplicitly]
public sealed class Installation
{
    /// <summary>Stable identifier. Never changes, even if the directory or name is updated.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>User-assigned friendly name. <see langword="null" /> means the UI falls back to <see cref="GameDirectory" />.</summary>
    public string? Name { get; set; }

    /// <summary>Absolute path to the directory containing <c>zoo.exe</c> and <c>zoo.ini</c>.</summary>
    public string GameDirectory { get; set; } = string.Empty;

    /// <summary>
    ///     <see langword="false" /> if the last validation attempt found <c>zoo.exe</c> missing. An installation must pass
    ///     validation to be added, but may become invalid afterwards (e.g. game uninstalled, drive disconnected).
    ///     Invalid installations are still retained in config until the user explicitly removes them.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>UTC timestamp of the last time this installation was opened in the launcher. <see langword="null" /> if never opened.</summary>
    public DateTime? LastOpened { get; set; }

    /// <summary>Display name used in all UI bindings. Falls back to <see cref="GameDirectory" /> when <see cref="Name" /> is <see langword="null" />.</summary>
    public string DisplayName => Name ?? GameDirectory;
}
```

- [ ] **Step 3: Build and verify**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```powershell
git add Source/Launcher/Models/LaunchBehaviour.cs Source/Launcher/Models/Installation.cs
git commit -m "feat(✨): add LaunchBehaviour enum and Installation model"
```

---

### Task 2: Update `LauncherConfig` and extend `StartupResult`/`StartupStatus`

**Files:**
- Rewrite: `Source/Launcher/Models/LauncherConfig.cs`
- Extend: `Source/Launcher/Models/StartupResult.cs`

> **Note:** Removing `GameDirectory` from `LauncherConfig` will cause `FileLocatorService` to fail to compile. The build will be broken after this task and restored in Task 4 (which rewrites `FileLocatorService` and `StartupService` together). Do not commit until Task 4 is complete — commit both tasks together.

- [ ] **Step 1: Rewrite `LauncherConfig.cs`**

```csharp
using System;
using System.Collections.Generic;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Persisted launcher-specific preferences, stored as JSON in <c>%AppData%\ZooTycoonLauncher\launcher.config</c>.</summary>
public sealed class LauncherConfig
{
    /// <summary>All registered Zoo Tycoon installations, in the order they were added.</summary>
    public List<Installation> Installations { get; set; } = [];

    /// <summary>
    ///     The <see cref="Installation.Id" /> of the most recently opened installation.
    ///     <see langword="null" /> if no installation has been opened yet.
    /// </summary>
    public Guid? LastOpenedInstallationId { get; set; }

    /// <summary>Controls how the launcher selects the active installation on startup.</summary>
    public LaunchBehaviour LaunchBehaviour { get; set; } = LaunchBehaviour.OpenLastUsed;

    /// <summary>Whether the launcher should minimise to the taskbar when the game is launched. Reserved for a future milestone; currently no UI reads this.</summary>
    public bool MinimiseOnLaunch { get; set; }
}
```

- [ ] **Step 2: Rewrite `StartupResult.cs`**

```csharp
using System.Collections.Generic;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Outcome of <see cref="Services.IStartupService.InitializeAsync" />, capturing the located paths, parsed model, active installation, and any user-facing warning.</summary>
public sealed record StartupResult(
    StartupStatus               Status,
    string?                     GameDirectory,
    string?                     ExePath,
    string?                     IniPath,
    ZooIniModel?                Model,
    LauncherConfig              Config,
    string?                     Warning,
    Installation?               ActiveInstallation,
    IReadOnlyList<Installation> InvalidInstallations);

/// <summary>The category of the startup outcome, used by <see cref="ViewModels.MainWindowViewModel" /> to decide which UI affordances to enable.</summary>
public enum StartupStatus
{
    /// <summary>Both <c>zoo.exe</c> and <c>zoo.ini</c> were found and the INI parsed successfully. The UI is enabled.</summary>
    Ready,

    /// <summary>Auto-discovery failed entirely and no installations are registered. The user must add an installation via Manage Installations.</summary>
    GameDirectoryUnknown,

    /// <summary><c>zoo.exe</c> was located but <c>zoo.ini</c> is missing. Settings tabs are disabled; Launch is enabled.</summary>
    IniMissing,

    /// <summary><c>zoo.ini</c> parsed successfully but <c>zoo.exe</c> is missing. Settings tabs are enabled; Launch is disabled.</summary>
    ExeMissing,

    /// <summary><c>zoo.ini</c> exists but could not be read or parsed. Settings tabs are disabled.</summary>
    IniParseFailed,

    /// <summary><see cref="LaunchBehaviour.PromptToChoose" /> is active; the VM must show the installation picker before proceeding.</summary>
    AwaitingUserSelection,

    /// <summary>Every registered installation failed validation; the VM must show the combined alert and then prompt the user to locate a new installation.</summary>
    AllInstallationsInvalid
}
```

- [ ] **Step 3: Verify the break is expected**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Compile errors referencing `config.GameDirectory` in `FileLocatorService.cs` and referencing the old `StartupResult` constructor in `StartupService.cs` and `MainWindowViewModel.cs`. These are fixed in Tasks 3 and 4.

---

### Task 3: `IInstallationService` and `InstallationService`

**Files:**
- Create: `Source/Launcher/Services/IInstallationService.cs`
- Create: `Source/Launcher/Services/InstallationService.cs`

- [ ] **Step 1: Create `IInstallationService.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Validates and manages the list of registered Zoo Tycoon installations stored in <c>launcher.config</c>.</summary>
public interface IInstallationService
{
    /// <summary>Returns <see langword="true" /> if the given directory contains <c>zoo.exe</c>.</summary>
    Task<bool> ValidateAsync(string gameDirectory);

    /// <summary>Re-validates all installations in the config and updates <see cref="Installation.IsValid" /> accordingly.</summary>
    Task RevalidateAllAsync();

    /// <summary>Returns all registered installations in config order.</summary>
    Task<IReadOnlyList<Installation>> GetAllAsync();

    /// <summary>Registers a new installation. Throws <see cref="InvalidOperationException" /> if the directory does not contain <c>zoo.exe</c>.</summary>
    /// <param name="gameDirectory">Absolute path to the directory containing <c>zoo.exe</c>.</param>
    /// <param name="name">Optional friendly name. When <see langword="null" /> the UI falls back to the directory path.</param>
    Task<Installation> AddAsync(string gameDirectory, string? name = null);

    /// <summary>Removes an installation by <see cref="Installation.Id" />. No-op if the Id is not found.</summary>
    Task RemoveAsync(Guid id);

    /// <summary>
    ///     Updates the name or game directory of an existing installation in place, preserving its
    ///     <see cref="Installation.Id" /> and <see cref="Installation.LastOpened" />.
    /// </summary>
    /// <param name="id">The <see cref="Installation.Id" /> of the installation to update.</param>
    /// <param name="name">New friendly name, or <see langword="null" /> to leave unchanged.</param>
    /// <param name="gameDirectory">New directory path, or <see langword="null" /> to leave unchanged.</param>
    Task UpdateAsync(Guid id, string? name = null, string? gameDirectory = null);

    /// <summary>Sets <see cref="LauncherConfig.LastOpenedInstallationId" /> to <paramref name="id" /> and saves the config.</summary>
    Task SetLastOpenedAsync(Guid id);

    /// <summary>
    ///     Runs auto-discovery (hard-coded install paths, then registry probes) and returns a
    ///     <see cref="LocatorResult" /> for the first valid directory found.
    ///     Returns a failure result when nothing is found.
    /// </summary>
    Task<LocatorResult> DiscoverAsync();
}
```

- [ ] **Step 2: Create `InstallationService.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
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
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0",         "Install Path"),
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0",         "Install_Path"),
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0",         "InstallPath"),
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0",         "Path"),
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

    private IEnumerable<string> EnumerateRegistryCandidates()
    {
        foreach ((string subKey, string valueName) in RegistryProbes)
        {
            string? value = _registry.ReadHklmString(subKey, valueName);

            if (!string.IsNullOrWhiteSpace(value) && _fileSystem.Directory.Exists(value))
            {
                yield return value;
            }
        }
    }
}
```

- [ ] **Step 3: Verify still broken (expected)**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Same compile errors as after Task 2 (the new files compile fine; existing breakage from `FileLocatorService`/`StartupService` remains). Continue to Task 4.

---

### Task 4: Simplify `FileLocatorService`, rewrite `StartupService`, and wire DI

This task restores a green build. All four files must be saved before building.

**Files:**
- Rewrite: `Source/Launcher/Services/IFileLocatorService.cs`
- Rewrite: `Source/Launcher/Services/FileLocatorService.cs`
- Extend: `Source/Launcher/Services/IStartupService.cs`
- Rewrite: `Source/Launcher/Services/StartupService.cs`
- Rewrite: `Source/Launcher/App.axaml.cs`

- [ ] **Step 1: Rewrite `IFileLocatorService.cs`**

```csharp
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
```

- [ ] **Step 2: Rewrite `FileLocatorService.cs`**

```csharp
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
```

- [ ] **Step 3: Extend `IStartupService.cs`**

```csharp
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
    Task<StartupResult> ApplyManualDirectoryAsync(string directoryPath);

    /// <summary>Opens a specific registered installation by its <see cref="Installation.Id" />, parsing its INI and updating <see cref="Installation.LastOpened" />.</summary>
    Task<StartupResult> OpenInstallationByIdAsync(Guid id);
}
```

- [ ] **Step 4: Rewrite `StartupService.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
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

        // OpenLastUsed with existing installations
        await _installations.RevalidateAllAsync();
        config = await _config.LoadAsync();

        List<Installation> invalid = config.Installations.FindAll(i => !i.IsValid);
        List<Installation> valid   = config.Installations.FindAll(i => i.IsValid);

        if (valid.Count == 0)
        {
            return new StartupResult(StartupStatus.AllInstallationsInvalid,
                                     GameDirectory: null, ExePath: null, IniPath: null, Model: null,
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

    private async Task<StartupResult> OpenInstallationAsync(Installation              installation,
                                                             LauncherConfig            config,
                                                             IReadOnlyList<Installation> invalidInstallations)
    {
        LocatorResult located = await _locator.LocateFilesAsync(installation.GameDirectory);

        installation.LastOpened             = DateTime.UtcNow;
        config.LastOpenedInstallationId     = installation.Id;
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
            (true,  true)  => StartupStatus.Ready,
            (true,  false) => StartupStatus.IniMissing,
            (false, true)  => StartupStatus.ExeMissing,
            _              => StartupStatus.GameDirectoryUnknown
        };

        string? warning = status switch
        {
            StartupStatus.IniMissing => parseWarning,
            StartupStatus.ExeMissing => "zoo.exe not found in the game directory. Launching is disabled.",
            _                        => null
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

    private static StartupResult AwaitingSelection(LauncherConfig config)
        => new(StartupStatus.AwaitingUserSelection,
               GameDirectory: null, ExePath: null, IniPath: null, Model: null,
               config, Warning: null, ActiveInstallation: null, InvalidInstallations: []);

    private static StartupResult NoDirectory(LauncherConfig config, string warning)
        => new(StartupStatus.GameDirectoryUnknown,
               GameDirectory: null, ExePath: null, IniPath: null, Model: null,
               config, warning, ActiveInstallation: null, InvalidInstallations: []);
}
```

- [ ] **Step 5: Rewrite `App.axaml.cs`**

```csharp
using System;
using System.IO.Abstractions;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Erdmier.ZooTycoonLauncher.Launcher.Services;
using Erdmier.ZooTycoonLauncher.Launcher.ViewModels;
using Erdmier.ZooTycoonLauncher.Launcher.Views;

using Microsoft.Extensions.DependencyInjection;

namespace Erdmier.ZooTycoonLauncher.Launcher;

/// <summary>
///     Represents the entry point of the application. Sets up the DI container, resolves <see cref="MainWindowViewModel" />,
///     and assigns it as the main window's data context.
/// </summary>
public class App : Application
{
    /// <summary>
    ///     The application-wide service provider. Exposed as a static property so that <see cref="Views.MainWindow" />
    ///     can resolve the folder-picker shim and dialog service after it loads.
    /// </summary>
    public static IServiceProvider? Services { get; private set; }

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        Services = BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new();

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IRegistryReader, WindowsRegistryReader>();

        services.AddSingleton<ILauncherConfigService>(sp => new LauncherConfigService(sp.GetRequiredService<IFileSystem>(),
                                                                                      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));

        services.AddSingleton<IFileLocatorService, FileLocatorService>();
        services.AddSingleton<IInstallationService, InstallationService>();
        services.AddSingleton<IIniParserService, IniParserService>();
        services.AddSingleton<IVersioningService, VersioningService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<IFolderPicker, AvaloniaFolderPicker>();
        services.AddSingleton<IShellService, WindowsShellService>();
        services.AddSingleton<ILauncherService, LauncherService>();
        services.AddSingleton<IDialogService, AvaloniaDialogService>();

        services.AddTransient<InstallationPickerViewModel>();
        services.AddTransient<ManageInstallationsViewModel>();
        services.AddTransient<InvalidInstallationsViewModel>();
        services.AddTransient<IniSettingsViewModel>();
        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}
```

- [ ] **Step 6: Build and verify**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors. (`MainWindowViewModel` will have compile errors for `NullStartupService` and the old `StartupResult` constructor — ignore them for now; they are fixed in Task 9.)

> If `MainWindowViewModel` errors block the build, open it now and update `NullStartupService.EmptyResult()` to the new signature: append `, ActiveInstallation: null, InvalidInstallations: []` to each `new StartupResult(...)` call, and add `Task<StartupResult> OpenInstallationByIdAsync(Guid id) => Task.FromResult(EmptyResult());` to `NullStartupService`. The full rewrite happens in Task 9.

- [ ] **Step 7: Commit Tasks 2, 3, and 4 together**

```powershell
git add Source/Launcher/Models/LauncherConfig.cs `
        Source/Launcher/Models/StartupResult.cs `
        Source/Launcher/Services/IInstallationService.cs `
        Source/Launcher/Services/InstallationService.cs `
        Source/Launcher/Services/IFileLocatorService.cs `
        Source/Launcher/Services/FileLocatorService.cs `
        Source/Launcher/Services/IStartupService.cs `
        Source/Launcher/Services/StartupService.cs `
        Source/Launcher/App.axaml.cs
git commit -m "feat(✨): add InstallationService and rewrite startup flow for multi-installation"
```

---

### Task 5: `IDialogService`, `AvaloniaDialogService`, and `InputDialogView`

**Files:**
- Create: `Source/Launcher/Services/IDialogService.cs`
- Create: `Source/Launcher/Services/AvaloniaDialogService.cs`
- Create: `Source/Launcher/Views/InputDialogView.axaml`
- Create: `Source/Launcher/Views/InputDialogView.axaml.cs`

- [ ] **Step 1: Create `IDialogService.cs`**

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Opens modal dialogs required by the multi-installation feature.</summary>
public interface IDialogService
{
    /// <summary>Shows the installation picker and returns the selected installation, or <see langword="null" /> if the user cancelled.</summary>
    Task<Installation?> ShowPickerAsync(IEnumerable<Installation> installations);

    /// <summary>Opens the Manage Installations dialog.</summary>
    Task ShowManageAsync();

    /// <summary>Shows the combined invalid-installations alert. Fix, Remove, and Ignore actions are applied inside the dialog.</summary>
    /// <param name="invalid">Installations that failed validation.</param>
    Task ShowInvalidInstallationsAlertAsync(IReadOnlyList<Installation> invalid);

    /// <summary>Shows a yes/no confirmation dialog and returns <see langword="true" /> if the user confirmed.</summary>
    Task<bool> ConfirmAsync(string message, string title = "Confirm");

    /// <summary>Shows a single-line text-input dialog and returns the entered text, or <see langword="null" /> if the user cancelled.</summary>
    Task<string?> ShowInputAsync(string prompt, string title, string? defaultValue = null);
}
```

- [ ] **Step 2: Create `InputDialogView.axaml`**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="Erdmier.ZooTycoonLauncher.Launcher.Views.InputDialogView"
        Title="Input"
        Width="340"
        Height="140"
        CanResize="False"
        CanMaximize="False"
        CanMinimize="False"
        WindowStartupLocation="CenterOwner"
        ShowInTaskbar="False">
    <StackPanel Margin="12" Spacing="8">
        <TextBlock x:Name="PromptText" TextWrapping="Wrap" />
        <TextBox x:Name="InputBox" />
        <StackPanel Orientation="Horizontal"
                    HorizontalAlignment="Right"
                    Spacing="6">
            <Button Content="OK" Click="OnOkClick" IsDefault="True" MinWidth="70" />
            <Button Content="Cancel" Click="OnCancelClick" IsCancel="True" MinWidth="70" />
        </StackPanel>
    </StackPanel>
</Window>
```

- [ ] **Step 3: Create `InputDialogView.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Erdmier.ZooTycoonLauncher.Launcher.Views;

/// <summary>A minimal single-line text-input dialog. No ViewModel — result is returned via <see cref="Window.ShowDialog{TResult}" />.</summary>
public partial class InputDialogView : Window
{
    /// <summary>Initialises a new instance of <see cref="InputDialogView" /> with a prompt and optional pre-filled text.</summary>
    public InputDialogView(string prompt, string defaultValue = "")
    {
        InitializeComponent();
        PromptText.Text  = prompt;
        InputBox.Text    = defaultValue;
        InputBox.CaretIndex = defaultValue.Length;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
        => Close(InputBox.Text);

    private void OnCancelClick(object? sender, RoutedEventArgs e)
        => Close(result: null);
}
```

- [ ] **Step 4: Create `AvaloniaDialogService.cs`**

`AvaloniaDialogService` resolves dialog ViewModels from the DI container on demand so each dialog gets a fresh instance. `SetOwner` is called from `MainWindow.OnLoaded` — it is NOT on the `IDialogService` interface (follows the same pattern as `AvaloniaFolderPicker.SetTopLevel`).

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia.Controls;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IDialogService" />
public sealed class AvaloniaDialogService : IDialogService
{
    private readonly IServiceProvider _services;

    private Window? _owner;

    /// <summary>Initialises a new instance of <see cref="AvaloniaDialogService" />.</summary>
    public AvaloniaDialogService(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>Sets the owner window used as the parent for all modal dialogs. Call this from <c>MainWindow.OnLoaded</c>.</summary>
    public void SetOwner(Window owner) => _owner = owner;

    /// <inheritdoc />
    public async Task<Installation?> ShowPickerAsync(IEnumerable<Installation> installations)
    {
        var vm = _services.GetRequiredService<InstallationPickerViewModel>();
        vm.Load(installations);

        var dialog = new Views.InstallationPickerView { DataContext = vm };

        return await dialog.ShowDialog<Installation?>(_owner!);
    }

    /// <inheritdoc />
    public async Task ShowManageAsync()
    {
        var vm = _services.GetRequiredService<ManageInstallationsViewModel>();
        await vm.LoadAsync();

        var dialog = new Views.ManageInstallationsView { DataContext = vm };
        await dialog.ShowDialog(_owner!);
    }

    /// <inheritdoc />
    public async Task ShowInvalidInstallationsAlertAsync(IReadOnlyList<Installation> invalid)
    {
        var vm = _services.GetRequiredService<InvalidInstallationsViewModel>();
        vm.LoadEntries(invalid);

        var dialog = new Views.InvalidInstallationsView { DataContext = vm };
        await dialog.ShowDialog(_owner!);
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string message, string title = "Confirm")
    {
        var dialog = new Views.InputDialogView(message)
        {
            Title = title
        };

        // Re-use InputDialogView as a yes/no prompt: OK = true, Cancel = false.
        string? result = await dialog.ShowDialog<string?>(_owner!);

        return result is not null;
    }

    /// <inheritdoc />
    public async Task<string?> ShowInputAsync(string prompt, string title, string? defaultValue = null)
    {
        var dialog = new Views.InputDialogView(prompt, defaultValue ?? string.Empty)
        {
            Title = title
        };

        return await dialog.ShowDialog<string?>(_owner!);
    }
}
```

- [ ] **Step 5: Build and verify**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded. (`InstallationPickerViewModel`, `ManageInstallationsViewModel`, and `InvalidInstallationsViewModel` don't exist yet — if `AvaloniaDialogService` causes type-not-found errors, comment out only the three `GetRequiredService<>` lines temporarily and restore them in Task 8.)

- [ ] **Step 6: Commit**

```powershell
git add Source/Launcher/Services/IDialogService.cs `
        Source/Launcher/Services/AvaloniaDialogService.cs `
        Source/Launcher/Views/InputDialogView.axaml `
        Source/Launcher/Views/InputDialogView.axaml.cs
git commit -m "feat(✨): add IDialogService and AvaloniaDialogService with InputDialogView"
```

---

### Task 6: `InstallationPickerViewModel` and `InstallationPickerView`

**Files:**
- Create: `Source/Launcher/ViewModels/InstallationPickerViewModel.cs`
- Create: `Source/Launcher/Views/InstallationPickerView.axaml`
- Create: `Source/Launcher/Views/InstallationPickerView.axaml.cs`

- [ ] **Step 1: Create `InstallationPickerViewModel.cs`**

```csharp
using System.Collections.Generic;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>ViewModel for the installation picker dialog. Exposes the list of registered installations and the user's selection.</summary>
public sealed partial class InstallationPickerViewModel : ViewModelBase
{
    [ ObservableProperty ]
    public partial ObservableCollection<Installation> Installations { get; set; } = [];

    [ ObservableProperty ]
    public partial Installation? SelectedInstallation { get; set; }

    /// <summary>
    ///     <see langword="true" /> when the selected installation is valid and the OK button should be enabled.
    ///     Updated whenever <see cref="SelectedInstallation" /> changes.
    /// </summary>
    public bool CanConfirm => SelectedInstallation?.IsValid ?? false;

    /// <summary>Populates the picker with the given installations.</summary>
    public void Load(IEnumerable<Installation> installations)
    {
        Installations = new ObservableCollection<Installation>(installations);
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedInstallationChanged(Installation? value) => OnPropertyChanged(nameof(CanConfirm));
}
```

- [ ] **Step 2: Create `InstallationPickerView.axaml`**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Erdmier.ZooTycoonLauncher.Launcher.ViewModels"
        xmlns:models="clr-namespace:Erdmier.ZooTycoonLauncher.Launcher.Models"
        x:Class="Erdmier.ZooTycoonLauncher.Launcher.Views.InstallationPickerView"
        x:DataType="vm:InstallationPickerViewModel"
        Title="Select Installation"
        Width="420"
        Height="260"
        CanResize="False"
        CanMaximize="False"
        CanMinimize="False"
        WindowStartupLocation="CenterOwner"
        ShowInTaskbar="False">
    <DockPanel Margin="8">
        <StackPanel DockPanel.Dock="Bottom"
                    Orientation="Horizontal"
                    HorizontalAlignment="Right"
                    Spacing="6"
                    Margin="0,8,0,0">
            <Button Content="OK"
                    IsEnabled="{Binding CanConfirm}"
                    Click="OnOkClick"
                    MinWidth="70" />
            <Button Content="Cancel"
                    Click="OnCancelClick"
                    IsCancel="True"
                    MinWidth="70" />
        </StackPanel>

        <ListBox ItemsSource="{Binding Installations}"
                 SelectedItem="{Binding SelectedInstallation}">
            <ListBox.ItemTemplate>
                <DataTemplate DataType="models:Installation">
                    <TextBlock Text="{Binding DisplayName}" Padding="2" />
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </DockPanel>
</Window>
```

- [ ] **Step 3: Create `InstallationPickerView.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

namespace Erdmier.ZooTycoonLauncher.Launcher.Views;

/// <summary>Modal picker dialog that lets the user select one registered installation.</summary>
public partial class InstallationPickerView : Window
{
    /// <summary>Initialises a new instance of <see cref="InstallationPickerView" />.</summary>
    public InstallationPickerView()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
        => Close((DataContext as InstallationPickerViewModel)?.SelectedInstallation);

    private void OnCancelClick(object? sender, RoutedEventArgs e)
        => Close(result: null as Installation);
}
```

- [ ] **Step 4: Build and verify**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```powershell
git add Source/Launcher/ViewModels/InstallationPickerViewModel.cs `
        Source/Launcher/Views/InstallationPickerView.axaml `
        Source/Launcher/Views/InstallationPickerView.axaml.cs
git commit -m "feat(✨): add InstallationPickerViewModel and InstallationPickerView"
```

---

### Task 7: `ManageInstallationsViewModel` and `ManageInstallationsView`

**Files:**
- Create: `Source/Launcher/ViewModels/ManageInstallationsViewModel.cs`
- Create: `Source/Launcher/Views/ManageInstallationsView.axaml`
- Create: `Source/Launcher/Views/ManageInstallationsView.axaml.cs`

- [ ] **Step 1: Create `ManageInstallationsViewModel.cs`**

```csharp
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.Services;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>ViewModel for the Manage Installations dialog. Supports add, remove, rename, fix, and set-default operations.</summary>
public sealed partial class ManageInstallationsViewModel : ViewModelBase
{
    private readonly IDialogService _dialog;

    private readonly IFolderPicker _folderPicker;

    private readonly IInstallationService _installations;

    /// <summary>Initialises a new instance of <see cref="ManageInstallationsViewModel" />.</summary>
    public ManageInstallationsViewModel(IInstallationService installations,
                                         IFolderPicker        folderPicker,
                                         IDialogService       dialog)
    {
        _installations = installations;
        _folderPicker  = folderPicker;
        _dialog        = dialog;
    }

    [ ObservableProperty ]
    public partial ObservableCollection<Installation> Installations { get; set; } = [];

    [ ObservableProperty ]
    public partial Installation? SelectedInstallation { get; set; }

    [ ObservableProperty ]
    public partial string? StatusMessage { get; set; }

    /// <summary>Loads the current installations list from config.</summary>
    public async Task LoadAsync()
    {
        var all = await _installations.GetAllAsync();
        Installations = new ObservableCollection<Installation>(all);
    }

    [ RelayCommand ]
    private async Task AddAsync()
    {
        string? picked = await _folderPicker.PickFolderAsync("Locate Zoo Tycoon installation directory");

        if (picked is null)
        {
            return;
        }

        bool valid = await _installations.ValidateAsync(picked);

        if (!valid)
        {
            StatusMessage = "The selected directory does not contain zoo.exe.";

            return;
        }

        string? name = await _dialog.ShowInputAsync("Friendly name (optional):", "Name Installation");

        await _installations.AddAsync(picked, string.IsNullOrWhiteSpace(name) ? null : name);
        StatusMessage = null;
        await LoadAsync();
    }

    [ RelayCommand(CanExecute = nameof(HasSelection)) ]
    private async Task RemoveAsync()
    {
        if (SelectedInstallation is null)
        {
            return;
        }

        bool confirmed = await _dialog.ConfirmAsync(
            $"Remove \"{SelectedInstallation.DisplayName}\" from the launcher? The game files on disk are not affected.",
            "Remove Installation");

        if (!confirmed)
        {
            return;
        }

        await _installations.RemoveAsync(SelectedInstallation.Id);
        await LoadAsync();
    }

    [ RelayCommand(CanExecute = nameof(HasSelection)) ]
    private async Task RenameAsync()
    {
        if (SelectedInstallation is null)
        {
            return;
        }

        string? newName = await _dialog.ShowInputAsync("New name:", "Rename Installation", SelectedInstallation.Name);

        if (newName is null)
        {
            return;
        }

        await _installations.UpdateAsync(SelectedInstallation.Id, name: string.IsNullOrWhiteSpace(newName) ? null : newName);
        await LoadAsync();
    }

    [ RelayCommand(CanExecute = nameof(HasSelection)) ]
    private async Task FixAsync()
    {
        if (SelectedInstallation is null)
        {
            return;
        }

        string? picked = await _folderPicker.PickFolderAsync("Locate replacement directory for " + SelectedInstallation.DisplayName);

        if (picked is null)
        {
            return;
        }

        bool valid = await _installations.ValidateAsync(picked);

        if (!valid)
        {
            StatusMessage = "The selected directory does not contain zoo.exe.";

            return;
        }

        await _installations.UpdateAsync(SelectedInstallation.Id, gameDirectory: picked);
        StatusMessage = null;
        await LoadAsync();
    }

    [ RelayCommand(CanExecute = nameof(HasSelection)) ]
    private async Task SetAsDefaultAsync()
    {
        if (SelectedInstallation is null)
        {
            return;
        }

        await _installations.SetLastOpenedAsync(SelectedInstallation.Id);
        StatusMessage = $"\"{SelectedInstallation.DisplayName}\" set as default.";
    }

    private bool HasSelection() => SelectedInstallation is not null;

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedInstallationChanged(Installation? value)
    {
        RemoveCommand.NotifyCanExecuteChanged();
        RenameCommand.NotifyCanExecuteChanged();
        FixCommand.NotifyCanExecuteChanged();
        SetAsDefaultCommand.NotifyCanExecuteChanged();
    }
}
```

- [ ] **Step 2: Create `ManageInstallationsView.axaml`**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Erdmier.ZooTycoonLauncher.Launcher.ViewModels"
        xmlns:models="clr-namespace:Erdmier.ZooTycoonLauncher.Launcher.Models"
        x:Class="Erdmier.ZooTycoonLauncher.Launcher.Views.ManageInstallationsView"
        x:DataType="vm:ManageInstallationsViewModel"
        Title="Manage Installations"
        Width="500"
        Height="340"
        CanResize="False"
        CanMaximize="False"
        CanMinimize="False"
        WindowStartupLocation="CenterOwner"
        ShowInTaskbar="False">
    <DockPanel Margin="8">
        <StackPanel DockPanel.Dock="Bottom" Margin="0,8,0,0" Spacing="4">
            <TextBlock Text="{Binding StatusMessage}"
                       IsVisible="{Binding StatusMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"
                       Foreground="Red"
                       TextWrapping="Wrap" />
            <Button Content="Close"
                    Click="OnCloseClick"
                    HorizontalAlignment="Right"
                    MinWidth="70" />
        </StackPanel>

        <Grid ColumnDefinitions="*,Auto">
            <ListBox Grid.Column="0"
                     ItemsSource="{Binding Installations}"
                     SelectedItem="{Binding SelectedInstallation}"
                     Margin="0,0,8,0">
                <ListBox.ItemTemplate>
                    <DataTemplate DataType="models:Installation">
                        <TextBlock Text="{Binding DisplayName}" Padding="2" />
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>

            <StackPanel Grid.Column="1" Spacing="4" Width="90">
                <Button Content="Add…"
                        Command="{Binding AddCommand}"
                        HorizontalAlignment="Stretch" />
                <Button Content="Remove"
                        Command="{Binding RemoveCommand}"
                        HorizontalAlignment="Stretch" />
                <Button Content="Rename…"
                        Command="{Binding RenameCommand}"
                        HorizontalAlignment="Stretch" />
                <Button Content="Fix…"
                        Command="{Binding FixCommand}"
                        HorizontalAlignment="Stretch" />
                <Separator />
                <Button Content="Set Default"
                        Command="{Binding SetAsDefaultCommand}"
                        HorizontalAlignment="Stretch" />
            </StackPanel>
        </Grid>
    </DockPanel>
</Window>
```

- [ ] **Step 3: Create `ManageInstallationsView.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Erdmier.ZooTycoonLauncher.Launcher.Views;

/// <summary>List-editor dialog for adding, removing, renaming, fixing, and setting the default installation.</summary>
public partial class ManageInstallationsView : Window
{
    /// <summary>Initialises a new instance of <see cref="ManageInstallationsView" />.</summary>
    public ManageInstallationsView()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
```

- [ ] **Step 4: Build and verify**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```powershell
git add Source/Launcher/ViewModels/ManageInstallationsViewModel.cs `
        Source/Launcher/Views/ManageInstallationsView.axaml `
        Source/Launcher/Views/ManageInstallationsView.axaml.cs
git commit -m "feat(✨): add ManageInstallationsViewModel and ManageInstallationsView"
```

---

### Task 8: `InvalidInstallationsViewModel` and `InvalidInstallationsView`

**Files:**
- Create: `Source/Launcher/ViewModels/InvalidInstallationsViewModel.cs`
- Create: `Source/Launcher/Views/InvalidInstallationsView.axaml`
- Create: `Source/Launcher/Views/InvalidInstallationsView.axaml.cs`

- [ ] **Step 1: Create `InvalidInstallationsViewModel.cs`**

`InvalidEntry` is a file-scoped mini-ViewModel. Each row's Fix/Remove/Ignore commands live on it so the XAML `DataTemplate` binds directly without `$parent` traversal.

```csharp
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.Services;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>ViewModel for the combined invalid-installations alert. Exposes per-row Fix / Remove / Ignore actions.</summary>
public sealed partial class InvalidInstallationsViewModel : ViewModelBase
{
    private readonly IFolderPicker _folderPicker;

    private readonly IInstallationService _installations;

    /// <summary>Initialises a new instance of <see cref="InvalidInstallationsViewModel" />.</summary>
    public InvalidInstallationsViewModel(IInstallationService installations, IFolderPicker folderPicker)
    {
        _installations = installations;
        _folderPicker  = folderPicker;
    }

    [ ObservableProperty ]
    public partial ObservableCollection<InvalidEntry> Entries { get; set; } = [];

    /// <summary><see langword="true" /> when every entry has been resolved (fixed, removed, or ignored). Enables the Continue button.</summary>
    public bool CanContinue => Entries.All(e => e.IsResolved);

    /// <summary>Populates the entry list from the given invalid installations.</summary>
    public void LoadEntries(IReadOnlyList<Installation> invalid)
    {
        Entries = new ObservableCollection<InvalidEntry>(
            invalid.Select(i => new InvalidEntry(i, _installations, _folderPicker, this)));
    }

    internal void NotifyCanContinueChanged() => OnPropertyChanged(nameof(CanContinue));
}

/// <summary>Wraps a single invalid installation entry with per-row Fix / Remove / Ignore commands.</summary>
file sealed partial class InvalidEntry : ViewModelBase
{
    private readonly IFolderPicker _folderPicker;

    private readonly IInstallationService _installations;

    private readonly InvalidInstallationsViewModel _parent;

    public InvalidEntry(Installation                  installation,
                        IInstallationService           installations,
                        IFolderPicker                  folderPicker,
                        InvalidInstallationsViewModel  parent)
    {
        Installation   = installation;
        _installations = installations;
        _folderPicker  = folderPicker;
        _parent        = parent;
    }

    public Installation Installation { get; }

    public string DisplayName => Installation.DisplayName;

    [ ObservableProperty ]
    public partial bool IsResolved { get; set; }

    [ ObservableProperty ]
    public partial string? StatusText { get; set; }

    [ RelayCommand ]
    private async Task FixAsync()
    {
        string? picked = await _folderPicker.PickFolderAsync("Locate replacement directory for " + DisplayName);

        if (picked is null)
        {
            return;
        }

        bool valid = await _installations.ValidateAsync(picked);

        if (!valid)
        {
            StatusText = "Invalid: zoo.exe not found in the selected directory.";

            return;
        }

        await _installations.UpdateAsync(Installation.Id, gameDirectory: picked);
        IsResolved = true;
        StatusText = null;
        _parent.NotifyCanContinueChanged();
    }

    [ RelayCommand ]
    private async Task RemoveAsync()
    {
        await _installations.RemoveAsync(Installation.Id);
        IsResolved = true;
        StatusText = null;
        _parent.NotifyCanContinueChanged();
    }

    [ RelayCommand ]
    private void Ignore()
    {
        IsResolved = true;
        StatusText = null;
        _parent.NotifyCanContinueChanged();
    }
}
```

- [ ] **Step 2: Create `InvalidInstallationsView.axaml`**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Erdmier.ZooTycoonLauncher.Launcher.ViewModels"
        x:Class="Erdmier.ZooTycoonLauncher.Launcher.Views.InvalidInstallationsView"
        x:DataType="vm:InvalidInstallationsViewModel"
        Title="Invalid Installations"
        Width="520"
        Height="320"
        CanResize="False"
        CanMaximize="False"
        CanMinimize="False"
        WindowStartupLocation="CenterOwner"
        ShowInTaskbar="False">
    <DockPanel Margin="8">
        <TextBlock DockPanel.Dock="Top"
                   TextWrapping="Wrap"
                   Margin="0,0,0,8"
                   Text="The following installations could not be found on disk. Fix, remove, or ignore each entry to continue." />

        <StackPanel DockPanel.Dock="Bottom"
                    Orientation="Horizontal"
                    HorizontalAlignment="Right"
                    Margin="0,8,0,0">
            <Button Content="Continue"
                    IsEnabled="{Binding CanContinue}"
                    Click="OnContinueClick"
                    MinWidth="80" />
        </StackPanel>

        <ListBox ItemsSource="{Binding Entries}">
            <ListBox.ItemTemplate>
                <DataTemplate x:DataType="vm:InvalidEntry">
                    <Grid ColumnDefinitions="*,Auto,Auto,Auto" Margin="0,3">
                        <StackPanel Grid.Column="0" VerticalAlignment="Center">
                            <TextBlock Text="{Binding DisplayName}" />
                            <TextBlock Text="{Binding StatusText}"
                                       IsVisible="{Binding StatusText, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"
                                       Foreground="Red"
                                       FontSize="11" />
                        </StackPanel>
                        <Button Grid.Column="1"
                                Content="Fix…"
                                Command="{Binding FixCommand}"
                                Margin="4,0"
                                MinWidth="50" />
                        <Button Grid.Column="2"
                                Content="Remove"
                                Command="{Binding RemoveCommand}"
                                Margin="0,0,4,0"
                                MinWidth="60" />
                        <Button Grid.Column="3"
                                Content="Ignore"
                                Command="{Binding IgnoreCommand}"
                                MinWidth="55" />
                    </Grid>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </DockPanel>
</Window>
```

> **Compiled-bindings note:** `x:DataType="vm:InvalidEntry"` in the `DataTemplate` requires `InvalidEntry` to be `public`. The class is declared `file sealed` in step 1 — change it to `public sealed` if Avalonia's XAML compiler cannot resolve the `file`-scoped type. Move it to its own file `InvalidEntry.cs` in `ViewModels/` if needed.

- [ ] **Step 3: Create `InvalidInstallationsView.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Erdmier.ZooTycoonLauncher.Launcher.Views;

/// <summary>Combined startup alert dialog listing all invalid installations with per-row Fix / Remove / Ignore actions.</summary>
public partial class InvalidInstallationsView : Window
{
    /// <summary>Initialises a new instance of <see cref="InvalidInstallationsView" />.</summary>
    public InvalidInstallationsView()
    {
        InitializeComponent();
    }

    private void OnContinueClick(object? sender, RoutedEventArgs e) => Close();
}
```

- [ ] **Step 4: Fix `InvalidEntry` visibility if needed**

If the build fails with a XAML compilation error about `vm:InvalidEntry` not being accessible, open `InvalidInstallationsViewModel.cs` and change:

```csharp
// Before
file sealed partial class InvalidEntry : ViewModelBase

// After — move to its own file Source/Launcher/ViewModels/InvalidEntry.cs
public sealed partial class InvalidEntry : ViewModelBase
```

Then create `Source/Launcher/ViewModels/InvalidEntry.cs` with just the `InvalidEntry` class body (same code, `public` instead of `file`).

- [ ] **Step 5: Build and verify**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```powershell
git add Source/Launcher/ViewModels/InvalidInstallationsViewModel.cs `
        Source/Launcher/Views/InvalidInstallationsView.axaml `
        Source/Launcher/Views/InvalidInstallationsView.axaml.cs
git commit -m "feat(✨): add InvalidInstallationsViewModel and InvalidInstallationsView"
```

---

### Task 9: Rewrite `MainWindowViewModel`

**Files:**
- Rewrite: `Source/Launcher/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: Rewrite `MainWindowViewModel.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.Services;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>Top-level orchestrating ViewModel. Owns the active installation, cached <see cref="ZooIniModel" />, and commands for installation management.</summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDialogService _dialog;

    private readonly IFolderPicker _folderPicker;

    private readonly IInstallationService _installations;

    private readonly ILauncherService _launcher;

    private readonly IShellService _shell;

    private readonly IStartupService _startup;

    /// <summary>Initialises a new instance of <see cref="MainWindowViewModel" /> with all required services.</summary>
    public MainWindowViewModel(IStartupService      startup,
                               IInstallationService  installations,
                               IDialogService        dialog,
                               IFolderPicker         folderPicker,
                               IShellService         shell,
                               ILauncherService      launcher,
                               IniSettingsViewModel  ini)
    {
        _startup       = startup;
        _installations = installations;
        _dialog        = dialog;
        _folderPicker  = folderPicker;
        _shell         = shell;
        _launcher      = launcher;
        Ini            = ini;

        Ini.PropertyChanged += OnIniPropertyChanged;
    }

    /// <summary>Parameterless constructor used by the XAML designer only.</summary>
    public MainWindowViewModel()
        : this(NullStartupService.Instance,
               NullInstallationService.Instance,
               NullDialogService.Instance,
               NullFolderPicker.Instance,
               NullShellService.Instance,
               NullLauncherService.Instance,
               new IniSettingsViewModel())
    { }

    /// <summary>The active Zoo Tycoon installation for this session. <see langword="null" /> when no installation is open.</summary>
    [ ObservableProperty ]
    public partial Installation? ActiveInstallation { get; set; }

    /// <summary>The cached persisted launcher config. Always non-null after <see cref="InitializeAsync" /> completes.</summary>
    public LauncherConfig Config { get; private set; } = new();

    [ ObservableProperty ]
    public partial string? ExePath { get; set; }

    [ ObservableProperty ]
    public partial string? GameDirectory { get; set; }

    [ ObservableProperty ]
    public partial bool HasExe { get; set; }

    [ ObservableProperty ]
    public partial bool HasIni { get; set; }

    /// <summary><see langword="true" /> when at least one installation is registered. Gates <see cref="ChangeInstallationCommand" />.</summary>
    [ ObservableProperty ]
    public partial bool HasInstallations { get; set; }

    /// <summary>
    ///     Mirrors <see cref="IniSettingsViewModel.IsDirty" />. Drives the unsaved-changes warning above the Launch button
    ///     and gates <see cref="CanLaunchGame" />.
    /// </summary>
    public bool HasPendingIniChanges => Ini.IsDirty;

    /// <summary>ViewModel for the INI Configurations tab.</summary>
    public IniSettingsViewModel Ini { get; }

    [ ObservableProperty ]
    public partial IReadOnlyList<IniDisplayEntry> IniEntries { get; set; } = [];

    [ ObservableProperty ]
    public partial string? IniPath { get; set; }

    [ ObservableProperty ]
    public partial bool IsBusy { get; set; }

    // NOTE: This implementation is temporary. Once the status bar is polished, this will be reworked or removed.
    public bool IsStatusBarVisible => !(!string.IsNullOrEmpty(StatusMessage) && StatusMessage.StartsWith(value: "Ready.", StringComparison.OrdinalIgnoreCase));

    /// <summary>The cached in-memory <c>zoo.ini</c>. Set by <see cref="InitializeAsync" /> on successful parse.</summary>
    public ZooIniModel? Model { get; private set; }

    [ ObservableProperty ]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Runs the full startup flow, handling picker and invalid-installation dialogs as needed.</summary>
    public async Task InitializeAsync()
    {
        IsBusy        = true;
        StatusMessage = "Locating Zoo Tycoon…";

        StartupResult result = await _startup.InitializeAsync();

        if (result.Status == StartupStatus.AwaitingUserSelection)
        {
            result = await HandlePickerAsync(result);
        }

        if (result.Status == StartupStatus.AllInstallationsInvalid || result.InvalidInstallations.Count > 0)
        {
            result = await HandleInvalidInstallationsAsync(result);
        }

        ApplyResult(result);
        IsBusy = false;
    }

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(IsStatusBarVisible));

    /// <summary>Opens File Explorer focused on <paramref name="path" />.</summary>
    [ RelayCommand ]
    private void RevealInExplorer(string? path) => _shell.RevealInExplorer(path);

    /// <summary>Spawns <c>zoo.exe</c>.</summary>
    [ RelayCommand(CanExecute = nameof(CanLaunchGame)) ]
    private async Task LaunchGameAsync()
    {
        if (ExePath is null)
        {
            StatusMessage = "Cannot launch: zoo.exe location is unknown.";

            return;
        }

        StatusMessage = "Launching Zoo Tycoon…";
        LaunchResult result = await _launcher.LaunchAsync(ExePath);
        StatusMessage = result.Success ? "Game launched." : $"Launch failed: {result.ErrorMessage}";
    }

    private bool CanLaunchGame() => HasExe && !HasPendingIniChanges;

    /// <summary>Opens the installation picker so the user can switch to a different registered installation.</summary>
    [ RelayCommand(CanExecute = nameof(HasInstallations)) ]
    private async Task ChangeInstallationAsync()
    {
        IReadOnlyList<Installation> all    = await _installations.GetAllAsync();
        Installation?               picked = await _dialog.ShowPickerAsync(all);

        if (picked is null)
        {
            return;
        }

        IsBusy        = true;
        StatusMessage = "Opening installation…";
        StartupResult result = await _startup.OpenInstallationByIdAsync(picked.Id);
        ApplyResult(result);
        IsBusy = false;
    }

    /// <summary>Opens the Manage Installations dialog.</summary>
    [ RelayCommand ]
    private async Task ManageInstallationsAsync()
    {
        await _dialog.ShowManageAsync();

        // Refresh HasInstallations after the dialog closes in case the user added or removed entries.
        IReadOnlyList<Installation> all = await _installations.GetAllAsync();
        HasInstallations = all.Count > 0;
        ChangeInstallationCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Opens the folder picker, registers the chosen directory, and re-runs startup.</summary>
    [ RelayCommand ]
    private async Task LocateManuallyAsync()
    {
        string? picked = await _folderPicker.PickFolderAsync("Locate Zoo Tycoon installation directory");

        if (picked is null)
        {
            return;
        }

        IsBusy        = true;
        StatusMessage = "Verifying selected directory…";
        StartupResult result = await _startup.ApplyManualDirectoryAsync(picked);
        ApplyResult(result);
        IsBusy = false;
    }

    private async Task<StartupResult> HandlePickerAsync(StartupResult result)
    {
        IReadOnlyList<Installation> all    = await _installations.GetAllAsync();
        Installation?               picked = await _dialog.ShowPickerAsync(all);

        if (picked is null)
        {
            return result; // user cancelled — keep AwaitingUserSelection
        }

        return await _startup.OpenInstallationByIdAsync(picked.Id);
    }

    private async Task<StartupResult> HandleInvalidInstallationsAsync(StartupResult result)
    {
        await _dialog.ShowInvalidInstallationsAlertAsync(result.InvalidInstallations);

        if (result.Status != StartupStatus.AllInstallationsInvalid)
        {
            return result; // partial invalid — active installation is already set
        }

        // Find the best valid installation after the user applied Fix/Remove/Ignore
        IReadOnlyList<Installation> all      = await _installations.GetAllAsync();
        Installation?               bestValid = all.Where(i => i.IsValid).OrderByDescending(i => i.LastOpened).FirstOrDefault();

        if (bestValid is not null)
        {
            return await _startup.OpenInstallationByIdAsync(bestValid.Id);
        }

        // All installations are still invalid or were removed — fall back to GameDirectoryUnknown
        return new StartupResult(StartupStatus.GameDirectoryUnknown,
                                 GameDirectory: null, ExePath: null, IniPath: null, Model: null,
                                 result.Config,
                                 "No valid installations. Use Manage Installations to add one.",
                                 ActiveInstallation: null,
                                 InvalidInstallations: []);
    }

    private void ApplyResult(StartupResult result)
    {
        Model               = result.Model;
        Config              = result.Config;
        ActiveInstallation  = result.ActiveInstallation;
        GameDirectory       = result.GameDirectory;
        IniPath             = result.IniPath;
        ExePath             = result.ExePath;
        HasExe              = result.ExePath is not null;
        HasIni              = result.Model is not null;
        HasInstallations    = result.Config.Installations.Count > 0;
        IniEntries          = BuildIniEntries(result.Model);

        StatusMessage = result.Status switch
        {
            StartupStatus.Ready                 => "Ready.",
            StartupStatus.GameDirectoryUnknown  => result.Warning ?? "Zoo Tycoon could not be located.",
            StartupStatus.IniMissing            => result.Warning ?? "Unable to find zoo.ini.",
            StartupStatus.ExeMissing            => result.Warning ?? "Unable to find zoo.exe.",
            StartupStatus.IniParseFailed        => result.Warning ?? "Failed to parse zoo.ini.",
            StartupStatus.AwaitingUserSelection => "Select an installation to continue.",
            StartupStatus.AllInstallationsInvalid => result.Warning ?? "All registered installations are invalid.",
            _                                   => string.Empty
        };

        Ini.ApplyModel(result.Model, result.IniPath);
    }

    private void OnIniPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IniSettingsViewModel.IsDirty))
        {
            return;
        }

        OnPropertyChanged(nameof(HasPendingIniChanges));
        LaunchGameCommand.NotifyCanExecuteChanged();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnHasExeChanged(bool value) => LaunchGameCommand.NotifyCanExecuteChanged();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnHasInstallationsChanged(bool value) => ChangeInstallationCommand.NotifyCanExecuteChanged();

    private static IReadOnlyList<IniDisplayEntry> BuildIniEntries(ZooIniModel? model)
    {
        if (model is null)
        {
            return [];
        }

        List<IniDisplayEntry> entries = new(ZooIniDefaults.KnownKeys.Count + model.UnknownKeys.Count);

        foreach (IniKeySpec spec in ZooIniDefaults.KnownKeys)
        {
            entries.Add(new IniDisplayEntry($"[{spec.Section}] {spec.Key}", spec.Read(model)));
        }

        foreach ((string compoundKey, string value) in model.UnknownKeys)
        {
            entries.Add(new IniDisplayEntry(compoundKey, value));
        }

        return entries;
    }
}

file sealed class NullStartupService : IStartupService
{
    public static readonly NullStartupService Instance = new();

    public Task<StartupResult> InitializeAsync() => Task.FromResult(EmptyResult());

    public Task<StartupResult> ApplyManualDirectoryAsync(string directoryPath) => Task.FromResult(EmptyResult());

    public Task<StartupResult> OpenInstallationByIdAsync(Guid id) => Task.FromResult(EmptyResult());

    private static StartupResult EmptyResult()
        => new(StartupStatus.GameDirectoryUnknown, GameDirectory: null, ExePath: null, IniPath: null,
               Model: null, new LauncherConfig(), Warning: null, ActiveInstallation: null, InvalidInstallations: []);
}

file sealed class NullInstallationService : IInstallationService
{
    public static readonly NullInstallationService Instance = new();

    public Task<bool> ValidateAsync(string gameDirectory) => Task.FromResult(false);

    public Task RevalidateAllAsync() => Task.CompletedTask;

    public Task<IReadOnlyList<Installation>> GetAllAsync() => Task.FromResult<IReadOnlyList<Installation>>([]);

    public Task<Installation> AddAsync(string gameDirectory, string? name = null)
        => Task.FromResult(new Installation { GameDirectory = gameDirectory, Name = name });

    public Task RemoveAsync(Guid id) => Task.CompletedTask;

    public Task UpdateAsync(Guid id, string? name = null, string? gameDirectory = null) => Task.CompletedTask;

    public Task SetLastOpenedAsync(Guid id) => Task.CompletedTask;

    public Task<LocatorResult> DiscoverAsync()
        => Task.FromResult(new LocatorResult(ExeFound: false, IniFound: false, ExePath: null, IniPath: null, GameDirectory: null));
}

file sealed class NullDialogService : IDialogService
{
    public static readonly NullDialogService Instance = new();

    public Task<Installation?> ShowPickerAsync(IEnumerable<Installation> installations)
        => Task.FromResult<Installation?>(null);

    public Task ShowManageAsync() => Task.CompletedTask;

    public Task ShowInvalidInstallationsAlertAsync(IReadOnlyList<Installation> invalid) => Task.CompletedTask;

    public Task<bool> ConfirmAsync(string message, string title = "Confirm") => Task.FromResult(false);

    public Task<string?> ShowInputAsync(string prompt, string title, string? defaultValue = null)
        => Task.FromResult<string?>(null);
}

file sealed class NullFolderPicker : IFolderPicker
{
    public static readonly NullFolderPicker Instance = new();

    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(null);
}

file sealed class NullShellService : IShellService
{
    public static readonly NullShellService Instance = new();

    public void RevealInExplorer(string? path) { }
}

file sealed class NullLauncherService : ILauncherService
{
    public static readonly NullLauncherService Instance = new();

    public Task<LaunchResult> LaunchAsync(string exePath)
        => Task.FromResult(new LaunchResult(Success: false, ErrorMessage: null));
}
```

- [ ] **Step 2: Build and verify**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```powershell
git add Source/Launcher/ViewModels/MainWindowViewModel.cs
git commit -m "feat(✨): update MainWindowViewModel for multi-installation startup flow"
```

---

### Task 10: Installation panel in `MainWindow.axaml` and dialog-service owner in `MainWindow.axaml.cs`

**Files:**
- Extend: `Source/Launcher/Views/MainWindow.axaml`
- Extend: `Source/Launcher/Views/MainWindow.axaml.cs`

- [ ] **Step 1: Add the installation panel and grow the window**

In `MainWindow.axaml`, make two changes:

**Change 1** — increase `Height` from `454` to `484`:
```xml
Height="484"
```

**Change 2** — add the installation panel between the `<Separator DockPanel.Dock="Top" />` (line 79) and the `<Panel DockPanel.Dock="Top">` that contains the `TabControl`. Insert:

```xml
<Border DockPanel.Dock="Top"
        Padding="6,4"
        BorderThickness="0,0,0,1">
    <Grid ColumnDefinitions="Auto,*,Auto,Auto">
        <TextBlock Grid.Column="0"
                   Text="Installation:"
                   VerticalAlignment="Center"
                   Margin="0,0,6,0" />
        <TextBlock Grid.Column="1"
                   Text="{Binding ActiveInstallation.DisplayName, FallbackValue='(none)', TargetNullValue='(none)'}"
                   VerticalAlignment="Center"
                   TextTrimming="CharacterEllipsis" />
        <Button Grid.Column="2"
                Content="Change…"
                Command="{Binding ChangeInstallationCommand}"
                Margin="6,0"
                Padding="6,2" />
        <Button Grid.Column="3"
                Content="Manage…"
                Command="{Binding ManageInstallationsCommand}"
                Padding="6,2" />
    </Grid>
</Border>
```

Also update the **File menu** — replace `Header="Locate Manually"` with `Header="Add Installation…"` to reflect the new intent.

- [ ] **Step 2: Register the dialog-service owner in `MainWindow.axaml.cs`**

In `MainWindow.axaml.cs`, inside `OnLoaded`, add after the folder-picker setup:

```csharp
if (App.Services?.GetService<IDialogService>() is AvaloniaDialogService dialogs)
{
    dialogs.SetOwner(this);
}
```

The full updated `OnLoaded` method:

```csharp
protected override async void OnLoaded(RoutedEventArgs e)
{
    base.OnLoaded(e);

    if (App.Services?.GetService<IFolderPicker>() is AvaloniaFolderPicker picker)
    {
        picker.SetTopLevel(this);
    }

    if (App.Services?.GetService<IDialogService>() is AvaloniaDialogService dialogs)
    {
        dialogs.SetOwner(this);
    }

    IniScrollViewer.PointerMoved  += OnIniScrollViewerPointerMoved;
    IniScrollViewer.PointerExited += OnIniScrollViewerPointerExited;

    if (DataContext is not MainWindowViewModel viewModel)
    {
        return;
    }

    try
    {
        await viewModel.InitializeAsync();
    }
    catch (Exception)
    {
        // The startup service catches all expected exceptions and translates them to StartupStatus values; if anything
        // still leaks through, swallow it here so a single bad disk read cannot crash the launcher on startup.
    }
}
```

The required `using` statement is already present (`using Erdmier.ZooTycoonLauncher.Launcher.Services;`). Add `using Microsoft.Extensions.DependencyInjection;` if it is not already imported.

- [ ] **Step 3: Build and verify**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```powershell
git add Source/Launcher/Views/MainWindow.axaml `
        Source/Launcher/Views/MainWindow.axaml.cs
git commit -m "feat(✨): add installation panel to MainWindow and wire dialog service owner"
```

---

### Task 11: Final integration — clean build and smoke test

- [ ] **Step 1: Clean build**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors, 0 warnings (or only pre-existing warnings).

- [ ] **Step 2: Run the application**

```powershell
dotnet run --project Source/Launcher/Launcher.csproj
```

- [ ] **Step 3: Verify the happy path (existing installation auto-discovered)**

1. Delete `%AppData%\ZooTycoonLauncher\launcher.config` if present (forces a fresh start).
2. Launch. The app should auto-discover Zoo Tycoon, register it silently, and show the installation panel with the directory path.
3. The INI Configurations tab loads and all fields are editable.

- [ ] **Step 4: Verify the Manage Installations dialog**

1. Click **Manage…** in the installation panel.
2. The dialog opens showing the auto-registered installation.
3. Click **Rename…**, enter a name, click OK. The list updates.
4. Close the dialog. The installation panel shows the new name.

- [ ] **Step 5: Verify the Change installation flow**

1. Click **Add…** in the Manage dialog and add a second valid installation directory (or the same one again with a different name).
2. Close Manage.
3. Click **Change…** in the panel. The picker shows both entries.
4. Select the second one and click OK. The panel updates.

- [ ] **Step 6: Verify the invalid-installations alert**

1. Open `launcher.config` in `%AppData%\ZooTycoonLauncher\` with a text editor.
2. Change one installation's `gameDirectory` to a non-existent path.
3. Restart the launcher. The invalid-installations alert should appear.
4. Click **Ignore** for the bad entry. The Continue button enables. Click **Continue**. The launcher opens with the valid installation.
