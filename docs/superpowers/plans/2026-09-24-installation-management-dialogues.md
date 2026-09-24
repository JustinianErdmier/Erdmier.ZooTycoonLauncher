# Installation Management Dialogues Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps
> use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Installation Manager's Info, Edit, Delete and Fix buttons real, switch on handler-published change messages so the grids refresh themselves, refresh the main
window after the manager changes the open installation, and pre-fill the Add dialogue for the first installation.

**Architecture:** Application handlers publish the four existing message records through a new `IApplicationEventPublisher`, implemented in Desktop by the CommunityToolkit
messenger (UI thread). Each dialogue is a small `ClassicWindow` view + view-model pair; Add and Edit share an extracted `InstallationFormView` component. The dialogue service
gains four methods; the main window re-runs a pointed boot of the open installation when the manager reports a change.

**Tech Stack:** .NET 10 / C# 13, Avalonia 11.3 + Classic.Avalonia, CommunityToolkit.Mvvm (ObservableProperty, RelayCommand, IMessenger), Mediator (source-generated), ErrorOr,
FluentValidation, EF Core + SQLite; tests: xUnit, NSubstitute, Shouldly, `Microsoft.Extensions.Time.Testing`.

**Spec:** `docs/superpowers/specs/2026-09-24-installation-management-dialogues-design.md` (decisions D1–D7 are binding). The SDD
(`docs/superpowers/specs/2026-05-26-zoo-tycoon-launcher-design.md`) wins where anything disagrees.

## Global Constraints

- Windows-only; .NET 10, C# 13; Avalonia 11.3 + Classic.Avalonia; British English in identifiers, comments, copy and commit messages (external API spellings unchanged).
- One type per file; file-scoped namespaces; no files at a project root; folders mirror namespaces.
- XML doc comments on every public type and member; `<c>…</c>` with no inner whitespace; spaced attribute brackets (`[ Fact ]`, `[ RelayCommand ]`).
- `GlobalUsings.cs` per assembly; a local `using` only for a namespace conflict, an alias, or an uncommon namespace used by one file.
- Vertical spacing: a blank line between consecutive statements of different kinds; align `=` in consecutive assignments and multi-line parameter lists (existing style).
- `[ ObservableProperty ]` on `partial` properties; `[ RelayCommand ]` on private methods; compiled bindings — every XAML file declares `x:DataType`.
- Every public `*ViewModel` in Desktop has a sibling `*View.axaml` in the parallel `Views/…` folder, and vice versa. `MainWindow.axaml` stays ≤ 100 lines.
- Designer constructors delegate to the real constructor passing `null!` for services (and `NullLogger<T>.Instance` for loggers); commands null-check services.
- Do NOT add `[ UsedImplicitly ]`.
- D1: Fix repairs the EXE only; the INI group box shows status and its **Create** button is always disabled.
- D2: Edit shows the folder read-only with **no Browse button**.
- D3: handlers publish change messages via `IApplicationEventPublisher`; publishing happens only after changes are persisted and never throws into a handler.
- D4: Info's History entries shows `—`.
- D5: Add with zero installations pre-fills Name with `Main` (editable) and ticks + locks Mark as default.
- D6: after the manager reports a change while an installation is open, the main window re-runs a pointed boot of that installation.
- D7: Relocate rejects a folder without `zoo.exe` with `Installation.ExeMissing`, leaving the row untouched.
- Timestamps display as local time, en-GB, `d MMM yyyy HH:mm`; `—` when null.
- Muted text uses `Opacity="0.6"` (existing idiom).
- Commit format: `type(gitmoji): subject` — `feat(✨)`, `fix(🐛)`, `refactor(♻️)`, `test(✅)`, `docs(📝)`; each message ends with the implementing session's
  `Co-Authored-By:` trailer.
- Build: `dotnet build Erdmier.ZooTycoonLauncher.slnx -nologo -v q` → 0 errors, 0 warnings. Tests: `dotnet test Tests/<Project> -nologo`. There is no Desktop test project; do not
  create one; never `dotnet run` the app.

## Review Focus

- **An installation removed between the grid loading and a dialogue opening or saving** — the dialogue shows "This installation no longer exists." and only Close / Cancel
  works; nothing throws. Pinned by: Task 2's `Handle_ReturnsNotFound_WhenInstallationMissing` (preview) plus the existing NotFound handler tests; Tasks 5–8 map
  `ErrorType.NotFound` to `InstallationDialogMessages.InstallationMissing` (manual check).
- **Deleting the default when others remain** — the confirmation names exactly the installation the delete promotes, including mixed-case names. Pinned by: Task 2's
  `FindDefaultPromotionCandidateAsync_WithExcludeId_MatchesPostDeleteCandidate` integration test.
- **Deleting the last installation** — "No installations will remain." and `DefaultInstallationChangedMessage(null)`. Pinned by: Task 1's
  `Handle_PublishesDefaultChangedWithNull_WhenLastInstallationRemoved` and Task 2's `Handle_ReturnsNoPromotion_WhenLastInstallation`.
- **Fix → Locate choosing a folder without `zoo.exe` (including the same folder)** — row untouched, inline error, nothing published. Pinned by: Task 2's
  `Handle_ReturnsExeMissing_AndLeavesRowUntouched_WhenFolderHasNoExe`.
- **Editing, fixing or deleting the installation that is currently open** (manager opened from Play) — the main window re-verifies it: a rename shows, Cannot Play becomes
  Ready, a deleted one falls back to normal resolution. Pinned by: the existing pointed-boot fall-back tests in `BootHandlerTests`; Task 9's manual check.

---

## File Map

| Status | Path | Task |
|---|---|---|
| Create | `Source/…Application/Common/Abstractions/IApplicationEventPublisher.cs` | 1 |
| Create | `Source/…Desktop/Composition/MessengerEventPublisher.cs` | 1 |
| Modify | `Source/…Application/Installations/{Add,Update,Delete,Relocate,SetDefault,Verify}/*Handler.cs` | 1 |
| Modify | `Tests/…Application.Tests.Unit/Installations/*HandlerTests.cs`, `Tests/…Application.Tests.Unit/GlobalUsings.cs` | 1 |
| Modify | `Source/…Desktop/Composition/DesktopServiceCollectionExtensions.cs`, `Source/…Desktop/GlobalUsings.cs` | 1, 5–8 |
| Modify | `Source/…Application/Common/Abstractions/IInstallationRepository.cs`, `Source/…Infrastructure/Persistence/Launcher/Repositories/InstallationRepository.cs` | 2 |
| Create | `Source/…Application/Installations/Delete/{PreviewInstallationDeletionQuery,PreviewInstallationDeletionHandler,InstallationDeletionPreview}.cs` | 2 |
| Modify | `Source/…Application/Boot/BootHandler.cs`, `Source/…Application/Installations/Relocate/RelocateInstallationHandler.cs` | 2 |
| Create | `Tests/…Application.Tests.Unit/Installations/PreviewInstallationDeletionHandlerTests.cs` | 2 |
| Modify | `Tests/…Infrastructure.Tests.Integration/Persistence/InstallationRepositoryTests.cs`, `Tests/…Application.Tests.Unit/Boot/BootHandlerTests.cs` | 2 |
| Modify | `Source/…Desktop/ViewModels/Common/InstallationGridViewModel.cs`, `…/ViewModels/Boot/OpenGameInstallationViewModel.cs`, `…/ViewModels/Dialogs/InstallationManagerDialogViewModel.cs`, `…/ViewModels/MainWindowViewModel.cs` | 3 |
| Create | `Source/…Desktop/ViewModels/Common/InstallationFormViewModel.cs`, `Source/…Desktop/Views/Common/InstallationFormView.axaml(.cs)` | 4 |
| Modify | `Source/…Desktop/ViewModels/Dialogs/AddInstallationDialogViewModel.cs`, `Source/…Desktop/Views/Dialogs/AddInstallationDialogView.axaml`, `…/Composition/AvaloniaDialogService.cs` | 4 |
| Modify | `docs/superpowers/specs/2026-05-26-zoo-tycoon-launcher-design.md` (§7.2.1 + revision 1.6) | 4 |
| Create | `Source/…Desktop/ViewModels/Dialogs/InstallationDialogMessages.cs` | 5 |
| Create | `Source/…Desktop/ViewModels/Dialogs/EditInstallationDialogViewModel.cs`, `Source/…Desktop/Views/Dialogs/EditInstallationDialogView.axaml(.cs)` | 5 |
| Create | `Source/…Desktop/ViewModels/Dialogs/InstallationInfoDialogViewModel.cs`, `Source/…Desktop/Views/Dialogs/InstallationInfoDialogView.axaml(.cs)` | 6 |
| Create | `Source/…Desktop/ViewModels/Dialogs/DeleteInstallationDialogViewModel.cs`, `Source/…Desktop/Views/Dialogs/DeleteInstallationDialogView.axaml(.cs)` | 7 |
| Create | `Source/…Desktop/ViewModels/Dialogs/FixInstallationDialogViewModel.cs`, `Source/…Desktop/Views/Dialogs/FixInstallationDialogView.axaml(.cs)` | 8 |
| Modify | `Source/…Desktop/Composition/IDialogService.cs`, `…/Composition/AvaloniaDialogService.cs`, `…/ViewModels/Boot/PlayViewModel.cs` (NoOp) | 5–8 |
| Modify | `Source/…Desktop/ViewModels/Dialogs/InstallationManagerDialogViewModel.cs` | 5–8 |
| Modify | `Source/…Desktop/ViewModels/Boot/PlayViewModel.cs`, `…/ViewModels/Tabs/GeneralTabViewModel.cs`, `…/Views/Tabs/GeneralTabView.axaml`, `…/ViewModels/MainWindowViewModel.cs` | 9 |

(`…Application` = `Erdmier.ZooTycoonLauncher.Application`, likewise for the other projects.)

---

## Task 1: Handlers publish change messages

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IApplicationEventPublisher.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/MessengerEventPublisher.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Add/AddInstallationHandler.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Update/UpdateInstallationHandler.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Delete/DeleteInstallationHandler.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Relocate/RelocateInstallationHandler.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/SetDefault/SetDefaultInstallationHandler.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Verify/VerifyInstallationHandler.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/{Add,Update,Delete,Relocate,SetDefault,Verify}InstallationHandlerTests.cs`

**Interfaces:**
- Produces: `IApplicationEventPublisher.Publish<TMessage>(TMessage message) where TMessage : class` (namespace `Erdmier.ZooTycoonLauncher.Application.Common.Abstractions`).
- Produces: every handler below takes `IApplicationEventPublisher events` as its **last** constructor parameter.
- Consumes: the existing records `InstallationAddedMessage(Guid InstallationId)`, `InstallationChangedMessage(Guid InstallationId)`,
  `InstallationDeletedMessage(Guid InstallationId)`, `DefaultInstallationChangedMessage(Guid? NewDefaultInstallationId)` (namespace
  `Erdmier.ZooTycoonLauncher.Application.Common.Messaging`).

- [ ] **Step 1: Create the abstraction**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IApplicationEventPublisher.cs
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>
///     Publishes installation-change notifications (the records in <c>Application.Common.Messaging</c>) to whichever layer composes the application. The Desktop
///     implementation forwards them to the CommunityToolkit messenger so the installation grids and the main window refresh (SDD §7.2.1–§7.2.4).
/// </summary>
public interface IApplicationEventPublisher
{
    /// <summary>Publishes <paramref name="message" /> to every current subscriber. Fire-and-forget: implementations must not throw into the caller.</summary>
    /// <param name="message">The message to publish.</param>
    /// <typeparam name="TMessage">The message type; subscribers register per concrete type.</typeparam>
    void Publish<TMessage>(TMessage message)
        where TMessage : class;
}
```

- [ ] **Step 2: Add the messaging namespace to the Application test global usings**

In `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs`, add after `global using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;`:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Common.Messaging;
```

- [ ] **Step 3: Write the failing publish tests**

Append these tests to the named existing test classes (each class already has the usings it needs via GlobalUsings).

`AddInstallationHandlerTests`:

```csharp
    [ Fact ]
    public async Task Handle_PublishesAddedAndDefaultChanged_WhenFirstInstallation()
    {
        AddInstallationCommand command = new(Name: "Main", Path: @"C:\Games\Main", MakeDefault: false);

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns(Array.Empty<GameInstallation>());

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings());

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();

        dbFactory.CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns(Substitute.For<IInstallationDbContextHandle>());

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.CaptureOriginalAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        AddInstallationHandler handler = new(installations, settings, verifier, dbFactory, snapshots, TimeProvider.System, events);

        ErrorOr<AddInstallationResult> result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new InstallationAddedMessage(result.Value.InstallationId));

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new DefaultInstallationChangedMessage(result.Value.InstallationId));
    }

    [ Fact ]
    public async Task Handle_PublishesOnlyAdded_WhenNotDefault()
    {
        AddInstallationCommand command = new(Name: "Second", Path: @"C:\Games\Second", MakeDefault: false);

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns([
                         new GameInstallation
                         {
                             Id       = Guid.CreateVersion7(),
                             Name     = "Main",
                             Path     = @"C:\Games\Main",
                             AddedUtc = DateTime.UtcNow
                         }
                     ]);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();

        dbFactory.CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns(Substitute.For<IInstallationDbContextHandle>());

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.CaptureOriginalAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        AddInstallationHandler handler = new(installations,
                                             Substitute.For<ILauncherSettingsRepository>(),
                                             verifier,
                                             dbFactory,
                                             snapshots,
                                             TimeProvider.System,
                                             events);

        ErrorOr<AddInstallationResult> result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new InstallationAddedMessage(result.Value.InstallationId));

        events.DidNotReceive()
              .Publish(Arg.Any<DefaultInstallationChangedMessage>());
    }
