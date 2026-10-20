# Restore Previous INI Follow-ups — Design Spec

> **Milestone:** 9.1 — follow-ups to milestone 9 (Epic 1 — MVP)
> **Date:** 2026-10-20
> **Author:** Justinian
> **Status:** Approved for implementation
> **Authoritative spec:** [1-zoo-tycoon-launcher-master-design.md](../1-zoo-tycoon-launcher-master-design.md) §7.6, §8.2, §9.3.2, §9.7, §13.5
> **Source of items:** [milestone-9 ledger](../../Ledgers/Epic-1-MVP/milestone-9-ledger.md) *Outstanding items*
> **Deferred work taken:** DW-17 (INI loggers and the shared activation token), DW-18 (hover vs focus help), DW-27 (INI hygiene)
> **Branch:** `feat/milestone-9.1-restore-previous-ini-follow-ups`, stacked on `feat/milestone-9-restore-previous-ini` (PR #7) until that merges

---

## 1. Goal

Close the INI area's open debt in one pass: the defects, polish and test gaps milestone 9's ledger left open, plus the three INI register items whose code sits in
the same files. No new user-facing feature; every change either fixes a defect, aligns with the prototype, or strengthens a test.

---

## 2. Decisions

| # | Question                                   | Decision                                                                                                                               |
|---|--------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------|
| 1 | Scope                                      | Milestone 9's outstanding items + DW-17 + DW-18 + DW-27. DW-13 (global exception handler) stays out — it is app-wide, not INI.          |
| 2 | Dark theme for the INI views               | **Dropped.** The token system does not exist yet; it belongs to the Settings + theming slice, recorded as DW-39.                         |
| 3 | DW-18 — hover and focus disagree           | **The hovered row wins**; when nothing is hovered, the focused row's help shows; with neither, the footer shows its dirty/saved state.   |

**Calls made by the author** (go in the ledger as rulings when the plan runs):

- **C1 — History tie-break by trigger.** Within one `CapturedUtc`, a `LauncherGui` archive lists before a `Manual` one, then the id breaks any remaining tie. Within one
  pipeline run the reconcile `Manual` archive always precedes the `LauncherGui` archive, and those are the only two archives that can share a timestamp. `Trigger` is
  stored as its name, so an ascending sort on the column puts `LauncherGui` before `Manual`; a code comment must say so, so nobody "fixes" it to descending.
- **C2 — The reconcile log moves after the commit.** `IniLoadSteps.ReconcileAsync` stops logging; a separate `IniLoadSteps.LogReconciliation(…)` is called by each
  caller after its `CommitAsync` succeeds, so a rolled-back transaction never logs a reconciliation. The write pipeline has two commits: on the no-op path, log after
  that commit; on the write path, log inside the existing post-write `try`, straight after `CommitAsync`, so a swallowed commit failure logs no reconciliation.
- **C3 — The write pipeline gains orphaned-temp cleanup only.** It calls `IIniFileStore.DeleteOrphanedTempFilesAsync` before its read, inside its existing read
  `try`, but does **not** adopt `IniLoadSteps.ReadAsync`'s `HasIni` guard: `HasIni` is a cached flag and the file on disk is the truth, so a Save must not be refused
  while the file exists.
- **C4 — The dialogue's monospace font is the prototype's stack** (`Lucida Console, Courier New, monospace`, as `win95.css --ff-mono` and the INI section view use)
  for every mono run in the Restore Previous INI dialogue.
- **C5 — Log levels.** Unexpected exceptions caught in a view model log at `Error` with the exception; an `ErrorOr` failure the user is shown logs at `Warning` without
  an exception. Messages name the installation id.
- **C6 — `IniKeySpec.TryParseBool` becomes public** (renamed `TryParseBool(string? value, out bool result)`, trimming and null-safe) so the toggle field reuses it.

---

## 3. Scope

### 3.1 Defects

1. **Same-millisecond history order (C1).** `IniSnapshotTransaction.ListHistoricalAsync` orders by `CapturedUtc` descending, then trigger (`LauncherGui` before
   `Manual`), then `Id` descending. The integration test `ListHistorical_EqualTimestamps_NewestArchiveFirst` drops its 5 ms delay and archives both rows with the
   *same* capture time, giving the `Manual` row the larger id, so it exercises the real tie. `ArchiveCurrentAsync` mints its own ids, so the test inserts the two
   `Historical` rows directly (through `AddAsync` with explicit ids, or the context) rather than archiving.
2. **Shared INI activation carries the first caller's token (DW-17).** `IniConfigTabViewModel.ActivateAsync` starts the shared load with the first caller's
   `CancellationToken`. The shared load runs uncancellable (`CancellationToken.None`); each caller awaits it through `Task.WaitAsync(callerToken)`, so cancelling one
   caller only stops that caller's wait. Tests: a cancelled first caller does not fault the second caller's load; a cancelled second caller does not cancel the load.
3. **Reconcile logged before commit (C2)** — `LoadAsync`, the history query and the write pipeline log only after a successful commit.
4. **Write pipeline skips orphan cleanup (C3).**

### 3.2 Observability — INI loggers (DW-17)

`IniConfigTabViewModel`, `IniEditorViewModel` and `RestorePreviousIniDialogViewModel` take an `ILogger<T>` (designer constructors pass `NullLogger<T>.Instance`, as the
other dialogue view models do). Every catch block and every shown `ErrorOr` failure in them logs per C5:

- tab: the load catch and the `result.IsError` branch;
- editor: save, reload and restore-open catches;
- dialogue: the history-load catch and the restore catch.

The editor's shown `ErrorOr` failures (the save error path and the reload `result.IsError` branch) log at `Warning` too.

Five constructors widen: `MainWindowViewModel` (which builds `PlayViewModel`) takes `ILogger<IniConfigTabViewModel>` and `ILogger<IniEditorViewModel>` from DI and
hands them on; `PlayViewModel` passes both to the tab; the tab passes its editor logger to each editor it creates — mirroring how `MainWindowViewModel` already passes
`ILogger<PlayViewModel>`. The dialogue view model is resolved from DI, which supplies its logger. Every test call site is updated.

### 3.3 Help on hover and focus (DW-18, decision 3)

Replace the single `IniFieldViewModel.IsHelpActive` flag with two — `IsHovered` and `IsFocused` — set by the row's pointer and focus events in `IniSectionView`. The
editor keeps the hovered field and the focused field separately; `ActiveHelp` is the hovered field's help, else the focused field's, else `null`. Changing section
clears both. Tests: pointer leaving a focused row keeps its help; focusing another row while hovering keeps the hovered help; leaving both shows the footer state.
SDD §9.3.2's sentence "The row's hover and focus events set the field's `IsHelpActive`" is amended to describe this rule.

### 3.4 INI hygiene (DW-27)

1. `IniErrors.StoreFailed` message: *"The installation's settings history could not be read or updated: {message}"* (it now covers open, reconcile, read and commit).
2. `ApplicationServiceCollectionExtensions`: class and method summaries list what is registered (Mediator, validators, the validation behaviour, `IniReconciler`,
   `IniWritePipeline`, `IIniSnapshotService`).
3. The `[ai]` footnote drops "on the original engine" so it matches the INI design text: *"Raising this above the stock 1,000 limit can make guest pathing thrash;
   values above 2,500 need a community AI patch."*
4. `IniEditorCatalogue`'s language rows use `LanguageId` / `SubLanguageId` instead of re-spelling `"lang"` / `"sublang"`; the two tests that re-spell them use the
   constants too.
5. `IniToggleFieldViewModel.IsTrue` is removed in favour of `IniKeySpec.TryParseBool` (C6).
6. `IniLanguageFieldViewModel.SelectedOption`: setting it raises `SelectedOption` once, after both numbers are written (suppress the notification between the two
   writes). Its designer constructor loads the default `lang` / `sublang` values so the designer shows a selection.
7. `IniConfigTabViewModel.Content` gets a private setter.
8. `IniConfigTabViewModel.ActivateAsync` doc names all three skips: no INI, pending edits, no mediator (designer).
9. `IDialogService.ShowErrorAsync` doc states the fallback: with no owner window it shows a non-modal box and returns without waiting.

### 3.5 Restore Previous INI dialogue polish

1. **Header alignment** — the diff header must stay aligned with the rows when the vertical scrollbar appears: put the header inside the scrolling area (sticky header,
   as the prototype's `position: sticky`) or reserve the scrollbar width on the header; builder's choice.
2. **Font (C4)** — all mono runs use the prototype stack.
3. **Row separators** — a 1 px dotted light-grey line under each diff row (`dialogs.jsx:485`).

### 3.6 Documentation

1. `conventions.md` §4.1 rewritten against what is committed: icons live in `Desktop/Assets/Win95Icons/` with their committed names (`sheetsanddocuments32.ico`,
   `foldercatalog16.ico`, `warning32.gif`, …); the "not yet committed" sentence and the `Assets/Icons/` copy task go; the table maps each site to the file each view
   actually uses (read the views), and marks any site whose view does not exist yet.
2. Milestone 9 spec §7.4: the "while loading" sentence is replaced by "the history loads before the window opens" (matches SDD §9.7).
3. SDD: §9.3.2 (decision 3); §13.5 deletes DW-17, DW-18, DW-27; revision 1.10 extended.

### 3.7 Test strength (milestone 9's deferred minors)

Domain comparer: every `UserSetting` spec has a non-null default; an ordering test whose registry order disagrees with section order (or a note proving none exists);
kept-key ordering; a present-but-null snapshot value. Snapshot reads: `GetByIdAsync` for the `Original` snapshot. Save and pipeline: `CountHistoricalAsync` throwing →
`StoreFailed` (Load and Save); count-before-commit on the no-op path; a selector-error test asserting no archive; a throwing selector → `StoreFailed`; fold Save's
`unchangedCount` into `historicalCount`. History queries: distinct timestamps in the order test; a result assertion in `Count_NeverOpensATransaction`. Restore: a
`Historical`-kind fixture; `DidNotReceive` archive on an unknown snapshot; a Restore-level `HistoricalCount` assertion. Editor: `RestoreDefaults` disabled assertion
before executing plus `HasPendingChanges` false after; busy and designer enablement of `Restore defaults`; the `No` path proves `DiscardChanges` ran; the `Yes` path
asserts save-before-open order; replace the vacuous `ResetCommand` assertion. Installation Info: the count query is not sent when the summary fails to load.

### 3.8 Out of scope

Dark theme and colour tokens (DW-39); DW-13; the Settings → Options rename (DW-37); real-time `zoo.ini` watching (DW-38).

---

## 4. Testing

Every behavioural change in §3.1–§3.4 gets a failing test first. §3.5 is checked by driving the dialogue in the running app with a history long enough to scroll,
against a `SampleInstallations/` copy, and replacing `ImplementationScreenshots/RestorePreviousIniDialogue.png` if it changes visibly. The whole solution builds with no
warnings and every test project passes.

---

## 5. Risks

- **Logger plumbing through constructors** widens five constructors and their call sites; the designer constructors must keep compiling with null loggers.
- **DW-18 touches the editor's help path**, which milestone 8's tests cover; those tests change with the flag rename.
