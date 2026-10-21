# Restore Previous INI Follow-ups Implementation Plan

**Date:** 2026-10-20

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the INI area's open debt — milestone 9's ledger items plus DW-17, DW-18 and DW-27 — with no new user-facing feature.

**Architecture:** Small, independent fixes across the existing INI slice: an ordering tweak in Infrastructure, a reconcile-log and orphan-cleanup change in the
shared Application pipeline steps, Domain/Application/Desktop hygiene, logger plumbing and a shared-activation fix in the INI view models, a hover/focus split for row
help, dialogue polish, stronger tests, and documentation.

**Tech Stack:** .NET 10, Avalonia 11.3 + Classic.Avalonia, CommunityToolkit.Mvvm, Mediator 3.0.2 (source-generated), ErrorOr, EF Core + SQLite, xUnit 2.9 + Shouldly +
NSubstitute.

**Spec:** [`Docs/Specs/Epic-1-MVP/milestone-9.1-restore-previous-ini-follow-ups-design.md`](../../Specs/Epic-1-MVP/milestone-9.1-restore-previous-ini-follow-ups-design.md)
— read it before any task; its decisions 1–3 and calls C1–C6 are binding. The SDD wins wherever neither covers a point. Background: the
[milestone-9 ledger](../../Ledgers/Epic-1-MVP/milestone-9-ledger.md).

## Global Constraints

- Branch `feat/milestone-9.1-restore-previous-ini-follow-ups` (exists; stacked on `feat/milestone-9-restore-previous-ini`). **Commit every task automatically** with the
  message given; do not wait for review first. Commits are unsigned (`git -c commit.gpgsign=false commit`), message ending with the trailer
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; stage only the task's files.
- Commit format `type(emoji): subject` (`feat`→✨, `fix`→🐛, `refactor`→♻️, `style`→🎨, `test`→✅, `docs`→📝), British English.
- **PowerShell** for `dotnet` and `git`; commit messages may go through a Git Bash heredoc. Build and test narrowly
  (`dotnet test Tests/<Project>/<Project>.csproj --filter "FullyQualifiedName~<Class>"`). On a file-lock error, stop and report.
- No background commands, no filesystem-wide searches. **Never write, rename or delete anything under `SampleInstallations/`** (the app may).
- `TreatWarningsAsErrors` and `Nullable` are on. One type per file; no files at a project root; file-scoped namespaces; XML docs on every public type and member;
  `<c>…</c>` without inside whitespace; spaced attributes (`[ Fact ]`); `[ ObservableProperty ]` on `partial` properties; never add `[ UsedImplicitly ]`; blank line
  between statements of different kinds; named arguments on literals in tests; designer constructors keep compiling.
- Log levels (C5): unexpected exceptions caught in a view model → `LogError(ex, …)`; an `ErrorOr` failure shown to the user → `LogWarning(…)` without an exception.
  Messages name the installation id as a structured `{InstallationId}` property.
- Markdown wrapped at 180 characters (measured with PowerShell `.Length`); new documents registered in `Erdmier.ZooTycoonLauncher.slnx`.
- The ledger `Docs/Ledgers/Epic-1-MVP/milestone-9.1-ledger.md` is written by the controller during the run.

## Review Focus

1. **A Save made after `zoo.ini` was edited outside the launcher** — the two archives it creates list newest-first deterministically (C1). Pinned in Task 1.
2. **A write that fails or whose commit is swallowed** — no "INI snapshot reconciled" log line appears (C2). Pinned in Task 2.
3. **Two callers activating the INI tab with different tokens, one cancelled** — the other still gets its load. Pinned in Task 4.
4. **Hovering one row while another has keyboard focus, then moving away** — the footer falls back to the focused row's help, never to blank while a row is focused
   (decision 3). Pinned in Task 5.
5. **A Save on an installation whose cached `HasIni` is false while `zoo.ini` exists** — the Save still succeeds (C3). Pinned in Task 2.

---

## Task 1: History tie-break by trigger (C1)

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/IniSnapshotTransaction.cs` (`ListHistoricalAsync`, ~line 33-35)
- Test: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Persistence/IniSnapshotRepositoryTests.cs`

Ordering becomes `OrderByDescending(CapturedUtc)` → `ThenBy(Trigger)` → `ThenByDescending(Id)`, with a comment: `Trigger` is stored as its name, so ascending puts
`LauncherGui` before `Manual` — the order a Save or Restore archives them within one instant. If EF cannot translate `ThenBy(snapshot => snapshot.Trigger)`, use
`ThenBy(snapshot => snapshot.Trigger == IniSnapshotTrigger.Manual ? 1 : 0)` instead (same comment).

