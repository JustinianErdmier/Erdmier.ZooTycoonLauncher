# INI Parser, File Locator, and Startup Flow — Design

**Date:** 2026-04-29
**Status:** Approved
**Related SDD sections:** §5 Architecture, §6.1 FileLocatorService, §6.2 IniParserService, §7 Data Design, §8 File Versioning System, §10 Error Handling, §11 Non-Functional Requirements

---

## 1. Goal

Implement the first executable slice of the launcher: discover the Zoo Tycoon installation, parse `zoo.ini`, and cache the resulting `ZooIniModel` plus discovered paths in memory so that subsequent milestones (settings tabs, save/undo, launch) can be wired up against a working data layer.

This milestone delivers:

- `IFileLocatorService` / `FileLocatorService` (auto + manual discovery)
- `IIniParserService` / `IniParserService` (round-trip parser with comment/order preservation)
- `ILauncherConfigService` / `LauncherConfigService` (persisted JSON config for game directory)
- `IStartupService` / `StartupService` (orchestrates the locate → parse → ensure-backup sequence)
- `IVersioningService` / `VersioningService` (interface + minimal `EnsureOriginalBackupAsync` only — full implementation deferred)
- DI wiring in `App.axaml.cs`
- `MainWindowViewModel` driving startup and exposing observable state
- Status-bar binding in `MainWindow.axaml` (minimum needed to verify the flow end-to-end)

It does **not** deliver:

- Settings tab ViewModels or Views
- Save / Undo / Full Reset commands (full versioning service)
- Launch command
- Tests (deferred to the next task)

---

## 2. Key design decisions

| # | Decision | Rationale |
|---|---|---|
| 1 | In-memory `ZooIniModel` and current paths owned by `MainWindowViewModel`. Persisted launcher config (game directory, etc.) owned by `ILauncherConfigService`. | Hybrid model: VM owns UI state, service owns persistence. Matches SDD §7.2 framing. |
| 2 | Round-trip layout preserved via `internal IniDocument? RawDocument` field on `ZooIniModel`. | Explicit, no hidden parser state. Parser remains a stateless function. |
| 3 | Use `Microsoft.Extensions.DependencyInjection` directly in `App.axaml.cs` — no `IHost`. | Matches SDD §5.3 example exactly; avoids unneeded host infrastructure. |
| 4 | A dedicated `IStartupService` orchestrates startup. `MainWindowViewModel.InitializeAsync()` calls it from the `MainWindow.Loaded` event. | Single orchestration site, easy to test, keeps the VM thin. |
| 5 | When discovery fails, `FileLocatorService` returns `LocatorResult` with `ExeFound = false`. The VM (not the service) opens the folder picker. | Keeps the service layer free of UI dependencies. The SDD's two-overload locator interface naturally supports this split. |
| 6 | All file I/O goes through `System.IO.Abstractions.IFileSystem`. Registry reads go through a hand-rolled `IRegistryReader`. | Testability without coupling tests to a real registry/filesystem. |
| 7 | Discovery order in `FileLocatorService.LocateFilesAsync()`: (1) configured directory, (2) common defaults, (3) registry, (4) return failure. | Filesystem checks are faster than registry queries; if the user has not relocated the install, the default path resolves before we query the registry. |
| 8 | `IVersioningService` interface introduced now with only `EnsureOriginalBackupAsync` implemented; other methods throw `NotImplementedException`. | Keeps the startup flow complete-end-to-end without scope-creeping into the full versioning task. |

---

## 3. Architecture

```
App.axaml.cs
└── ServiceProvider (Microsoft.Extensions.DependencyInjection)
    ├── IFileSystem               (System.IO.Abstractions.FileSystem)
    ├── IRegistryReader           (WindowsRegistryReader)
    ├── ILauncherConfigService    (LauncherConfigService)
    ├── IFileLocatorService       (FileLocatorService) ── depends on IFileSystem, IRegistryReader, ILauncherConfigService
    ├── IIniParserService         (IniParserService)   ── depends on IFileSystem
    ├── IVersioningService        (VersioningService)  ── depends on IFileSystem  (only EnsureOriginalBackupAsync implemented)
    ├── IStartupService           (StartupService)     ── depends on the four services above
    ├── IFolderPicker             (AvaloniaFolderPicker, registered after MainWindow loads)
    └── MainWindowViewModel       ── depends on IStartupService, IFolderPicker
```

