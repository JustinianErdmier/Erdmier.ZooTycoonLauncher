# Style Tokens and Milestone 11 Follow-ups Implementation Plan

**Date:** 2026-10-22

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Name every typography value, spacing value and repeated control size once, snap spacing onto the Windows 9x guideline values, use one fading
method, keep each group honest with its own architecture test, and close milestone 11's four polish items.

**Architecture:** Desktop only, plus tests and docs. New resource dictionaries under `Desktop/Styles/` (`Typography.axaml`, `Spacing.axaml`,
`Controls.axaml`) beside `Colours.axaml`, a `SpaceExtension` markup extension for per-side thicknesses, view sweeps, four architecture tests, the Options
save guard, and test-hygiene fixes.

**Tech Stack:** .NET 10, Avalonia 11.3 + Classic.Avalonia, CommunityToolkit.Mvvm, xUnit 2.9 + Shouldly + NSubstitute.

**Spec:** [`Docs/Specs/Epic-1-MVP/milestone-11.1-style-tokens-and-follow-ups-design.md`](../../Specs/Epic-1-MVP/milestone-11.1-style-tokens-and-follow-ups-design.md)
— read it before any task; decisions 1–6 (incl. 1a, 1b) and calls C1–C6 are binding; §4's token values are used verbatim.

## Global Constraints

- Branch `feat/milestone-11.1-style-tokens-and-follow-ups` (exists). **Commit every task automatically**; do not wait for review. Unsigned
  (`git -c commit.gpgsign=false commit`), message ending with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; stage only the task's files.
- Commit format `type(emoji): subject` (`feat`→✨, `fix`→🐛, `refactor`→♻️, `style`→🎨, `test`→✅, `docs`→📝), British English.
- **PowerShell** for `dotnet` and `git`; commit messages through a Git Bash heredoc. Foreground commands only. On a file-lock error, stop and report.
- No filesystem-wide searches. **Never write, rename or delete anything under `SampleInstallations/`.**
- `TreatWarningsAsErrors` and `Nullable` on. One type per file; folder mirrors namespace; file-scoped namespaces; XML docs on public members; spaced attributes;
  named arguments on literals in tests; every XAML file keeps `x:DataType`; `MainWindow.axaml` ≤ 100 lines; British English.
- Views use tokens through `{StaticResource …}` (or `{tokens:Space …}` for thicknesses); no new literal colours, fonts, sizes or spacing.
- Markdown wrapped at 180 characters (PowerShell `.Length`); new documents registered in `Erdmier.ZooTycoonLauncher.slnx`.
- The ledger `Docs/Ledgers/Epic-1-MVP/milestone-11.1-ledger.md` is written by the controller; Task 3 adds the "new token" column to its spacing table.

## Review Focus

1. **A view that clips or overlaps after the spacing snap** (tight dialogues such as Options, Fix, Delete) — nothing cut off. Pinned by the Task 8 drive.
2. **A thickness token name that does not exist** — the app fails loudly at load, not silently at zero. Pinned in Task 3.
3. **Esc pressed twice during an Options save** — the dialogue stays open until the save ends, then closes once. Pinned in Task 6.
4. **A literal sneaking back in through a style setter or an owner-qualified attribute** — each group's test catches it. Pinned in Tasks 1–4.
5. **The 16 px icons drawn at 32 px** — pixel-doubled and not blurred, and still 32 px. Pinned by the Task 8 drive.

---

## Task 0: Before screenshots and the spacing inventory (controller)

- [x] **Step 1:** Capture every main-window state and every dialogue from the current build (launched the same way each time) to the run workspace.
- [x] **Step 2:** Write the ledger's spacing table: every `Margin`, `Padding`, `Spacing`, `RowSpacing`, `ColumnSpacing`, `HorizontalSpacing`, `VerticalSpacing`
  value in `Source/Erdmier.ZooTycoonLauncher.Desktop/**/*.axaml` (file, line, attribute, original value), with an empty "new token" column.