- [x] **Step 1: Rewrite the failing test** `ListHistorical_EqualTimestamps_NewestArchiveFirst`: remove the `Task.Delay`; after `ImportAsync()`, insert two
  `Historical` snapshots directly through the transaction's `AddAsync` (or the context) with the **same** `CapturedUtc`, a `Manual` one with the *larger* explicit id
  and a `LauncherGui` one with the smaller; commit; assert `ListHistoricalAsync` returns the `LauncherGui` one first.
- [x] **Step 2: Run it** — the new test fails:

  ```powershell
  dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj --filter "FullyQualifiedName~IniSnapshotRepositoryTests"
  ```
- [x] **Step 3: Implement** the ordering and comment.
- [x] **Step 4: Run** the same filter, then the whole project → pass.
- [x] **Step 5: Commit** — `fix(🐛): list same-instant INI archives newest first by trigger`

---

## Task 2: Reconcile log after commit, and orphan cleanup in the write pipeline (C2, C3)

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniLoadSteps.cs`, `IniSnapshotService.cs`, `IniWritePipeline.cs`,
  `IniConfig/History/GetIniHistoryHandler.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/` (`IniWritePipelineTests.cs`, `IniSnapshotServiceTests.cs`, `GetIniHistoryHandlerTests.cs`,
  `SaveIniHandlerTests.cs`)

**Interfaces:**
- Produces:
  - `IniLoadSteps.ReconcileAsync(...)` keeps its role but **no longer logs** (drop its logger and installation parameters if nothing else needs them).
  - `public static void LogReconciliation(ILogger logger, GameInstallation installation, IniReconciliation reconciliation)` on `IniLoadSteps` — logs the existing
    `"INI snapshot reconciled for {InstallationId}: {Outcome} ({ChangedKeyCount} changed keys)"` information line when the outcome is not `Unchanged`.

Callers log after their successful `CommitAsync`: `LoadAsync`; `GetIniHistoryHandler`; the pipeline's no-op path (after its commit) and write path (inside the
post-write `try`, straight after `CommitAsync`). The pipeline calls `IIniFileStore.DeleteOrphanedTempFilesAsync(installation.Path, ct)` before `ReadAsync`, inside its
existing read `try`; it does **not** check `HasIni`.

To assert logging, use a test `ILogger` that records entries (add a small `RecordingLogger<T>` test helper if none exists) rather than mocking `ILogger` extension
methods.

- [x] **Step 1: Write the failing tests:**
  - `Write_CommitFails_LogsNoReconciliation` (pipeline: disk drift, `CommitAsync` throws after the write → no "reconciled" entry);
  - `Write_Succeeds_LogsReconciliationAfterCommit`;
  - `Write_DeletesOrphanedTempFilesBeforeReading` (`Received.InOrder`: `DeleteOrphanedTempFilesAsync` → `ReadAsync`);
  - `Save_HasIniFalseButFilePresent_Succeeds` (installation with `HasIni = false`, file on disk → not an error);
  - `Load_StoreFailsAtCommit_LogsNoReconciliation`; `History_StoreFailsAfterReconcile_LogsNoReconciliation`.
- [x] **Step 2: Run** — Application tests filtered `IniWritePipelineTests|IniSnapshotServiceTests|GetIniHistoryHandlerTests|SaveIniHandlerTests` → new tests fail.
- [x] **Step 3: Implement.**
- [x] **Step 4: Run** the whole Application test project → pass.
- [x] **Step 5: Commit** — `fix(🐛): log INI reconciliation only after commit and clean temp files before saving`

---

## Task 3: Domain and Application hygiene (DW-27, part 1; C6)

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Domain/IniKeys/IniKeySpec.cs` (`TryParseBool`, line ~92)
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniErrors.cs` (line ~29),
  `Common/Extensions/ApplicationServiceCollectionExtensions.cs` (summaries, lines ~5-10)
- Test: `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/IniKeys/IniKeySpecTests.cs`, any Application test asserting the old `StoreFailed` text

**Interfaces:**
- Produces: `public static bool TryParseBool(string? value, out bool result)` on `IniKeySpec` — trims; `null`/blank → `false`; `"0"`/`"1"` and `bool.TryParse` spellings
  as today. Existing private callers use it unchanged.

- [x] **Step 1: Write the failing tests** — `TryParseBool` theory: `"1"`→(true,true), `" 0 "`→(true,false), `"true"`→(true,true), `"False"`→(true,false),
  `"2"`→(false,_), `null`→(false,_), `""`→(false,_).
- [x] **Step 2: Run** — Domain tests filtered `IniKeySpecTests` → compile failure.
- [x] **Step 3: Implement** the public method; set `IniErrors.StoreFailed` to
  `$"The installation's settings history could not be read or updated: {message}"` (update any test asserting the old text); rewrite both
  `ApplicationServiceCollectionExtensions` summaries to list Mediator, the validators, the validation behaviour, `IniReconciler`, `IniWritePipeline` and
  `IIniSnapshotService`.
- [x] **Step 4: Run** Domain and Application test projects → pass.
- [x] **Step 5: Commit** — `refactor(♻️): expose IniKeySpec.TryParseBool and correct INI error and registration wording`

---

## Task 4: INI view-model loggers and the shared activation token (DW-17)

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/IniConfigTabViewModel.cs`, `ViewModels/IniConfig/IniEditorViewModel.cs`,
  `ViewModels/Dialogs/RestorePreviousIniDialogViewModel.cs`, `ViewModels/Boot/PlayViewModel.cs`, `ViewModels/MainWindowViewModel.cs` (constructor and the
  `PlayViewModel` creation, ~lines 364/373)
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/` — `Tabs/IniConfigTabViewModelTests.cs`, `IniConfig/IniEditorViewModelTests.cs`,
  `Dialogs/RestorePreviousIniDialogViewModelTests.cs`, `Boot/PlayTestData.cs`, `Boot/PlayViewModelTests.cs`, and every other construction site

**Interfaces:**
- Produces (append parameters; keep existing order):
  - `IniConfigTabViewModel(..., ILogger<IniConfigTabViewModel> logger, ILogger<IniEditorViewModel> editorLogger)`
  - `IniEditorViewModel(Guid installationId, IMediator mediator, IDialogService dialogs, IniConfigResult initial, ILogger<IniEditorViewModel> logger)`
  - `RestorePreviousIniDialogViewModel(IMediator mediator, IDialogService dialogs, ILogger<RestorePreviousIniDialogViewModel> logger)`
  - `PlayViewModel(...)` and `MainWindowViewModel(...)` gain `ILogger<IniConfigTabViewModel>` and `ILogger<IniEditorViewModel>` and hand them on.
  - Designer constructors pass `NullLogger<T>.Instance`.

Log per the Global Constraints in: the tab's load catch and `result.IsError` branch; the editor's save catch and save `ErrorOr` error path, reload catch and reload
`result.IsError` branch, restore-open catch; the dialogue's history-load catch and restore catch (and its error-result branches, at `Warning`).

Shared activation: `ActivateAsync` starts the shared `LoadAsync(_mediator, CancellationToken.None)` and returns `_activation.WaitAsync(cancellationToken)` (for both the
first and later callers). Update the `ActivateAsync` XML doc to name its three skips (no INI, pending edits, no mediator) and that each caller's token cancels only
its own wait.

- [x] **Step 1: Write the failing tests:**
  - `ConcurrentActivation_FirstCallerCancels_SecondStillLoads` (first call's token cancelled while the mediator `Send` is pending → the second call completes and
    `Content` is the editor);
  - `ConcurrentActivation_SecondCallerCancels_LoadContinues` (second call's awaited task is cancelled; the first completes with the editor);
  - `Activate_LoadThrows_LogsError` (tab), `Save_Throws_LogsError` and `Reload_Fails_LogsWarning` (editor), `Initialise_QueryThrows_LogsError` (dialogue) — with a
    recording logger.
- [x] **Step 2: Run** — Desktop tests → compile failure or new tests fail.
- [x] **Step 3: Implement** the constructors, logging and activation change; update every construction site (DI resolves `ILogger<T>` automatically).
- [x] **Step 4: Run** the Desktop and Architecture test projects; build the Desktop project → pass.
- [x] **Step 5: Commit** — `fix(🐛): log INI view-model failures and let each caller cancel only its own INI activation`

---

## Task 5: Hover and focus help (DW-18, decision 3)

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/Fields/IniFieldViewModel.cs` (`IsHelpActive`, ~34-36),
  `ViewModels/IniConfig/IniEditorViewModel.cs` (`OnFieldPropertyChanged` ~362-376, `OnSelectedSectionChanged` ~210-221),
  `Views/IniConfig/IniSectionView.axaml.cs` (~14-28)
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/IniConfig/IniEditorViewModelTests.cs` and any test using `IsHelpActive`

**Interfaces:**
- Produces: `IniFieldViewModel` replaces `IsHelpActive` with `[ ObservableProperty ] public partial bool IsHovered` and `IsFocused`. The view's pointer events set
  `IsHovered`, focus events set `IsFocused`. The editor tracks `_hoveredField` and `_focusedField`; `ActiveHelp = (_hoveredField ?? _focusedField)?.Help`. Changing
  section clears both fields' flags and both references.

- [x] **Step 1: Write the failing tests:**
  - `Help_PointerLeavesFocusedRow_KeepsItsHelp`;
  - `Help_HoverAnotherRowWhileOneIsFocused_ShowsTheHoveredHelp`, then hover ends → the focused row's help;
  - `Help_NeitherHoveredNorFocused_ShowsTheFooterState`;
  - `Help_SectionChange_ClearsHoverAndFocus`. Rename/adjust existing `IsHelpActive` tests.
- [x] **Step 2: Run** — Desktop tests filtered `IniEditorViewModelTests` → fail.
- [x] **Step 3: Implement.**
- [x] **Step 4: Run** the Desktop test project; build the Desktop project → pass.
- [x] **Step 5: Commit** — `fix(🐛): keep a focused INI row's help when the pointer leaves it`