Service lifetimes are all `Singleton` except `MainWindowViewModel` which is `Transient` (matches SDD §5.3).

---

## 4. Components

### 4.1 FileLocatorService (SDD §6.1)

```csharp
public interface IFileLocatorService
{
    Task<LocatorResult> LocateFilesAsync();
    Task<LocatorResult> LocateFilesAsync(string directoryPath);
}
```

**Auto-overload strategy (priority order):**

1. `ILauncherConfigService.LoadAsync()` → if `GameDirectory` is set and contains `zoo.exe`, use it.
2. Probe common defaults: `C:\Program Files (x86)\Microsoft Games\Zoo Tycoon\`, `C:\Program Files\Microsoft Games\Zoo Tycoon\`.
3. Query the registry: `HKLM\SOFTWARE\Microsoft Games\Zoo Tycoon\1.0` and `HKLM\SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0`. Try value names `Install Path`, `Install_Path`, `InstallPath`, and `Path` and use the first that resolves to an existing directory.
4. Return `LocatorResult { ExeFound = false, IniFound = false, … }` if nothing is found. Do not open a folder picker.

**Manual overload:** validates the directory contains `zoo.exe`, then checks for `zoo.ini` next to it. The auto overload internally delegates to this once a candidate directory is identified, so existence-checks are not duplicated.

**Async:** registry/file-system probes are synchronous APIs. The auto-overload wraps the strategy chain in `Task.Run` so callers' `await` does not block the UI thread.

**Dependencies:** `IFileSystem`, `IRegistryReader`, `ILauncherConfigService`.

### 4.2 IniParserService (SDD §6.2)

```csharp
public interface IIniParserService
{
    Task<ZooIniModel> ReadAsync(string iniFilePath);
    Task            WriteAsync(string iniFilePath, ZooIniModel model);
    ZooIniModel     GetDefaults();
}
```

**Supporting types (internal, in `Models/`):**

```csharp
internal sealed class IniDocument
{
    public List<IniLine> Lines { get; } = [];
}

internal abstract record IniLine;
internal sealed record IniSectionHeader(string Name, string RawText) : IniLine;
internal sealed record IniKeyValue(string Section, string Key, string Value, string RawText) : IniLine;
internal sealed record IniComment(string RawText) : IniLine;
internal sealed record IniBlank : IniLine;
```

**Known-keys registry:** `Models/ZooIniDefaults.cs` is the single source of truth (SDD §11). Each entry binds an INI section + key to a typed property on `ZooIniModel` via small typed factory helpers (`IniKeySpec.Bool`, `IniKeySpec.Int`, `IniKeySpec.Str`). Section names are matched case-insensitively. Boolean parsing accepts `"0"`/`"1"` and tolerates `true`/`false`. Out-of-range integers fall back to defaults.

**Read flow:**
1. `IFileSystem.File.ReadAllLinesAsync` → classify each line into an `IniLine`.
2. Construct a fresh `ZooIniModel` (defaults pre-populated by submodel ctors).
3. For every `IniKeyValue`: if it matches a known key, invoke its `Write` setter (with fallback-to-default on parse failure); otherwise add to `model.UnknownKeys` keyed as `"Section.Key"`.
4. Attach the `IniDocument` to `model.RawDocument`.

**Write flow:**
1. If `model.RawDocument` is null, build a fresh document by emitting `[section]` headers and known keys in `KnownKeys` order, plus any `UnknownKeys`.
2. Otherwise walk `RawDocument.Lines`, replacing the value of each `IniKeyValue` line whose section+key matches a known key with the model's current value. Append known-but-missing keys at the end of their section. Append unknown keys verbatim.
3. Atomic write per SDD §11: serialize to `iniFilePath + ".tmp"`, then `IFileSystem.File.Move(tmp, iniFilePath, overwrite: true)`.

**`GetDefaults()`:** returns `new ZooIniModel()` — submodel constructors already supply documented defaults.

**Dependencies:** `IFileSystem`.

### 4.3 LauncherConfigService

```csharp
public interface ILauncherConfigService
{
    Task<LauncherConfig> LoadAsync();
    Task SaveAsync(LauncherConfig config);
    string ConfigFilePath { get; }
}

