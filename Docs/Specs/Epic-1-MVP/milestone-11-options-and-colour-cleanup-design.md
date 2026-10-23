# Options Dialogue and Colour Cleanup — Design Spec

> **Milestone:** 11 (Epic 1 — MVP)
> **Date:** 2026-10-21
> **Author:** Justinian
> **Status:** Approved for implementation (calls C1–C7 accepted)
> **Authoritative spec:** [1-zoo-tycoon-nexus-master-design.md](../1-zoo-tycoon-nexus-master-design.md) §5.1, §5.2, §9.8, §9.10, §9.11, §9.12, §13.2
> **Deferred work taken:** DW-37 (rename Settings to Options), DW-39 (closed by dropping dark mode), DW-20 (colour tokens as bare strings)
> **Design reference:** `Docs/UserInterfaceDesign/HiFiPrototype/src/dialogs.jsx` (`SettingsDialog`), `Docs/UserInterfaceDesign/conventions.md` §1
> **Branch:** `feat/milestone-11-options-and-colour-cleanup`

---

## 1. Goal

Give the launcher its preferences dialogue, **Options**, so a player can choose what opens at start-up and whether the launcher closes once the game starts —
both settings the launcher already stores and obeys, with no way to change them today. At the same time, **drop the dark-mode feature entirely** (maintainer's
decision: the Windows 95 look has no dark variant, and a half-built one should not linger) and **define every colour the views use once**, by name, so the look
stays consistent and a stray literal colour is caught by a test.

## 2. Decisions (maintainer)

| # | Question                         | Decision                                                                                                                                       |
|---|----------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | Dark mode / themes               | **Scrapped.** No Theme group, no theme setting, no theme service. Remove what already exists (the `LauncherTheme` enum, property and column).   |
| 2 | Colour cleanup                   | **In this milestone**, light only: named colours replace literal colours in views and the bare `"Green"` / `"Red"` strings in code (DW-20).     |
| 3 | Dialogue name                    | **Options**, opened from `File → Options…` (DW-37, Win9x standard commands).                                                                     |
| 4 | Dialogue contents                | The prototype's Settings dialogue minus its Theme group: **Startup** and **Game launch**. Nothing else.                                          |
| 5 | Out of scope                     | Help → Contents and the About dialogue.                                                                                                         |

## 3. Non-goals and anti-goals

- No theme of any kind, no `DynamicResource` theme switching, no OS dark-mode detection.
- No new settings beyond the two the launcher already has.
- Not a restyle: the cleanup must not change how the launcher looks, apart from the two literal colours that disagree with the conventions (C5).

## 4. Calls made by the author

These go in the ledger as rulings when the plan runs; each is cheap to reverse.

- **C1 — Copy is the prototype's**, with the Theme group removed and the title changed: window title `Options` (two leading spaces, conventions §4.1), title-bar
  icon `controls32.ico` (conventions §4.1 already lists it for this dialogue). **Startup** group: muted line *"When the launcher opens, restore:"*, then four radio
  buttons — `The default installation`, `The installation I last played`, `The installation I last opened in the launcher`, `No installation — show the picker`.
  **Game launch** group: check box `Close launcher after the game starts`, with the muted helper *"When ticked, the launcher exits as soon as `zoo.exe` reports
  started."* (`zoo.exe` in the monospace run, indented under the check box). Footer `OK` (default) and `Cancel`, right-aligned, `MinWidth="75"`.
- **C2 — Saving.** The dialogue loads the current values when it opens. `OK` saves both values in one command and closes; `Cancel`, the title-bar close and
  `Esc` discard. `OK` is enabled once the stored values have loaded (so a failed load cannot overwrite them with defaults); saving unchanged
  values is harmless. A save failure shows `Cannot Save Options` with the error and keeps the dialogue open. Nothing applies live: the startup preference is read at the
  next start and the close setting at the next launch, which is already how the code works.
  `DefaultInstallationId` is never touched by this dialogue.
- **C3 — The menu item.** `File → Options…` replaces the inert `Settings…` item in the same place. `O` is already `Open Installation…`'s accelerator, so this
  item uses `P` (`O<u>p</u>tions…`, as Windows Explorer's *Folder Options* does). The item is enabled in every main-window state after boot (the menu is hidden
  while booting).