---

## Task 1: Typography tokens

**Files:** create `Source/Erdmier.ZooTycoonLauncher.Desktop/Styles/Typography.axaml`; modify `App.axaml` and every view setting a font family, size or weight;
create `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/NoLiteralTypographyTests.cs`.

**Interfaces:** Produces resources `FontFamilySystem`, `FontFamilyMono` (`FontFamily`, conventions §1.3 stacks), `FontSizeBase` 11, `FontSizeStrong` 14,
`FontSizeAboutTitle` 13 (`x:Double`), `FontWeightStrong` (`Bold`), `FontWeightHeading` (`Solid`).

- [x] **Step 1: Write the failing test** `Views_UseNamedTypography`: every `FontFamily` / `FontSize` / `FontWeight` attribute (incl. owner-qualified such as
  `TextElement.FontSize`, and style setters, multi-line) in Desktop `*.axaml` except `Styles/Typography.axaml` holds a `{…}` value; failures list `file:line`.
  Share the scanning helper with `NoLiteralColoursTests` rather than copying it (extract `XamlAttributeScanner` into the test project).
- [x] **Step 2: Run** → fails, listing today's literals.
- [x] **Step 3: Implement** the dictionary and apply it: `Consolas` and the Lucida stack → `FontFamilyMono`; `11` → `FontSizeBase`; `Bold` → `FontWeightStrong`;
  `Solid` → `FontWeightHeading`. Do not apply `FontFamilySystem`, `FontSizeStrong` or `FontSizeAboutTitle` anywhere (C2).
- [x] **Step 4: Prove it bites** (plant a literal, see the failure, revert); run Architecture, Desktop and a full build (0 warnings) → pass.
- [x] **Step 5: Commit** — `style(🎨): define the launcher's typography once`

---

## Task 2: Shared styles and one fading method

**Files:** create `Desktop/Styles/Controls.axaml`; modify `App.axaml` and the views; create `Tests/…Tests.Architecture/SharedSizeStylesTests.cs`.

**Interfaces:** Produces style classes `DialogButton` (`Button`, `MinWidth` 75), `IconLarge` / `IconSmall` (`Image`, 32 / 16 px, `RenderOptions.BitmapInterpolationMode`
`None`), `MutedText` (`TextBlock`, `Foreground` `ForegroundMuted`, `FontSize` `FontSizeBase`).

- [x] **Step 1: Write the failing test** `Views_UseSharedSizeStyles`: outside `Styles/Controls.axaml`, no `MinWidth="75"`, and no `Image` with `Width`/`Height` of
  32 or 16. Also `Views_DoNotFadeTextWithOpacity`: no `Opacity="0.6"` on a `TextBlock`/`Run`/`StackPanel` of text (a whole `GroupBox`'s disabled `0.55` is allowed).
- [x] **Step 2: Run** → fails.
- [x] **Step 3: Implement:** buttons use `Classes="DialogButton"`; icons `Classes="IconLarge"` / `"IconSmall"` (crisp, decision 1a); every `Opacity="0.6"` fade
  becomes `Classes="MutedText"`, except the eight Installation Info row labels, which use `Foreground="{StaticResource ForegroundDim}"` (decision 1).
- [x] **Step 4: Prove it bites; run** Architecture, Desktop, full build → pass.
- [x] **Step 5: Commit** — `style(🎨): share button, icon and muted-text styles`

---

## Task 3: Spacing tokens and the snap

**Files:** create `Desktop/Styles/Spacing.axaml`, `Desktop/Styles/SpaceExtension.cs`; modify `App.axaml` and every view with spacing; create
`Tests/…Tests.Architecture/NoLiteralSpacingTests.cs` and `Tests/…Desktop.Tests.Unit/Styles/SpaceExtensionTests.cs`; fill the ledger's "new token" column.

**Interfaces:** Produces `x:Double` resources with spec §4's names and values (`GapSmallest` 3, `GapLabel` 5, `GapRelated` 6, `MarginDialog` 11,
`GapUnrelated` 11, `GroupBoxBottom` 11, `GroupBoxInset` 14, `GroupBoxFirstControl` 18, `GapIconBody` 11, `GapIconMessage` 17), plus style classes
`IconBesideBody` / `IconBesideMessage` in `Controls.axaml` that give an icon its right margin (token minus `GapRelated`, derived in the token file);
`public sealed class SpaceExtension : MarkupExtension`
(XML namespace prefix `tokens`) with a constructor `SpaceExtension(string uniform)` and settable `Left`, `Top`, `Right`, `Bottom`, `Horizontal`, `Vertical`
token names, returning a `Thickness`; an unset side is 0; an unknown name throws `InvalidOperationException` naming it.