public sealed class LauncherConfig
{
    public string? GameDirectory   { get; set; }
    public bool    MinimiseOnLaunch { get; set; }
}
```

- File location: `<appData>/ZooTycoonLauncher/launcher.config`. The `<appData>` root is supplied as a constructor parameter (defaulted at DI registration to `Environment.GetFolderPath(SpecialFolder.ApplicationData)`).
- `LoadAsync` returns a fresh default config if the file is missing, empty, or fails to parse (logs but does not throw).
- `SaveAsync` ensures the parent directory exists and writes via `JsonSerializer` with `WriteIndented = true` and camelCase property names, matching the SDD §7.2 example.
- Atomic write: temp file + `File.Move(overwrite: true)`.
- No caching: each `LoadAsync` re-reads from disk. The startup service calls `LoadAsync` once; the in-memory copy lives on `MainWindowViewModel` thereafter.

**Dependencies:** `IFileSystem`, plus the AppData root path string.

### 4.4 VersioningService (partial — `EnsureOriginalBackupAsync` only)

```csharp
public interface IVersioningService
{
    Task EnsureOriginalBackupAsync(string iniFilePath);
    Task CreateUndoSnapshotAsync(string iniFilePath);   // NotImplementedException for now
    Task<bool> RestoreUndoAsync(string iniFilePath);    // NotImplementedException for now
    Task<bool> RestoreOriginalAsync(string iniFilePath); // NotImplementedException for now
    bool UndoSnapshotExists(string iniFilePath);
    bool OriginalBackupExists(string iniFilePath);
}
```

Only `EnsureOriginalBackupAsync` and the two `*Exists` predicates are implemented in this milestone. The two `Exists` predicates are trivial and useful to have ready; the three deferred methods throw `NotImplementedException` and will be filled in when versioning is tackled as a dedicated task.

`EnsureOriginalBackupAsync` follows SDD §8.1 exactly: if `<iniFilePath>.original` does not exist, copy `<iniFilePath>` to it. Otherwise no-op.

**Dependencies:** `IFileSystem`.

### 4.5 StartupService

```csharp
public interface IStartupService
{
    Task<StartupResult> InitializeAsync();
    Task<StartupResult> ApplyManualDirectoryAsync(string directoryPath);
}

public sealed record StartupResult(
    StartupStatus Status,
    string? GameDirectory,
    string? ExePath,
    string? IniPath,
    ZooIniModel? Model,
    LauncherConfig Config,
    string? Warning);

public enum StartupStatus
{
    Ready,
    GameDirectoryUnknown,
    IniMissing,
    ExeMissing,
    IniParseFailed
}
```

**`InitializeAsync` flow:**
1. Load config.
2. Auto-locate.
3. If neither exe nor ini found → `GameDirectoryUnknown` + warning.
4. If ini found: parse it (catch + return `IniParseFailed` on error), then `EnsureOriginalBackupAsync`.
5. If `locator.GameDirectory != config.GameDirectory`, persist the discovered directory back to config.
6. Return `StartupResult` with the appropriate status.

**`ApplyManualDirectoryAsync`:** same as steps 2–6 but using the manual locator overload.

**Dependencies:** `ILauncherConfigService`, `IFileLocatorService`, `IIniParserService`, `IVersioningService`.

### 4.6 MainWindowViewModel

Replace the `Greeting` placeholder with:

- Observable state: `IsBusy`, `StatusMessage`, `HasIni`, `HasExe`, `GameDirectory`, `IniPath`, `ExePath`, plus `Model` (the cached `ZooIniModel`) and `Config` (the cached `LauncherConfig`).
- Constructor: takes `IStartupService` and `IFolderPicker`.
- `InitializeAsync` — called from `MainWindow.Loaded`; sets `IsBusy`, calls the startup service, applies the result.
- `LocateManuallyAsync` command — opens the picker via `IFolderPicker`, then calls `_startup.ApplyManualDirectoryAsync(picked)` and re-applies the result.

`IFolderPicker` is a small interface registered with the DI container after `MainWindow` loads (the Avalonia implementation needs the active `TopLevel`).

### 4.7 MainWindow.axaml / .axaml.cs

- Add a status bar at the bottom of the `DockPanel`: `ProgressBar IsIndeterminate="{Binding IsBusy}"` next to `TextBlock Text="{Binding StatusMessage}"`.
- Bind `IsEnabled` of the existing `TabControl` to `HasIni`.
- Bind `IsEnabled` of the "Launch Game" menu item to `HasExe`.
- `MainWindow.axaml.cs.Loaded += async (_, _) => await ((MainWindowViewModel)DataContext).InitializeAsync();`
- Register the `IFolderPicker` Avalonia implementation against the DI container after the window loads.

### 4.8 App.axaml.cs

Build a `ServiceCollection`, register the services as singletons + `MainWindowViewModel` as transient, build the provider, resolve the VM, assign as `DataContext`. Hold the provider on `App` for the lifetime of the application.

---

## 5. Data flow

```
App.OnFrameworkInitializationCompleted
  ↓
