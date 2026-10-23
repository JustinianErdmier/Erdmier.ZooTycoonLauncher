# Milestone 13 — Game Config Tab Rename: Ledger

**Date:** 2026-10-23

**Plan:** [`milestone-13-game-config-tab-rename.md`](../../Plans/Epic-1-MVP/milestone-13-game-config-tab-rename.md) ·
**Branch:** `feat/milestone-13-game-config-tab-rename`

The main window's **INI Config** tab is renamed **Game Config**, so the launcher stops showing users a file-format name. The Options dialogue's *INI editor* group box
becomes *Config editor*. Code identifiers, folders and namespaces containing `IniConfig` keep their names; the tab is still a view of `zoo.ini`.

## Rulings taken without the maintainer

- **R1 — Alt-C in the SDD.** SDD §9.1 lists the tab as Alt-I, but no access key is wired up in the code. With General already on G, the SDD changes to Alt-C (the `C` of
  *Config*). Nothing in the code changes. Cost if wrong: one access-key letter in the SDD, to be changed when access keys are actually wired up.
- **R2 — Task 1 also renamed two XML doc comments in `GeneralTabViewModel.cs`.** They were Task 2's scope, but sat in the same file and fell under the same rule. Cost if
  wrong: none.

## Defects found in the plan

- **Task 2 Step 3 expected two `INI Config` survivors in `Source/Tests`; there are three.** Task 1's negative assertion `Contains("INI Config")` in
  `GameConfigTabWordingTests.cs` has to keep the literal. It was kept.
- **A line in Task 3 Step 4's prose ran past 180 characters.** It was re-wrapped while Task 3's boxes were ticked.

## Incidents

- **Task 1 had no running-app check.** The computer-use `request_access` call could not resolve the launcher as an application, so the implementer could not drive it
  and took no screenshots.

## Outstanding items

### Before the PR

- Verify in the running app that the Game Config tab header fits on one line and the Options *Config editor* group box reads correctly.

### Revisit in the final review

- **A few edited C# doc and comment lines grew one character past their file's previous widest line** (`IniConfigTabViewModel.cs:4` and `:163`, `OptionsDialogViewModel.cs:4`,
  `IniDisplayText.cs:3`, `MainWindowViewModel.cs:368`). This is cosmetic; the final review will triage it.

### Decision for the maintainer, any time

- **The documents' shorthand "the INI tab" was left alone.** The SDD (for example §3.1, §7.1, §9.3.1, §9.5, §14.5 and the glossary), `conventions.md` and the manual test plan
  use it as a short name for the same tab in dozens of places. The plan asked only for the `INI Config` wording, so those stay; a later pass can rename them if the
  documents should say *Game Config* throughout.