# Startup Flow — Software Design Document

> **Slice:** Startup Flow (Phase 1 — MVP)
> **Date:** 28 May 2026
> **Author:** Justinian
> **Status:** Approved for implementation
> **Authoritative spec:** [2026-05-26-zoo-tycoon-launcher-design.md](./2026-05-26-zoo-tycoon-launcher-design.md) §7.1, §9

---

## 1. Goal

Wire the `BootCommand` → `BootHandler` state machine (SDD §7.1.1) into the Desktop layer so the launcher transitions from its placeholder banner to one of five real main-window states on startup. The Installation Manager dialogues, the real INI snapshot service, and the screen-modes panel are deferred to follow-on slices.

---

## 2. Scope

### In scope

- `BootCommand` + `BootResult` + `BootHandler` — full §7.1.1 state machine.
- `IIniSnapshotService.SynchroniseAsync` stub (interface extension + `NullIniSnapshotService` no-op).
- `MainWindowViewModel` updated to dispatch `BootCommand` on load and route its result to the correct child VM via `ActiveContent`.
- Five main-window state UserControl + ViewModel pairs: Looking, ReadyToPlay, CannotPlay, NoGameInstallationFound, OpenGameInstallation.
- ReadyToPlay + CannotPlay receive a tab skeleton (General, INI Config, Scenarios) — placeholder content only.
- `BootHandlerTests.cs` unit tests covering every branch of §7.1.1.

### Stubbed / deferred

| Item | Stub behaviour | Deferred to |
|---|---|---|
| Add Installation dialogue | AutoLocate returns `NoGameInstallationFound` with `LocatedCandidatePath` set | Installation Manager slice |
| "Add Installation" button in NoGameInstallationFound view | Disabled, "Coming in a future update" tooltip | Installation Manager slice |
| Installation picker in OpenGameInstallation view | Placeholder label only | Installation Manager slice |
| Launch Game button | Rendered but disabled in ReadyToPlay | Launch Game slice |
| General tab content (screen modes, last-played) | Placeholder labels | Screen Modes + Launch Game slices |
| INI Config tab content | Single centred placeholder string | INI Config slice |
| Scenarios tab content | Single centred placeholder string | INI Config slice |
| Real `SynchroniseAsync` implementation | `NullIniSnapshotService` logs a warning and returns `Success` | INI Config slice |

---

## 3. Application layer

### 3.1 BootCommand + BootResult

```
Application/Boot/BootCommand.cs
Application/Boot/BootResult.cs
Application/Boot/BootHandler.cs
```

`BootCommand` is a parameterless `ICommand<ErrorOr<BootResult>>`.

`BootResult` carries the terminal state and any payload needed by the Desktop layer:

```csharp
public sealed record BootResult(
    BootOutcome Outcome,
    InstallationSummary? ActiveInstallation,
    string? LocatedCandidatePath);
```

`BootOutcome` and `BootResult` live in the same file — they are a tightly-coupled pair and are never used separately.

```csharp
public enum BootOutcome
{
    ReadyToPlay,
    CannotPlay,
    NoGameInstallationFound,
    OpenGameInstallation,
}
```

`LookingForZooTycoon` is **not** a terminal outcome — it is the VM's initial state before the command returns.

`LocatedCandidatePath` is non-null when `AutoLocate` finds a directory containing `zoo.exe` but cannot add it because the Add Installation dialogue is deferred. The `NoGameInstallationFoundViewModel` surfaces this path so the user can see what was discovered.

### 3.2 BootHandler algorithm

The handler implements SDD §7.1.1 in full. Injected dependencies: `ILauncherSettingsRepository`, `IInstallationRepository`, `IInstallationVerifier`, `IInstallationLocator`, `IIniSnapshotService`, `TimeProvider`.

