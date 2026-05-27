# UI conventions

> Companion to [SDD §9](../superpowers/specs/2026-05-26-zoo-tycoon-launcher-design.md#9-user-interface). Where §9 describes **what** each view contains, this doc fixes the recurring **how** — tokens, layout primitives, icon names, theming hooks — so an agent implementing one slice can do it without reverse-engineering the prototype.

---

## 0. How to use this doc

When you sit down to implement a state, a tab, a dialogue, or an INI section:

1. Read SDD §9.x for the screen you're building.
2. Watch the relevant section of `ZooTycoonLauncherHiFiUIPrototype.gif` once.
3. Read this doc end-to-end **the first time**. After that, jump to the named patterns by name (`Section §3 Standard INI row`, `Section §5 Dialogue footer`).
4. Reach for **DynamicResource** keys named here. If you find yourself hard-coding a colour, an icon path, or a spacing number that isn't in this doc, stop and add it here first.

This doc is opinionated, not exhaustive. It captures the things that recur **across multiple views**. One-off affordances belong in §9.x of the SDD.

---

## 1. Token registry

The launcher overlays a small set of named brushes on top of `Classic.Avalonia.Theme`. Two `ResourceDictionary` variants — `LauncherClassicTheme.Light.axaml` and `LauncherClassicTheme.Dark.axaml` — bind each key to a different swatch. Views always reach for the key via `DynamicResource`; `StaticResource` is forbidden for anything in this table because the live theme switch (§7) depends on dynamic resolution.

### 1.1 Neutrals

Bevel topology is preserved between light and dark — `LightHighlight` is always "the bright bevel edge", `DarkShadow` is always "the deepest bevel edge". The names describe the *role*, not the colour.

| Key                       | Role                                             | Light             | Dark              |
|---------------------------|--------------------------------------------------|-------------------|-------------------|
| `Surface`                 | Window / button background; the primary chrome   | `#C0C0C0` silver  | `#2E2E2E`         |
| `SurfaceHighlight`        | Inner-light highlight on raised surfaces         | `#DFDFDF`         | `#424242`         |
| `LightHighlight`          | Outer bright bevel edge                          | `#FFFFFF`         | `#5A5A5A`         |
| `MidShadow`               | Mid bevel shadow                                 | `#808080`         | `#1A1A1A`         |
| `DeepShadow`              | Deeper inner shadow                              | `#404040`         | `#0A0A0A`         |
| `DarkShadow`              | Outermost bevel edge                             | `#000000`         | `#050505`         |
| `Foreground`              | Default text colour                              | `#000000`         | `#E8E8E8`         |
| `ForegroundMuted`         | Muted / hint text                                | `#808080`         | `#9A9A9A`         |
| `ForegroundDim`           | Faint label colour (Info dialogue row labels, INI sub-headers) | `#404040` | `#9A9A9A` |
| `InputBackground`         | TextBox / ListBox / DataGrid background          | `#FFFFFF`         | `#1C1C1C`         |
| `InputBackgroundReadOnly` | Read-only input                                  | `#C0C0C0`         | `#2E2E2E`         |
| `InputBackgroundInvalid`  | Validation-error input background                | `#FFE0E0`         | `#5A1F1F`         |

### 1.2 Accents

These swap colour between themes but never role. Use them through their key, not by name. The light values match the prototype's hardcoded swatches; the dark values match `.theme-dark` in the prototype CSS.

| Key            | Use                                                                          | Light       | Dark        | Replaces prototype `dk-c-*` class |
|----------------|------------------------------------------------------------------------------|-------------|-------------|-----------------------------------|
| `AccentNavy`   | Title-bar gradient start; key informational accent (diff "Snapshot" column)  | `#000080`   | `#3060D0`   | `dk-c-navy`                       |
| `AccentNavyBright` | Title-bar gradient end                                                   | `#1084D0`   | `#3060D0`   | —                                 |
| `AccentTeal`   | Desktop backdrop                                                             | `#008080`   | `#0D3535`   | —                                 |
| `AccentGreen`  | OK / valid badge; "EXE present" / "INI present" headlines                    | `#008000`   | `#5CD070`   | `dk-c-green`                      |
| `AccentRed`    | Strong error text ("No EXE found!", invalid-name message)                    | `#C00000`   | `#FF8080`   | `dk-c-red`                        |
| `AccentMaroon` | "Cannot Play" heading; dirty-state ("● Unsaved changes"); diff "Current" col | `#800000`   | `#D04848`   | `dk-c-maroon`                     |
| `AccentAmber`  | "No INI found" headline; warn-level badges                                   | `#806000`   | `#D4A050`   | `dk-c-amber`                      |
| `AccentYellow` | Tooltip background (Classic.Avalonia default — keep as-is)                   | `#FFFF80`   | `#FFE070`   | —                                 |

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

Optional `TextBlock` immediately below the label, styled with `MutedText`. Carries the *units / range / shape* of the value: `px`, `1–60 ticks/sec`, `0–10,000,000`, `0=Off · 1=Standard · 2=Verbose`, `Inverted in INI` (for `noMenuMusic`), etc. Defaults stay on the input bounds, not in the hint.

### 3.3 Help wiring

Every row participates in the hover + status help affordance (SDD §9.3.2):

1. **Tooltip.** Set `ToolTip.Tip` on the **row's parent panel** (the `Grid` itself, not the label or input). Avalonia's tooltip propagates from the hovered visual up the parent chain, so attaching to the panel makes the whole row trigger the tip. Tip text comes from `IIniHelpRegistry.GetLongDescription(IniKeyId id)` — a registry seeded from `Resources/IniTooltips.axaml` in the Ref build.
2. **Status-bar one-liner.** Attach `PointerEntered` / `PointerExited` on the row (`Grid.PointerEntered=…`). Enter → call `IStatusBarSink.SetHelp(IIniHelpRegistry.GetShortHelp(id))`. Exit → call `IStatusBarSink.ClearHelp()`. The status-bar VM falls back to its dirty-state indicator when `SetHelp` hasn't been called.
3. **Keyboard parity.** The same enter/exit pair fires on `GotFocus` / `LostFocus` for the row's input control, so tab navigation surfaces the same one-liner.

A shared `IniRowBehavior` attached behaviour wires (1)–(3) given an `IniKeyId`. Hand-rolling per row is forbidden.

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
- **Headline colour.** Maroon for Cannot Play, foreground (`Foreground`) elsewhere. Use the `AccentMaroon` token.
- **Muted state.** When the whole group needs to read as "not actionable" (Cannot Play's Display), set `Opacity="0.55"` on the `GroupBox`. Don't recolour individual children — the opacity layer does the right thing in both themes.

### 4.1 Icon vocabulary

The icon set is the prototype's `assets/icons/` directory, lowercase-dash names. ICO files are **not yet committed to the repo** — they will be added as part of the Phase 1 Desktop assembly task. Until then, treat the table below as the contract; views can stub with the `question.ico` placeholder.

| Site                                                  | Icon                               | Notes                                  |
|-------------------------------------------------------|------------------------------------|----------------------------------------|
| Title-bar — launcher                                  | `appmark.svg` (vector, app-owned)  | Abstract green play-cell; not an ICO   |
| Ready · Status group box                              | `check.ico`                        | "Ready to Play" check                  |
| Ready · Display group box                             | `mystify.ico`                      | Reused in Cannot Play (muted)          |
| Looking · Status group box                            | `search.ico`                       |                                        |
| Cannot Play · Status group box                        | `warning.ico`                      |                                        |
| Cannot Play · Display group box                       | `mystify.ico`                      | Same as Ready, group is muted          |
| No Installation · Status group box                    | `critical.ico`                     |                                        |
| Open Picker · Status group box                        | `folder-catalog.ico`               |                                        |
| Installation Manager · title bar                      | `folder-catalog.ico`               |                                        |
| Add / Edit Installation · title bar + body            | `folder-catalog.ico`               | 16 px title, 32 px body                |
| Delete Installation · title bar + body                | `warning.ico`                      |                                        |
| Settings · title bar                                  | `controls.ico`                     |                                        |
| About · title bar                                     | `info.ico`                         | Body keeps the `appmark` logo          |
| Restore Previous INI · title bar                      | `sheets-docs.ico`                  |                                        |
| Installation Info · title bar / body                  | `folder-catalog.ico` / `info.ico`  |                                        |
| Fix Installation · title bar                          | `warning.ico`                      |                                        |
| Fix EXE / Fix INI group boxes — valid state           | `mail-check.ico`                   |                                        |
| Fix EXE / Fix INI group boxes — invalid state         | `mail-cross.ico`                   |                                        |
| INI tab placeholder (disabled)                        | *(no icon)*                        | Muted helper text only                 |
| INI tab — No INI sub-state                            | `warning.ico`                      |                                        |

Phase 1 task: copy `assets/icons/*.ico` from the prototype into `Source/Erdmier.ZooTycoonLauncher.Desktop/Assets/Icons/`, register them as Avalonia assets, and expose them through `IIconSource` (or whatever the composition root chooses to call it). Until then, every view that names an icon by this table is forward-compatible — only the resolver is missing.

---

## 5. Dialogue footer

Every modal dialogue ends with a horizontal button row aligned to the right. Universal rules:

- **No ellipses on button labels.** Ever. `Save`, `Cancel`, `OK`, `Close`, `Delete`, `Restore`, `Fix`, `Browse`, `Locate`, `Create`, `Add`, `Edit`, `Info`. Ellipses on **menu items** that open further dialogues are fine; ellipses on **buttons** that commit a decision are not.
- **Default button** is the rightmost in single-action footers (`OK` / `Close`), or the leftmost-of-the-confirming-pair when there's a primary-plus-cancel (`Save` `Cancel`, `Restore` `Cancel`, `Delete` `Cancel`). Render the default with `Classes="default"` (Avalonia equivalent of the prototype's `--default` outline).
- **Gap.** `SpacingMedium` (6 px) between buttons. `SpacingDialogPad` (14 px) between the body and the footer.
- **Width.** Buttons are `MinWidth="75"`; the `default` variant gets the same width. The "wide" `Launch Game` and `Add Installation` calls-to-action on the main-window states bump to `MinWidth="110"`.

Footer order from left to right matches the natural reading order: positive action(s) first, destructive action(s) second, dismissive action last. The prototype follows this and so does §9.

---

## 6. Hover + status help wiring

A single application service drives the status-bar help affordance:

```csharp
public interface IStatusBarSink
{
    void SetHelp(string oneLiner);
    void ClearHelp();
}
```

The implementation owns the main window's status-bar cell 1 (`Foreground` italic when a one-liner is set, `Foreground` normal + dirty/saved indicator otherwise). Views never poke the cell directly.

`IniRowBehavior` (§3.3) is the only caller in the MVP. Future surfaces can call `SetHelp` too — Phase 2's saves catalogue, for example — but they go through the same service.

The full long-form tooltip text comes from `IIniHelpRegistry`. The two methods that matter are:

```csharp
string GetShortHelp(IniKeyId id);   // status-bar one-liner
string GetLongDescription(IniKeyId id); // OS-level tooltip
```

The registry is seeded from `Resources/IniTooltips.axaml` (Ref launcher) at app startup, then immutable. Strings are British English; the prototype's prose lives in `src/ini-config.jsx` as a `HELP` object — port that map into the registry.

---

## 7. Theming

Three pieces work together:

### 7.1 `IThemeService`

```csharp
public interface IThemeService
{
    LauncherTheme Current { get; }            // System | Light | Dark
    ThemeVariant ActiveVariant { get; }       // Light | Dark — resolved from Current + OS
    event EventHandler<ThemeChangedEventArgs> ThemeChanged;

    Task SetThemeAsync(LauncherTheme theme, CancellationToken ct);
}
```

`SetThemeAsync` writes through `ISettingsService.UpdateAsync(s => s.Theme = theme)`, recomputes `ActiveVariant`, and raises `ThemeChanged`. In `System` mode, the implementation also subscribes to Avalonia's `Application.Current.ActualThemeVariantProperty` and re-raises `ThemeChanged` when the OS preference flips. Single hookup, no scattered subscriptions in views.

### 7.2 Resource dictionaries

```
Source/Erdmier.ZooTycoonLauncher.Desktop/Theme/
├── LauncherClassicTheme.axaml         Master — merges Light / Dark.
├── LauncherClassicTheme.Light.axaml   Token values for §1 keys in light mode.
└── LauncherClassicTheme.Dark.axaml    Token values for §1 keys in dark mode.
```

The master dictionary uses `ResourceDictionary.ThemeDictionaries` (Avalonia 11.x) so the theme variant switch picks up the right child without an app-level reload. `Classic.Avalonia.Theme` is applied first in `App.axaml`; the launcher's master sits on top.

### 7.3 View binding rule

Every brush, font, and spacing referenced from XAML uses `{DynamicResource KeyName}`. `{StaticResource …}` is forbidden for anything in §1 — the live switch breaks otherwise. An architecture-style test (§11.x) greps the `Desktop` XAML tree and fails on any `StaticResource` whose key matches a §1 token.

---

## 8. Button labels and writing style

- **British English everywhere** (`Behaviour`, `Colour`, `Minimise`, `Initialise`). Don't switch a US spelling that comes from a Windows API surface (`Color`, `Initialize` on `IInitializable`).
- **Sentence case** in labels and headings, not Title Case. `Restore previous INI`, not `Restore Previous INI` — *except* when the phrase is itself a proper noun (window titles like `Installation Manager`, `Zoo Tycoon Launcher`).
  - SDD §9 currently writes some titles in Title Case; treat the SDD's title as the canonical window title text. Body copy and inline buttons remain sentence case.
- **No ellipses on buttons** (see §5). Menu items that open a dialogue keep their ellipsis (`Open Installation…`, `Installation Manager…`).
- **No periods at the end of single-sentence muted prose** unless the prose is multi-sentence. (Match the prototype.)
- **Mono runs** for paths, INI keys, INI sections (`zoo.ini`, `zoo.exe`, `[user]`, `screenwidth`), and timestamps. Use the `FontFamilyMono` token.

---

## 9. State-bar message catalogue

Cell 1 of the status bar carries the primary message; cell 2 carries the secondary detail; cell 3 carries the version. Cell 2's text comes from this table unless a hover help one-liner has been published through `IStatusBarSink` (which overrides cell 1, see §6).

| State          | Cell 1                                        | Cell 2                                  | Notes                                |
|----------------|-----------------------------------------------|------------------------------------------|---------------------------------------|
| Looking        | `Discovering installations…`                  | `Reading registry…`                      | Cell 2 updates as boot phases advance |
| Ready          | `Ready — <installation name>`                 | `Display: <w>×<h>` (General) / `INI loaded` (INI Config) |                                       |
| Cannot Play    | `Cannot launch — installation invalid`        | `Fix required` (bold, `AccentMaroon`)    |                                       |
| No Installation| `No installation registered`                  | *(empty)*                                |                                       |
| Open Picker    | `Choose an installation to open`              | *(empty)*                                |                                       |

Cell 1 string in italic + `AccentMaroon` while the INI editor has unsaved changes (override from `IniConfigTabViewModel`). The full set of state strings is fixed and tested.

---

## 10. Open questions for future revisions

Captured here so the SDD doesn't accumulate UI-shaped notes:

- **High-DPI scaling.** Classic.Avalonia targets 96 DPI; the prototype is pixel-fixed. Phase 1 ships at the OS scale; Phase 3 (§13.4) revisits.
- **Keyboard accelerators on dialogues.** The prototype assigns Alt-accelerators (`Btn accel="A"` etc.); Avalonia's `_A` underscore notation does the same. Audit pass during Phase 1's dialogue work.
- **What's-this `?` button.** The prototype prototyped a Win95 title-bar `?` button as an alternative help affordance and discarded it. Don't reintroduce without explicit user request.
- **Tooltip styling.** Classic.Avalonia's default tooltip is close to the prototype's yellow OS tooltip but not identical. Worth pinning during Phase 1 if it reads wrong against dark mode.
