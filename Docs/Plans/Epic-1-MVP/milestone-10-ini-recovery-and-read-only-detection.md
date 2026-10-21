# INI Recovery and Read-only Detection Implementation Plan

**Date:** 2026-10-21

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let an installation with no `zoo.ini` get one (fresh defaults, or a located copy) from the INI tab and the Fix dialogue, and explain a read-only
installation when it opens instead of failing the first Save with "access denied".

**Architecture:** A Domain default-document factory and a write-access enumeration; Application commands `CreateDefaultIniCommand` / `LocateIniCommand` sharing one
create-then-import step, a write probe consulted at boot and on write failure; Infrastructure create-only write, external read and probe through `IFileSystem`;
Desktop buttons on the No INI box and the Fix dialogue, a re-boot that reopens on the INI tab, and a read-only strip.

**Tech Stack:** .NET 10, Avalonia 11.3 + Classic.Avalonia, CommunityToolkit.Mvvm, Mediator 3.0.2 (source-generated), ErrorOr, FluentValidation, EF Core + SQLite,
System.IO.Abstractions, xUnit 2.9 + Shouldly + NSubstitute.

**Spec:** [`Docs/Specs/Epic-1-MVP/milestone-10-ini-recovery-and-read-only-detection-design.md`](../../Specs/Epic-1-MVP/milestone-10-ini-recovery-and-read-only-detection-design.md)
— read it before any task; its decisions 1–5 and calls C1–C10 are binding, and its §4 C9 copy is used verbatim. The SDD wins wherever neither covers a point.

## Global Constraints

- Branch `feat/milestone-10-ini-recovery-and-read-only-detection` (exists). **Commit every task automatically** with the message given; do not wait for review first.
  Commits are unsigned (`git -c commit.gpgsign=false commit`), message ending with the trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; stage only
  the task's files.
- Commit format `type(emoji): subject` (`feat`→✨, `fix`→🐛, `test`→✅, `docs`→📝), British English.
- **PowerShell** for `dotnet` and `git`; commit messages may go through a Git Bash heredoc. Build and test narrowly
  (`dotnet test Tests/<Project>/<Project>.csproj --filter "FullyQualifiedName~<Class>"`). On a file-lock error, stop and report.
- No background commands, no filesystem-wide searches. **Never write, rename or delete anything under `SampleInstallations/`** (the app may).
- `TreatWarningsAsErrors` and `Nullable` are on. One type per file; folder mirrors namespace; no files at a project root; file-scoped namespaces; XML docs on every
  public type and member; `<c>…</c>` without inside whitespace; spaced attributes (`[ Fact ]`); `[ ObservableProperty ]` on `partial` properties, `[ RelayCommand ]`
  on private async methods; never add `[ UsedImplicitly ]`; blank line between statements of different kinds; named arguments on literals in tests; designer
  constructors keep compiling; every XAML file keeps `x:DataType`; every new `*ViewModel` has a sibling view.
- File system access only through `IFileSystem`; INI text is Latin-1 everywhere (as `IniFileStore` already does).
- Log levels: an unexpected exception caught → `LogError(ex, …)`; an `ErrorOr` failure shown to the user → `LogWarning(…)`; structured `{InstallationId}`.
- Markdown wrapped at 180 characters (PowerShell `.Length`); new documents registered in `Erdmier.ZooTycoonLauncher.slnx`.
- The ledger `Docs/Ledgers/Epic-1-MVP/milestone-10-ledger.md` is written by the controller during the run.

## Review Focus

1. **`zoo.ini` appears between the box showing and the click** (the user restored it, or the game made it) — the action never overwrites it, says so, and the
   window picks the file up. Pinned in Tasks 2 and 3.
2. **An installation that once had `zoo.ini` and still has snapshots** — Create keeps `Original`, archives the old `Current` as `Manual`, so old settings stay
   restorable. Pinned in Task 3.
3. **A located file that is not a `zoo.ini`** (a Notepad `.ini`, an empty file, a huge file) — refused with the "does not look like" message, nothing written. Pinned
   in Task 3.
