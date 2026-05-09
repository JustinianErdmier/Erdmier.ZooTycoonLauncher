# Software Design Document

## Zoo Tycoon Launcher

- **Version:** 1.2
- **Last Revision Date:** 9 May 2026
- **Status:** Living Draft — tracked alongside milestone plans under `~/Docs/Plans/` (`./Plans`)

> **Implementation status (2026-05-09).** Milestones delivered to date: file locator + registry probe + launcher-config persistence, INI parser with round-trip fidelity,
> versioning service (`zoo.ini.original` written on first run, `zoo.ini.undo` snapshot taken before every save), startup orchestration via `IStartupService`, the INI
> Configurations tab with grouped editors, dirty-tracked save/discard, hover-driven field descriptions in the status area, and the Launch Game button (with a pending-INI-changes
> guard). Deferred: a true Home/Overview tab with live system data, the Undo Last Save / Full Reset commands, Save Files tab, Custom Content tab, and the test project.
>
> **Planned revamp (pre-Save Files/Custom Content tabs).** Multi-installation management is the next major milestone: the launcher config, startup flow, and UI will all be
> reworked to support registering, naming, switching between, and validating multiple Zoo Tycoon installations. See [§5.4](#54-multi-installation-startup-flow),
> [§6.5](#65-installationservice), [§7.2](#72-launcher-configuration), and [§7.3](#73-installation-model) for the full design.

---

## Table of Contents

1. [Introduction](#1-Introduction)
2. [Scope](#2-Scope)
3. [Definitions and Acronyms](#3-Definitions-and-Acronyms)
4. [System Overview](#4-System-Overview)
5. [Architecture](#5-Architecture)
6. [Module Descriptions](#6-Module-Descriptions)
7. [Data Design](#7-Data-Design)
8. [File Versioning System](#8-File-Versioning-System)
9. [Configuration Settings Reference](#9-Configuration-Settings-Reference)
10. [Error Handling](#10-Error-Handling)
11. [Non-Functional Requirements](#11-Non-Functional-Requirements)
12. [Dependencies and Third-Party Libraries](#12-Dependencies-and-Third-Party-Libraries)
13. [Constraints and Assumptions](#13-Constraints-and-Assumptions)
14. [Open Questions](#14-Open-Questions)

---

## 1. Introduction

This document describes the software design for a custom launcher application for the game **Zoo Tycoon (2001, Microsoft Games)**. The launcher provides a graphical interface
through which users may discover, read, modify, and persist the game's `zoo.ini` configuration file, and subsequently launch the game. The launcher also implements a lightweight
file versioning system to protect users against accidental misconfiguration.

The application is a native Windows desktop application written in **C# (.NET 10)** using the **Avalonia UI** framework with the **Classic Avalonia Theme**, giving it the visual
appearance of a Windows 95/98-era application consistent with the era of the game.

---

## 2. Scope

The launcher covers the following functional areas:

- **Multi-installation management.** Users may register one or more Zoo Tycoon installations with the launcher, assign each a custom name, and switch between them. Adding or
  removing an installation in the launcher does not install or uninstall the game on the user's computer — it only registers or deregisters that directory with the launcher.
- **Automatic discovery** of `zoo.exe` and `zoo.ini` on the host machine when no installations are registered.
- **Parsing and in-memory representation** of all known `zoo.ini` keys, including round-trip fidelity for unknown keys, comments, and blank lines.
- **A tabbed GUI** allowing the user to view and edit settings grouped by category.
- **Persisting changes** back to `zoo.ini` on disk.
- **A file versioning system** providing an original backup and a single-level undo.
- **Launching `zoo.exe`** directly from the application.
- **Resetting `zoo.ini`** to factory defaults or to the last saved state.

The launcher does **not** cover:

- Mod management or installation.
- Network or multiplayer configuration.
- Any modification of the game's executable or assets.

---

## 3. Definitions and Acronyms

| Term               | Definition                                                                                               |
|--------------------|----------------------------------------------------------------------------------------------------------|
| `zoo.ini`          | The primary plain-text INI configuration file read by Zoo Tycoon at startup.                             |
| `zoo.ini.original` | A copy of `zoo.ini` taken the first time the launcher runs, representing the unmodified state.           |
| `zoo.ini.undo`     | A copy of `zoo.ini` taken immediately before each save operation, representing the previous saved state. |
| `zoo.exe`          | The Zoo Tycoon game executable.                                                                          |
| INI                | A plain-text key–value configuration file format using `[Section]` headers.                              |
| Installation       | A registered Zoo Tycoon installation: a directory containing both `zoo.exe` and `zoo.ini`.               |
| SDD                | Software Design Document.                                                                                |
| VM                 | ViewModel (in the context of the MVVM pattern).                                                          |
| MVVM               | Model–View–ViewModel architectural pattern.                                                              |
| Avalonia           | A cross-platform .NET UI framework. Used here in Windows-only mode.                                      |

---

## 4. System Overview

```
┌─────────────────────────────────────────────────────────┐
│                    Zoo Tycoon Launcher                  │
│                                                         │
│  ┌───────────────┐        ┌──────────────────────────┐  │
│  │  Avalonia UI  │◄──────►│    ViewModels (MVVM)     │  │
│  │  (Views)      │        └──────────┬───────────────┘  │
│  └───────────────┘                   │                  │
│                                      ▼                  │
│                          ┌──────────────────────────┐   │
│                          │      Service Layer        │   │
│                          │  ┌─────────────────────┐ │   │
│                          │  │  IniParserService   │ │   │
│                          │  ├─────────────────────┤ │   │
│                          │  │  FileLocatorService │ │   │
│                          │  ├─────────────────────┤ │   │
│                          │  │  InstallationService│ │   │
│                          │  ├─────────────────────┤ │   │
│                          │  │  VersioningService  │ │   │
│                          │  ├─────────────────────┤ │   │
│                          │  │  LauncherService    │ │   │
│                          │  └─────────────────────┘ │   │
│                          └──────────────────────────┘   │
│                                      │                  │
│                                      ▼                  │
│                          ┌──────────────────────────┐   │
│                          │       File System         │   │
│                          │  zoo.ini                  │   │
│                          │  zoo.ini.original         │   │
│                          │  zoo.ini.undo             │   │
│                          │  zoo.exe                  │   │
│                          │  launcher.config          │   │
│                          └──────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

The application follows the **MVVM** pattern throughout. Views bind to ViewModels; ViewModels call Services; Services interact with the file system and operating system. There is
no database or network layer.

---

## 5. Architecture

### 5.1 Architectural Pattern

**MVVM (Model–View–ViewModel)** is used as the primary pattern, consistent with Avalonia best practice. Data binding is two-way wherever settings are editable. Commands (e.g.
Save, Reset, Launch) are exposed from ViewModels as `ICommand` implementations using `CommunityToolkit.Mvvm`. Source generators (`[ObservableProperty]`, `[RelayCommand]`) are
used throughout; hand-rolled INPC is not permitted.

### 5.2 Actual Project Structure

```
Launcher/
├── Launcher.csproj
├── App.axaml / App.axaml.cs
├── Assets/
│   └── (icons, images)
├── Models/
│   ├── ZooIniModel.cs          — in-memory representation of zoo.ini
│   ├── ZooIniDefaults.cs       — single source of truth for all known INI keys (IniKeySpec list)
│   ├── IniRanges.cs            — numeric min/max constants for XAML NumericUpDown bindings
│   ├── IniDocument.cs          — raw line-by-line structure for round-trip writes
│   ├── IniDisplayEntry.cs      — flat key/value pair for the General tab read-only list
│   ├── LauncherConfig.cs       — persisted launcher settings (see §7.2)
│   ├── Installation.cs         — single registered installation entry (see §7.3)
│   ├── LaunchBehaviour.cs      — enum: OpenLastUsed | PromptToChoose
│   ├── StartupResult.cs        — output of IStartupService (status + located paths + model)
│   ├── StartupStatus.cs        — enum: Ready | GameDirectoryUnknown | IniMissing | …
│   └── LaunchResult.cs         — output of ILauncherService (Success + ErrorMessage)
├── Services/
│   ├── IIniParserService.cs / IniParserService.cs
│   ├── IFileLocatorService.cs / FileLocatorService.cs
│   ├── IInstallationService.cs / InstallationService.cs
│   ├── IVersioningService.cs / VersioningService.cs
│   ├── ILauncherService.cs / LauncherService.cs
│   ├── ILauncherConfigService.cs / LauncherConfigService.cs
│   ├── IStartupService.cs / StartupService.cs
│   ├── IRegistryReader.cs / WindowsRegistryReader.cs
│   ├── IFolderPicker.cs / AvaloniaFolderPicker.cs
│   └── IShellService.cs / WindowsShellService.cs
├── ViewModels/
│   ├── ViewModelBase.cs
│   ├── MainWindowViewModel.cs  — top-level orchestrator
│   └── IniSettingsViewModel.cs — INI Configurations tab
└── Views/
    ├── MainWindow.axaml / MainWindow.axaml.cs
    └── ViewLocator.cs
```

### 5.3 Dependency Injection

Services are registered in `App.OnFrameworkInitializationCompleted` using
`Microsoft.Extensions.DependencyInjection` and exposed via `App.Services`. ViewModels receive their dependencies via constructor injection. The folder picker depends on the live
`TopLevel`, so `MainWindow.OnLoaded` hands it in once the window exists.

```csharp
// App.axaml.cs (actual registrations)
services.AddSingleton<IFileSystem, FileSystem>();               // System.IO.Abstractions
services.AddSingleton<IRegistryReader, WindowsRegistryReader>();
services.AddSingleton<ILauncherConfigService, LauncherConfigService>();
services.AddSingleton<IFileLocatorService, FileLocatorService>();
services.AddSingleton<IInstallationService, InstallationService>();
services.AddSingleton<IIniParserService, IniParserService>();
services.AddSingleton<IVersioningService, VersioningService>();
services.AddSingleton<ILauncherService, LauncherService>();
services.AddSingleton<IStartupService, StartupService>();
services.AddSingleton<IFolderPicker, AvaloniaFolderPicker>();
services.AddSingleton<IShellService, WindowsShellService>();
services.AddTransient<IniSettingsViewModel>();
services.AddTransient<MainWindowViewModel>();
```

### 5.4 Multi-Installation Startup Flow

On every launch, `IStartupService.InitializeAsync` evaluates the launcher config and opens the appropriate installation. The flow is governed by two config properties:
`LaunchBehaviour` and the `Installations` list.

```
┌── No installations in config ──────────────────────────────────────────────────┐
│                                                                                 │
│  LaunchBehaviour = OpenLastUsed                                                 │
│    → Auto-discover via registry / hard-coded paths (§6.1)                      │
│      ├── Found     → Register it, open it                                      │
│      └── Not found → Prompt user to locate manually                            │
│                                                                                 │
│  LaunchBehaviour = PromptToChoose                                               │
│    → Prompt user to locate an installation manually                             │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘

┌── Installations in config ──────────────────────────────────────────────────────┐
│                                                                                  │
│  LaunchBehaviour = OpenLastUsed                                                  │
│    → Validate the last-opened installation                                       │
│      ├── Valid     → Open it                                                     │
│      └── Invalid   → Try each remaining installation in config order             │
│            ├── At least one valid                                                │
│            │     → Open the first valid one                                      │
│            │     → Alert user about invalid entries (Fix / Remove / Ignore)      │
│            │       Mark invalid entries as IsValid = false in config             │
│            └── None valid                                                        │
│                  → Alert user about all invalid entries (Fix / Remove / Ignore)  │
│                  → Mark all as IsValid = false                                   │
│                  → Prompt user to locate a new installation manually             │
│                                                                                  │
│  LaunchBehaviour = PromptToChoose                                                │
│    → Show installation picker listing all registered installations               │
│    → User selects one                                                            │
│      ├── Valid     → Open it                                                     │
│      └── Invalid   → Alert user (Fix / Remove / Open Another)                   │
│                                                                                  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

**"Fix"** means the user is prompted to re-locate the directory for that installation entry — the entry is updated in place rather than removed and re-added, preserving the
installation's `Id`, `Name`, and history. **"Remove"** deletes the entry from the config. **"Ignore"** leaves it flagged as `IsValid = false` and proceeds.

> **Note:** The launcher makes no changes to the game installation on disk during Fix, Remove, or Ignore. These operations only affect `launcher.config`.

---

## 6. Module Descriptions

### 6.1 FileLocatorService

**Responsibility:** Given a candidate directory (or no directory), confirm that `zoo.exe` and `zoo.ini` are present. Also performs automatic discovery when no directory is
provided.

**Interface:**

```csharp
public interface IFileLocatorService
{
    /// <summary>Attempts to locate zoo.exe and zoo.ini automatically.</summary>
    Task<LocatorResult> LocateFilesAsync();

    /// <summary>Confirms that zoo.exe and zoo.ini exist within the given directory.</summary>
    Task<LocatorResult> LocateFilesAsync(string directoryPath);
}
```

**Auto-discovery strategy (in priority order):**

1. Persisted `GameDirectory` from `launcher.config` (the most recently used directory).
2. Eight value-name variants under `HKLM\SOFTWARE\Microsoft Games\Zoo Tycoon\1.0` and the WOW6432Node equivalent.
3. Hard-coded paths: `%ProgramFiles(x86)%\Microsoft Games\Zoo Tycoon` and `%ProgramFiles%\Microsoft Games\Zoo Tycoon`.
4. If none of the above succeeds, it returns a failure result — the caller prompts the user via the folder picker.

---

### 6.2 IniParserService

**Responsibility:** Read `zoo.ini` from disk into a typed `ZooIniModel` and write a `ZooIniModel` back to disk, preserving comments, blank lines, and key ordering.

**Interface:**

```csharp
public interface IIniParserService
{
    Task<ZooIniModel> ReadAsync(string iniFilePath);
    Task WriteAsync(string iniFilePath, ZooIniModel model);
}
```

**Behaviour:**

- The parser tokenises the file into an `IniDocument` (a sequence of `IniLine` records: `IniSectionHeader`, `IniKeyValue`, `IniComment`, `IniBlank`). This structure is stashed
  on `ZooIniModel.RawDocument` and re-emitted verbatim on the next write operation, preserving comments, blank lines, and key ordering.
- Keys present in the file but not known to the launcher are stored verbatim in `ZooIniModel.UnknownKeys` (`"Section.Key"` → `"Value"`) and written back on every save.
- Keys absent from the file but known to the launcher are treated as having their default values and written to the file on the next save.
- All string-to-typed-value conversions are performed here, with silent fallback to the property's current value on parse failure (SDD §10).
- Writes use the temp-file-then-`Move(overwrite: true)` pattern (SDD §11) to guarantee atomic updates.

---

### 6.3 VersioningService

**Responsibility:** Manage the `zoo.ini.original` and `zoo.ini.undo` backup files. See [§8](#8-File-Versioning-System) for full detail.

**Interface:**

```csharp
public interface IVersioningService
{
    /// <summary>Creates zoo.ini.original on first launch if it does not already exist.</summary>
    Task EnsureOriginalBackupAsync(string iniFilePath);

    /// <summary>Copies the current zoo.ini to zoo.ini.undo before a save operation.</summary>
    Task CreateUndoSnapshotAsync(string iniFilePath);

    /// <summary>Restores zoo.ini from zoo.ini.undo.</summary>
    Task<bool> RestoreUndoAsync(string iniFilePath);

    /// <summary>Restores zoo.ini from zoo.ini.original.</summary>
    Task<bool> RestoreOriginalAsync(string iniFilePath);

    bool UndoSnapshotExists(string iniFilePath);
    bool OriginalBackupExists(string iniFilePath);
}
```

---

### 6.4 LauncherService

**Responsibility:** Start `zoo.exe` as a child process.

**Interface:**

```csharp
public interface ILauncherService
{
    Task<LaunchResult> LaunchAsync(string exePath);
}
```

**Behaviour:**

- The game is launched via `System.Diagnostics.Process.Start` with `UseShellExecute = true`, giving the OS the same launch semantics as a double click.
- The working directory is set to the game's installation directory so the game resolves `zoo.ini` and its assets via expected relative paths.
- The launcher remains open after launch and does not exit or minimise automatically (configurable in a future version — see §14 Q1).
- On failure, a `LaunchResult` with `Success = false` and the OS error message is returned and surfaced in the status bar.

---

### 6.5 InstallationService

**Responsibility:** Validate and manage the list of registered Zoo Tycoon installations stored in `launcher.config`. Acts as the bridge between raw directory paths and typed
`Installation` records.

**Interface:**

```csharp
public interface IInstallationService
{
    /// <summary>Returns true if the given directory contains both zoo.exe and zoo.ini.</summary>
    Task<bool> ValidateAsync(string gameDirectory);

    /// <summary>Re-validates all installations in the config and updates IsValid accordingly.</summary>
    Task RevalidateAllAsync();

    /// <summary>Registers a new installation. Throws if the directory is not valid.</summary>
    Task<Installation> AddAsync(string gameDirectory, string? name = null);

    /// <summary>Removes an installation by Id.</summary>
    Task RemoveAsync(Guid id);

    /// <summary>Updates the name or game directory of an existing installation.</summary>
    Task UpdateAsync(Guid id, string? name = null, string? gameDirectory = null);
}
```

---

### 6.6 ViewModels

All ViewModels inherit from `ViewModelBase` (`ObservableObject` from CommunityToolkit.Mvvm). Source generators are used for all observable properties (`[ObservableProperty]`
on `partial` properties) and commands (`[RelayCommand]` on `private` async methods).

**`MainWindowViewModel`** is the top-level orchestrator. It holds:

- `IniSettingsViewModel Ini` — the INI Configurations tab.
- Paths and status flags (`ExePath`, `IniPath`, `HasExe`, `HasIni`, `IsBusy`, `StatusMessage`).
- `HasPendingIniChanges` — computed mirror of `Ini.IsDirty`; gates `LaunchGameCommand` and the unsaved-changes warning.
- `IsStatusBarVisible` — computed: hides the status bar when the message is `"Ready."`.
- Commands: `LaunchGameCommand`, `LocateManuallyCommand`, `RevealInExplorerCommand`.

**`IniSettingsViewModel`** owns the INI Configurations tab. It holds:

- One observable property per editable INI key, plus a `*Tooltip` string for each (built once at construction from the `Tt.*` XAML resource dictionary and the defaults model).
- `IsDirty` — flipped on any edit, cleared on save/discard.
- `HoverDescription` — set by `MainWindow.axaml.cs` via bubbled `PointerMoved`; drives `DisplayStatus`.
- `DisplayStatus` — computed: `HoverDescription ?? StatusMessage`; shown in the status area above the Discard/Undo/Save buttons.
- Commands: `SaveCommand`, `DiscardCommand`, `UndoCommand`.

---

## 7. Data Design

### 7.1 ZooIniModel

`ZooIniModel` is a plain C# class composed of strongly typed submodels, one per INI section. It is not a record, so property-change tracking can be added if needed. Unknown
keys are preserved in `UnknownKeys` for round-trip fidelity.

```csharp
public class ZooIniModel
{
    public UserSettings     User     { get; set; } = new();
    public UiSettings       Ui       { get; set; } = new();
    public AdvancedSettings Advanced { get; set; } = new();
    public AiSettings       Ai       { get; set; } = new();
    public DebugSettings    Debug    { get; set; } = new();
    public LanguageSettings Language { get; set; } = new();
    public MapSettings      Map      { get; set; } = new();
    public Dictionary<string, string> UnknownKeys { get; set; } = new();

    internal IniDocument? RawDocument { get; init; }
}
```

See [§9](#9-Configuration-Settings-Reference) for the full settings reference.

### 7.2 Launcher Configuration

`launcher.config` is a JSON file stored in `%AppData%\ZooTycoonLauncher\`. It is managed by `ILauncherConfigService` and written atomically (temp file + `Move(overwrite: true)`).

**Model:**

```csharp
public class LauncherConfig
{
    public List<Installation> Installations       { get; set; } = [];
    public Guid?              LastOpenedInstallationId { get; set; }
    public LaunchBehaviour    LaunchBehaviour     { get; set; } = LaunchBehaviour.OpenLastUsed;
    public bool               MinimiseOnLaunch    { get; set; } = false;
}

public enum LaunchBehaviour
{
    OpenLastUsed,
    PromptToChoose
}
```

**Example `launcher.config`:**

```json
{
  "installations":            [
    {
      "id":            "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "name":          "My Zoo Tycoon",
      "gameDirectory": "C:\\Program Files (x86)\\Microsoft Games\\Zoo Tycoon",
      "isValid":       true,
      "lastOpened":    "2026-05-09T12:00:00Z"
    }
  ],
  "lastOpenedInstallationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "launchBehaviour":          "OpenLastUsed",
  "minimiseOnLaunch":         false
}
```

### 7.3 Installation Model

Each entry in `LauncherConfig.Installations` is an `Installation` record:

```csharp
public class Installation
{
    /// <summary>Stable identifier. Never changes, even if the directory or name is updated.</summary>
    public Guid      Id            { get; set; } = Guid.NewGuid();

    /// <summary>User-assigned friendly name. Null means the UI falls back to the directory path.</summary>
    public string?   Name          { get; set; }

    /// <summary>Absolute path to the directory containing zoo.exe and zoo.ini.</summary>
    public string    GameDirectory { get; set; } = string.Empty;

    /// <summary>
    ///     False if the last validation attempt found zoo.exe or zoo.ini missing. An installation
    ///     must pass validation to be added, but may become invalid afterwards (e.g. game uninstalled,
    ///     drive disconnected). Invalid installations are still retained in config until the user
    ///     explicitly removes them.
    /// </summary>
    public bool      IsValid       { get; set; } = true;

    /// <summary>UTC timestamp of the last time this installation was opened in the launcher. Null if never opened.</summary>
    public DateTime? LastOpened    { get; set; }
}
```

---

## 8. File Versioning System

The versioning system operates on two backup files that reside in the same directory as `zoo.ini`.

### 8.1 zoo.ini.original

| Property         | Detail                                                                                           |
|------------------|--------------------------------------------------------------------------------------------------|
| **Created**      | The first time the launcher successfully locates `zoo.ini`, if this file does not already exist. |
| **Updated**      | Never. This file is written once and is thereafter read-only.                                    |
| **Restored via** | "Full Reset" action in the GUI.                                                                  |
| **Purpose**      | Represents the pristine, pre-launcher state of the configuration.                                |

```csharp
// In VersioningService.EnsureOriginalBackupAsync
var originalPath = iniFilePath + ".original";
if (!File.Exists(originalPath))
    File.Copy(iniFilePath, originalPath);
```

### 8.2 zoo.ini.undo

| Property              | Detail                                                                          |
|-----------------------|---------------------------------------------------------------------------------|
| **Created / Updated** | Immediately before every successful save operation.                             |
| **Restored via**      | "Undo Last Save" action in the GUI.                                             |
| **Purpose**           | Provides a single-level undo, allowing the user to revert the most recent save. |

```csharp
// In VersioningService.CreateUndoSnapshotAsync
var undoPath = iniFilePath + ".undo";
File.Copy(iniFilePath, undoPath, overwrite: true);
```

**Save sequence (in `IniSettingsViewModel.SaveAsync`):**

1. Call `VersioningService.CreateUndoSnapshotAsync` — captures `zoo.ini` → `zoo.ini.undo`.
2. Call `IniParserService.WriteAsync` — atomically writes the new settings.
3. Re-read `zoo.ini` from disk to confirm and reflect any normalisation.
4. Set `IsDirty = false`.

**Undo sequence (in `IniSettingsViewModel.UndoCommand` — deferred):**

1. Confirm with the user via a dialogue.
2. Call `VersioningService.RestoreUndoAsync` — copies `zoo.ini.undo` over `zoo.ini`.
3. Reload and refresh all bindings.

**Full Reset sequence (deferred):**

1. Confirm with the user via a dialogue.
2. Call `VersioningService.RestoreOriginalAsync` — copies `zoo.ini.original` over `zoo.ini`.
3. Reload and refresh all bindings.

---

## 9. Configuration Settings Reference

This section documents all `zoo.ini` settings that the launcher exposes in its GUI. Keys, section names, and observed values are derived from inspection of a real `zoo.ini`
file. Each submodel in `ZooIniModel` corresponds directly to an INI section.

> **Note on defaults:** Where a setting's factory default differs from the observed file value, the known factory default is listed. Where the factory default is uncertain, the
> observed value is used and marked *(observed)*.

### 9.1 Display and Performance Settings

INI Section: `[user]`  
Model class: `UserSettings`

| Key            | Type    | Default | Range / Options       | Description                                              |
|----------------|---------|---------|-----------------------|----------------------------------------------------------|
| `fullscreen`   | Boolean | `1`     | `0`, `1`              | Run the game in fullscreen (`1`) or windowed (`0`) mode. |
| `screenwidth`  | Integer | `800`   | Detected from display | Horizontal resolution in pixels.                         |
| `screenheight` | Integer | `600`   | Detected from display | Vertical resolution in pixels.                           |
| `UpdateRate`   | Integer | `15`    | `1`–`60`              | Game logic update rate (ticks per second).               |
| `DrawRate`     | Integer | `60`    | `15`–`120`            | Target frame rate cap (FPS).                             |

> **Note:** `fullscreen` is presented in the GUI as a screen mode drop-down (Fullscreen / Windowed) alongside the width and height fields.

### 9.2 Graphics Quality Settings

INI Section: `[advanced]`  
Model class: `AdvancedSettings`

| Key             | Type    | Default | Range / Options | Description                                                                                 |
|-----------------|---------|---------|-----------------|---------------------------------------------------------------------------------------------|
| `level`         | Integer | `2`     | `0`–`4`         | Overall quality preset. `0`=Total Quality, `1`=Quality, `2`=Balance, `3`=Speed, `4`=Paused. |
| `loadHalfAnims` | Boolean | `0`     | `0`, `1`        | Load reduced-detail animation sets to improve performance.                                  |
| `drag`          | Boolean | `0`     | `0`, `1`        | Reduce rendering quality during drag operations.                                            |
| `click`         | Boolean | `0`     | `0`, `1`        | Reduce rendering quality during click operations.                                           |
| `normal`        | Boolean | `0`     | `0`, `1`        | Reduce rendering quality during normal operation.                                           |

### 9.3 Audio Settings

INI Sections: `[UI]`, `[advanced]`  
Model classes: `UiSettings`, `AdvancedSettings`

| Key                    | INI Section  | Type    | Default               | Range / Options    | Description                                                                     |
|------------------------|--------------|---------|-----------------------|--------------------|---------------------------------------------------------------------------------|
| `noMenuMusic`          | `[UI]`       | Boolean | `0`                   | `0`, `1`           | Suppress main menu background music. Note inverted logic: `1` = music disabled. |
| `menuMusic`            | `[UI]`       | String  | `sounds/mainmenu.wav` | Relative file path | Path to the main menu music file, relative to the game directory.               |
| `menuMusicAttenuation` | `[UI]`       | Integer | `1500`                | `0`–`10000`        | Attenuation applied to menu music. Higher values are quieter.                   |
| `userAttenuation`      | `[UI]`       | Integer | `0`                   | `0`–`10000`        | Additional attenuation applied globally to all game audio.                      |
| `playMovie`            | `[UI]`       | Boolean | `0`                   | `0`, `1`           | Play the first intro movie on startup.                                          |
| `movievolume1`         | `[UI]`       | Integer | `-1000`               | `-10000`–`0`       | Volume for the first intro movie. `0` = full volume, `-10000` = silent.         |
| `playSecondMovie`      | `[UI]`       | Boolean | `0`                   | `0`, `1`           | Play the second intro movie on startup.                                         |
| `movievolume2`         | `[UI]`       | Integer | `-1000`               | `-10000`–`0`       | Volume for the second intro movie.                                              |
| `use8BitSound`         | `[advanced]` | Boolean | `0`                   | `0`, `1`           | Force 8-bit audio output. May improve compatibility on older hardware.          |

### 9.4 Gameplay Settings

INI Sections: `[UI]`, `[ai]`  
Model classes: `UiSettings`, `AiSettings`

| Key               | INI Section | Type    | Default  | Range / Options   | Description                                                   |
|-------------------|-------------|---------|----------|-------------------|---------------------------------------------------------------|
| `MSStartingCash`  | `[UI]`      | Integer | `70000`  | `0`–`10,000,000`  | Cash available at the start of a new game.                    |
| `MSCashIncrement` | `[UI]`      | Integer | `5000`   | `100`–`1,000,000` | Denomination by which cash is awarded.                        |
| `MSMinCash`       | `[UI]`      | Integer | `10000`  | `0`–`10,000,000`  | Minimum cash the player can hold.                             |
| `MSMaxCash`       | `[UI]`      | Integer | `500000` | `0`–`10,000,000`  | Maximum cash the player can hold.                             |
| `maxGuests`       | `[ai]`      | Integer | `1000`   | `1`–`10,000`      | Maximum number of guests permitted in the zoo simultaneously. |

### 9.5 Interface and UI Settings

INI Section: `[UI]`  
Model class: `UiSettings`

| Key                      | Type    | Default | Range / Options | Description                                                                  |
|--------------------------|---------|---------|-----------------|------------------------------------------------------------------------------|
| `useAlternateCursors`    | Boolean | `0`     | `0`, `1`        | Use monochrome cursors. Recommended if the cursor flickers or disappears.    |
| `tooltipDelay`           | Integer | `1`     | `0`–`60`        | Delay in seconds before tooltips appear.                                     |
| `tooltipDuration`        | Integer | `3000`  | `0`–`30000`     | Duration in milliseconds that tooltips remain visible.                       |
| `MessageDisplay`         | Boolean | `1`     | `0`, `1`        | Show in-game notification messages.                                          |
| `mouseScrollThreshold`   | Integer | `1`     | `0`–`50`        | Pixel distance from the screen edge at which mouse-edge scrolling activates. |
| `mouseScrollDelay`       | Integer | `1`     | `0`–`10`        | Delay before mouse-edge scrolling begins.                                    |
| `mouseScrollX`           | Integer | `27`    | `1`–`200`       | Horizontal mouse-edge scroll speed.                                          |
| `mouseScrollY`           | Integer | `27`    | `1`–`200`       | Vertical mouse-edge scroll speed.                                            |
| `keyScrollX`             | Integer | `64`    | `1`–`200`       | Horizontal keyboard scroll speed.                                            |
| `keyScrollY`             | Integer | `64`    | `1`–`200`       | Vertical keyboard scroll speed.                                              |
| `minimumMessageInterval` | Integer | `60`    | `0`–`3600`      | Minimum interval in seconds between repeated notification messages.          |
| `helpType`               | Integer | `1`     | `0`, `1`, `2`   | Help system verbosity. `0`=Off, `1`=Standard, `2`=Verbose.                   |

### 9.6 Map Settings

INI Section: `[Map]`  
Model class: `MapSettings`

| Key    | Type    | Default | Range / Options | Description                      |
|--------|---------|---------|-----------------|----------------------------------|
| `mapX` | Integer | `75`    | `1`–`128`       | Default new zoo width in tiles.  |
| `mapY` | Integer | `75`    | `1`–`128`       | Default new zoo height in tiles. |

### 9.7 Language Settings

INI Section: `[language]`  
Model class: `LanguageSettings`

| Key       | Type    | Default          | Range / Options           | Description                          |
|-----------|---------|------------------|---------------------------|--------------------------------------|
| `lang`    | Integer | `9` *(observed)* | Windows LANGID integer    | Windows primary language identifier. |
| `sublang` | Integer | `1` *(observed)* | Windows SUBLANGID integer | Windows sub-language identifier.     |

> **Note:** `lang` and `sublang` correspond to Windows LANGID/SUBLANGID constants (e.g. `lang=9, sublang=1` = English (United States)). The launcher exposes these as a friendly
> language drop-down.

### 9.8 Debug Settings

INI Section: `[debug]`  
Model class: `DebugSettings`

| Key            | Type    | Default            | Range / Options   | Description                                                      |
|----------------|---------|--------------------|-------------------|------------------------------------------------------------------|
| `drawfps`      | Boolean | `0`                | `0`, `1`          | Display an FPS counter overlay during gameplay.                  |
| `drawfpsx`     | Integer | `720` *(observed)* | `0`–screen width  | Horizontal pixel position of the FPS counter overlay.            |
| `drawfpsy`     | Integer | `20` *(observed)*  | `0`–screen height | Vertical pixel position of the FPS counter overlay.              |
| `logCutoff`    | Integer | `1` *(observed)*   | `0`–`5`           | Logging verbosity cutoff level. Lower values are more verbose.   |
| `sendLogfile`  | Boolean | `1` *(observed)*   | `0`, `1`          | Write log output to a file on disk.                              |
| `sendDebugger` | Boolean | `1` *(observed)*   | `0`, `1`          | Send log output to an attached debugger via `OutputDebugString`. |

### 9.9 Read-Only and Runtime State Keys

The following keys are present in `zoo.ini` but are written and managed by the game at runtime. They are not editable via the launcher GUI. The INI parser preserves them verbatim
on every save.

| Key                           | INI Section | Description                                                             |
|-------------------------------|-------------|-------------------------------------------------------------------------|
| `lastfile`                    | `[user]`    | Path to the most recently opened save game file.                        |
| `showUserEntityWarning`       | `[user]`    | Records whether the user entity warning dialogue has been shown.        |
| `lastWindowX` / `lastWindowY` | `[UI]`      | Last recorded window position. Written by the game on exit.             |
| `startedFirstTutorial`        | `[UI]`      | Records whether the main campaign tutorial has been started.            |
| `startedDinoTutorial`         | `[UI]`      | Records whether the Dinosaur Digs expansion tutorial has been started.  |
| `startedAquaTutorial`         | `[UI]`      | Records whether the Marine Mania expansion tutorial has been started.   |
| `progresscalls`               | `[UI]`      | Internal counter used by the loading screen progress bar.               |
| `defaultEditCharLimit`        | `[UI]`      | Maximum character length for editable text fields. Managed by the game. |
| `completedExhibitAttenuation` | `[UI]`      | Audio attenuation value for completed exhibit notifications.            |

### 9.10 Unmanaged INI Sections

The following sections are present in `zoo.ini` but contain no user-configurable settings. They are fully preserved verbatim through the `UnknownKeys` mechanism on every save and
are never presented in the GUI.

| Section      | Description                                                                                           |
|--------------|-------------------------------------------------------------------------------------------------------|
| `[mgr]`      | Engine subsystem manager class assignments (e.g. `aimgr`, `soundmgr`). Internal engine configuration. |
| `[lib]`      | Game DLL filenames (e.g. `res0.dll`, `lang0.dll`). Not user-configurable.                             |
| `[resource]` | Semicolon-delimited asset search path list. Modified by the game when expansion packs are installed.  |
| `[scenario]` | Per-scenario completion flags (e.g. `aa=0`, `ab=1`). Written by the game on scenario completion.      |

---

## 10. Error Handling

| Scenario                                             | Behaviour                                                                                                                                                 |
|------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------|
| `zoo.exe` not found                                  | Status bar shows a warning. The Launch Game button is disabled. User is prompted to locate the game manually.                                             |
| `zoo.ini` not found                                  | Status bar shows a warning. Settings tabs are disabled.                                                                                                   |
| `zoo.ini` parse failure (malformed key)              | The offending key is stored verbatim in `UnknownKeys` and a non-blocking warning is logged. Parsing continues.                                            |
| Out-of-range or unparseable INI value                | Silently falls back to the property's current value (intentional — §11 Reliability). The original text is preserved in `RawDocument` until the next save. |
| Write failure (e.g. read-only file)                  | A modal error dialogue is shown. The undo snapshot is retained. The file on disk is not modified.                                                         |
| `zoo.ini.original` already exists on first launch    | No action is taken. The existing original is preserved.                                                                                                   |
| `zoo.ini.undo` does not exist when Undo is requested | The Undo button is disabled.                                                                                                                              |
| Game fails to launch                                 | A modal error dialogue displays the OS error message.                                                                                                     |
| Installation becomes invalid after registration      | `Installation.IsValid` is set to `false`. On next launch the startup flow alerts the user and offers Fix / Remove / Ignore (see §5.4).                    |
| All registered installations are invalid on startup  | The user is alerted to all invalid entries and offered Fix / Remove / Ignore before being prompted to locate a new installation.                          |
| User cancels installation location prompt            | The launcher opens with no active installation; all tabs that require an installation are disabled.                                                       |

All file I/O operations are wrapped in `try-catch` blocks. Exceptions are caught at the service layer and translated into result objects or status messages surfaced to the VM.

---

## 11. Non-Functional Requirements

| Category            | Requirement                                                                                                                                                                                                                 |
|---------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Performance**     | The application must launch and display its main window within 2 seconds on a modern machine. INI file parsing must complete within 500 ms.                                                                                 |
| **Reliability**     | The launcher must never corrupt `zoo.ini`. Writes are performed atomically: the new content is written to a temporary file first, then the temporary file is moved over `zoo.ini` using `File.Move` with `overwrite: true`. |
| **Usability**       | All settings display their description and default value when the field is hovered. Changed-but-unsaved values are tracked via the `IsDirty` flag. The user is prompted to confirm destructive actions (Undo, Full Reset).  |
| **Compatibility**   | Target: Windows 10 and Windows 11, x64. The launcher targets .NET 10.                                                                                                                                                       |
| **Maintainability** | All known INI keys and their defaults are defined in a single location (`ZooIniDefaults.cs`) to simplify future additions.                                                                                                  |
| **UI**              | The Classic Avalonia Theme is used throughout, giving the launcher the visual appearance of a Windows 95/98-era application consistent with the era of the game.                                                            |

---

## 12. Dependencies and Third-Party Libraries

| Package                                    | Version            | Purpose                                                  |
|--------------------------------------------|--------------------|----------------------------------------------------------|
| `Avalonia`                                 | 11.x               | UI framework                                             |
| `Classic.Avalonia.Theme`                   | *latest*           | Windows 95/98-era visual theme                           |
| `Avalonia.Desktop`                         | 11.x               | Desktop application host                                 |
| `CommunityToolkit.Mvvm`                    | 8.x                | `ObservableObject`, `RelayCommand`, source generators    |
| `Microsoft.Extensions.DependencyInjection` | 8.x                | Dependency injection container                           |
| `System.IO.Abstractions`                   | *latest*           | `IFileSystem` abstraction for testable file I/O          |
| `JetBrains.Annotations`                    | *latest*           | `[UsedImplicitly]` and other static-analysis annotations |
| `System.Text.Json`                         | *(in-box .NET 10)* | Launcher config file serialisation                       |

No third-party INI parser library is used; parsing is implemented directly to preserve comments, ordering, and round-trip fidelity.

---

## 13. Constraints and Assumptions

- The launcher targets **Zoo Tycoon (2001)** only. Compatibility with Zoo Tycoon 2 or any other title is out of scope.
- It is assumed that `zoo.ini` uses the standard Windows INI format (ANSI or UTF-8 without BOM encoding).
- It is assumed that the user has appropriate file system permissions to read and write in the game's installation directory.
- The launcher runs on **Windows only**. Avalonia's cross-platform capabilities are not exploited; Windows-specific APIs (Registry, `Process.Start` with Windows conventions) are
  used freely.
- The launcher does not require an internet connection for any functionality.
- Adding or removing an installation in the launcher does not install or uninstall the game on the user's computer. It only registers or deregisters that directory with the
  launcher.
- `zoo.exe` and `zoo.ini` must reside in the same directory. There is no supported configuration where they are split across directories.

---

## 14. Open Questions

| # | Question                                                                                                                                                                                   | Owner    | Status                                                                                |
|---|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------|---------------------------------------------------------------------------------------|
| 1 | Should the launcher minimise to the system tray when the game is launched, or remain visible?                                                                                              | Design   | Open                                                                                  |
| 2 | Should a "create default zoo.ini" option be provided when no `zoo.ini` is found?                                                                                                           | Design   | Open                                                                                  |
| 3 | Are there additional `zoo.ini` keys not yet documented here that should be exposed? Requires testing against multiple Zoo Tycoon builds and expansion packs (Dinosaur Digs, Marine Mania). | Research | Open                                                                                  |
| 4 | Should the launcher support command-line arguments to `zoo.exe` (e.g. `-f` for fullscreen)?                                                                                                | Design   | Open                                                                                  |
| 5 | What is the exact UI treatment for the installation header — a dedicated panel below the menu bar, or integrated into the window title?                                                    | Design   | Open (multi-installation milestone)                                                   |
| 6 | When the user selects "Fix" for an invalid installation, should the fix update the directory in place (preserving the entry's `Id`, `Name`, history) or create a new entry?                | Design   | Open (multi-installation milestone) — in-place update is the current intent; see §5.4 |
| 7 | Should the installation picker (shown when `LaunchBehaviour = PromptToChoose`) be a modal dialogue or a dedicated startup screen?                                                          | Design   | Open (multi-installation milestone)                                                   |

---

*End of Document*
