# Launch Game — Software Design Document

> **Slice:** Launch Game (Phase 1 — MVP)
> **Date:** 3 June 2026
> **Author:** Justinian
> **Status:** Draft — pending review
> **Authoritative spec:** [2026-05-26-zoo-tycoon-launcher-design.md](./2026-05-26-zoo-tycoon-launcher-design.md) §7.10, §9.2

---

## 1. Goal

Wire the headline use case: the user clicks **Launch Game** on the General tab and `zoo.exe` starts. The slice covers just-in-time re-verification, process start with shell-execute semantics matching the Ref launcher (SDD §7.10), `LastPlayedUtc` stamping, the modeless error dialogue for process-start failures, and `LauncherSettings.CloseAfterGameLaunch` honouring the user's "close the launcher when the game starts" preference.

This slice does **not** introduce screen-mode enumeration, the INI Config editor, the Settings dialogue, or any new menu items. The Launch button currently rendered disabled in [GeneralTabView.axaml](../../../Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/GeneralTabView.axaml) becomes the click target.

---

## 2. Scope

### In scope

- `LaunchGameCommand` + `LaunchGameResult` + `LaunchGameOutcome` + `LaunchGameHandler` — full §7.10 behaviour with just-in-time verification, drift persistence, `LastPlayedUtc` stamping.
- `IProcessLauncher` abstraction + `ProcessLaunchResult` result record.
- `WindowsProcessLauncher` infrastructure implementation — `Process.Start` with `UseShellExecute = true`, working directory set to the exe's folder, no arguments.
- `IApplicationLifecycle` Desktop abstraction + `AvaloniaApplicationLifecycle` implementation — thin wrapper over `IClassicDesktopStyleApplicationLifetime.Shutdown()`.
- `IDialogService` Desktop abstraction + `AvaloniaDialogService` implementation — one method, `ShowLaunchError(string message)`, opens a modeless `LaunchErrorView` owned by `MainWindow`.
- `LaunchErrorView` + `LaunchErrorViewModel` — small Win95-style modal-looking but modeless window.
- `GeneralTabViewModel` extension: `[ RelayCommand ]` for launch, `IsBusy` gate, `LaunchOutcomeRaised` CLR event.
- `ReadyToPlayViewModel` extension: subscribes to `LaunchOutcomeRaised` and routes each outcome to the appropriate Desktop capability.
- `MainWindowViewModel.RouteResult` update: passes the new orchestration delegates and services to `ReadyToPlayViewModel`.
- `LaunchGameHandlerTests.cs` — exhaustive branch coverage with fakes.
- `WindowsProcessLauncherTests.cs` — two integration smoke tests against `cmd.exe`.
- Architecture tests for namespace folders and `IProcessLauncher` dependency direction.

### Out of scope

| Item | Why deferred |
|---|---|
| Screen-mode dropdown on the General tab | Separate "Screen modes on the General tab" slice (SDD §13.2). The Launch button does not depend on screen-mode selection. |
| Tracking the spawned process to detect ZT1 exit | Fire-and-forget per design decision; the button re-enables immediately. |
| Multi-instance protection (preventing two ZT1 launches) | ZT1 itself handles same-process re-entry. Worth revisiting only if telemetry shows real user pain. |
| "Launching…" transient status / toast | No status bar component exists yet. The instantaneous re-enable is sufficient feedback when `CloseAfterGameLaunch = false`. |
| Settings UI for `CloseAfterGameLaunch` | Separate "Settings + theming" slice (SDD §13.2). The flag is read from `LauncherSettings` via the existing repository; its default is whatever the schema defines today. |

---

## 3. Application layer

### 3.1 LaunchGameCommand + LaunchGameResult + LaunchGameOutcome

```text
Application/Game/Launch/LaunchGameCommand.cs
Application/Game/Launch/LaunchGameResult.cs
Application/Game/Launch/LaunchGameHandler.cs
```

`LaunchGameCommand` carries only the installation id:

```csharp
public sealed record LaunchGameCommand(Guid InstallationId) : ICommand<ErrorOr<LaunchGameResult>>;
```

`LaunchGameResult` and `LaunchGameOutcome` live in the same file — they are a tightly-coupled pair and are never used separately, matching the precedent set by `BootResult` / `BootOutcome`.

