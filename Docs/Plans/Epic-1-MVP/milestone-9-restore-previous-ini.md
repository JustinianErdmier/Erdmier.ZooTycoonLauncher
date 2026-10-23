# Restore Previous INI Implementation Plan

**Date:** 2026-10-20

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let the user roll `zoo.ini` back to any earlier version through a Restore Previous INI dialogue, finish the INI editor footer (`Restore previous…`,
`Restore defaults`, `Save`, `Reset`), show Installation Info's real history count, and sweep the app's button labels onto the Win9x ellipsis rule.

**Architecture:** A pure Domain comparer decides both what the dialogue shows and what a restore writes. Save's §8.2 pipeline is extracted from `SaveIniHandler` into a
shared `IniWritePipeline` that Save and the new `RestoreIniSnapshotCommand` both drive with their own edit selector. New snapshot reads on the transaction feed a
`GetIniHistoryQuery`; a non-migrating repository read feeds Installation Info. The Desktop adds a dialogue pair, a generic Yes/No confirmation pair and three editor commands.

**Tech Stack:** .NET 10, Avalonia 11.3 + Classic.Avalonia, CommunityToolkit.Mvvm, Mediator 3.0.2 (source-generated), ErrorOr, FluentValidation, Ardalis.SmartEnum,
EF Core + SQLite, xUnit 2.9 + Shouldly + NSubstitute.

**Spec:** [`Docs/Specs/Epic-1-MVP/milestone-9-restore-previous-ini-design.md`](../../Specs/Epic-1-MVP/milestone-9-restore-previous-ini-design.md) — read it before any
task; its calls C1–C11 are binding. The SDD ([`1-zoo-tycoon-nexus-master-design.md`](../../Specs/1-zoo-tycoon-nexus-master-design.md)) wins wherever neither covers
a point. Layout and copy for the dialogue come from [`HiFiPrototype/src/dialogs.jsx`](../../UserInterfaceDesign/HiFiPrototype/src/dialogs.jsx) lines 427–506.

## Global Constraints

- Branch `feat/milestone-9-restore-previous-ini` (already created). **Commit every task automatically** with the message given; do not wait for review first.
- Commit format `type(emoji): subject` (`feat`→✨, `refactor`→♻️, `test`→✅, `docs`→📝), British English, ending with the trailer
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Use a PowerShell here-string with the closing `'@` at column 0. Stage only the task's files.
- **PowerShell only**, never Bash. Build narrowly (`dotnet build Source/<Project>/<Project>.csproj`) and test narrowly
  (`dotnet test Tests/<Project>/<Project>.csproj --filter "FullyQualifiedName~<Class>"`). On a file-lock error, stop and report; do not investigate.
- No background commands and no filesystem-wide searches. **Never write, rename or delete anything under `SampleInstallations/`.**
- `TreatWarningsAsErrors` and `Nullable` are on: every warning fails the build.
- One type per file, named after the type, in a folder mirroring its namespace; no files at a project root; file-scoped namespaces. Add a namespace to the assembly's
  `GlobalUsings.cs` only once it exists.
- XML doc comments on every public type and member; `<c>…</c>` without inside whitespace; British English everywhere.
- Spaced attributes (`[ Fact ]`, `[ ObservableProperty ]`, `[ RelayCommand ]`); `[ ObservableProperty ]` on `partial` properties; never add `[ UsedImplicitly ]`.
- Blank line between consecutive statements of different kinds. Named arguments on literals in tests (`Section: "user"`), matching the existing tests.
- UTC for every stored timestamp; local time only in Desktop display text, formatted `d MMM yyyy HH:mm` with the `en-GB` culture (spec C6).
- Every XAML file declares `x:DataType`; every public `*ViewModel` in Desktop has a `*View.axaml` sibling and a parameterless designer constructor.
- Markdown hard-wrapped at 180 characters (measure with PowerShell `.Length`); every new document registered in `Erdmier.ZooTycoonLauncher.slnx`.
- The milestone ledger `Docs/Ledgers/Epic-1-MVP/milestone-9-ledger.md` is written by the controller during the run (rulings, plan defects, outstanding items, incidents);
  its first outstanding item is the stale icon section of `Docs/UserInterfaceDesign/conventions.md` §4 (spec §12).

## Review Focus

1. **A snapshot holding an invalid raw value** (`screenwidth=abc`) — shown and restored as the key's default, never written raw (C3). Pinned in Tasks 1 and 6.
2. **`zoo.ini` edited outside the launcher after the dialogue opened** — the restore reconciles first, then still writes the snapshot's values. Pinned in Task 6.
3. **A snapshot that differs from `Current` only in game-managed keys** — no rows, *No differences*, `Restore` disabled; a forced command is a no-op with no archive. Pinned
   in Tasks 1, 6 and 8.
4. **Pressing `Restore` twice quickly** — only one `RestoreIniSnapshotCommand` is sent. Pinned in Task 8.
5. **Installation Info for an installation never opened since this milestone** — `0 snapshots`, and no database file is created. Pinned in Tasks 2 and 11.

---

## File map