4. **The probe on a folder the launcher cannot write** — the boot still reaches Ready to Play / Cannot Play, the strip shows, and no probe file is left behind.
   Pinned in Tasks 2 and 4.
5. **A Save on a read-only installation** — the error names the cause and the fix, not the OS's "Access to the path … is denied". Pinned in Task 4.

---

## Task 1: Domain — default document and write access

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniKeys/ZooIniDefaultDocument.cs`, `Source/Erdmier.ZooTycoonLauncher.Domain/Installations/InstallationWriteAccess.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/IniKeys/ZooIniDefaultDocumentTests.cs`

**Interfaces:**
- Produces: `public static class ZooIniDefaultDocument` with `string Render()` — the C1 text — and `IniDocument Create()` (`IniDocument.Parse(Render())`).
- Produces: `public enum InstallationWriteAccess { Unknown, Writable, FolderReadOnly, FileReadOnly }`.

`Render()` format: for each name in `ZooIniDefaults.Sections`, `[name]` then `key=value` for that section's specs (registry order, registry key casing) whose
`DefaultValue` is non-null; one blank line between sections; every line ends `\r\n`, including the last.

- [x] **Step 1: Write the failing tests** in `ZooIniDefaultDocumentTests`:
  - `Render_StartsWithTheUserSection`: with `first = ZooIniDefaults.Keys[0]`, the text starts with `$"[user]\r\n{first.Id.Key}={first.DefaultValue}\r\n"`.
  - `Render_UsesCrlfOnly`: no `\n` without a preceding `\r`; ends with `"\r\n"`; contains `"\r\n\r\n[UI]\r\n"`.
  - `Render_OmitsKeysWithNoDefault`: contains no `lastfile=`, `lastWindowX=`, `progresscalls=`; contains `showUserEntityWarning=0` and `startedFirstTutorial=0`.
  - `Create_ExtractsEveryDefault`: `ZooIniDefaults.ExtractValues(ZooIniDefaultDocument.Create())` holds, for every spec with a non-null `DefaultValue`, that value.
  - `Render_HasNoScenarioSection`: does not contain `[scenario]`.
- [x] **Step 2: Run** the Domain test project with `--filter "FullyQualifiedName~ZooIniDefaultDocumentTests"` → fails to compile.
- [x] **Step 3: Implement** both types (XML docs cite SDD §7.4 and the spec's C1).
- [x] **Step 4: Run** the filter, then the whole Domain project → pass.
- [x] **Step 5: Commit** — `feat(✨): build a default zoo.ini from the INI registry`

---

## Task 2: Infrastructure — create-only write, external read, write probe

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniFileStore.cs`, `Source/Erdmier.ZooTycoonLauncher.Infrastructure/IniConfig/IniFileStore.cs`,
  `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Extensions/InfrastructureServiceCollectionExtensions.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IInstallationWriteProbe.cs`,
  `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Discovery/InstallationWriteProbe.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/IniConfig/IniFileStoreTests.cs`,
  create `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Discovery/InstallationWriteProbeTests.cs`

**Interfaces:**
- Consumes: `InstallationWriteAccess` (Task 1).
- Produces on `IIniFileStore`:
  - `Task<bool> TryCreateAsync(string installationPath, string text, CancellationToken cancellationToken)` — writes `zoo.ini` via a `zoo.ini.tmp.<guid N>` temp file
    and `File.Move(temp, target, overwrite: false)`; returns `false` (and deletes the temp) when `zoo.ini` already exists, before or at the move; other IO and
    access exceptions propagate after the temp is deleted.
  - `Task<string?> ReadExternalAsync(string filePath, long maxBytes, CancellationToken cancellationToken)` — the file's Latin-1 text, or `null` when it is larger
    than `maxBytes`; missing-file / IO / access exceptions propagate.
- Produces: `public interface IInstallationWriteProbe { Task<InstallationWriteAccess> ProbeAsync(string installationPath, CancellationToken cancellationToken); }`,
  registered as a singleton.