```csharp
public sealed record LaunchGameResult(
    LaunchGameOutcome Outcome,
    bool CloseAfterGameLaunch,
    string? FailureMessage);

public enum LaunchGameOutcome
{
    Started,
    Drifted,
    StartFailed,
}
```

`CloseAfterGameLaunch` is meaningful only when `Outcome == Started`. On `Drifted` and `StartFailed` it is `false`.

`FailureMessage` is non-null only when `Outcome == StartFailed`. It is the message the modeless dialogue displays verbatim — typically the `Win32Exception.Message` from `Process.Start`, but possibly our own pre-check string ("Zoo Tycoon executable not found at …").

### 3.2 LaunchGameHandler algorithm

The handler depends directly on six services — no nested `IMediator.Send`, matching the existing handler style (`BootHandler` is the precedent):

```csharp
public sealed class LaunchGameHandler : ICommandHandler<LaunchGameCommand, ErrorOr<LaunchGameResult>>
{
    private readonly IInstallationRepository _installations;
    private readonly IInstallationVerifier _verifier;
    private readonly IProcessLauncher _processLauncher;
    private readonly ILauncherSettingsRepository _settings;
    private readonly TimeProvider _clock;
    private readonly ILogger<LaunchGameHandler> _logger;
    // ... ctor ...
}
```

Algorithm (`Handle`):

1. **Resolve the row.** `GameInstallation? row = await _installations.GetByIdAsync(command.InstallationId, ct)`. If null → return `Error.NotFound`.
2. **Re-verify.** `VerificationResult result = await _verifier.VerifyAsync(row.Path, ct)`.
3. **Persist drift before deciding.** If `row.HasExe != result.HasExe` or `row.HasIni != result.HasIni`, update the row's `HasExe`, `HasIni`, and `ModifiedUtc`, then `await _installations.UpdateAsync(row, ct)`. This mirrors `BootHandler.VerifyAsync` and ensures the next boot does not lie about state.
4. **Drift check.** If `!result.HasExe` → return `LaunchGameResult(Drifted, false, null)`. No launch attempt, no `LastPlayedUtc` bump.
5. **Launch.** `ProcessLaunchResult launch = await _processLauncher.LaunchAsync(exePath: Path.Combine(row.Path, "zoo.exe"), workingDirectory: row.Path, ct)`.
6. **Start-failure check.** If `!launch.Started` → return `LaunchGameResult(StartFailed, false, launch.ErrorMessage)`. No `LastPlayedUtc` bump.
7. **Read settings.** `LauncherSettings settings = await _settings.GetAsync(ct)`. Read **after** the launch so the result reflects the current `CloseAfterGameLaunch` value, not a stale read from boot.
8. **Stamp LastPlayedUtc.** `row.LastPlayedUtc = _clock.GetUtcNow().UtcDateTime; await _installations.UpdateAsync(row, ct);`. Wrap in `try` / `catch`; on failure, log `Warning` and continue — ZT1 is already running; refusing to admit it would lie to the user.
9. **Return.** `LaunchGameResult(Started, settings.CloseAfterGameLaunch, null)`.

### 3.3 IProcessLauncher

```text
Application/Common/Abstractions/IProcessLauncher.cs
Application/Common/Models/ProcessLaunchResult.cs
```

```csharp
public interface IProcessLauncher
{
    Task<ProcessLaunchResult> LaunchAsync(string exePath, string workingDirectory, CancellationToken cancellationToken);
}

public sealed record ProcessLaunchResult(bool Started, string? ErrorMessage);
```

The interface stays narrow: it spawns and forgets. Tracking child process lifetime is intentionally out of scope.

### 3.4 Service registration

`AddApplication` registers the source-generated `LaunchGameHandler` automatically via the Mediator generator. No manual line additions are needed in `ApplicationServiceCollectionExtensions`.

---

## 4. Infrastructure layer

### 4.1 WindowsProcessLauncher

```text
Infrastructure/Game/WindowsProcessLauncher.cs
```

