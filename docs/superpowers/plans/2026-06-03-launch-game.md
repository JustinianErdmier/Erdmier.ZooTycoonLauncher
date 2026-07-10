# Launch Game Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire the headline use case — clicking **Launch Game** on the General tab starts `zoo.exe` with the correct working directory, persists drift detected at click-time, stamps `LastPlayedUtc`, honours `LauncherSettings.CloseAfterGameLaunch`, and shows a modeless error window when `Process.Start` fails.

**Architecture:** A new CQRS slice `LaunchGameCommand` / `LaunchGameHandler` runs the verify → launch → stamp pipeline against fresh dependencies (`IInstallationRepository`, `IInstallationVerifier`, `IProcessLauncher`, `ILauncherSettingsRepository`, `TimeProvider`). The handler returns a discriminated outcome (`Started` | `Drifted` | `StartFailed`). Desktop wires `GeneralTabViewModel` to dispatch the command and raise a CLR event that `ReadyToPlayViewModel` consumes, calling one of three chrome capabilities — re-issue `BootCommand` (delegate), shutdown (`IApplicationLifecycle`), or show error window (`IDialogService`) — based on the outcome.

**Tech Stack:** .NET 10, C# 13, Avalonia 11, CommunityToolkit.Mvvm, Mediator 2.1.7 (martinothamar), ErrorOr 2.0.1, EF Core + SQLite, xUnit, Shouldly, NSubstitute, `Microsoft.Extensions.Time.Testing.FakeTimeProvider`, NetArchTest.

**Authoritative spec:** [`docs/superpowers/specs/2026-06-03-launch-game-design.md`](../specs/2026-06-03-launch-game-design.md)

---

## File map

### New files

| Path | Purpose |
|---|---|
| `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IProcessLauncher.cs` | Interface for OS process start, swappable in tests. |
| `Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/ProcessLaunchResult.cs` | `(Started, ErrorMessage)` record returned by `IProcessLauncher`. |
| `Source/Erdmier.ZooTycoonLauncher.Application/Game/Launch/LaunchGameCommand.cs` | The `ICommand<ErrorOr<LaunchGameResult>>` request. |
| `Source/Erdmier.ZooTycoonLauncher.Application/Game/Launch/LaunchGameResult.cs` | Result record. |
| `Source/Erdmier.ZooTycoonLauncher.Application/Game/Launch/LaunchGameOutcome.cs` | Discriminated outcome enum for `LaunchGameResult.Outcome`. |
| `Source/Erdmier.ZooTycoonLauncher.Application/Game/Launch/LaunchGameHandler.cs` | The handler — verify, persist drift, launch, stamp, return. |
| `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Game/WindowsProcessLauncher.cs` | `Process.Start` with `UseShellExecute=true`, exception mapping. |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IApplicationLifecycle.cs` | One-method abstraction over `IClassicDesktopStyleApplicationLifetime.Shutdown()`. |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaApplicationLifecycle.cs` | The Avalonia-bound implementation. |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs` | One-method abstraction for opening the modeless error window. |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaDialogService.cs` | Implementation: constructs view, sets owner, calls `Show(owner)`. |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/LaunchErrorViewModel.cs` | Holds the message string. |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/LaunchErrorView.axaml` | Modeless Win95-style error window. |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/LaunchErrorView.axaml.cs` | Code-behind with `OnOkClick` handler. |
| `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Game/Launch/LaunchGameHandlerTests.cs` | Full branch coverage with fakes. |
| `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Game/WindowsProcessLauncherTests.cs` | Two smoke tests against `cmd.exe`. |

### Modified files

| Path | Change |
|---|---|
| `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Extensions/InfrastructureServiceCollectionExtensions.cs` | Register `IProcessLauncher → WindowsProcessLauncher` (singleton). |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs` | Register `IApplicationLifecycle` and `IDialogService` (singletons). |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/GeneralTabViewModel.cs` | Add `IMediator` ctor parameter, `IsBusy`, `[ RelayCommand ] LaunchAsync`, `LaunchOutcomeRaised` event. |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/GeneralTabView.axaml` | Bind `Launch Game` button to `LaunchCommand`. |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/ReadyToPlayViewModel.cs` | Accept reboot delegate + chrome services, subscribe to `LaunchOutcomeRaised`. |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs` | Add `IApplicationLifecycle` + `IDialogService` ctor params; pass them and `BootAsync` to `ReadyToPlayViewModel`. |
| `Source/Erdmier.ZooTycoonLauncher.Application/GlobalUsings.cs` | Add `Erdmier.ZooTycoonLauncher.Application.Game.Launch` so handler tests resolve types cleanly. |
| `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs` | Add `Erdmier.ZooTycoonLauncher.Application.Game.Launch`. |
| `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/GlobalUsings.cs` | Add `Erdmier.ZooTycoonLauncher.Infrastructure.Game`. |

No EF migrations, no Domain layer changes, no architecture-test additions (existing `OneTypePerFileTests`, `NoFilesAtAssemblyRootTests`, `DependencyDirectionTests` cover the new files transitively).

---

## Task 1: Application contracts — IProcessLauncher and ProcessLaunchResult

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IProcessLauncher.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/ProcessLaunchResult.cs`

- [ ] **Step 1: Create `ProcessLaunchResult`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>The outcome of a single <see cref="IProcessLauncher.LaunchAsync(string, string, System.Threading.CancellationToken)" /> call.</summary>
/// <param name="Started"><see langword="true" /> when the OS accepted the start request and produced a process handle.</param>
/// <param name="ErrorMessage">Non-<see langword="null" /> when <paramref name="Started" /> is <see langword="false" />; the message displayed to the user verbatim.</param>
public sealed record ProcessLaunchResult(bool Started, string? ErrorMessage);
```