---

## Task 6: Desktop hygiene (DW-27, part 2)

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniEditorCatalogue.cs` (`[ai]` footnote ~123; language rows ~153-154),
  `ViewModels/IniConfig/Fields/IniToggleFieldViewModel.cs` (`IsTrue` ~38-47), `ViewModels/IniConfig/Fields/IniLanguageFieldViewModel.cs` (designer ctor ~31-36,
  `SelectedOption` ~42-63), `ViewModels/Tabs/IniConfigTabViewModel.cs` (`Content` ~45-47), `Composition/IDialogService.cs` (`ShowErrorAsync` doc ~59-62)
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/IniConfig/Fields/IniFieldViewModelTests.cs`, `IniConfig/IniEditorViewModelTests.cs` (~345),
  `IniConfig/IniEditorCatalogueTests.cs`

Values:
- `[ai]` footnote: `"Raising this above the stock 1,000 limit can make guest pathing thrash; values above 2,500 need a community AI patch."`
- Language rows and the two re-spelling tests use `IniEditorCatalogue.LanguageId` / `SubLanguageId`.
- The toggle uses `IniKeySpec.TryParseBool(value, out bool parsed) && parsed`; `IsTrue` is deleted.
- `SelectedOption` raises `PropertyChanged(SelectedOption)` exactly once per set, after both numbers are written; the designer constructor loads the default
  `lang`/`sublang` values so a selection shows.
