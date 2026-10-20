# Milestone 9 — Restore Previous INI: Ledger

**Date:** 2026-10-20

**Plan:** [`milestone-9-restore-previous-ini.md`](../../Plans/Epic-1-MVP/milestone-9-restore-previous-ini.md) ·
**Spec:** [`milestone-9-restore-previous-ini-design.md`](../../Specs/Epic-1-MVP/milestone-9-restore-previous-ini-design.md) ·
**Branch:** `feat/milestone-9-restore-previous-ini`

Executed with subagent-driven development: Sonnet implementers, Opus task reviewers, one Opus whole-branch review, one fix wave and its re-review. All twelve
tasks completed. Final position: clean build with 0 warnings; 438 tests passing (Domain 123, Application 146, Infrastructure 50, Desktop 106,
Architecture 13). The dialogue was driven in the running app against a `SampleInstallations/` copy (see *Verification*).

---

## Rulings taken without the maintainer

Each entry: what was decided — why — what it costs if wrong.

1. **Unsigned commits for the whole run.** GPG signing timed out (the agent's cached passphrase had expired and the pinentry prompt went unanswered); the maintainer
   said "Commit without signing". Every commit from the plan onwards is unsigned. *Cost:* re-sign by rebase if signed history is wanted.
2. **Shell split.** The skill's helper scripts ran through Git Bash; `dotnet` and most `git` commands through PowerShell (CLAUDE.md); commits through Git Bash
   heredocs because PowerShell here-strings had mangled emoji. *Cost:* none functional.
3. **`.superpowers/` excluded locally** through `.git/info/exclude` (not committed) so the run's scratch workspace never shows in `git status`. *Cost:* none.
4. **P1 — interface members split across Tasks 8 and 9.** The plan had Task 8 add both new `IDialogService` members, but `AvaloniaDialogService` could not
   implement the dialogue member until Task 9 created the view, so Task 8 would not have built. Task 8 added only `ShowConfirmationAsync`; Task 9 added
   `ShowRestorePreviousIniAsync` with the view. *Cost:* none.
5. **P2 — in-app drive moved to the controller** when the implementer had no desktop control. *Cost:* the drive happened after the final review, not in Task 10.
6. **Shared read-and-reconcile prologue (Task 5 fix).** `IniLoadSteps` was extracted for `LoadAsync` and the history query; `IniWritePipeline` was allowed to
   adopt it only if no Save test changed (it did adopt it in the fix wave). *Cost:* none.
7. **Mixed `Co-Authored-By` trailers.** Several implementer commits carry a "Claude Sonnet 5.5" trailer from their own harness rather than the Opus line the run
   asked for; left as is because it names the model that wrote the code. *Cost:* cosmetic inconsistency in the branch history.
8. **Restore dialogue goes busy before the confirmation, not on `Yes`,** so a second press cannot stack a second prompt. *Cost:* none visible (the prompt is modal).
9. **Named colours in the dialogue** (Maroon, Navy, Gray, Red), as the INI editor already uses, because conventions.md's accent tokens do not exist yet. *Cost:* the
   dialogue is not dark-theme aware (outstanding item below).
10. **Fix waves stopped after one** because its re-review found no Critical or Important issue (CLAUDE.md's stop rule). The residual same-millisecond history tie
    is parked below. *Cost:* two snapshots archived in one Save/Restore after an external edit may list in either order.

## Defects found in the plan

- **Task 8/9 interface ordering** (ruling 4): the plan's Task 8 would not have compiled.
- **Task 10 Step 5 assumed implementer desktop control**, which subagents do not get without the maintainer approving access interactively (ruling 5).
- **Task 7's `IsDisabledWhenEverythingIsAlreadyDefault` test** as specified executes before asserting, so it cannot prove the "already default" case (deferred
  below).
- **Global Constraints' commit trailer** conflicted with the implementers' own harness attribution (ruling 7).

## Incidents

- **GPG signing timeout** at the first plan commit; resolved by the maintainer's instruction (ruling 1).
- **Double-send found by a stricter test (Task 8, fix round 1).** `AsyncRelayCommand.Execute` ignores `CanExecute`, so a second press sent a second restore; an
  `IsBusy` guard at the top of `RestoreAsync` fixed it.
- **Close-ordering regression introduced by a fix (Task 8, fix round 1)** — `CloseRequested` fired while busy, which would have blocked the dialogue's own close;
  fixed in round 2.
- **Drive side effects (expected):** the drive registered a "Sample Copy 2" installation in the maintainer's `Launcher.db` and the app rewrote
  `SampleInstallations/Zoo Tycoon - Copy (2)/zoo.ini` (Save and Restore) — both through the app, as CLAUDE.md allows. Remove the registration in the
  Installation Manager if unwanted.

## Verification

- Clean solution build, 0 warnings, 0 errors; all five test projects green.
- In-app drive (dev build, `Sample Copy 2`): `Restore previous…` disabled with no history; two Saves enabled it; the dialogue listed two `LauncherGui` rows and the
  `OriginalImport` row with the newest selected and its diff shown; the confirmation opened owned by and centred on the dialogue with `Restore` / `Cancel` disabled
  behind it; `Yes` restored `screenwidth=1024` on disk and in the editor; `Restore defaults` produced unsaved edits and then disabled itself; `Reset` brought the
  saved values back. Screenshot: `Docs/UserInterfaceDesign/ImplementationScreenshots/RestorePreviousIniDialogue.png`.
- Not driven: the error dialogue path, a snapshot with kept keys, and an external edit while the dialogue is open (all covered by unit tests).

---

## Outstanding items

### Before the next INI milestone

- **Conventions §4 is stale on icons** — it says no ICO files are committed and names files that do not match the committed ones (spec §12).
- **Dark theme for the INI views** — the dialogue and the editor footer use named colours; add the conventions' accent tokens and switch both (ruling 9).
- **Same-millisecond history tie** — `ListHistoricalAsync` orders by `CapturedUtc` then `Id`; a reconcile `Manual` archive and the `LauncherGui` archive from one
  pipeline run share `CapturedUtc` and usually the v7 millisecond. Rank the trigger before the id
  (`ThenBy(s => s.Trigger == IniSnapshotTrigger.Manual ? 1 : 0)`) and make `ListHistorical_EqualTimestamps_NewestArchiveFirst` drop its 5 ms delay.
- **Diff header alignment** — the dialogue's diff header sits outside the scroll viewer, so it is a scrollbar-width wider than the rows once the list scrolls.

### Polish, whenever the area is next touched

- `zoo.ini` in the dialogue's intro uses Lucida Console while the rest of the dialogue uses Consolas; the prototype's dotted diff-row separators are not reproduced.
- The reconcile log is written before the pipeline commits, so a rolled-back write still logs "INI snapshot reconciled".
- The pipeline does not clean orphaned temp files before reading (pre-existing; `LoadAsync` does).
- Neither the dialogue view model nor the editor has a logger, so their new exception catches show errors without logging them (DW-17 territory).

### Test strength (deferred task-review minors)

- Comparer: a test that every `UserSetting` spec has a non-null default; an ordering test that can fail on section order; kept-key ordering; a present-but-null
  snapshot value.
- Snapshot reads: `GetByIdAsync` for the `Original` snapshot.
- Save/pipeline: `CountHistoricalAsync` throwing → `StoreFailed`; count-before-commit on the no-op path; a selector-error test asserting no archive; a throwing
  selector → `StoreFailed`; the no-op path's separate `unchangedCount` local.
- History queries: distinct timestamps in the order test; a result assertion in `Count_NeverOpensATransaction`.
- Restore: a `Historical`-kind fixture; `DidNotReceive` archive on an unknown snapshot; a Restore-level `HistoricalCount` assertion.
- Editor: `RestoreDefaults_IsDisabledWhenEverythingIsAlreadyDefault` asserts after executing; busy/designer enablement of `Restore defaults`; the `No` path cannot
  prove `DiscardChanges` ran; the `Yes` path does not assert save-before-open order; vacuous `ResetCommand` assertion in `RestorePrevious_WhileDialogueOpen_EditorIsBusy`.
- Installation Info: no test that the count query is skipped when the summary fails to load.
- Spec wording: milestone spec §7.4 still mentions a loading state that never shows (the SDD, which wins, is correct).