- **C4 — Dropping the theme.** Delete `LauncherTheme` and `LauncherSettings.Theme`; an EF Core migration on `LauncherDbContext` drops the `Theme` column. The
  view model, command and view names use `Options` (`OptionsDialogViewModel`, `OptionsDialogView`, `SaveLauncherOptionsCommand`); the `LauncherSettings` entity
  and table keep their names (DW-37).
- **C5 — The colour set.** A `Colours.axaml` resource dictionary (merged in `App.axaml`) defines one `SolidColorBrush` per colour the views use, keyed by the
  conventions §1 names, light values only: `ForegroundMuted` `#808080`, `ForegroundDim` `#404040`, `AccentGreen` `#008000`, `AccentRed` `#C00000`,
  `AccentMaroon` `#800000`, `AccentNavy` `#000080`, `AccentAmber` `#806000`, `Surface` `#C0C0C0`, `InputBackground` `#FFFFFF`, `MidShadow` `#808080`, plus one new
  key, `RuleDotted` `#E0E0E0`, for the Restore dialogue's dotted row separators. Views use `{StaticResource …}` (no theme switching, so nothing dynamic is
  needed). Two literals disagree with the conventions and change appearance slightly: XAML `Red` (`#FF0000`) becomes `AccentRed` `#C00000`, and `DarkGoldenrod`
  becomes `AccentAmber` `#806000` — both the prototype's values. `Transparent` stays a literal (it is not a colour choice).
- **C6 — Status colours leave the Domain.** `InstallationValidity.ColourToken` is removed (a colour is presentation, not domain). The Desktop maps validity to a
  brush with a converter (`Valid` → `AccentGreen`, anything else → `AccentRed`); the grid's `IsSelectionInvalid` tests the validity itself, not the string
  `"Red"`; the Info dialogue binds the validity. `MainWindowViewModel`'s `Brushes.Red` status colour becomes a boolean, turned into a brush by a converter (not
  a style block, which would push `MainWindow.axaml` past its 100-line cap). View models hold no brushes.
- **C7 — A test keeps it clean.** An architecture test fails when any `*.axaml` under the Desktop project sets `Foreground`, `Background`, `BorderBrush`,
  `Fill`, `Stroke` or `Color` to a literal colour (a name or `#…`) rather than a resource or binding — `Transparent` and `Colours.axaml` itself excepted — and
  when Desktop or Domain code uses `Brushes.` or `Colors.`.

## 5. SDD and conventions amendments (same milestone)

- §5.1 / §5.2: remove `Theme` from `LauncherSettings`, the `LauncherTheme` enum, and `ColourToken` from the `InstallationValidity` listing; §6.2: remove the
  `Theme` column. Sweep the rest of the SDD for theme / dark-mode / `ColourToken` mentions (for example the conventions summary in §9 and the prototype GIF note).
- §9.8: retitle **Options dialogue**, `File → Options…`, two group boxes as built (C1, C2), no theme.
- §9.10: the menu row becomes `O<u>p</u>tions…` (C3), with the ellipsis.
- §9.11: delete the section (and its table-of-contents entry); renumber nothing — leave a one-line *"Removed in milestone 11: the launcher has one, light,
  appearance."* note so later references resolve.
- §9.12 and §13.2: drop the dark/light theme mentions; §13.2's Settings bullet becomes Options without theming.
- §13.5: delete DW-37, DW-39 and DW-20.
- conventions §1: remove the Dark columns and the "Bevel topology … between light and dark" and "swap colour between themes" sentences; add `RuleDotted`; say the
  colours live in `Colours.axaml` and are used through `StaticResource`.
- Revision 1.13.

## 6. Testing

- **Application:** the options query returns the stored values; the save command writes both and leaves `DefaultInstallationId` alone; its validator rejects an
  unknown startup preference.
- **Infrastructure:** the migration applies to a database created by the previous schema; the settings round-trip without a `Theme` column.
- **Desktop:** the dialogue loads the stored values, `OK` sends one save with the chosen values and closes, `Cancel` sends nothing, a failed save shows the error
  and stays; the menu command opens it; the validity-to-brush converter; `IsSelectionInvalid` without strings.
- **Architecture:** C7.
- **In the running app:** open `File → Options…`, change both settings, restart the launcher and confirm the startup choice took effect, launch-close behaviour
  checked by reading the stored value (the game is not launched). Before any colour change lands, capture the General tab, INI editor, Installation Manager
  and Restore dialogue; afterwards compare them to confirm only the two C5 colours moved.
