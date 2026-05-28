# Startup Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps
> use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire the `BootCommand` → `BootHandler` state machine (SDD §7.1.1) into the Desktop layer so the launcher transitions from its placeholder banner to one of five real
main-window states on startup.

**Architecture:** `BootHandler` implements SDD §7.1.1 in full, dispatched via Mediator from `MainWindowViewModel.BootAsync`. The result is routed to one of five state
`UserControl`+`ViewModel` pairs via a `ContentControl` resolved by the existing `ViewLocator`. Tab ViewModels and Views are skeleton-only (no real content).
`NullIniSnapshotService` gains a `SynchroniseAsync` no-op stub.

**Tech Stack:** .NET 10 / C# 13, Avalonia 11.3, CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`), Mediator (martinothamar), ErrorOr, NSubstitute, Shouldly, xUnit,
FakeTimeProvider.

**Spec:** `docs/superpowers/specs/2026-05-28-startup-flow-design.md`

---

## Task 1: IIniSnapshotService.SynchroniseAsync + NullIniSnapshotService stub

**Files:**

- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniSnapshotService.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/IniSnapshots/NullIniSnapshotService.cs`

- [ ] **Step 1: Add `SynchroniseAsync` to the interface**

Replace the entire content of `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniSnapshotService.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>
///     Capture and synchronisation surface for per-installation INI snapshots (SDD §7.3 / §8.2). The Installation Lifecycle slice only invokes
///     <see cref="CaptureOriginalAsync" />; the INI Config slice expands the surface.
/// </summary>
/// <remarks>
///     The Infrastructure layer ships a <c>NullIniSnapshotService</c> that returns <see cref="ErrorOr.Result.Success" /> and logs a warning for both methods. The real
///     implementation lands in the INI Config slice with no signature change.
/// </remarks>
public interface IIniSnapshotService
{
    /// <summary>
    ///     Reads <c>zoo.ini</c> for the supplied installation and writes the <c>Original</c> + <c>Current</c> snapshots in one transaction. No-op when
    ///     <see cref="GameInstallation.HasIni" /> is <see langword="false" />.
    /// </summary>
    /// <param name="installation">The newly created installation row.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success on capture or no-op; a typed error on parse or persist failure.</returns>
    Task<ErrorOr<Success>> CaptureOriginalAsync(GameInstallation installation, CancellationToken cancellationToken);