- [ ] **Step 2: Create `IProcessLauncher`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Starts external processes (the game executable) so the handler stays free of <see cref="System.Diagnostics.Process" /> calls.</summary>
public interface IProcessLauncher
{
    /// <summary>Starts the executable at <paramref name="exePath" /> with the supplied working directory.</summary>
    /// <param name="exePath">Fully-qualified path to the executable.</param>
    /// <param name="workingDirectory">Directory the process treats as its working directory; ZT1 resolves <c>zoo.ini</c> and asset folders relative to this.</param>
    /// <param name="cancellationToken">Cancellation token; observed only until the OS accepts the start request.</param>
    /// <returns>A <see cref="ProcessLaunchResult" /> describing whether the start succeeded.</returns>
    Task<ProcessLaunchResult> LaunchAsync(string exePath, string workingDirectory, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Build the solution**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: build succeeds. The two new files compile but nothing references them yet.

- [ ] **Step 4: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IProcessLauncher.cs Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/ProcessLaunchResult.cs
git commit -m "feat(✨): add IProcessLauncher abstraction and ProcessLaunchResult record"
```

---

## Task 2: Application contracts — LaunchGameCommand, LaunchGameResult, LaunchGameOutcome

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Game/Launch/LaunchGameCommand.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Game/Launch/LaunchGameResult.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/GlobalUsings.cs`

- [ ] **Step 1: Create `LaunchGameCommand`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Game.Launch;

/// <summary>Request to launch <c>zoo.exe</c> for a single installation. SDD §7.10.</summary>
/// <param name="InstallationId">The identifier of the installation to launch.</param>
public sealed record LaunchGameCommand(Guid InstallationId) : ICommand<ErrorOr<LaunchGameResult>>;
```

- [ ] **Step 2: Create `LaunchGameResult` (with `LaunchGameOutcome` co-located)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Game.Launch;

/// <summary>The outcome of dispatching <see cref="LaunchGameCommand" />.</summary>
/// <param name="Outcome">Which terminal branch the handler took.</param>
/// <param name="CloseAfterGameLaunch">Snapshot of <c>LauncherSettings.CloseAfterGameLaunch</c> at the moment of launch; meaningful only when <paramref name="Outcome" /> is <see cref="LaunchGameOutcome.Started" />.</param>
/// <param name="FailureMessage">Non-<see langword="null" /> only when <paramref name="Outcome" /> is <see cref="LaunchGameOutcome.StartFailed" />; the message displayed verbatim to the user.</param>
public sealed record LaunchGameResult(LaunchGameOutcome Outcome, bool CloseAfterGameLaunch, string? FailureMessage);

/// <summary>The three terminal branches of <see cref="LaunchGameCommand" />.</summary>
/// <remarks>Co-located with <see cref="LaunchGameResult" /> because the two types are a tightly-coupled pair, matching the precedent set by <c>BootResult</c> / <c>BootOutcome</c>.</remarks>
public enum LaunchGameOutcome
{
    /// <summary><c>zoo.exe</c> started successfully.</summary>
    Started,

    /// <summary>Just-in-time verification detected drift (e.g. <c>zoo.exe</c> missing). No launch attempted.</summary>
    Drifted,

    /// <summary>The OS rejected the start request (AV block, ACL deny, file in use). See <see cref="LaunchGameResult.FailureMessage" />.</summary>
    StartFailed,
}
```

- [ ] **Step 3: Add namespace to Application GlobalUsings**

Open `Source/Erdmier.ZooTycoonLauncher.Application/GlobalUsings.cs` and add the new namespace alphabetically. The existing file should now look like:

```csharp
// existing usings unchanged ...
global using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
global using Erdmier.ZooTycoonLauncher.Application.Common.Models;
global using Erdmier.ZooTycoonLauncher.Application.Game.Launch;
// ... rest unchanged
```

(Read the file first; insert the line in the right position.)

- [ ] **Step 4: Build the solution**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Application/Game/Launch/LaunchGameCommand.cs Source/Erdmier.ZooTycoonLauncher.Application/Game/Launch/LaunchGameResult.cs Source/Erdmier.ZooTycoonLauncher.Application/GlobalUsings.cs
git commit -m "feat(✨): add LaunchGameCommand and LaunchGameResult discriminated outcome"
```

---

## Task 3: LaunchGameHandler — happy path (TDD)

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Game/Launch/LaunchGameHandler.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Game/Launch/LaunchGameHandlerTests.cs`
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs`

- [ ] **Step 1: Extend the test project's GlobalUsings**

Open `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs` and add:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Game.Launch;
```

Place it after the other `Application.*` entries (alphabetical within the group is fine).

- [ ] **Step 2: Write the failing happy-path test**

Create `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Game/Launch/LaunchGameHandlerTests.cs`:

```csharp
using Microsoft.Extensions.Logging.Abstractions;

namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Game.Launch;

public sealed class LaunchGameHandlerTests
{
    [ Fact ]
    public async Task Handle_NoDrift_LaunchesProcessAndStampsLastPlayedUtc()
    {
        Guid id = Guid.CreateVersion7();
        DateTimeOffset now = new(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero);
        FakeTimeProvider clock = new(now);

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Main",
            Path     = @"C:\Games\Zoo",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow,
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IProcessLauncher launcher = Substitute.For<IProcessLauncher>();
        launcher.LaunchAsync(@"C:\Games\Zoo\zoo.exe", @"C:\Games\Zoo", Arg.Any<CancellationToken>())
                .Returns(new ProcessLaunchResult(Started: true, ErrorMessage: null));

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { CloseAfterGameLaunch = true });

        LaunchGameHandler handler = new(installations, verifier, launcher, settings, clock, NullLogger<LaunchGameHandler>.Instance);

        ErrorOr<LaunchGameResult> result = await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(LaunchGameOutcome.Started);
        result.Value.CloseAfterGameLaunch.ShouldBeTrue();
        result.Value.FailureMessage.ShouldBeNull();
        row.LastPlayedUtc.ShouldBe(now.UtcDateTime);
        await installations.Received().UpdateAsync(row, Arg.Any<CancellationToken>());
    }
}
```

Note: `Microsoft.Extensions.Logging.Abstractions` is a transitive dependency through Mediator's source generator + DI; if the compiler can't find it, add `<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />` to the test project. Check first with the build in Step 3.

- [ ] **Step 3: Build to confirm the test fails to compile (no handler yet)**

```powershell
dotnet build Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
```

Expected: build fails with `CS0246: type 'LaunchGameHandler' not found`.

- [ ] **Step 4: Create `LaunchGameHandler` (minimal implementation that passes the happy path)**

Create `Source/Erdmier.ZooTycoonLauncher.Application/Game/Launch/LaunchGameHandler.cs`:

```csharp
using Microsoft.Extensions.Logging;

namespace Erdmier.ZooTycoonLauncher.Application.Game.Launch;

/// <summary>Handler for <see cref="LaunchGameCommand" />. SDD §7.10.</summary>
public sealed class LaunchGameHandler : ICommandHandler<LaunchGameCommand, ErrorOr<LaunchGameResult>>
{
    private readonly TimeProvider _clock;

    private readonly IInstallationRepository _installations;

    private readonly ILogger<LaunchGameHandler> _logger;

    private readonly IProcessLauncher _processLauncher;

    private readonly ILauncherSettingsRepository _settings;

    private readonly IInstallationVerifier _verifier;

    /// <summary>Initialises a new instance.</summary>
    public LaunchGameHandler(IInstallationRepository    installations,
                             IInstallationVerifier      verifier,
                             IProcessLauncher           processLauncher,
                             ILauncherSettingsRepository settings,
                             TimeProvider               clock,
                             ILogger<LaunchGameHandler> logger)
    {
        _installations   = installations;
        _verifier        = verifier;
        _processLauncher = processLauncher;
        _settings        = settings;
        _clock           = clock;
        _logger          = logger;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<LaunchGameResult>> Handle(LaunchGameCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", $"No installation with id {command.InstallationId}.");
        }

        VerificationResult result = await _verifier.VerifyAsync(row.Path, cancellationToken);

        if (row.HasExe    != result.HasExe
            || row.HasIni != result.HasIni)
        {
            row.HasExe = result.HasExe;
            row.HasIni = result.HasIni;

            row.ModifiedUtc = _clock.GetUtcNow()
                                    .UtcDateTime;

            await _installations.UpdateAsync(row, cancellationToken);
        }

        if (!result.HasExe)
        {
            return new LaunchGameResult(LaunchGameOutcome.Drifted, CloseAfterGameLaunch: false, FailureMessage: null);
        }

        string exePath = Path.Combine(row.Path, path2: "zoo.exe");

        ProcessLaunchResult launch = await _processLauncher.LaunchAsync(exePath, row.Path, cancellationToken);

        if (!launch.Started)
        {
            return new LaunchGameResult(LaunchGameOutcome.StartFailed, CloseAfterGameLaunch: false, launch.ErrorMessage);
        }

        LauncherSettings launcherSettings = await _settings.GetAsync(cancellationToken);

        try
        {
            row.LastPlayedUtc = _clock.GetUtcNow()
                                      .UtcDateTime;

            await _installations.UpdateAsync(row, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, message: "Process started but LastPlayedUtc update failed for {InstallationId}", row.Id);
        }

        return new LaunchGameResult(LaunchGameOutcome.Started, launcherSettings.CloseAfterGameLaunch, FailureMessage: null);
    }
}
```

`Microsoft.Extensions.Logging` is already transitively referenced by the DI extensions; no PackageReference change needed.

- [ ] **Step 5: Run the test and confirm it passes**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj --filter FullyQualifiedName~LaunchGameHandlerTests
```

Expected: 1 passed.

- [ ] **Step 6: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Application/Game/Launch/LaunchGameHandler.cs Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Game/Launch/LaunchGameHandlerTests.cs Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs
git commit -m "feat(✨): add LaunchGameHandler with happy-path test"
```

---

## Task 4: LaunchGameHandler — drift detection branch (TDD)

**Files:**
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Game/Launch/LaunchGameHandlerTests.cs`

- [ ] **Step 1: Add the drift test**

Append this `[ Fact ]` inside `LaunchGameHandlerTests`:

```csharp
[ Fact ]
public async Task Handle_DriftDetected_PersistsAndReturnsDrifted()
{
    Guid id = Guid.CreateVersion7();
    FakeTimeProvider clock = new(new DateTimeOffset(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero));

    GameInstallation row = new()
    {
        Id       = id,
        Name     = "Main",
        Path     = @"C:\Games\Zoo",
        HasExe   = true,            // row claims valid
        HasIni   = true,
        AddedUtc = DateTime.UtcNow,
    };

    IInstallationRepository installations = Substitute.For<IInstallationRepository>();
    installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

    IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
    verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
            .Returns(new VerificationResult(DirectoryExists: true, HasExe: false, HasIni: true)); // drift

    IProcessLauncher launcher = Substitute.For<IProcessLauncher>();

    LaunchGameHandler handler = new(installations,
                                    verifier,
                                    launcher,
                                    Substitute.For<ILauncherSettingsRepository>(),
                                    clock,
                                    NullLogger<LaunchGameHandler>.Instance);

    ErrorOr<LaunchGameResult> result = await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

    result.IsError.ShouldBeFalse();
    result.Value.Outcome.ShouldBe(LaunchGameOutcome.Drifted);
    result.Value.CloseAfterGameLaunch.ShouldBeFalse();
    result.Value.FailureMessage.ShouldBeNull();

    row.HasExe.ShouldBeFalse();                                   // drift persisted on the row
    await installations.Received().UpdateAsync(row, Arg.Any<CancellationToken>());
    await launcher.DidNotReceive().LaunchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
```

- [ ] **Step 2: Run the new test**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj --filter FullyQualifiedName~Handle_DriftDetected
```

Expected: 1 passed. (Handler from Task 3 already implements drift; this confirms behaviour.)

- [ ] **Step 3: Commit**

```powershell
git add Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Game/Launch/LaunchGameHandlerTests.cs
git commit -m "test(✅): cover LaunchGameHandler drift-detection branch"
```

---

## Task 5: LaunchGameHandler — start-failure branch (TDD)

**Files:**
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Game/Launch/LaunchGameHandlerTests.cs`

- [ ] **Step 1: Add the start-failure test**

Append inside `LaunchGameHandlerTests`:

```csharp
[ Fact ]
public async Task Handle_ProcessStartFails_ReturnsStartFailedAndDoesNotStampLastPlayed()
{
    Guid id = Guid.CreateVersion7();
    DateTime before = new(year: 2026, month: 1, day: 1, hour: 0, minute: 0, second: 0, DateTimeKind.Utc);
    FakeTimeProvider clock = new(new DateTimeOffset(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero));

    GameInstallation row = new()
    {
        Id             = id,
        Name           = "Main",
        Path           = @"C:\Games\Zoo",
        HasExe         = true,
        HasIni         = true,
        AddedUtc       = DateTime.UtcNow,
        LastPlayedUtc  = before,
    };

    IInstallationRepository installations = Substitute.For<IInstallationRepository>();
    installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

    IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
    verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
            .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

    IProcessLauncher launcher = Substitute.For<IProcessLauncher>();
    launcher.LaunchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessLaunchResult(Started: false, ErrorMessage: "Antivirus blocked execution."));

    LaunchGameHandler handler = new(installations,
                                    verifier,
                                    launcher,
                                    Substitute.For<ILauncherSettingsRepository>(),
                                    clock,
                                    NullLogger<LaunchGameHandler>.Instance);

    ErrorOr<LaunchGameResult> result = await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

    result.IsError.ShouldBeFalse();
    result.Value.Outcome.ShouldBe(LaunchGameOutcome.StartFailed);
    result.Value.FailureMessage.ShouldBe(expected: "Antivirus blocked execution.");
    result.Value.CloseAfterGameLaunch.ShouldBeFalse();
    row.LastPlayedUtc.ShouldBe(before);                                   // unchanged
}
```

- [ ] **Step 2: Run the new test**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj --filter FullyQualifiedName~Handle_ProcessStartFails
```

Expected: 1 passed.

- [ ] **Step 3: Commit**

```powershell
git add Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Game/Launch/LaunchGameHandlerTests.cs
git commit -m "test(✅): cover LaunchGameHandler process-start failure branch"
```

---

## Task 6: LaunchGameHandler — edge cases (not-found, verifier-throws, stamp-throws, settings-read-after-launch)

**Files:**
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Game/Launch/LaunchGameHandlerTests.cs`

- [ ] **Step 1: Add the four remaining tests**

Append inside `LaunchGameHandlerTests`:

```csharp
[ Fact ]
public async Task Handle_InstallationNotFound_ReturnsError()
{
    Guid id = Guid.CreateVersion7();
    FakeTimeProvider clock = new(new DateTimeOffset(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero));

    IInstallationRepository installations = Substitute.For<IInstallationRepository>();
    installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                 .Returns((GameInstallation?) null);

    LaunchGameHandler handler = new(installations,
                                    Substitute.For<IInstallationVerifier>(),
                                    Substitute.For<IProcessLauncher>(),
                                    Substitute.For<ILauncherSettingsRepository>(),
                                    clock,
                                    NullLogger<LaunchGameHandler>.Instance);

    ErrorOr<LaunchGameResult> result = await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

    result.IsError.ShouldBeTrue();
    result.FirstError.Type.ShouldBe(ErrorType.NotFound);
}

[ Fact ]
public async Task Handle_VerifierThrows_PropagatesException()
{
    Guid id = Guid.CreateVersion7();
    FakeTimeProvider clock = new(new DateTimeOffset(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero));

    GameInstallation row = new() { Id = id, Name = "Main", Path = @"C:\Games\Zoo", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

    IInstallationRepository installations = Substitute.For<IInstallationRepository>();
    installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

    IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
    verifier.VerifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException(message: "Access denied"));

    LaunchGameHandler handler = new(installations,
                                    verifier,
                                    Substitute.For<IProcessLauncher>(),
                                    Substitute.For<ILauncherSettingsRepository>(),
                                    clock,
                                    NullLogger<LaunchGameHandler>.Instance);

    UnauthorizedAccessException ex = await Should.ThrowAsync<UnauthorizedAccessException>(
        async () => await handler.Handle(new LaunchGameCommand(id), CancellationToken.None));

    ex.Message.ShouldBe(expected: "Access denied");
}

[ Fact ]
public async Task Handle_LastPlayedUpdateThrows_StillReturnsStarted()
{
    Guid id = Guid.CreateVersion7();
    DateTimeOffset now = new(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero);
    FakeTimeProvider clock = new(now);

    GameInstallation row = new() { Id = id, Name = "Main", Path = @"C:\Games\Zoo", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

    IInstallationRepository installations = Substitute.For<IInstallationRepository>();
    installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);
    installations.When(r => r.UpdateAsync(row, Arg.Any<CancellationToken>()))
                 .Do(_ => throw new InvalidOperationException(message: "DB locked"));

    IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
    verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
            .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

    IProcessLauncher launcher = Substitute.For<IProcessLauncher>();
    launcher.LaunchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessLaunchResult(Started: true, ErrorMessage: null));

    ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
    settings.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new LauncherSettings { CloseAfterGameLaunch = false });

