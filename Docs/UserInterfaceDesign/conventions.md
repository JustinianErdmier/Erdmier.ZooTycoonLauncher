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

The launcher overlays a small set of named values on top of `Classic.Avalonia.Theme`. They live in four files under
`Source/Erdmier.ZooTycoonLauncher.Desktop/Styles/`, all merged in `App.axaml`, and views reach for a key through `{StaticResource …}`:
`Colours.axaml` (§1.1, §1.2), `Typography.axaml` (§1.3), `Spacing.axaml` (§1.4) and `Controls.axaml` (§2). A literal in a view is a defect, and an architecture test
per group (named in each section; §7 lists every colour attribute) fails on it. The colours come first. There is one, light,
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

| Key                | Use                                                                                                              | Light     |
|--------------------|------------------------------------------------------------------------------------------------------------------|-----------|
| `AccentNavy`       | Title-bar gradient start; key informational accent (diff "Snapshot" column)                                      | `#000080` |
| `AccentNavyBright` | Title-bar gradient end                                                                                           | `#1084D0` |
| `AccentTeal`       | Desktop backdrop                                                                                                 | `#008080` |
| `AccentGreen`      | OK / valid badge; "EXE present" / "INI present" headlines                                                        | `#008000` |
| `AccentRed`        | Strong error text ("No EXE found!", invalid-name message); "Cannot Play" heading (the prototype draws it maroon) | `#C00000` |
| `AccentMaroon`     | Dirty-state ("● Unsaved changes"); diff "Current" col                                                            | `#800000` |
| `AccentAmber`      | "No INI found" headline; warn-level badges                                                                       | `#806000` |
| `AccentYellow`     | Tooltip background (Classic.Avalonia default — keep as-is)                                                       | `#FFFF80` |

### 1.3 Typography

Defined in `Styles/Typography.axaml`. `NoLiteralTypographyTests` fails on a literal `FontFamily`, `FontSize` or `FontWeight` in a view or a style setter; `FontStyle="Italic"`
is a style rather than a token and stays a literal.

| Key                  | Value                                                                    | Use                                                                         |
|----------------------|--------------------------------------------------------------------------|-----------------------------------------------------------------------------|
| `FontFamilySystem`   | `Microsoft Sans Serif, MS Sans Serif, W95FA, Tahoma, Geneva, sans-serif` | The default face (defined; the views inherit it rather than set it)         |
| `FontFamilyMono`     | `Lucida Console, Courier New, monospace`                                 | INI keys, paths, timestamps and every other mono run                        |
| `FontSizeBase`       | 11                                                                       | Base UI text                                                                |
| `FontSizeStrong`     | 14                                                                       | State headlines ("Ready to Play", "Cannot Play", "Looking for Zoo Tycoon…") |
| `FontSizeAboutTitle` | 13                                                                       | About-dialogue product name                                                 |
| `FontWeightStrong`   | `Bold`                                                                   | Bold labels and values                                                      |
| `FontWeightHeading`  | `Solid`                                                                  | The weight the state headlines use                                          |

The prototype disables font smoothing on body text (`-webkit-font-smoothing: none`); the launcher does not apply an equivalent (`TextOptions.TextRenderingMode` is not set
anywhere). Mono runs always use `{StaticResource FontFamilyMono}`.

### 1.4 Spacings

Defined in `Styles/Spacing.axaml`, in pixels. The values are Microsoft's Windows 9x layout guideline distances, which the guidelines give in dialog units (DLUs) and which
convert here for 8 pt MS Sans Serif at 96 DPI (about 1.5 px per DLU across, 1.6 px down; a 50 × 14 DLU command button is 75 × 23 px, the `MinWidth="75"` of
`DialogButton`). `NoLiteralSpacingTests` fails on a literal `Margin`, `Padding`, `Spacing`, `RowSpacing`, `ColumnSpacing`, `HorizontalSpacing` or `VerticalSpacing` in a view or a
style setter,
other than `0`.

| Key                    | px | Guideline (DLUs)                                                                            | Use                                                  |
|------------------------|----|---------------------------------------------------------------------------------------------|------------------------------------------------------|
| `GapSmallest`          | 3  | Smallest space between controls (2)                                                         | Tight pairs, such as a label and the value beside it |
| `GapLabel`             | 5  | Text label to its control (3)                                                               | A label above or beside the control it names         |
| `GapRelated`           | 6  | Related controls; buttons in a row; controls inside a group box (4)                         | The default gap, including the footer button gap     |
| `MarginDialog`         | 11 | Dialogue margins on all sides (7)                                                           | A dialogue body's margin to its window               |
| `GapUnrelated`         | 11 | Unrelated controls; paragraphs of text (7)                                                  | Section-to-section gap; body-to-footer gap           |
| `GroupBoxBottom`       | 11 | Last control to the bottom of a group box (7)                                               | Bottom padding of a group box                        |
| `GroupBoxInset`        | 14 | Group box left edge to its controls (9)                                                     | Left and right padding of a group box                |
| `GroupBoxFirstControl` | 18 | Group box top edge, including its header, to its first control (11)                         | Top of a group box to its first control              |
| `GapIconBody`          | 11 | Icon to the body text beside it in a property-sheet layout (measured; not in the guideline) | Icon headers in group boxes and info dialogues       |
| `GapIconMessage`       | 17 | Icon to the message text in a message prompt (measured; not in the guideline)               | Confirmation, error and save prompts                 |