| Layer          | Create                                                                                                                                       | Modify                                                                                         |
|----------------|----------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------|
| Domain         | `IniSnapshots/IniSnapshotComparer.cs`, `IniSnapshots/IniSnapshotComparison.cs`, `IniSnapshots/IniDiffRow.cs`                                | —                                                                                              |
| Application    | `IniConfig/Common/IniWritePipeline.cs`, `IniConfig/Common/IniEditSelector.cs`, `IniConfig/History/*`, `IniConfig/Restore/*`, `Common/Models/IniHistoryResult.cs`, `Common/Models/IniHistoryEntry.cs` | `IIniSnapshotTransaction`, `IIniSnapshotRepository`, `IniConfigResult`, `IniSnapshotService`, `SaveIniHandler`, `IniErrors`, `ApplicationServiceCollectionExtensions` |
| Infrastructure | —                                                                                                                                            | `IniSnapshotTransaction`, `IniSnapshotRepository`, `InstallationDbContextFactory`               |
| Desktop        | `ConfirmationPrompt*`, `RestorePreviousIniDialog*`, `Models/IniConfig/IniHistoryRow.cs`, `Models/IniConfig/IniHistoryDiffRow.cs`             | field VMs, `IniEditorViewModel`, `IniEditorView.axaml`, `IDialogService`, `AvaloniaDialogService`, DI, three `NoOpDialogService` copies, Info VM/view, label views |
| Docs           | —                                                                                                                                            | SDD, `manual-end-to-end-tests.md`                                                              |

---

## Task 1: Domain — `IniSnapshotComparer`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniDiffRow.cs`, `IniSnapshotComparison.cs`, `IniSnapshotComparer.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/IniSnapshots/IniSnapshotComparerTests.cs`

**Interfaces:**
- Produces:
  - `public sealed record IniDiffRow(IniKeyId Id, string? Current, string Snapshot);` — `Current` is the effective current value, `null` when `Current` lacks the key;
    `Snapshot` is the snapshot's effective value (never null: every user setting has a default).
  - `public sealed record IniSnapshotComparison(IReadOnlyList<IniDiffRow> Rows, IReadOnlyList<IniKeyId> KeptKeys);`
  - `public static class IniSnapshotComparer` with
    `public static IniSnapshotComparison Compare(IReadOnlyDictionary<IniKeyId, string?> current, IReadOnlyDictionary<IniKeyId, string?> snapshot)` and
    `public static IReadOnlyDictionary<IniKeyId, string?> ValuesOf(IniSnapshot snapshot)` (maps `IniValue.Section`/`Key` to `IniKeyId`).

Rules (spec §4, C1, C3, C4, C8): consider only `ZooIniDefaults.Keys` with `Role == IniKeyRole.UserSetting`; a key absent from `snapshot` (`!ContainsKey`) goes to
`KeptKeys` when `current` has it, else is ignored; a key absent from `current` but present in `snapshot` is always a row; otherwise a row when
`!spec.AreEquivalent(spec.EffectiveValue(current), spec.EffectiveValue(snapshot))`. Order rows and kept keys by `ZooIniDefaults.Sections` index, then by position in
`ZooIniDefaults.Keys`.

- [x] **Step 1: Write the failing tests** — in `IniSnapshotComparerTests` (`public sealed class`, namespace `…Domain.Tests.Unit.IniSnapshots`), with a helper
  `Map(params (string Section, string Key, string? Value)[])`:
  - `Compare_EqualMaps_HasNoRowsOrKeptKeys`
  - `Compare_GameManagedDifference_IsIgnored` — `lastWindowX` differs → `Rows.ShouldBeEmpty()`.
  - `Compare_ChangedUserSetting_IsARowWithBothEffectiveValues` — current `screenwidth=1920`, snapshot `1024` → one row `(user/screenwidth, "1920", "1024")`.
  - `Compare_KeyMissingFromSnapshot_IsAKeptKeyNotARow` — current has `tooltipDelay`, snapshot lacks it → `KeptKeys` is `[UI/tooltipDelay]`, no row.
  - `Compare_KeyMissingFromCurrent_IsARowEvenAtTheDefault` — snapshot `screenwidth=800` (its default), current lacks it → row with `Current == null`, `Snapshot == "800"`.
  - `Compare_InvalidSnapshotValue_ComparesAsTheDefault` — snapshot `screenwidth=abc`, current `800` → no row; current `1024` → row with `Snapshot == "800"`.
  - `Compare_BoolSpellings_AreEquivalent` — `fullscreen` `1` vs `true` → no row (use whichever spellings `IniKeySpec.AreEquivalent` treats as equal for `Bool`).
  - `Compare_OrdersRowsBySectionThenRegistryOrder` — differences in `[UI]` and `[user]` → `[user]` rows first.
  - `ValuesOf_MapsSectionAndKeyCaseInsensitively` — an `IniValue { Section = "USER", Key = "ScreenWidth", Value = "1024" }` is found under `new IniKeyId("user", "screenwidth")`.

- [x] **Step 2: Run to verify they fail** — compile failure from:

  ```powershell
  dotnet test Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj --filter "FullyQualifiedName~IniSnapshotComparerTests"
  ```
- [x] **Step 3: Implement** the three types in `Domain/IniSnapshots/` per the rules above.
- [x] **Step 4: Run to verify they pass** — same command → all pass; then the whole Domain test project passes.
- [x] **Step 5: Commit** — `feat(✨): compare INI snapshots by effective user-setting values`

---