    LaunchGameHandler handler = new(installations, verifier, launcher, settings, clock, NullLogger<LaunchGameHandler>.Instance);

    ErrorOr<LaunchGameResult> result = await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

    result.IsError.ShouldBeFalse();
    result.Value.Outcome.ShouldBe(LaunchGameOutcome.Started);
}

[ Fact ]
public async Task Handle_SettingsReadAfterLaunch()
{
    Guid id = Guid.CreateVersion7();
    FakeTimeProvider clock = new(new DateTimeOffset(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero));

    GameInstallation row = new() { Id = id, Name = "Main", Path = @"C:\Games\Zoo", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

    IInstallationRepository installations = Substitute.For<IInstallationRepository>();
    installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

    IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
    verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
            .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

    IProcessLauncher launcher = Substitute.For<IProcessLauncher>();
    launcher.LaunchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessLaunchResult(Started: true, ErrorMessage: null));

    ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
    settings.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new LauncherSettings { CloseAfterGameLaunch = true });

    LaunchGameHandler handler = new(installations, verifier, launcher, settings, clock, NullLogger<LaunchGameHandler>.Instance);

    await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

    Received.InOrder(() =>
    {
        launcher.LaunchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        settings.GetAsync(Arg.Any<CancellationToken>());
    });
}
```

You need to add two `using` statements to the test file's top: `using NSubstitute.ExceptionExtensions;` (for `.Throws`) and `using System;` (already present via GlobalUsings, but verify).

- [ ] **Step 2: Run all handler tests**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj --filter FullyQualifiedName~LaunchGameHandlerTests
```

