# Multi-Installation — Design

- **Date:** 2026-05-09
- **Status:** Draft
- **Companion plan:** [`2026-05-09—multi-installation.md`](./2026-05-09—multi-installation.md)
- **SDD reference:** [`SoftwareDesignDocument.md`](../SoftwareDesignDocument.md) — §5.4 Multi-Installation Startup Flow, §6.5 InstallationService, §7.2 Launcher Configuration, §7.3 Installation Model

---

## 1. Goal

Rework the launcher config, startup flow, and main window UI to support registering, naming, switching between, and validating multiple Zoo Tycoon installations. After this milestone a user can manage any number of installations from a single Manage Installations dialog and the launcher will open the correct one automatically on every launch.

---

## 2. Scope

### In scope

This milestone delivers:

- **`LaunchBehaviour` enum** — `OpenLastUsed` | `PromptToChoose`.
- **`Installation` model** — `Id`, `Name`, `GameDirectory`, `IsValid`, `LastOpened` per SDD §7.3.
- **`LauncherConfig` rewrite** — replaces the single `GameDirectory` string with `List<Installation>`, `LastOpenedInstallationId`, and `LaunchBehaviour` per SDD §7.2.
- **`IInstallationService` / `InstallationService`** — owns all CRUD operations on the installations list plus auto-discovery (registry + hard-coded paths), per SDD §6.5.
- **`FileLocatorService` simplification** — config dependency removed; the no-arg overload is deleted; the service becomes a pure stateless directory validator.
- **`StartupService` rewrite** — implements the full §5.4 branching tree (no installations → discover; `OpenLastUsed` → validate last, walk remainder, collect invalids; `PromptToChoose` → return `AwaitingUserSelection`).
- **`StartupResult` extension** — adds `ActiveInstallation` and `InvalidInstallations`; adds `AwaitingUserSelection` and `AllInstallationsInvalid` status values.
- **Dedicated installation panel** in `MainWindow.axaml` — narrow strip between menu bar and tab strip showing the active installation name, a `Change…` button, and a `Manage…` button.
- **`ManageInstallationsViewModel` / `ManageInstallationsView`** — list-editor dialog for adding, removing, renaming, fixing, and setting the default installation.
- **`InstallationPickerViewModel` / `InstallationPickerView`** — modal dialog shown on startup when `LaunchBehaviour = PromptToChoose` and from the `Change…` button.
- **`InvalidInstallationsViewModel` / `InvalidInstallationsView`** — combined startup alert dialog listing all invalid entries with per-row Fix / Remove / Ignore actions.
- **`MainWindowViewModel` updates** — `ActiveInstallation` property, `ManageInstallationsCommand`, `ChangeInstallationCommand`, and handling of the two new startup statuses.

### Out of scope

It does **not** deliver:

- **Undo Last Save / Full Reset commands** — deferred; no dependency on this milestone.
- **Home/Overview tab with live system data** — deferred; no dependency on this milestone.
- **Save Files tab** — deferred to a future milestone.
- **Custom Content tab** — deferred to a future milestone.
- **`MinimiseOnLaunch` UI** — the config field is retained but there is still no UI for it; deferred to a future milestone.
- **Test project** — deferred; code is written to be testable (services behind interfaces) but no test project is introduced in this milestone.

---

## 3. Key design decisions

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | `InstallationService` owns all discovery and validation; `FileLocatorService` becomes a pure stateless helper | Eliminates the split config-reading responsibility that existed between `FileLocatorService` and any future `InstallationService`. Matches SDD §6.5 exactly. |
| 2 | Installation picker is a modal dialog, not a startup screen | Idiomatic Windows 95/98 pattern; keeps the main window as the single application surface; far less new UI scaffolding. |
| 3 | Active installation shown in a dedicated panel below the menu bar | Makes the active installation visible without requiring the user to check the title bar; provides a natural home for `Change…` and `Manage…` buttons. |
| 4 | Invalid installation alert is one combined dialog, not one dialog per installation | Fewer interruptions at startup; user can resolve all problems in one pass. |
| 5 | `Fix…` updates the entry in-place (preserving `Id`, `Name`, `LastOpened`) | Consistent with SDD §5.4 intent; avoids orphaning history when a drive letter or path changes. |
| 6 | Old `LauncherConfig.GameDirectory` is removed with no migration | The file is user-local with no external consumers; it is simply rewritten on first launch after this milestone lands. |
| 7 | Auto-discovered installation is silently registered with `Name = null` | Avoids interrupting the happy path; the user can name it later via Manage Installations. The display name falls back to the directory path. |