## Task 2: Snapshot reads in the transaction and the repository

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniSnapshotTransaction.cs`, `IIniSnapshotRepository.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/IniSnapshotTransaction.cs`, `IniSnapshotRepository.cs`, `InstallationDbContextFactory.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Persistence/IniSnapshotRepositoryTests.cs`

**Interfaces:**
- Produces on `IIniSnapshotTransaction`:
  - `Task<IReadOnlyList<IniSnapshot>> ListHistoricalAsync(CancellationToken cancellationToken);` — `Historical` only, `CapturedUtc` descending, values included.
  - `Task<IniSnapshot?> GetOriginalAsync(CancellationToken cancellationToken);` — with values.
  - `Task<IniSnapshot?> GetByIdAsync(Guid snapshotId, CancellationToken cancellationToken);` — `Historical` or `Original` only (a `Current` id returns `null`), with values.
  - `Task<int> CountHistoricalAsync(CancellationToken cancellationToken);`
- Produces on `IIniSnapshotRepository`:
  - `Task<int> CountHistoricalAsync(Guid installationId, CancellationToken cancellationToken);` — answers `0` when the installation's database file does not exist,
    **without creating or migrating it**; otherwise opens the existing file without `MigrateAsync` and counts.
- Produces on `InstallationDbContextFactory`: `internal Task<InstallationDbContext?> OpenExistingAsync(Guid installationId, CancellationToken cancellationToken)` —
  `null` when `IFileSystem.File.Exists(_locations.InstallationDatabasePath(installationId))` is false.

- [x] **Step 1: Write the failing integration tests** in `IniSnapshotRepositoryTests` (reuse its `ImportAsync()` helper; archive by calling
  `ArchiveCurrentAsync(IniSnapshotTrigger.LauncherGui, …)` with distinct capture times):
  - `ListHistorical_ReturnsNewestFirstWithValues`
  - `GetOriginal_ReturnsTheFirstImportWithValues`
  - `GetById_ReturnsAHistoricalSnapshot` and `GetById_CurrentOrUnknownId_ReturnsNull`
  - `CountHistorical_CountsOnlyHistorical` (transaction method)
  - `RepositoryCountHistorical_MissingDatabase_ReturnsZeroAndCreatesNoFile` —
    `File.Exists(Path.Combine(_directory, $"{id}.db")).ShouldBeFalse()` after the call.
  - `RepositoryCountHistorical_ExistingDatabase_Counts`
- [x] **Step 2: Run to verify they fail** — compile failure from:

  ```powershell
  dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj --filter "FullyQualifiedName~IniSnapshotRepositoryTests"
  ```
- [x] **Step 3: Implement** the interface members and their EF implementations (`Include(snapshot => snapshot.Values)`, `AsNoTracking()` on the read paths). Add the same
  members to every `IIniSnapshotTransaction` / `IIniSnapshotRepository` fake or substitute helper in the Application tests if any fails to compile (NSubstitute
  substitutes need nothing).
- [x] **Step 4: Run to verify they pass** — same command; then the full Infrastructure and Application test projects pass.
- [x] **Step 5: Commit** — `feat(✨): read historical and original INI snapshots`

---

## Task 3: `IniConfigResult.HistoricalCount`

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/IniConfigResult.cs`, `IniConfig/Common/IniSnapshotService.cs` (line ~103), `IniConfig/Save/SaveIniHandler.cs`
- Modify: every `new IniConfigResult(` in `Source/` and `Tests/` (Desktop designer constructor, `IniEditorTestData`, Application test data)
- Test: `Tests/…Application.Tests.Unit/IniConfig/IniSnapshotServiceTests.cs` (or the existing service test file), `SaveIniHandlerTests.cs`

**Interfaces:**
- Consumes: `IIniSnapshotTransaction.CountHistoricalAsync` (Task 2).
- Produces: `public sealed record IniConfigResult(IReadOnlyDictionary<IniKeyId, string?> Values, DateTime FileLastWriteUtc, int HistoricalCount);`

`LoadAsync` counts inside the transaction it already opens, before `CommitAsync` (spec C9). Save counts after its archive (or, on the no-op path, before its early
commit) — inside the existing store-failure `try`, never after the file write.

- [x] **Step 1: Write the failing tests** — `Load_ReturnsTheHistoricalCount` (substitute `CountHistoricalAsync` returns `3` → `result.Value.HistoricalCount.ShouldBe(3)`);
  `Save_ReturnsTheHistoricalCountAfterArchiving`; `Save_NoOpEdits_ReturnTheUnchangedCount`.
- [x] **Step 2: Run to verify they fail** — Application tests filtered to the two classes → compile failure.
- [x] **Step 3: Implement** the record change and fill it in both places; update every constructor call (designer and test data pass `0` unless a test needs more).
- [x] **Step 4: Run to verify they pass** — `dotnet test` on Application and Desktop test projects → all pass.
- [x] **Step 5: Commit** — `feat(✨): carry the historical snapshot count in INI results`

---

## Task 4: Extract `IniWritePipeline` from `SaveIniHandler`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniEditSelector.cs`, `IniWritePipeline.cs`
- Modify: `IniConfig/Save/SaveIniHandler.cs`, `Common/Extensions/ApplicationServiceCollectionExtensions.cs`
- Test: `Tests/…Application.Tests.Unit/IniConfig/SaveIniHandlerTests.cs` (construction only), new `IniWritePipelineTests.cs`