Probe algorithm (C6): if `zoo.ini` exists and has `FileAttributes.ReadOnly` → `FileReadOnly`. Otherwise create `zoo.ini.tmp.<guid N>` in the folder (empty
content) and delete it: success → `Writable`; `UnauthorizedAccessException` → `FolderReadOnly`; any other exception → `Unknown` (log at `Debug`). A delete failure
after a successful create is logged as a warning and does not change the result. Never throws.

- [x] **Step 1: Write the failing tests.**
  - `IniFileStoreTests`: `TryCreate_WritesTheTextWhenAbsent` (Latin-1 round trip incl. `é`, returns true, no `*.tmp.*` left); `TryCreate_ExistingFile_ReturnsFalseAndKeepsIt`
    (pre-existing content unchanged, no temp left); `ReadExternal_ReturnsLatin1Text`; `ReadExternal_OverLimit_ReturnsNull` (`maxBytes: 4` on a 5-byte file).
  - `InstallationWriteProbeTests` (real temp directory per test, cleaned up): `Probe_WritableFolder_IsWritable_AndLeavesNoFile`;
    `Probe_ReadOnlyIniAttribute_IsFileReadOnly` (clear the attribute in `finally`); `Probe_MissingFolder_IsUnknown`.
- [x] **Step 2: Run** the two filters → compile failure.
- [x] **Step 3: Implement** the store methods and the probe (inject `IFileSystem` and `ILogger<InstallationWriteProbe>`), and the DI registration.
- [x] **Step 4: Run** the filters, then the whole Infrastructure project and the Architecture project → pass.
- [x] **Step 5: Commit** — `feat(✨): add a create-only INI write and an installation write probe`

---