```
1. settings ← GetAsync()

2. switch settings.LauncherStartupPreference:

   NoInstallation
     → return BootResult(OpenGameInstallation, null, null)

   DefaultInstallation, DefaultInstallationId = null, zero rows
     → AutoLocate

   DefaultInstallation, DefaultInstallationId = null, rows ≥ 1
     → promoted ← FindDefaultPromotionCandidateAsync()
        settings.DefaultInstallationId ← promoted.Id
        UpdateAsync(settings)
     → Verify(promoted)

   DefaultInstallation, DefaultInstallationId set
     → row ← GetByIdAsync(DefaultInstallationId)
        if null → return BootResult(NoGameInstallationFound, null, null)
     → Verify(row)

   LastPlayedInstallation
     → rows ← GetAllAsync()
        candidate ← rows.OrderByDescending(r => r.LastPlayedUtc)
                         .FirstOrDefault(r => r.LastPlayedUtc != null)
        if null → fall back to DefaultInstallation resolution
     → Verify(candidate)

   LastOpenedInstallation
     → same as LastPlayed but on LastOpenedUtc

AutoLocate:
   located ← IInstallationLocator.LocateAsync(persistedLastKnownPath: null)
   if !located.Found → return BootResult(NoGameInstallationFound, null, null)
   // Stub: Add Installation dialogue deferred; surface the found path to the user
   → return BootResult(NoGameInstallationFound, null, located.Path)

Verify(row):
   result ← IInstallationVerifier.VerifyAsync(row.Path)
   if HasExe or HasIni changed:
     row.HasExe ← result.HasExe
     row.HasIni ← result.HasIni
     row.ModifiedUtc ← clock.GetUtcNow().UtcDateTime
     UpdateAsync(row)
   if !result.HasExe → return BootResult(CannotPlay, Project(row, settings), null)

   syncResult ← IIniSnapshotService.SynchroniseAsync(row)
   if syncResult.IsError → return BootResult(CannotPlay, Project(row, settings), null)

   row.LastOpenedUtc ← clock.GetUtcNow().UtcDateTime
   UpdateAsync(row)
   → return BootResult(ReadyToPlay, Project(row, settings), null)

Project(row, settings):
   new InstallationSummary(
     row.Id, row.Name, row.Path, row.Validity,
     IsDefault: settings.DefaultInstallationId == row.Id,
     row.AddedUtc, row.ModifiedUtc, row.LastPlayedUtc, row.LastOpenedUtc)
```

**`LastOpenedUtc` stamping** happens inside the handler (not in the VM) because the handler is the thing that "opens" an installation. When the Installation Manager and picker slices land, they will use a dedicated command for the same stamp.

### 3.3 IIniSnapshotService extension

Add to the existing interface in `Application/Common/Abstractions/IIniSnapshotService.cs`:

```csharp
/// <summary>
/// Checks whether <c>zoo.ini</c> has drifted on disk since the <c>Current</c> snapshot was written. When drift is
/// detected, archives <c>Current</c> to <c>Historical</c> and writes a new <c>Current</c> from the on-disk values.
/// No-op when <see cref="GameInstallation.HasIni" /> is <see langword="false" />.
/// </summary>
/// <param name="installation">The installation to synchronise.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns><see cref="ErrorOr.Result.Success" /> on synchronisation or no-op; a typed error on parse or persist failure.</returns>
Task<ErrorOr<Success>> SynchroniseAsync(GameInstallation installation, CancellationToken cancellationToken);
```

`NullIniSnapshotService.SynchroniseAsync` returns `Result.Success` and emits a `Warning` log when `installation.HasIni` is true — identical pattern to `CaptureOriginalAsync`.

---

## 4. Desktop layer

### 4.1 MainWindowViewModel changes