**Interfaces:**
- Produces:
  - The delegate and the pipeline:

    ```csharp
    public delegate Task<ErrorOr<IReadOnlyDictionary<IniKeyId, string>>> IniEditSelector(IIniSnapshotTransaction transaction,
                                                                                         IReadOnlyDictionary<IniKeyId, string?> reconciledValues,
                                                                                         CancellationToken cancellationToken);

    public sealed class IniWritePipeline
    {
        public IniWritePipeline(TimeProvider clock, IIniFileStore files, IInstallationRepository installations, ILogger<IniWritePipeline> logger,
                                IniReconciler reconciler, IIniSnapshotRepository snapshots);

        public Task<ErrorOr<IniConfigResult>> WriteAsync(Guid installationId, IniEditSelector selectEdits, CancellationToken cancellationToken);
    }
    ```
  - Registered `services.AddScoped<IniWritePipeline>()`.
  - `SaveIniHandler` ctor becomes `(IniWritePipeline pipeline)`; its spec-resolution guard and the `AreEquivalent` raw filter move into its selector.

`WriteAsync` is today's `SaveIniHandler.Handle` body with the edit loop replaced by `await selectEdits(transaction, reconciliation.Values, ct)`: an error from the selector
returns that error (transaction disposed uncommitted); an empty edit set commits and returns the reconciled values; selector exceptions fall into the existing
`StoreFailed` catch. Every log message, error, ordering and the swallowed post-write failure stay as they are.

- [x] **Step 1: Write the failing tests** — `IniWritePipelineTests`: `WriteAsync_SelectorError_ReturnsItAndWritesNothing`
  (`_files.DidNotReceive().WriteAsync(…)`, `transaction.DidNotReceive().CommitAsync(…)`); `WriteAsync_EmptySelection_CommitsWithoutArchiving`.
  Update `SaveIniHandlerTests` construction to `new SaveIniHandler(new IniWritePipeline(…same substitutes…))` — **no assertion changes**.
- [x] **Step 2: Run to verify they fail** — filter `IniWritePipelineTests|SaveIniHandlerTests` → compile failure.
- [x] **Step 3: Implement** the delegate, the pipeline and the slimmed handler; register the pipeline.
- [x] **Step 4: Run to verify they pass** — the whole Application test project passes with every pre-existing `SaveIniHandlerTests` assertion untouched.
- [x] **Step 5: Commit** — `refactor(♻️): extract the INI write pipeline from Save`

---

## Task 5: `GetIniHistoryQuery` and `GetIniHistoryCountQuery`

**Files:**
- Create: `Source/…Application/Common/Models/IniHistoryEntry.cs`, `IniHistoryResult.cs`
- Create: `Source/…Application/IniConfig/History/GetIniHistoryQuery.cs`, `GetIniHistoryHandler.cs`, `GetIniHistoryCountQuery.cs`, `GetIniHistoryCountHandler.cs`
- Modify: `Source/…Application/GlobalUsings.cs` only if a new namespace needs it
- Test: `Tests/…Application.Tests.Unit/IniConfig/GetIniHistoryHandlerTests.cs`, `GetIniHistoryCountHandlerTests.cs`

**Interfaces:**
- Consumes: `IniSnapshotComparer` (Task 1), the Task 2 reads, `IniReconciler.ReconcileAsync`, `IIniFileStore.ReadAsync`.
- Produces:
  - `public sealed record IniHistoryEntry(Guid SnapshotId, IniSnapshotKind Kind, IniSnapshotTrigger Trigger, DateTime CapturedUtc, IniSnapshotComparison Comparison);`
  - `public sealed record IniHistoryResult(IReadOnlyList<IniHistoryEntry> Entries);`
  - `public sealed record GetIniHistoryQuery(Guid InstallationId) : IQuery<ErrorOr<IniHistoryResult>>;`
  - `public sealed record GetIniHistoryCountQuery(Guid InstallationId) : IQuery<ErrorOr<int>>;`

History handler (spec §5.2): installation lookup → read file (`Missing` / `ReadFailed` as `IniSnapshotService.LoadAsync` maps them) → `BeginAsync` → reconcile →
`ListHistoricalAsync` + `GetOriginalAsync` → one entry per snapshot, `Historical` newest first then `Original`, each compared against the **reconciled** values with
`IniSnapshotComparer.Compare(reconciled, IniSnapshotComparer.ValuesOf(snapshot))` → commit → result. Store exceptions → `StoreFailed`. Count handler (spec §5.5, C10):
installation lookup → `IIniSnapshotRepository.CountHistoricalAsync(id)`; exceptions → `StoreFailed`; never reads the file, never reconciles.

- [x] **Step 1: Write the failing tests:**
  - `History_ListsHistoricalNewestFirstThenOriginal`
  - `History_ComparesEachSnapshotAgainstTheReconciledValues` — the disk holds `screenwidth=1280` while `Current` says `800`; a snapshot holding `1280` shows no row.
  - `History_ReconcilesBeforeReading` — `Received.InOrder` reconcile → `ListHistoricalAsync`.
  - `History_UnknownInstallation_ReturnsNotFound`, `History_MissingFile_ReturnsMissing`, `History_StoreThrows_ReturnsStoreFailed`
  - `Count_ReturnsTheRepositoryCount`, `Count_NeverOpensATransaction` (`_snapshots.DidNotReceive().BeginAsync(…)`), `Count_StoreThrows_ReturnsStoreFailed`