```

`UpdateInstallationHandlerTests`:

```csharp
    [ Fact ]
    public async Task Handle_PublishesChangedAndDefaultChanged_WhenMadeDefault()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Main",
                         Path     = @"C:\Games\Main",
                         AddedUtc = DateTime.UtcNow
                     });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = Guid.CreateVersion7()
                });

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        UpdateInstallationHandler handler = new(installations, settings, TimeProvider.System, events);

        ErrorOr<Success> result = await handler.Handle(new UpdateInstallationCommand(id, Name: "Renamed", MakeDefault: true), CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new InstallationChangedMessage(id));

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new DefaultInstallationChangedMessage(id));
    }

    [ Fact ]
    public async Task Handle_PublishesOnlyChanged_WhenAlreadyDefault()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Main",
                         Path     = @"C:\Games\Main",
                         AddedUtc = DateTime.UtcNow
                     });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = id
                });

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        UpdateInstallationHandler handler = new(installations, settings, TimeProvider.System, events);

        ErrorOr<Success> result = await handler.Handle(new UpdateInstallationCommand(id, Name: "Renamed", MakeDefault: true), CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new InstallationChangedMessage(id));

        events.DidNotReceive()
              .Publish(Arg.Any<DefaultInstallationChangedMessage>());
    }
```

`DeleteInstallationHandlerTests` (Task 2 later changes the `FindDefaultPromotionCandidateAsync` mock signature in these tests):

```csharp
    [ Fact ]
    public async Task Handle_PublishesDeletedAndDefaultChanged_WhenDefaultRemoved()
    {
        Guid removedId  = Guid.CreateVersion7();
        Guid promotedId = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(removedId, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = removedId,
                         Name     = "Removed",
                         Path     = @"C:\Games\Removed",
                         AddedUtc = DateTime.UtcNow
                     });

        installations.FindDefaultPromotionCandidateAsync(Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = promotedId,
                         Name     = "Promoted",
                         Path     = @"C:\Games\Promoted",
                         AddedUtc = DateTime.UtcNow
                     });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = removedId
                });

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        DeleteInstallationHandler handler = new(installations, settings, Substitute.For<IInstallationDbContextFactory>(), events);

        ErrorOr<DeleteInstallationResult> result = await handler.Handle(new DeleteInstallationCommand(removedId), CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new InstallationDeletedMessage(removedId));

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new DefaultInstallationChangedMessage(promotedId));
    }

    [ Fact ]
    public async Task Handle_PublishesDefaultChangedWithNull_WhenLastInstallationRemoved()
    {
        Guid removedId = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(removedId, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = removedId,
                         Name     = "Last",
                         Path     = @"C:\Games\Last",
                         AddedUtc = DateTime.UtcNow
                     });

        installations.FindDefaultPromotionCandidateAsync(Arg.Any<CancellationToken>())
                     .Returns((GameInstallation?)null);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = removedId
                });

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        DeleteInstallationHandler handler = new(installations, settings, Substitute.For<IInstallationDbContextFactory>(), events);

        ErrorOr<DeleteInstallationResult> result = await handler.Handle(new DeleteInstallationCommand(removedId), CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new InstallationDeletedMessage(removedId));

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new DefaultInstallationChangedMessage(NewDefaultInstallationId: null));
    }

    [ Fact ]
    public async Task Handle_PublishesOnlyDeleted_WhenNonDefaultRemoved()
    {
        Guid removedId = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(removedId, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = removedId,
                         Name     = "Spare",
                         Path     = @"C:\Games\Spare",
                         AddedUtc = DateTime.UtcNow
                     });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = Guid.CreateVersion7()
                });

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        DeleteInstallationHandler handler = new(installations, settings, Substitute.For<IInstallationDbContextFactory>(), events);

        ErrorOr<DeleteInstallationResult> result = await handler.Handle(new DeleteInstallationCommand(removedId), CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new InstallationDeletedMessage(removedId));

        events.DidNotReceive()
              .Publish(Arg.Any<DefaultInstallationChangedMessage>());
    }
```

`RelocateInstallationHandlerTests`:

```csharp
    [ Fact ]
    public async Task Handle_PublishesChanged_WhenRelocated()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Main",
                         Path     = @"C:\Games\Old",
                         AddedUtc = DateTime.UtcNow
                     });

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(path: @"C:\Games\New", Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        RelocateInstallationHandler handler = new(installations, verifier, TimeProvider.System, events);

        ErrorOr<RelocateInstallationResult> result = await handler.Handle(new RelocateInstallationCommand(id, NewPath: @"C:\Games\New"), CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new InstallationChangedMessage(id));
    }