- [x] **Step 1: Write the failing tests:** `SpaceExtensionTests` — `Uniform_ResolvesTheToken`, `PerSide_BuildsTheThickness`, `HorizontalVertical_Pair`,
  `UnknownToken_Throws` (inject a resolver so the tests need no `Application`); `NoLiteralSpacingTests.Views_UseSpacingTokens` — every spacing attribute and setter
  holds `{…}` or `0`.
- [x] **Step 2: Run** → fail.
- [x] **Step 3: Implement and snap** by role (spec C4): root dialogue/window body margins → `MarginDialog`; gaps between groups or sections →
  `GapUnrelated`; gaps between buttons, form rows, related controls → `GapRelated`; label-to-control → `GapLabel`; 1–2 px gaps → `GapSmallest`; group-box padding
  → `GroupBoxInset` sides, `GroupBoxBottom` bottom, and a top set so the first control sits `GroupBoxFirstControl` below the box's top edge; icon-beside-text
  `DockPanel` (`HorizontalSpacing="10"` + text `Margin="5 0 0 0"`) → `HorizontalSpacing="{StaticResource GapRelated}"`, no text margin, and the icon takes
  `IconBesideBody` (group-box / property-sheet layouts) or `IconBesideMessage` (confirmation, error and save prompts); the Auto-locate trail
  (`NoGameInstallationFoundView.axaml`, a `HeaderedContentControl` drawn as a group box) takes left and right `GroupBoxInset`, bottom `GroupBoxBottom`, a top
  that puts its first row `GroupBoxFirstControl` below the box's top edge after the header, and its grid's `RowSpacing="4"` becomes `GapRelated` (its
  `11 15 15 12` padding goes). Leave the main window size and the INI row-label width alone. Record each value's token in the ledger table.
- [x] **Step 4: Run** every test project and a full build (0 warnings) → pass.
- [x] **Step 5: Commit** — `style(🎨): snap spacing to the Windows 9x guideline tokens`

---

## Task 4: Widen the colour test

**Files:** modify `Tests/…Tests.Architecture/NoLiteralColoursTests.cs` (and the shared scanner from Task 1).

- [x] **Step 1:** Add to the scan: any attribute or setter property ending in `Brush` (e.g. `SelectionBrush`, `CaretBrush`), `OpacityMask`, element-content colours
  (`<SolidColorBrush …>Red</SolidColorBrush>`, `<Color>…</Color>`) outside `Colours.axaml`, and setters with other attributes between `<Setter` and
  `Property`/`Value`. Prove each bites, revert.
- [x] **Step 2: Run** Architecture → pass. **Commit** — `test(✅): widen the literal-colour scan`

---

## Task 5: Test hygiene

**Files:** create `Tests/…Tests.Architecture/Common/ProjectPaths.cs`; modify `MainWindowSizeTests.cs` and every test using `ResolveProjectDirectory`; the
launcher persistence test helpers (`ColumnExistsAsync`, `TableExistsAsync`); the migration Down test.

