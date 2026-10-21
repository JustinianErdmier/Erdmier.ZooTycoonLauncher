# Milestone 9.2 — INI Display Polish: Ledger

**Date:** 2026-10-21

**Plan:** [`milestone-9.2-ini-display-polish.md`](../../Plans/Epic-1-MVP/milestone-9.2-ini-display-polish.md) · **Spec:** none — the five decisions agreed in chat
on 2026-10-21 are recorded in the plan's *Decisions* section · **Branch:** `feat/milestone-9.2-ini-display-polish`, cut from `main` at `710c5a2`

Executed with subagent-driven development: Sonnet implementers, Opus task reviewers, one Opus whole-branch review, two fix waves with scoped re-reviews.
All five tasks completed; Tasks 1 and 3 each took one fix round. Final position: clean build with 0 warnings; every test project green (Domain 134,
Application 160, Infrastructure 51, Desktop 151, Architecture 13).

---

## Rulings taken without the maintainer

1. **Unsigned commits**, continuing the milestone 9 instruction while the GPG agent stays locked. *Cost:* unsigned history; re-sign by rebase if wanted.
2. **A defensive style makes a read-only `NumericUpDown`'s inner text box read-only and grey** (`App.axaml`). Classic.Avalonia ships its templates compiled,
   so whether the template forwards `IsReadOnly` could not be read; if it does, the template binding wins and the setters do nothing. *Cost:* two redundant
   styles.
3. **Cash input rejects a decimal fraction rather than truncating it after the fact.** The parse style is `Currency` without the decimal point, sign and
   parentheses (every cash key's minimum is 0). Truncating could not work: `NumericUpDown` keeps its own parsed value, so the view-model truncation added in
   Task 1's fix round never reached the box, which showed `$1,001` while `1000` was saved. Because the control parses as you type, `$1,000.75`
   in practice commits as `$1,000` — shown equals saved. *Cost:* a player typing cents gets the whole-dollar part only, which is what the game stores anyway.
4. **A second, docs-only fix wave** to make the SDD and plan describe that observed behaviour (the first wave had written "the box reverts"). *Cost:* none.

## Defects found in the plan

- **Review Focus 2 and the Task 1 test chose truncation of typed decimals** without accounting for how `NumericUpDown` round-trips its value; the test
  checked only the written raw, so it passed while the box showed a different number. Caught in the running app; fixed in the first fix wave.
- **Task 4 placed the inert-headers sentence in SDD §9.7's Diff-viewer bullet**; it belongs to the Snapshot-grid bullet. Moved in the first fix wave.

## Incidents

- **A stray empty `IniEditorCatalogueTests.cs` appeared at the repository root** during Task 3 (a mis-aimed redirect). It was never committed and was deleted
  in Task 3's fix round.
- **Task 2's implementer captured no failing run** before implementing (tests and code written in one pass). The review found the tests sound.
- **Opening the INI tab during the drive archived a `Manual` snapshot** (21 Oct 2026 13:11) because `zoo.ini` had changed since 12:36 — expected reconcile
  behaviour, not a defect.

## Verification

- Clean build, 0 warnings; all five test projects pass.
- **Checked in the running app** against the default installation, saving nothing (every edit Reset or declined):
  - cash rows read `$75,000` etc.; `250000` and `$250,000` commit as `$250,000`; `$1,000.75` commits as `$1,000`;
  - `lang` / `sublang` are grey with no spinner; typing, the Up arrow and the wheel change nothing; choosing German writes `7` / `1`;
  - the Restore diff shows `true` / `false` and `0 – Total Quality` → `2 – Balance`; history headers neither depress nor resize; the confirmation title clears
    its icon.
- `Docs/UserInterfaceDesign/ImplementationScreenshots/RestorePreviousIniDialogue.png` refreshed.

---

## Outstanding items

### Polish, whenever the area is next touched

- `IniDisplayText` parses a cash value with the input parse style; harmless, because diff values are effective values (an `Int` spec rejects `$10,000`).
- `conventions.md` still has pre-existing prose lines over 180 characters throughout (22 of them).

### Not planned

- Read-only rows still take focus and show hover help — matches a Win9x read-only edit control; not specified.
- `LaunchErrorView` keeps the OS title bar and no leading spaces (decision 3).
- Dark-theme rendering of the read-only grey and the diff colours — DW-39.