---

## 4. Architecture

### 4.1 `IFileLocatorService` / `FileLocatorService` (simplified)

The `ILauncherConfigService` constructor dependency is removed. The no-arg `LocateFilesAsync()` overload is deleted from both the interface and implementation. The single remaining method is:

```csharp
Task<LocatorResult> LocateFilesAsync(string directoryPath);
```

This is a pure, stateless probe: it checks whether `zoo.exe` and `zoo.ini` exist in the given directory and returns a `LocatorResult`. It has no knowledge of registered installations or config state.

### 4.2 `IInstallationService` / `InstallationService` (new)

Constructor dependencies: `ILauncherConfigService`, `IFileLocatorService`.

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

    /// <summary>Updates the name or game directory of an existing installation in-place.</summary>
    Task UpdateAsync(Guid id, string? name = null, string? gameDirectory = null);

    /// <summary>
    ///     Runs auto-discovery (registry probes, then hard-coded paths) and returns the first valid
    ///     LocatorResult. Returns a failure result if nothing is found.
    /// </summary>
    Task<LocatorResult> DiscoverAsync();
}
```

`DiscoverAsync` absorbs the registry-probe and hard-coded-path logic previously in `FileLocatorService.LocateFilesAsync()`. The registry probe constants (`RegistryProbes`, `DefaultInstallPaths`) move to `InstallationService`.

### 4.3 `StartupService` (rewritten)

Constructor dependencies: `ILauncherConfigService`, `IInstallationService`, `IIniParserService`, `IVersioningService`.

`IFileLocatorService` is no longer a direct dependency of `StartupService`; `InstallationService` wraps it.

`InitializeAsync` implements the §5.4 branching tree:

```
No installations in config
  LaunchBehaviour = OpenLastUsed
    → DiscoverAsync()
      Found     → AddAsync (silent), open it
      Not found → return GameDirectoryUnknown
  LaunchBehaviour = PromptToChoose
    → return AwaitingUserSelection

Installations in config
  LaunchBehaviour = OpenLastUsed
    → Validate last-opened; if invalid, walk remainder in config order
      At least one valid → open first valid; collect invalids → return with InvalidInstallations list
      None valid         → return AllInstallationsInvalid with full InvalidInstallations list
  LaunchBehaviour = PromptToChoose
    → return AwaitingUserSelection (VM shows picker)