ServiceProvider built; MainWindowViewModel resolved; assigned as DataContext
  ↓
MainWindow.Loaded fires
  ↓
MainWindowViewModel.InitializeAsync()
  ↓
IStartupService.InitializeAsync()
  ├── ILauncherConfigService.LoadAsync()       → LauncherConfig
  ├── IFileLocatorService.LocateFilesAsync()    → LocatorResult
  ├── IIniParserService.ReadAsync(iniPath)      → ZooIniModel  (only if ini found)
  ├── IVersioningService.EnsureOriginalBackupAsync(iniPath)
  └── ILauncherConfigService.SaveAsync(config)  (only if directory changed)
  ↓
StartupResult flows back to VM; observable properties updated; status bar reflects state
```

---

## 6. Error handling

| Failure | Where caught | Surfaced as |
|---|---|---|
| Registry/file-system probes throw during locate | `FileLocatorService` (try/catch around each strategy step) | Treated as "not found"; chain continues to next step |
| `zoo.exe` not found by any strategy | `StartupResult.Status` | `GameDirectoryUnknown` or `ExeMissing`; status warning + Launch disabled + manual-locate prompt |
| `zoo.ini` not found in located directory | `StartupResult.Status` | `IniMissing`; status warning, settings tabs disabled |
| `zoo.ini` exists but disk read fails | `IniParserService.ReadAsync` propagates; `StartupService` catches | `IniParseFailed`; warning text contains the OS error message; settings tabs disabled |
| Single malformed line in `zoo.ini` | `IniParserService` — line stored as `UnknownKey` if it has `=`, else verbatim as comment-or-blank; never throws | Parsing continues; no warning |
| Unknown value type/range (e.g. `fullscreen=hello`) | `IniKeySpec` setter falls back to default | Parsing continues; no warning (matches SDD §6.2) |
| `launcher.config` missing/corrupt | `LauncherConfigService.LoadAsync` returns a fresh default | No warning; treated as first run |
| Folder-picker cancelled | VM no-ops | Status unchanged |
| Folder-picker returns invalid directory | `StartupService.ApplyManualDirectoryAsync` returns `GameDirectoryUnknown` | Status warning: "No zoo.exe found in selected folder." |

All file I/O is wrapped in try/catch at the service boundary; the VM never sees an unhandled exception. Errors bubble up as data (status enums + warning strings).

---

## 7. Testability shape

Tests are not part of this milestone but the code is written to be testable:

- All file ops go through `System.IO.Abstractions.IFileSystem` (mockable via `MockFileSystem`).
- Registry access goes through a hand-rolled `IRegistryReader`.
- `LauncherConfigService` takes the AppData root as a ctor parameter (no direct `Environment` calls inside the service body).
- `StartupService` depends only on interfaces — pure orchestration, easy to unit-test by faking the four collaborators.
- `IFolderPicker` keeps Avalonia UI concerns out of the VM, so the VM is testable without a UI host.

---

## 8. Out of scope (deferred)

- Settings tab ViewModels and Views (next milestone).
- Save / Undo / Full Reset commands and the rest of `IVersioningService`.
- Launch command (`ILauncherService`).
- Test project and tests.
- `MinimiseOnLaunch` UI (the field exists in `LauncherConfig` for forward-compat but no UI reads it yet).
- Read-only status surface for the runtime-state keys listed in SDD §9.9 (those round-trip through the parser already; the read-only StatusViewModel that displays them is a later UI task).
