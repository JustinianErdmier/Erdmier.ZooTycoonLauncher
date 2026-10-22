# Milestone 11.1 — Style Tokens and Follow-ups: Ledger

**Date:** 2026-10-22

**Plan:** [`milestone-11.1-style-tokens-and-follow-ups.md`](../../Plans/Epic-1-MVP/milestone-11.1-style-tokens-and-follow-ups.md) ·
**Spec:** [`milestone-11.1-style-tokens-and-follow-ups-design.md`](../../Specs/Epic-1-MVP/milestone-11.1-style-tokens-and-follow-ups-design.md) ·
**Branch:** `feat/milestone-11.1-style-tokens-and-follow-ups`, cut from `main` at `838ba0b`

Every typography value, spacing value and repeated control size in the views now comes from a token file under `Desktop/Styles/` (`Colours`, `Typography`,
`Spacing`, `Controls`), and an architecture test per group keeps literals out. Spacing moved onto the Windows 9x guideline values; faded text uses the
`MutedText` style; icons draw pixel-crisp. The four polish items milestone 11 left open are closed.

After the first review the maintainer added four follow-ups to this milestone (spec §6a, plan Tasks 9–13): INI fields keep focus only from the keyboard, the
muted grey darkens to `#666666`, the installation grid's headers stop reacting to clicks, and conventions.md lists only the colours `Colours.axaml` defines,
with a test that keeps the two in step.

## Rulings taken without the maintainer

- **One project-path helper for the scanner too.** Task 5's `ProjectPaths` also replaced the shared XAML scanner's own project lookup, which the plan did not name.
  Cost if wrong: none; one resolver was the point.
- **Crisp icons are set per `Image`.** Avalonia 11.3 rejects `RenderOptions.BitmapInterpolationMode` as a style setter (`AVLN2000`), so each icon sets
  `BitmapInterpolationMode="None"` itself and a third test, `Icons_DrawWithoutSmoothing`, enforces it. Cost if wrong: one attribute repeated on 24 images
  instead of one setter.