The guidelines name no icon gap, so the two icon tokens come from measuring real screenshots at actual size (Internet Options' tabs; Windows 98's *Error Details* message
box and Classic.Avalonia's message box).

Derived values sit beside the tokens in `Spacing.axaml`, each computed from the tokens above and never written in a view (`DerivedResources_MatchTheTokens` checks the
arithmetic):

- **`IconBesideBodyMargin`** (`0,0,5,0`) and **`IconBesideMessageMargin`** (`0,0,11,0`). An icon sits in a `DockPanel` whose `HorizontalSpacing` is `GapRelated` (the right
  distance from the text to a button), so the icon carries the rest of its gap as a right margin: the icon token minus `GapRelated`. Apply them through the `IconBesideBody`
  and `IconBesideMessage` classes (§2).
- **`GroupBoxHeaderHeight`** (6) and **`GroupBoxPadding`** (`14,12,14,11`). The guideline measures a group box's first control from the top edge of the title text, but
  Classic.Avalonia's content area starts at the frame line, about 6 px below the title's top (measured on screen). The padding is therefore `GroupBoxInset` left and right,
  `GroupBoxFirstControl` minus `GroupBoxHeaderHeight` on top and `GroupBoxBottom` below, so the first control sits `GroupBoxFirstControl` under the box's top edge. The
  Auto-locate trail, a `HeaderedContentControl` drawn as a group box, takes `Padding="{StaticResource GroupBoxPadding}"`.

**Writing a margin or padding.** A `Spacing` takes `{StaticResource GapRelated}`. A `Margin` or `Padding` is a `Thickness`, built from token names by the `{tokens:Space …}`
markup extension (`Styles/SpaceExtension.cs`, `xmlns:tokens="using:Erdmier.ZooTycoonLauncher.Desktop.Styles"`), so no per-combination resource is needed:

```xml
<StackPanel Margin="{tokens:Space MarginDialog}" Spacing="{StaticResource GapRelated}" />
<Border Padding="{tokens:Space Left=GroupBoxInset, Top=GroupBoxFirstControl}" />
<TextBlock Margin="{tokens:Space Horizontal=GapLabel}" />
```

A bare token fills all four sides; `Horizontal`, `Vertical`, `Left`, `Top`, `Right` and `Bottom` name individual sides (an unset side is 0, and the specific sides override the
general ones). An unknown token name throws when the view loads. Layout dimensions that are not spacing (`MainWindowWidth` 720, `MainWindowHeight` 460, `RowLabelWidth` 190) stay
as plain sizes.

---

## 2. View-composition primitives

Reuse these wherever you can. The shared styles live in `Styles/Controls.axaml`; views opt in through `Classes`. `SharedSizeStylesTests` fails on a view that writes
`MinWidth="75"`, a 32 or 16 px `Width`/`Height` on an `Image`, or `Opacity="0.6"` on text, instead of using them.

| Primitive                                          | Role                                                                                                                |
|----------------------------------------------------|---------------------------------------------------------------------------------------------------------------------|
| `Button.DialogButton`                              | A dialogue push button: `MinWidth` 75, the Win95/98 minimum command-button width.                                   |
| `Image.IconLarge` / `Image.IconSmall`              | A 32 px or 16 px icon. Each `Image` also sets `RenderOptions.BitmapInterpolationMode="None"` itself (see below).    |
| `Image.IconBesideBody` / `Image.IconBesideMessage` | The right margin that completes `GapIconBody` or `GapIconMessage` beside the text (§1.4). Combine with `IconLarge`. |
| `TextBlock.MutedText`                              | Secondary text: `ForegroundMuted` at `FontSizeBase`. The one method for fading text (§7).                           |
| `GroupBox`                                         | Bevelled fieldset with a `Header` text label (the prototype's `<GroupBox label>`). Padding is `GroupBoxPadding`.    |
| `StatusBadge` *(not built)*                        | Inline pill: `OK`, `Err`, `Warn` or `Neutral`.                                                                      |
| `Etched` *(not built)*                             | 1-px horizontal divider with the classic two-line shadow.                                                           |

Icons stay pixel-crisp (16 px art shown at 32 px is pixel-doubled, not smoothed). Avalonia 11.3 cannot style `RenderOptions.BitmapInterpolationMode`, so every icon
`Image` sets it to `None` itself, and `Icons_DrawWithoutSmoothing` enforces that.

`GroupBox` and the icon styles together compose the "icon header" pattern in §4.

---

## 3. Standard INI row

Every editable INI key renders the same shape, regardless of whether the control is a CheckBox, NumericUpDown, TextBox, ComboBox, or radio. The row is a horizontal `Grid` with
two columns:

```
┌─────────────────────────────┬───────────────────────────────────┐
│ <mono-label>                │ <input-control>                   │
│ <muted hint sub-line>       │                                   │
└─────────────────────────────┴───────────────────────────────────┘
   RowLabelWidth (190 px)        minmax(0, 1fr)
```

Use a `Grid` with `ColumnDefinitions="190,*"` and the right column's `MinWidth="0"`. Avalonia's flex-equivalent quirk is the same as the prototype's: without an explicit
`MinWidth="0"` (or `Width="*"` with `SharedSizeGroup` constraints), long combo content blows out the column.

### 3.1 Label

`TextBlock` with `FontFamily="{StaticResource FontFamilyMono}"` and `FontSize="{StaticResource FontSizeBase}"`. The text is the **literal INI key** (`fullscreen`, `screenwidth`,
`MSStartingCash`, `tooltipDelay`, `Lang`, …) — not a friendly version. The friendly version goes in the hint or, if it doesn't fit, in the tooltip.

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

- **Icon column** is 32 px wide, top-aligned (or centred when the body is one line), pinned to the left. Use an `Image` with `Classes="IconLarge IconBesideBody"`.
- **Body column** fills the remaining width. The first line is a bold headline (14 px for the five main-window states, 11 px elsewhere). Subsequent lines are muted prose, tables,
  or nested controls.
- **Action button(s)** sit at the right when the row is short (Ready / NoInstall — single primary action) or in a footer row below the body when there are multiple actions or the
  body is long (Cannot Play — `Fix` + `Manage Installations`).
- **Headline colour.** Red for Cannot Play (the `AccentRed` token; the prototype draws it maroon), foreground (`Foreground`) elsewhere.
- **Disabled group (reserved).** If a whole group ever needs to read as "not actionable", set `Opacity="0.55"` on the `GroupBox` rather than recolouring its children.
  No view uses this today (Cannot Play leaves its Display and Your System groups un-muted, SDD §9.1). It is a disabled state, not a way to fade text; faded text uses `MutedText`
(§7).

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
- **Default button** is the rightmost in single-action footers (`OK` / `Close`), or the leftmost-of-the-confirming-pair when there's a primary-plus-cancel (`Save` `Cancel`,
  `Restore` `Cancel`, `Delete` `Cancel`). Mark the default with `IsDefault="True"` (the Classic theme draws the outline), as `OptionsDialogView` and `ConfirmationPromptView` do.
- **Gap.** `GapRelated` (6 px) between buttons. `GapUnrelated` (11 px) between the body and the footer; the dialogue's own margin is `MarginDialog` (11 px).
- **Width.** Buttons take the `DialogButton` class (`MinWidth="75"`, §2); the default button gets the same width. A button whose label is longer than that sizes to its
  content.

Footer order from left to right matches the natural reading order: positive action(s) first, destructive action(s) second, dismissive action last. The prototype follows this and
so does §9.

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
- **One way to fade text.** Muted text takes the `MutedText` class (§2) or `{StaticResource ForegroundMuted}`; it is never faded with `Opacity`.
  `Views_DoNotFadeTextWithOpacity` (`SharedSizeStylesTests`) fails on `Opacity="0.6"`. No view sets `Opacity` today; §4's `0.55` is reserved for a disabled group box.
- **Wider than colours.** `NoLiteralColoursTests` also checks `SelectionBrush`, `CaretBrush`, `OpacityMask`, any other `*Brush` attribute, colours written as element
  content and setters with an attribute between `<Setter` and `Property` / `Value`.

---

## 8. Button labels and writing style

- **British English everywhere** (`Behaviour`, `Colour`, `Minimise`, `Initialise`). Don't switch a US spelling that comes from a Windows API surface (`Color`, `Initialize` on
  `IInitializable`).
- **Sentence case** in labels and headings, not Title Case. `Restore previous INI`, not `Restore Previous INI` — *except* when the phrase is itself a proper noun (window titles
  like `Installation Manager`, `Zoo Tycoon Launcher`).
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
- **Keyboard accelerators on dialogues.** The prototype assigns Alt-accelerators (`Btn accel="A"` etc.); Avalonia's `_A` underscore notation does the same. Audit pass during
  Phase 1's dialogue work.
- **What's-this `?` button.** The prototype prototyped a Win95 title-bar `?` button as an alternative help affordance and discarded it. Don't reintroduce without explicit user
  request.
- **Tooltip styling.** Classic.Avalonia's default tooltip is close to the prototype's yellow OS tooltip but not identical. Worth pinning during Phase 1 if it reads wrong
  against the Windows 95 original.