    /// <summary>
    ///     Checks whether <c>zoo.ini</c> has drifted on disk since the <c>Current</c> snapshot was written. When drift is detected, archives <c>Current</c> to
    ///     <c>Historical</c> and writes a new <c>Current</c> from the on-disk values. No-op when <see cref="GameInstallation.HasIni" /> is <see langword="false" />.
    /// </summary>
    /// <param name="installation">The installation to synchronise.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="ErrorOr.Result.Success" /> on synchronisation or no-op; a typed error on parse or persist failure.</returns>
    Task<ErrorOr<Success>> SynchroniseAsync(GameInstallation installation, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Implement the stub in `NullIniSnapshotService`**

Replace the entire content of `Source/Erdmier.ZooTycoonLauncher.Infrastructure/IniSnapshots/NullIniSnapshotService.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.IniSnapshots;

/// <summary>
///     Placeholder <see cref="IIniSnapshotService" /> shipped during the Installation Lifecycle slice. The INI Config slice replaces it with a real implementation that parses
///     <c>zoo.ini</c> and writes the <c>Original</c> + <c>Current</c> snapshots.
/// </summary>
public sealed class NullIniSnapshotService : IIniSnapshotService
{
    private readonly ILogger _logger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="logger">The Serilog logger.</param>
    public NullIniSnapshotService(ILogger logger) => _logger = logger;

    /// <inheritdoc />
    public Task<ErrorOr<Success>> CaptureOriginalAsync(GameInstallation installation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (installation.HasIni)
        {
            _logger.Warning(messageTemplate: "INI snapshot capture for {InstallationId} ({Name}) is deferred — replace NullIniSnapshotService when the INI Config slice lands.",
                            installation.Id,
                            installation.Name);
        }

        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    /// <inheritdoc />
    public Task<ErrorOr<Success>> SynchroniseAsync(GameInstallation installation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (installation.HasIni)
        {
            _logger.Warning(messageTemplate: "INI snapshot synchronisation for {InstallationId} ({Name}) is deferred — replace NullIniSnapshotService when the INI Config slice lands.",
                            installation.Id,
                            installation.Name);
        }

        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }
}
```

- [ ] **Step 3: Build and run tests**

```
dotnet build Erdmier.ZooTycoonLauncher.slnx
dotnet test Erdmier.ZooTycoonLauncher.slnx
```

Expected: build succeeds, all existing tests pass (the new interface method is only tested indirectly in Task 3).

- [ ] **Step 4: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniSnapshotService.cs
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure/IniSnapshots/NullIniSnapshotService.cs
git commit -m "feat(✨): add IIniSnapshotService.SynchroniseAsync stub"
```

---

## Task 2: BootCommand + BootResult types

**Files:**

- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Boot/BootCommand.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Boot/BootResult.cs`
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs`

- [ ] **Step 1: Create `BootCommand.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Boot;

/// <summary>Dispatched at application startup to resolve which installation to open and transition the main window to the correct state (SDD §7.1).</summary>
public sealed record BootCommand : ICommand<ErrorOr<BootResult>>;
```

- [ ] **Step 2: Create `BootResult.cs`**

`BootOutcome` and `BootResult` are a tightly coupled pair never used separately — documented exception to the one-type-per-file rule.

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Boot;

/// <summary>Terminal state determined by <see cref="BootHandler" /> after running the SDD §7.1.1 state machine.</summary>
public enum BootOutcome
{
    /// <summary>The active installation is valid; the launcher is ready to play.</summary>
    ReadyToPlay,

    /// <summary>The active installation is invalid or synchronisation failed; the launcher cannot launch the game.</summary>
    CannotPlay,

    /// <summary>No suitable installation was found; the user must add one.</summary>
    NoGameInstallationFound,

    /// <summary>The startup preference is <c>NoInstallation</c>; the user must select an installation manually.</summary>
    OpenGameInstallation,
}

/// <summary>
///     Result returned by <see cref="BootHandler" />. <see cref="BootOutcome" /> and <see cref="BootResult" /> are a tightly-coupled pair and are never used separately; they
///     live in the same file as the documented exception to the one-type-per-file rule.
/// </summary>
/// <param name="Outcome">The terminal state.</param>
/// <param name="ActiveInstallation">The installation to display, or <see langword="null" /> when none was resolved.</param>
/// <param name="LocatedCandidatePath">
///     Non-null when <see cref="BootOutcome.NoGameInstallationFound" /> is returned because <see cref="IInstallationLocator" /> found a candidate directory but the Add
///     Installation dialogue is deferred; surfaces the discovery to the user.
/// </param>
public sealed record BootResult(BootOutcome Outcome, InstallationSummary? ActiveInstallation, string? LocatedCandidatePath);
```

- [ ] **Step 3: Add the Boot namespace to the test project's global usings**

Open `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs` and add one line after the existing `Application.Installations.*` usings:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Boot;
```

- [ ] **Step 4: Build**

```
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: clean build.

- [ ] **Step 5: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Application/Boot/BootCommand.cs
git add Source/Erdmier.ZooTycoonLauncher.Application/Boot/BootResult.cs
git add Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs
git commit -m "feat(✨): add BootCommand and BootResult types"
```

---

## Task 3: BootHandler (TDD)

**Files:**

- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Boot/BootHandlerTests.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Boot/BootHandler.cs`

- [ ] **Step 1: Create the test file with all 11 failing tests**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Boot;

public sealed class BootHandlerTests
{
    [ Fact ]
    public async Task Handle_ReturnsReadyToPlay_WhenDefaultInstallationValid()
    {
        Guid id = Guid.CreateVersion7();

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { DefaultInstallationId = id });

        GameInstallation row = new()
        {
            Id = id, Name = "Main", Path = @"C:\ZT",
            HasExe = true, HasIni = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(row, Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(), snapshots, TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.ReadyToPlay);
        result.Value.ActiveInstallation!.Id.ShouldBe(id);
    }

    [ Fact ]
    public async Task Handle_ReturnsCannotPlay_WhenVerificationFails()
    {
        Guid id = Guid.CreateVersion7();

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { DefaultInstallationId = id });

        GameInstallation row = new()
        {
            Id = id, Name = "Main", Path = @"C:\ZT",
            HasExe = true, HasIni = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: false, HasIni: true));

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(),
                                  Substitute.For<IIniSnapshotService>(), TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.CannotPlay);
        result.Value.ActiveInstallation!.Id.ShouldBe(id);
    }

    [ Fact ]
    public async Task Handle_ReturnsCannotPlay_WhenSynchroniseFails()
    {
        Guid id = Guid.CreateVersion7();

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { DefaultInstallationId = id });

        GameInstallation row = new()
        {
            Id = id, Name = "Main", Path = @"C:\ZT",
            HasExe = true, HasIni = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(row, Arg.Any<CancellationToken>())
                 .Returns(Error.Failure(code: "Ini.SyncFailed", description: "boom"));

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(), snapshots, TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.CannotPlay);
    }

    [ Fact ]
    public async Task Handle_PersistsVerificationDrift()
    {
        Guid     id      = Guid.CreateVersion7();
        DateTime fakeNow = new(year: 2026, month: 5, day: 28, hour: 10, minute: 0, second: 0, DateTimeKind.Utc);

        FakeTimeProvider clock = new(fakeNow);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { DefaultInstallationId = id });

        GameInstallation row = new()
        {
            Id = id, Name = "Main", Path = @"C:\ZT",
            HasExe = true, HasIni = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: false, HasIni: true));

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(),
                                  Substitute.For<IIniSnapshotService>(), clock);

        await handler.Handle(new BootCommand(), CancellationToken.None);

        await installations.Received(requiredNumberOfCalls: 1)
                           .UpdateAsync(Arg.Is<GameInstallation>(i => i.Id == id
                                                                      && !i.HasExe
                                                                      && i.HasIni
                                                                      && i.ModifiedUtc == fakeNow),
                                        Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_StampsLastOpenedUtc_OnReadyToPlay()
    {
        Guid     id      = Guid.CreateVersion7();
        DateTime fakeNow = new(year: 2026, month: 5, day: 28, hour: 10, minute: 0, second: 0, DateTimeKind.Utc);

        FakeTimeProvider clock = new(fakeNow);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { DefaultInstallationId = id });

        GameInstallation row = new()
        {
            Id = id, Name = "Main", Path = @"C:\ZT",
            HasExe = true, HasIni = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(row, Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(), snapshots, clock);

        await handler.Handle(new BootCommand(), CancellationToken.None);

        await installations.Received()
                           .UpdateAsync(Arg.Is<GameInstallation>(i => i.Id == id && i.LastOpenedUtc == fakeNow),
                                        Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_PromotesDefault_WhenDefaultIdNullAndRowsExist()
    {
        Guid             promotedId = Guid.CreateVersion7();
        GameInstallation promoted   = new() { Id = promotedId, Name = "Alpha", Path = @"C:\ZT", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        LauncherSettings            settingsRow   = new() { DefaultInstallationId = null };
        ILauncherSettingsRepository settings      = Substitute.For<ILauncherSettingsRepository>();
        IInstallationRepository     installations = Substitute.For<IInstallationRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(settingsRow);

        installations.FindDefaultPromotionCandidateAsync(Arg.Any<CancellationToken>())
                     .Returns(promoted);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(promoted.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(promoted, Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(), snapshots, TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.Value.Outcome.ShouldBe(BootOutcome.ReadyToPlay);

        await settings.Received(requiredNumberOfCalls: 1)
                      .UpdateAsync(Arg.Is<LauncherSettings>(s => s.DefaultInstallationId == promotedId),
                                   Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_AutoLocates_CandidateFound_ReturnsNoGameInstallationFoundWithPath()
    {
        ILauncherSettingsRepository settings      = Substitute.For<ILauncherSettingsRepository>();
        IInstallationRepository     installations = Substitute.For<IInstallationRepository>();
        IInstallationLocator        locator       = Substitute.For<IInstallationLocator>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings());

        installations.FindDefaultPromotionCandidateAsync(Arg.Any<CancellationToken>())
                     .Returns((GameInstallation?)null);

        locator.LocateAsync(persistedLastKnownPath: null, Arg.Any<CancellationToken>())
               .Returns(new LocatedDirectory(Path: @"C:\Games\ZT", Trail: Array.Empty<LocationProbeAttempt>()));

        BootHandler handler = new(settings, installations,
                                  Substitute.For<IInstallationVerifier>(), locator,
                                  Substitute.For<IIniSnapshotService>(), TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.NoGameInstallationFound);
        result.Value.LocatedCandidatePath.ShouldBe(@"C:\Games\ZT");
    }

    [ Fact ]
    public async Task Handle_AutoLocates_NothingFound_ReturnsNoGameInstallationFound()
    {
        ILauncherSettingsRepository settings      = Substitute.For<ILauncherSettingsRepository>();
        IInstallationRepository     installations = Substitute.For<IInstallationRepository>();
        IInstallationLocator        locator       = Substitute.For<IInstallationLocator>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings());

        installations.FindDefaultPromotionCandidateAsync(Arg.Any<CancellationToken>())
                     .Returns((GameInstallation?)null);

        locator.LocateAsync(persistedLastKnownPath: null, Arg.Any<CancellationToken>())
               .Returns(new LocatedDirectory(Path: null, Trail: Array.Empty<LocationProbeAttempt>()));

        BootHandler handler = new(settings, installations,
                                  Substitute.For<IInstallationVerifier>(), locator,
                                  Substitute.For<IIniSnapshotService>(), TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.NoGameInstallationFound);
        result.Value.LocatedCandidatePath.ShouldBeNull();
    }

    [ Fact ]
    public async Task Handle_ReturnsOpenGameInstallation_WhenPreferenceIsNoInstallation()
    {
        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { LauncherStartupPreference = LauncherStartupPreference.NoInstallation });

        BootHandler handler = new(settings,
                                  Substitute.For<IInstallationRepository>(),
                                  Substitute.For<IInstallationVerifier>(),
                                  Substitute.For<IInstallationLocator>(),
                                  Substitute.For<IIniSnapshotService>(),
                                  TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.OpenGameInstallation);
    }

    [ Fact ]
    public async Task Handle_FallsBackToDefault_WhenLastPlayedHasNoCandidate()
    {
        Guid id = Guid.CreateVersion7();

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    LauncherStartupPreference = LauncherStartupPreference.LastPlayedInstallation,
                    DefaultInstallationId     = id
                });

        GameInstallation noLastPlayed = new() { Id = Guid.CreateVersion7(), Name = "A", Path = @"C:\A", AddedUtc = DateTime.UtcNow, LastPlayedUtc = null };
        GameInstallation defaultRow   = new() { Id = id,                   Name = "B", Path = @"C:\B", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns(new[] { noLastPlayed });

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(defaultRow);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(defaultRow.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(defaultRow, Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(), snapshots, TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.ReadyToPlay);
        result.Value.ActiveInstallation!.Id.ShouldBe(id);
    }

    [ Fact ]
    public async Task Handle_FallsBackToDefault_WhenLastOpenedHasNoCandidate()
    {
        Guid id = Guid.CreateVersion7();

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    LauncherStartupPreference = LauncherStartupPreference.LastOpenedInstallation,
                    DefaultInstallationId     = id
                });

        GameInstallation noLastOpened = new() { Id = Guid.CreateVersion7(), Name = "A", Path = @"C:\A", AddedUtc = DateTime.UtcNow, LastOpenedUtc = null };
        GameInstallation defaultRow   = new() { Id = id,                   Name = "B", Path = @"C:\B", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns(new[] { noLastOpened });

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(defaultRow);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(defaultRow.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(defaultRow, Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(), snapshots, TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.ReadyToPlay);
        result.Value.ActiveInstallation!.Id.ShouldBe(id);
    }
}
```

- [ ] **Step 2: Run tests — verify 11 failures (BootHandler does not exist yet)**

```
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
```

Expected: build error or 11 failures because `BootHandler` is not yet defined.

- [ ] **Step 3: Create `BootHandler.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Boot;

/// <summary>Handler for <see cref="BootCommand" />. Implements the SDD §7.1.1 startup state machine.</summary>
public sealed class BootHandler : ICommandHandler<BootCommand, ErrorOr<BootResult>>
{
    private readonly TimeProvider                _clock;

    private readonly IInstallationRepository     _installations;

    private readonly IInstallationLocator        _locator;

    private readonly IIniSnapshotService         _iniSnapshots;

    private readonly ILauncherSettingsRepository _settings;

    private readonly IInstallationVerifier       _verifier;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="settings">Launcher settings repository.</param>
    /// <param name="installations">Installation repository.</param>
    /// <param name="verifier">File-system verifier.</param>
    /// <param name="locator">Registry and path locator.</param>
    /// <param name="iniSnapshots">INI snapshot service (stub until the INI Config slice).</param>
    /// <param name="clock">Time provider for UTC timestamps.</param>
    public BootHandler(ILauncherSettingsRepository settings,
                       IInstallationRepository     installations,
                       IInstallationVerifier       verifier,
                       IInstallationLocator        locator,
                       IIniSnapshotService         iniSnapshots,
                       TimeProvider                clock)
    {
        _settings      = settings;
        _installations = installations;
        _verifier      = verifier;
        _locator       = locator;
        _iniSnapshots  = iniSnapshots;
        _clock         = clock;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<BootResult>> Handle(BootCommand command, CancellationToken cancellationToken)
    {
        LauncherSettings settings = await _settings.GetAsync(cancellationToken);

        if (settings.LauncherStartupPreference == LauncherStartupPreference.NoInstallation)
        {
            return new BootResult(BootOutcome.OpenGameInstallation, ActiveInstallation: null, LocatedCandidatePath: null);
        }

        if (settings.LauncherStartupPreference == LauncherStartupPreference.LastPlayedInstallation
            || settings.LauncherStartupPreference == LauncherStartupPreference.LastOpenedInstallation)
        {
            bool useLastPlayed = settings.LauncherStartupPreference == LauncherStartupPreference.LastPlayedInstallation;

            IReadOnlyList<GameInstallation> all = await _installations.GetAllAsync(cancellationToken);

            GameInstallation? candidate = useLastPlayed
                ? all.OrderByDescending(r => r.LastPlayedUtc).FirstOrDefault(r => r.LastPlayedUtc is not null)
                : all.OrderByDescending(r => r.LastOpenedUtc).FirstOrDefault(r => r.LastOpenedUtc is not null);

            if (candidate is not null)
            {
                return await VerifyAsync(candidate, settings, cancellationToken);
            }
        }

        return await ResolveDefaultAsync(settings, cancellationToken);
    }

    private async ValueTask<ErrorOr<BootResult>> ResolveDefaultAsync(LauncherSettings settings, CancellationToken cancellationToken)
    {
        if (settings.DefaultInstallationId is null)
        {
            GameInstallation? promoted = await _installations.FindDefaultPromotionCandidateAsync(cancellationToken);

            if (promoted is null)
            {
                return await AutoLocateAsync(cancellationToken);
            }

            settings.DefaultInstallationId = promoted.Id;

            await _settings.UpdateAsync(settings, cancellationToken);

            return await VerifyAsync(promoted, settings, cancellationToken);
        }

        GameInstallation? row = await _installations.GetByIdAsync(settings.DefaultInstallationId.Value, cancellationToken);

        if (row is null)
        {
            return new BootResult(BootOutcome.NoGameInstallationFound, ActiveInstallation: null, LocatedCandidatePath: null);
        }

        return await VerifyAsync(row, settings, cancellationToken);
    }

    private async ValueTask<BootResult> AutoLocateAsync(CancellationToken cancellationToken)
    {
        LocatedDirectory located = await _locator.LocateAsync(persistedLastKnownPath: null, cancellationToken);

        return new BootResult(BootOutcome.NoGameInstallationFound, ActiveInstallation: null, located.Path);
    }

    private async ValueTask<ErrorOr<BootResult>> VerifyAsync(GameInstallation row, LauncherSettings settings, CancellationToken cancellationToken)
    {
        VerificationResult result = await _verifier.VerifyAsync(row.Path, cancellationToken);

        if (row.HasExe != result.HasExe || row.HasIni != result.HasIni)
        {
            row.HasExe      = result.HasExe;
            row.HasIni      = result.HasIni;
            row.ModifiedUtc = _clock.GetUtcNow().UtcDateTime;

            await _installations.UpdateAsync(row, cancellationToken);
        }

        if (!result.HasExe)
        {
            return new BootResult(BootOutcome.CannotPlay, Project(row, settings), LocatedCandidatePath: null);
        }

        ErrorOr<Success> syncResult = await _iniSnapshots.SynchroniseAsync(row, cancellationToken);

        if (syncResult.IsError)
        {
            return new BootResult(BootOutcome.CannotPlay, Project(row, settings), LocatedCandidatePath: null);
        }

        row.LastOpenedUtc = _clock.GetUtcNow().UtcDateTime;

        await _installations.UpdateAsync(row, cancellationToken);

        return new BootResult(BootOutcome.ReadyToPlay, Project(row, settings), LocatedCandidatePath: null);
    }

    private static InstallationSummary Project(GameInstallation row, LauncherSettings settings)
        => new(row.Id,
               row.Name,
               row.Path,
               row.Validity,
               IsDefault: settings.DefaultInstallationId == row.Id,
               row.AddedUtc,
               row.ModifiedUtc,
               row.LastPlayedUtc,
               row.LastOpenedUtc);
}
```

- [ ] **Step 4: Run tests — verify all 11 of them pass**

```
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
```

Expected: 11 new tests pass; all existing tests still pass.

- [ ] **Step 5: Full build**

```
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

- [ ] **Step 6: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Application/Boot/BootHandler.cs
git add Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Boot/BootHandlerTests.cs
git commit -m "feat(✨): add BootHandler with full §7.1.1 state machine"
```

---

## Task 4: Tab ViewModels + Tab Views

**Files:**

- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/GeneralTabViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/IniConfigTabViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/ScenariosTabViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/GeneralTabView.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/GeneralTabView.axaml.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/IniConfigTabView.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/IniConfigTabView.axaml.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/ScenariosTabView.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/ScenariosTabView.axaml.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`

- [ ] **Step 1: Add required usings to Desktop `GlobalUsings.cs`**

Append to `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`:

```csharp
global using Avalonia.Interactivity;

global using CommunityToolkit.Mvvm.Input;

global using ErrorOr;

global using Mediator;

global using Erdmier.ZooTycoonLauncher.Application.Common.Models;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;
```

The full file after the edit:

```csharp
global using Avalonia;
global using Avalonia.Controls;
global using Avalonia.Interactivity;
global using Avalonia.Markup.Xaml;

global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;

global using ErrorOr;

global using Mediator;

global using Erdmier.ZooTycoonLauncher.Application.Common.Extensions;
global using Erdmier.ZooTycoonLauncher.Application.Common.Models;
global using Erdmier.ZooTycoonLauncher.Desktop.Composition;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Common.Extensions;

global using JetBrains.Annotations;

global using Microsoft.Extensions.DependencyInjection;
```

- [ ] **Step 2: Create `GeneralTabViewModel.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;

/// <summary>The view model for the General tab inside the ReadyToPlay and CannotPlay states. Skeleton — content lands in the Launch Game and Screen Modes slices.</summary>
public sealed partial class GeneralTabViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The installation whose general information is displayed.</param>
    public GeneralTabViewModel(InstallationSummary installation)
    {
        InstallationName = installation.Name;
        InstallationPath = installation.Path;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public GeneralTabViewModel() : this(new InstallationSummary(Guid.Empty,
                                                                 "Designer Installation",
                                                                 @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
                                                                 InstallationValidity.Valid,
                                                                 IsDefault: true,
                                                                 DateTime.UtcNow,
                                                                 null, null, null)) { }

    /// <summary>The installation's user-visible name.</summary>
    public string InstallationName { get; }

    /// <summary>The installation's directory path.</summary>
    public string InstallationPath { get; }
}
```

- [ ] **Step 3: Create `IniConfigTabViewModel.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;

/// <summary>The view model for the INI Config tab. Skeleton — content lands in the INI Config slice.</summary>
public sealed partial class IniConfigTabViewModel : ViewModelBase;
```

- [ ] **Step 4: Create `ScenariosTabViewModel.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;

/// <summary>The view model for the Scenarios tab. Skeleton — content lands in the INI Config slice.</summary>
public sealed partial class ScenariosTabViewModel : ViewModelBase;
```

- [ ] **Step 5: Create `GeneralTabView.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Tabs.GeneralTabView"
             x:DataType="vm:GeneralTabViewModel">
    <StackPanel Margin="8"
                Spacing="4">
        <TextBlock Text="{Binding InstallationName}"
                   FontWeight="Bold" />
        <TextBlock Text="{Binding InstallationPath}" />
        <Button Content="Launch Game"
                IsEnabled="False"
                Margin="0,8,0,0" />
        <TextBlock Text="Screen modes — coming soon"
                   Margin="0,8,0,0" />
        <TextBlock Text="Last played — coming soon" />
    </StackPanel>
</UserControl>
```

- [ ] **Step 6: Create `GeneralTabView.axaml.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Tabs;

/// <summary>View for the General tab.</summary>
public sealed partial class GeneralTabView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public GeneralTabView() => AvaloniaXamlLoader.Load(this);
}
```

- [ ] **Step 7: Create `IniConfigTabView.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Tabs.IniConfigTabView"
             x:DataType="vm:IniConfigTabViewModel">
    <TextBlock Text="INI configuration — coming soon"
               HorizontalAlignment="Center"
               VerticalAlignment="Center" />
</UserControl>
```

- [ ] **Step 8: Create `IniConfigTabView.axaml.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Tabs;

/// <summary>View for the INI Config tab.</summary>
public sealed partial class IniConfigTabView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public IniConfigTabView() => AvaloniaXamlLoader.Load(this);
}
```

- [ ] **Step 9: Create `ScenariosTabView.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Tabs.ScenariosTabView"
             x:DataType="vm:ScenariosTabViewModel">
    <TextBlock Text="Scenarios — coming soon"
               HorizontalAlignment="Center"
               VerticalAlignment="Center" />
</UserControl>
```

- [ ] **Step 10: Create `ScenariosTabView.axaml.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Tabs;

/// <summary>View for the Scenarios tab.</summary>
public sealed partial class ScenariosTabView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public ScenariosTabView() => AvaloniaXamlLoader.Load(this);
}
```

- [ ] **Step 11: Build**

```
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: clean build. If GlobalUsings introduces ambiguities, resolve them before continuing.

- [ ] **Step 12: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs
git add Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/
git commit -m "feat(✨): add tab ViewModels and views skeleton"
```

---

## Task 5: Boot State ViewModels

**Files:**

- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/LookingForZooTycoonViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/ReadyToPlayViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/CannotPlayViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/NoGameInstallationFoundViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/OpenGameInstallationViewModel.cs`

- [ ] **Step 1: Create `LookingForZooTycoonViewModel.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>The view model for the transient "looking for Zoo Tycoon" state shown while <c>BootCommand</c> is in flight.</summary>
public sealed partial class LookingForZooTycoonViewModel : ViewModelBase;
```

- [ ] **Step 2: Create `ReadyToPlayViewModel.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>The view model for the ReadyToPlay state — the active installation is valid and the game can be launched.</summary>
public sealed partial class ReadyToPlayViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The resolved active installation.</param>
    public ReadyToPlayViewModel(InstallationSummary installation)
    {
        InstallationName = installation.Name;
        InstallationPath = installation.Path;
        IsDefault        = installation.IsDefault;
        GeneralTab       = new GeneralTabViewModel(installation);
        IniConfigTab     = new IniConfigTabViewModel();
        ScenariosTab     = new ScenariosTabViewModel();
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public ReadyToPlayViewModel() : this(new InstallationSummary(Guid.Empty,
                                                                  "Designer Installation",
                                                                  @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
                                                                  InstallationValidity.Valid,
                                                                  IsDefault: true,
                                                                  DateTime.UtcNow,
                                                                  null, null, null)) { }

    /// <summary>The installation's user-visible name.</summary>
    public string InstallationName { get; }

    /// <summary>The installation's directory path.</summary>
    public string InstallationPath { get; }

    /// <summary><see langword="true" /> when this is the default installation.</summary>
    public bool IsDefault { get; }

    /// <summary>General tab view model.</summary>
    public GeneralTabViewModel GeneralTab { get; }

    /// <summary>INI Config tab view model.</summary>
    public IniConfigTabViewModel IniConfigTab { get; }

    /// <summary>Scenarios tab view model.</summary>
    public ScenariosTabViewModel ScenariosTab { get; }
}
```

- [ ] **Step 3: Create `CannotPlayViewModel.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>The view model for the CannotPlay state — the active installation is invalid or synchronisation failed.</summary>
public sealed partial class CannotPlayViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The resolved active installation (invalid).</param>
    public CannotPlayViewModel(InstallationSummary installation)
    {
        InstallationName = installation.Name;
        InstallationPath = installation.Path;
        IsDefault        = installation.IsDefault;
        GeneralTab       = new GeneralTabViewModel(installation);
        IniConfigTab     = new IniConfigTabViewModel();
        ScenariosTab     = new ScenariosTabViewModel();
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public CannotPlayViewModel() : this(new InstallationSummary(Guid.Empty,
                                                                 "Designer Installation",
                                                                 @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
                                                                 InstallationValidity.InvalidNoExe,
                                                                 IsDefault: false,
                                                                 DateTime.UtcNow,
                                                                 null, null, null)) { }

    /// <summary>The installation's user-visible name.</summary>
    public string InstallationName { get; }

    /// <summary>The installation's directory path.</summary>
    public string InstallationPath { get; }

    /// <summary><see langword="true" /> when this is the default installation.</summary>
    public bool IsDefault { get; }

    /// <summary>General tab view model.</summary>
    public GeneralTabViewModel GeneralTab { get; }

    /// <summary>INI Config tab view model.</summary>
    public IniConfigTabViewModel IniConfigTab { get; }

    /// <summary>Scenarios tab view model.</summary>
    public ScenariosTabViewModel ScenariosTab { get; }
}
```

- [ ] **Step 4: Create `NoGameInstallationFoundViewModel.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>The view model for the NoGameInstallationFound state. Optionally surfaces a candidate path the locator found but could not add because the dialogue is deferred.</summary>
public sealed partial class NoGameInstallationFoundViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="locatedCandidatePath">
    ///     The path discovered by <c>IInstallationLocator</c>, or <see langword="null" /> when nothing was found or the state was reached without running the locator.
    /// </param>
    public NoGameInstallationFoundViewModel(string? locatedCandidatePath)
        => LocatedCandidatePath = locatedCandidatePath;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public NoGameInstallationFoundViewModel() : this(locatedCandidatePath: @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon") { }

    /// <summary>Path discovered by the auto-locate scan, or <see langword="null" />.</summary>
    public string? LocatedCandidatePath { get; }

    /// <summary><see langword="true" /> when a candidate path was discovered and can be surfaced to the user.</summary>
    public bool HasLocatedPath => LocatedCandidatePath is not null;
}
```

- [ ] **Step 5: Create `OpenGameInstallationViewModel.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>The view model for the OpenGameInstallation state — shown when the startup preference is <c>NoInstallation</c>.</summary>
public sealed partial class OpenGameInstallationViewModel : ViewModelBase;
```

- [ ] **Step 6: Build**

```
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

- [ ] **Step 7: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/
git commit -m "feat(✨): add Boot state ViewModels"
```

---

## Task 6: Boot State Views

**Files:**

- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/LookingForZooTycoonView.axaml` + `.axaml.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/ReadyToPlayView.axaml` + `.axaml.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/CannotPlayView.axaml` + `.axaml.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/NoGameInstallationFoundView.axaml` + `.axaml.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/OpenGameInstallationView.axaml` + `.axaml.cs`

- [ ] **Step 1: Create `LookingForZooTycoonView.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Boot.LookingForZooTycoonView"
             x:DataType="vm:LookingForZooTycoonViewModel">
    <TextBlock Text="Looking for Zoo Tycoon…"
               HorizontalAlignment="Center"
               VerticalAlignment="Center"
               FontSize="14" />
</UserControl>
```

- [ ] **Step 2: Create `LookingForZooTycoonView.axaml.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Boot;

/// <summary>Transient view shown while <c>BootCommand</c> is in flight.</summary>
public sealed partial class LookingForZooTycoonView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public LookingForZooTycoonView() => AvaloniaXamlLoader.Load(this);
}
```

- [ ] **Step 3: Create `ReadyToPlayView.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Boot.ReadyToPlayView"
             x:DataType="vm:ReadyToPlayViewModel">
    <DockPanel>
        <StackPanel DockPanel.Dock="Top"
                    Margin="8,8,8,4">
            <TextBlock Text="{Binding InstallationName}"
                       FontWeight="Bold" />
            <TextBlock Text="{Binding InstallationPath}" />
        </StackPanel>
        <TabControl>
            <TabItem Header="General">
                <ContentControl Content="{Binding GeneralTab}" />
            </TabItem>
            <TabItem Header="INI Config">
                <ContentControl Content="{Binding IniConfigTab}" />
            </TabItem>
            <TabItem Header="Scenarios">
                <ContentControl Content="{Binding ScenariosTab}" />
            </TabItem>
        </TabControl>
    </DockPanel>
</UserControl>
```

- [ ] **Step 4: Create `ReadyToPlayView.axaml.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Boot;

/// <summary>View for the ReadyToPlay state.</summary>
public sealed partial class ReadyToPlayView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public ReadyToPlayView() => AvaloniaXamlLoader.Load(this);
}
```

- [ ] **Step 5: Create `CannotPlayView.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Boot.CannotPlayView"
             x:DataType="vm:CannotPlayViewModel">
    <DockPanel>
        <StackPanel DockPanel.Dock="Top"
                    Margin="8,8,8,4">
            <TextBlock Text="{Binding InstallationName}"
                       FontWeight="Bold" />
            <TextBlock Text="{Binding InstallationPath}" />
            <TextBlock Text="This installation is invalid and cannot be launched."
                       Foreground="Red"
                       Margin="0,4,0,0" />
        </StackPanel>
        <TabControl>
            <TabItem Header="General">
                <ContentControl Content="{Binding GeneralTab}" />
            </TabItem>
            <TabItem Header="INI Config">
                <ContentControl Content="{Binding IniConfigTab}" />
            </TabItem>
            <TabItem Header="Scenarios">
                <ContentControl Content="{Binding ScenariosTab}" />
            </TabItem>
        </TabControl>
    </DockPanel>
</UserControl>
```

- [ ] **Step 6: Create `CannotPlayView.axaml.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Boot;

/// <summary>View for the CannotPlay state.</summary>
public sealed partial class CannotPlayView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public CannotPlayView() => AvaloniaXamlLoader.Load(this);
}
```

- [ ] **Step 7: Create `NoGameInstallationFoundView.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Boot.NoGameInstallationFoundView"
             x:DataType="vm:NoGameInstallationFoundViewModel">
    <StackPanel HorizontalAlignment="Center"
                VerticalAlignment="Center"
                Spacing="8"
                Margin="16">
        <TextBlock Text="No Zoo Tycoon installation was found."
                   FontWeight="Bold" />
        <StackPanel IsVisible="{Binding HasLocatedPath}"
                    Spacing="4">
            <TextBlock Text="A candidate was found at:" />
            <TextBox Text="{Binding LocatedCandidatePath}"
                     IsReadOnly="True" />
        </StackPanel>
        <Button Content="Add Installation"
                IsEnabled="False"
                ToolTip.Tip="Coming in a future update." />
    </StackPanel>
</UserControl>
```

- [ ] **Step 8: Create `NoGameInstallationFoundView.axaml.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Boot;

/// <summary>View for the NoGameInstallationFound state.</summary>
public sealed partial class NoGameInstallationFoundView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public NoGameInstallationFoundView() => AvaloniaXamlLoader.Load(this);
}
```

- [ ] **Step 9: Create `OpenGameInstallationView.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Boot.OpenGameInstallationView"
             x:DataType="vm:OpenGameInstallationViewModel">
    <StackPanel HorizontalAlignment="Center"
                VerticalAlignment="Center"
                Spacing="8"
                Margin="16">
        <TextBlock Text="No installation is selected. Use the Installation Manager to add one." />
        <Button Content="Add / Pick Installation"
                IsEnabled="False"
                ToolTip.Tip="Coming in a future update." />
    </StackPanel>
</UserControl>
```

- [ ] **Step 10: Create `OpenGameInstallationView.axaml.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Boot;

/// <summary>View for the OpenGameInstallation state.</summary>
public sealed partial class OpenGameInstallationView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public OpenGameInstallationView() => AvaloniaXamlLoader.Load(this);
}
```

- [ ] **Step 11: Build**

```
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

- [ ] **Step 12: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/
git commit -m "feat(✨): add Boot state views"
```

---

## Task 7: MainWindowViewModel + MainWindow wiring

**Files:**

- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml.cs`

- [ ] **Step 1: Replace `MainWindowViewModel.cs`**

The local alias `AppBoot` avoids the naming clash between the generated `BootCommand` property (IAsyncRelayCommand, produced by `[RelayCommand]` on `BootAsync`) and the
`BootCommand` record in `Application.Boot`.

```csharp
using AppBoot = Erdmier.ZooTycoonLauncher.Application.Boot;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels;

/// <summary>The main window's view model. Dispatches <c>BootCommand</c> on load and routes the result to the active state view model via <c>ActiveContent</c> (SDD §9.2).</summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    public MainWindowViewModel(IMediator mediator) => _mediator = mediator;

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

    private static ViewModelBase RouteResult(AppBoot.BootResult result) => result.Outcome switch
    {
        AppBoot.BootOutcome.ReadyToPlay             => new ReadyToPlayViewModel(result.ActiveInstallation!),
        AppBoot.BootOutcome.CannotPlay              => new CannotPlayViewModel(result.ActiveInstallation!),
        AppBoot.BootOutcome.NoGameInstallationFound => new NoGameInstallationFoundViewModel(result.LocatedCandidatePath),
        AppBoot.BootOutcome.OpenGameInstallation    => new OpenGameInstallationViewModel(),
        _                                           => new NoGameInstallationFoundViewModel(locatedCandidatePath: null)
    };
}
```

- [ ] **Step 2: Replace `MainWindow.axaml`**

The `<Grid>` placeholder is removed; `ContentControl` is now the sole child.

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels"
        x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="Zoo Tycoon Launcher"
        Width="800"
        Height="600">
    <ContentControl Content="{Binding ActiveContent}" />
</Window>
```

- [ ] **Step 3: Replace `MainWindow.axaml.cs`**

`OnLoaded` fires `BootCommand` immediately after the window is displayed; the `if` guard is a belt-and-suspenders check that the DataContext was set correctly by `App.axaml.cs`.

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views;

/// <summary>The application's main window — chrome only. Hosts the active state view via a <see cref="ContentControl" /> resolved by the <see cref="Composition.ViewLocator" />.</summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Initialises a new instance.</summary>
    public MainWindow() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is MainWindowViewModel vm)
        {
            vm.BootCommand.Execute(null);
        }
    }
}
```

- [ ] **Step 4: Build**

```
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: clean build. If there are CS8602 nullable warnings on `result.ActiveInstallation!`, the `!` operator is correct — `ActiveInstallation` is non-null whenever `ReadyToPlay`
or `CannotPlay` is returned by the handler.

- [ ] **Step 5: Run all tests**

```
dotnet test Erdmier.ZooTycoonLauncher.slnx
```

Expected: all tests pass (the 11 BootHandlerTests and all existing tests).

- [ ] **Step 6: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml.cs
git commit -m "feat(✨): wire BootCommand into MainWindowViewModel and MainWindow"
```

---

## Self-Review Checklist

- **Spec coverage:**
    - ✅ `BootCommand` + `BootResult` + `BootHandler` — Task 2 + 3
    - ✅ `IIniSnapshotService.SynchroniseAsync` stub — Task 1
    - ✅ `MainWindowViewModel` dispatches BootCommand from OnLoaded — Task 7
    - ✅ Five state VM and View pairs — Tasks 5 + 6
    - ✅ Three tab VM and View pairs — Task 4
    - ✅ Designer constructors on VMs that take parameters — Tasks 4 + 5
    - ✅ All 11 `BootHandlerTests` — Task 3
    - ✅ `LocatedCandidatePath` surfaced in `NoGameInstallationFoundView` — Task 6

- **Type consistency across tasks:**
    - `BootCommand`, `BootResult`, `BootOutcome` defined in Task 2; used by `AppBoot.*` alias in Task 7
    - `GeneralTabViewModel(InstallationSummary)` defined in Task 4; used as `new GeneralTabViewModel(installation)` in Tasks 5
    - `InstallationSummary` — available via `global using Erdmier.ZooTycoonLauncher.Application.Common.Models` added in Task 4

- **No placeholders** — all steps contain complete code.