- [x] **Step 2: Run to verify they fail** — filter `GetIniHistory` → compile failure.
- [x] **Step 3: Implement** the four types and two handlers.
- [x] **Step 4: Run to verify they pass** — Application test project passes.
- [x] **Step 5: Commit** — `feat(✨): query INI history with per-snapshot differences`

---

## Task 6: `RestoreIniSnapshotCommand`

**Files:**
- Create: `Source/…Application/IniConfig/Restore/RestoreIniSnapshotCommand.cs`, `RestoreIniSnapshotHandler.cs`, `RestoreIniSnapshotValidator.cs`
- Modify: `Source/…Application/IniConfig/Common/IniErrors.cs`
- Test: `Tests/…Application.Tests.Unit/IniConfig/RestoreIniSnapshotHandlerTests.cs`, `RestoreIniSnapshotValidatorTests.cs`

**Interfaces:**
- Consumes: `IniWritePipeline.WriteAsync` (Task 4), `GetByIdAsync` (Task 2), `IniSnapshotComparer` (Task 1).
- Produces:
  - `public sealed record RestoreIniSnapshotCommand(Guid InstallationId, Guid SnapshotId) : ICommand<ErrorOr<IniConfigResult>>;`
  - `public static Error SnapshotNotFound(Guid snapshotId)` on `IniErrors` → `Error.NotFound(code: "Ini.SnapshotNotFound", "The selected version of zoo.ini no longer exists.")`.

The selector reads the snapshot by id (`null` → `SnapshotNotFound`) and returns `Compare(reconciled, ValuesOf(snapshot)).Rows` as `row.Id → row.Snapshot` (spec C8). The
validator requires both ids non-empty.

- [x] **Step 1: Write the failing tests** (reuse `IniTestData` from the Save tests):
  - `Restore_FollowsTheSdd82Order` — `Received.InOrder`: `ArchiveCurrentAsync(IniSnapshotTrigger.LauncherGui, …)` → `WriteAsync` → `UpdateCurrentAsync` → `CommitAsync`.
  - `Restore_WritesOnlyTheDifferingUserSettings` — the written text changes `screenwidth` only; a differing `lastWindowX` in the snapshot is untouched.
  - `Restore_KeyMissingFromSnapshot_IsLeftInTheFile`
  - `Restore_InvalidSnapshotValue_WritesTheDefault` — snapshot `screenwidth=abc`, current `1024` → written `screenwidth=800`.
  - `Restore_NothingDiffers_CommitsWithoutArchivingOrWriting`
  - `Restore_ExternalEditSinceOpening_IsReconciledThenOverwritten` — disk `screenwidth=1280`, `Current` `800`, snapshot `1024` → reconcile archives `Manual`, then the
    file is written with `1024`.
  - `Restore_UnknownSnapshot_ReturnsSnapshotNotFoundAndWritesNothing`
  - `Restore_WriteFails_ReturnsWriteFailed`
  - Validator: `EmptySnapshotId_Fails`, `EmptyInstallationId_Fails`.
- [x] **Step 2: Run to verify they fail** — filter `RestoreIniSnapshot` → compile failure.
- [x] **Step 3: Implement** the command, handler (ctor `(IniWritePipeline pipeline)`), validator and error.
- [x] **Step 4: Run to verify they pass** — Application test project passes.
- [x] **Step 5: Commit** — `feat(✨): restore zoo.ini to a previous snapshot`

---

## Task 7: Editor — `Reset` rename and `Restore defaults`

**Files:**
- Modify: `Source/…Desktop/ViewModels/IniConfig/Fields/IniFieldViewModel.cs`, `IniKeyedFieldViewModel.cs`
- Modify: `Source/…Desktop/ViewModels/IniConfig/IniEditorViewModel.cs`, `Views/IniConfig/IniEditorView.axaml`, and any caller of `RevertCommand` / `RevertAsync`
- Test: `Tests/…Desktop.Tests.Unit/IniConfig/Fields/IniFieldViewModelTests.cs`, `IniConfig/IniEditorViewModelTests.cs`

**Interfaces:**
- Produces:
  - `IniFieldViewModel`: `public virtual bool IsAtDefault => true;` and `public virtual void ResetToDefault() { }`.
  - `IniKeyedFieldViewModel`: `IsAtDefault => Spec.AreEquivalent(CurrentRaw, Spec.DefaultValue)`; `ResetToDefault()` → `ApplyValue(Spec.DefaultValue)` then `NotifyValueChanged()`.
  - `IniEditorViewModel`: `ResetCommand` (the full rename of `RevertCommand` / `RevertAsync` / `CanSaveOrRevert` → `CanSaveOrReset`, comments and tests included);
    `RestoreDefaultsCommand` from `[ RelayCommand(CanExecute = nameof(CanRestoreDefaults)) ] private void RestoreDefaults()`, where
    `CanRestoreDefaults() => !IsBusy && _mediator is not null && AllFields.Any(field => !field.IsAtDefault)`; `RaiseDirtyChanged()` also re-evaluates it.