- `Content` setter becomes private (`[ ObservableProperty ] public partial ViewModelBase Content { get; private set; }`).
- `ShowErrorAsync` doc: *"Shows a modal error message box owned by the active window. With no owner window, shows a non-modal box and returns without waiting."*

- [x] **Step 1: Write the failing tests** — `SelectedOption_Set_RaisesChangeOnceAfterBothWrites` (count `SelectedOption` notifications and assert the pair at
  notification time); `LanguageDesigner_ShowsTheDefaultSelection`; catalogue test for the footnote text.
- [x] **Step 2: Run** → fail.
- [x] **Step 3: Implement** all items above.
- [x] **Step 4: Run** the Desktop test project; build the Desktop project → pass.
- [x] **Step 5: Commit** — `refactor(♻️): tidy the INI editor's catalogue, language picker and dialogue docs`

---

## Task 7: Restore Previous INI dialogue polish

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/RestorePreviousIniDialogView.axaml` (diff area ~96-148; mono runs ~19, 50, 72, 129)
- Modify (if the screenshot changes): `Docs/UserInterfaceDesign/ImplementationScreenshots/RestorePreviousIniDialogue.png`

Requirements (spec §3.5): the diff header stays column-aligned with the rows when the vertical scrollbar shows (header inside the scrolling area as a sticky header,
or the scrollbar width reserved on the header — builder's choice); every mono run uses `FontFamily="Lucida Console, Courier New, monospace"` (C4); each diff row has a
1 px dotted light-grey bottom separator (`dialogs.jsx:485`).

- [x] **Step 1: Implement**; build the Desktop project; run the Architecture tests.
- [ ] **Step 2: Drive it** (if desktop control is available): launch the app, open an installation with enough history that a snapshot's diff scrolls (save several
  different settings in one Save first), open `Restore previous…`, confirm header alignment with the scrollbar visible, the font and the separators; replace the
  screenshot if it changed visibly; close the app. Without desktop control, say so in the report — the controller drives it.
- [x] **Step 3: Commit** — `style(🎨): align the restore diff header and match the prototype's font and row separators`