- Add `[ObservableProperty] private object? _activeContent;`.
- `MainWindow.axaml.cs` calls `vm.BootCommand.Execute(null)` from `OnLoaded`; the VM's `[RelayCommand]` `BootAsync` method:
  1. Sets `ActiveContent = new LookingForZooTycoonViewModel()`.
  2. Dispatches `BootCommand` to `IMediator`.
  3. On result, creates the appropriate child VM and assigns `ActiveContent`.
  4. On error (unexpected `ErrorOr` failure), falls back to `NoGameInstallationFoundViewModel(null)`.
- `MainWindow.axaml` replaces its placeholder content with `<ContentControl Content="{Binding ActiveContent}"/>`.

### 4.2 ViewLocator convention

The existing `ViewLocator` uses `fullName.Replace("ViewModel", "View")`. This substitution naturally maps:

```
…Desktop.ViewModels.Boot.ReadyToPlayViewModel
→ …Desktop.Views.Boot.ReadyToPlayView

…Desktop.ViewModels.Tabs.GeneralTabViewModel
→ …Desktop.Views.Tabs.GeneralTabView
```

`ViewModels` → `Views` because the segment contains `"ViewModel"`. No ViewLocator changes are required.

### 4.3 State ViewModels + Views

All state VMs live under `Desktop/ViewModels/Boot/`; their views under `Desktop/Views/Boot/`.

#### LookingForZooTycoonViewModel / LookingForZooTycoonView
No data. View: centred animated ellipsis or spinner with "Looking for Zoo Tycoon…" label.

#### ReadyToPlayViewModel / ReadyToPlayView
Constructor: `ReadyToPlayViewModel(InstallationSummary installation)`.

Exposes:
- `InstallationName` (string)
- `InstallationPath` (string)
- `IsDefault` (bool)
- `GeneralTab` (`GeneralTabViewModel`)
- `IniConfigTab` (`IniConfigTabViewModel`)
- `ScenariosTab` (`ScenariosTabViewModel`)

Tab VMs are created inline (no DI). View hosts a `TabControl` with three `TabItem`s; each tab's `Content` is a `ContentControl` bound to the matching tab VM, resolved by `ViewLocator`.

#### CannotPlayViewModel / CannotPlayView
Identical structure to `ReadyToPlayViewModel`. Launch Game button in `GeneralTabView` renders as disabled with a tooltip explaining the installation is invalid.

#### NoGameInstallationFoundViewModel / NoGameInstallationFoundView
Constructor: `NoGameInstallationFoundViewModel(string? locatedCandidatePath)`.

Exposes `LocatedCandidatePath` (string?). View: status message; if `LocatedCandidatePath` is non-null, a read-only text field shows the path with label "A candidate was found at:". "Add Installation" button is disabled with tooltip "Coming in a future update".

#### OpenGameInstallationViewModel / OpenGameInstallationView
No data. View: "No installation is selected. Use the Installation Manager to add one." Picker button stub (disabled).

### 4.4 Tab ViewModels + Views

All tab VMs under `Desktop/ViewModels/Tabs/`; views under `Desktop/Views/Tabs/`.

| VM | View content |
|---|---|
| `GeneralTabViewModel` | Installation name + path (read-only labels); disabled "Launch Game" button; "Screen modes — coming soon" placeholder; "Last played — coming soon" placeholder |
| `IniConfigTabViewModel` | Single centred label: "INI configuration — coming soon" |
| `ScenariosTabViewModel` | Single centred label: "Scenarios — coming soon" |

### 4.5 Designer constructors

Every VM that accepts constructor parameters exposes a parameterless constructor that delegates to safe defaults. No `[UsedImplicitly]` attribute — per project convention the user adds it when ReSharper flags a type.

```csharp
// Example:
public ReadyToPlayViewModel() : this(new InstallationSummary(
    Guid.Empty, "Designer", @"C:\", InstallationValidity.Valid,
    IsDefault: true, DateTime.UtcNow, null, null, null)) { }
```

### 4.6 DI registration

Only `MainWindowViewModel` is registered in `AddDesktop()` — it already is from foundations. All child VMs are constructed directly by their parents with `new`. No DI changes needed.

