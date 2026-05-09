# Software Design Document

## Zoo Tycoon Launcher

**Version:** 1.1
**Status:** Living draft — tracked alongside milestone plans under [`Docs/Plans/`](./Plans/).
**Date:** 8 May 2026 (last revision)

> **Implementation status (2026-05-08).** Milestones delivered to date: file locator + registry probe + launcher-config persistence, INI parser with round-trip fidelity,
> versioning service (`zoo.ini.original` written on first run, `zoo.ini.undo` snapshot taken before every save), startup orchestration via `IStartupService`, the INI
> Configurations tab with grouped editors and dirty-tracked save/discard, and the Launch Game button (with a pending-INI-changes guard). Deferred: a true Home/Overview tab with
> live system data, the Undo Last Save / Full Reset commands, and the test project. See [`Docs/Plans/`](./Plans/) for per-milestone design and execution records.

---

## Table of Contents

1. [Introduction](#1-Introduction)
2. [Scope](#2-Scope)
3. [Definitions and Acronyms](#3-Definitions-and-Acronyms)
4. [System Overview](#4-System-Overview)
5. [Architecture](#5-Architecture)
6. [Example Module Descriptions](#6-Example-Module-Descriptions)
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

The application is a native Windows desktop application written in **C# (.NET 10)** using the **Avalonia UI** framework with its built-in **Fluent Theme**.

---

## 2. Scope

The launcher covers the following functional areas:

- Automatic discovery of `zoo.exe` and `zoo.ini` on the host machine.
- Parsing and in-memory representation of all known `zoo.ini` keys.
- A tabbed GUI which will allow the user to view and edit settings grouped by category.
- Persisting changes back to `zoo.ini` on disk.
- A file versioning system providing an original backup and a single-level undo.
- Launching `zoo.exe` directly from the application.
- Resetting `zoo.ini` to factory defaults or to the last saved state.

The launcher does **not** cover:

- Mod management or installation.
- Save-game management.
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
│                          └──────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

The application follows the **MVVM** pattern throughout. Views bind to ViewModels; ViewModels call Services; Services interact with the file system and operating system. There is
no database or network layer.

---

## 5. Architecture

### 5.1 Architectural Pattern

**MVVM (Model–View–ViewModel)** is used as the primary pattern, consistent with Avalonia best practice. Data binding is two-way wherever settings are editable. Commands (e.g. Save,
Reset, Launch) are exposed from ViewModels as `ICommand` implementations using CommunityToolkit.Mvvm.

### 5.2 Initial Project Structure Example

```
Launcher/
├── Launcher.csproj
├── App.axaml
├── App.axaml.cs
├── Assets/
│   └── (icons, images)
├── Models/
│   ├── ZooIniModel.cs
│   ├── UserSettings.cs
│   ├── UISettings.cs
│   ├── AdvancedSettings.cs
│   ├── AISettings.cs
│   ├── DebugSettings.cs
│   ├── LanguageSettings.cs
│   ├── MapSettings.cs
│   ├── LocatorResult.cs
│   └── LaunchResult.cs
├── Services/
│   ├── IIniParserService.cs
│   ├── IniParserService.cs
│   ├── IFileLocatorService.cs
│   ├── FileLocatorService.cs
│   ├── IVersioningService.cs
│   ├── VersioningService.cs
│   ├── ILauncherService.cs
│   └── LauncherService.cs
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   ├── StatusViewModel.cs
│   ├── DisplaySettingsViewModel.cs
│   ├── GraphicsSettingsViewModel.cs
│   ├── AudioSettingsViewModel.cs
│   ├── GameplaySettingsViewModel.cs
│   ├── InterfaceSettingsViewModel.cs
│   ├── MapSettingsViewModel.cs
│   ├── LanguageSettingsViewModel.cs
│   └── DebugSettingsViewModel.cs
└── Views/
    ├── MainWindow.axaml
    ├── MainWindow.axaml.cs
    ├── StatusView.axaml
    ├── DisplaySettingsView.axaml
    ├── GraphicsSettingsView.axaml
    ├── AudioSettingsView.axaml
    ├── GameplaySettingsView.axaml
    ├── InterfaceSettingsView.axaml
    ├── MapSettingsView.axaml
    ├── LanguageSettingsView.axaml
    └── DebugSettingsView.axaml
```

### 5.3 Dependency Injection

Services are registered in `App.OnFrameworkInitializationCompleted` using Microsoft.Extensions.DependencyInjection and exposed via `App.Services`. ViewModels receive their
dependencies via constructor injection. The actual composition root also includes `IStartupService` (orchestrates the locate → parse → ensure-backup sequence), `ILauncherConfigService`
(persists `%AppData%\ZooTycoonLauncher\launcher.config`), `IRegistryReader` / `WindowsRegistryReader` (HKLM probes), `IFolderPicker` / `AvaloniaFolderPicker`, and `IShellService`
/ `WindowsShellService`. The folder picker depends on the live `TopLevel`, so `MainWindow.OnLoaded` hands it in once the window exists.

```csharp
// Example registration (App.axaml.cs)
services.AddSingleton<IFileLocatorService, FileLocatorService>();
services.AddSingleton<IIniParserService, IniParserService>();
services.AddSingleton<IVersioningService, VersioningService>();
services.AddSingleton<ILauncherService, LauncherService>();
services.AddSingleton<IStartupService, StartupService>();
services.AddSingleton<ILauncherConfigService, LauncherConfigService>();
services.AddSingleton<IRegistryReader, WindowsRegistryReader>();
services.AddSingleton<IFolderPicker, AvaloniaFolderPicker>();
services.AddSingleton<IShellService, WindowsShellService>();
services.AddTransient<MainWindowViewModel>();
```

---

## 6. Example Module Descriptions

### 6.1 FileLocatorService

**Responsibility:** Locate `zoo.exe` and `zoo.ini` on the host file system.

**Interface:**

```csharp
public interface IFileLocatorService
{
    /// <summary>
    /// Attempts to locate zoo.exe and zoo.ini automatically.
    /// </summary>
    Task<LocatorResult> LocateFilesAsync();

    /// <summary>
    /// Allows the user to manually specify the directory containing zoo.exe.
    /// </summary>
    Task<LocatorResult> LocateFilesAsync(string directoryPath);
}
```

`LocatorResult` is defined in `Models/LocatorResult.cs`:

```csharp
public record LocatorResult(
    bool ExeFound,
    bool IniFound,
    string? ExePath,
    string? IniPath,
    string? GameDirectory
);
```

**Discovery Strategy (in priority order):**

1. Check the launcher's own persisted setting for the game directory (from a local `launcher.config` file).
2. Query the Windows Registry for Zoo Tycoon installation paths:
    - `HKLM\SOFTWARE\Microsoft Games\Zoo Tycoon\1.0`
    - `HKLM\SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0`
3. Check common default installation directories:
    - `C:\Program Files (x86)\Microsoft Games\Zoo Tycoon\`
    - `C:\Program Files\Microsoft Games\Zoo Tycoon\`
4. If none of the above succeeds, prompt the user via a folder-picker dialogue.

---

### 6.2 IniParserService

**Responsibility:** Read `zoo.ini` from disk into a typed `ZooIniModel` and write a `ZooIniModel` back to disk, preserving comments and key ordering where possible.

**Interface:**

```csharp
public interface IIniParserService
{
    Task<ZooIniModel> ReadAsync(string iniFilePath);
    Task WriteAsync(string iniFilePath, ZooIniModel model);
    ZooIniModel GetDefaults();
}
```

**Behaviour:**

- The parser reads the file line by line. Section headers (`[SectionName]`) and key–value pairs (`Key=Value`) are stored. Comment lines (`;` prefix) and blank lines are preserved
  in an ordered structure so that round-trip writes do not destroy the file's formatting.
- Keys that are present in the file but not known to the launcher are stored in a `Dictionary<string, string> UnknownKeys` collection and written back verbatim, ensuring no data
  loss.
- Keys that are absent from the file but known to the launcher are treated as having their default values; they are written to the file on the next save.
- All string-to-typed-value conversions (integers, booleans, enumerations) are performed here, with fallback to defaults on parse failure.

---

### 6.3 VersioningService

**Responsibility:** Manage the `zoo.ini.original` and `zoo.ini.undo` backup files. See [Section 8](#8-File-Versioning-System) for full detail.

**Interface:**

```csharp
public interface IVersioningService
{
    /// <summary>
    /// Called once on first launch. Creates zoo.ini.original if it does not already exist.
    /// </summary>
    Task EnsureOriginalBackupAsync(string iniFilePath);

    /// <summary>
    /// Copies the current zoo.ini to zoo.ini.undo before a save operation.
    /// </summary>
    Task CreateUndoSnapshotAsync(string iniFilePath);

    /// <summary>
    /// Restores zoo.ini from zoo.ini.undo.
    /// </summary>
    Task<bool> RestoreUndoAsync(string iniFilePath);

    /// <summary>
    /// Restores zoo.ini from zoo.ini.original.
    /// </summary>
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

`LaunchResult` is defined in `Models/LaunchResult.cs`:

```csharp
public record LaunchResult(bool Success, string? ErrorMessage);
```

**Behaviour:**

- The game is launched via `System.Diagnostics.Process.Start`.
- The working directory is set to the game's installation directory.
- The launcher remains open after launch; it does not exit or minimise automatically (this may be made configurable in a future version).
- If the process fails to start (e.g. the executable is missing or access is denied), a `LaunchResult` with `Success = false` and an appropriate `ErrorMessage` is returned.

---

### 6.5 ViewModels

Each settings category has its own ViewModel. All ViewModels inherit from a common `ViewModelBase` (which implements `INotifyPropertyChanged` via CommunityToolkit.Mvvm's
`ObservableObject`).

**MainWindowViewModel** orchestrates the application. It holds:

- `StatusViewModel` — read-only status information.
- One ViewModel per settings tab.
- Commands: `SaveChangesCommand`, `UndoLastSaveCommand`, `FullResetCommand`, `LoadIniCommand`, `LaunchGameCommand`.
- An `IsDirty` flag that tracks whether unsaved changes exist.

**Tab ViewModels** each expose observable properties corresponding to their respective settings group. Each property setter marks `IsDirty = true` on the parent
`MainWindowViewModel`.

---

## 7. Data Design

### 7.1 ZooIniModel

`ZooIniModel` is a plain C# class (not a record, to allow property change tracking if needed) that acts as the in-memory representation of `zoo.ini`. It is composed of
strongly typed submodels, one per settings category.

```csharp
public class ZooIniModel
{
    public UserSettings     User     { get; set; } = new();
    public UISettings       UI       { get; set; } = new();
    public AdvancedSettings Advanced { get; set; } = new();
    public AISettings       AI       { get; set; } = new();
    public DebugSettings    Debug    { get; set; } = new();
    public LanguageSettings Language { get; set; } = new();
    public MapSettings      Map      { get; set; } = new();
    public Dictionary<string, string> UnknownKeys { get; set; } = new();
}
```

Each submodel class exposes properties with default values matching Zoo Tycoon's known defaults. See [Section 9](#9-Configuration-Settings-Reference) for the full settings
reference.

### 7.2 Launcher Configuration

A small `launcher.config` JSON file is stored in `%AppData%\ZooTycoonLauncher\` to persist the user's game directory path and any launcher-specific preferences between sessions.
This file is entirely separate from `zoo.ini` and is managed by a lightweight `LauncherConfigService` (not described in full detail here as it is low complexity).

```json
{
  "gameDirectory": "C:\\Program Files (x86)\\Microsoft Games\\Zoo Tycoon",
  "minimiseOnLaunch": false
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

**Creation logic:**

```csharp
// In VersioningService.EnsureOriginalBackupAsync
var originalPath = iniFilePath + ".original";
if (!File.Exists(originalPath))
{
    File.Copy(iniFilePath, originalPath);
}
```

### 8.2 zoo.ini.undo

| Property              | Detail                                                                          |
|-----------------------|---------------------------------------------------------------------------------|
| **Created / Updated** | Immediately before every successful save operation.                             |
| **Restored via**      | "Undo Last Save" action in the GUI.                                             |
| **Purpose**           | Provides a single-level undo, allowing the user to revert the most recent save. |

**Snapshot logic (called before each write operation):**

```csharp
// In VersioningService.CreateUndoSnapshotAsync
var undoPath = iniFilePath + ".undo";
File.Copy(iniFilePath, undoPath, overwrite: true);
```

**Save sequence (in MainWindowViewModel.SaveChangesCommand):**

1. Call `VersioningService.CreateUndoSnapshotAsync` — captures the current `zoo.ini` to `zoo.ini.undo`.
2. Call `IniParserService.WriteAsync` — writes the new settings to `zoo.ini`.
3. Reload the in-memory model from disk to confirm the write operation succeeded.
4. Update `IsDirty = false`.

**Undo sequence (in MainWindowViewModel.UndoLastSaveCommand):**

1. Confirm with the user via a dialogue.
2. Call `VersioningService.RestoreUndoAsync` — copies `zoo.ini.undo` over `zoo.ini`.
3. Reload the in-memory model from `zoo.ini`.
4. Refresh all ViewModels.

**Full Reset sequence (in MainWindowViewModel.FullResetCommand):**

1. Confirm with the user via a dialogue.
2. Call `VersioningService.RestoreOriginalAsync` — copies `zoo.ini.original` over `zoo.ini`.
3. Reload the in-memory model from `zoo.ini`.
4. Refresh all ViewModels.

---

## 9. Configuration Settings Reference

This section documents all `zoo.ini` settings that the launcher will expose in its GUI. Keys, section names, and observed values are derived from inspection of a real `zoo.ini`
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

> **Note:** `screenwidth` and `screenheight` are presented in the GUI as a single combined resolution picker (e.g. `1280x768`) but are written as two separate keys. `fullscreen` is
> presented as a screen mode selector alongside the resolution picker.

### 9.2 Graphics Quality Settings

INI Section: `[advanced]`  
Model class: `AdvancedSettings`

The `[advanced]` section controls the game's performance/quality trade-off. The inline comments in `zoo.ini` document the meaning of the `level` enumeration values.

| Key             | Type    | Default | Range / Options | Description                                                                                 |
|-----------------|---------|---------|-----------------|---------------------------------------------------------------------------------------------|
| `level`         | Integer | `2`     | `0`–`4`         | Overall quality preset. `0`=Total Quality, `1`=Quality, `2`=Balance, `3`=Speed, `4`=Paused. |
| `loadHalfAnims` | Boolean | `0`     | `0`, `1`        | Load reduced-detail animation sets to improve performance.                                  |
| `drag`          | Boolean | `0`     | `0`, `1`        | Reduce rendering quality during drag operations.                                            |
| `click`         | Boolean | `0`     | `0`, `1`        | Reduce rendering quality during click operations.                                           |
| `normal`        | Boolean | `0`     | `0`, `1`        | Reduce rendering quality during normal operation.                                           |

### 9.3 Audio Settings

INI Sections: `[UI]`, `[advanced]`  
Model classes: `UISettings`, `AdvancedSettings`

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
Model classes: `UISettings`, `AISettings`

| Key               | INI Section | Type    | Default  | Range / Options   | Description                                                   |
|-------------------|-------------|---------|----------|-------------------|---------------------------------------------------------------|
| `MSStartingCash`  | `[UI]`      | Integer | `70000`  | `0`–`10,000,000`  | Cash available at the start of a new game.                    |
| `MSCashIncrement` | `[UI]`      | Integer | `5000`   | `100`–`1,000,000` | Denomination by which cash is awarded.                        |
| `MSMinCash`       | `[UI]`      | Integer | `10000`  | `0`–`10,000,000`  | Minimum cash the player can hold.                             |
| `MSMaxCash`       | `[UI]`      | Integer | `500000` | `0`–`10,000,000`  | Maximum cash the player can hold.                             |
| `maxGuests`       | `[ai]`      | Integer | `1000`   | `1`–`10,000`      | Maximum number of guests permitted in the zoo simultaneously. |

### 9.5 Interface and UI Settings

INI Section: `[UI]`  
Model class: `UISettings`

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

> **Note:** `lang` and `sublang` correspond to Windows LANGID/SUBLANGID constants (e.g. `lang=9, sublang=1` = English (United States)). The launcher will expose these as a friendly
> language drop-down populated from the set of language strings bundled with the game installation.

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

The following keys are present in `zoo.ini` but are written and managed by the game at runtime. They are not editable via the launcher GUI and are displayed as read-only
information in the status area where relevant. The INI parser preserves them verbatim on every save.

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

| Scenario                                             | Behaviour                                                                                                                          |
|------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------|
| `zoo.exe` not found                                  | Status area shows a warning. The "Launch Game" button is disabled. User is prompted to locate the game manually.                   |
| `zoo.ini` not found                                  | Status area shows a warning. Settings tabs are disabled. User is prompted to locate the file or create a default one.              |
| `zoo.ini` parse failure (malformed key)              | The offending key is stored verbatim in `UnknownKeys` and a non-blocking warning is logged. Parsing continues.                     |
| Write failure (e.g. read-only file)                  | A modal error dialogue is shown. The undo snapshot created before the failed write is discarded. The file on disk is not modified. |
| `zoo.ini.original` already exists on first launch    | No action is taken. The existing original is preserved.                                                                            |
| `zoo.ini.undo` does not exist when Undo is requested | The "Undo Last Save" button is disabled.                                                                                           |
| Game fails to launch                                 | A modal error dialogue displays the OS error message.                                                                              |

All file I/O operations are wrapped in `try-catch` blocks. Exceptions are caught at the service layer and translated into result objects or error messages that are surfaced to the
ViewModel layer for display.

---

## 11. Non-Functional Requirements

| Category            | Requirement                                                                                                                                                                                                                 |
|---------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Performance**     | The application must launch and display its main window within 2 seconds on a modern machine. INI file parsing must complete within 500 ms.                                                                                 |
| **Reliability**     | The launcher must never corrupt `zoo.ini`. Writes are performed atomically: the new content is written to a temporary file first, then the temporary file is moved over `zoo.ini` using `File.Move` with `overwrite: true`. |
| **Usability**       | All settings must display their default value as hint text. Changed-but-unsaved values must be visually distinguished from saved values. The user must be prompted to confirm destructive actions (Undo, Full Reset).       |
| **Compatibility**   | Target: Windows 10 and Windows 11, x64. The launcher targets .NET 10.                                                                                                                                                       |
| **Maintainability** | All known INI keys and their defaults are defined in a single location (`ZooIniDefaults.cs`) to simplify future additions.                                                                                                  |
| **UI**              | The Avalonia Fluent Theme is used throughout. No custom theme is implemented at this stage.                                                                                                                                 |

---

## 12. Dependencies and Third-Party Libraries

| Package                                    | Version            | Purpose                                               |
|--------------------------------------------|--------------------|-------------------------------------------------------|
| `Avalonia`                                 | 11.x               | UI framework                                          |
| `Avalonia.Themes.Fluent`                   | 11.x               | Fluent visual theme                                   |
| `Avalonia.Desktop`                         | 11.x               | Desktop application host                              |
| `CommunityToolkit.Mvvm`                    | 8.x                | `ObservableObject`, `RelayCommand`, source generators |
| `Microsoft.Extensions.DependencyInjection` | 8.x                | Dependency injection container                        |
| `System.Text.Json`                         | *(in-box .NET 10)* | Launcher config file serialisation                    |

No third-party INI parser library is used; parsing is implemented directly to preserve comments, ordering, and round-trip fidelity.

---

## 13. Constraints and Assumptions

- The launcher targets **Zoo Tycoon (2001)** only. Compatibility with Zoo Tycoon 2 or any other title is out of scope.
- It is assumed that `zoo.ini` uses the standard Windows INI format (ANSI or UTF-8 without BOM encoding).
- It is assumed that the user has appropriate file system permissions to read and write in the game's installation directory.
- The launcher runs on **Windows only**. Avalonia's cross-platform capabilities are not exploited; Windows-specific APIs (Registry, `Process.Start` with Windows conventions) are
  used freely.
- The launcher does not require an internet connection for any functionality.
- Only one `zoo.ini` file is managed at a time. Multi-installation support is not in scope for v1.0.

---

## 14. Open Questions

| # | Question                                                                                                                                                                                   | Owner    | Status   |
|---|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------|----------|
| 1 | Should the launcher minimise to the system tray when the game is launched, or remain visible?                                                                                              | Design   | Open     |
| 2 | Should a "create default zoo.ini" option be provided when no `zoo.ini` is found?                                                                                                           | Design   | Open     |
| 3 | Are there additional `zoo.ini` keys not yet documented here that should be exposed? Requires testing against multiple Zoo Tycoon builds and expansion packs (Dinosaur Digs, Marine Mania). | Research | Open     |
| 4 | Should the launcher support command-line arguments to `zoo.exe` (e.g. `-f` for fullscreen)?                                                                                                | Design   | Open     |
| 5 | Should a "profile" system be considered for v2.0, allowing users to save and switch between multiple INI configurations?                                                                   | Future   | Deferred |

---

*End of Document*