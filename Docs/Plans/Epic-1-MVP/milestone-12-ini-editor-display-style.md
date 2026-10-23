# INI Editor Display Style Implementation Plan

**Date:** 2026-10-22

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let the player choose, in Options, whether the INI Config editor shows `zoo.ini` in a Friendly style (pages by purpose, plain labels, meaning-based
values, volume sliders; the default) or an INI style (the file's sections and keys, literally), with the Restore Previous INI dialogue following the choice.

**Architecture:** A new `IniEditorStyle` setting flows Domain → `Launcher.db` → `LauncherOptions` → Options dialogue → a Desktop `IIniEditorStyleState`
singleton. The Desktop catalogue keeps each key's control kind and choice options once; two layouts (INI, Friendly) arrange those keys into sections or pages
with per-style labels, hints, help and value conversions. Field view models keep raw INI values as the source of truth; conversions only change what a
control shows and accepts. A style switch rebuilds the editor's sections and re-applies pending edits by key.

**Tech Stack:** .NET 10, Avalonia 11.3 + Classic.Avalonia, CommunityToolkit.Mvvm, Mediator, ErrorOr, Ardalis.SmartEnum, EF Core + SQLite, xUnit 2.9 + Shouldly +
NSubstitute, Avalonia.Headless.XUnit.

**Spec:** [`Docs/Specs/Epic-1-MVP/milestone-12-ini-editor-display-style-design.md`](../../Specs/Epic-1-MVP/milestone-12-ini-editor-display-style-design.md) — read
it before any task; decisions 1–12 and calls C1–C16 are binding; the §6.1 page table, §6.3 conversions, §6.4 slider rules and the §4 option copy are used verbatim.

## Global Constraints

- Branch `feat/milestone-12-ini-editor-display-style`. **Commit every task automatically**; do not wait for review. Unsigned
  (`git -c commit.gpgsign=false commit`), message ending with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; stage only the task's files.
- Commit format `type(emoji): subject` (`feat`→✨, `fix`→🐛, `refactor`→♻️, `style`→🎨, `test`→✅, `docs`→📝), British English.
- **PowerShell** for `dotnet` and `git`; commit messages through a Git Bash heredoc. Foreground commands only. On a file-lock error, stop and report.
- No filesystem-wide searches. **Never write, rename or delete anything under `SampleInstallations/`.**
- `TreatWarningsAsErrors` and `Nullable` on. One type per file; folder mirrors namespace; file-scoped namespaces; XML docs on public members; spaced attributes;
  named arguments on literals in tests; every XAML file keeps `x:DataType`; `MainWindow.axaml` ≤ 100 lines; GlobalUsings over local usings.
- Views use tokens through `{StaticResource …}` / `{tokens:Space …}`; no new literal colours, fonts, sizes or spacing (the architecture tests enforce it).
- **Reading and writing never change** (spec §3): no task touches the INI parser, the snapshot store, `IniSnapshotComparer` or what a save, Reset, Restore
  defaults or Restore writes.
- Every public `*ViewModel` has a sibling `*View.axaml` and vice versa; new views join the headless view-load suite automatically.
- Markdown wrapped at 180 characters (PowerShell `.Length`); the ledger `Docs/Ledgers/Epic-1-MVP/milestone-12-ledger.md` is the controller's.

## Review Focus

1. **A style switch with unsaved edits** — every edited key keeps its edited raw value and the editor stays dirty; nothing is written. Pinned in Task 4.
2. **A volume value off the 1 % grid** (e.g. `menuMusicAttenuation=1234`) — opening and switching styles leaves the file value unchanged; only moving the slider
   rewrites it. Pinned in Task 3.
3. **A key shown twice or not at all** in either layout — fails a test, not the player. Pinned in Task 2.
4. **The Restore dialogue's change count** — identical in both styles for the same snapshot, the language pair included. Pinned in Task 6.
5. **Saving in the Friendly style** — `zoo.ini` holds raw values (`noMenuMusic=0` when *Enable menu music* is ticked; attenuation, not volume). Pinned in Task 4
   and the Task 8 drive.

---

## Task 0: Before screenshots (controller)

- [ ] **Step 1:** Create the branch and ledger; capture the INI Config tab (every section) and the Restore and Options dialogues from the current build.

---

## Task 1: The `IniEditorStyle` setting end to end (Domain, Application, Infrastructure)

**Files:** create `Domain/Settings/IniEditorStyle.cs`; modify `Domain/Settings/LauncherSettings.cs`, `Application/Settings/Common/LauncherOptions.cs`,
`Application/Settings/Save/SaveLauncherOptionsCommand.cs` and its handler, `Application/Settings/Get/GetLauncherOptionsHandler.cs`,
`Infrastructure/Persistence/Launcher/Configurations/LauncherSettingsConfiguration.cs`; add a migration `AddIniEditorStyle` under
`Persistence/Launcher/Migrations`; tests in the Domain, Application and Infrastructure test projects.

**Interfaces — Produces:**
- `public sealed class IniEditorStyle : SmartEnum<IniEditorStyle>` with `Friendly` ("Friendly", 1) and `Ini` ("Ini", 2), and
  `public string DisplayName { get; }` = *Friendly style* / *INI style*.
- `LauncherSettings.IniEditorStyle` (`IniEditorStyle`, default `IniEditorStyle.Friendly`).
- `LauncherOptions(LauncherStartupPreference StartupPreference, bool CloseAfterGameLaunch, IniEditorStyle EditorStyle)`.
- `SaveLauncherOptionsCommand(LauncherStartupPreference StartupPreference, bool CloseAfterGameLaunch, IniEditorStyle EditorStyle)`.

- [ ] **Step 1: Failing tests:** `IniEditorStyle_HasFriendlyAndIni` (names, ids, display names); `LauncherSettings_DefaultsToFriendly`;
  `Save_StoresTheEditorStyle` and `Get_ReturnsTheEditorStyle` (Application); `Migration_AddsIniEditorStyleWithFriendlyDefault` (an existing row reads
  `Friendly`, `pragma_table_info` default `'Friendly'`) and `Migration_Down_RemovesIniEditorStyle` (Infrastructure).
- [ ] **Step 2: Run** → fail.
- [ ] **Step 3: Implement.** Column stored as the SmartEnum name (as `LauncherStartupPreference`), not null, default `'Friendly'`; generate the migration with
  `dotnet ef migrations add AddIniEditorStyle --project Source/Erdmier.ZooTycoonLauncher.Infrastructure --context LauncherDbContext --output-dir Persistence/Launcher/Migrations`.
  Update every existing `LauncherOptions` / `SaveLauncherOptionsCommand` construction (Desktop's Options dialogue passes `IniEditorStyle.Friendly` until Task 5).
- [ ] **Step 4: Run** Domain, Application, Infrastructure, Architecture + full build (0 warnings) → pass.
- [ ] **Step 5: Commit** — `feat(✨): store the INI editor display style in the launcher settings`

---

## Task 2: Two layouts over one catalogue, with value conversions (Desktop models)

**Files:** modify `Desktop/Models/IniConfig/IniFieldDescriptor.cs`, `IniSectionDescriptor.cs`, `IniFieldGroupDescriptor.cs`, `IniControlKind.cs`,
`IniEditorCatalogue.cs`; create `IniValueConversion.cs`, `IniEditorFriendlyCatalogue.cs`, `IniEditorLayouts.cs`, `IniFieldLocation.cs` (all under
`Models/IniConfig/`); tests in `Tests/…Desktop.Tests.Unit/IniConfig/`.

**Interfaces — Produces:**
- `IniControlKind.Slider` (new member).
- `public sealed class IniValueConversion : SmartEnum<IniValueConversion>`: `None`, `Invert` (bool), `VolumeFromAttenuation` (`volume = 10000 − raw`),
  `VolumeFromMovieVolume` (`volume = raw + 10000`), each with `int ToDisplay(int raw)` / `int ToRaw(int display)` (Invert works on `0`/`1`).
- `IniFieldDescriptor` gains `IniValueConversion Conversion { get; init; } = IniValueConversion.None` (replaces `Inverted`, which becomes
  `Conversion == IniValueConversion.Invert`).
- `IniSectionDescriptor(string Section, string Title, string Descriptor, string? Footnote, IReadOnlyList<IniFieldGroupDescriptor> Groups)` — INI: `Title` is
  the bracketed section name; Friendly: the page title, `Section` a stable page id (`display`, `graphics`, …).
- `IniFieldGroupDescriptor(string? SubHeader, IReadOnlyList<IniFieldDescriptor> Fields)` gains `string? Footnote { get; init; }`.
- `IniEditorCatalogue.Sections` stays the **INI layout**; its shared choice-option lists (fullscreen, quality preset, help level) and `Languages` become public
  static members both layouts use.
- `IniEditorFriendlyCatalogue.Sections` — the **Friendly layout**, spec §6.1 verbatim (pages, descriptions, groups, labels, order), §6.2 rows, §6.3
  conversions, the four volume rows as `IniControlKind.Slider`, the `[ai]` / `[Map]` footnotes on their groups, the language page as the picker alone.
- `IniEditorLayouts.Sections(IniEditorStyle style)` and `IniEditorLayouts.TryLocate(IniEditorStyle style, IniKeyId id, out IniFieldLocation? location)` with
  `public sealed record IniFieldLocation(IniSectionDescriptor Section, IniFieldGroupDescriptor Group, IniFieldDescriptor Field, int Order)`.

- [ ] **Step 1: Failing tests:** `EachLayout_ShowsEveryUserSettingKeyOnce` (theory over both styles; compare against the user-setting keys of
  `ZooIniDefaults`, minus `[scenario]`; the language picker counts for `lang` and `sublang`); `FriendlyLayout_MatchesTheSpecPageOrder` (the eight titles in
  order); `Conversion_RoundTrips` (theory: every conversion, every raw value in its key's range: 0–10000, −10000–0, 0/1); `IniLayout_NoMenuMusicIsLiteral`
  (no conversion, caption *No menu music*, hint *1 = no menu music*); `FriendlyLayout_VolumeRowsAreSliders`.
- [ ] **Step 2: Run** → fail.
- [ ] **Step 3: Implement.** Draft the Friendly hints and help from today's help (spec §12), each describing the value as the Friendly control shows it; volume
  hints *Silent – Full*. Keep the INI layout's text as today except `noMenuMusic` (spec §5).
- [ ] **Step 4: Run** Desktop + Architecture + full build → pass.
- [ ] **Step 5: Commit** — `feat(✨): add the Friendly and INI editor layouts over one catalogue`

---

## Task 3: The volume slider field

**Files:** create `Desktop/ViewModels/IniConfig/Fields/IniSliderFieldViewModel.cs` and `Desktop/Views/IniConfig/Fields/IniSliderFieldView.axaml(.cs)`; modify
`IniFieldFactory.cs` (Slider kind), `IniToggleFieldViewModel.cs` (read `Conversion`); `Views/IniConfig/IniFieldFocusRelease.cs` or the slider view's
code-behind for focus release; tests under `Tests/…Desktop.Tests.Unit/IniConfig/Fields/`.

**Interfaces — Produces:** `public sealed partial class IniSliderFieldViewModel : IniKeyedFieldViewModel` with `[ObservableProperty] int Percent` (0–100) and
`string PercentText` (*75 %*).

- [ ] **Step 1: Failing tests:** `Load_ShowsThePercentOfTheConvertedValue` (`menuMusicAttenuation=2500` → 75 %; `movievolume1=-10000` → 0 %);
  `MovingTheSlider_WritesRawInStepsOf100` (75 % → attenuation `2500`; movie 30 % → `-7000`); `OffStepValue_IsKeptUntilMoved` (`1234` loads at 88 %,
  `TryGetEdit` false, `IsDirty` false; after `Percent = 87` the edit is `1300`); `Reset_RestoresTheRawBaseline`.
- [ ] **Step 2: Run** → fail.
- [ ] **Step 3: Implement.** Raw stays the source of truth (spec C5, C16). The view: Classic `Slider` 0–100, `SmallChange` 1, `LargeChange` 10,
  `TickFrequency` 10 with ticks shown, the percentage readout beside it; a pointer drag that ends releases focus through `IniFieldFocusRelease.AfterCommit`, a
  Tab-focused slider keeps it (spec §6.4).
- [ ] **Step 4: Run** Desktop + Architecture + full build → pass.
- [ ] **Step 5: Commit** — `feat(✨): show the Friendly volumes as sliders`

---

## Task 4: The editor renders either style and switches live

**Files:** create `Desktop/Composition/IIniEditorStyleState.cs`, `IniEditorStyleState.cs` (registered as a singleton in
`DesktopServiceCollectionExtensions`); modify `IniFieldFactory.cs`, `IniEditorViewModel.cs`, `IniSectionViewModel.cs`, `IniFieldGroup.cs`,
`ViewModels/Tabs/IniConfigTabViewModel.cs`, `Views/IniConfig/IniSectionView.axaml`, `IniEditorView.axaml`, `Fields/IniLanguageFieldView.axaml` and the
language field VM; the boot path that loads options; tests.

**Interfaces — Produces:**
- `public interface IIniEditorStyleState { IniEditorStyle Style { get; set; } event EventHandler? StyleChanged; }` — set from `GetLauncherOptionsQuery` at boot,
  by Options after a save (Task 5); raises `StyleChanged` only when the value changes.
- `IniFieldFactory.CreateSections(IniEditorStyle style)`.
- `IniEditorViewModel` takes the style at construction and gains `public void ApplyStyle(IniEditorStyle style)`: capture each keyed field's baseline and pending
  edit by `IniKeyId`, rebuild `Sections` from the layout, `Load` baselines, re-apply edits, select the first section (spec C7). `Sections` becomes observable.
- `IniSectionViewModel.Header` → `Title`; `IsIniStyle` drives the "section of zoo.ini" suffix and the monospace labels; `IniFieldGroup` exposes `Footnote` and
  whether it renders as a sub-header (INI) or a group box (Friendly).
- The language field VM exposes `CodesText` (*Windows language codes: LANGID 9 · SUBLANGID 1*, current values) and `ShowCodeRows` (INI only).

- [ ] **Step 1: Failing tests:** `ApplyStyle_KeepsPendingEditsByKey` (edit `screenwidth` and `noMenuMusic` in INI style, switch to Friendly: same raw edits,
  `HasPendingChanges` true, no mediator send); `ApplyStyle_SelectsTheFirstSection`; `FriendlyEditor_SaveWritesRawValues` (tick *Enable menu music* → the save
  command carries `noMenuMusic=0`); `IniConfigTab_ReappliesStyleWhenStateChanges`; `LanguageField_CodesTextShowsCurrentValues`.
- [ ] **Step 2: Run** → fail.
- [ ] **Step 3: Implement** the views: Friendly page header (title, description, no suffix), group boxes with footnotes, labels in the system face, check boxes
  captioned with their label; INI style unchanged apart from the literal `noMenuMusic`. Keep the milestone 11.1 focus wiring on every row.
- [ ] **Step 4: Run** Desktop + Architecture + full build → pass (the headless view-load suite included).
- [ ] **Step 5: Commit** — `feat(✨): render the INI editor in the chosen style and switch live`

---

## Task 5: Options chooses the style

**Files:** modify `Desktop/ViewModels/Dialogs/OptionsDialogViewModel.cs`, `Desktop/Views/Dialogs/OptionsDialogView.axaml`; tests in
`Tests/…Desktop.Tests.Unit/Dialogs/OptionsDialogViewModelTests.cs`.

**Interfaces — Consumes:** `IIniEditorStyleState` (Task 4), `LauncherOptions.EditorStyle` (Task 1).

- [ ] **Step 1: Failing tests:** `Initialise_LoadsTheEditorStyle`; `Ok_SavesTheEditorStyleAndAppliesIt` (state set after a successful save, not before);
  `FailedSave_DoesNotApplyTheStyle`; `StyleDescription_FollowsTheSelection` (the two §4 strings verbatim).
- [ ] **Step 2: Run** → fail.
- [ ] **Step 3: Implement** the *INI editor* group box between *Startup* and *Game launch*: the muted line *"Show settings in the INI Config tab:"*, radios
  `Friendly style` / `INI style`, the selected option's muted description beneath (spec §4, C15).
- [ ] **Step 4: Run** Desktop + Architecture + full build → pass.
- [ ] **Step 5: Commit** — `feat(✨): choose the INI editor style in Options`

---

## Task 6: The Restore dialogue follows the style

**Files:** modify `Desktop/Models/IniConfig/IniDisplayText.cs`, `Desktop/ViewModels/Dialogs/RestorePreviousIniDialogViewModel.cs` (and its view if the key
column needs width); tests in `Tests/…Desktop.Tests.Unit/IniConfig/IniDisplayTextTests.cs` and `Dialogs/RestorePreviousIniDialogViewModelTests.cs`.

**Interfaces — Produces:** `IniDisplayText.KeyText(IniEditorStyle style, IniKeyId id)` and `IniDisplayText.Format(IniEditorStyle style, IniKeyId id, string value)`;
the dialogue reads `IIniEditorStyleState.Style` when it builds its rows.

- [ ] **Step 1: Failing tests:** `KeyText_Ini_IsSectionSlashKey`; `KeyText_Friendly_IsPageGroupLabel` (*Sound › Intro movies › Play the first movie*;
  `lang` → *Language › Language › Language code*, `sublang` → *… › Region code*); `Format_Friendly_Toggle_IsOnOffByMeaning` (`noMenuMusic=1` → `Off`);
  `Format_Friendly_Volume_IsPercent` (`userAttenuation=2500` → `75 %`); `Format_Ini_IsUnchanged`; `Rows_SameCountInBothStyles` (a snapshot differing in
  `lang`, `sublang` and three others: 5 rows either way); `Rows_Friendly_FollowPageOrder`; `KeptKeysNote_NamesKeysInTheStyle`.
- [ ] **Step 2: Run** → fail.
- [ ] **Step 3: Implement** (spec §8, C8, C9, C11, C14). `(not set)` and unmappable raw values stay verbatim.
- [ ] **Step 4: Run** Desktop + Architecture + full build → pass.
- [ ] **Step 5: Commit** — `feat(✨): show the Restore diff in the chosen style`

---

## Task 7: Headless coverage and documentation

**Files:** `Tests/…Desktop.Tests.Unit/Views/` (headless editor tests, beside `IniEditorHarness`); `Docs/Specs/1-zoo-tycoon-launcher-master-design.md`;
`Docs/UserInterfaceDesign/conventions.md`.

- [ ] **Step 1: Headless tests:** `Editor_RendersInBothStyles` (every section/page selected in turn, no logged warning); `FriendlySoundPage_EnableMenuMusicTicked`
  when the file holds `noMenuMusic=0`; `FriendlySlider_DragReleasesFocus` and `FriendlySlider_TabKeepsFocus`. Prove one bites (break a conversion), revert.
- [ ] **Step 2: Docs.** The SDD already carries the design (revision 1.16, §5.1, §5.2, §5.5, §6.2, §9.3.3, §9.7, §9.8, §13.2): correct anything the build
  changed, and list the new tests in §11.4. conventions: the Friendly page header and group-box pattern, and the slider row. Prose ≤ 180.
- [ ] **Step 3: Run** every test project + full build → pass. **Commit** — `test(✅): cover both editor styles headlessly and document them`

---

## Task 8: Check it in the running app (controller)

- [ ] **Step 1:** Every Friendly page and every INI section; Options switches the style live with unsaved edits kept; Save in Friendly style and confirm `zoo.ini`
  holds raw values; the sliders (drag, arrows, Page Up/Down, an off-step value untouched); the Restore dialogue in both styles. Restore any value changed.
- [ ] **Step 2:** Close the launcher.

## Verification

- [ ] Clean build with 0 warnings; every test project passes.
- [ ] Task 8 checked in the running app, or recorded in the ledger as left to the maintainer.