The Language combo (`IniLanguageFieldViewModel`, no `Id`) follows its two wrapped `lang` / `sublang` number fields; confirm they are in `AllFields` and so reset there. If
they are not, override `ResetToDefault` / `IsAtDefault` on the language field to delegate to them. Footer labels in this task: `Revert` → `Reset`; `Restore defaults` bound
to `RestoreDefaultsCommand` (remove `IsEnabled="False"`). `Undo` is untouched until Task 10.

- [x] **Step 1: Write the failing tests:**
  - Field: `ResetToDefault_AppliesTheSpecDefaultAndMarksDirty`; `IsAtDefault_FollowsTheControlValue`.
  - Editor: `RestoreDefaults_SetsEveryFieldToItsDefault` (every field `IsAtDefault`, `HasPendingChanges` true when the loaded values differed, and the language field's
    selection equals the option for the default `lang` / `sublang`); `RestoreDefaults_IsDisabledWhenEverythingIsAlreadyDefault`;
    `RestoreDefaults_ThenReset_ReloadsTheSavedValues` (`ResetCommand` sends `GetIniConfigQuery` and loads its result); rename existing `Revert_*` tests to `Reset_*`.
- [x] **Step 2: Run to verify they fail** — compile failure from:

  ```powershell
  dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj --filter "FullyQualifiedName~IniConfig"
  ```
- [x] **Step 3: Implement** the members, commands and footer bindings.
- [x] **Step 4: Run to verify they pass** — whole Desktop test project passes; `dotnet build Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj`.
- [x] **Step 5: Commit** — `feat(✨): restore INI defaults as unsaved edits and rename Revert to Reset`

---

## Task 8: Confirmation prompt and the Restore dialogue view model

**Files:**
- Create: `Source/…Desktop/ViewModels/Dialogs/ConfirmationPromptViewModel.cs`, `Views/Dialogs/ConfirmationPromptView.axaml(.cs)`
- Create: `Source/…Desktop/ViewModels/Dialogs/RestorePreviousIniDialogViewModel.cs`, `Models/IniConfig/IniHistoryRow.cs`, `Models/IniConfig/IniHistoryDiffRow.cs`
- Modify: `Source/…Desktop/Composition/IDialogService.cs`, and the three `NoOpDialogService` copies (`IniEditorViewModel.cs`, `IniConfigTabViewModel.cs`, `PlayViewModel.cs`)
- Test: `Tests/…Desktop.Tests.Unit/Dialogs/RestorePreviousIniDialogViewModelTests.cs`

**Interfaces:**
- Consumes: `GetIniHistoryQuery`, `RestoreIniSnapshotCommand`, `IniHistoryEntry`.
- Produces on `IDialogService`:
  - `Task<bool> ShowConfirmationAsync(string title, string message);` — Yes/No; `true` only for `Yes`. No-op copies return `false`.
  - `Task<IniConfigResult?> ShowRestorePreviousIniAsync(Guid installationId);` — the new result after a restore, `null` on cancel. No-op copies return `null`.
- Produces:
  - `public sealed class ConfirmationPromptViewModel(string title, string message) : ViewModelBase` with `Title`, `Message` (designer ctor: placeholder text).
  - `public sealed record IniHistoryRow(IniHistoryEntry Entry, string CapturedText, string TriggerText, string DeltaText);`
  - `public sealed record IniHistoryDiffRow(string SectionText, string KeyText, string CurrentText, string SnapshotText);` — `SectionText` `"[user]"`, `KeyText`
    `"screenwidth"`, `CurrentText` `"(not set)"` when `Current` is null.
  - `RestorePreviousIniDialogViewModel(IMediator mediator, IDialogService dialogs)`: `Task InitialiseAsync(Guid installationId, CancellationToken cancellationToken = default)`;
    `IReadOnlyList<IniHistoryRow> Rows`; `[ ObservableProperty ] IniHistoryRow? SelectedRow`; `IReadOnlyList<IniHistoryDiffRow> DiffRows`; `string ChangeCountText`;
    `bool HasNoDifferences`; `bool HasKeptKeys`; `string KeptKeysText`; `string? ErrorMessage`; `bool IsBusy`; `RestoreCommand`; `CancelCommand`;
    `IniConfigResult? Result`; `event EventHandler? CloseRequested`.

Copy, verbatim (spec §7.4, C7):
- `ChangeCountText`: `"1 change"`, otherwise `"{n} changes"`.
- `KeptKeysText`: `"Not in this snapshot, so they keep their current values: "` + keys as `[section]/key` joined by `", "`.
- `CapturedText`: `CapturedUtc.ToLocalTime().ToString("d MMM yyyy HH:mm", CultureInfo.GetCultureInfo("en-GB"))`; `TriggerText`: the smart enum `Name`; `DeltaText`: row count.
- Confirmation title `"Restore Previous INI"`; message, with the second sentence only when kept keys exist (`n == 1` uses the singular form):

  ```text
  Restore zoo.ini to the version captured {CapturedText}? The current version is archived to history first, so this can be undone.
   1 setting is not in this snapshot and will keep its current value.
   {n} settings are not in this snapshot and will keep their current values.
  ```