```

`ApplyManualDirectoryAsync` is retained for the "locate manually" fallback after all installations are invalid and the user opts to add a new one.

### 4.4 `StartupResult` / `StartupStatus` (extended)

New properties on `StartupResult`:

```csharp
Installation?              ActiveInstallation    // null when no installation is open
IReadOnlyList<Installation> InvalidInstallations  // empty when none failed
```

New `StartupStatus` values:

```csharp
AwaitingUserSelection,  // PromptToChoose — VM must show picker before proceeding
AllInstallationsInvalid // every registered installation failed validation
```

### 4.5 `MainWindowViewModel` (extended)

New observable properties:
- `Installation? ActiveInstallation` — bound to the dedicated panel's display name text block

New commands:
- `ManageInstallationsCommand` — constructs and opens `ManageInstallationsView` as a dialog
- `ChangeInstallationCommand` — opens `InstallationPickerView`; disabled when no installations are registered (panel shows `(none)` and only `Manage…` is active in that state)

`InitializeAsync` is extended to handle the two new statuses:
- `AwaitingUserSelection` → show `InstallationPickerView`, then call `StartupService.InitializeAsync` again with the selected installation applied
- `AllInstallationsInvalid` / partial invalid list → show `InvalidInstallationsView`; VM applies the user's Fix / Remove / Ignore decisions via `InstallationService` and persists them; then calls `StartupService.InitializeAsync` again to re-evaluate the updated installation list

### 4.6 New view pairs

| ViewModel | View | Purpose |
|-----------|------|---------|
| `ManageInstallationsViewModel` | `ManageInstallationsView` | List-editor dialog: Add, Remove, Rename, Fix, Set as Default |
| `InstallationPickerViewModel` | `InstallationPickerView` | Selection-only dialog: pick one installation from the registered list |
| `InvalidInstallationsViewModel` | `InvalidInstallationsView` | Startup alert: combined per-row Fix / Remove / Ignore for all invalid entries |

### 4.7 DI wiring

New registration added to `App.axaml.cs`:

```csharp
services.AddSingleton<IInstallationService, InstallationService>();
```

`FileLocatorService` registration is unchanged (interface contract shrinks but the concrete type name is retained).

`ManageInstallationsViewModel`, `InstallationPickerViewModel`, and `InvalidInstallationsViewModel` are registered as `Transient` (dialog-scoped).

---

## 5. Data models

### `LaunchBehaviour` (new)

```csharp
public enum LaunchBehaviour { OpenLastUsed, PromptToChoose }
```

### `Installation` (new)

```csharp
public class Installation
{
    public Guid      Id            { get; set; } = Guid.NewGuid();
    public string?   Name          { get; set; }
    public string    GameDirectory { get; set; } = string.Empty;
    public bool      IsValid       { get; set; } = true;
    public DateTime? LastOpened    { get; set; }

    /// <summary>Display name used in all UI bindings. Falls back to <see cref="GameDirectory" /> when <see cref="Name" /> is null.</summary>
    public string DisplayName => Name ?? GameDirectory;
}
```

### `LauncherConfig` (rewritten)

```csharp
public sealed class LauncherConfig
{
    public List<Installation> Installations            { get; set; } = [];
    public Guid?              LastOpenedInstallationId { get; set; }
    public LaunchBehaviour    LaunchBehaviour          { get; set; } = LaunchBehaviour.OpenLastUsed;
    public bool               MinimiseOnLaunch         { get; set; } = false;
}
```

`GameDirectory` is removed. No migration; the file is rewritten on first launch.

---

## 6. Error handling

| Condition | Handling strategy |
|-----------|-------------------|
| No installations; auto-discovery succeeds | Silently register with `Name = null`; open; update `LastOpenedInstallationId` |
| No installations; auto-discovery fails | Return `GameDirectoryUnknown`; status bar message; `Manage…` button enabled so user can add one |
| `PromptToChoose`; user cancels picker | Launcher opens with no active installation; file-dependent tabs disabled; panel shows `(none)` |
| One or more installations invalid on startup (`OpenLastUsed`) | `InvalidInstallationsView` shown; user resolves per-row; launcher proceeds with first valid installation |
| All installations invalid | `AllInstallationsInvalid` returned; `InvalidInstallationsView` shown; after resolution, `ApplyManualDirectoryAsync` called if user chose to locate a new one |
| `Fix…` chosen; new directory still invalid | Error message shown inside the dialog; Fix dialog stays open |
| `Fix…` chosen; new directory valid | `InstallationService.UpdateAsync` called in-place; `Id`, `Name`, `LastOpened` preserved; `IsValid` set to `true` |
| `Ignore` chosen for an invalid installation | `IsValid` remains `false` in config; launcher proceeds without that entry |
| `Add…` in Manage dialog; directory missing `zoo.exe` / `zoo.ini` | Error message shown in the dialog; entry not added |
| Config write fails during any installation mutation | Exception caught in `InstallationService`; surfaced as a status-bar error; in-memory state rolled back |
| All installations invalid; user cancels the final locate-manually prompt | Launcher opens with no active installation; all file-dependent tabs disabled |