```csharp
public sealed class WindowsProcessLauncher : IProcessLauncher
{
    public Task<ProcessLaunchResult> LaunchAsync(string exePath, string workingDirectory, CancellationToken cancellationToken)
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName         = exePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute  = true,
            };

            Process? process = Process.Start(startInfo);

            return Task.FromResult(process is null
                ? new ProcessLaunchResult(Started: false, ErrorMessage: "The system did not start a process for the game executable.")
                : new ProcessLaunchResult(Started: true, ErrorMessage: null));
        }
        catch (Win32Exception ex)        { return Task.FromResult(new ProcessLaunchResult(false, ex.Message)); }
        catch (InvalidOperationException ex) { return Task.FromResult(new ProcessLaunchResult(false, ex.Message)); }
        catch (FileNotFoundException ex) { return Task.FromResult(new ProcessLaunchResult(false, ex.Message)); }
    }
}
```

`UseShellExecute = true` lets Windows resolve manifests, side-by-side assemblies, and the same shell semantics a double-click would. Setting `WorkingDirectory` to the exe's folder is critical: ZT1 resolves `zoo.ini` and asset folders by relative path.

The handler computes both arguments; this class is a pure adapter. The `CancellationToken` is observed only insofar as the synchronous `Process.Start` returns quickly — there is no in-flight async operation to cancel after the OS has accepted the request.

### 4.2 Service registration

`InfrastructureServiceCollectionExtensions.AddInfrastructure` adds:

```csharp
services.AddSingleton<IProcessLauncher, WindowsProcessLauncher>();
```

Singleton is appropriate because the implementation is stateless.

---

## 5. Desktop layer

### 5.1 Topology

```text
MainWindowViewModel
└── ReadyToPlayViewModel
    ├── GeneralTabViewModel        ← owns LaunchCommand, raises LaunchOutcomeRaised
    ├── IniConfigTabViewModel
    └── ScenariosTabViewModel
```

`GeneralTabViewModel` dispatches the command and exposes the outcome via a CLR event. `ReadyToPlayViewModel` subscribes to that event and reaches into Desktop capabilities to enact each outcome. `MainWindowViewModel` provides the reboot delegate and the chrome services (`IApplicationLifecycle`, `IDialogService`).

### 5.2 IApplicationLifecycle

```text
Desktop/Composition/IApplicationLifecycle.cs
Desktop/Composition/AvaloniaApplicationLifecycle.cs
```

```csharp
public interface IApplicationLifecycle
{
    void RequestShutdown();
}

internal sealed class AvaloniaApplicationLifecycle : IApplicationLifecycle
{
    public void RequestShutdown()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
```

Registered singleton in `DesktopServiceCollectionExtensions`.

### 5.3 IDialogService

```text
Desktop/Composition/IDialogService.cs
Desktop/Composition/AvaloniaDialogService.cs
```

```csharp
public interface IDialogService
{
    void ShowLaunchError(string message);
}
```

The implementation finds the current `MainWindow` (via `IClassicDesktopStyleApplicationLifetime.MainWindow`), constructs `LaunchErrorView` with a `LaunchErrorViewModel(message)`, and calls `Show(ownerWindow)`. If `MainWindow` is somehow null at the moment of the call (impossible in normal flow — Launch can only be clicked after MainWindow is shown), the implementation falls back to `Show()` with no owner so the error still surfaces.

The interface is intentionally minimal — one method — but is structured to grow as later slices add Fix / Picker / About / Restore dialogues.

Registered singleton.

### 5.4 LaunchErrorView + LaunchErrorViewModel

```text
Desktop/Views/Dialogs/LaunchErrorView.axaml(.cs)
Desktop/ViewModels/Dialogs/LaunchErrorViewModel.cs
```

A small Win95-style `Window`:

- Title: "Cannot Launch Zoo Tycoon"
- Icon: standard error glyph
- Body: a `TextBlock` bound to `Message`, plus a single `OK` button
- `Sizing`: `SizeToContent="WidthAndHeight"`, `CanResize="False"`, `WindowStartupLocation="CenterOwner"`

`LaunchErrorViewModel`:

```csharp
public sealed partial class LaunchErrorViewModel : ViewModelBase
{
    public LaunchErrorViewModel(string message) => Message = message;

    public string Message { get; }
}
```

The OK button closes the window via XAML wiring (`Click="OnOkClick"` in code-behind, calling `Close()`) — there is no command needed because the only behaviour is "close this window."

A parameterless designer constructor provides a placeholder message string. The XAML file declares `x:DataType`.

### 5.5 GeneralTabViewModel changes

The view model gains a real launch command, an `IsBusy` gate, and a CLR event:

```csharp
public sealed partial class GeneralTabViewModel : ViewModelBase
{
    private readonly Guid _installationId;
    private readonly IMediator _mediator;

    public GeneralTabViewModel(InstallationSummary installation, IMediator mediator)
    {
        _installationId  = installation.Id;
        _mediator        = mediator;
        InstallationName = installation.Name;
        InstallationPath = installation.Path;
        CanLaunch        = installation.Validity == InstallationValidity.Valid;
    }

    public event EventHandler<LaunchGameResult>? LaunchOutcomeRaised;

    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(LaunchCommand)) ]
    public partial bool IsBusy { get; set; }

    public bool CanLaunch { get; }

    public string InstallationName { get; }

    public string InstallationPath { get; }

    [ RelayCommand(CanExecute = nameof(CanExecuteLaunch)) ]
    private async Task LaunchAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;

        try
        {
            ErrorOr<LaunchGameResult> result =
                await _mediator.Send(new LaunchGameCommand(_installationId), cancellationToken);

            LaunchGameResult outcome = result.IsError
                ? new LaunchGameResult(LaunchGameOutcome.StartFailed, false, result.FirstError.Description)
                : result.Value;

            LaunchOutcomeRaised?.Invoke(this, outcome);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteLaunch() => CanLaunch && !IsBusy;
}
```

`CanLaunch` is set once at construction from the boot-supplied `InstallationSummary.Validity`. The just-in-time verification inside the handler is what catches drift that happens *after* boot.

A parameterless designer constructor stays in place (and is updated to pass a null-object `IMediator`, matching the existing convention).

### 5.6 GeneralTabView changes

```xml
<Button Content="Launch Game"
        Command="{ Binding LaunchCommand }"
        Margin="0,8,0,0" />
```

The disabled placeholder text "Screen modes — coming soon" and "Last played — coming soon" stay in place; they will be replaced by the screen-modes slice.

### 5.7 ReadyToPlayViewModel changes

The view model accepts the orchestration plumbing and subscribes to the General tab's event:

```csharp
public sealed class ReadyToPlayViewModel : ViewModelBase
{
    private readonly Func<CancellationToken, Task> _rebootAsync;
    private readonly IApplicationLifecycle _lifecycle;
    private readonly IDialogService _dialogs;

    public ReadyToPlayViewModel(
        InstallationSummary installation,
        Func<CancellationToken, Task> rebootAsync,
        IApplicationLifecycle lifecycle,
        IDialogService dialogs,
        IMediator mediator)
    {
        _rebootAsync = rebootAsync;
        _lifecycle   = lifecycle;
        _dialogs     = dialogs;

        InstallationName = installation.Name;
        InstallationPath = installation.Path;
        IsDefault        = installation.IsDefault;

        GeneralTab   = new GeneralTabViewModel(installation, mediator);
        IniConfigTab = new IniConfigTabViewModel();
        ScenariosTab = new ScenariosTabViewModel();

        GeneralTab.LaunchOutcomeRaised += OnLaunchOutcomeRaised;
    }

    private async void OnLaunchOutcomeRaised(object? sender, LaunchGameResult result)
    {
        switch (result.Outcome)
        {
            case LaunchGameOutcome.Started when result.CloseAfterGameLaunch:
                _lifecycle.RequestShutdown();
                break;

            case LaunchGameOutcome.Started:
                break;

            case LaunchGameOutcome.Drifted:
                await _rebootAsync(CancellationToken.None);
                break;

            case LaunchGameOutcome.StartFailed:
                _dialogs.ShowLaunchError(result.FailureMessage ?? "Zoo Tycoon could not be launched.");
                break;
        }
    }

    // existing public properties...
}
```

`async void` is acceptable for an event handler; exceptions from `_rebootAsync` would otherwise be lost, so the handler wraps the call in `try` / `catch` and surfaces failures via `_dialogs.ShowLaunchError` — see §6 for the matrix.

### 5.8 MainWindowViewModel changes

`RouteResult` updates to inject the new plumbing:

```csharp
AppBoot.BootOutcome.ReadyToPlay =>
    new ReadyToPlayViewModel(
        result.ActiveInstallation!,
        rebootAsync: BootAsync,
        lifecycle:   _lifecycle,
        dialogs:     _dialogs,
        mediator:    _mediator),
```