Behaviour: on load select the first row (newest, decision 6); `RestoreCommand` can execute only when not busy, a row is selected and it has rows; on execute, confirm →
`No` sends nothing; `Yes` sets `IsBusy`, sends `RestoreIniSnapshotCommand`, on success sets `Result` and raises `CloseRequested`, on error calls
`ShowErrorAsync("Cannot Restore zoo.ini", error.Description)` and stays open. A load error sets `ErrorMessage` and leaves `Rows` empty. `CancelCommand` raises
`CloseRequested` with `Result` null.

- [x] **Step 1: Write the failing tests:**
  - `Initialise_SelectsTheNewestRowAndShowsItsDiff`
  - `SelectingARow_UpdatesTheDiffCaptionAndKeptKeys`
  - `EmptyDiff_DisablesRestoreAndShowsNoDifferences`
  - `Restore_ConfirmationDeclined_SendsNothing`
  - `Restore_Succeeds_SetsResultAndRequestsClose`
  - `Restore_Fails_ShowsTheErrorAndStaysOpen`
  - `Restore_WhileBusy_CannotExecute` — a pending `Send` (unfinished `TaskCompletionSource`) keeps `RestoreCommand.CanExecute(null)` false, so a second press sends nothing
    (`_mediator.Received(1).Send(Arg.Any<RestoreIniSnapshotCommand>(), …)`).
  - `Confirmation_MentionsKeptKeysOnlyWhenPresent`
  - `Initialise_QueryFails_SetsErrorMessageAndDisablesRestore`
- [x] **Step 2: Run to verify they fail** — Desktop tests filtered `RestorePreviousIniDialogViewModelTests` → compile failure.
- [x] **Step 3: Implement** the types, the two interface members, the no-op copies, and `ConfirmationPromptView` modelled on `SaveChangesPromptView` (title and message
  bound, `Yes` default, `No`), which keeps the view-model pair rule. The Restore dialogue's own view comes in Task 9.
- [x] **Step 4: Run to verify they pass** — Desktop test project passes; Desktop project builds.
- [x] **Step 5: Commit** — `feat(✨): add the Restore Previous INI dialogue view model and a confirmation prompt`

---

## Task 9: Restore dialogue view and dialogue-service wiring

**Files:**
- Create: `Source/…Desktop/Views/Dialogs/RestorePreviousIniDialogView.axaml`, `RestorePreviousIniDialogView.axaml.cs`
- Modify: `Source/…Desktop/Composition/AvaloniaDialogService.cs`, `DesktopServiceCollectionExtensions.cs` (register `RestorePreviousIniDialogViewModel` transient)

**Interfaces:**
- Consumes: Task 8's view models and interface members.

The view is a `ClassicWindow` titled `"  Restore Previous INI"` (match the existing two-space title style), `Icon="/Assets/Win95Icons/sheetsanddocuments32.ico"`, `Width="620"`,
`SizeToContent="Height"`, `CanResize="False"`, `WindowStartupLocation="CenterOwner"`, `ShowInTaskbar="False"`, laid out per `dialogs.jsx:440-501`: intro line, snapshot
`DataGrid` (`Captured` mono, `Trigger`, `Δ keys` right-aligned mono; height ≈140), caption row, a sunken scrolling diff table (≈140 high; `Key` with the section muted, `Current`
maroon, `Snapshot` navy, using the existing theme brushes/tokens rather than literal colours), the *No differences — this snapshot matches the current state.* italic
centred message, the kept-keys line (warning styled, wrapping), and a footer `Restore` (`IsDefault`) / `Cancel`. Code-behind closes on `CloseRequested`.
`ShowRestorePreviousIniAsync` resolves the VM from DI, `InitialiseAsync`, shows the dialogue on `ResolveOwner()` and returns `vm.Result`; `ShowConfirmationAsync` mirrors
`ShowSaveChangesPromptAsync` with `new ConfirmationPromptViewModel(title, message)`.

- [x] **Step 1: Build** — `dotnet build Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj` → succeeds.
- [x] **Step 2: Run the architecture tests** — `dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/Erdmier.ZooTycoonLauncher.Tests.Architecture.csproj` → pass.
- [x] **Step 3: Commit** — `feat(✨): show the Restore Previous INI dialogue`

(The dialogue is reachable only after Task 10; it is driven in the app there.)

---

## Task 10: Editor — `Restore previous…`

**Files:**
- Modify: `Source/…Desktop/ViewModels/IniConfig/IniEditorViewModel.cs`, `Views/IniConfig/IniEditorView.axaml`
- Test: `Tests/…Desktop.Tests.Unit/IniConfig/IniEditorViewModelTests.cs`

**Interfaces:**
- Consumes: `IniConfigResult.HistoricalCount` (Task 3), `IDialogService.ShowRestorePreviousIniAsync` (Task 8).
- Produces: `[ ObservableProperty ] public partial int HistoricalCount { get; set; }` set in `Load`; `RestorePreviousCommand` from
  `[ RelayCommand(CanExecute = nameof(CanRestorePrevious)) ] private async Task RestorePreviousAsync(CancellationToken cancellationToken)`, where
  `CanRestorePrevious() => !IsBusy && _mediator is not null && HistoricalCount > 0`.