---

## Task 8: Test strength (milestone 9's deferred minors)

**Files:** test files only, plus Save's `unchangedCount` fold in `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniWritePipeline.cs`.

Add or tighten (spec §3.7), each as its own named test or assertion:
- Domain `IniSnapshotComparerTests` / `ZooIniDefaultsTests`: `EveryUserSetting_HasANonNullDefault`; an ordering test whose registry order disagrees with section
  order — if no such pair exists, a test asserting the registry is already section-ordered, with a comment saying why; `Compare_KeptKeys_FollowSectionThenRegistryOrder`;
  `Compare_SnapshotValuePresentButNull_ComparesAsTheDefault`.
- Infrastructure: `GetById_ReturnsTheOriginalSnapshot`.
- Application: `Load_CountThrows_ReturnsStoreFailed`, `Save_CountThrows_ReturnsStoreFailedAndWritesNothing`, the no-op Save asserts count before commit
  (`Received.InOrder`), `WriteAsync_SelectorError_ReturnsItAndWritesNothing` also asserts `ArchiveCurrentAsync` not received, `WriteAsync_SelectorThrows_ReturnsStoreFailed`;
  history order test uses distinct timestamps; `Count_NeverOpensATransaction` asserts `result.IsError.ShouldBeFalse()`; Restore tests use a `Historical`-kind fixture,
  assert no archive on an unknown snapshot, and `Restore_ReturnsTheHistoricalCount`. Fold the pipeline's `unchangedCount` local into `historicalCount`.
- Desktop: `RestoreDefaults_IsDisabledWhenEverythingIsAlreadyDefault` asserts `CanExecute` false **before** executing and `HasPendingChanges` false after;
  `RestoreDefaults_WhileBusy_IsDisabled`; `RestoreDefaults_Designer_IsDisabled`; the `No` path proves `DiscardChanges` ran (dialogue returns `null`, or assert inside the
  `ShowRestorePreviousIniAsync` callback); the `Yes` path asserts save-before-open with `Received.InOrder`; replace the vacuous `ResetCommand` assertion in
  `RestorePrevious_WhileDialogueOpen_EditorIsBusy` with one that holds pending edits; `HistoryEntries_SummaryFails_DoesNotQueryTheCount`.

- [x] **Step 1: Write the tests**; run each project's filter — every new test passes against current code (these pin existing behaviour); a test that *fails* is a real
  defect: fix it in the owning source file and say so in the report.
- [x] **Step 2: Run** all five test projects → pass.
- [x] **Step 3: Commit** — `test(✅): strengthen the INI history, restore and editor tests`

---

## Task 9: Documentation

**Files:**
- Modify: `Docs/UserInterfaceDesign/conventions.md` (§4.1, ~lines 170-195), `Docs/Specs/Epic-1-MVP/milestone-9-restore-previous-ini-design.md` (§7.4 ~line 225),
  `Docs/Specs/1-zoo-tycoon-launcher-master-design.md` (§9.3.2 ~1382; §13.5 DW-17, DW-18, DW-27; revision 1.10 row), `Docs/TestPlans/manual-end-to-end-tests.md`

- conventions §4.1: icons live in `Desktop/Assets/Win95Icons/` under their committed names; remove "not yet committed" and the `Assets/Icons/` copy task; the table
  maps each site to the file its view actually uses (read each view's `Icon=`/`Source=`), marking sites whose view does not exist yet.
- Milestone 9 spec §7.4: replace the "while loading" sentence with "The history loads before the window opens."
- SDD §9.3.2: the row's pointer sets the field's `IsHovered` and focus sets `IsFocused`; the footer shows the hovered row's help, else the focused row's, else the
  dirty/saved state. Delete DW-17, DW-18, DW-27 from §13.5 and extend the revision 1.10 row to say so and to list this milestone's amendments.
- Manual test plan: under `# Restore Previous INI`, add stub *10. Help on hover and focus* in the existing stub format.

- [x] **Step 1: Apply the edits; check wrap** —
  `Get-Content <file> | Where-Object { $_.Length -gt 180 -and -not $_.StartsWith('|') }` returns nothing for each file.
- [x] **Step 2: Commit** — `docs(📝): bring the icon conventions and the SDD in line with milestone 9.1`

---

## Final verification

- [x] `dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx` → 0 warnings, 0 errors.
- [x] `dotnet test Erdmier.ZooTycoonLauncher.slnx` → all pass.
- [ ] The dialogue polish and the hover/focus help are checked in the running app.
