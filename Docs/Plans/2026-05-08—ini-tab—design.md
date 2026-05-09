# INI Tab — Design

**Status:** Draft (open questions resolved 2026-05-08)
**Date:** 2026-05-08
**Companion plan:** [`2026-05-08-ini-tab.md`](./2026-05-08-ini-tab.md)
**SDD reference:** [`SoftwareDesignDocument.md`](../../SoftwareDesignDocument.md) — §5 (Architecture), §6.5 (ViewModels), §7 (Data Design), §8 (Versioning), §9 (Configuration
Settings Reference), §10 (Error Handling), §11 (Non-Functional Requirements).

---

## 1. Goal

Replace the placeholder `<TabItem Header="INI Configurations" />` in `MainWindow.axaml` with a fully-functional editor for every user-editable key in `zoo.ini`, organised into
logically grouped `GroupBox`es on a **single tab**. Edits are buffered in memory; changes hit disk only when the user presses **Save**, and the Save button is enabled only while
there are unsaved changes.

## 2. Scope

### In scope

- Display and edit of every editable key listed in SDD §9.1–§9.8 (the seven category sections), bound to the existing strongly-typed submodels on `ZooIniModel`.
- A dirty-tracking `IniSettingsViewModel` that buffers edits in observable properties.
- A Save button (right-aligned at the bottom of the tab, mirroring the Home tab's Launch Game button) gated on `IsDirty`.
- Save flow: `VersioningService.CreateUndoSnapshotAsync` → apply working values to `ZooIniModel` → `IniParserService.WriteAsync` → reload from disk → reset `IsDirty`.
- Status-bar feedback during save and on success/failure, reusing the existing `MainWindowViewModel.StatusMessage`.
- A new `IniRanges` static class as the single source of truth for numeric ranges, consumed by both `ZooIniDefaults` (parser-side validation) and the XAML `NumericUpDown`
  controls (UI-side clamping).
- Per-key tooltips sourced from a centralised resource dictionary so every editable field describes its purpose on hover.
- A friendly **Screen mode** drop-down in place of a bare "Fullscreen" checkbox, and a friendly **Language** drop-down in place of raw `lang`/`sublang` integer inputs.

### Out of scope (for this milestone)

- **Read-only runtime-state keys** (SDD §9.9: `lastfile`, `showUserEntityWarning`, `lastWindow*`, `started*Tutorial`, `progresscalls`, `defaultEditCharLimit`,
  `completedExhibitAttenuation`). The parser already preserves them on round-trip; the SDD designates the **status area** as their display surface, not the INI tab.
- **Unmanaged sections** (SDD §9.10: `[mgr]`, `[lib]`, `[resource]`, `[scenario]`). Preserved verbatim via `ZooIniModel.UnknownKeys`; not exposed in the GUI.
- **Resolution combo + screen-mode picker driven by `IDisplayInfoService`** (SDD §9.1 note about a combined resolution picker). The screen-mode drop-down is included in this
  milestone, but `screenwidth`/`screenheight` remain raw integer fields rather than a list of detected resolutions. The full picker depends on `IDisplayInfoService` (separate plan:
  `2026-05-08-overview-dynamic-data.md`) and is deferred.
- **Install-driven language enumeration** (SDD §9.7 note: "populated from the set of language strings bundled with the game installation"). The language drop-down ships with a
  hard-coded canonical list in this milestone; sourcing options from the install's `lang*.dll` resources is a follow-up.
- **Undo Last Save / Full Reset commands** (SDD §8). `EnsureOriginalBackupAsync` already runs on startup; the snapshot-before-write path is wired here, but the `Undo`/`Reset` UI
  commands are deferred to a separate plan.
- **Tests.** No test project exists yet — same posture as the 2026-04-29 milestone.

## 3. Architecture

### 3.1 Two-layer state

The view model holds **two** copies of the model conceptually:

| Layer      | Purpose                                                                                                             | Lifetime                                                                                                                                  |
|------------|---------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------|
| `Original` | The `ZooIniModel` last loaded/saved from disk. The disk-of-record snapshot.                                         | Replaced wholesale on every successful read or write.                                                                                     |
| `Working`  | The live, edited values, exposed as `[ObservableProperty]`s on `IniSettingsViewModel` and bound directly to the UI. | Mutated by the user; reset to `Original` after a successful save (or when a new `ZooIniModel` arrives from a `LocateManually` operation). |

`IsDirty` is a single observable bool driven by an "any working-property changed since last sync" signal. The simplest correct implementation: every `partial void OnXxxChanged`
setter for the observable properties calls `MarkDirty()`, which sets `IsDirty = true`. `IsDirty` is reset to `false` only by `ApplyOriginal()` (sync working from Original) and
`OnSaved()` (after a successful write).

We deliberately **do not** compute `IsDirty` by deep-comparing Working vs Original. Two reasons:

1. With ~46 properties spanning seven submodels, a deep compare is repeated work on every property change.
2. The simple "set on any change, reset on sync/save" semantics are easier to reason about and match how `LocateManuallyAsync` already works elsewhere.

The trade-off: editing a value to its prior value still flips `IsDirty` to `true`. This is acceptable — the user sees a Save button that re-saves an effectively unchanged file,
which the parser handles gracefully (the `MergeModelIntoDocument` path emits the original `RawText` when the value matches the prior). If we want true value-equality dirty tracking
later, we add it as a polish task.

### 3.2 ViewModel hierarchy

```
MainWindowViewModel               (existing — orchestrator)
 ├─ Model: ZooIniModel?           (existing — disk-of-record)
 ├─ IniPath: string?              (existing)
 ├─ IniSettingsViewModel Ini      (NEW — owns the edit/save loop for the tab)
 │   ├─ ObservableProperty per editable key (46 properties)
 │   ├─ Derived projections: PlayMenuMusic, ScreenModeIndex, SelectedLanguage
 │   ├─ LanguageOptions: IReadOnlyList<LanguageOption>
 │   ├─ IsDirty: bool
 │   ├─ SaveCommand: IRelayCommand (CanExecute = IsDirty)
 │   └─ ApplyModel(ZooIniModel) / OnSaved()
 └─ HasIni: bool                  (existing — gates IsEnabled on the tab content)
```

`IniSettingsViewModel` is constructed via DI and exposed as a property on `MainWindowViewModel.Ini`. The XAML for the INI tab binds to `Ini.<Property>` so the property surface
stays focused.

### 3.3 Save sequence

Implements SDD §8.2. The full sequence on `IniSettingsViewModel.SaveCommand`:

1. **Guard.** No-op if `IsDirty == false` (defensive — the button is `IsEnabled`-gated but commands can fire from other paths).
2. **Resolve dependencies.** Pull `iniPath` from the parent `MainWindowViewModel`. If null (game not located), set `StatusMessage = "Cannot save: zoo.ini path is unknown."` and
   return. The button should already be disabled here via `HasIni`, but defensive.
3. **Apply Working → Original.** Mutate the live `ZooIniModel` (the same instance the parser populated; we keep a reference) so the parser's `WriteAsync` round-trip path uses the
   cached `RawDocument` and preserves comments/ordering. The "apply" is a sequence of property assignments: `original.User.Fullscreen = working.Fullscreen; …`. Code-generated
   mechanical mapping per `ZooIniDefaults.KnownKeys` would be elegant but is overkill — explicit assignments keep the source readable.
4. **Snapshot.** `await _versioning.CreateUndoSnapshotAsync(iniPath)` to write `zoo.ini.undo`.
5. **Write.** `await _parser.WriteAsync(iniPath, original)`.
6. **Verify.** `await _parser.ReadAsync(iniPath)` and assign the freshly-loaded model back through the same `ApplyModel` path so any normalization applied by the parser is
   reflected in the UI.
7. **Reset.** `IsDirty = false` and `StatusMessage = "Saved."`.
8. **Failure.** Any exception in steps 4–6 is caught, surfaced as `StatusMessage = "Save failed: <reason>."`, and **`IsDirty` remains `true`** so the user can retry. Per SDD §10,
   the undo snapshot is "discarded" on write failure — practically, the snapshot from step 4 may already exist on disk. Acceptable: it represents the previous-good state, which is
   exactly what undo should restore to. **Status-bar messaging only** — modal dialogues per SDD §10 are deferred to a follow-up (see §3.11).

### 3.4 Section → tab grouping

Single tab; one `GroupBox` per SDD §9 category, in this order:

| GroupBox                | SDD § | INI sections covered | Editable keys                                                                                                                                                                                                            |
|-------------------------|-------|----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Display and Performance | 9.1   | `[user]`             | `fullscreen`, `screenwidth`, `screenheight`, `UpdateRate`, `DrawRate`                                                                                                                                                    |
| Graphics Quality        | 9.2   | `[advanced]`         | `level`, `loadHalfAnims`, `drag`, `click`, `normal`                                                                                                                                                                      |
| Audio                   | 9.3   | `[UI]`, `[advanced]` | `noMenuMusic`, `menuMusic`, `menuMusicAttenuation`, `userAttenuation`, `playMovie`, `movievolume1`, `playSecondMovie`, `movievolume2`, `use8BitSound`                                                                    |
| Gameplay                | 9.4   | `[UI]`, `[ai]`       | `MSStartingCash`, `MSCashIncrement`, `MSMinCash`, `MSMaxCash`, `maxGuests`                                                                                                                                               |
| Interface               | 9.5   | `[UI]`               | `useAlternateCursors`, `tooltipDelay`, `tooltipDuration`, `MessageDisplay`, `mouseScrollThreshold`, `mouseScrollDelay`, `mouseScrollX`, `mouseScrollY`, `keyScrollX`, `keyScrollY`, `minimumMessageInterval`, `helpType` |
| Map                     | 9.6   | `[Map]`              | `mapX`, `mapY`                                                                                                                                                                                                           |
| Language                | 9.7   | `[language]`         | `lang` + `sublang` (presented as a single "Language" drop-down — see §3.6)                                                                                                                                               |
| Debug                   | 9.8   | `[debug]`            | `drawfps`, `drawfpsx`, `drawfpsy`, `logCutoff`, `sendLogfile`, `sendDebugger`                                                                                                                                            |

Total: **46 editable keys** across **8 group boxes**.

### 3.5 Display & Performance layout — screen mode + adjacent width/height

Per the resolved decision on Q1, this group's layout differs from the standard "label / control" grid pattern used elsewhere:

```
Screen mode:    [Fullscreen ▾]
Screen width:   [800]    Screen height: [600]
Update rate:    [15]
Draw rate:      [60]
```

- "Screen mode" is a `ComboBox` with two items: "Fullscreen" (index 0) and "Windowed" (index 1). It binds to a derived `ScreenModeIndex` property on the VM that maps `Fullscreen` (
  bool) to `0`/`1`.
- Width and height share row 1, with both labels and controls sized via a 4-column `Grid` (`Auto,*,Auto,*`).
- Update rate and Draw rate span columns 1–3 below them.

### 3.6 Language drop-down

Per the resolved decision on Q2, `lang`/`sublang` are exposed as a **single** drop-down rather than two integer inputs. The view model holds:

```csharp
public sealed class LanguageOption
{
    public LanguageOption(int lang, int subLang, string displayName)
    {
        Lang = lang; SubLang = subLang; DisplayName = displayName;
    }
    public int Lang { get; }
    public int SubLang { get; }
    public string DisplayName { get; }
}
```

`IniSettingsViewModel` exposes a static `LanguageOptions` list (10 common Windows LANGID/SUBLANGID combinations) and a `SelectedLanguage` property that does a value-equality lookup
against `(Lang, SubLang)`:

```csharp
public LanguageOption? SelectedLanguage
{
    get => LanguageOptions.FirstOrDefault(o => o.Lang == Lang && o.SubLang == SubLang);
    set { if (value is not null) { Lang = value.Lang; SubLang = value.SubLang; } }
}
```

The hard-coded options for v1:

| Display name             | LANGID | SUBLANGID |
|--------------------------|--------|-----------|
| English (United States)  | 9      | 1         |
| English (United Kingdom) | 9      | 2         |
| German (Germany)         | 7      | 1         |
| French (France)          | 12     | 1         |
| Spanish (Modern)         | 10     | 3         |
| Italian (Italy)          | 16     | 1         |
| Japanese                 | 17     | 1         |
| Portuguese (Brazil)      | 22     | 1         |
| Dutch (Netherlands)      | 19     | 1         |
| Swedish (Sweden)         | 29     | 1         |

If the loaded `lang`/`sublang` do not match any option, `SelectedLanguage` returns null and the `ComboBox` shows nothing selected. The user can pick a known option to overwrite, or
leave it untouched and the parser preserves the original integer values verbatim. This is acceptable for v1; install-driven enumeration is a follow-up (§2 "Out of scope").

### 3.7 Control type per key

Selection rules — kept boring on purpose so the Classic theme's stock primitives do the heavy lifting:

| Key shape                                       | Control                                                                                                                   | Notes                                                                        |
|-------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------|
| `bool`                                          | `<CheckBox>`                                                                                                              | Two-way bound to the observable property.                                    |
| `bool` with friendly inverted phrasing          | `<CheckBox>` bound to a derived `int`/`bool` getter/setter                                                                | Used for `noMenuMusic` → `PlayMenuMusic`. See §3.10.                         |
| `bool` presented as a binary choice             | `<ComboBox>` with two items, bound to a derived `int`                                                                     | Used for `fullscreen` → `ScreenModeIndex` per the resolved decision on Q1.   |
| `int` with `min`/`max` from `IniRanges`         | `<NumericUpDown Minimum="{x:Static models:IniRanges.XxxMin}" Maximum="{x:Static models:IniRanges.XxxMax}" Increment="…">` | The control clamps on commit. See §3.8 for the single-source-of-truth setup. |
| `int` enum (small, dense values 0..N)           | `<ComboBox SelectedIndex="{Binding Xxx}">` populated with hand-rolled `<ComboBoxItem>` labels                             | Used for `level` (5 presets) and `helpType` (3 modes).                       |
| `int` pair forming a single user-facing concept | `<ComboBox ItemsSource="…" SelectedItem="…">` over a typed options list                                                   | Used for `lang`+`sublang` → `SelectedLanguage`. See §3.6.                    |
| `string`                                        | `<TextBox>`                                                                                                               | Used only for `menuMusic`.                                                   |

### 3.8 `IniRanges` — single source of truth for numeric bounds

Per the resolved decision on Q3, every numeric range is held in a new `public static class IniRanges` in `Models/IniRanges.cs`. Two consumers reference these constants:

1. **`ZooIniDefaults`** — `IniKeySpec.Int(…, min: IniRanges.XxxMin, max: IniRanges.XxxMax)` for parser-side validation.
2. **XAML `NumericUpDown`** — `Minimum="{x:Static models:IniRanges.XxxMin}" Maximum="{x:Static models:IniRanges.XxxMax}"` for UI-side clamping.

`IniRanges` is `public` because compiled XAML in Avalonia 11 needs reflective access to types referenced via `{x:Static}` from XAML. Constants follow the naming convention
`<PropertyName><Min|Max>` using the C# property name (PascalCase), not the INI key name. Examples: `ScreenWidthMin`, `MaxGuestsMax`, `MovieVolume1Min`.

For keys where the parser deliberately doesn't validate (e.g. `screenwidth` is "detected from display" per SDD §9.1) but the XAML still wants a defensive cap to prevent absurd
input, the range constant exists and is used **only** by XAML; `ZooIniDefaults` does not pass `min:`/`max:` for those. The constant is still the single source of truth for the XAML
side.

The implication for adding a new INI key is updated in CLAUDE.md from:

> Adding a new INI key means adding a row [in `ZooIniDefaults`] and a property on the matching submodel — nothing else.

To:

> Adding a new INI key means adding a row in `ZooIniDefaults`, a property on the matching submodel, and (if the key has numeric ranges or is otherwise numerically validated) a pair
> of `Min`/`Max` constants in `IniRanges`. The `ZooIniDefaults` row references those constants rather than hard-coding numbers.

### 3.9 Per-key tooltips

Per the resolved decision on Q5, every editable control gets a `ToolTip.Tip` sourced from a centralised resource dictionary at `Source/Launcher/Resources/IniTooltips.axaml`. The
dictionary contains one `<sys:String x:Key="TT.<PropertyName>">` entry per editable key (~45 entries — see plan Task 3 for the full list).

The dictionary is merged into the application's resources in `App.axaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://Erdmier.ZooTycoonLauncher.Launcher/Resources/IniTooltips.axaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

Controls reference tooltips with `ToolTip.Tip="{StaticResource TT.<PropertyName>}"`. Centralising the strings here:

- Keeps tooltip prose out of the GroupBox XAML so the layout stays scannable.
- Makes editing tooltip wording a single-file concern.
- Trivially extends to launcher-UI localisation later (the launcher itself is currently English-only, but the SDD's language drop-down is for the *game's* language, not the
  launcher's UI).

Tooltip strings should be **one to two sentences**: state what the setting does and any non-obvious gotcha (inverted logic, units, performance trade-off). They should not duplicate
the visible label, and they should not paraphrase the SDD's full description verbatim — concise summaries that surface the key point.

### 3.10 Inverted-logic `noMenuMusic`

Per the resolved decision on Q6, `[UI] noMenuMusic` (where `1` = music *disabled*) is presented to the user as a positively-phrased "Play menu music" check via a derived property
on the VM:

```csharp
public bool PlayMenuMusic
{
    get => !NoMenuMusic;
    set => NoMenuMusic = !value;
}
```

`OnNoMenuMusicChanged` raises `INotifyPropertyChanged` for both itself and `PlayMenuMusic` so a programmatic update via either path keeps the UI consistent.

### 3.11 Error-handling UX

Per the resolved decision on Q4, all user-facing error reporting on the INI tab flows through `IniSettingsViewModel.StatusMessage` (status-bar-only). SDD §10's "modal error
dialogue" UX is deferred — once `Classic.CommonControls.MessageBox` is wired into the project, save failures and undo confirmations will be promoted to dialogues; until then, the
status bar is the sole error surface. This matches the rest of the launcher's existing pattern (see `MainWindowViewModel`'s status bar usage).

### 3.12 IsEnabled gating

| Element                          | Bound to                     | Reason                                                                                           |
|----------------------------------|------------------------------|--------------------------------------------------------------------------------------------------|
| Tab content (group boxes + Save) | `MainWindowViewModel.HasIni` | Already in place on the `<TabControl>`; preserves the existing "no INI = greyed tabs" semantics. |
| Save button                      | `Ini.IsDirty`                | Per the user's requirement.                                                                      |

The `<TabControl>` itself stays enabled (so users can read tab headers), but its `IsEnabled` propagates `false` into all child content. We already have
`IsEnabled="{Binding HasIni}"` on the `<TabControl>` from the prior milestone.

## 4. Data flow

```
Disk (zoo.ini)
  │
  │ IIniParserService.ReadAsync
  ▼
ZooIniModel (Original, owned by MainWindowViewModel.Model)
  │
  │ IniSettingsViewModel.ApplyModel(model)
  ▼
[ObservableProperty] fields on IniSettingsViewModel  (Working)
  │
  │ user edits via XAML two-way bindings
  ▼
IsDirty = true  →  Save button enabled
  │
  │ IniSettingsViewModel.SaveAsync
  ▼
Apply Working → Original (mutate ZooIniModel in place)
  │
  ├─ IVersioningService.CreateUndoSnapshotAsync(iniPath)
  │   └─ writes zoo.ini.undo
  │
  ├─ IIniParserService.WriteAsync(iniPath, Original)
  │   └─ atomic temp-file + rename, preserves RawDocument layout
  │
  └─ IIniParserService.ReadAsync(iniPath)  (verification re-read)
      └─ replaces Original; ApplyModel re-syncs Working

IsDirty = false; StatusMessage = "Saved."
```

## 5. Why `IniSettingsViewModel` is a single VM, not eight

The SDD's §5.2 example structure suggested per-tab VMs (`DisplaySettingsViewModel`, `GraphicsSettingsViewModel`, …). The user's instruction here — **"all INI values should be found
in the single INI tab, but logically grouped using group boxes"** — supersedes that, and a single VM matches a single tab. Pros: one `IsDirty`, one `SaveCommand`, no cross-VM event
plumbing. Cons: the file is large (~280 lines of mechanical observable-property declarations + helper getters/setters). Acceptable: the file is mechanically generated from
`ZooIniDefaults` and unlikely to need frequent edits.

If the property surface ever needs to be split (e.g. separate tabs for Display vs Audio vs Debug because the Classic-themed window grows too tall), the natural split is along the
GroupBox lines from §3.4, and the dirty/save plumbing can lift to `MainWindowViewModel` then.

The "~280 lines" estimate assumes ~5 lines per property (declaration + partial OnChanged + sync + apply) plus shared scaffolding and the language/screen-mode/inverted-music helper
sections.

## 6. Atomicity & error handling (SDD §10, §11)

- `IniParserService.WriteAsync` already implements the SDD §11 "Reliability" requirement: write-to-temp + `File.Move(overwrite: true)`. No changes needed.
- `IVersioningService.CreateUndoSnapshotAsync` is invoked **before** the write so `zoo.ini.undo` represents the last-known-good state.
- All exceptions in the save path are caught at the VM and surfaced via `StatusMessage` — see §3.11.

## 7. Performance (SDD §11)

- 46 observable properties + 8 group boxes is a trivial UI. No virtualization needed.
- `WriteAsync` is the only I/O on the save path and runs on a background thread via `await`. Snapshot copy is a single `File.Copy` — sub-millisecond.
- `ApplyModel` runs on the UI thread and triggers ~46 INPC events; this is fine for a one-shot operation.

## 8. Designer support

`IniSettingsViewModel` follows the existing pattern — a parameterless ctor that constructs a no-op model so the previewer renders. The parameterless ctor synthesises a fresh
`ZooIniModel` (defaults) and calls `ApplyModel` so the previewer shows defaulted values rather than blanks.

## 9. Resolved decisions

The six open questions raised in the original draft of this design were resolved on 2026-05-08:

| # | Question                                                                                                  | Decision                                                                                                                                                                                                                                                           |
|---|-----------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | Replace `screenwidth`/`screenheight`/`fullscreen` with the SDD §9.1 combined picker now?                  | **Partial.** Width and height stay separate but render adjacent on a single row. `fullscreen` is promoted to a "Screen mode" drop-down with "Fullscreen" / "Windowed" options. The full `IDisplayInfoService`-driven resolution list remains deferred. (See §3.5.) |
| 2 | Replace `lang`/`sublang` with a friendly drop-down now?                                                   | **Yes — implement now** with a hard-coded canonical list of 10 common Windows LANGID/SUBLANGID combinations. Install-driven enumeration is deferred. (See §3.6.)                                                                                                   |
| 3 | Make `ZooIniDefaults` ranges flow into XAML `NumericUpDown.Minimum`/`Maximum` for single-source-of-truth? | **Yes.** Introduce `IniRanges` (public static constants). Both `ZooIniDefaults` and XAML reference it. (See §3.8.)                                                                                                                                                 |
| 4 | Modal error dialogue UX per SDD §10 now or later?                                                         | **Later.** Status-bar-only for this milestone. (See §3.11.)                                                                                                                                                                                                        |
| 5 | Per-key tooltips now or later?                                                                            | **Now.** Centralised resource dictionary at `Resources/IniTooltips.axaml`. (See §3.9.)                                                                                                                                                                             |
| 6 | Inverted-logic display for `noMenuMusic`?                                                                 | **Yes.** Derived `PlayMenuMusic` getter/setter on the VM. (See §3.10.)                                                                                                                                                                                             |

No questions remain open as of the date of this revision.

## 10. Plan alignment

The companion plan ([`2026-05-08-ini-tab.md`](./2026-05-08-ini-tab.md)) decomposes this design into 14 tasks. Each task ends with a build verification via
`mcp__rider__build_solution` (canonical) or `dotnet build` (fallback), per the project's existing convention. No tests in this milestone — same posture as
`2026-04-29-ini-parser-and-startup.md`.