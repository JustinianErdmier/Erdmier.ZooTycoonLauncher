# Milestone 12 — INI Editor Display Style: Ledger

**Date:** 2026-10-22

**Plan:** [`milestone-12-ini-editor-display-style.md`](../../Plans/Epic-1-MVP/milestone-12-ini-editor-display-style.md) ·
**Spec:** [`milestone-12-ini-editor-display-style-design.md`](../../Specs/Epic-1-MVP/milestone-12-ini-editor-display-style-design.md) ·
**Branch:** `feat/milestone-12-ini-editor-display-style`, cut from `main` at `df749b9`

The INI Config editor has two display styles, chosen in Options: the **Friendly style** (the default) groups settings into eight pages by purpose, with plain
labels, meaning-based check boxes and volume sliders; the **INI style** shows `zoo.ini` as it is stored, the inverted `noMenuMusic` included. A style switch
re-renders an open editor at once and keeps unsaved edits; the Restore Previous INI dialogue follows the style. What is read and written never changes.

## Rulings taken without the maintainer

- **R1 — The toggle's conversion moved to Task 2.** Task 2 replaced `IniFieldDescriptor.Inverted` with a `Conversion`, which the toggle view model reads; the
  plan gave that edit to Task 3, so Task 2 made it to keep the build green. Cost if wrong: none.
- **R2 — Hidden language rows.** `IniFieldDescriptor` gained `IsHidden`. The Friendly layout keeps the `lang` / `sublang` rows hidden (labelled *Language code*
  / *Region code*) so they still carry edits and drive the language picker; coverage means one keyed row per user-setting key in each layout. Cost if wrong: one
  extra flag on the descriptor.
- **R3 — Options kept the stored style before Task 5.** Until the radios existed, OK passed back the style it had loaded. Cost if wrong: none.
- **R4 / R4a — Check boxes and hint lines.** Friendly check boxes carry no hint line (their caption is their label, C4); INI-style check boxes keep the hints
  the INI catalogue gives them (only `noMenuMusic`, per C12). Cost if wrong: a one-line default hint under each Friendly check box later.
- **R5 — One meaning for the cash keys.** The Friendly help for `MSCashIncrement`, `MSMinCash` and `MSMaxCash` was reworded to the INI meaning (the
  denomination cash is awarded in; the minimum and maximum cash the player may hold). Cost if wrong: if the INI meaning is itself wrong, both styles are wrong
  together.
- **R6 — The style loads lazily.** The boot never loaded launcher options, so the INI tab loads them once before its first editor; a failed read keeps the style
  already in force. Cost if wrong: none seen — the load is awaited, so no flash of the wrong style.
- **R7 — Friendly captions.** The section list's caption reads *Page*, and the editor-level description above the pane is hidden because each page header
  carries its own. Cost if wrong: one caption.
- **R8 — No stale style after a save.** `IIniEditorStyleState` keeps a `Version`; the tab's lazy load assigns only if nothing set the style while its query was
  in flight. Cost if wrong: a just-saved style could revert in memory until the next load.
- **R9 — Restore key column font.** In the Friendly style the Restore dialogue's *Page › Group › Label* column uses the system face, like the Friendly labels;
  the INI style keeps the monospace face. Cost if wrong: one font.

## Defects found in the plan

- **Task 2 forced an edit the plan gave to Task 3** (R1).
- **Task 2's coverage test counted the language picker for `lang` and `sublang`,** which clashed with the INI layout showing both the picker and the code rows
  (R2).
- **Task 1 told Options to send a hard-coded Friendly** until Task 5, which would have reset a stored INI style on OK (R3).
- **The plan assumed the boot loads launcher options;** it does not (R6).

## Incidents

- **Three display defects reached the running app** past every review, because reviews read code: the slider readout was clipped at the group-box edge, the
  language-codes line was cut off, and the Width / Height hints repeated their labels. Found in the Task 8 drive and fixed in the final wave.

## Verification

- In the running app (Task 8): Friendly is the default; the Display, Sound, Graphics and Language pages render with group boxes and plain labels; dragging a
  volume slider works and gives focus back; switching to the INI style in Options re-renders the editor at once and keeps the unsaved volume edit (66 % shows as
  `menuMusicAttenuation` 3400); `noMenuMusic` is literal in the INI style; the Restore dialogue shows 7 changes in both styles, Friendly as *Page › Group ›
  Label* with On / Off. Nothing was saved — Save would have written the real `zoo.ini`; saving raw values is covered by `FriendlyEditor_SaveWritesRawValues`.
- After the final fix wave: the readout shows "85 %" / "100 %" in full, a real thumb drag works and Reset restores it, the language codes line wraps, and
  the Width / Height hints read "1–16384, default 800" / "default 600".
- Clean build with 0 warnings; tests: Application 205, Desktop 353, Domain 140, Infrastructure 62, Architecture 27 — all pass.

## Outstanding items

### Polish, whenever the area is next touched

- `IniFieldViewModel.IsIniStyle` is settable, and the properties derived from it do not notify; it is set before binding, so harmless today.
- `IniEditorViewModel.ApplyStyle` would silently drop an edit whose key the new layout lacks; the coverage test makes that unreachable.
- `IIniEditorStyleState.Version` exposes one consumer's concurrency detail; a `TrySetIfUnchanged` would keep the check in one place.
- The field views use literal widths (`Width="120"` and the like); width tokens would join the spacing tokens.
- The Friendly hint and help copy, and the page, group and label names, are a first draft to fine-tune (decision 11).
- conventions §3.6 says the slider stretches to `MaxWidth="200"`; inside the left-aligned row it sits near its 120 px `MinWidth`. The readout fits either way.
- The manual test plan's *Editor Styles* case lacks the blank line before its closing `---`; a few new members and tests lack the blank line before them.
- SDD §11.4 does not yet list the final wave's tests (Page Up / Down, layout parity, footnotes, dispose, off-step not dirtying), and §9.7 does not mention
  that the Friendly key column uses the system face (R9).
- No headless test presses the slider's thumb; releasing focus after a thumb drag is covered by the manual test plan and was checked by hand.

### Not planned

- The `[scenario]` section joins both layouts after the Phase 0 research (DW-04).
