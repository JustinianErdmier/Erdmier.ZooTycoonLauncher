# Milestone 11 — Options Dialogue and Colour Cleanup: Ledger

**Date:** 2026-10-22

**Plan:** [`milestone-11-options-and-colour-cleanup.md`](../../Plans/Epic-1-MVP/milestone-11-options-and-colour-cleanup.md) ·
**Spec:** [`milestone-11-options-and-colour-cleanup-design.md`](../../Specs/Epic-1-MVP/milestone-11-options-and-colour-cleanup-design.md) ·
**Branch:** `feat/milestone-11-options-and-colour-cleanup`, cut from `main` at `b13a955`

The maintainer scrapped the dark-mode feature during the brainstorm and kept the colour cleanup in this milestone. Executed with subagent-driven development:
Sonnet implementers, Opus task reviewers, one Opus whole-branch review and one fix wave with its re-review. All nine steps completed (Tasks 0 and 8 by the
controller); Tasks 6 and 7 each took one fix round. Final position: clean build with 0 warnings; every test project green (Domain 138, Application 202,
Infrastructure 60, Desktop 205, Architecture 16).

---

## Rulings taken without the maintainer

1. **Unsigned commits**, continuing the earlier instruction. *Cost:* unsigned history; re-sign by rebase if wanted.
2. **`OK` is disabled until the stored options have loaded**, where C2 said "always enabled": a failed load must not let `OK` overwrite the stored values with
   defaults. The spec and SDD were amended. *Cost:* `OK` is greyed for the moment the options load.
3. **Muted text keeps `Opacity="0.6"`**: it is not a colour literal, and switching it to `ForegroundMuted` would change the look, which the spec forbids.
   *Cost:* two ways of muting text remain (both documented in conventions §7).
4. **`IsStatusError` mirrors the old red exactly** — true only for Cannot Play; a boot failure keeps the default colour. *Cost:* none (behaviour unchanged).
5. **Task 6 also pins the converters' fallback colours to `Colours.axaml` and bans `Color.Parse` / `Color.FromRgb` and friends**, so "define once" holds even
   for the deliberate fallback. *Cost:* one extra architecture test.
6. **The status bar's display cell is now visible in Ready to Play.** It had been bound to a null brush, which drew nothing; the new converter leaves the
   default text colour. It was always meant to show (the manual test plan's note asks for it). *Cost:* the hard-coded `Display: 1920 × 1080` (DW-09) now shows
   on every Ready to Play screen, and is wrong on other monitors until DW-09 lands.
7. **The Cannot Play heading stays `AccentRed`** (C5 maps the literal `Red` to `AccentRed`), and conventions now say so. The prototype draws it maroon.
   *Cost:* a one-word change if maroon is wanted.

## Defects found in the plan

- **Task 5 / Review Focus 4 assumed a boot failure painted the status bar red**; the old code used red only for Cannot Play (ruling 4).
- **Task 3's test list omitted "the menu command opens it"**, which spec §6 asked for; added in the fix wave.
- **Task 7's amendment list missed** the SDD §9.1 status-bar wording, the §9.2.2 folder tree, §11.5's architecture-rule list and two conventions statements
  about the Cannot Play colour; caught in review and fixed.

## Incidents

- **Saved options seemed to vanish on restart during the in-app check.** A launcher started from the controller's shell runs inside the Claude app's package
  context, and Windows redirects its `%LOCALAPPDATA%` to the package's own storage — a separate `Launcher.db`. One instance was started that way and another
  through the desktop tool, which uses the real one. Diagnosed by reading both databases; not a code defect. Both databases were restored afterwards. Recorded
  in the session memory for future checks.
- **PowerToys' Command Palette repeatedly held the foreground**, so the desktop tool was used to bring the launcher forward; it started a second instance, which
  was closed.
- **A Task 7 implementer and the fix-wave implementer each left background work running**; the controller stopped them.

## Verification

- Clean build, 0 warnings; all five test projects pass.
- **Checked in the running app** (22 Oct 2026, 09:18–09:24):
  - `File → Options…` shows the layout and copy of spec C1 with the stored values; saving "No installation" and the close option stuck, and a restart (same
    launch method) opened the picker. The maintainer's options were then restored to *The default installation* with the close option off, in both databases.
  - Before/after captures of the General tab, INI editor, Restore Previous INI dialogue and Installation Manager differ only by ±1 shading and the newly
    visible status-bar display cell (ruling 6). Red and amber text did not appear on those screens.
- **Not driven:** a failed save (database locked) — covered by unit tests.

---

## Outstanding items

### Polish, whenever the area is next touched

- Esc or the title-bar close during an in-flight save does not cancel it (the save is a sub-second write).
- The no-literal-colours test checks the six property names C7 lists; colour-valued properties outside them and element-content colours
  (`<SolidColorBrush>Red</SolidColorBrush>`) are not scanned, nor a setter with another attribute before `Property`.
- `ResolveProjectDirectory` is duplicated across architecture tests; some test helpers dispose the EF-owned connection; the migration Down test's name
  promises a default it does not assert.
- conventions §1.2 keeps the prototype's `dk-c-*` column; conventions has pre-existing prose lines over 180 characters.

### Not planned

- Other slices' handlers mutate the shared tracked `LauncherSettings` / installation entities the same way the Options save did before its fix; only the Options
  path relied on Cancel discarding.
- The hard-coded status-bar display text — DW-09.