Expected: 7 passed.

- [ ] **Step 3: Commit**

```powershell
git add Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Game/Launch/LaunchGameHandlerTests.cs
git commit -m "test(✅): cover LaunchGameHandler edge cases — not-found, verifier-throws, stamp-throws, ordering"
```

---

## Task 7: WindowsProcessLauncher — implementation + smoke tests + DI registration

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Game/WindowsProcessLauncher.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Extensions/InfrastructureServiceCollectionExtensions.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Game/WindowsProcessLauncherTests.cs`
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/GlobalUsings.cs`

- [ ] **Step 1: Add the namespace to Infrastructure tests' GlobalUsings**

In `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/GlobalUsings.cs`, add (alphabetical with the other `Infrastructure.*` entries):

```csharp
global using Erdmier.ZooTycoonLauncher.Infrastructure.Game;
```

- [ ] **Step 2: Write the failing smoke tests**

Create `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Game/WindowsProcessLauncherTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Game;

public sealed class WindowsProcessLauncherTests
{
    [ Fact ]
    public async Task LaunchAsync_KnownGoodExe_ReturnsStarted()
    {
        WindowsProcessLauncher launcher = new();
        string cmdPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), path2: "cmd.exe");

        ProcessLaunchResult result = await launcher.LaunchAsync(cmdPath, workingDirectory: Environment.SystemDirectory, CancellationToken.None);

        result.Started.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [ Fact ]
    public async Task LaunchAsync_NonExistentPath_ReturnsStartFailedWithMessage()
    {
        WindowsProcessLauncher launcher = new();
        string missingPath = @"C:\definitely-not-real-" + Guid.NewGuid().ToString(format: "N") + ".exe";

        ProcessLaunchResult result = await launcher.LaunchAsync(missingPath, workingDirectory: @"C:\", CancellationToken.None);

        result.Started.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
    }
}
```

