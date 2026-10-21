# Milestone 9.1 — Restore Previous INI Follow-ups: Ledger

**Date:** 2026-10-20

**Plan:** [`milestone-9.1-restore-previous-ini-follow-ups.md`](../../Plans/Epic-1-MVP/milestone-9.1-restore-previous-ini-follow-ups.md) ·
**Spec:** [`milestone-9.1-restore-previous-ini-follow-ups-design.md`](../../Specs/Epic-1-MVP/milestone-9.1-restore-previous-ini-follow-ups-design.md) ·
**Branch:** `feat/milestone-9.1-restore-previous-ini-follow-ups`, stacked on `feat/milestone-9-restore-previous-ini` (PR #7)

Executed with subagent-driven development: Sonnet implementers, Opus task reviewers, one Opus whole-branch review, one fix wave and its re-review. All nine
tasks completed; Tasks 3, 7 and 9 each took one fix round. The run was paused once at the maintainer's request after Task 9's implementation and resumed later the
same day. Final position: clean build with 0 warnings; every test project green (Domain 134, Application 160, Infrastructure 51, Desktop 128, Architecture 13).

---

## Rulings taken without the maintainer

1. **Unsigned commits**, continuing milestone 9's instruction while the GPG agent stays locked. *Cost:* unsigned history; re-sign by rebase if wanted.
2. **The run's scratch ledger is cleaned into this file at the end**, as in milestone 9. *Cost:* none.
3. **The in-app drive of the dialogue polish (Task 7) moved to the controller**, because subagents have no desktop control. *Cost:* a later manual pass.
4. **The visual check was then left to the maintainer.** Two attempts found another application (Rider and PowerToys, then Edge) holding the foreground; the
   controller closed the launcher rather than compete for the desktop. The XAML was reviewed twice and judged sound. *Cost:* the header alignment, font and
   separators are unverified on screen until someone opens the dialogue (see *Outstanding items*).

## Defects found in the plan

- **conventions.md §3.3 was out of scope.** The spec and Task 9 named only SDD §9.3.2 for the hover/focus rule, but conventions §3.3 also described the retired
  `IsHelpActive` flag. The whole-branch review caught it; the fix wave rewrote it.
- **Task 7 assumed implementer desktop control** (as milestone 9's Task 10 did).
- **Task 8's item list cited a stale line number** (`IniSnapshotComparerTests.cs` "~699" in a 132-line file); the implementer found the right lines.

## Incidents

- **A Desktop build hit a file lock** during Task 7's fix round: the controller's own launcher instance, closing at that moment, still held the output. A retry
  succeeded once it exited.
- **`open_application` launched new launcher instances** instead of focusing the running one, leaving two extra processes; the controller closed them.
- **Line endings.** Some implementers' tools (Git Bash `sed -i`, scripted writes) turned edited files LF in the working copy, and one added a BOM to a view;
  the BOM was removed in review. The index is LF with `core.autocrlf=true`, so the repository is unaffected.

## Verification

- Clean build, 0 warnings; all five test projects pass.
- Mutation checks: reverting the activation token, and moving `LogReconciliation` before `CommitAsync`, each made the new tests fail.
- **Not verified on screen:** the Restore dialogue's diff header alignment (with and without the scrollbar), the Lucida Console font and the 1 px dotted
  separators, and the hover/focus help in the running editor.

---

## Outstanding items

### Before merging PR #7 and this branch

- **Open the Restore Previous INI dialogue once** with a history long enough to scroll (save four or more settings in one Save first): check the header columns
  line up with the rows, the dotted separators are crisp, and the font matches; refresh
  `Docs/UserInterfaceDesign/ImplementationScreenshots/RestorePreviousIniDialogue.png` if it changed.
- **Hover/focus help:** tab into an INI row, then hover another and move away — the footer should fall back to the focused row's help.

### Polish, whenever the area is next touched

- `IniWritePipeline`'s read block near-duplicates `IniLoadSteps.ReadAsync` minus the `HasIni` guard (kept apart deliberately by C3; a guard-skipping overload would
  remove the copy).
- Nested ternaries in the editor's hover/focus tracking; the repeated sender pattern in `IniSectionView`'s four handlers.
- `MainWindowViewModel` logger fields out of alphabetical order; Desktop test `GlobalUsings` order; a long log line in `IniConfigTabViewModel`.
- SDD §7.7 crash-recovery table row (~line 1034) is 33 characters wider than its neighbours; §9.3.2 has a stray wrap ("panel" alone) and trailing spaces.
- conventions.md has 25 pre-existing prose lines over 180 characters.
- Unnamed `AddHours`/`AddDays` literals remain in `IniWritePipelineTests`, `SaveIniHandlerTests`, `RestoreIniSnapshotHandlerTests` and
  `RestorePreviousIniDialogViewModelTests`.
- One hover test uses rows from two different sections (still correct for the view model).

### Not planned

- `IsHovered` can stay set after leaving the INI tab (no pointer-exit on detach) — pre-existing, not specified.
- Other dialogues (Add/Edit, Info, Fix) still use `Consolas`; C4 was scoped to the Restore dialogue.
- A Save that succeeds while the cached `HasIni` is false does not refresh the flag (C3 leaves it alone by design).
- Dark theme and colour tokens — DW-39 (Settings + theming slice).