## Task 3: Application — Create and Locate commands

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniErrors.cs`
- Create under `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/`: `Common/IniReadOnlyText.cs`, `Common/IniRecoverySteps.cs`,
  `Recovery/IniRecoveryResult.cs`, `Recovery/CreateDefaultIniCommand.cs`, `Recovery/CreateDefaultIniHandler.cs`, `Recovery/CreateDefaultIniValidator.cs`,
  `Recovery/LocateIniCommand.cs`, `Recovery/LocateIniHandler.cs`, `Recovery/LocateIniValidator.cs`
- Test: create `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/CreateDefaultIniHandlerTests.cs`, `LocateIniHandlerTests.cs`,
  `LocateIniValidatorTests.cs` (reuse `IniTestData`)

**Interfaces:**
- Consumes: Task 1 (`ZooIniDefaultDocument.Render()`), Task 2 (`TryCreateAsync`, `ReadExternalAsync`, `IInstallationWriteProbe`); existing
  `IInstallationRepository.UpdateAsync`, `IIniSnapshotService.SynchroniseAsync`, `IApplicationEventPublisher.Publish(new InstallationChangedMessage(id))`.
- Produces:
  - `IniErrors.AlreadyExists(string installationPath)` → `Error.Conflict("Ini.AlreadyExists", $"zoo.ini already exists in \"{path}\", so nothing was written.")`;
    `IniErrors.NotAnIni(string sourcePath)` → `Error.Validation("Ini.NotAnIni", $"\"{fileName}\" does not look like a Zoo Tycoon zoo.ini.")`;
    `IniErrors.ReadOnly(InstallationWriteAccess access)` → `Error.Failure("Ini.ReadOnly", IniReadOnlyText.For(access)!)`.
  - `public static class IniReadOnlyText` with `string? For(InstallationWriteAccess access)` — the two C9 strip sentences for `FolderReadOnly` / `FileReadOnly`
    (plain text: `zoo.ini` without backticks), `null` otherwise.
  - `public sealed record IniRecoveryResult(InstallationValidity Validity);`
  - `public sealed record CreateDefaultIniCommand(Guid InstallationId) : ICommand<ErrorOr<IniRecoveryResult>>;`
  - `public sealed record LocateIniCommand(Guid InstallationId, string SourcePath) : ICommand<ErrorOr<IniRecoveryResult>>;`
  - `IniRecoverySteps.CreateAndImportAsync(GameInstallation row, string text, CancellationToken)` → `ErrorOr<IniRecoveryResult>`, shared by both handlers.

`CreateAndImportAsync` (spec §5.1, C2, C4, C8): delete orphaned temp files → `TryCreateAsync`; `false` → `AlreadyExists`; `UnauthorizedAccessException` → probe;
`FolderReadOnly`/`FileReadOnly` → `ReadOnly(access)`, else `WriteFailed(ex.Message)`; `IOException` → `WriteFailed`. On success: `row.HasIni = true`,
`ModifiedUtc = clock now`, `UpdateAsync`, publish `InstallationChangedMessage`, then `SynchroniseAsync` — an error or exception there is logged (`LogWarning` /
`LogError`) and swallowed. Return `new IniRecoveryResult(row.Validity)`. Locate first reads the source with `maxBytes: 1_048_576` (`null` → `NotAnIni`; IO/access
exception → `ReadFailed(ex.Message)`), and requires a section header whose name is in `ZooIniDefaults.Sections` (case-insensitive) — else `NotAnIni`. Both
handlers return `InstallationNotFound` for an unknown id. Validators: id not empty (`"An installation must be specified."`); `SourcePath` not empty and rooted
(`"Choose a file to copy."`).

- [x] **Step 1: Write the failing tests** (substitutes for the store, probe, repository, snapshot service, publisher, clock, logger):
  - `Create_WritesTheDefaultDocument_SetsHasIni_PublishesAndSynchronises` — `TryCreateAsync` received `ZooIniDefaultDocument.Render()`; `UpdateAsync` got
    `HasIni == true`; one `InstallationChangedMessage`; `SynchroniseAsync` called once **after** `UpdateAsync` (`Received.InOrder`); result validity `Valid` for a row
    with `HasExe`.
  - `Create_FileAlreadyExists_ReturnsAlreadyExists_AndChangesNothing` — no `UpdateAsync`, no publish, no synchronise.
  - `Create_AccessDenied_FolderReadOnly_ReturnsReadOnly` — error code `Ini.ReadOnly`, description equals `IniReadOnlyText.For(FolderReadOnly)`.
  - `Create_AccessDenied_ProbeUnknown_ReturnsWriteFailed`.
  - `Create_SynchroniseFails_StillSucceeds_AndLogsWarning` (use `RecordingLogger<T>`).
  - `Create_UnknownInstallation_ReturnsNotFound`.
  - For the surviving-`Current` case (Review Focus 2) add, in `IniReconcilerTests` or `IniSnapshotServiceTests` (whichever already drives the reconcile with a
    stored `Current`), `Synchronise_FileReplacingAnEarlierOne_KeepsOriginal_ArchivesCurrentAsManual` if no existing test already pins it — say which in the report.
  - `Locate_CopiesTheSourceTextUnchanged` — `TryCreateAsync` received exactly the source text.
  - `Locate_NoRecognisedSection_ReturnsNotAnIni`; `Locate_EmptyFile_ReturnsNotAnIni`; `Locate_OverLimit_ReturnsNotAnIni`; `Locate_SourceUnreadable_ReturnsReadFailed`;
    in each, `TryCreateAsync` not received.
  - `LocateIniValidatorTests`: empty id, empty path, relative path each fail with the messages above; a rooted path passes.
- [x] **Step 2: Run** `--filter "FullyQualifiedName~CreateDefaultIni|FullyQualifiedName~LocateIni"` on the Application test project → compile failure.
- [x] **Step 3: Implement** the files above.
- [x] **Step 4: Run** the filter, then the whole Application project and the Architecture project → pass.
- [x] **Step 5: Commit** — `feat(✨): create or locate a missing zoo.ini`

---

## Task 4: Application — probe at boot, read-only errors on write

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Boot/BootResult.cs`, `Boot/BootHandler.cs`, `IniConfig/Common/IniWritePipeline.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Boot/BootHandlerTests.cs`, `IniConfig/IniWritePipelineTests.cs`

**Interfaces:**
- Consumes: `IInstallationWriteProbe`, `IniErrors.ReadOnly`, `InstallationWriteAccess`.
- Produces: `BootResult` gains a trailing `InstallationWriteAccess WriteAccess = InstallationWriteAccess.Unknown`; `BootHandler` and `IniWritePipeline` take
  `IInstallationWriteProbe` in their constructors.