```

`SetDefaultInstallationHandlerTests`:

```csharp
    [ Fact ]
    public async Task Handle_PublishesDefaultChanged_WhenDefaultMoves()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Main",
                         Path     = @"C:\Games\Main",
                         AddedUtc = DateTime.UtcNow
                     });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = Guid.CreateVersion7()
                });

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        SetDefaultInstallationHandler handler = new(installations, settings, events);

        ErrorOr<Success> result = await handler.Handle(new SetDefaultInstallationCommand(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new DefaultInstallationChangedMessage(id));
    }

    [ Fact ]
    public async Task Handle_DoesNotPublish_WhenAlreadyDefault()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Main",
                         Path     = @"C:\Games\Main",
                         AddedUtc = DateTime.UtcNow
                     });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = id
                });

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        SetDefaultInstallationHandler handler = new(installations, settings, events);

        ErrorOr<Success> result = await handler.Handle(new SetDefaultInstallationCommand(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.DidNotReceive()
              .Publish(Arg.Any<DefaultInstallationChangedMessage>());
    }
```

`VerifyInstallationHandlerTests`:

```csharp
    [ Fact ]
    public async Task Handle_PublishesChanged_WhenFlagsDrift()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Main",
                         Path     = @"C:\Games\Main",
                         HasExe   = true,
                         HasIni   = true,
                         AddedUtc = DateTime.UtcNow
                     });

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(path: @"C:\Games\Main", Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: false, HasIni: true));

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        VerifyInstallationHandler handler = new(installations, verifier, TimeProvider.System, events);

        ErrorOr<VerificationResult> result = await handler.Handle(new VerifyInstallationQuery(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new InstallationChangedMessage(id));
    }

    [ Fact ]
    public async Task Handle_DoesNotPublish_WhenFlagsUnchanged()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Main",
                         Path     = @"C:\Games\Main",
                         HasExe   = true,
                         HasIni   = true,
                         AddedUtc = DateTime.UtcNow
                     });

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(path: @"C:\Games\Main", Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        VerifyInstallationHandler handler = new(installations, verifier, TimeProvider.System, events);

        ErrorOr<VerificationResult> result = await handler.Handle(new VerifyInstallationQuery(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.DidNotReceive()
              .Publish(Arg.Any<InstallationChangedMessage>());
    }
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit -nologo`
Expected: FAIL to compile — `CS1729` ("does not contain a constructor that takes N arguments") on every new test.

- [ ] **Step 5: Inject the publisher and publish in each handler**

For each of the six handlers: add a field `private readonly IApplicationEventPublisher _events;` (alphabetically placed among the existing fields, as the files already order
them), append `IApplicationEventPublisher events` as the **last** constructor parameter (aligned with the existing parameter list), assign `_events = events;` (aligned with the
other assignments), and add `/// <param name="events">Publishes installation-change messages after changes are persisted (SDD §7.2).</param>` where the constructor already
documents its parameters. Then publish exactly as follows.

`AddInstallationHandler.Handle` — replace the final `return` with:

```csharp
        _events.Publish(new InstallationAddedMessage(row.Id));

        if (becameDefault)
        {
            _events.Publish(new DefaultInstallationChangedMessage(row.Id));
        }

        return new AddInstallationResult(row.Id, verification.Validity, becameDefault);
```

`UpdateInstallationHandler.Handle` — from the row update to the end:

```csharp
        await _installations.UpdateAsync(row, cancellationToken);

        _events.Publish(new InstallationChangedMessage(row.Id));

        if (command.MakeDefault)
        {
            LauncherSettings settings = await _settings.GetAsync(cancellationToken);

            if (settings.DefaultInstallationId != row.Id)
            {
                settings.DefaultInstallationId = row.Id;

                await _settings.UpdateAsync(settings, cancellationToken);

                _events.Publish(new DefaultInstallationChangedMessage(row.Id));
            }
        }

        return Result.Success;
```

`DeleteInstallationHandler.Handle` — replace the tail after `await _dbFactory.DeleteAsync(row.Id, cancellationToken);`:

```csharp
        await _dbFactory.DeleteAsync(row.Id, cancellationToken);

        _events.Publish(new InstallationDeletedMessage(row.Id));

        if (removedWasDefault)
        {
            _events.Publish(new DefaultInstallationChangedMessage(newDefaultId));
        }

        return new DeleteInstallationResult(removedWasDefault, newDefaultId);
```

`RelocateInstallationHandler.Handle` — after `await _installations.AddAsync(relocated, cancellationToken);`:

```csharp
        await _installations.DeleteAsync(row.Id, cancellationToken);
        await _installations.AddAsync(relocated, cancellationToken);

        _events.Publish(new InstallationChangedMessage(row.Id));

        return new RelocateInstallationResult(verification.Validity);
```

`SetDefaultInstallationHandler.Handle` — the tail:

```csharp
        settings.DefaultInstallationId = row.Id;
        await _settings.UpdateAsync(settings, cancellationToken);

        _events.Publish(new DefaultInstallationChangedMessage(row.Id));

        return Result.Success;
```

`VerifyInstallationHandler.Handle` — inside the drift branch, after `await _installations.UpdateAsync(row, cancellationToken);`:

```csharp
            await _installations.UpdateAsync(row, cancellationToken);

            _events.Publish(new InstallationChangedMessage(row.Id));
```

- [ ] **Step 6: Update the existing handler constructions in the tests**

Every existing construction of these six handlers in `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit` gets `Substitute.For<IApplicationEventPublisher>()` appended as
the last argument (16 sites: `AddInstallationHandlerTests` ×3, `DeleteInstallationHandlerTests` ×3, `RelocateInstallationHandlerTests` ×3, `SetDefaultInstallationHandlerTests` ×2,
`UpdateInstallationHandlerTests` ×3, `VerifyInstallationHandlerTests` ×2). Example:

```csharp
        DeleteInstallationHandler handler = new(installations, settings, dbFactory, Substitute.For<IApplicationEventPublisher>());
```

- [ ] **Step 7: Run the Application tests**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit -nologo`
Expected: PASS — 54 existing + 12 new = 66.

- [ ] **Step 8: Implement and register the Desktop publisher**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/MessengerEventPublisher.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>
///     The CommunityToolkit-messenger implementation of <see cref="IApplicationEventPublisher" />. Sends on the UI thread — immediately when already on it, otherwise
///     posted — so recipients such as <see cref="InstallationGridViewModel" /> never observe a message off the UI thread.
/// </summary>
internal sealed class MessengerEventPublisher : IApplicationEventPublisher
{
    private readonly IMessenger _messenger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="messenger">The application-wide messenger.</param>
    public MessengerEventPublisher(IMessenger messenger) => _messenger = messenger;

    /// <inheritdoc />
    public void Publish<TMessage>(TMessage message)
        where TMessage : class
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _messenger.Send(message);

            return;
        }

        Dispatcher.UIThread.Post(() => _messenger.Send(message));
    }
}
```

In `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`, add (sorted into the Application group, before `…Application.Common.Extensions`):

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
```

In `DesktopServiceCollectionExtensions.AddDesktop`, after the `IMessenger` registration:

```csharp
        services.AddSingleton<IApplicationEventPublisher, MessengerEventPublisher>();
```

- [ ] **Step 9: Build and run the architecture tests**

Run: `dotnet build Erdmier.ZooTycoonLauncher.slnx -nologo -v q` then `dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture -nologo`
Expected: build 0 warnings / 0 errors; architecture tests PASS (13).

- [ ] **Step 10: Commit**

```bash
git add Source/Erdmier.ZooTycoonLauncher.Application Source/Erdmier.ZooTycoonLauncher.Desktop/Composition Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m "feat(✨): publish installation change messages from the Application handlers"
```

---

## Task 2: Deletion preview, promotion exclusion, and Relocate's `zoo.exe` check

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IInstallationRepository.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Repositories/InstallationRepository.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Boot/BootHandler.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Delete/DeleteInstallationHandler.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Relocate/RelocateInstallationHandler.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Delete/PreviewInstallationDeletionQuery.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Delete/InstallationDeletionPreview.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Delete/PreviewInstallationDeletionHandler.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/PreviewInstallationDeletionHandlerTests.cs` (create)
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/RelocateInstallationHandlerTests.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/DeleteInstallationHandlerTests.cs`, `…/Boot/BootHandlerTests.cs` (mock signature)
- Test: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Persistence/InstallationRepositoryTests.cs`

**Interfaces:**
- Produces: `IInstallationRepository.FindDefaultPromotionCandidateAsync(Guid? excludeId, CancellationToken cancellationToken)` (replaces the one-parameter form).
- Produces: `PreviewInstallationDeletionQuery(Guid InstallationId) : IQuery<ErrorOr<InstallationDeletionPreview>>` and
  `InstallationDeletionPreview(string Name, bool IsDefault, string? PromotedName)` (namespace `Erdmier.ZooTycoonLauncher.Application.Installations.Delete`).
- Produces: `RelocateInstallationCommand` returns `Error.Validation("Installation.ExeMissing", …)` when the folder exists but lacks `zoo.exe`.

- [ ] **Step 1: Write the failing tests**

Add to `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Persistence/InstallationRepositoryTests.cs` (uses the class's existing `NewInstallation` helper and
`_repository`):

```csharp
    [ Fact ]
    public async Task FindDefaultPromotionCandidateAsync_WithExcludeId_SkipsExcludedRow()
    {
        GameInstallation antelope = NewInstallation(name: "antelope", path: @"C:\Games\antelope");
        await _repository.AddAsync(antelope, CancellationToken.None);
        await _repository.AddAsync(NewInstallation(name: "Buffalo", path: @"C:\Games\Buffalo"), CancellationToken.None);
        await _repository.AddAsync(NewInstallation(name: "zebra", path: @"C:\Games\zebra"), CancellationToken.None);

        GameInstallation? winner = await _repository.FindDefaultPromotionCandidateAsync(antelope.Id, CancellationToken.None);

        winner.ShouldNotBeNull();
        winner.Name.ShouldBe(expected: "Buffalo");
    }

    [ Fact ]
    public async Task FindDefaultPromotionCandidateAsync_WithExcludeId_MatchesPostDeleteCandidate()
    {
        GameInstallation doomed = NewInstallation(name: "Aardvark", path: @"C:\Games\Aardvark");
        await _repository.AddAsync(doomed, CancellationToken.None);
        await _repository.AddAsync(NewInstallation(name: "bison", path: @"C:\Games\bison"), CancellationToken.None);
        await _repository.AddAsync(NewInstallation(name: "Camel", path: @"C:\Games\Camel"), CancellationToken.None);

        GameInstallation? previewed = await _repository.FindDefaultPromotionCandidateAsync(doomed.Id, CancellationToken.None);

        await _repository.DeleteAsync(doomed.Id, CancellationToken.None);

        GameInstallation? promoted = await _repository.FindDefaultPromotionCandidateAsync(excludeId: null, CancellationToken.None);

        previewed.ShouldNotBeNull();
        promoted.ShouldNotBeNull();
        previewed.Id.ShouldBe(promoted.Id);
    }
```

Create `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/PreviewInstallationDeletionHandlerTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class PreviewInstallationDeletionHandlerTests
{
    [ Fact ]
    public async Task Handle_ReturnsPromotedName_WhenDefaultWithSuccessor()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Main",
                         Path     = @"C:\Games\Main",
                         AddedUtc = DateTime.UtcNow
                     });

        installations.FindDefaultPromotionCandidateAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = Guid.CreateVersion7(),
                         Name     = "Archive",
                         Path     = @"C:\Games\Archive",
                         AddedUtc = DateTime.UtcNow
                     });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = id
                });

        PreviewInstallationDeletionHandler handler = new(installations, settings);

        ErrorOr<InstallationDeletionPreview> result = await handler.Handle(new PreviewInstallationDeletionQuery(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(new InstallationDeletionPreview(Name: "Main", IsDefault: true, PromotedName: "Archive"));
    }

    [ Fact ]
    public async Task Handle_ReturnsNoPromotion_WhenLastInstallation()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Main",
                         Path     = @"C:\Games\Main",
                         AddedUtc = DateTime.UtcNow
                     });

        installations.FindDefaultPromotionCandidateAsync(id, Arg.Any<CancellationToken>())
                     .Returns((GameInstallation?)null);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = id
                });

        PreviewInstallationDeletionHandler handler = new(installations, settings);

        ErrorOr<InstallationDeletionPreview> result = await handler.Handle(new PreviewInstallationDeletionQuery(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(new InstallationDeletionPreview(Name: "Main", IsDefault: true, PromotedName: null));
    }

    [ Fact ]
    public async Task Handle_ReturnsNotDefault_WithoutQueryingPromotion_WhenNotDefault()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Spare",
                         Path     = @"C:\Games\Spare",
                         AddedUtc = DateTime.UtcNow
                     });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = Guid.CreateVersion7()
                });

        PreviewInstallationDeletionHandler handler = new(installations, settings);

        ErrorOr<InstallationDeletionPreview> result = await handler.Handle(new PreviewInstallationDeletionQuery(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(new InstallationDeletionPreview(Name: "Spare", IsDefault: false, PromotedName: null));

        await installations.DidNotReceive()
                           .FindDefaultPromotionCandidateAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_ReturnsNotFound_WhenInstallationMissing()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                     .Returns((GameInstallation?)null);

        PreviewInstallationDeletionHandler handler = new(installations, Substitute.For<ILauncherSettingsRepository>());

        ErrorOr<InstallationDeletionPreview> result = await handler.Handle(new PreviewInstallationDeletionQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }
}
```

Add to `RelocateInstallationHandlerTests`:

```csharp
    [ Fact ]
    public async Task Handle_ReturnsExeMissing_AndLeavesRowUntouched_WhenFolderHasNoExe()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Main",
                         Path     = @"C:\Games\Old",
                         AddedUtc = DateTime.UtcNow
                     });

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(path: @"C:\Games\NoExe", Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: false, HasIni: true));

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        RelocateInstallationHandler handler = new(installations, verifier, TimeProvider.System, events);

        ErrorOr<RelocateInstallationResult> result = await handler.Handle(new RelocateInstallationCommand(id, NewPath: @"C:\Games\NoExe"), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Installation.ExeMissing");

        await installations.DidNotReceive()
                           .DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        await installations.DidNotReceive()
                           .AddAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>());

        events.DidNotReceive()
              .Publish(Arg.Any<InstallationChangedMessage>());
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit -nologo`
Expected: FAIL to compile — `PreviewInstallationDeletionHandler` / `PreviewInstallationDeletionQuery` / `InstallationDeletionPreview` not found, and no
`FindDefaultPromotionCandidateAsync` overload taking a `Guid`.

- [ ] **Step 3: Change the repository method**

In `IInstallationRepository`, replace the `FindDefaultPromotionCandidateAsync` member (keep the summary, adding the parameter):

```csharp
    /// <summary>
    ///     Picks the row that should be promoted to default — the alphabetically-first row (case-insensitive on <see cref="GameInstallation.Name" />) — or
    ///     <see langword="null" /> when no rows remain.
    /// </summary>
    /// <param name="excludeId">
    ///     When supplied, the row with this id is ignored — used to preview the promotion before that row is deleted. Pass <see langword="null" /> after a
    ///     delete has already happened.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<GameInstallation?> FindDefaultPromotionCandidateAsync(Guid? excludeId, CancellationToken cancellationToken);
```

In `InstallationRepository`:

```csharp
    /// <inheritdoc />
    public Task<GameInstallation?> FindDefaultPromotionCandidateAsync(Guid? excludeId, CancellationToken cancellationToken)
        => _context.GameInstallations
                   .Where(i => excludeId == null || i.Id != excludeId)
                   .OrderBy(i => i.Name.ToLower())
                   .FirstOrDefaultAsync(cancellationToken);
```

Update the two production callers to pass `excludeId: null` — `BootHandler.ResolveDefaultAsync`:

```csharp
        GameInstallation? promoted = await _installations.FindDefaultPromotionCandidateAsync(excludeId: null, cancellationToken);
```

and `DeleteInstallationHandler.Handle`:

```csharp
            GameInstallation? promotion = await _installations.FindDefaultPromotionCandidateAsync(excludeId: null, cancellationToken);
```

Update every test reference: in `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit` replace `FindDefaultPromotionCandidateAsync(Arg.Any<CancellationToken>())` with
`FindDefaultPromotionCandidateAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())` (`BootHandlerTests` ×5, `DeleteInstallationHandlerTests` — including the two tests added in
Task 1); in `InstallationRepositoryTests` replace `FindDefaultPromotionCandidateAsync(CancellationToken.None)` with
`FindDefaultPromotionCandidateAsync(excludeId: null, CancellationToken.None)`.

- [ ] **Step 4: Create the preview slice**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Application/Installations/Delete/PreviewInstallationDeletionQuery.cs
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>
///     Describes what deleting an installation would do, without deleting it — used by the Delete Installation confirmation (SDD §7.2.4, §9.5) to name the
///     installation that would be promoted to default.
/// </summary>
/// <param name="InstallationId">The installation the user is about to delete.</param>
public sealed record PreviewInstallationDeletionQuery(Guid InstallationId) : IQuery<ErrorOr<InstallationDeletionPreview>>;
```

```csharp
// Source/Erdmier.ZooTycoonLauncher.Application/Installations/Delete/InstallationDeletionPreview.cs
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>Outcome of a <see cref="PreviewInstallationDeletionQuery" />.</summary>
/// <param name="Name">The name of the installation that would be deleted.</param>
/// <param name="IsDefault"><see langword="true" /> when that installation is the current launcher default.</param>
/// <param name="PromotedName">
///     When <paramref name="IsDefault" /> is <see langword="true" />, the name of the installation the delete would promote to default, or <see langword="null" />
///     when it is the last installation. Always <see langword="null" /> when <paramref name="IsDefault" /> is <see langword="false" />.
/// </param>
public sealed record InstallationDeletionPreview(string Name, bool IsDefault, string? PromotedName);
```

```csharp
// Source/Erdmier.ZooTycoonLauncher.Application/Installations/Delete/PreviewInstallationDeletionHandler.cs
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>
///     Handler for <see cref="PreviewInstallationDeletionQuery" />. Uses the same promotion rule as <see cref="DeleteInstallationHandler" /> (excluding the row being
///     previewed), so the confirmation cannot name a different successor from the one the delete promotes.
/// </summary>
public sealed class PreviewInstallationDeletionHandler : IQueryHandler<PreviewInstallationDeletionQuery, ErrorOr<InstallationDeletionPreview>>
{
    private readonly IInstallationRepository _installations;

    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="settings">Launcher settings repository.</param>
    public PreviewInstallationDeletionHandler(IInstallationRepository installations, ILauncherSettingsRepository settings)
    {
        _installations = installations;
        _settings      = settings;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<InstallationDeletionPreview>> Handle(PreviewInstallationDeletionQuery query, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(query.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", $"No installation with id {query.InstallationId}.");
        }

        LauncherSettings settings  = await _settings.GetAsync(cancellationToken);
        bool             isDefault = settings.DefaultInstallationId == row.Id;

        if (!isDefault)
        {
            return new InstallationDeletionPreview(row.Name, IsDefault: false, PromotedName: null);
        }

        GameInstallation? successor = await _installations.FindDefaultPromotionCandidateAsync(row.Id, cancellationToken);

        return new InstallationDeletionPreview(row.Name, IsDefault: true, successor?.Name);
    }
}
```

- [ ] **Step 5: Make Relocate reject a folder without `zoo.exe`**

In `RelocateInstallationHandler.Handle`, directly after the `Installation.PathMissing` check:

```csharp
        if (!verification.HasExe)
        {
            // SDD §7.2.5: relocation exists to recover a missing zoo.exe — never move an installation to a folder that still lacks it.
            return Error.Validation(code: "Installation.ExeMissing", $"The folder \"{command.NewPath}\" does not contain zoo.exe.");
        }
```

- [ ] **Step 6: Run the tests**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit -nologo` then `dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration -nologo`
Expected: PASS — Application 66 + 5 = 71; Infrastructure 24 + 2 = 26.

- [ ] **Step 7: Build and commit**

Run: `dotnet build Erdmier.ZooTycoonLauncher.slnx -nologo -v q` → 0 warnings / 0 errors.

```bash
git add Source/Erdmier.ZooTycoonLauncher.Application Source/Erdmier.ZooTycoonLauncher.Infrastructure Tests
git commit -m "feat(✨): preview installation deletion and reject relocations without zoo.exe"
```

---

## Task 3: Grids refresh themselves from the messages

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Common/InstallationGridViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/OpenGameInstallationViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/InstallationManagerDialogViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs`

**Interfaces:**
- Consumes: messages now published by the handlers (Task 1) via `MessengerEventPublisher` on the UI thread.
- Produces: no public API change. `InstallationGridViewModel` coalesces message-driven reloads.

- [ ] **Step 1: Coalesce the grid's message-driven reloads**

In `InstallationGridViewModel`, add two fields after `_disposed`:

```csharp
    private bool _isReloading;

    private bool _reloadPending;
```

Update the class summary's last sentence to: "Subscribes to the installation change messages the Application handlers publish, so the grid refreshes itself whoever
made the change."

Replace the `ScheduleReload` comment + method with:

```csharp
    // Marshals the reload onto the UI thread (the publisher already sends on it, but Receive must not rely on that) and coalesces bursts: deleting the default publishes
    // InstallationDeletedMessage and DefaultInstallationChangedMessage together, which must cost one reload, not two overlapping queries on the shared DbContext.
    private void ScheduleReload()
        => Dispatcher.UIThread.Post(() =>
        {
            if (!_disposed)
            {
                _ = ReloadCoalescedAsync();
            }
        });

    // Runs only on the UI thread (see ScheduleReload), so the two flags need no locking. A message that arrives whilst a reload is in flight marks one follow-up reload
    // instead of starting a second, overlapping one. A failed reload leaves the previous rows in place.
    private async Task ReloadCoalescedAsync()
    {
        if (_isReloading)
        {
            _reloadPending = true;

            return;
        }

        _isReloading = true;

        try
        {
            do
            {
                _reloadPending = false;

                await LoadAsync();
            }
            while (_reloadPending && !_disposed);
        }
        finally
        {
            _isReloading = false;
        }
    }
```

- [ ] **Step 2: Drop the hosts' explicit reloads**

`OpenGameInstallationViewModel` — replace `AddAsync` and `ManageAsync`:

```csharp
    // The new row reaches Grid through InstallationAddedMessage, so no explicit reload is needed.
    [ RelayCommand ]
    private async Task AddAsync()
    {
        if (_dialogs is null)
        {
            return;
        }

        await _dialogs.ShowAddInstallationAsync(prefilledPath: null);
    }

    // Changes made in the manager reach Grid through the change messages, so no explicit reload is needed.
    [ RelayCommand ]
    private async Task ManageAsync()
    {
        if (_dialogs is null)
        {
            return;
        }

        await _dialogs.ShowInstallationManagerAsync();
    }
```

`InstallationManagerDialogViewModel.AddAsync` — keep `HasChanges`, drop the reload:

```csharp
    [ RelayCommand ]
    private async Task AddAsync()
    {
        if (_dialogs is null)
        {
            return;
        }

        AddInstallationResult? result = await _dialogs.ShowAddInstallationAsync(prefilledPath: null);

        if (result is not null)
        {
            HasChanges = true;
        }
    }
```

`MainWindowViewModel.ManageInstallationsAsync` — remove the picker branch (Task 9 extends this method):

```csharp
        bool changed = await _dialogs.ShowInstallationManagerAsync();

        if (!changed)
        {
            return;
        }

        // The picker needs nothing here: its grid refreshes itself from the change messages.
        if (ActiveContent is NoGameInstallationFoundViewModel)
        {
            await RunBootAsync(installationId: null, cancellationToken);
        }
```

Update the `//` comment block above `ManageInstallationsAsync` so it no longer says the picker grid is reloaded here.

- [ ] **Step 3: Build and run the architecture tests**

Run: `dotnet build Erdmier.ZooTycoonLauncher.slnx -nologo -v q` then `dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture -nologo`
Expected: 0 warnings / 0 errors; 13 PASS.

- [ ] **Step 4: Commit**

```bash
git add Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels
git commit -m "refactor(♻️): refresh installation grids from change messages and coalesce reloads"
```

---

## Task 4: Shared installation form, the Add dialogue's first-installation defaults, and SDD §7.2.1

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Common/InstallationFormViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Common/InstallationFormView.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Common/InstallationFormView.axaml.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/AddInstallationDialogViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/AddInstallationDialogView.axaml`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaDialogService.cs`
- Modify: `docs/superpowers/specs/2026-05-26-zoo-tycoon-launcher-design.md`

**Interfaces:**
- Produces: `InstallationFormViewModel(IDialogService dialogs)` with `Name`, `Path`, `MakeDefault`, `ErrorMessage`, `IsFolderReadOnly`, `IsBrowseVisible` (default `true`),
  `IsDefaultLocked`, `LockDefault()`, `BrowseCommand` — namespace `Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common`. Consumed by Task 5.
- Produces: `InstallationFormView` — xmlns `using:Erdmier.ZooTycoonLauncher.Desktop.Views.Common`; bind with `DataContext="{Binding Form}"`.
- Produces: `AddInstallationDialogViewModel.Form` and `Task InitialiseAsync(CancellationToken cancellationToken = default)`.

- [ ] **Step 1: Create the form view model**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Common/InstallationFormViewModel.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common;

/// <summary>
///     Shared view model for the installation form — Name, Folder (with Browse), Mark as default, and the inline error — hosted by the Add and Edit Installation
///     dialogues (SDD §7.2.1, §7.2.3, §9.5). The host sets the mode flags and owns Save; the form owns only the inputs and the folder picker.
/// </summary>
public sealed partial class InstallationFormViewModel : ViewModelBase
{
    private readonly IDialogService? _dialogs;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="dialogs">The dialogue service — used for the folder picker behind Browse.</param>
    public InstallationFormViewModel(IDialogService dialogs) => _dialogs = dialogs;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public InstallationFormViewModel()
        : this(null!)
    { }

    /// <summary>The most recent validation or dispatch error, or <see langword="null" /> when none. Shown under the inputs.</summary>
    [ ObservableProperty ]
    public partial string? ErrorMessage { get; set; }

    /// <summary><see langword="true" /> when the Browse button is shown (Add); <see langword="false" /> hides it (Edit).</summary>
    [ ObservableProperty ]
    public partial bool IsBrowseVisible { get; set; } = true;

    /// <summary>
    ///     <see langword="true" /> when Mark as default is ticked and cannot be changed — the first installation in Add (SDD §7.2.1), or the current default in Edit
    ///     (SDD §7.2.3). Set through <see cref="LockDefault" />.
    /// </summary>
    [ ObservableProperty ]
    public partial bool IsDefaultLocked { get; set; }

    /// <summary><see langword="true" /> when the Folder input is read-only (Edit — relocation happens through Fix).</summary>
    [ ObservableProperty ]
    public partial bool IsFolderReadOnly { get; set; }

    /// <summary>Whether the installation should be (or stay) the launcher default. Bound to the checkbox.</summary>
    [ ObservableProperty ]
    public partial bool MakeDefault { get; set; }

    /// <summary>The user-visible installation name. Bound to the Name input.</summary>
    [ ObservableProperty ]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>The installation folder (the one containing <c>zoo.exe</c>). Bound to the Folder input.</summary>
    [ ObservableProperty ]
    public partial string Path { get; set; } = string.Empty;

    /// <summary>Ticks Mark as default and prevents it being unticked.</summary>
    public void LockDefault()
    {
        MakeDefault     = true;
        IsDefaultLocked = true;
    }

    [ RelayCommand ]
    private async Task BrowseAsync()
    {
        if (_dialogs is null)
        {
            return;
        }

        string? chosen = await _dialogs.PickFolderAsync(string.IsNullOrWhiteSpace(Path) ? null : Path);

        if (!string.IsNullOrWhiteSpace(chosen))
        {
            Path = chosen;
        }
    }
}
```

- [ ] **Step 2: Create the form view**

```xml
<!-- Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Common/InstallationFormView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Common.InstallationFormView"
             x:DataType="vm:InstallationFormViewModel">
    <StackPanel Orientation="Vertical"
                Spacing="10">
        <Grid ColumnDefinitions="15*, 85*"
              RowDefinitions="*, *, *"
              RowSpacing="5">
            <TextBlock Grid.Column="0"
                       Grid.Row="0"
                       Text="Name:" />

            <TextBox Grid.Column="1"
                     Grid.Row="0"
                     Text="{Binding Name, Mode=TwoWay}" />

            <TextBlock Grid.Column="0"
                       Grid.Row="1"
                       Text="Folder:" />

            <DockPanel Grid.Column="1"
                       Grid.Row="1"
                       HorizontalSpacing="5">
                <Button DockPanel.Dock="Right"
                        Command="{Binding BrowseCommand}"
                        Content="Browse…"
                        IsVisible="{Binding IsBrowseVisible}"
                        MinWidth="75" />

                <TextBox IsReadOnly="{Binding IsFolderReadOnly}"
                         Text="{Binding Path, Mode=TwoWay}" />
            </DockPanel>

            <CheckBox Content="Mark as default installation"
                      Grid.Column="1"
                      Grid.Row="2"
                      IsChecked="{Binding MakeDefault, Mode=TwoWay}"
                      IsEnabled="{Binding !IsDefaultLocked}" />
        </Grid>

        <DockPanel HorizontalSpacing="10"
                   IsVisible="{Binding ErrorMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}">
            <Image DockPanel.Dock="Left"
                   Height="16"
                   Source="/Assets/Win95Icons/cancel16.gif"
                   VerticalAlignment="Center"
                   Width="16" />

            <TextBlock Foreground="Red"
                       Margin="2 0 0 0"
                       Text="{Binding ErrorMessage}"
                       TextWrapping="Wrap" />
        </DockPanel>
    </StackPanel>
</UserControl>
```

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Common/InstallationFormView.axaml.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Common;

/// <summary>Code-behind for <c>InstallationFormView.axaml</c> — the shared Name / Folder / Default form hosted by the Add and Edit Installation dialogues.</summary>
public sealed partial class InstallationFormView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public InstallationFormView() => AvaloniaXamlLoader.Load(this);
}
```

- [ ] **Step 3: Rewrite the Add dialogue view model around the form**

Replace the whole of `AddInstallationDialogViewModel.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     The view model for the Add Installation modal (SDD §7.2.1, §9.5). Hosts the shared <see cref="InstallationFormViewModel" /> and dispatches
///     <see cref="AddInstallationCommand" /> on Save. With no installations registered yet, the name is pre-filled with <c>Main</c> and Mark as default is ticked and
///     locked, because the first installation always becomes the default. Raises <see cref="CloseRequested" /> with the dispatched result on success, or
///     <see langword="null" /> when the user cancels.
/// </summary>
public sealed partial class AddInstallationDialogViewModel : ViewModelBase
{
    private const string FirstInstallationName = "Main";

    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">The dialogue service — passed to the form for the folder picker.</param>
    public AddInstallationDialogViewModel(IMediator mediator, IDialogService dialogs)
    {
        _mediator = mediator;

        Form = new InstallationFormViewModel(dialogs);

        Form.PropertyChanged += OnFormPropertyChanged;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public AddInstallationDialogViewModel()
        : this(null!, null!)
    { }

    /// <summary>The shared Name / Folder / Default form. Bound to <c>InstallationFormView.DataContext</c>.</summary>
    public InstallationFormViewModel Form { get; }

    /// <summary><see langword="true" /> while a dispatch is in flight.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary>
    ///     Applies the first-installation defaults (SDD §7.2.1): with no installations registered, pre-fills the name with <c>Main</c> and ticks and locks Mark as
    ///     default. Must be awaited by the dialogue service before the window is shown.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_mediator is null)
        {
            return;
        }

        ErrorOr<IReadOnlyList<InstallationSummary>> existing = await _mediator.Send(new GetAllInstallationsQuery(), cancellationToken);

        if (!existing.IsError
            && existing.Value.Count == 0)
        {
            Form.Name = FirstInstallationName;

            Form.LockDefault();
        }
    }

    /// <summary>Sets the initial folder when the dialogue is being opened with a discovered candidate.</summary>
    /// <param name="prefilledPath">The candidate path to pre-fill, or <see langword="null" />.</param>
    public void PrefillPath(string? prefilledPath)
    {
        if (!string.IsNullOrWhiteSpace(prefilledPath))
        {
            Form.Path = prefilledPath;
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteSave)) ]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        IsBusy            = true;
        Form.ErrorMessage = null;

        try
        {
            ErrorOr<AddInstallationResult> result =
                await _mediator.Send(new AddInstallationCommand(Form.Name.Trim(), Form.Path.Trim(), Form.MakeDefault), cancellationToken);

            if (result.IsError)
            {
                Form.ErrorMessage = result.FirstError.Description;

                return;
            }

            CloseRequested?.Invoke(this, result.Value);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ RelayCommand ]
    private void Cancel() => CloseRequested?.Invoke(this, e: null);

    private bool CanExecuteSave()
        => !IsBusy
           && !string.IsNullOrWhiteSpace(Form.Name)
           && !string.IsNullOrWhiteSpace(Form.Path)

           // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
           && _mediator is not null;

    private void OnFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(InstallationFormViewModel.Name)
                           or nameof(InstallationFormViewModel.Path))
        {
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Raised when the dialogue should close. Argument is the dispatched <see cref="AddInstallationResult" /> on Save, or <see langword="null" /> on Cancel.</summary>
    public event EventHandler<AddInstallationResult?>? CloseRequested;
}
```

- [ ] **Step 4: Host the form in the Add view**

In `AddInstallationDialogView.axaml`, add `xmlns:common="using:Erdmier.ZooTycoonLauncher.Desktop.Views.Common"` to the root element, then replace the `<Grid
ColumnDefinitions="15*, 85*" …>…</Grid>` block **and** the error `<DockPanel …IsVisible="{Binding ErrorMessage…}">…</DockPanel>` block (and the stray blank line after it) with:

```xml
                <common:InstallationFormView DataContext="{Binding Form}" />
```

Leave the header, the two explanatory `TextBlock`s, the `Separator`, and the Save / Cancel `UniformStack` unchanged.

- [ ] **Step 5: Initialise the Add dialogue before showing it**

In `AvaloniaDialogService.ShowAddInstallationAsync`, after `vm.PrefillPath(prefilledPath);`:

```csharp
        AddInstallationDialogViewModel vm = _services.GetRequiredService<AddInstallationDialogViewModel>();
        vm.PrefillPath(prefilledPath);

        await vm.InitialiseAsync();
```

- [ ] **Step 6: Amend SDD §7.2.1 (revision 1.6)**

In `docs/superpowers/specs/2026-05-26-zoo-tycoon-launcher-design.md`:
- Replace the bullet `    - Zero existing installations → placeholder `Main`.` with
  `    - Zero existing installations → the Name input is **pre-filled** with `Main` (an editable value, not a placeholder).`
- In the Document-control table set **Version** to `1.6` and **Last updated** to `24 September 2026`, keeping the column alignment.
- Append this revision-history row after the `1.5` row:

```text
| 1.6     | 24 September 2026 | Justinian | §7.2.1: with no installations registered, the Add Installation dialogue pre-fills the Name input with `Main` (an editable value, previously described as a placeholder); the Default checkbox stays ticked and read-only. Drafted by Claude during the installation-management-dialogues slice. |
```

- Prose lines stay ≤ 180 characters (tables exempt); keep LF line endings and no BOM. Verify with PowerShell `[string].Length`.

- [ ] **Step 7: Build, run the architecture tests, and commit**

Run: `dotnet build Erdmier.ZooTycoonLauncher.slnx -nologo -v q` then `dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture -nologo`
Expected: 0 warnings / 0 errors; 13 PASS.

```bash
git add Source/Erdmier.ZooTycoonLauncher.Desktop docs/superpowers/specs/2026-05-26-zoo-tycoon-launcher-design.md
git commit -m "feat(✨): share the installation form and pre-fill Main for the first installation"
```

---

## Task 5: Edit Installation dialogue

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/InstallationDialogMessages.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/EditInstallationDialogViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/EditInstallationDialogView.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/EditInstallationDialogView.axaml.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaDialogService.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/PlayViewModel.cs` (file-scoped `NoOpDialogService`)
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/InstallationManagerDialogViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`

**Interfaces:**
- Consumes: `InstallationFormViewModel` + `InstallationFormView` (Task 4); `GetInstallationByIdQuery(Guid) → ErrorOr<InstallationSummary>`;
  `UpdateInstallationCommand(Guid InstallationId, string Name, bool MakeDefault) → ErrorOr<Success>`.
- Produces: `InstallationDialogMessages.InstallationMissing` / `.UnexpectedFailure` (Tasks 6–8 use them); `IDialogService.ShowEditInstallationAsync(Guid) → Task<bool>`.

- [ ] **Step 1: Add the global usings**

In `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`, add in sorted position:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Installations.GetById;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Update;
global using Microsoft.Extensions.Logging.Abstractions;
```

- [ ] **Step 2: Create the shared dialogue messages**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/InstallationDialogMessages.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>User-facing messages shared by the installation management dialogues (Info, Edit, Delete, Fix).</summary>
public static class InstallationDialogMessages
{
    /// <summary>Shown when the installation was removed after the grid loaded (the handler returned <c>Installation.NotFound</c>).</summary>
    public const string InstallationMissing = "This installation no longer exists.";

    /// <summary>Shown when a dispatch throws unexpectedly; the exception itself is logged.</summary>
    public const string UnexpectedFailure = "Something went wrong — see the launcher log for details.";
}
```

- [ ] **Step 3: Create the Edit view model**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/EditInstallationDialogViewModel.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     View model for the Edit Installation modal (SDD §7.2.3, §9.5). Hosts the shared <see cref="InstallationFormViewModel" /> in edit mode — Name editable, Folder
///     read-only without Browse, Mark as default locked when the installation is already the default — and dispatches <see cref="UpdateInstallationCommand" /> on Save.
///     Raises <see cref="CloseRequested" /> with <see langword="true" /> after a successful save, or <see langword="false" /> on Cancel.
/// </summary>
public sealed partial class EditInstallationDialogViewModel : ViewModelBase
{
    private readonly ILogger<EditInstallationDialogViewModel> _logger;

    private readonly IMediator _mediator;

    private Guid _installationId;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">The dialogue service — passed to the form (Browse is hidden in edit mode).</param>
    /// <param name="logger">Logger for unexpected load and save failures.</param>
    public EditInstallationDialogViewModel(IMediator mediator, IDialogService dialogs, ILogger<EditInstallationDialogViewModel> logger)
    {
        _mediator = mediator;
        _logger   = logger;

        Form = new InstallationFormViewModel(dialogs)
        {
            IsFolderReadOnly = true,
            IsBrowseVisible  = false
        };

        Form.PropertyChanged += OnFormPropertyChanged;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public EditInstallationDialogViewModel()
        : this(null!, null!, NullLogger<EditInstallationDialogViewModel>.Instance)
    { }

    /// <summary>The shared Name / Folder / Default form, in edit mode. Bound to <c>InstallationFormView.DataContext</c>.</summary>
    public InstallationFormViewModel Form { get; }

    /// <summary><see langword="true" /> while a save is in flight.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary><see langword="true" /> once the installation has loaded; Save stays disabled until then (and for good if it no longer exists).</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    public partial bool IsLoaded { get; set; }

    /// <summary>Loads the installation into the form. Must be awaited by the dialogue service before the window is shown.</summary>
    /// <param name="installationId">The installation to edit.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task InitialiseAsync(Guid installationId, CancellationToken cancellationToken = default)
    {
        _installationId = installationId;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_mediator is null)
        {
            return;
        }

        try
        {
            ErrorOr<InstallationSummary> result = await _mediator.Send(new GetInstallationByIdQuery(installationId), cancellationToken);

            if (result.IsError)
            {
                Form.ErrorMessage = InstallationDialogMessages.InstallationMissing;

                return;
            }

            Form.Name = result.Value.Name;
            Form.Path = result.Value.Path;

            if (result.Value.IsDefault)
            {
                Form.LockDefault();
            }

            IsLoaded = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to load installation {InstallationId} for editing.", installationId);

            Form.ErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteSave)) ]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        IsBusy            = true;
        Form.ErrorMessage = null;

        try
        {
            ErrorOr<Success> result =
                await _mediator.Send(new UpdateInstallationCommand(_installationId, Form.Name.Trim(), Form.MakeDefault), cancellationToken);

            if (result.IsError)
            {
                Form.ErrorMessage = result.FirstError.Type == ErrorType.NotFound
                                        ? InstallationDialogMessages.InstallationMissing
                                        : result.FirstError.Description;

                return;
            }

            CloseRequested?.Invoke(this, e: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to save installation {InstallationId}.", _installationId);

            Form.ErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ RelayCommand ]
    private void Cancel() => CloseRequested?.Invoke(this, e: false);

    private bool CanExecuteSave() => IsLoaded && !IsBusy && !string.IsNullOrWhiteSpace(Form.Name);

    private void OnFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(InstallationFormViewModel.Name))
        {
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Raised when the dialogue should close. Argument is <see langword="true" /> after a successful save, <see langword="false" /> on Cancel.</summary>
    public event EventHandler<bool>? CloseRequested;
}
```

- [ ] **Step 4: Create the Edit view**

```xml
<!-- Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/EditInstallationDialogView.axaml -->
<ClassicWindow xmlns="https://github.com/avaloniaui"
               xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
               xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs"
               xmlns:common="using:Erdmier.ZooTycoonLauncher.Desktop.Views.Common"
               xmlns:layout="clr-namespace:Erdmier.ZooTycoonLauncher.Desktop.Views.Layout"
               x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs.EditInstallationDialogView"
               x:DataType="vm:EditInstallationDialogViewModel"
               Icon="/Assets/Win95Icons/foldercatalog16.ico"
               Title="Edit Installation"
               Width="420"
               SizeToContent="Height"
               CanResize="False"
               WindowStartupLocation="CenterOwner"
               ShowInTaskbar="False">
    <StackPanel Margin="8"
                Orientation="Vertical"
                Spacing="10">
        <DockPanel HorizontalSpacing="10">
            <Image DockPanel.Dock="Left"
                   Height="32"
                   Source="/Assets/Win95Icons/foldercatalog16.ico"
                   VerticalAlignment="Top"
                   Width="32" />

            <StackPanel Margin="5 0 0 0"
                        Orientation="Vertical"
                        Spacing="4">
                <TextBlock FontWeight="Solid"
                           Text="Edit installation" />

                <TextBlock Opacity="0.6"
                           Text="Rename this installation or change which one is the launcher's default."
                           TextWrapping="Wrap" />

                <common:InstallationFormView DataContext="{Binding Form}"
                                             Margin="0 6 0 0" />

                <layout:UniformStack HorizontalAlignment="Right"
                                     Margin="0 5"
                                     Spacing="8">
                    <Button Command="{Binding SaveCommand}"
                            Content="Save"
                            IsDefault="True"
                            MinWidth="75" />
                    <Button Command="{Binding CancelCommand}"
                            Content="Cancel"
                            IsCancel="True"
                            MinWidth="75" />
                </layout:UniformStack>
            </StackPanel>
        </DockPanel>
    </StackPanel>
</ClassicWindow>
```

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/EditInstallationDialogView.axaml.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>The Win95-styled modal Edit Installation dialogue. Closes itself when the view model raises <c>CloseRequested</c>, returning whether the edit was saved.</summary>
public sealed partial class EditInstallationDialogView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public EditInstallationDialogView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs eventArgs)
    {
        base.OnDataContextChanged(eventArgs);

        if (DataContext is not EditInstallationDialogViewModel viewModel)
        {
            return;
        }

        viewModel.CloseRequested -= OnCloseRequested;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, bool saved) => Close(saved);
}
```

- [ ] **Step 5: Add the dialogue-service member**

`IDialogService` — add after `ShowInstallationManagerAsync`:

```csharp
    /// <summary>Opens the modal Edit Installation dialogue (SDD §7.2.3, §9.5) for the given installation.</summary>
    /// <param name="installationId">The installation to edit.</param>
    /// <returns><see langword="true" /> when the edit was saved; otherwise <see langword="false" />.</returns>
    Task<bool> ShowEditInstallationAsync(Guid installationId);
```

`AvaloniaDialogService` — add after `ShowInstallationManagerAsync`:

```csharp
    /// <inheritdoc />
    public async Task<bool> ShowEditInstallationAsync(Guid installationId)
    {
        Window? owner = ResolveOwner();

        if (owner is null)
        {
            return false;
        }

        EditInstallationDialogViewModel vm = _services.GetRequiredService<EditInstallationDialogViewModel>();

        await vm.InitialiseAsync(installationId);

        EditInstallationDialogView view = new()
        {
            DataContext = vm
        };

        return await view.ShowDialog<bool>(owner);
    }
```

`NoOpDialogService` in `PlayViewModel.cs` — add:

```csharp
    public Task<bool> ShowEditInstallationAsync(Guid installationId) => Task.FromResult(false);
```

`DesktopServiceCollectionExtensions.AddDesktop` — after the `AddInstallationDialogViewModel` registration:

```csharp
        services.AddTransient<EditInstallationDialogViewModel>();
```

- [ ] **Step 6: Wire the manager's Edit command**

In `InstallationManagerDialogViewModel`, replace the `EditAsync` stub:

```csharp
    [ RelayCommand(CanExecute = nameof(CanExecuteSelectionCommand)) ]
    private async Task EditAsync()
    {
        if (_dialogs is null
            || Grid.SelectedRow is null)
        {
            return;
        }

        if (await _dialogs.ShowEditInstallationAsync(Grid.SelectedRow.Id))
        {
            HasChanges = true;
        }
    }
```

Update the class summary (drop "Edit … scaffolded stubs") and the `HasChanges` summary to read "…changed anything during this session of the dialogue (a successful Add, Edit,
Delete or Fix)…".

- [ ] **Step 7: Build, run the architecture tests, and commit**

Run: `dotnet build Erdmier.ZooTycoonLauncher.slnx -nologo -v q` then `dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture -nologo`
Expected: 0 warnings / 0 errors; 13 PASS.

Manual check (for the PR checklist): Manager → select a row → Edit: title "Edit Installation", folder read-only with no Browse, default ticked and disabled for the default
row; renaming to an existing name shows the validator's error inline; Save closes and the grid shows the new name.

```bash
git add Source/Erdmier.ZooTycoonLauncher.Desktop
git commit -m "feat(✨): add the Edit Installation dialogue"
```

---

## Task 6: Installation Info dialogue

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/InstallationInfoDialogViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/InstallationInfoDialogView.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/InstallationInfoDialogView.axaml.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs`, `…/Composition/AvaloniaDialogService.cs`, `…/Composition/DesktopServiceCollectionExtensions.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/PlayViewModel.cs` (NoOp)
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/InstallationManagerDialogViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/OpenGameInstallationViewModel.cs`

**Interfaces:**
- Consumes: `GetInstallationByIdQuery`; `InstallationDialogMessages` (Task 5); `ColourTokenToBrushConverter` (namespace `Erdmier.ZooTycoonLauncher.Desktop.Converters`).
- Produces: `IDialogService.ShowInstallationInfoAsync(Guid) → Task`.

- [ ] **Step 1: Create the Info view model**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/InstallationInfoDialogViewModel.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     View model for the read-only Installation Info modal (SDD §7.2.6, §9.5): name, path, status, default flag, and the added / last-opened / last-played timestamps
///     in the user's local time. History entries shows "—" until the INI Config slice writes snapshots.
/// </summary>
public sealed partial class InstallationInfoDialogViewModel : ViewModelBase
{
    private const string NotAvailable = "—";

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo(name: "en-GB");

    private readonly ILogger<InstallationInfoDialogViewModel> _logger;

    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="logger">Logger for unexpected load failures.</param>
    public InstallationInfoDialogViewModel(IMediator mediator, ILogger<InstallationInfoDialogViewModel> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public InstallationInfoDialogViewModel()
        : this(null!, NullLogger<InstallationInfoDialogViewModel>.Instance)
    { }

    /// <summary>When the installation was added, in local time; "—" until loaded.</summary>
    [ ObservableProperty ]
    public partial string AddedText { get; set; } = NotAvailable;

    /// <summary>"Yes" when the installation is the launcher default, otherwise "No"; "—" until loaded.</summary>
    [ ObservableProperty ]
    public partial string DefaultText { get; set; } = NotAvailable;

    /// <summary>The load error, or <see langword="null" /> when the installation loaded.</summary>
    [ ObservableProperty ]
    public partial string? ErrorMessage { get; set; }

    /// <summary>Always "—": INI history is not recorded until the INI Config slice.</summary>
    public string HistoryEntriesText => NotAvailable;

    /// <summary>When the installation last became the active installation, in local time, or "—".</summary>
    [ ObservableProperty ]
    public partial string LastOpenedText { get; set; } = NotAvailable;

    /// <summary>When the game was last launched from this installation, in local time, or "—".</summary>
    [ ObservableProperty ]
    public partial string LastPlayedText { get; set; } = NotAvailable;

    /// <summary>The installation's name; "—" until loaded.</summary>
    [ ObservableProperty ]
    public partial string Name { get; set; } = NotAvailable;

    /// <summary>The installation's folder; "—" until loaded.</summary>
    [ ObservableProperty ]
    public partial string Path { get; set; } = NotAvailable;

    /// <summary>The validity colour token (<c>Green</c> / <c>Red</c>) for the Status value.</summary>
    [ ObservableProperty ]
    public partial string StatusColourToken { get; set; } = "Green";

    /// <summary>The validity display name (e.g. <c>Valid</c>, <c>Invalid — No EXE</c>); "—" until loaded.</summary>
    [ ObservableProperty ]
    public partial string StatusText { get; set; } = NotAvailable;

    /// <summary>Loads the installation. Must be awaited by the dialogue service before the window is shown.</summary>
    /// <param name="installationId">The installation to describe.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task InitialiseAsync(Guid installationId, CancellationToken cancellationToken = default)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_mediator is null)
        {
            return;
        }

        try
        {
            ErrorOr<InstallationSummary> result = await _mediator.Send(new GetInstallationByIdQuery(installationId), cancellationToken);

            if (result.IsError)
            {
                ErrorMessage = InstallationDialogMessages.InstallationMissing;

                return;
            }

            InstallationSummary summary = result.Value;

            Name              = summary.Name;
            Path              = summary.Path;
            StatusText        = summary.Validity.DisplayName;
            StatusColourToken = summary.Validity.ColourToken;
            DefaultText       = summary.IsDefault ? "Yes" : "No";
            AddedText         = FormatTimestamp(summary.AddedUtc);
            LastOpenedText    = FormatTimestamp(summary.LastOpenedUtc);
            LastPlayedText    = FormatTimestamp(summary.LastPlayedUtc);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to load installation {InstallationId} for the Info dialogue.", installationId);

            ErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
    }

    [ RelayCommand ]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    // Storage is UTC; display is local time, en-GB (e.g. "12 Mar 2025 18:02") — SDD §7.2.6.
    private static string FormatTimestamp(DateTime? utc)
        => utc?.ToLocalTime()
               .ToString(format: "d MMM yyyy HH:mm", DisplayCulture)
           ?? NotAvailable;

    /// <summary>Raised when the dialogue should close.</summary>
    public event EventHandler? CloseRequested;
}
```

- [ ] **Step 2: Create the Info view**

```xml
<!-- Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/InstallationInfoDialogView.axaml -->
<ClassicWindow xmlns="https://github.com/avaloniaui"
               xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
               xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs"
               xmlns:converters="using:Erdmier.ZooTycoonLauncher.Desktop.Converters"
               x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs.InstallationInfoDialogView"
               x:DataType="vm:InstallationInfoDialogViewModel"
               Icon="/Assets/Win95Icons/foldercatalog16.ico"
               Title="Installation Info"
               Width="420"
               SizeToContent="Height"
               CanResize="False"
               WindowStartupLocation="CenterOwner"
               ShowInTaskbar="False">
    <ClassicWindow.Resources>
        <converters:ColourTokenToBrushConverter x:Key="ColourTokenToBrush" />
    </ClassicWindow.Resources>

    <StackPanel Margin="8"
                Orientation="Vertical"
                Spacing="10">
        <DockPanel HorizontalSpacing="10">
            <Image DockPanel.Dock="Left"
                   Height="32"
                   Source="/Assets/Win95Icons/information32.gif"
                   VerticalAlignment="Top"
                   Width="32" />

            <StackPanel Margin="5 0 0 0"
                        Orientation="Vertical"
                        Spacing="6">
                <TextBlock Foreground="Red"
                           IsVisible="{Binding ErrorMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"
                           Text="{Binding ErrorMessage}"
                           TextWrapping="Wrap" />

                <Grid ColumnDefinitions="Auto, *"
                      ColumnSpacing="16"
                      RowDefinitions="Auto, Auto, Auto, Auto, Auto, Auto, Auto, Auto"
                      RowSpacing="6">
                    <TextBlock Grid.Column="0" Grid.Row="0" Opacity="0.6" Text="Name:" />
                    <TextBlock Grid.Column="1" Grid.Row="0" FontWeight="Bold" Text="{Binding Name}" TextWrapping="Wrap" />

                    <TextBlock Grid.Column="0" Grid.Row="1" Opacity="0.6" Text="Path:" />
                    <TextBlock Grid.Column="1" Grid.Row="1" FontFamily="Consolas" Text="{Binding Path}" TextWrapping="Wrap" />

                    <TextBlock Grid.Column="0" Grid.Row="2" Opacity="0.6" Text="Status:" />
                    <TextBlock Grid.Column="1"
                               Grid.Row="2"
                               Foreground="{Binding StatusColourToken, Converter={StaticResource ColourTokenToBrush}}"
                               Text="{Binding StatusText}" />

                    <TextBlock Grid.Column="0" Grid.Row="3" Opacity="0.6" Text="Default:" />
                    <TextBlock Grid.Column="1" Grid.Row="3" Text="{Binding DefaultText}" />

                    <TextBlock Grid.Column="0" Grid.Row="4" Opacity="0.6" Text="Added:" />
                    <TextBlock Grid.Column="1" Grid.Row="4" Text="{Binding AddedText}" />

                    <TextBlock Grid.Column="0" Grid.Row="5" Opacity="0.6" Text="Last opened:" />
                    <TextBlock Grid.Column="1" Grid.Row="5" Text="{Binding LastOpenedText}" />

                    <TextBlock Grid.Column="0" Grid.Row="6" Opacity="0.6" Text="Last played:" />
                    <TextBlock Grid.Column="1" Grid.Row="6" Text="{Binding LastPlayedText}" />

                    <TextBlock Grid.Column="0" Grid.Row="7" Opacity="0.6" Text="History entries:" />
                    <TextBlock Grid.Column="1" Grid.Row="7" Text="{Binding HistoryEntriesText}" />
                </Grid>

                <Button Command="{Binding CloseCommand}"
                        Content="Close"
                        HorizontalAlignment="Right"
                        IsCancel="True"
                        IsDefault="True"
                        Margin="0 5"
                        MinWidth="75" />
            </StackPanel>
        </DockPanel>
    </StackPanel>
</ClassicWindow>
```

Format the attributes one per line to match the repository's XAML style when writing the file (the compact rows above are for plan brevity only).

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/InstallationInfoDialogView.axaml.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>The Win95-styled read-only Installation Info dialogue. Closes itself when the view model raises <c>CloseRequested</c>.</summary>
public sealed partial class InstallationInfoDialogView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public InstallationInfoDialogView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs eventArgs)
    {
        base.OnDataContextChanged(eventArgs);

        if (DataContext is not InstallationInfoDialogViewModel viewModel)
        {
            return;
        }

        viewModel.CloseRequested -= OnCloseRequested;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();
}
```

- [ ] **Step 3: Add the dialogue-service member**

`IDialogService`:

```csharp
    /// <summary>Opens the read-only Installation Info dialogue (SDD §7.2.6, §9.5) for the given installation.</summary>
    /// <param name="installationId">The installation to describe.</param>
    Task ShowInstallationInfoAsync(Guid installationId);
```

`AvaloniaDialogService`:

```csharp
    /// <inheritdoc />
    public async Task ShowInstallationInfoAsync(Guid installationId)
    {
        Window? owner = ResolveOwner();

        if (owner is null)
        {
            return;
        }

        InstallationInfoDialogViewModel vm = _services.GetRequiredService<InstallationInfoDialogViewModel>();

        await vm.InitialiseAsync(installationId);

        InstallationInfoDialogView view = new()
        {
            DataContext = vm
        };

        await view.ShowDialog(owner);
    }
```

`NoOpDialogService` in `PlayViewModel.cs`:

```csharp
    public Task ShowInstallationInfoAsync(Guid installationId) => Task.CompletedTask;
```

`DesktopServiceCollectionExtensions.AddDesktop`:

```csharp
        services.AddTransient<InstallationInfoDialogViewModel>();
```

- [ ] **Step 4: Wire the manager's and the picker's Info commands**

`InstallationManagerDialogViewModel` — replace the `InfoAsync` stub (Info changes nothing, so `HasChanges` is untouched):

```csharp
    [ RelayCommand(CanExecute = nameof(CanExecuteSelectionCommand)) ]
    private async Task InfoAsync()
    {
        if (_dialogs is null
            || Grid.SelectedRow is null)
        {
            return;
        }

        await _dialogs.ShowInstallationInfoAsync(Grid.SelectedRow.Id);
    }
```

`OpenGameInstallationViewModel` — replace the `InfoAsync` stub with the same body.

- [ ] **Step 5: Build, run the architecture tests, and commit**

Run: `dotnet build Erdmier.ZooTycoonLauncher.slnx -nologo -v q` then `dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture -nologo`
Expected: 0 warnings / 0 errors; 13 PASS.

Manual check: Manager and picker → Info shows all eight rows; timestamps as `12 Mar 2025 18:02` in local time; "—" for never-opened / never-played and History entries.

```bash
git add Source/Erdmier.ZooTycoonLauncher.Desktop
git commit -m "feat(✨): add the Installation Info dialogue"
```

---

## Task 7: Delete Installation dialogue

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/DeleteInstallationDialogViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/DeleteInstallationDialogView.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/DeleteInstallationDialogView.axaml.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs`, `…/Composition/AvaloniaDialogService.cs`, `…/Composition/DesktopServiceCollectionExtensions.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/PlayViewModel.cs` (NoOp)
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/InstallationManagerDialogViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`

**Interfaces:**
- Consumes: `PreviewInstallationDeletionQuery` / `InstallationDeletionPreview` (Task 2); `DeleteInstallationCommand(Guid) → ErrorOr<DeleteInstallationResult>`;
  `InstallationDialogMessages` (Task 5).
- Produces: `IDialogService.ShowDeleteInstallationAsync(Guid) → Task<bool>`.

- [ ] **Step 1: Add the global using**

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Installations.Delete;
```

- [ ] **Step 2: Create the Delete view model**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/DeleteInstallationDialogViewModel.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     View model for the Delete Installation confirmation (SDD §7.2.4, §9.5). Previews the delete — naming the installation that would be promoted to default, or that
///     none would remain — and dispatches <see cref="DeleteInstallationCommand" /> on confirm. Raises <see cref="CloseRequested" /> with <see langword="true" /> after
///     a successful delete, or <see langword="false" /> on Cancel.
/// </summary>
public sealed partial class DeleteInstallationDialogViewModel : ViewModelBase
{
    private readonly ILogger<DeleteInstallationDialogViewModel> _logger;

    private readonly IMediator _mediator;

    private Guid _installationId;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="logger">Logger for unexpected preview and delete failures.</param>
    public DeleteInstallationDialogViewModel(IMediator mediator, ILogger<DeleteInstallationDialogViewModel> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public DeleteInstallationDialogViewModel()
        : this(null!, NullLogger<DeleteInstallationDialogViewModel>.Instance)
    { }

    /// <summary>The preview or delete error, or <see langword="null" /> when none.</summary>
    [ ObservableProperty ]
    public partial string? ErrorMessage { get; set; }

    /// <summary><see langword="true" /> when the installation is the default and another installation would be promoted.</summary>
    [ ObservableProperty ]
    public partial bool HasPromotion { get; set; }

    /// <summary>The name of the installation being deleted.</summary>
    [ ObservableProperty ]
    public partial string InstallationName { get; set; } = string.Empty;

    /// <summary><see langword="true" /> while the delete is in flight.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(DeleteCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary><see langword="true" /> when the installation is the default and no other installation remains.</summary>
    [ ObservableProperty ]
    public partial bool IsLastInstallation { get; set; }

    /// <summary><see langword="true" /> once the preview has loaded; Delete stays disabled until then.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(DeleteCommand)) ]
    public partial bool IsLoaded { get; set; }

    /// <summary>The name of the installation that would be promoted to default, or empty when none.</summary>
    [ ObservableProperty ]
    public partial string PromotedName { get; set; } = string.Empty;

    /// <summary>Loads the deletion preview. Must be awaited by the dialogue service before the window is shown.</summary>
    /// <param name="installationId">The installation the user asked to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task InitialiseAsync(Guid installationId, CancellationToken cancellationToken = default)
    {
        _installationId = installationId;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_mediator is null)
        {
            return;
        }

        try
        {
            ErrorOr<InstallationDeletionPreview> result = await _mediator.Send(new PreviewInstallationDeletionQuery(installationId), cancellationToken);

            if (result.IsError)
            {
                ErrorMessage = InstallationDialogMessages.InstallationMissing;

                return;
            }

            InstallationDeletionPreview preview = result.Value;

            InstallationName   = preview.Name;
            HasPromotion       = preview is { IsDefault: true, PromotedName: not null };
            IsLastInstallation = preview is { IsDefault: true, PromotedName: null };
            PromotedName       = preview.PromotedName ?? string.Empty;
            IsLoaded           = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to preview deleting installation {InstallationId}.", installationId);

            ErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteDelete)) ]
    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        IsBusy       = true;
        ErrorMessage = null;

        try
        {
            ErrorOr<DeleteInstallationResult> result = await _mediator.Send(new DeleteInstallationCommand(_installationId), cancellationToken);

            if (result.IsError)
            {
                ErrorMessage = result.FirstError.Type == ErrorType.NotFound
                                   ? InstallationDialogMessages.InstallationMissing
                                   : result.FirstError.Description;

                return;
            }

            CloseRequested?.Invoke(this, e: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to delete installation {InstallationId}.", _installationId);

            ErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ RelayCommand ]
    private void Cancel() => CloseRequested?.Invoke(this, e: false);

    private bool CanExecuteDelete() => IsLoaded && !IsBusy;

    /// <summary>Raised when the dialogue should close. Argument is <see langword="true" /> after a successful delete, <see langword="false" /> on Cancel.</summary>
    public event EventHandler<bool>? CloseRequested;
}
```

- [ ] **Step 3: Create the Delete view**

```xml
<!-- Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/DeleteInstallationDialogView.axaml -->
<ClassicWindow xmlns="https://github.com/avaloniaui"
               xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
               xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs"
               xmlns:layout="clr-namespace:Erdmier.ZooTycoonLauncher.Desktop.Views.Layout"
               x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs.DeleteInstallationDialogView"
               x:DataType="vm:DeleteInstallationDialogViewModel"
               Icon="/Assets/Win95Icons/warning32.gif"
               Title="Delete Installation"
               Width="440"
               SizeToContent="Height"
               CanResize="False"
               WindowStartupLocation="CenterOwner"
               ShowInTaskbar="False">
    <StackPanel Margin="8"
                Orientation="Vertical"
                Spacing="10">
        <DockPanel HorizontalSpacing="10">
            <Image DockPanel.Dock="Left"
                   Height="32"
                   Source="/Assets/Win95Icons/warning32.gif"
                   VerticalAlignment="Top"
                   Width="32" />

            <StackPanel Margin="5 0 0 0"
                        Orientation="Vertical"
                        Spacing="8">
                <TextBlock IsVisible="{Binding IsLoaded}"
                           TextWrapping="Wrap">
                    <Run Text="Remove " />
                    <Run FontWeight="Bold"
                         Text="{Binding InstallationName}" />
                    <Run Text=" from the launcher's registry?" />
                </TextBlock>

                <TextBlock IsVisible="{Binding IsLoaded}"
                           Opacity="0.6"
                           Text="The launcher's database for this installation, including INI history, will be deleted. The game folder on disk is left untouched."
                           TextWrapping="Wrap" />

                <TextBlock IsVisible="{Binding HasPromotion}"
                           TextWrapping="Wrap">
                    <Run FontWeight="Bold"
                         Text="{Binding PromotedName}" />
                    <Run Text=" will be promoted to default." />
                </TextBlock>

                <TextBlock IsVisible="{Binding IsLastInstallation}"
                           Text="No installations will remain."
                           TextWrapping="Wrap" />

                <TextBlock Foreground="Red"
                           IsVisible="{Binding ErrorMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"
                           Text="{Binding ErrorMessage}"
                           TextWrapping="Wrap" />

                <layout:UniformStack HorizontalAlignment="Right"
                                     Margin="0 5"
                                     Spacing="8">
                    <Button Command="{Binding DeleteCommand}"
                            Content="Delete"
                            MinWidth="75" />
                    <Button Command="{Binding CancelCommand}"
                            Content="Cancel"
                            IsCancel="True"
                            IsDefault="True"
                            MinWidth="75" />
                </layout:UniformStack>
            </StackPanel>
        </DockPanel>
    </StackPanel>
</ClassicWindow>
```

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/DeleteInstallationDialogView.axaml.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>The Win95-styled Delete Installation confirmation. Closes itself when the view model raises <c>CloseRequested</c>, returning whether the delete happened.</summary>
public sealed partial class DeleteInstallationDialogView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public DeleteInstallationDialogView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs eventArgs)
    {
        base.OnDataContextChanged(eventArgs);

        if (DataContext is not DeleteInstallationDialogViewModel viewModel)
        {
            return;
        }

        viewModel.CloseRequested -= OnCloseRequested;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, bool deleted) => Close(deleted);
}
```

- [ ] **Step 4: Add the dialogue-service member**

`IDialogService`:

```csharp
    /// <summary>Opens the Delete Installation confirmation (SDD §7.2.4, §9.5) for the given installation.</summary>
    /// <param name="installationId">The installation to delete.</param>
    /// <returns><see langword="true" /> when the installation was deleted; otherwise <see langword="false" />.</returns>
    Task<bool> ShowDeleteInstallationAsync(Guid installationId);
```

`AvaloniaDialogService`:

```csharp
    /// <inheritdoc />
    public async Task<bool> ShowDeleteInstallationAsync(Guid installationId)
    {
        Window? owner = ResolveOwner();

        if (owner is null)
        {
            return false;
        }

        DeleteInstallationDialogViewModel vm = _services.GetRequiredService<DeleteInstallationDialogViewModel>();

        await vm.InitialiseAsync(installationId);

        DeleteInstallationDialogView view = new()
        {
            DataContext = vm
        };

        return await view.ShowDialog<bool>(owner);
    }
```

`NoOpDialogService`: `public Task<bool> ShowDeleteInstallationAsync(Guid installationId) => Task.FromResult(false);`
`AddDesktop`: `services.AddTransient<DeleteInstallationDialogViewModel>();`

- [ ] **Step 5: Wire the manager's Delete command**

```csharp
    [ RelayCommand(CanExecute = nameof(CanExecuteSelectionCommand)) ]
    private async Task DeleteAsync()
    {
        if (_dialogs is null
            || Grid.SelectedRow is null)
        {
            return;
        }

        if (await _dialogs.ShowDeleteInstallationAsync(Grid.SelectedRow.Id))
        {
            HasChanges = true;
        }
    }
```

- [ ] **Step 6: Build, run the architecture tests, and commit**

Run: `dotnet build Erdmier.ZooTycoonLauncher.slnx -nologo -v q` then `dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture -nologo`
Expected: 0 warnings / 0 errors; 13 PASS.

Manual check: deleting the default names the promoted installation; deleting the last shows "No installations will remain."; Cancel is the default button; after Delete the
grid drops the row and re-bolds the new default.

```bash
git add Source/Erdmier.ZooTycoonLauncher.Desktop
git commit -m "feat(✨): add the Delete Installation confirmation"
```

---

## Task 8: Fix Installation dialogue

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/FixInstallationDialogViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/FixInstallationDialogView.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/FixInstallationDialogView.axaml.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs`, `…/Composition/AvaloniaDialogService.cs`, `…/Composition/DesktopServiceCollectionExtensions.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/PlayViewModel.cs` (NoOp)
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/InstallationManagerDialogViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`

**Interfaces:**
- Consumes: `GetInstallationByIdQuery`; `VerifyInstallationQuery(Guid) → ErrorOr<VerificationResult>` (re-probes and persists drift, publishing on drift — Task 1);
  `RelocateInstallationCommand(Guid InstallationId, string NewPath) → ErrorOr<RelocateInstallationResult(InstallationValidity NewValidity)>` (rejects no-`zoo.exe` folders —
  Task 2); `IDialogService.PickFolderAsync(string?)`; `InstallationDialogMessages`.
- Produces: `IDialogService.ShowFixInstallationAsync(Guid) → Task<bool>`.

- [ ] **Step 1: Add the global usings**

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Installations.Relocate;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Verify;
```

- [ ] **Step 2: Create the Fix view model**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/FixInstallationDialogViewModel.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     View model for the Fix Installation modal (SDD §7.2.5, §9.5). Re-probes the installation folder on open, and lets the user relocate an installation whose
///     <c>zoo.exe</c> is missing. INI repair (Create) is deferred to the INI Config slice, so the INI box reports status only. <see cref="HasChanges" /> tells the
///     caller whether anything was persisted — a relocation, or drift found by the re-probe.
/// </summary>
public sealed partial class FixInstallationDialogViewModel : ViewModelBase
{
    private readonly IDialogService? _dialogs;

    private readonly ILogger<FixInstallationDialogViewModel> _logger;

    private readonly IMediator _mediator;

    private Guid _installationId;

    private string? _path;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">The dialogue service — used for the folder picker behind Locate.</param>
    /// <param name="logger">Logger for unexpected load and relocation failures.</param>
    public FixInstallationDialogViewModel(IMediator mediator, IDialogService dialogs, ILogger<FixInstallationDialogViewModel> logger)
    {
        _mediator = mediator;
        _dialogs  = dialogs;
        _logger   = logger;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public FixInstallationDialogViewModel()
        : this(null!, null!, NullLogger<FixInstallationDialogViewModel>.Instance)
    { }

    /// <summary>The load error, or <see langword="null" /> when the installation loaded.</summary>
    [ ObservableProperty ]
    public partial string? ErrorMessage { get; set; }

    /// <summary>The most recent Locate error (for example, a folder without <c>zoo.exe</c>), shown inside the Fix EXE box.</summary>
    [ ObservableProperty ]
    public partial string? ExeErrorMessage { get; set; }

    /// <summary><see langword="true" /> when anything was persisted whilst the dialogue was open. Read by the dialogue service once the window closes.</summary>
    public bool HasChanges { get; private set; }

    /// <summary><see langword="true" /> when <c>zoo.exe</c> is present in the installation folder.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(LocateCommand)) ]
    public partial bool HasExe { get; set; }

    /// <summary><see langword="true" /> when <c>zoo.ini</c> is present in the installation folder.</summary>
    [ ObservableProperty ]
    public partial bool HasIni { get; set; }

    /// <summary><see langword="true" /> while a relocation is in flight.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(LocateCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary><see langword="true" /> once the installation has loaded and been re-probed.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(LocateCommand)) ]
    public partial bool IsLoaded { get; set; }

    /// <summary>
    ///     Loads the installation and re-probes its folder — the stored flags are only refreshed at boot, so they can be stale. Must be awaited by the dialogue service
    ///     before the window is shown.
    /// </summary>
    /// <param name="installationId">The installation to fix.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task InitialiseAsync(Guid installationId, CancellationToken cancellationToken = default)
    {
        _installationId = installationId;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_mediator is null)
        {
            return;
        }

        try
        {
            ErrorOr<InstallationSummary> summary = await _mediator.Send(new GetInstallationByIdQuery(installationId), cancellationToken);

            if (summary.IsError)
            {
                ErrorMessage = InstallationDialogMessages.InstallationMissing;

                return;
            }

            ErrorOr<VerificationResult> verification = await _mediator.Send(new VerifyInstallationQuery(installationId), cancellationToken);

            if (verification.IsError)
            {
                ErrorMessage = InstallationDialogMessages.InstallationMissing;

                return;
            }

            _path = summary.Value.Path;

            HasExe = verification.Value.HasExe;
            HasIni = verification.Value.HasIni;

            // VerifyInstallationQuery persists drift, so a difference from the stored flags means the row changed.
            HasChanges = HasExe != summary.Value.Validity.HasExe || HasIni != summary.Value.Validity.HasIni;

            IsLoaded = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to load installation {InstallationId} for fixing.", installationId);

            ErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteLocate)) ]
    private async Task LocateAsync(CancellationToken cancellationToken)
    {
        if (_dialogs is null)
        {
            return;
        }

        string? chosen = await _dialogs.PickFolderAsync(_path);

        if (string.IsNullOrWhiteSpace(chosen))
        {
            return;
        }

        IsBusy          = true;
        ExeErrorMessage = null;

        try
        {
            ErrorOr<RelocateInstallationResult> result = await _mediator.Send(new RelocateInstallationCommand(_installationId, chosen), cancellationToken);

            if (result.IsError)
            {
                ExeErrorMessage = result.FirstError.Type == ErrorType.NotFound
                                      ? InstallationDialogMessages.InstallationMissing
                                      : result.FirstError.Description;

                return;
            }

            _path = chosen;

            HasExe     = result.Value.NewValidity.HasExe;
            HasIni     = result.Value.NewValidity.HasIni;
            HasChanges = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to relocate installation {InstallationId}.", _installationId);

            ExeErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ RelayCommand ]
    private void Ok() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private bool CanExecuteLocate() => IsLoaded && !HasExe && !IsBusy;

    /// <summary>Raised when the dialogue should close.</summary>
    public event EventHandler? CloseRequested;
}
```

- [ ] **Step 3: Create the Fix view**

```xml
<!-- Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/FixInstallationDialogView.axaml -->
<ClassicWindow xmlns="https://github.com/avaloniaui"
               xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
               xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs"
               xmlns:controls="clr-namespace:Classic.Avalonia.Theme.Controls;assembly=Classic.Avalonia.Theme"
               x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs.FixInstallationDialogView"
               x:DataType="vm:FixInstallationDialogViewModel"
               Icon="/Assets/Win95Icons/warning32.gif"
               Title="Fix Installation"
               Width="460"
               SizeToContent="Height"
               CanResize="False"
               WindowStartupLocation="CenterOwner"
               ShowInTaskbar="False">
    <StackPanel Margin="8"
                Orientation="Vertical"
                Spacing="10">
        <TextBlock Opacity="0.6"
                   Text="Recover an installation by locating missing files or restoring a fresh INI. The launcher never patches zoo.exe."
                   TextWrapping="Wrap" />

        <TextBlock Foreground="Red"
                   IsVisible="{Binding ErrorMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"
                   Text="{Binding ErrorMessage}"
                   TextWrapping="Wrap" />

        <controls:GroupBox Header="Fix EXE"
                           IsVisible="{Binding IsLoaded}">
            <DockPanel HorizontalSpacing="10"
                       Margin="4">
                <Image DockPanel.Dock="Left"
                       Height="32"
                       IsVisible="{Binding HasExe}"
                       Source="/Assets/Win95Icons/ok32.gif"
                       VerticalAlignment="Top"
                       Width="32" />

                <Image DockPanel.Dock="Left"
                       Height="32"
                       IsVisible="{Binding !HasExe}"
                       Source="/Assets/Win95Icons/error32.gif"
                       VerticalAlignment="Top"
                       Width="32" />

                <Button DockPanel.Dock="Right"
                        Command="{Binding LocateCommand}"
                        Content="Locate"
                        MinWidth="75"
                        VerticalAlignment="Top" />

                <StackPanel Orientation="Vertical"
                            Spacing="4">
                    <StackPanel IsVisible="{Binding HasExe}"
                                Orientation="Vertical"
                                Spacing="4">
                        <TextBlock Foreground="Green"
                                   Text="EXE present" />
                        <TextBlock Opacity="0.6"
                                   Text="zoo.exe was located at the expected path."
                                   TextWrapping="Wrap" />
                    </StackPanel>

                    <StackPanel IsVisible="{Binding !HasExe}"
                                Orientation="Vertical"
                                Spacing="4">
                        <TextBlock Foreground="Red"
                                   Text="No EXE found!" />
                        <TextBlock Opacity="0.6"
                                   Text="zoo.exe is missing from this installation's directory. Browse to a folder that contains zoo.exe."
                                   TextWrapping="Wrap" />
                    </StackPanel>

                    <TextBlock Foreground="Red"
                               IsVisible="{Binding ExeErrorMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"
                               Text="{Binding ExeErrorMessage}"
                               TextWrapping="Wrap" />
                </StackPanel>
            </DockPanel>
        </controls:GroupBox>

        <controls:GroupBox Header="Fix INI"
                           IsVisible="{Binding IsLoaded}">
            <DockPanel HorizontalSpacing="10"
                       Margin="4">
                <Image DockPanel.Dock="Left"
                       Height="32"
                       IsVisible="{Binding HasIni}"
                       Source="/Assets/Win95Icons/ok32.gif"
                       VerticalAlignment="Top"
                       Width="32" />

                <Image DockPanel.Dock="Left"
                       Height="32"
                       IsVisible="{Binding !HasIni}"
                       Source="/Assets/Win95Icons/error32.gif"
                       VerticalAlignment="Top"
                       Width="32" />

                <Button DockPanel.Dock="Right"
                        Content="Create"
                        IsEnabled="False"
                        MinWidth="75"
                        VerticalAlignment="Top" />

                <StackPanel Orientation="Vertical"
                            Spacing="4">
                    <StackPanel IsVisible="{Binding HasIni}"
                                Orientation="Vertical"
                                Spacing="4">
                        <TextBlock Foreground="Green"
                                   Text="INI present" />
                        <TextBlock Opacity="0.6"
                                   Text="zoo.ini was found in the installation folder."
                                   TextWrapping="Wrap" />
                    </StackPanel>

                    <StackPanel IsVisible="{Binding !HasIni}"
                                Orientation="Vertical"
                                Spacing="4">
                        <TextBlock Foreground="DarkGoldenrod"
                                   Text="No INI found" />
                        <TextBlock Opacity="0.6"
                                   Text="INI repair arrives with the INI Config slice."
                                   TextWrapping="Wrap" />
                    </StackPanel>
                </StackPanel>
            </DockPanel>
        </controls:GroupBox>

        <Button Command="{Binding OkCommand}"
                Content="OK"
                HorizontalAlignment="Right"
                IsCancel="True"
                IsDefault="True"
                MinWidth="75" />
    </StackPanel>
</ClassicWindow>
```

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/FixInstallationDialogView.axaml.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>The Win95-styled Fix Installation dialogue. Closes itself when the view model raises <c>CloseRequested</c>.</summary>
public sealed partial class FixInstallationDialogView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public FixInstallationDialogView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs eventArgs)
    {
        base.OnDataContextChanged(eventArgs);

        if (DataContext is not FixInstallationDialogViewModel viewModel)
        {
            return;
        }

        viewModel.CloseRequested -= OnCloseRequested;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();
}
```

- [ ] **Step 4: Add the dialogue-service member**

`IDialogService`:

```csharp
    /// <summary>Opens the Fix Installation dialogue (SDD §7.2.5, §9.5) for the given installation.</summary>
    /// <param name="installationId">The installation to fix.</param>
    /// <returns><see langword="true" /> when anything was persisted (a relocation, or drift found by the re-probe); otherwise <see langword="false" />.</returns>
    Task<bool> ShowFixInstallationAsync(Guid installationId);
```

`AvaloniaDialogService` (returns `vm.HasChanges`, so closing with the title-bar ✕ after a relocation still reports the change):

```csharp
    /// <inheritdoc />
    public async Task<bool> ShowFixInstallationAsync(Guid installationId)
    {
        Window? owner = ResolveOwner();

        if (owner is null)
        {
            return false;
        }

        FixInstallationDialogViewModel vm = _services.GetRequiredService<FixInstallationDialogViewModel>();

        await vm.InitialiseAsync(installationId);

        FixInstallationDialogView view = new()
        {
            DataContext = vm
        };

        await view.ShowDialog(owner);

        return vm.HasChanges;
    }
```

`NoOpDialogService`: `public Task<bool> ShowFixInstallationAsync(Guid installationId) => Task.FromResult(false);`
`AddDesktop`: `services.AddTransient<FixInstallationDialogViewModel>();`

- [ ] **Step 5: Wire the manager's Fix command**

```csharp
    [ RelayCommand(CanExecute = nameof(CanExecuteFixCommand)) ]
    private async Task FixAsync()
    {
        if (_dialogs is null
            || Grid.SelectedRow is null)
        {
            return;
        }

        if (await _dialogs.ShowFixInstallationAsync(Grid.SelectedRow.Id))
        {
            HasChanges = true;
        }
    }
```

Update the class summary: all five commands are now live.

- [ ] **Step 6: Build, run the architecture tests, and commit**

Run: `dotnet build Erdmier.ZooTycoonLauncher.slnx -nologo -v q` then `dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture -nologo`
Expected: 0 warnings / 0 errors; 13 PASS.

Manual check: Fix on a No-EXE row → Locate a folder without `zoo.exe` shows the red inline error and nothing moves; Locate a folder with `zoo.exe` flips the box to present;
INI Create is always disabled; a missing INI shows "INI repair arrives with the INI Config slice."

```bash
git add Source/Erdmier.ZooTycoonLauncher.Desktop
git commit -m "feat(✨): add the Fix Installation dialogue for missing zoo.exe"
```

---

## Task 9: Main-window refresh and the Cannot Play entry point

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/PlayViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/GeneralTabViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/GeneralTabView.axaml`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs`

**Interfaces:**
- Produces: `PlayViewModel.InstallationId` (`Guid`); `PlayViewModel` constructor gains `Func<CancellationToken, Task> openInstallationManagerAsync` after `rebootAsync`;
  `GeneralTabViewModel` constructor gains the same callback as its last parameter and exposes `OpenInstallationManagerCommand`.

- [ ] **Step 1: Carry the manager callback through `PlayViewModel` into the General tab**

`GeneralTabViewModel` — add a field and extend the public constructor:

```csharp
    private readonly Func<CancellationToken, Task>? _openInstallationManagerAsync;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The installation whose general information is displayed.</param>
    /// <param name="canPlay"><see langword="true" /> when the owning <see cref="Boot.PlayViewModel" /> is in the ReadyToPlay state; drives <see cref="CanPlay" />.</param>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="openInstallationManagerAsync">Opens the Installation Manager through the main window, which refreshes the open installation afterwards.</param>
    public GeneralTabViewModel(InstallationSummary installation, bool canPlay, IMediator mediator, Func<CancellationToken, Task> openInstallationManagerAsync)
        : this(installation, canPlay)
    {
        _mediator                     = mediator;
        _openInstallationManagerAsync = openInstallationManagerAsync;
    }
```

and the command (after `LaunchAsync`):

```csharp
    // Cannot Play's "Open Installation Manager…" button — the natural route into Fix (SDD §9.1).
    [ RelayCommand ]
    private Task OpenInstallationManagerAsync(CancellationToken cancellationToken)
        => _openInstallationManagerAsync?.Invoke(cancellationToken) ?? Task.CompletedTask;
```

`PlayViewModel` — add the property, the constructor parameter (after `rebootAsync`, with `<param>` doc "Opens the Installation Manager through the main window (Cannot Play's
button)."), set `InstallationId = installation.Id;`, and pass the callback on:

```csharp
    /// <summary>The identifier of the open installation — used by the main window to re-verify it after the Installation Manager reports a change.</summary>
    public Guid InstallationId { get; }
```

```csharp
        GeneralTab   = new GeneralTabViewModel(installation, canPlay, mediator, openInstallationManagerAsync);
```

The designer constructor leaves `InstallationId` as `Guid.Empty` (no change needed).

- [ ] **Step 2: Bind the Cannot Play button**

In `GeneralTabView.axaml`, add `Command="{Binding OpenInstallationManagerCommand}"` to the `Button` whose content is `Open Installation Manager…` (first attribute, matching the
Launch Game button above it).

- [ ] **Step 3: Refresh the open installation after the manager changes it**

In `MainWindowViewModel.RouteResult`, pass `ManageInstallationsAsync` to both `PlayViewModel` constructions, after the reboot callback:

```csharp
            AppBoot.BootOutcome.ReadyToPlay => new PlayViewModel(result.ActiveInstallation!,
                                                                 canPlay: true,
                                                                 ct => RunBootAsync(result.ActiveInstallation!.Id, ct),
                                                                 ManageInstallationsAsync,
                                                                 _lifecycle,
                                                                 _dialogs,
                                                                 _mediator),
```

(and the same for `CannotPlay` with `canPlay: false`). Replace the body of `ManageInstallationsAsync` after the `changed` check:

```csharp
        bool changed = await _dialogs.ShowInstallationManagerAsync();

        if (!changed)
        {
            return;
        }

        switch (ActiveContent)
        {
            // Re-verify the open installation: a rename shows, Cannot Play becomes Ready after a fix, and a deleted installation falls back to the normal resolution
            // (SDD §7.2.4).
            case PlayViewModel play:
                await RunBootAsync(play.InstallationId, cancellationToken);

                break;

            // Mirrors that state's own post-Add reboot, so a first installation added via the manager is picked up.
            case NoGameInstallationFoundViewModel:
                await RunBootAsync(installationId: null, cancellationToken);

                break;

            // The picker needs nothing: its grid refreshes itself from the change messages.
        }
```

Rewrite the `//` comment block above `ManageInstallationsAsync` to describe exactly these three cases.

- [ ] **Step 4: Full verification**

Run: `dotnet clean Erdmier.ZooTycoonLauncher.slnx -nologo -v q`, `dotnet build Erdmier.ZooTycoonLauncher.slnx -nologo -v q`, then
`dotnet test Erdmier.ZooTycoonLauncher.slnx -nologo`
Expected: 0 warnings / 0 errors; Domain 10, Architecture 13, Application 71, Infrastructure 26 — all PASS.

Manual check: from Ready to Play, File → Installation Manager… → Edit the open installation's name → Close: the main window re-verifies and shows the new name. From Cannot
Play (missing `zoo.exe`), the General tab's "Open Installation Manager…" → Fix → Locate a folder with `zoo.exe` → OK → Close: the window moves to Ready to Play. Deleting the
open installation from the manager falls back to normal resolution.

- [ ] **Step 5: Commit**

```bash
git add Source/Erdmier.ZooTycoonLauncher.Desktop
git commit -m "feat(✨): refresh the open installation after manager changes and wire Cannot Play's manager button"
```