The first test spawns `cmd.exe` momentarily — without arguments, it exits as soon as no console is attached. It is harmless. We do not wait for or kill the process.

- [ ] **Step 3: Build to confirm compile failure**

```powershell
dotnet build Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj
```

Expected: `CS0246: type 'WindowsProcessLauncher' not found`.

- [ ] **Step 4: Create `WindowsProcessLauncher`**

Create `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Game/WindowsProcessLauncher.cs`:

```csharp
using System.ComponentModel;
using System.Diagnostics;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Game;

/// <summary>The Windows implementation of <see cref="IProcessLauncher" />. Starts <c>zoo.exe</c> with the shell-execute semantics that match the Ref launcher. SDD §7.10.</summary>
public sealed class WindowsProcessLauncher : IProcessLauncher
{
    /// <inheritdoc />
    public Task<ProcessLaunchResult> LaunchAsync(string exePath, string workingDirectory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
        catch (Win32Exception ex)
        {
            return Task.FromResult(new ProcessLaunchResult(Started: false, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(new ProcessLaunchResult(Started: false, ex.Message));
        }
        catch (FileNotFoundException ex)
        {
            return Task.FromResult(new ProcessLaunchResult(Started: false, ex.Message));
        }
    }
}
```

- [ ] **Step 5: Expose the new namespace in Infrastructure GlobalUsings**

Open `Source/Erdmier.ZooTycoonLauncher.Infrastructure/GlobalUsings.cs`. Add one new line, alphabetically with the other `Infrastructure.*` entries (place it after `Infrastructure.Discovery`):

```csharp
global using Erdmier.ZooTycoonLauncher.Infrastructure.Game;
```

- [ ] **Step 6: Register in DI**

Open `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Extensions/InfrastructureServiceCollectionExtensions.cs` and add (place it alongside the other singletons such as `IInstallationVerifier`):

```csharp
services.AddSingleton<IProcessLauncher, WindowsProcessLauncher>();
```

- [ ] **Step 7: Run the smoke tests**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj --filter FullyQualifiedName~WindowsProcessLauncherTests
```

Expected: 2 passed.

- [ ] **Step 8: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure/Game/WindowsProcessLauncher.cs Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Extensions/InfrastructureServiceCollectionExtensions.cs Source/Erdmier.ZooTycoonLauncher.Infrastructure/GlobalUsings.cs Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Game/WindowsProcessLauncherTests.cs Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/GlobalUsings.cs
git commit -m "feat(✨): add WindowsProcessLauncher and register IProcessLauncher in DI"
```

---

## Task 8: IApplicationLifecycle abstraction + Avalonia implementation

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IApplicationLifecycle.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaApplicationLifecycle.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs`

- [ ] **Step 1: Create `IApplicationLifecycle`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>Wraps the Avalonia application lifetime so view models can request shutdown without referencing Avalonia directly.</summary>
public interface IApplicationLifecycle
{
    /// <summary>Requests that the application shut down. Equivalent to pressing the main window's close button.</summary>
    void RequestShutdown();
}
```

- [ ] **Step 2: Create `AvaloniaApplicationLifecycle`**

```csharp
using Avalonia.Controls.ApplicationLifetimes;

namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>The Avalonia-bound implementation of <see cref="IApplicationLifecycle" />.</summary>
internal sealed class AvaloniaApplicationLifecycle : IApplicationLifecycle
{
    /// <inheritdoc />
    public void RequestShutdown()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
```

- [ ] **Step 3: Register in DI**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs` and add (place it with the other singletons):

```csharp
services.AddSingleton<IApplicationLifecycle, AvaloniaApplicationLifecycle>();
```

- [ ] **Step 4: Build**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IApplicationLifecycle.cs Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaApplicationLifecycle.cs Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs
git commit -m "feat(✨): add IApplicationLifecycle Desktop abstraction"
```

---

## Task 9: LaunchErrorView + LaunchErrorViewModel + IDialogService chain

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/LaunchErrorViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/LaunchErrorView.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/LaunchErrorView.axaml.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaDialogService.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`

We build the entire dialogue chain in one task so no file is shipped half-implemented.

- [ ] **Step 1: Create `LaunchErrorViewModel`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>The view model for the modeless launch-error window. Holds the message displayed verbatim.</summary>
public sealed class LaunchErrorViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">The error message to display.</param>
    public LaunchErrorViewModel(string message) => Message = message;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public LaunchErrorViewModel()
        : this(message: "Zoo Tycoon could not be launched: example message.")
    { }

    /// <summary>The error message to display verbatim.</summary>
    public string Message { get; }
}
```

- [ ] **Step 2: Create `LaunchErrorView.axaml`**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs"
        x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs.LaunchErrorView"
        x:DataType="vm:LaunchErrorViewModel"
        Title="Cannot Launch Zoo Tycoon"
        SizeToContent="WidthAndHeight"
        CanResize="False"
        WindowStartupLocation="CenterOwner"
        ShowInTaskbar="False">
    <StackPanel Margin="12"
                Spacing="12"
                MinWidth="320">
        <TextBlock Text="{ Binding Message }"
                   TextWrapping="Wrap" />
        <Button Content="OK"
                HorizontalAlignment="Right"
                MinWidth="80"
                Click="OnOkClick" />
    </StackPanel>