`BootHandler.VerifyAsync`: when `result.HasExe`, probe once and put the value on both the Cannot Play and Ready to Play results (not when `zoo.exe` is missing). The
pipeline's write-failure catch: `UnauthorizedAccessException` → probe → `ReadOnly(access)` for the two read-only values, else today's `WriteFailed`.

- [x] **Step 1: Write the failing tests.**
  - `BootHandlerTests`: `Boot_ReadyToPlay_CarriesTheProbeResult` (`FolderReadOnly`); `Boot_NoIni_StillProbes` (`CannotPlay` carries `Writable`);
    `Boot_NoExe_DoesNotProbe` (probe not received, `Unknown`).
  - `IniWritePipelineTests`: `Write_AccessDenied_FileReadOnly_ReturnsReadOnly` (code `Ini.ReadOnly`, description = `IniReadOnlyText.For(FileReadOnly)`, transaction
    rolled back as today); `Write_AccessDenied_ProbeWritable_ReturnsWriteFailed`.
- [x] **Step 2: Run** `--filter "FullyQualifiedName~BootHandlerTests|FullyQualifiedName~IniWritePipelineTests"` → fail.
- [x] **Step 3: Implement**; update every existing construction site and test fixture for the new constructor parameter (substitute returns `Writable`).
- [x] **Step 4: Run** the filter, then the whole Application project → pass.
- [x] **Step 5: Commit** — `feat(✨): detect read-only installations at boot and explain failed writes`

---

## Task 5: Desktop — No INI box actions and reopening on the INI tab

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs`, `Composition/AvaloniaDialogService.cs`, every other `IDialogService`
  implementation (the stubs in `ViewModels/Tabs/IniConfigTabViewModel.cs` and `ViewModels/IniConfig/IniEditorViewModel.cs`, and the test fakes),
  `ViewModels/IniConfig/IniPlaceholderViewModel.cs`, `Views/IniConfig/IniPlaceholderView.axaml`, `ViewModels/Tabs/IniConfigTabViewModel.cs`,
  `ViewModels/Boot/PlayViewModel.cs`, `ViewModels/MainWindowViewModel.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Tabs/IniConfigTabViewModelTests.cs`, create `IniConfig/IniPlaceholderViewModelTests.cs`,
  `Boot/PlayViewModelTests.cs`

**Interfaces:**
- Consumes: `CreateDefaultIniCommand`, `LocateIniCommand`, `IniRecoveryResult`, `IniErrors` codes (Task 3).
- Produces:
  - `IDialogService.PickFileAsync(string? startPath, string title)` → `Task<string?>` (Avalonia: `OpenFilePickerAsync`, single file, filters `INI files` `*.ini`
    then `All files` `*`, suggested start folder `startPath`; title `Locate zoo.ini`).
  - `IniPlaceholderViewModel.NoIni(Guid installationId, string installationPath, IMediator mediator, IDialogService dialogs,`
    `Func<CancellationToken, Task> reopenOnIniTabAsync, ILogger logger)` replaces the parameterless `NoIni()`; it exposes `bool HasActions` (true only for this case),
    `bool IsBusy`, `CreateCommand`, `LocateCommand` (both disabled while busy). `Loading()` / `Unreadable(…)` unchanged (`HasActions == false`).
  - `PlayViewModel`: `public const int IniConfigTabIndex = 1;` and two new trailing constructor parameters, `Func<CancellationToken, Task> reopenOnIniTabAsync` and
    `int initialTabIndex = 0` (applied after the tabs are built). `MainWindowViewModel.RunBootAsync(Guid?, CancellationToken, int initialTabIndex = 0)` passes the
    index through `RouteResult`; the reopen delegate is `ct => RunBootAsync(id, ct, PlayViewModel.IniConfigTabIndex)`.
  - `IniConfigTabViewModel`'s public constructor gains `Func<CancellationToken, Task> reopenOnIniTabAsync` (and, for Task 7, keep room for the write access).

Placeholder behaviour (spec §5.3, C9): body text and button labels from C9; `Create` is the default button. Create → send `CreateDefaultIniCommand`; Locate →
`PickFileAsync(installationPath, "Locate zoo.ini")`, cancel → nothing, else send `LocateIniCommand`. Success or `Ini.AlreadyExists` → `reopenOnIniTabAsync`. Other
errors → `ShowErrorAsync("Cannot Create zoo.ini" | "Cannot Copy zoo.ini", description)`, log warning, stay. Exceptions → log error, same dialogue with
`"zoo.ini could not be created: {message}"`. The view shows the two buttons under the body (left-aligned, `Spacing="6"`, `MinWidth="75"`) only when `HasActions`.

- [x] **Step 1: Write the failing tests.**
  - `IniPlaceholderViewModelTests`: `Create_Success_ReopensOnTheIniTab`; `Create_AlreadyExists_ReopensOnTheIniTab`; `Create_Fails_ShowsErrorAndStays` (title
    `Cannot Create zoo.ini`, no reopen); `Locate_Cancelled_SendsNothing`; `Locate_Success_SendsTheChosenPathAndReopens`; `Commands_DisabledWhileBusy` (a
    never-completing send; `CanExecute` false for both); `NonNoIniCases_HaveNoActions`.
  - `PlayViewModelTests`: `InitialTabIndex_SelectsTheIniTab`.
  - `IniConfigTabViewModelTests`: the No INI content is a placeholder with `HasActions`.
- [x] **Step 2: Run** the Desktop filters → fail.
- [x] **Step 3: Implement**; update the designer constructors and every construction site.
- [x] **Step 4: Run** the whole Desktop project and the Architecture project → pass.
- [x] **Step 5: Commit** — `feat(✨): offer Create and Locate on the INI tab's No INI box`