- [x] **Step 1:** Replace the duplicated project-directory resolvers with `ProjectPaths.Desktop` / `ProjectPaths.Source` (static, documented).
- [x] **Step 2:** The helpers stop disposing the connection EF owns (open it if closed; leave it to the context); parameterise the table/column names.
- [x] **Step 3:** The Down test asserts the restored column's default is `'System'` (`pragma_table_info` `dflt_value`), so its name is true.
- [x] **Step 4: Run** Architecture and Infrastructure → pass. **Commit** — `test(✅): share project paths and tidy the persistence test helpers`

---

## Task 6: Options — no cancel or close during a save

**Files:** modify `Desktop/ViewModels/Dialogs/OptionsDialogViewModel.cs`, `Desktop/Views/Dialogs/OptionsDialogView.axaml(.cs)`; test
`Tests/…Desktop.Tests.Unit/Dialogs/OptionsDialogViewModelTests.cs`.

**Interfaces:** Produces `OptionsDialogViewModel.CanClose` (`bool`, `!IsBusy`), mirrored from `RestorePreviousIniDialogViewModel.CanClose`; `CancelCommand` disabled
while busy; the view's `Closing` handler refuses a user close while `!CanClose`.

- [x] **Step 1: Write the failing tests:** `Cancel_WhileSaving_CannotExecute`; `CanClose_FalseWhileSaving_TrueAfter`; `EscTwiceDuringSave_ClosesOnceAfterSave`
  (two `CancelCommand` attempts during a pending save do nothing; completing the save raises `CloseRequested` exactly once).
- [x] **Step 2–4:** Run → fail; implement as `RestorePreviousIniDialogViewModel` does; run Desktop → pass.
- [x] **Step 5: Commit** — `fix(🐛): keep the Options dialogue open while it saves`

---

## Task 7: Documentation

- [x] **Step 1: conventions.md** — §1.3 / §1.4 become the typography and spacing token tables as built (spec §4, incl. `GapIconBody` and the DLU column);
  §2 points at `Styles/Controls.axaml` (`DialogButton`, `IconLarge`/`IconSmall` crisp, `MutedText`); §7 names one fading method (`MutedText`) and keeps §4's
  `0.55` group-box rule as a disabled state; drop §1.2's `dk-c-*` column; hard-wrap every prose line over 180.