Flow (spec §7.3, decision 4, C11): with pending changes, `ShowSaveChangesPromptAsync()` — `Yes` → `TrySaveAsync`, stop on `false`; `No` → `DiscardChanges()`; `Cancel` →
stop. Then `ShowRestorePreviousIniAsync(_installationId)`: a result → `Load(result)`; `null` → reload exactly as `ResetCommand` does (send `GetIniConfigQuery`, `Load` on
success, the existing error dialogue on failure). Footer: the `Undo` button becomes `Restore previous…` bound to `RestorePreviousCommand`.

- [x] **Step 1: Write the failing tests:**
  - `RestorePrevious_NoHistory_IsDisabled` / `RestorePrevious_WithHistory_IsEnabled`
  - `RestorePrevious_PendingEdits_Yes_SavesThenOpens`; `…_Yes_SaveFails_DoesNotOpen`; `…_No_DiscardsThenOpens`; `…_Cancel_KeepsEditsAndDoesNotOpen`
  - `RestorePrevious_Restored_LoadsTheReturnedResult`
  - `RestorePrevious_Cancelled_ReloadsFromGetIniConfig`
- [x] **Step 2: Run to verify they fail** — Desktop tests filtered `IniEditorViewModelTests` → fail.
- [x] **Step 3: Implement.**
- [x] **Step 4: Run to verify they pass** — Desktop test project passes; Desktop builds.
- [x] **Step 5: Drive it in the app** — `dotnet run --project Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj` against a
  `SampleInstallations/` copy: save twice, open `Restore previous…`, check the newest row is selected, restore, confirm `zoo.ini` and the editor follow; try
  `Restore defaults` then `Reset`. Screenshot the dialogue to `Docs/UserInterfaceDesign/ImplementationScreenshots/RestorePreviousIniDialogue.png` and register it in the
  `.slnx`. Close the app.
- [x] **Step 6: Commit** — `feat(✨): open Restore Previous INI from the editor footer`

---

## Task 11: Installation Info — History entries (DW-08)

**Files:**
- Modify: `Source/…Desktop/ViewModels/Dialogs/InstallationInfoDialogViewModel.cs` (line 44 and `InitialiseAsync`)
- Test: `Tests/…Desktop.Tests.Unit/Dialogs/InstallationInfoDialogViewModelTests.cs` (create if absent)

**Interfaces:**
- Consumes: `GetIniHistoryCountQuery` (Task 5).
- Produces: `[ ObservableProperty ] public partial string HistoryEntriesText { get; set; } = NotAvailable;` (doc comment updated).

Text: `"1 snapshot"`, otherwise `"{n} snapshots"`; `"—"` when the query errors or throws. Send the count query after the summary loads successfully.

- [x] **Step 1: Write the failing tests** — `HistoryEntries_Zero_ReadsZeroSnapshots`, `…_One_ReadsOneSnapshot`, `…_Many_ReadsTheCount`, `…_QueryFails_ReadsDash`.
- [x] **Step 2: Run to verify they fail.**
- [x] **Step 3: Implement.**
- [x] **Step 4: Run to verify they pass** — Desktop test project passes.
- [x] **Step 5: Commit** — `feat(✨): show the INI history count in Installation Info`

---

## Task 12: Win9x label sweep, SDD amendments and manual test stubs

**Files:**
- Modify: `Source/…Desktop/Views/Dialogs/InstallationManagerDialogView.axaml` (`Add…`, `Edit…`, `Fix…`), `Views/Boot/OpenGameInstallationView.axaml` (`Add…`, `Manage…`),
  `Views/Dialogs/FixInstallationDialogView.axaml` (`Locate…`)
- Modify: `Docs/Specs/1-zoo-tycoon-nexus-master-design.md`, `Docs/TestPlans/manual-end-to-end-tests.md`

Use the real ellipsis character `…` (U+2026), as `Browse…` does. Leave `Info`, `Delete`, `Create`, `Open` unchanged (spec §7.6).

SDD: apply spec §10 in full — §7.3.3, §7.6, §9.3 (footer), §9.4, §9.5, §9.6, §9.7 (local time, `Original` last, newest selected, user settings only, the kept-keys note),
§13.5 (delete DW-02, DW-03, DW-08) — and extend the revision 1.9 row (and the *Last updated* date) to say so. Manual test plan: a new `# Restore Previous INI` part after
`# INI Config Core`, with stubs in the existing format for: 1 restore a snapshot; 2 restore then restore back; 3 the unsaved-changes prompt's three answers; 4 `Restore
previous…` disabled with no history; 5 `Restore defaults` then `Reset`; 6 `Restore defaults` then `Save`; 7 a snapshot with kept keys; 8 the label sweep; 9 Installation
Info history entries. Rename the INI Config Core stub *3. Revert* heading to *3. Reset* to match.

- [x] **Step 1: Apply the label changes; build the Desktop project.**
- [x] **Step 2: Apply the SDD and test-plan edits; verify no prose line exceeds 180 characters** —
  `Get-Content <file> | Where-Object { $_.Length -gt 180 -and -not $_.StartsWith('|') }` returns nothing for both files.
- [x] **Step 3: Commit** — `docs(📝): sweep labels onto the Win9x ellipsis rule and amend the SDD for milestone 9`

---

## Final verification

- [x] `dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx` → 0 warnings, 0 errors.
- [x] `dotnet test Erdmier.ZooTycoonLauncher.slnx` → all pass.
- [x] The app runs, and every Review Focus line holds when driven by hand.