`BootAsync` is the existing `[ RelayCommand ] BootAsync(CancellationToken)`. Passing it as a delegate avoids promoting `MainWindowViewModel` to an interface for one method.

The constructor gains the two new chrome services:

```csharp
public MainWindowViewModel(IMediator mediator, IApplicationLifecycle lifecycle, IDialogService dialogs);
```

---

## 6. Failure model

Every operation in the slice maps to exactly one user-visible outcome:

| Operation | Failure mode | User experience |
|---|---|---|
| `GetByIdAsync` returns null | Installation row deleted between boot and click | Launch button click yields `ErrorOr.Error`; `GeneralTabViewModel` converts to `StartFailed` with the error description; modeless dialogue appears |
| `IInstallationRepository` throws | DB locked or schema mismatch | `ErrorOr.Error`; modeless dialogue with the exception message |
| `IInstallationVerifier` throws | Path unreachable (network drive timeout, permission denied on parent) | `ErrorOr.Error`; modeless dialogue |
| Drift detected (`HasExe = false`) | User deleted / moved `zoo.exe` after boot | Drift persisted to row; return `Drifted`; UI re-issues `BootCommand`; state machine routes to `CannotPlay` |
| `Process.Start` throws `Win32Exception` | Antivirus block, ACL deny, file in use | `WindowsProcessLauncher` catches and returns `(false, ex.Message)`; handler returns `StartFailed`; modeless dialogue |
| `Process.Start` returns null | Documented impossible for `.exe` with `UseShellExecute`; defended | Same path as Win32Exception with a fixed message |
| `LastPlayedUtc` `UpdateAsync` throws | DB locked at exactly the wrong moment | Warning logged; still return `Started` (ZT1 is running, refusing to admit it would lie) |
| `_rebootAsync` throws inside `OnLaunchOutcomeRaised` | Boot pipeline misconfigured | Caught; surfaced via `_dialogs.ShowLaunchError($"The launcher could not refresh installation state: {ex.Message}")` |

The modeless `LaunchErrorView` is the only error-surfacing mechanism in this slice. There is no inline banner, status bar, or toast.

---

## 7. Cancellation

The slice respects `CancellationToken` on every async call. Cancellation between the process start (step 5 in §3.2) and the `LastPlayedUtc` stamp (step 8) leaves ZT1 running with no recorded launch — the same trade-off as the stamp-write-fails case. The slice does not attempt to terminate the spawned process on cancellation; that would countermand the user's explicit intent ("launch the game") for an internal bookkeeping concern.

---

## 8. Persistence

No new tables or columns. Two columns on the existing `GameInstallations` table are written:

- `HasExe`, `HasIni`, `ModifiedUtc` — only when drift is detected (step 3 in §3.2).
- `LastPlayedUtc` — only on `Started` (step 8 in §3.2).

No EF Core migration is required.

---

## 9. Testing strategy

### 9.1 Application.Tests.Unit — `LaunchGameHandlerTests`

Fakes for `IInstallationRepository`, `IInstallationVerifier`, `IProcessLauncher`, `ILauncherSettingsRepository`; `FakeTimeProvider` for the clock.

| Test | Behaviour exercised |
|---|---|
| `Handle_InstallationNotFound_ReturnsError` | Repo returns null → `result.IsError` |
| `Handle_DriftDetected_PersistsAndReturnsDrifted` | Verifier reports `HasExe=false`, row had `HasExe=true` → `UpdateAsync` called with drift; outcome `Drifted`; `IProcessLauncher` never called |
| `Handle_NoDrift_InvokesProcessLauncher` | Verifier matches row → launcher called once with correct `exePath` and `workingDirectory` |
| `Handle_ProcessStartFails_ReturnsStartFailedWithMessage` | Launcher returns `(false, "AV blocked")` → outcome `StartFailed`; `FailureMessage = "AV blocked"`; no `LastPlayedUtc` write |
| `Handle_LaunchSucceeds_StampsLastPlayedUtc` | Happy path → row's `LastPlayedUtc` equals fake clock's value |
| `Handle_LaunchSucceeds_ReturnsCloseAfterGameLaunchFromSettings` | Settings repo returns `CloseAfterGameLaunch=true` → result reflects it |
| `Handle_LastPlayedUpdateThrows_StillReturnsStarted` | Stamp write throws → outcome `Started`; warning logged |
| `Handle_VerifierThrows_ReturnsError` | Verifier throws → `result.IsError` |
| `Handle_NoDrift_SettingsReadAfterLaunch` | Order assertion: launcher invoked before settings read |