</Window>
```

- [ ] **Step 3: Create `LaunchErrorView.axaml.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>Modeless Win95-style error window for launch failures.</summary>
public sealed partial class LaunchErrorView : Window
{
    /// <summary>Initialises a new instance.</summary>
    public LaunchErrorView() => AvaloniaXamlLoader.Load(this);

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close();
}
```

(The `Avalonia.Controls`, `Avalonia.Interactivity`, and `Avalonia.Markup.Xaml` namespaces are already exposed via `Desktop/GlobalUsings.cs`.)

- [ ] **Step 4: Create `IDialogService`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>Surface for opening Desktop-layer dialogues (modeless or modal). Grows with each new dialogue slice.</summary>
public interface IDialogService
{
    /// <summary>Opens the modeless launch-error window with the supplied message. Owned by <c>MainWindow</c> when available.</summary>
    /// <param name="message">The error message to display verbatim.</param>
    void ShowLaunchError(string message);
}
```

- [ ] **Step 5: Create `AvaloniaDialogService`**

```csharp
using Avalonia.Controls.ApplicationLifetimes;

using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;
using Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>The Avalonia-bound implementation of <see cref="IDialogService" />.</summary>
internal sealed class AvaloniaDialogService : IDialogService
{
    /// <inheritdoc />
    public void ShowLaunchError(string message)
    {
        LaunchErrorView view = new()
        {
            DataContext = new LaunchErrorViewModel(message),
        };

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            view.Show(desktop.MainWindow);
        }
        else
        {
            view.Show();
        }
    }
}
```

- [ ] **Step 6: Register in DI**

In `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs`, add (next to the `IApplicationLifecycle` line from Task 8):

```csharp
services.AddSingleton<IDialogService, AvaloniaDialogService>();
```

- [ ] **Step 7: Expose new namespaces in Desktop GlobalUsings**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`. Add:

```csharp
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;
global using Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;
```

Place them alphabetically with the other `Erdmier.ZooTycoonLauncher.Desktop.*` entries.

- [ ] **Step 8: Build the solution**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: build succeeds, including the AXAML compilation step.

- [ ] **Step 9: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/LaunchErrorViewModel.cs Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/LaunchErrorView.axaml Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/LaunchErrorView.axaml.cs Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaDialogService.cs Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs
git commit -m "feat(✨): add LaunchErrorView and IDialogService Desktop abstraction"
```

---

## Task 10: GeneralTabViewModel — add IMediator, IsBusy, LaunchCommand, LaunchOutcomeRaised event

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/GeneralTabViewModel.cs`

- [ ] **Step 1: Replace the file with the extended version**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/GeneralTabViewModel.cs` and replace the entire contents with:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;

/// <summary>The view model for the General tab inside the ReadyToPlay and CannotPlay states. Owns the Launch Game command. SDD §7.10, §9.2.</summary>
public sealed partial class GeneralTabViewModel : ViewModelBase
{
    private readonly Guid _installationId;

    private readonly IMediator? _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The installation whose general information is displayed.</param>
    /// <param name="mediator">The Mediator dispatcher.</param>
    public GeneralTabViewModel(InstallationSummary installation, IMediator mediator)
        : this(installation)
        => _mediator = mediator;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public GeneralTabViewModel()
        : this(new InstallationSummary(Guid.Empty,
                                       Name: "Designer Installation",
                                       Path: @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
                                       InstallationValidity.Valid,
                                       IsDefault: true,
                                       DateTime.UtcNow,
                                       ModifiedUtc: null,
                                       LastPlayedUtc: null,
                                       LastOpenedUtc: null))
    { }

    private GeneralTabViewModel(InstallationSummary installation)
    {
        _installationId  = installation.Id;
        InstallationName = installation.Name;
        InstallationPath = installation.Path;
        CanLaunch        = installation.Validity == InstallationValidity.Valid;
    }

    /// <summary>Raised after the launch command receives a result. <see cref="ReadyToPlayViewModel" /> subscribes and routes outcomes to chrome capabilities.</summary>
    public event EventHandler<LaunchGameResult>? LaunchOutcomeRaised;

    /// <summary><see langword="true" /> when the installation summary was valid at boot; the just-in-time verification inside the handler catches drift that happens after boot.</summary>
    public bool CanLaunch { get; }

    /// <summary>The installation's user-visible name.</summary>
    public string InstallationName { get; }

    /// <summary>The installation's directory path.</summary>
    public string InstallationPath { get; }

    /// <summary><see langword="true" /> while a launch is in flight; disables the button to prevent double-dispatch.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(LaunchCommand)) ]
    public partial bool IsBusy { get; set; }