---

## 5. Testing strategy

### BootHandlerTests.cs (Application unit tests)

| Test | What it verifies |
|---|---|
| `Handle_ReturnsReadyToPlay_WhenDefaultInstallationValid` | Happy path — stored DefaultId, valid dir, sync OK |
| `Handle_ReturnsCannotPlay_WhenVerificationFails` | DefaultId set, `HasExe = false` after verify |
| `Handle_ReturnsCannotPlay_WhenSynchroniseFails` | Verify OK, `SynchroniseAsync` returns error |
| `Handle_PersistsVerificationDrift` | HasExe/HasIni change → `UpdateAsync` called with new flags |
| `Handle_StampsLastOpenedUtc_OnReadyToPlay` | `UpdateAsync` called with non-null `LastOpenedUtc` |
| `Handle_PromotesDefault_WhenDefaultIdNullAndRowsExist` | Alphabetically-first row promoted, settings written |
| `Handle_AutoLocates_CandidateFound_ReturnsNoGameInstallationFoundWithPath` | Zero rows, locator finds path → `LocatedCandidatePath` set |
| `Handle_AutoLocates_NothingFound_ReturnsNoGameInstallationFound` | Zero rows, locator returns nothing |
| `Handle_ReturnsOpenGameInstallation_WhenPreferenceIsNoInstallation` | `NoInstallation` preference short-circuits immediately |
| `Handle_FallsBackToDefault_WhenLastPlayedHasNoCandidate` | `LastPlayedInstallation` pref, no rows have `LastPlayedUtc` |
| `Handle_FallsBackToDefault_WhenLastOpenedHasNoCandidate` | `LastOpenedInstallation` pref, no rows have `LastOpenedUtc` |

No new integration tests — `NullIniSnapshotService.SynchroniseAsync` is a one-liner stub; existing `InstallationVerifier` integration tests already cover the verify path.

No Desktop VM tests — the architecture test's `ViewModelHasViewTests` rule covers structural compliance; VMs are thin glue with no independently testable logic at this skeleton stage.

---

## 6. File manifest

### Create — Application

```
Source/Erdmier.ZooTycoonLauncher.Application/Boot/BootCommand.cs
Source/Erdmier.ZooTycoonLauncher.Application/Boot/BootResult.cs
Source/Erdmier.ZooTycoonLauncher.Application/Boot/BootHandler.cs
```

### Modify — Application

```
Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniSnapshotService.cs
Source/Erdmier.ZooTycoonLauncher.Application/GlobalUsings.cs
```

### Modify — Infrastructure

```
Source/Erdmier.ZooTycoonLauncher.Infrastructure/IniSnapshots/NullIniSnapshotService.cs
```

### Modify — Desktop

```
Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs
```

### Create — Desktop

```
Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/LookingForZooTycoonViewModel.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/ReadyToPlayViewModel.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/CannotPlayViewModel.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/NoGameInstallationFoundViewModel.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/OpenGameInstallationViewModel.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/GeneralTabViewModel.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/IniConfigTabViewModel.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/ScenariosTabViewModel.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/LookingForZooTycoonView.axaml
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/LookingForZooTycoonView.axaml.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/ReadyToPlayView.axaml
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/ReadyToPlayView.axaml.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/CannotPlayView.axaml
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/CannotPlayView.axaml.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/NoGameInstallationFoundView.axaml
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/NoGameInstallationFoundView.axaml.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/OpenGameInstallationView.axaml
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/OpenGameInstallationView.axaml.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/GeneralTabView.axaml
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/GeneralTabView.axaml.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/IniConfigTabView.axaml
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/IniConfigTabView.axaml.cs
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/ScenariosTabView.axaml
Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/ScenariosTabView.axaml.cs
```

### Create — Tests

```
Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Boot/BootHandlerTests.cs
```
