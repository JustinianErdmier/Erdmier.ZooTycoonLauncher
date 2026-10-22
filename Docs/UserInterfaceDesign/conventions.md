# UI conventions

> Companion to [SDD §9](../Specs/1-zoo-tycoon-launcher-master-design.md#9-user-interface). Where §9 describes **what** each view contains, this doc fixes the recurring
> **how** — tokens, layout primitives and icon names — so an agent implementing one slice can do it without reverse-engineering the prototype.

---

## 0. How to use this doc

When you sit down to implement a state, a tab, a dialogue, or an INI section:

1. Read SDD §9.x for the screen you're building.
2. Watch the relevant section of `ZooTycoonLauncherHiFiUIPrototype.gif` once.
3. Read this doc end-to-end **the first time**. After that, jump to the named patterns by name (`Section §3 Standard INI row`, `Section §5 Dialogue footer`).
4. Reach for the named colours and resource keys here. If you find yourself hard-coding a colour, an icon path, or a spacing number that isn't in this doc, stop and add it
   here first.

This doc is opinionated, not exhaustive. It captures the things that recur **across multiple views**. One-off affordances belong in §9.x of the SDD.

---

## 1. Token registry

The launcher overlays a small set of named brushes on top of `Classic.Avalonia.Theme`. The colours the views use are defined once, by name, in
`Source/Erdmier.ZooTycoonLauncher.Desktop/Styles/Colours.axaml` (merged in `App.axaml`), and views reach for a key through `{StaticResource …}`. There is one, light,
appearance, so nothing needs dynamic resolution. A literal colour in a view is a defect: the architecture test `NoLiteralColoursTests` fails on any `*.axaml` that sets
`Foreground`, `Background`, `BorderBrush`, `Fill`, `Stroke` or `Color` to a name or `#…` instead of a key (`Transparent` is not a colour choice and stays a literal), and on
`Brushes.` or `Colors.` in Desktop or Domain code. View models hold no brushes: a converter turns a state into a brush.

`Colours.axaml` defines the keys the views use today (`Surface`, `MidShadow`, `ForegroundMuted`, `ForegroundDim`, `InputBackground`, `AccentGreen`, `AccentRed`,
`AccentMaroon`, `AccentNavy`, `AccentAmber` and `RuleDotted`). The other keys below are reserved role names: add one to `Colours.axaml` when a view first needs it.

### 1.1 Neutrals

The names describe the *role*, not the colour: `LightHighlight` is "the bright bevel edge" and `DarkShadow` is "the deepest bevel edge".

| Key                       | Role                                             | Light             |
|---------------------------|--------------------------------------------------|-------------------|
| `Surface`                 | Window / button background; the primary chrome   | `#C0C0C0` silver  |
| `SurfaceHighlight`        | Inner-light highlight on raised surfaces         | `#DFDFDF`         |
| `LightHighlight`          | Outer bright bevel edge                          | `#FFFFFF`         |
| `MidShadow`               | Mid bevel shadow                                 | `#808080`         |
| `DeepShadow`              | Deeper inner shadow                              | `#404040`         |
| `DarkShadow`              | Outermost bevel edge                             | `#000000`         |
| `Foreground`              | Default text colour                              | `#000000`         |
| `ForegroundMuted`         | Muted / hint text                                | `#808080`         |
| `ForegroundDim`           | Faint label colour (Info dialogue row labels, INI sub-headers) | `#404040` |
| `InputBackground`         | TextBox / ListBox / DataGrid background          | `#FFFFFF`         |
| `InputBackgroundReadOnly` | Read-only input                                  | `#C0C0C0`         |
| `InputBackgroundInvalid`  | Validation-error input background                | `#FFE0E0`         |
| `RuleDotted`              | Dotted row separators (Restore dialogue)         | `#E0E0E0`         |

### 1.2 Accents

Use them through their key, not by name. The values match the prototype's hardcoded swatches.

| Key                | Use                                                                                                              | Light     | Replaces prototype `dk-c-*` class |
|--------------------|------------------------------------------------------------------------------------------------------------------|-----------|-----------------------------------|
| `AccentNavy`       | Title-bar gradient start; key informational accent (diff "Snapshot" column)                                      | `#000080` | `dk-c-navy`                       |
| `AccentNavyBright` | Title-bar gradient end                                                                                           | `#1084D0` | —                                 |
| `AccentTeal`       | Desktop backdrop                                                                                                 | `#008080` | —                                 |
| `AccentGreen`      | OK / valid badge; "EXE present" / "INI present" headlines                                                        | `#008000` | `dk-c-green`                      |
| `AccentRed`        | Strong error text ("No EXE found!", invalid-name message); "Cannot Play" heading (the prototype draws it maroon) | `#C00000` | `dk-c-red`                        |
| `AccentMaroon`     | Dirty-state ("● Unsaved changes"); diff "Current" col                                                            | `#800000` | `dk-c-maroon`                     |
| `AccentAmber`      | "No INI found" headline; warn-level badges                                                                       | `#806000` | `dk-c-amber`                      |
| `AccentYellow`     | Tooltip background (Classic.Avalonia default — keep as-is)                                                       | `#FFFF80` | —                                 |

### 1.3 Typography

| Key                  | Stack                                                                      | Size  |
|----------------------|----------------------------------------------------------------------------|-------|
| `FontFamilySystem`   | `Microsoft Sans Serif, MS Sans Serif, W95FA, Tahoma, Geneva, sans-serif`   | 11 px |
| `FontFamilyMono`     | `Lucida Console, Courier New, monospace`                                   | 11 px |
| `FontSizeBase`       | Base UI text                                                               | 11 px |
| `FontSizeStrong`     | State headlines ("Ready to Play", "Cannot Play", "Looking for Zoo Tycoon…") | 14 px |
| `FontSizeAboutTitle` | About-dialogue product name                                                | 13 px |

The launcher disables sub-pixel anti-aliasing on body text (`-webkit-font-smoothing: none` in the prototype; Avalonia equivalent is `TextOptions.TextRenderingMode="Alias"` on the root window). Mono runs always carry the `FontFamilyMono` style.

### 1.4 Spacings

| Key                | Value   | Use                                                          |
|--------------------|---------|--------------------------------------------------------------|
| `SpacingXSmall`    | 2 px    | Inner padding of bevels                                      |
| `SpacingSmall`     | 4 px    | Default control-to-control gap                               |
| `SpacingMedium`    | 6 px    | Default footer button gap                                    |
| `SpacingLarge`     | 10–12 px | Section-to-section gap inside a window                      |
| `SpacingDialogPad` | 14 px   | Dialogue body padding                                        |
| `RowLabelWidth`    | 190 px  | INI tab row label column (§3)                                |
| `MainWindowWidth`  | 720 px  | Fixed                                                        |
| `MainWindowHeight` | 460 px  | Fixed                                                        |

---

## 2. View-composition primitives

Reuse these wherever you can. Each is a single `UserControl` (or `Style`) in `Desktop/Views/Primitives/`.

| Primitive            | Role                                                                                       |
|----------------------|--------------------------------------------------------------------------------------------|
| `GroupBox`           | Bevelled fieldset with a `Header` text label; replaces every `<GroupBox label="…">` in the prototype. |
| `IconBlock`          | `Image` sized to `Width="32" Height="32"` with `RenderOptions.BitmapInterpolationMode="None"` (the ICO must stay crisp). Variant `IconBlockSmall` is 16 px (title-bar icons). |
| `StatusBadge`        | Inline pill: `OK` (green bold), `Err` (red bold), `Warn` (amber bold), `Neutral` (foreground). Use `Variant` property. |
| `MutedText`          | A `TextBlock` style hooked to `ForegroundMuted` and `FontSizeBase`. Common enough to deserve its own style. |
| `Etched`             | 1-px horizontal divider with the classic two-line shadow (used in the About dialogue and between INI sub-headers). |

`GroupBox` and `IconBlock` together compose the "icon header" pattern in §4.

---

## 3. Standard INI row

Every editable INI key renders the same shape, regardless of whether the control is a CheckBox, NumericUpDown, TextBox, ComboBox, or radio. The row is a horizontal `Grid` with two columns:

```
┌─────────────────────────────┬───────────────────────────────────┐
│ <mono-label>                │ <input-control>                   │
│ <muted hint sub-line>       │                                   │
└─────────────────────────────┴───────────────────────────────────┘
   RowLabelWidth (190 px)        minmax(0, 1fr)
```

Use a `Grid` with `ColumnDefinitions="190,*"` and the right column's `MinWidth="0"`. Avalonia's flex-equivalent quirk is the same as the prototype's: without an explicit `MinWidth="0"` (or `Width="*"` with `SharedSizeGroup` constraints), long combo content blows out the column.

### 3.1 Label

`TextBlock` with `FontFamily="{DynamicResource FontFamilyMono}"` and `FontSize="11"`. The text is the **literal INI key** (`fullscreen`, `screenwidth`, `MSStartingCash`, `tooltipDelay`, `Lang`, …) — not a friendly version. The friendly version goes in the hint or, if it doesn't fit, in the tooltip.

### 3.2 Hint sub-line

Optional `TextBlock` immediately below the label, styled with `MutedText`. Carries the *units / range / shape* of the value: `px`, `1–60 ticks/sec`, `$0–$10,000,000`,
`0=Off · 1=Standard · 2=Verbose`, `Inverted in INI` (for `noMenuMusic`), etc. Defaults stay on the input bounds, not in the hint.

### 3.3 Help wiring

Every row participates in the hover + status help affordance (SDD §9.3.2):

1. **Tooltip.** Set `ToolTip.Tip` on the **row's parent panel** (the `Grid` itself, not the label or input). Avalonia's tooltip propagates from the hovered visual up the parent
   chain, so attaching to the panel makes the whole row trigger the tip. Tip text comes from `IniFieldDescriptor.Help` in `IniEditorCatalogue` (`Desktop/Models/IniConfig/`), not
   `IIniHelpRegistry`.
2. **Editor footer one-liner.** The row template in `IniSectionView` sets the field's `IsHovered` on `PointerEntered` / `PointerExited`. The editor's own footer (not the
   main-window status bar) shows the hovered row's help, else the focused row's, else the dirty / saved state.
3. **Keyboard parity.** The same row template sets the field's `IsFocused` on `GotFocus` / `LostFocus` for the row's input control, so tab navigation surfaces the same help
   line when no row is hovered.

The wiring lives once in `IniSectionView`'s row template; rows never hand-roll it.

### 3.4 Section header and sub-header

Each section begins with a `SectionHeader` primitive:

```
[user] section of zoo.ini
———————————————————————————
```

Mono-bold section name + muted "section of `zoo.ini`" continuation + a 1-px solid `Surface`-shaded underline.

`SubHeader` (only used inside `[UI]`): mono bold text in `ForegroundDim`, dotted-underlined in `SurfaceHighlight`. Reads `Audio`, `Gameplay (cash)`, `Interface`.

---

## 4. Group-box + icon header pattern

Used by the Status / Display / INI status / Fix-EXE / Fix-INI / Auto-locate-trail group boxes — basically every state surface that frames itself in a `GroupBox`. Shape:

```
┌─ Status ────────────────────────────────────────────────┐
│ [ICON 32px]   <headline>                  [ACTION BTN]  │
│               <body / table / muted prose>              │
└─────────────────────────────────────────────────────────┘
```

- **Icon column** is 32 px wide, top-aligned (or centred when the body is one line), pinned to the left. Use `IconBlock`.
- **Body column** fills the remaining width. The first line is a bold headline (14 px for the five main-window states, 11 px elsewhere). Subsequent lines are muted prose, tables, or nested controls.
- **Action button(s)** sit at the right when the row is short (Ready / NoInstall — single primary action) or in a footer row below the body when there are multiple actions or the body is long (Cannot Play — `Fix` + `Manage Installations`).
- **Headline colour.** Red for Cannot Play (the `AccentRed` token; the prototype draws it maroon), foreground (`Foreground`) elsewhere.
- **Muted state.** When the whole group needs to read as "not actionable" (Cannot Play's Display), set `Opacity="0.55"` on the `GroupBox`.
  Don't recolour individual children — the opacity layer does the right thing.

### 4.1 Icon vocabulary

The icons are the Windows 95 set committed under `Source/Erdmier.ZooTycoonLauncher.Desktop/Assets/Win95Icons/`, referenced from XAML as
`/Assets/Win95Icons/<name>` (an `Image Source=` or a window `Icon=`). The file name carries the artwork's native size (`ok32.gif`, `foldercatalog16.ico`), but the
`Height` / `Width` on the `Image` decide the rendered size. The launcher's own mark lives in `Assets/BrandIcons/` and `Assets/Images/`. The table maps each site to the
file its view uses today; a site whose view does not exist yet is marked *(not built)* and has no file yet.

| Site                                               | Icon                                        | Notes                                                         |
|----------------------------------------------------|---------------------------------------------|---------------------------------------------------------------|
| Main window and the small prompts · title bar      | `BrandIcons/ZTL_Title-Icon.png`             | App-owned mark; also the Confirmation, Error and Save prompts |
| Play · General tab · Status group box, can play    | `ok32.gif`                                  |                                                               |
| Play · General tab · Status group box, cannot play | `warning32.gif`                             |                                                               |
| Play · General tab · Display group box             | `mystify16.ico`                             | Drawn at 32 px                                                |
| Play · General tab · Your System group box         | `mycomputer16.ico`                          | Drawn at 32 px                                                |
| Play · General tab · ready / not-ready line        | `ok16.gif` / `error32.gif`                  | Both drawn at 16 px                                           |
| Looking for Zoo Tycoon                             | `searchcomputer16.ico`                      | Drawn at 32 px                                                |
| No Game Installation Found                         | `error32.gif`                               |                                                               |
| Open Game Installation                             | `foldercatalog16.ico`                       | Drawn at 32 px                                                |
| Installation Manager · title bar                   | `installationoncomputer16.ico`              |                                                               |
| Add / Edit Installation · title bar + body         | `foldercatalog16.ico`                       |                                                               |
| Installation form · error line                     | `cancel16.gif`                              |                                                               |
| Delete Installation · title bar + body             | `warning32.gif`                             |                                                               |
| Installation Info · title bar / body               | `foldercatalog16.ico` / `information32.gif` |                                                               |
| Fix Installation · title bar                       | `warning32.gif`                             |                                                               |
| Fix EXE / Fix INI group boxes · valid / invalid    | `ok32.gif` / `error32.gif`                  |                                                               |
| Restore Previous INI · title bar                   | `sheetsanddocuments32.ico`                  |                                                               |
| Confirmation and Save-changes prompts · body       | `warning32.gif`                             |                                                               |
| Error message · body                               | `error32.gif`                               |                                                               |
| Launch error · body                                | `cancel32.gif`                              |                                                               |
| INI tab · No INI sub-state                         | `warning32.gif`                             |                                                               |
| INI tab · read-only strip                          | `warning32.gif`                             | Drawn at 16 px                                                |
| INI tab placeholder (disabled)                     | *(no icon)*                                 | Muted helper text only                                        |
| Options · title bar                                | `controls32.ico`                            |                                                               |
| About · title bar *(not built)*                    | *(none chosen)*                             | No view yet; body keeps the app logo                          |

Every `ClassicWindow` title starts with two spaces so the text clears the title-bar icon (Classic.Avalonia leaves no gap), including titles bound from a view model.


---

## 5. Dialogue footer

Every modal dialogue ends with a horizontal button row aligned to the right. Universal rules:

- **Ellipses follow the Win9x rule.** A button or menu item whose command needs more input before it can complete — it opens a dialogue or picker to collect
  that input — ends with an ellipsis (`Browse…`, `Open Installation…`). Buttons that commit a decision on the spot (`Save`, `Cancel`, `OK`, `Close`, `Restore`) carry
  none, and neither do commands that only show information or ask for a confirmation.
- **Default button** is the rightmost in single-action footers (`OK` / `Close`), or the leftmost-of-the-confirming-pair when there's a primary-plus-cancel (`Save` `Cancel`, `Restore` `Cancel`, `Delete` `Cancel`). Render the default with `Classes="default"` (Avalonia equivalent of the prototype's `--default` outline).
- **Gap.** `SpacingMedium` (6 px) between buttons. `SpacingDialogPad` (14 px) between the body and the footer.
- **Width.** Buttons are `MinWidth="75"`; the `default` variant gets the same width. The "wide" `Launch Game` and `Add Installation` calls-to-action on the main-window states bump to `MinWidth="110"`.

Footer order from left to right matches the natural reading order: positive action(s) first, destructive action(s) second, dismissive action last. The prototype follows this and so does §9.

---

## 6. Hover + status help wiring

The INI editor's footer label carries help: the hovered or focused row's `Help` (italic), otherwise `● Unsaved changes` (maroon, bold), otherwise
`All changes saved · Last write: <local time>` (muted). There is no `IStatusBarSink` or `IIniHelpRegistry`; the main-window status bar is unaffected (SDD §9.3.2).

---

## 7. Colours

There is no theming: the launcher has one, light, appearance, and the dark-mode feature was dropped in milestone 11 (SDD §9.11).

- **One place.** Every colour the views use is a `SolidColorBrush` in `Styles/Colours.axaml` (§1), merged in `App.axaml`.
- **`StaticResource`.** Views reference a key as `{StaticResource AccentRed}`; nothing in the table needs `DynamicResource`.
- **Chosen by converters.** A view model exposes state (`IsStatusError`, `InstallationValidity`), never a brush. `ValidityToBrushConverter` maps `Valid` to `AccentGreen` and
  anything else to `AccentRed`; `ErrorToBrushConverter` maps `true` to `AccentRed` and otherwise leaves the default foreground.
- **Guarded by a test.** `NoLiteralColoursTests` (Tests.Architecture) fails on a literal colour in a Desktop `*.axaml` and on `Brushes.` or `Colors.` in Desktop or Domain code.
  Muted text is `Opacity="0.6"` or `ForegroundMuted`, as the views already do.

---

## 8. Button labels and writing style

- **British English everywhere** (`Behaviour`, `Colour`, `Minimise`, `Initialise`). Don't switch a US spelling that comes from a Windows API surface (`Color`, `Initialize` on `IInitializable`).
- **Sentence case** in labels and headings, not Title Case. `Restore previous INI`, not `Restore Previous INI` — *except* when the phrase is itself a proper noun (window titles like `Installation Manager`, `Zoo Tycoon Launcher`).
  - SDD §9 currently writes some titles in Title Case; treat the SDD's title as the canonical window title text. Body copy and inline buttons remain sentence case.
- **Ellipses follow the Win9x rule** (see §5), on buttons and menu items alike (`Browse…`, `Open Installation…`, `Installation Manager…`).
- **No periods at the end of single-sentence muted prose** unless the prose is multi-sentence. (Match the prototype.)
- **Mono runs** for paths, INI keys, INI sections (`zoo.ini`, `zoo.exe`, `[user]`, `screenwidth`), and timestamps. Use the `FontFamilyMono` token.

---

## 9. State-bar message catalogue

Cell 1 of the status bar carries the primary message; cell 2 carries the secondary detail; cell 3 carries the version. Cell 2's text comes from this table.

| State           | Cell 1                                 | Cell 2                                                   | Notes                                 |
|-----------------|----------------------------------------|----------------------------------------------------------|---------------------------------------|
| Looking         | `Discovering installations…`           | `Reading registry…`                                      | Cell 2 updates as boot phases advance |
| Ready           | `Ready — <installation name>`          | `Display: <w>×<h>` (General) / `INI loaded` (INI Config) |                                       |
| Cannot Play     | `Cannot launch — installation invalid` | `Fix required` (`AccentRed`)                             |                                       |
| No Installation | `No installation registered`           | *(empty)*                                                |                                       |
| Open Picker     | `Choose an installation to open`       | *(empty)*                                                |                                       |

The INI editor's own footer shows unsaved-change state (§6). The full set of state strings is fixed and tested.

---

## 10. Open questions for future revisions

Captured here so the SDD doesn't accumulate UI-shaped notes:

- **High-DPI scaling.** Classic.Avalonia targets 96 DPI; the prototype is pixel-fixed. Phase 1 ships at the OS scale; Phase 3 (§13.4) revisits.
- **Keyboard accelerators on dialogues.** The prototype assigns Alt-accelerators (`Btn accel="A"` etc.); Avalonia's `_A` underscore notation does the same. Audit pass during Phase 1's dialogue work.
- **What's-this `?` button.** The prototype prototyped a Win95 title-bar `?` button as an alternative help affordance and discarded it. Don't reintroduce without explicit user request.
- **Tooltip styling.** Classic.Avalonia's default tooltip is close to the prototype's yellow OS tooltip but not identical. Worth pinning during Phase 1 if it reads wrong
  against the Windows 95 original.