- **The group-box header is 6 px, not 14.** Measured on screen: Classic.Avalonia's content area starts at the frame line and the title text rises about 6 px
  above it, so `GroupBoxPadding` is `14,12,14,11` (the guideline's 18 px from the box top, less 6). Cost if wrong: group-box content sits a pixel or two off the
  guideline.
- **Label-to-value column gaps all take `GapUnrelated` (11).** Info was `16`, General's Display and System `20`, General's Status `8`. Cost if wrong: Status's
  columns are 3 px wider and the others 5–9 px narrower than before.
- **Dialogue button rows take `Top=GapUnrelated` and no bottom margin**, since the dialogue's own `MarginDialog` gives the bottom edge. Cost if wrong: buttons
  sit a few pixels closer to the content above.
- **The Options explanatory text's checkbox indent took `GroupBoxInset` (14)**, down from `20`; the drive found it aligned. Cost if wrong: the text no longer
  lines up exactly under the checkbox label.
- **The Restore dialogue's `Cancel` got the same busy guard as Options.** `RelayCommand.Execute` ignores `CanExecute`, so a direct call could close the dialogue
  mid-restore. This was outside the plan; a test (`Cancel_WhileRestoring_DoesNotClose`) covers it. Cost if wrong: none.
- **Two documentation commits (`7fb2d95`, `954ffe7`) carry a `Claude Sonnet 5.5` co-author trailer**, the model that wrote them, instead of the run's usual
  `Opus 5.5`. Left as accurate rather than rewriting history. Cost if wrong: two trailers differ.
- **Enter in an INI text or number box still presses the editor's default *Save*** as well as committing the field. It did so before the follow-ups, and a
  default button taking Enter is the Windows 9x convention. Cost if wrong: pressing Enter to confirm one value writes `zoo.ini`; making Enter field-only is a
  one-line `e.Handled = true`.
- **The installation grid's columns are no longer resizable.** In Classic.Avalonia the column header handles both the click and the resize drag, so an inert
  header cannot be resized; spec §6a.3 allowed this fallback. Cost if wrong: a long path cannot be widened by dragging (Path already takes twice Name's share).
- **A keyboard user who opens a drop-down and then picks an item with the mouse loses focus when the list closes**, because the pick is pointer focus. It
  follows the spec's "only keyboard focus lasts" rule. Cost if wrong: that mixed-input user presses Tab once more.

## Defects found in the plan

- **Task 3 assumed a 14 px group-box header.** The first snap put group-box content 4 px under the frame line, touching the icons; fixed in `4053838` after an
  on-screen measurement.
- **Task 3 did not anticipate that a token on a custom control's property needs a public property field.** `UniformStack.SpacingProperty` was private; a literal
  went through the CLR setter, but `{StaticResource GapRelated}` goes through the field, which threw `FieldAccessException` when the view loaded (see
  *Incidents*). Fixed in `8faf776`.
- **Task 4's bite proof could not plant a setter with an extra attribute in live XAML**, because Avalonia rejects it at compile time; that case was proven in an XML
  comment, and the scanners now blank comments, so it is covered by reasoning about the regex rather than by a live plant.
- **Spec C3 and conventions described markup that does not exist** (`Classes="default"`, `MinWidth="110"`, a `0.55` Cannot Play group box,
  `TextRenderingMode`, `StatusBadge`, `Etched`). Task 7's fix round made the docs describe the views as built.

## Incidents

- **Opening Options crashed the launcher** (Task 8 drive): the private `UniformStack.SpacingProperty` above, affecting all five views with a `UniformStack`
  (Options, Add, Edit and Delete Installation, the installation picker). The build, every test and three review seats had passed it; only the running app
  showed it. Fixed with `AvaloniaProperties_ArePublic`, and the final review added `TokenReferencesTests` so a mistyped or wrongly typed resource reference
  also fails a test rather than a view load.
- **The game was started by mistake** during the drive: a click meant for the Installation Manager landed on *Launch Game*. `zoo.exe` reached its "CD Required"
  prompt, briefly changed the display mode and exited. The redirected test database's *Last played* became 22 October 12:15, and the game rewrote its own
  `zoo.ini` as it does on every start; the launcher saved nothing.
- **Implementers left background work running** in Task 1 and in Task 13 (a hung line-ending check); each was told to stop it and did.
- **Task 9's first cut broke keyboard focus on drop-downs.** Avalonia's ComboBox moves focus to its list item on open and back to itself on close, and the
  row's focus handlers treated those moves as leaving the field, so a Tab-focused drop-down lost focus when its list closed. Review caught it; fixed in
  `f2450ce` (focus moves inside the row keep the state). The second review found the narrowed click check written but never called; wired in `0fc60ae`.

## Verification

- Clean build of the solution with 0 warnings (after the follow-ups, at `1c07b8c`).
- Tests: Architecture 27, Application 202, Domain 138, Desktop 235, Infrastructure 60 — all pass.
- In the running app (Task 12, the follow-ups): picking from the `fullscreen` drop-down, a spinner step and a toggle click each give focus back and the
  footer shows the save status; a field reached with Tab keeps focus and its help, also through opening and closing its list from the keyboard; Esc clears
  focus. The muted grey reads darker and stays lighter than the Info labels. A held mouse button on the Installation Manager's column header does not press it.
  The one value changed during the check was reset, so nothing was saved.
- In the running app (Task 8): all eleven before-captures retaken and compared — the main window's General and INI Config tabs, Restore Previous INI and its
  confirmation, Installation Manager, Info, Edit, Delete, Add, Options and the installation picker. Muted text was `#808080` (now `#666666`), the Info labels measure `#404040`,
  icons are pixel-crisp, nothing is clipped. Esc closes Options when idle (the earlier failure was focus) and Restore. The title-bar close during a save is
  covered by the view model's tests only; a local save is too quick to catch by hand.

## Outstanding items

### Polish, whenever the area is next touched

- The XAML scanners miss property-element syntax (`<Setter.Value>Red</Setter.Value>`, `<SolidColorBrush.Color>…`), single-quoted attribute values and literals in
  markup-extension positional parameters.
- `SharedSizeStylesTests` and `AvaloniaPropertyFieldTests` build `new Regex` rather than `[GeneratedRegex]`; the scanner rebuilds two regexes per `Scan` call.
- `SharedSizeStylesTests`' style-block regex does not handle a selector-first, nested or self-closing `<Style>`, and reports `<Image Image.Width>` twice.
- The Button `MinWidth` style-setter path of `Views_UseSharedSizeStyles` was not proven with a planted setter (the attribute path was).
- The Infrastructure tests' parameter-adding code is repeated across three classes; `ProjectPaths.Project` no longer checks for the `.csproj`.
- `AvaloniaProperties_ArePublic` scans source text, so a property field declared in an unusual form could slip past it.
- `TokenReferencesTests` do not assert that they matched at least one reference, so a regex regression that matches nothing would quietly turn them off.
- `Views_DoNotFadeTextWithOpacity` flags any `Opacity` other than `1` (bindings and `1.0` included), while its summary and SDD §11.5 say "literal"; it also
  reports two literal-`MinWidth` buttons on one line once.
- `XamlResourceKeys.CollectShared` reads only the top level of `Styles/`.
- The INI focus code treats a drop-down's list as inside its row by walking from the popup to its owner. That holds while popups are separate windows (the
  Windows default); turning on overlay popups would break it. `IniFieldFocusRelease.IsWithin` and the click filter have no test outside the running app.
- `ColourRegistryTests`' duplicate-key path was not proven with a planted duplicate (the no-hex path was).

### Not planned

- A headless Avalonia test that loads every view now exists on its own branch, `feat/headless-view-load-tests`, cut from this branch; it opens after this PR
  merges. It is also the natural home for tests of the INI focus behaviour above.
- Remapping the spacing tokens onto an even 2 → 14 scale: the table below records every original value and its token for that.

---

## Spacing before and after

Every spacing value in the Desktop views before the snap (recorded at `d6445b8`, before any spacing change), with the token it became. Line numbers are
the original file's. To move to an even 2 → 14 scale later, remap each token here instead of searching the views.

| File | Line | Attribute | Original | New token |
|------|------|-----------|----------|-----------|
| `Views/Boot/LookingForZooTycoonView.axaml` | 6 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Boot/LookingForZooTycoonView.axaml` | 12 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Boot/LookingForZooTycoonView.axaml` | 14 | `Spacing` | `4` | `GapRelated` |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 7 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 8 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 22 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 24 | `Spacing` | `4` | `GapRelated` |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 34 | `Spacing` | `2` | `GapLabel` |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 43 | `Padding` | `11 15 15 12` | `GroupBoxPadding` |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 46 | `RowSpacing` | `4` | `GapRelated` |
| `Views/Boot/OpenGameInstallationView.axaml` | 9 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Boot/OpenGameInstallationView.axaml` | 10 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Boot/OpenGameInstallationView.axaml` | 17 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Boot/OpenGameInstallationView.axaml` | 19 | `Spacing` | `4` | `GapRelated` |
| `Views/Boot/OpenGameInstallationView.axaml` | 32 | `HorizontalSpacing` | `8` | `GapRelated` |
| `Views/Boot/OpenGameInstallationView.axaml` | 38 | `Padding` | `25 0` | removed (`Classes="DialogButton"` gives the 75 px minimum width) |
| `Views/Boot/OpenGameInstallationView.axaml` | 41 | `Spacing` | `8` | `GapRelated` |
| `Views/Boot/OpenGameInstallationView.axaml` | 44 | `Padding` | `10 0` | `{tokens:Space Horizontal=GapUnrelated}` |
| `Views/Boot/OpenGameInstallationView.axaml` | 48 | `Padding` | `10 0` | `{tokens:Space Horizontal=GapUnrelated}` |
| `Views/Boot/OpenGameInstallationView.axaml` | 52 | `Padding` | `10 0` | `{tokens:Space Horizontal=GapUnrelated}` |
| `Views/Common/InstallationFormView.axaml` | 7 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Common/InstallationFormView.axaml` | 10 | `RowSpacing` | `5` | `GapRelated` |
| `Views/Common/InstallationFormView.axaml` | 26 | `HorizontalSpacing` | `5` | `GapRelated` |
| `Views/Common/InstallationFormView.axaml` | 45 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Common/InstallationFormView.axaml` | 54 | `Margin` | `2 0 0 0` | removed (icon-to-text gap is now `GapRelated`) |
| `Views/Common/InstallationGridView.axaml` | 28 | `Margin` | `4 0` | `{tokens:Space Horizontal=GapLabel}` |
| `Views/Common/InstallationGridView.axaml` | 51 | `Margin` | `4 0` | `{tokens:Space Horizontal=GapLabel}` |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 15 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 17 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 18 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 25 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 27 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 43 | `Margin` | `0 5` | `{tokens:Space Top=GapUnrelated}` |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 44 | `Spacing` | `8` | `GapRelated` |
| `Views/Dialogs/ConfirmationPromptView.axaml` | 23 | `Margin` | `14` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/ConfirmationPromptView.axaml` | 24 | `Spacing` | `14` | `GapUnrelated` |
| `Views/Dialogs/ConfirmationPromptView.axaml` | 25 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/ConfirmationPromptView.axaml` | 40 | `Spacing` | `6` | `GapRelated` |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 14 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 16 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 17 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 24 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 26 | `Spacing` | `8` | `GapUnrelated` |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 54 | `Margin` | `0 5` | `{tokens:Space Top=GapUnrelated}` |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 55 | `Spacing` | `8` | `GapRelated` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 15 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 17 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 18 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 25 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 27 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 36 | `Margin` | `0 6 0 0` | `{tokens:Space Top=GapRelated}` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 39 | `Margin` | `0 5` | `{tokens:Space Top=GapUnrelated}` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 40 | `Spacing` | `8` | `GapRelated` |
| `Views/Dialogs/ErrorMessageView.axaml` | 24 | `Margin` | `14` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/ErrorMessageView.axaml` | 25 | `Spacing` | `14` | `GapUnrelated` |
| `Views/Dialogs/ErrorMessageView.axaml` | 26 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 14 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 16 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 30 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 31 | `Margin` | `4` | removed (the group box now carries `GroupBoxPadding`) |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 53 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 56 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 68 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 89 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 90 | `Margin` | `4` | removed (the group box now carries `GroupBoxPadding`) |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 107 | `Spacing` | `6` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 119 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 122 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 134 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 18 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 20 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 21 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 28 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 30 | `Spacing` | `6` | `GapRelated` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 37 | `ColumnSpacing` | `16` | `GapUnrelated` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 40 | `RowSpacing` | `6` | `GapRelated` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 116 | `Margin` | `0 5` | `{tokens:Space Top=GapUnrelated}` |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 14 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 17 | `Margin` | `0 8 0 0` | `{tokens:Space Top=GapUnrelated}` |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 18 | `Spacing` | `8` | `GapRelated` |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 32 | `Margin` | `8 0 0 0` | `{tokens:Space Left=GapRelated}` |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 33 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 54 | `Margin` | `0 12 0 0` | `{tokens:Space Top=GapUnrelated}` |
| `Views/Dialogs/LaunchErrorView.axaml` | 25 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/LaunchErrorView.axaml` | 27 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/LaunchErrorView.axaml` | 28 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/OptionsDialogView.axaml` | 15 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/OptionsDialogView.axaml` | 17 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/OptionsDialogView.axaml` | 24 | `Padding` | `10 15` | `GroupBoxPadding` |
| `Views/Dialogs/OptionsDialogView.axaml` | 26 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/OptionsDialogView.axaml` | 49 | `Padding` | `10 15` | `GroupBoxPadding` |
| `Views/Dialogs/OptionsDialogView.axaml` | 51 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/OptionsDialogView.axaml` | 55 | `Margin` | `20 0 0 0` | `{tokens:Space Left=GroupBoxInset}` |
| `Views/Dialogs/OptionsDialogView.axaml` | 65 | `Margin` | `0 5` | `{tokens:Space Top=GapUnrelated}` |
| `Views/Dialogs/OptionsDialogView.axaml` | 66 | `Spacing` | `8` | `GapRelated` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 14 | `Margin` | `12` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 16 | `Spacing` | `8` | `GapUnrelated` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 54 | `Margin` | `4 0` | `{tokens:Space Horizontal=GapLabel}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 68 | `Margin` | `4 0` | `{tokens:Space Horizontal=GapLabel}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 76 | `Margin` | `4 0` | `{tokens:Space Horizontal=GapLabel}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 114 | `Margin` | `6 3` | `{tokens:Space Horizontal=GapRelated, Vertical=GapSmallest}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 136 | `Margin` | `6 2` | `{tokens:Space Horizontal=GapRelated, Vertical=GapSmallest}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 171 | `Margin` | `14` | `{tokens:Space GapUnrelated}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 187 | `Spacing` | `6` | `GapRelated` |
| `Views/Dialogs/SaveChangesPromptView.axaml` | 23 | `Margin` | `14` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/SaveChangesPromptView.axaml` | 24 | `Spacing` | `14` | `GapUnrelated` |
| `Views/Dialogs/SaveChangesPromptView.axaml` | 25 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/SaveChangesPromptView.axaml` | 40 | `Spacing` | `6` | `GapRelated` |
| `Views/IniConfig/IniEditorView.axaml` | 7 | `ColumnSpacing` | `8` | `GapRelated` |
| `Views/IniConfig/IniEditorView.axaml` | 8 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/IniConfig/IniEditorView.axaml` | 10 | `RowSpacing` | `4` | `GapRelated` |
| `Views/IniConfig/IniEditorView.axaml` | 43 | `Margin` | `10 8` | `{tokens:Space Horizontal=GapUnrelated, Vertical=GapRelated}` |
| `Views/IniConfig/IniEditorView.axaml` | 52 | `Spacing` | `6` | `GapRelated` |
| `Views/IniConfig/IniPlaceholderView.axaml` | 8 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/IniConfig/IniPlaceholderView.axaml` | 9 | `Padding` | `10 15` | `GroupBoxPadding` |
| `Views/IniConfig/IniPlaceholderView.axaml` | 11 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/IniConfig/IniPlaceholderView.axaml` | 19 | `Spacing` | `4` | `GapRelated` |
| `Views/IniConfig/IniPlaceholderView.axaml` | 37 | `Margin` | `0 8 0 0` | `{tokens:Space Top=GapUnrelated}` |
| `Views/IniConfig/IniPlaceholderView.axaml` | 39 | `Spacing` | `6` | `GapRelated` |
| `Views/IniConfig/IniSectionView.axaml` | 7 | `Spacing` | `6` | `GapRelated` |
| `Views/IniConfig/IniSectionView.axaml` | 9 | `Spacing` | `6` | `GapRelated` |
| `Views/IniConfig/IniSectionView.axaml` | 24 | `Margin` | `0 4 0 0` | `{tokens:Space Top=GapRelated}` |
| `Views/IniConfig/IniSectionView.axaml` | 25 | `Spacing` | `6` | `GapRelated` |
| `Views/IniConfig/IniSectionView.axaml` | 39 | `Margin` | `0 2` | `{tokens:Space Vertical=GapSmallest}` |
| `Views/IniConfig/IniSectionView.axaml` | 69 | `Margin` | `0 6 0 0` | `{tokens:Space Top=GapRelated}` |
| `Views/MainWindow.axaml` | 37 | `Margin` | `5 1 5 2` | `{tokens:Space Left=GapLabel, Top=GapSmallest, Right=GapLabel, Bottom=GapSmallest}` |
| `Views/MainWindow.axaml` | 66 | `Margin` | `5 6 5 8` | `{tokens:Space MarginDialog}` |
| `Views/MainWindow.axaml` | 70 | `Margin` | `0 2 0 0` | `{tokens:Space Top=GapSmallest}` |
| `Views/MainWindow.axaml` | 83 | `Margin` | `0 2 0 0` | `{tokens:Space Top=GapSmallest}` |
| `Views/Tabs/GeneralTabView.axaml` | 7 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Tabs/GeneralTabView.axaml` | 8 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Tabs/GeneralTabView.axaml` | 10 | `Spacing` | `8` | `GapLabel` |
| `Views/Tabs/GeneralTabView.axaml` | 18 | `Padding` | `10 15` | `GroupBoxPadding` |
| `Views/Tabs/GeneralTabView.axaml` | 19 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 50 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Tabs/GeneralTabView.axaml` | 52 | `Spacing` | `4` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 63 | `ColumnSpacing` | `8` | `GapUnrelated` |
| `Views/Tabs/GeneralTabView.axaml` | 64 | `Margin` | `0 2 0 0` | `{tokens:Space Top=GapSmallest}` |
| `Views/Tabs/GeneralTabView.axaml` | 66 | `RowSpacing` | `2` | `GapSmallest` |
| `Views/Tabs/GeneralTabView.axaml` | 141 | `Padding` | `10 15` | `GroupBoxPadding` |
| `Views/Tabs/GeneralTabView.axaml` | 142 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 150 | `Spacing` | `6` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 152 | `ColumnSpacing` | `20` | `GapUnrelated` |
| `Views/Tabs/GeneralTabView.axaml` | 154 | `RowSpacing` | `2` | `GapSmallest` |
| `Views/Tabs/GeneralTabView.axaml` | 207 | `Padding` | `10 15` | `GroupBoxPadding` |
| `Views/Tabs/GeneralTabView.axaml` | 208 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 216 | `Spacing` | `6` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 218 | `ColumnSpacing` | `20` | `GapUnrelated` |
| `Views/Tabs/GeneralTabView.axaml` | 220 | `RowSpacing` | `2` | `GapSmallest` |
| `Views/Tabs/GeneralTabView.axaml` | 258 | `Spacing` | `6` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 275 | `Spacing` | `6` | `GapRelated` |
| `Views/Tabs/IniConfigTabView.axaml` | 9 | `Margin` | `0,0,0,6` | `{tokens:Space Bottom=GapRelated}` |
| `Views/Tabs/IniConfigTabView.axaml` | 12 | `Margin` | `0,0,6,0` | `{tokens:Space Right=GapRelated}` |

### How the values were classified (Task 3)

Tokens in the table are shown as written in the views: a bare name is a `{StaticResource …}`, and `{tokens:Space …}` is the `SpaceExtension`. Rulings behind the less obvious rows:

- **Icon beside text.** Every `HorizontalSpacing` of `10` beside an icon became `GapRelated`; the text's `5 0 0 0` margin was removed, and the icon took `IconBesideBody` (Boot
  views, Add / Edit / Delete / Info dialogues, General tab, Fix, INI status) or `IconBesideMessage` (confirmation, error, launch-error and save prompts). The derived margins
  (`IconBesideBodyMargin` 5, `IconBesideMessageMargin` 11) live in `Spacing.axaml` with the arithmetic beside them.
- **Group boxes.** Every `Padding` of `10 15`, and the Auto-locate trail's `11 15 15 12`, became `GroupBoxPadding` (left and right `GroupBoxInset` 14, bottom
  `GroupBoxBottom` 11, top `GroupBoxFirstControl` 18 minus the 6 px the title text rises above the frame line = 12). The Fix dialogue's two group boxes had
  no padding and a `Margin="4"` on the dock inside; the margin went and they now take `GroupBoxPadding` like every other group box.
- **Dialogue and tab-page body margins** (`8`, `12`, `14`) became `MarginDialog`; gaps between sections and paragraphs (`8`, `10`, `14`) `GapUnrelated`; button rows, form rows and
  stacks of related controls (`4`, `5`, `6`, `8`) `GapRelated`; a label stacked over its control (`2` in the Auto-locate candidate, `8` in the Installation Profile row) `GapLabel`.
- **Judgement calls.** Button `Padding` `10 0` (Add / Info / Manage) has no guideline role: it took `GapUnrelated` as the nearest sensible token; Open's
  `25 0` was dropped for `DialogButton`. The Options explanatory text's `20 0 0 0` indent under a checkbox took `GroupBoxInset`. The `0 5` margin around
  each dialogue's button row became `Top=GapUnrelated` (the dialogue's `MarginDialog` gives the bottom edge). Data-grid cell margins `4 0` became
  `Horizontal=GapLabel`; the Restore diff rows' `6 3` and `6 2` became `Horizontal=GapRelated, Vertical=GapSmallest`.
- **Label-to-value column gaps** (Info `16`, General Display and System `20`, General Status `8`) all became `GapUnrelated`.
- **Main window.** The menu margin `5 1 5 2` became `Horizontal=GapLabel, Vertical=GapSmallest`; the content margin `5 6 5 8` became `MarginDialog` (the
  boot views have no margin of their own, and Win9x insets the tab control the same); the status bars' `0 2 0 0` became `Top=GapSmallest`. The window size
  and the INI tab's row-label width are untouched.
