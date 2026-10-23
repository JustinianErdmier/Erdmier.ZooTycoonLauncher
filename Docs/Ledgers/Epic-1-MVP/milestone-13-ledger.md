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
- **R3 — "the INI tab" becomes "the Game Config tab" in the final fix wave.** The documents' shorthand "the INI tab" names the tab, so the controller ruled it is renamed with
  the rest. Done in the final fix wave. Cost if wrong: one wording revert.
- **R4 — The Task 3 commits carry a Claude Sonnet 5.5 Co-Authored-By trailer instead of Opus 5.5.** Left as is; no history rewrite. Cost if wrong: cosmetic attribution.
- **R5 — Further fix waves after the final review.** The SDD-driven process allows one fix wave; CLAUDE.md asks for waves until a re-review has no Critical or Important
  findings, so it took three. Cost if wrong: two extra review seats.
- **R6 — The glossary *Drift* definition says "archives … first" instead of "before adopting".** The longer tab name pushed the row past its column; the shorter wording
  keeps the table aligned without re-padding every row. Cost if wrong: slightly terser glossary wording.
- **R7 — SDD §9.3.3's short last line ("open editor.") stays.** A short final line is a normal paragraph end in hard-wrapped prose. Cost if wrong: cosmetic.

## Defects found in the plan

- **Task 2 Step 3 expected two `INI Config` survivors in `Source/Tests`; there are three.** Task 1's negative assertion `Contains("INI Config")` in
  `GameConfigTabWordingTests.cs` has to keep the literal. It was kept.
- **A line in Task 3 Step 4's prose ran past 180 characters.** It was re-wrapped while Task 3's boxes were ticked.

## Incidents

- **Task 1 had no running-app check.** The computer-use `request_access` call could not resolve the launcher as an application, so the implementer could not drive it
  and took no screenshots. Resolved: the controller captured the window through user32 instead; the tab strip reads "General | Game Config" on one line, unclipped, and
  File → Options shows the *Config editor* group box with "Show settings in the Game Config tab:". The app was closed.
- **Two fix-round commit subjects start with a stray `@ `.** `067c6fe` and `3da2281` break the `type(emoji): subject` format; left as is (no history rewrite) — a squash merge
  would clean it.
- **The first fix wave wrote a NUL byte into this ledger.** PowerShell expanded backtick escapes in a double-quoted string (`` `0 `` → NUL, `` `t `` → TAB), so git saw the
  file as binary. The second wave rewrote the line from a literal string; the file is plain text again.

## Outstanding items

### Before the PR

Nothing outstanding: the running-app check is done (see Incidents).

### Revisit in the final review

Closed by the final fix wave:

- **R3 done.** Every "the INI tab" shorthand in the source comments, the SDD, conventions.md and the manual test plan now reads "the Game Config tab"; the revision rows
  before 1.17 keep their original wording.
- **SDD revision row 1.17 extended** to list every section that was renamed.
- **The four over-wide XML doc and comment lines were re-wrapped** (IniConfigTabViewModel.cs:4, OptionsDialogViewModel.cs:4, IniDisplayText.cs:3,
  MainWindowViewModel.cs:368). The log call at IniConfigTabViewModel.cs:163 is kept as it is: it is a single log call.
- **The headless wording tests leave their windows open.** This is harmless under headless XUnit, so it is kept.

## Final review

Verdict: **Ready after fixes**. The fixes took three reviewed waves (R5): the "INI tab" rename and polish, then the ledger repair and SDD table alignment, then the
glossary *Drift* row. The last re-review has no Critical or Important findings; nothing is outstanding.