- [x] **Step 2: SDD** — §9's conventions summary and §9.11 name the four token files and their tests; §11.5 lists the three new tests; DW-24 reworded to its
  remaining half (reusable stack/dock panel controls, `UniformStack`'s `TODO`s); revision **1.14**.
- [x] **Step 3: Verify** prose ≤ 180; **commit** — `docs(📝): document the launcher's style tokens`

---

## Task 8: Check it in the running app (controller)

- [x] **Step 1:** Recapture Task 0's screens and compare. Expected: lighter muted text, the Info labels' darker grey, Lucida Console where Consolas was,
  pixel-crisp icons, spacing shifts. Nothing clipped, overlapping or misaligned; icons still 32 / 16 px.
- [x] **Step 2:** Options still opens, saves and closes normally (the in-flight guard itself is unit-tested; a local save is too quick to catch by hand).
- [x] **Step 3:** Close the launcher; restore any setting changed.

## Verification

- [x] Clean build with 0 warnings; every test project passes.
- [x] Task 8 checked in the running app, or recorded in the ledger as left to the maintainer.

---

## Follow-ups after the first review (spec §6a, decisions 7–12)

Added 2026-10-22 from the maintainer's own use of the reviewed build. Same branch and PR; same Global Constraints.

## Task 9: INI field focus follows the keyboard

**Files:** modify `Desktop/Views/IniConfig/IniSectionView.axaml(.cs)` and, where a field needs it, the `Views/IniConfig/Fields/*View.axaml(.cs)` and
`Views/IniConfig/IniEditorView.axaml(.cs)`; `ViewModels/IniConfig/IniEditorViewModel.cs` only if the footer logic must change; tests in
`Tests/…Desktop.Tests.Unit` (and the headless project if it exists on this branch by then; otherwise view-model tests plus the Task 12 drive).

**Interfaces:** the footer keeps reading `IniFieldViewModel.IsFocused` / `IsHovered`; `IsFocused` becomes true only for keyboard focus
(`GotFocusEventArgs.NavigationMethod` is `Tab` or `Directional`), false for `Pointer`, and unchanged for `Unspecified` (a drop-down's own focus moves).

- [x] **Step 1: Write the failing tests** for whatever logic lands outside code-behind (e.g. a small focus-policy helper deciding from a `NavigationMethod`
  whether a field counts as focused, and whether a pointer-focused control should release focus after a commit). Name them for the spec's rules:
  `PointerFocus_DoesNotDriveTheFooter`, `KeyboardFocus_DrivesTheFooter`, `Escape_ClearsFieldFocus`.
- [x] **Step 2: Run** → fail.
- [x] **Step 3: Implement** spec §6a.1: keyboard-only `IsFocused`; a pointer-focused field releases focus (to the editor, not to another input) when a
  drop-down closes, after a toggle click, after a spinner step, and on Enter in a text or number box; a pointer press on a non-input part of the editor and
  Esc while an INI field is focused clear focus (a typed value commits as on lost focus). Section switching still clears hover and focus.
- [x] **Step 4: Run** Desktop + Architecture and a full build (0 warnings) → pass.
- [x] **Step 5: Commit** — `fix(🐛): let INI fields keep focus only from the keyboard`

---

## Task 10: A slightly darker muted grey

- [x] **Step 1:** `Styles/Colours.axaml`: `ForegroundMuted` `#808080` → `#666666` (leave `MidShadow`). Update every doc that quotes the muted value:
  conventions §1.1 and §7, SDD §9 and its 1.14 revision row (the ledger is the controller's).
- [x] **Step 2: Run** Architecture → pass. **Commit** — `style(🎨): darken the muted text grey for legibility`

---

## Task 11: Installation grid headers do not react to clicks

**Files:** `Desktop/Views/Common/InstallationGridView.axaml` (and a shared style in `Styles/Controls.axaml` if both grids can use one).

- [x] **Step 1:** Stop the column headers reacting to clicks, as `RestorePreviousIniDialogView`'s grid does (`DataGridColumnHeader` not hit-testable). Keep
  `CanUserResizeColumns="True"` if the Classic theme's resize grip still works with the header itself inert; if it cannot, turn resizing off and say so in
  the report. If the Restore grid and this one can share one style class, move the style into `Controls.axaml` and use it in both.
- [x] **Step 2: Run** Desktop + Architecture → pass. **Commit** — `fix(🐛): stop the installation grid's headers reacting to clicks`

---

## Task 12: Check the follow-ups in the running app (controller)

- [x] **Step 1:** INI editor: click the `fullscreen` drop-down and pick a value → focus leaves, footer shows the save status; click a number box's spinner,
  a toggle, type in a number box and press Enter → same; Tab to a field → it stays focused with its help shown; click empty editor space and press Esc
  while a field is focused → focus clears. Restore any value changed (Reset, or no save).
- [x] **Step 2:** Muted text is legible and still lighter than the Info labels.
- [x] **Step 3:** Installation Manager and Open Game Installation: clicking a column header does nothing visible.

---

## Task 13: The colour registry lists only what the launcher defines

**Files:** `Docs/UserInterfaceDesign/conventions.md` §1 (intro, §1.1, §1.2); create
`Tests/…Tests.Architecture/ColourRegistryTests.cs` (helpers under `Common/` if reusable).

- [x] **Step 1: Write the failing test** `ConventionsTables_MatchColoursAxaml`: parse the §1.1 and §1.2 Markdown tables (key in backticks in the first column,
  value as the first `#RRGGBB` in the row) and `Styles/Colours.axaml` (`x:Key` + `Color`), and assert the two key→value sets are equal, reporting each key
  missing from either side or differing in value. Assert both sets are non-empty so the test cannot pass vacuously.
- [x] **Step 2: Run** → fail (the ten unused keys).
- [x] **Step 3: Edit conventions.md** per spec §6a.4: remove `SurfaceHighlight`, `LightHighlight`, `DeepShadow`, `DarkShadow`, `Foreground`,
  `InputBackgroundReadOnly`, `InputBackgroundInvalid`, `AccentNavyBright`, `AccentTeal`, `AccentYellow`; drop the "reserved role names" paragraph and state
  the rule (a colour the theme already sets gets a key only when the launcher overrides it); tidy the §1 introduction (one "literal is a defect" sentence,
  point at §7 for the attribute list). Fix any other reference to a removed key (conventions and SDD §9). Prose ≤ 180.
- [x] **Step 4: Run** Architecture → pass. **Commit** — `docs(📝): list only the colours the launcher defines, and test it`

---

## Task 14: Tests that do what they say (spec §6a.5, decision 11)

**Files:** `Tests/…Tests.Architecture/TokenReferencesTests.cs`, `SharedSizeStylesTests.cs`, `ColourRegistryTests.cs` (proof only),
`Common/XamlResourceKeys.cs`; SDD §11.5 if a test's description changes.

- [ ] **Step 1:** `TokenReferencesTests`: each test asserts it matched at least one reference (`ResourceReferences_Resolve` a `{StaticResource …}`,
  `SpaceTokens_NameDoubleResources` a `{tokens:Space …}`, `SpacingResources_HaveTheRightType` a spacing attribute holding a resource). Prove each guard by
  pointing the test at an empty input or breaking its regex temporarily; revert.
- [ ] **Step 2:** `Views_DoNotFadeTextWithOpacity`: skip values that start with `{`; treat `1`, `1.0` and `1.00` as opaque. `Views_UseSharedSizeStyles`: report
  every literal-`MinWidth` button, keyed by position rather than line. Prove both with plants; revert.
- [ ] **Step 3:** Prove the unproven paths with plants (record the failure output, then revert): a `<Setter Property="MinWidth" Value="80" />` in a
  `Button` style inside a view, and a duplicated key in `Colours.axaml`.
- [ ] **Step 4:** `XamlResourceKeys.CollectShared` reads `Styles/**/*.axaml`.
- [ ] **Step 5: Run** Architecture + a full build (0 warnings). **Commit** — `test(✅): make the token tests check what they claim`

---

## Task 15: Test-code tidy (spec §6a.6, decision 12)

**Files:** the architecture tests that build `new Regex` (`SharedSizeStylesTests`, `AvaloniaPropertyFieldTests`, `Common/XamlAttributeScanner.cs` and any other);
the Infrastructure persistence tests' parameter helpers (`LauncherDbContextTests`, `InstallationDbContextTests`, `IniSnapshotRepositoryTests`);
`Tests/…Tests.Architecture/Common/ProjectPaths.cs`.

- [ ] **Step 1:** Fixed patterns become `[GeneratedRegex]` on `partial` classes; patterns built from arguments (the scanner's property-name alternations) are built
  once per distinct name set and cached, not per `Scan` call.
- [ ] **Step 2:** One shared helper (one type per file, under the Infrastructure test project's `Common/` or `Persistence/` folder) adds a named SQLite
  parameter to a `DbCommand`; the three classes use it.
- [ ] **Step 3:** `ProjectPaths.Project(name)` checks that `<name>/<name>.csproj` exists, as the old resolver did.
- [ ] **Step 4: Run** Architecture + Infrastructure + a full build (0 warnings). **Commit** — `refactor(♻️): tidy the architecture and persistence test code`

## Verification (follow-ups)

- [ ] Clean build with 0 warnings; every test project passes (after Tasks 14–15).
- [x] Task 12 checked in the running app.