---

## Task 6: Desktop — Fix dialogue INI actions

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/FixInstallationDialogViewModel.cs`, `Views/Dialogs/FixInstallationDialogView.axaml`
- Test: the Fix dialogue tests in `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Dialogs/DialogViewModelTests.cs` (or a new `FixInstallationDialogViewModelTests.cs`
  if that file has none — say which)

**Interfaces:**
- Consumes: Task 3 commands, `IDialogService.PickFileAsync` (Task 5).
- Produces: `CreateIniCommand`, `LocateIniCommand` on the view model, enabled when `IsLoaded && HasExe && !HasIni && !IsBusy`; `IniErrorMessage` (string?).

Success sets `HasIni` from the result, `HasChanges = true`, clears `IniErrorMessage`. `Ini.AlreadyExists` re-probes with the existing `VerifyInstallationQuery`
path (as `InitialiseAsync` does) so the group flips to *INI present*, and sets `HasChanges = true`. Other errors set `IniErrorMessage` (shown under the group's
text, maroon, like `ExeErrorMessage`). View: replace the static `Create` button with a right-docked vertical `StackPanel` (`Spacing="6"`) holding `Create` and
`Locate…`; the missing-INI text becomes C9's Fix INI copy.

- [x] **Step 1: Write the failing tests:** `CreateIni_Success_FlipsToIniPresentAndMarksChanged`; `CreateIni_AlreadyExists_Reprobes`; `CreateIni_Fails_ShowsInlineError`;
  `IniActions_DisabledWithoutExe` (an `InvalidNoExeOrIni` installation); `LocateIni_Cancelled_SendsNothing`.
- [x] **Step 2: Run** the filter → fail.
- [x] **Step 3: Implement.**
- [x] **Step 4: Run** the whole Desktop project → pass.
- [x] **Step 5: Commit** — `feat(✨): repair a missing zoo.ini from the Fix Installation dialogue`

---

## Task 7: Desktop — read-only strip and the General tab hint

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/IniConfigTabViewModel.cs`, `Views/Tabs/IniConfigTabView.axaml` (the tab's view, whatever its
  exact file name), `ViewModels/Boot/PlayViewModel.cs`, `ViewModels/MainWindowViewModel.cs`, `ViewModels/Tabs/GeneralTabViewModel.cs` and its view
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Tabs/IniConfigTabViewModelTests.cs`, `Tabs/GeneralTabViewModelTests.cs`

**Interfaces:**
- Consumes: `BootResult.WriteAccess` (Task 4), `IniReadOnlyText.For` (Task 3).
- Produces: `IniConfigTabViewModel.ReadOnlyWarning` (`string?`, from `IniReadOnlyText.For(writeAccess)`) and `HasReadOnlyWarning`; the write access reaches the tab
  through `PlayViewModel`'s constructor (new parameter `InstallationWriteAccess writeAccess = InstallationWriteAccess.Unknown`) from `RouteResult`.

The strip sits above the tab's content (editor, No INI box or placeholder): `warning16`-style icon (`/Assets/Win95Icons/warning32.gif` at 16 px), maroon text,
wrapping, visible only when `HasReadOnlyWarning`. General tab, Cannot Play with only the INI missing: append C10's sentence to the existing message.

- [x] **Step 1: Write the failing tests:** `ReadOnlyWarning_FolderReadOnly_ShowsTheFolderText`; `ReadOnlyWarning_FileReadOnly_ShowsTheFileText`;
  `ReadOnlyWarning_WritableOrUnknown_IsHidden`; `GeneralTab_MissingOnlyIni_PointsAtTheIniTab` (message contains `Use the INI Config tab to create or locate zoo.ini.`).
- [x] **Step 2: Run** → fail.
- [x] **Step 3: Implement.**
- [x] **Step 4: Run** the whole Desktop project and the Architecture project → pass.
- [x] **Step 5: Commit** — `feat(✨): warn on the INI tab when an installation is read-only`

---

## Task 8: Documentation

**Files:**
- Modify: `Docs/Specs/1-zoo-tycoon-launcher-master-design.md`, `Docs/UserInterfaceDesign/conventions.md` (§4.1 icon table: the read-only strip's icon; only if a new
  icon site exists), `Docs/TestPlans/manual-end-to-end-tests.md` (add a short "INI recovery" stub in the existing stub style)

- [x] **Step 1: SDD** per the spec's §8: §7.4 (replace the MVP note and button list with the two actions as built, C2–C4), §7.2.5 (*Fix INI* options; Copy From
  Another out), §7.1.2 Verify (the write probe and its four results), §9.3.1 (No INI case: buttons, read-only strip), §9.5 (*Fix INI* group), §14.5 row 9 (MVP
  mitigation implemented), §13.5 (delete DW-01 and DW-12; add **DW-40 — Copy From Another Installation**: SDD §7.4's third action with its installation selector,
  source "Milestone 10 spec"), §13.2 if it lists the No INI placeholder as message-only. Revision **1.12** (Version, Last updated 21 October 2026 or the commit date,
  and a revision-history row in the 1.11 row's shape).
- [x] **Step 2: Verify** no changed prose line exceeds 180 characters (PowerShell `.Length`).
- [x] **Step 3: Commit** — `docs(📝): document INI recovery and read-only detection in the SDD`

---

## Task 9: Check it in the running app (controller)

Run by the controller, which has desktop control; implementers do not.

- [x] **Step 1: Prepare** a scratch copy of `SampleInstallations/Zoo Tycoon` under the session scratchpad (copy *from* the sample; never write inside it), delete
  the copy's `zoo.ini`, and register it with the launcher's Add dialogue.
- [x] **Step 2: Drive** — the INI tab shows the No INI box with both buttons; Create → the window re-boots onto the INI tab with the editor and the General tab is
  Ready to Play; delete `zoo.ini` again, reopen, Locate the sample's `zoo.ini` (read-only use) → copied, editor shows its values, history holds the archived
  `Current`; mark the copy's `zoo.ini` Read-only, reopen → the file strip shows, and Save gives the read-only message; clear the attribute. Fix dialogue: delete
  `zoo.ini`, Installation Manager → Fix → `Create` flips the group to *INI present*.
- [x] **Step 3: Clean up** — delete the scratch installation through the launcher, close the app, delete the scratch folder.

## Verification

- [x] Clean build with 0 warnings; every test project passes.
- [x] Task 9 checked in the running app, or recorded in the ledger as left to the maintainer.