### 9.2 Infrastructure.Tests.Integration — `WindowsProcessLauncherTests`

Two smoke tests:

| Test | Setup | Assert |
|---|---|---|
| `LaunchAsync_KnownGoodExe_ReturnsStarted` | `cmd.exe /c exit` | `Started == true`; `ErrorMessage == null` |
| `LaunchAsync_NonExistentPath_ReturnsStartFailedWithMessage` | A guaranteed-missing path | `Started == false`; `ErrorMessage` non-null and non-empty |

These run only on Windows — guarded by the same OS check the rest of `Infrastructure.Tests.Integration` uses.

### 9.3 Tests.Architecture

Augment the existing rule set:

- `LaunchGameCommand`, `LaunchGameResult`, `LaunchGameHandler` live under `Application/Game/Launch/`.
- `WindowsProcessLauncher` lives under `Infrastructure/Game/`.
- `IProcessLauncher` is referenced by `Application` and `Infrastructure` only; never by `Domain`.
- `IApplicationLifecycle`, `IDialogService`, and their implementations live in `Desktop/Composition/`.
- `LaunchErrorView` + `LaunchErrorViewModel` follow the view-pair rule (each has a sibling).
- `MainWindow.axaml` line count remains ≤ 100.

### 9.4 Manual smoke

Per CLAUDE.md "for UI changes, start the dev server and use the feature in a browser before reporting the task as complete":

1. **Happy path.** Launch with a valid installation → ZT1 starts.
2. **CloseAfterGameLaunch.** Toggle the flag in the DB to `true`, launch → ZT1 starts, launcher window closes.
3. **Drift simulation.** Rename `zoo.exe` → `zoo.bak` between boot and click → click Launch → UI transitions to `CannotPlay`.
4. **Start-failure simulation.** Apply deny-execute ACLs to `zoo.exe` (or substitute a corrupt stub) → click Launch → modeless `LaunchErrorView` appears with the Win32 error message.

There is no `Desktop.Tests.Unit` project today; introducing one for this slice is overkill. The manual smoke checklist is the verification gate.

---

## 10. Conventions checklist

- [x] One type per file (`LaunchGameOutcome` co-located with `LaunchGameResult` per the documented `BootOutcome` / `BootResult` precedent).
- [x] No files at any project root.
- [x] File-scoped namespaces in all new files.
- [x] British English in prose, identifier wording, and XML doc text.
- [x] XML doc comments on every public type and member.
- [x] `<c>…</c>` carries no inside whitespace.
- [x] Spaced bracket attribute style.
- [x] UTC timestamps (`LastPlayedUtc`, `ModifiedUtc`).
- [x] Source-generated MVVM (`[ ObservableProperty ]` on `partial` properties; `[ RelayCommand ]` on private async methods).
- [x] Compiled bindings (`x:DataType` declared in every new XAML file).
- [x] Designer constructors marked `[ UsedImplicitly ]` are **not** added proactively; ReSharper warnings are addressed by the author after-the-fact.
- [x] View-pair rule: `LaunchErrorView` ↔ `LaunchErrorViewModel`.
- [x] Architecture-test coverage for namespace folders and dependency direction.

---

## 11. Risks and open questions

### 11.1 ZT1 multi-instance behaviour is unverified

ZT1 may or may not allow a second process instance. Fire-and-forget means the launcher does not prevent the user from clicking Launch twice. If ZT1 refuses the second start, the user sees no feedback — the click "did nothing." Acceptable for MVP; revisit if reported.

### 11.2 IDialogService surface will widen

The interface ships with one method. Fix / Picker / Restore dialogues will each add a method. Worth re-evaluating whether a `Window`-returning factory pattern (`IWindow ShowDialog<TViewModel>(TViewModel vm)`) would scale better once we have three or four dialogue types. Not blocking for this slice.

### 11.3 No tests for the `async void` event handler

`OnLaunchOutcomeRaised` is `async void` per the .NET event pattern. Its branches are exercised indirectly through the handler tests, but there is no Desktop-layer test asserting that a given `LaunchGameOutcome` produces the correct chrome call. Adding `Desktop.Tests.Unit` for this is intentionally deferred (see §9.4).