    [ RelayCommand(CanExecute = nameof(CanExecuteLaunch)) ]
    private async Task LaunchAsync(CancellationToken cancellationToken)
    {
        if (_mediator is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            ErrorOr<LaunchGameResult> result =
                await _mediator.Send(new LaunchGameCommand(_installationId), cancellationToken);

            LaunchGameResult outcome = result.IsError
                ? new LaunchGameResult(LaunchGameOutcome.StartFailed, CloseAfterGameLaunch: false, result.FirstError.Description)
                : result.Value;

            LaunchOutcomeRaised?.Invoke(this, outcome);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteLaunch() => CanLaunch && !IsBusy && _mediator is not null;
}
```

Note: the runtime ctor delegates to the private installation-only ctor used by the designer; only the runtime ctor assigns `_mediator`. The `CanExecuteLaunch` predicate gates on `_mediator is not null`, so the designer surface shows a disabled button.

The file needs a top-of-file `using` for `IMediator` (or it can rely on the `Application.Game.Launch` global using already added in Task 2 plus `Mediator` namespace which arrives via the Desktop GlobalUsings.cs). Verify by building.

- [ ] **Step 2: Add `Application.Game.Launch` namespace to Desktop GlobalUsings**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`. Add one new line:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Game.Launch;
```

Place it alphabetically among the other `Erdmier.ZooTycoonLauncher.Application.*` entries (after `Application.Common.Models`).

The other namespaces this view model needs (`Mediator`, `CommunityToolkit.Mvvm.ComponentModel`, `CommunityToolkit.Mvvm.Input`) are already exposed by the existing file.

- [ ] **Step 3: Build**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: build succeeds. `[ RelayCommand ]` source generator produces `LaunchCommand`.

- [ ] **Step 4: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/GeneralTabViewModel.cs Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs
git commit -m "feat(✨): add LaunchCommand and LaunchOutcomeRaised event to GeneralTabViewModel"
```

---

## Task 11: GeneralTabView — bind the Launch Game button

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/GeneralTabView.axaml`

- [ ] **Step 1: Replace the placeholder Button**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/GeneralTabView.axaml`. Replace:

```xml
<Button Content="Launch Game"
        IsEnabled="False"
        Margin="0,8,0,0" />
```

with:

```xml
<Button Content="Launch Game"
        Command="{ Binding LaunchCommand }"
        Margin="0,8,0,0" />
```

Leave the "Screen modes — coming soon" and "Last played — coming soon" placeholders in place; they belong to the next slice.

- [ ] **Step 2: Build**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/GeneralTabView.axaml
git commit -m "feat(✨): bind General tab Launch Game button to LaunchCommand"
```

---

## Task 12: ReadyToPlayViewModel — orchestrate launch outcomes

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/ReadyToPlayViewModel.cs`

- [ ] **Step 1: Replace the file with the orchestrating version**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/ReadyToPlayViewModel.cs` and replace the contents with:

```csharp
using Erdmier.ZooTycoonLauncher.Desktop.Composition;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>The view model for the ReadyToPlay state — the active installation is valid and the game can be launched. Routes launch outcomes from the General tab to chrome capabilities. SDD §7.10.</summary>
public sealed class ReadyToPlayViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;

    private readonly IApplicationLifecycle _lifecycle;

    private readonly Func<CancellationToken, Task> _rebootAsync;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The resolved active installation.</param>
    /// <param name="rebootAsync">Delegate that re-issues the boot pipeline (typically <c>MainWindowViewModel.BootAsync</c>).</param>
    /// <param name="lifecycle">Chrome service for requesting application shutdown.</param>
    /// <param name="dialogs">Chrome service for opening modeless dialogues.</param>
    /// <param name="mediator">The Mediator dispatcher (passed to the General tab).</param>
    public ReadyToPlayViewModel(InstallationSummary           installation,
                                Func<CancellationToken, Task> rebootAsync,
                                IApplicationLifecycle         lifecycle,
                                IDialogService                dialogs,
                                IMediator                     mediator)
    {
        _rebootAsync     = rebootAsync;
        _lifecycle       = lifecycle;
        _dialogs         = dialogs;

        InstallationName = installation.Name;
        InstallationPath = installation.Path;
        IsDefault        = installation.IsDefault;

        GeneralTab   = new GeneralTabViewModel(installation, mediator);
        IniConfigTab = new IniConfigTabViewModel();
        ScenariosTab = new ScenariosTabViewModel();

        GeneralTab.LaunchOutcomeRaised += OnLaunchOutcomeRaised;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public ReadyToPlayViewModel()
        : this(new InstallationSummary(Guid.Empty,
                                       Name: "Designer Installation",
                                       Path: @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
                                       InstallationValidity.Valid,
                                       IsDefault: true,
                                       DateTime.UtcNow,
                                       ModifiedUtc: null,
                                       LastPlayedUtc: null,
                                       LastOpenedUtc: null))
    { }

    private ReadyToPlayViewModel(InstallationSummary installation)
    {
        _rebootAsync     = static _ => Task.CompletedTask;
        _lifecycle       = new NoOpApplicationLifecycle();
        _dialogs         = new NoOpDialogService();

        InstallationName = installation.Name;
        InstallationPath = installation.Path;
        IsDefault        = installation.IsDefault;

        GeneralTab   = new GeneralTabViewModel();
        IniConfigTab = new IniConfigTabViewModel();
        ScenariosTab = new ScenariosTabViewModel();

        GeneralTab.LaunchOutcomeRaised += OnLaunchOutcomeRaised;
    }

    /// <summary>General tab view model.</summary>
    public GeneralTabViewModel GeneralTab { get; }

    /// <summary>INI Config tab view model.</summary>
    public IniConfigTabViewModel IniConfigTab { get; }

    /// <summary>The installation's user-visible name.</summary>
    public string InstallationName { get; }

    /// <summary>The installation's directory path.</summary>
    public string InstallationPath { get; }

    /// <summary><see langword="true" /> when this is the default installation.</summary>
    public bool IsDefault { get; }

    /// <summary>Scenarios tab view model.</summary>
    public ScenariosTabViewModel ScenariosTab { get; }

    private async void OnLaunchOutcomeRaised(object? sender, LaunchGameResult result)
    {
        try
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
        catch (Exception ex)
        {
            _dialogs.ShowLaunchError($"The launcher could not refresh installation state: {ex.Message}");
        }
    }
}

file sealed class NoOpApplicationLifecycle : IApplicationLifecycle
{
    public void RequestShutdown() { }
}

file sealed class NoOpDialogService : IDialogService
{
    public void ShowLaunchError(string message) { }
}
```

The file-scoped `NoOp*` types service the designer constructor only — they let the XAML designer instantiate `ReadyToPlayViewModel` without any real DI plumbing.

- [ ] **Step 2: Build**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: the **only** remaining build break is `MainWindowViewModel.RouteResult` — it still calls `new ReadyToPlayViewModel(result.ActiveInstallation!)`, which now has the wrong signature. Task 13 fixes that.

If other build errors appear, stop and diagnose before continuing.

- [ ] **Step 3: Stage but do not commit yet**

This task and Task 13 must commit together — the build is broken in between. Skip the commit for now; we will commit after Task 13.

---

## Task 13: MainWindowViewModel — wire chrome services through to ReadyToPlayViewModel

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: Replace the file**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs` and replace the contents with:

```csharp
using Erdmier.ZooTycoonLauncher.Desktop.Composition;

using AppBoot = Erdmier.ZooTycoonLauncher.Application.Boot;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels;

/// <summary>The main window's view model. Dispatches <c>BootCommand</c> on load and routes the result to the active state view model via <c>ActiveContent</c> (SDD §9.2).</summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;

    private readonly IApplicationLifecycle _lifecycle;

    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="lifecycle">Chrome service for requesting application shutdown.</param>
    /// <param name="dialogs">Chrome service for opening modeless dialogues.</param>
    public MainWindowViewModel(IMediator mediator, IApplicationLifecycle lifecycle, IDialogService dialogs)
    {
        _mediator  = mediator;
        _lifecycle = lifecycle;
        _dialogs   = dialogs;
    }

    /// <summary>The currently active state or content view model; drives the main window's <c>ContentControl</c> via <see cref="Composition.ViewLocator" />.</summary>
    [ ObservableProperty ]
    public partial object? ActiveContent { get; set; }

    [ RelayCommand ]
    private async Task BootAsync(CancellationToken cancellationToken)
    {
        ActiveContent = new LookingForZooTycoonViewModel();

        ErrorOr<AppBoot.BootResult> result = await _mediator.Send(new AppBoot.BootCommand(), cancellationToken);

        ActiveContent = result.IsError ? new NoGameInstallationFoundViewModel(locatedCandidatePath: null) : RouteResult(result.Value);
    }

    private ViewModelBase RouteResult(AppBoot.BootResult result)
        => result.Outcome switch
        {
            AppBoot.BootOutcome.ReadyToPlay             => new ReadyToPlayViewModel(result.ActiveInstallation!,
                                                                                    rebootAsync: BootAsync,
                                                                                    _lifecycle,
                                                                                    _dialogs,
                                                                                    _mediator),
            AppBoot.BootOutcome.CannotPlay              => new CannotPlayViewModel(result.ActiveInstallation!),
            AppBoot.BootOutcome.NoGameInstallationFound => new NoGameInstallationFoundViewModel(result.LocatedCandidatePath),
            AppBoot.BootOutcome.OpenGameInstallation    => new OpenGameInstallationViewModel(),
            var _                                       => new NoGameInstallationFoundViewModel(locatedCandidatePath: null),
        };
}
```

Two changes worth flagging:

1. `RouteResult` is no longer `static` — it captures `BootAsync`, `_lifecycle`, `_dialogs`, `_mediator`.
2. `BootAsync` is referenced as a method group (`rebootAsync: BootAsync`); the generated `BootCommand` already exists, but passing the raw async method is the right hook for the reboot delegate.

- [ ] **Step 2: Build**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: build succeeds.

- [ ] **Step 3: Run the full test suite**

```powershell
dotnet test Erdmier.ZooTycoonLauncher.slnx
```

Expected: all tests pass, including architecture tests (`OneTypePerFileTests`, `NoFilesAtAssemblyRootTests`, `DependencyDirectionTests`, `MainWindowSizeTests`).

- [ ] **Step 4: Commit Tasks 12 + 13 together**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/ReadyToPlayViewModel.cs Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs
git commit -m "feat(✨): orchestrate launch outcomes through ReadyToPlay and MainWindow view models"
```

---

## Task 14: Clean build and manual smoke test

**Files:** none modified — verification only.

- [ ] **Step 1: Clean and full rebuild**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: clean succeeds; build succeeds with no warnings introduced by this slice.

- [ ] **Step 2: Run the full test suite**

```powershell
dotnet test Erdmier.ZooTycoonLauncher.slnx
```

Expected: all tests pass.

- [ ] **Step 3: Launch the application**

```powershell
dotnet run --project Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj
```

Expected: launcher window appears, boots to `ReadyToPlay` (assuming a valid installation already exists in `Launcher.db`).

- [ ] **Step 4: Manual smoke — happy path**

Click **Launch Game**.

Expected: `zoo.exe` starts. With `CloseAfterGameLaunch = false` (current default), the launcher remains open and the button re-enables immediately.

- [ ] **Step 5: Manual smoke — `CloseAfterGameLaunch`**

Stop the launcher. With a SQLite tool (or `Microsoft.Data.Sqlite` PowerShell snippet), set `LauncherSettings.CloseAfterGameLaunch = 1`:

```powershell
$db = "$env:LOCALAPPDATA\ZooTycoonLauncher\Data\Launcher.db"
& "sqlite3" $db "UPDATE LauncherSettings SET CloseAfterGameLaunch = 1 WHERE Id = 1;"
```

If `sqlite3.exe` is not on PATH, use any SQLite GUI to make the update. Then `dotnet run` again, click **Launch Game**.

Expected: `zoo.exe` starts, the launcher window closes.

Reset the flag afterwards:

```powershell
& "sqlite3" $db "UPDATE LauncherSettings SET CloseAfterGameLaunch = 0 WHERE Id = 1;"
```

- [ ] **Step 6: Manual smoke — drift simulation**

Stop the launcher. Rename `zoo.exe` → `zoo.bak` in the installation directory. `dotnet run`. Once boot completes (still `ReadyToPlay` because boot happened *before* the rename — drift only shows after a re-verify), click **Launch Game**.

Expected: the UI transitions to `CannotPlay`. The drift-detection branch of the handler fired and re-issued boot through the lifecycle.

Rename `zoo.bak` back to `zoo.exe` and verify the next launch picks up the corrected state.

- [ ] **Step 7: Manual smoke — start-failure simulation**

Stop the launcher. Apply a deny-execute ACL to `zoo.exe`:

```powershell
$exe = "C:\Path\To\Zoo\zoo.exe"
icacls $exe /deny "$env:USERNAME:(RX)"
```

`dotnet run`, click **Launch Game**.

Expected: a modeless `LaunchErrorView` window appears with the Win32 access-denied message. The launcher's main window stays in `ReadyToPlay`. Clicking **OK** closes the error window.

Reset the ACL afterwards:

```powershell
icacls $exe /remove:d "$env:USERNAME"
```

- [ ] **Step 8: Final commit (only if any docs or fixes were needed during smoke)**

If no fixes were needed, this task closes without a commit. If fixes were needed, commit them with a clear message describing what was wrong.

---

## Notes for the executor

- **Conventional commits with gitmoji** — every commit message uses the `type(emoji): subject` form. Type → emoji map (from CLAUDE.md): `feat`→✨, `fix`→🐛, `refactor`→♻️, `style`→🎨, `docs`→📝, `chore`→🔧, `perf`→⚡, `test`→✅, `build`→📦, `ci`→👷.
- **PowerShell, not Bash.** All shell commands above are PowerShell.
- **No `[ UsedImplicitly ]` attributes are added in this plan.** Per user feedback, ReSharper warnings are addressed after-the-fact by the author, not pre-emptively.
- **Designer constructors** stay marked plain (no `[ UsedImplicitly ]`); the existing convention in the codebase has them naked.
- **British English** in any comments or new prose you author.
- **One type per file.** Note that the original plan called for `LaunchGameOutcome` to be co-located with `LaunchGameResult` based on a false reading of the `BootResult`/`BootOutcome` precedent; the executed code instead splits them into separate files to satisfy `OneTypePerFileTests`. The file-scoped `NoOp*` types in `ReadyToPlayViewModel`'s designer-only path remain a documented exception (`file`-scoped types are exempt from the rule).
- **If a test fails or build breaks unexpectedly**, do not push past it. Diagnose first; the plan is wrong before your environment is.
- **Tasks 12 and 13 share a commit.** The build is intentionally broken between them; do not commit Task 12 alone.
