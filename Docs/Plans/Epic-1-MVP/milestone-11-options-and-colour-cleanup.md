# Options Dialogue and Colour Cleanup Implementation Plan

**Date:** 2026-10-21

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the Options dialogue (startup preference, close-after-launch), remove the unused theme setting, and define every view colour once by name with a
test that keeps literal colours out.

**Architecture:** Domain loses `LauncherTheme` and `InstallationValidity.ColourToken`; an EF migration drops the `Theme` column. Application gains a launcher
options query and save command. Desktop gains `OptionsDialogViewModel` / `OptionsDialogView` behind `File → Options…`, a `Colours.axaml` resource dictionary
used through `StaticResource`, and converters that turn validity and error state into brushes. An architecture test bans literal colours.

**Tech Stack:** .NET 10, Avalonia 11.3 + Classic.Avalonia, CommunityToolkit.Mvvm, Mediator 3.0.2 (source-generated), ErrorOr, FluentValidation, EF Core + SQLite,
Ardalis.SmartEnum, xUnit 2.9 + Shouldly + NSubstitute.

**Spec:** [`Docs/Specs/Epic-1-MVP/milestone-11-options-and-colour-cleanup-design.md`](../../Specs/Epic-1-MVP/milestone-11-options-and-colour-cleanup-design.md) —
read it before any task; decisions 1–5 and calls C1–C7 are binding, and C1's copy and C5's colour values are used verbatim.

## Global Constraints

- Branch `feat/milestone-11-options-and-colour-cleanup` (exists). **Commit every task automatically** with the message given; do not wait for review first.
  Commits are unsigned (`git -c commit.gpgsign=false commit`), message ending with the trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; stage only
  the task's files.
- Commit format `type(emoji): subject` (`feat`→✨, `fix`→🐛, `refactor`→♻️, `style`→🎨, `test`→✅, `docs`→📝), British English.
- **PowerShell** for `dotnet` and `git`; commit messages through a Git Bash heredoc. Build and test narrowly. On a file-lock error, stop and report.
- No background commands, no filesystem-wide searches. **Never write, rename or delete anything under `SampleInstallations/`.**
- `TreatWarningsAsErrors` and `Nullable` are on. One type per file; folder mirrors namespace; no files at a project root; file-scoped namespaces; XML docs on every
  public type and member; `<c>…</c>` without inside whitespace; spaced attributes; `[ ObservableProperty ]` on `partial` properties, `[ RelayCommand ]` on private
  async methods; never add `[ UsedImplicitly ]`; blank line between statements of different kinds; named arguments on literals in tests; designer constructors
  keep compiling; every XAML file keeps `x:DataType`; every new `*ViewModel` has a sibling view; `MainWindow.axaml` stays ≤ 100 lines.
- Log levels: unexpected exception → `LogError(ex, …)`; an `ErrorOr` failure shown to the user → `LogWarning(…)`.
- Markdown wrapped at 180 characters (PowerShell `.Length`); new documents registered in `Erdmier.ZooTycoonLauncher.slnx`.
- The ledger `Docs/Ledgers/Epic-1-MVP/milestone-11-ledger.md` is written by the controller.

## Review Focus

1. **An existing `Launcher.db` with a `Theme` column and stored settings** — the migration keeps the startup preference, close setting and default id. Pinned in
   Task 1.
2. **Opening Options, changing nothing, pressing `OK`** — saves the same values and never touches the default installation. Pinned in Tasks 2 and 3.
3. **A save that fails** (database locked) — the dialogue says so and stays open with the user's choices intact. Pinned in Task 3.
4. **The status bar's error colour after the colour change** — still red (`AccentRed`) for a boot failure and default otherwise. Pinned in Task 5.
5. **A colour literal sneaking back into a view** — the architecture test fails, naming the file and line. Pinned in Task 6.

---

## Task 0: Before screenshots (controller)

- [x] **Step 1:** Launch the current build and save PNG captures of the General tab, the INI editor, the Installation Manager and the Restore Previous INI
  dialogue to the run workspace (not committed). Close the launcher.

---

## Task 1: Remove the theme setting

**Files:**
- Delete: `Source/Erdmier.ZooTycoonLauncher.Domain/Settings/LauncherTheme.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Domain/Settings/LauncherSettings.cs`,
  `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Configurations/LauncherSettingsConfiguration.cs`, any test referencing `Theme`
- Create: a migration `RemoveLauncherTheme` under `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Migrations/`, generated with:

  ```powershell
  dotnet ef migrations add RemoveLauncherTheme --project Source/Erdmier.ZooTycoonLauncher.Infrastructure --context LauncherDbContext --output-dir Persistence/Launcher/Migrations
  ```
- Test: the Launcher persistence tests in `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/` (follow
  `IniSnapshotRepositoryTests.Migration_UpgradesADatabaseCreatedByTheInitialSchema`)

- [x] **Step 1: Write the failing test** `Migration_DropsTheThemeColumn_AndKeepsSettings`: build a database at the `InitialLauncherSchema` migration, insert a
  settings row (`LauncherStartupPreference` = `LastPlayedInstallation`, `CloseAfterGameLaunch` = true, a `DefaultInstallationId`, `Theme` = `Dark`), migrate to
  latest, then assert the `Theme` column is gone (`PRAGMA table_info`) and the repository returns the three values unchanged.
- [x] **Step 2: Run** → fails (no migration yet).
- [x] **Step 3: Implement** — delete the enum and property, remove its configuration, generate the migration (its `Down` re-adds the column with the old
  default). Fix every reference, including tests.
- [x] **Step 4: Run** Domain, Application, Infrastructure, Architecture → pass.
- [x] **Step 5: Commit** — `refactor(♻️): remove the unused launcher theme setting`

---

## Task 2: Application — launcher options query and save

**Files:** create under `Source/Erdmier.ZooTycoonLauncher.Application/Settings/`: `Common/LauncherOptions.cs`, `Get/GetLauncherOptionsQuery.cs`,
`Get/GetLauncherOptionsHandler.cs`, `Save/SaveLauncherOptionsCommand.cs`, `Save/SaveLauncherOptionsHandler.cs`, `Save/SaveLauncherOptionsValidator.cs`;
tests under `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Settings/`.

**Interfaces:**
- Consumes: `ILauncherSettingsRepository.GetAsync` / `UpdateAsync`.
- Produces: `public sealed record LauncherOptions(LauncherStartupPreference StartupPreference, bool CloseAfterGameLaunch);`
  `public sealed record GetLauncherOptionsQuery : IQuery<ErrorOr<LauncherOptions>>;`
  `public sealed record SaveLauncherOptionsCommand(LauncherStartupPreference StartupPreference, bool CloseAfterGameLaunch) : ICommand<ErrorOr<Success>>;`

Save loads the settings, sets the two properties, and calls `UpdateAsync` — `DefaultInstallationId` is never assigned. A repository exception propagates (the
dialogue catches it). Validator: `StartupPreference` not null (`"Choose what the launcher opens at start-up."`).

- [x] **Step 1: Write the failing tests:** `Get_ReturnsTheStoredValues`; `Save_WritesBothValues_AndKeepsTheDefaultInstallation` (stored default id unchanged in
  the entity passed to `UpdateAsync`); `Save_UnchangedValues_StillSaves`; `Validator_NullPreference_Fails`.
- [x] **Step 2: Run** `--filter "FullyQualifiedName~LauncherOptions"` → compile failure.
- [x] **Step 3: Implement.**
- [x] **Step 4: Run** the filter, then Application + Architecture → pass.
- [x] **Step 5: Commit** — `feat(✨): read and save the launcher options`

---

## Task 3: Desktop — the Options dialogue and menu item

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/OptionsDialogViewModel.cs`, `Views/Dialogs/OptionsDialogView.axaml` (+ `.axaml.cs`)
- Modify: `Composition/IDialogService.cs`, `Composition/AvaloniaDialogService.cs`, every `IDialogService` stub, DI registration, `Views/MainWindow.axaml`,
  `ViewModels/MainWindowViewModel.cs`
- Test: create `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Dialogs/OptionsDialogViewModelTests.cs`

**Interfaces:**
- Consumes: Task 2's query and command.
- Produces: `Task IDialogService.ShowOptionsAsync()`; `OptionsDialogViewModel` with `InitialiseAsync(CancellationToken)`, observable `StartupPreference`
  (`LauncherStartupPreference`), `CloseAfterGameLaunch` (`bool`), `ErrorMessage` (`string?`), `IsBusy`, `OkCommand`, `CancelCommand`, `event EventHandler?
  CloseRequested`; `MainWindowViewModel.OpenOptionsCommand`.

Copy, layout and behaviour from spec C1–C3 verbatim: title `"  Options"`, icon `/Assets/Win95Icons/controls32.ico`, `Width="460"`, two group boxes, the four
radio buttons bound one-to-one to the four `LauncherStartupPreference` values (a converter or four boolean properties — implementer's choice, tested), check box
and indented muted helper with the monospace `zoo.exe`, `OK` (default) / `Cancel`. `OK` sends `SaveLauncherOptionsCommand`; success closes; an `ErrorOr`
failure or exception shows `ShowErrorAsync("Cannot Save Options", …)` and stays open with the choices intact. The menu header is `O_ptions…` bound to
`OpenOptionsCommand` in place of the inert `Settings…` item.

- [x] **Step 1: Write the failing tests:** `Initialise_LoadsTheStoredValues`; `Ok_SendsOneSaveWithTheChosenValues_AndCloses`; `Cancel_SendsNothing_AndCloses`;
  `Ok_SaveFails_ShowsErrorAndStaysOpen` (values unchanged, `CloseRequested` not raised, warning logged); `Ok_SaveThrows_ShowsErrorAndStaysOpen`;
  `OkWhileBusy_CannotExecute`; and one test that each radio choice maps to its preference.
- [x] **Step 2: Run** the filter → fail.
- [x] **Step 3: Implement.**
- [x] **Step 4: Run** Desktop + Architecture → pass.
- [x] **Step 5: Commit** — `feat(✨): add the Options dialogue`

---

## Task 4: Colours defined once

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Styles/Colours.axaml` (a `ResourceDictionary`), merged in `App.axaml`
- Modify: every `*.axaml` under `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/` with a literal colour

The dictionary holds one `SolidColorBrush` per C5 key with C5's exact values (`ForegroundMuted`, `ForegroundDim`, `AccentGreen`, `AccentRed`, `AccentMaroon`,
`AccentNavy`, `AccentAmber`, `Surface`, `InputBackground`, `MidShadow`, `RuleDotted`). Map literals: `Gray` text → `ForegroundMuted`, `Gray` borders →
`MidShadow`, `#404040` → `ForegroundDim`, `Green` → `AccentGreen`, `Red` → `AccentRed`, `Maroon` → `AccentMaroon`, `Navy` → `AccentNavy`, `DarkGoldenrod` →
`AccentAmber`, `Silver` → `Surface`, `White` → `InputBackground`, `#E0E0E0` → `RuleDotted`; `Transparent` stays. Use `{StaticResource …}`.

- [x] **Step 1:** Implement; then `Select-String` the Desktop views for `(Foreground|Background|Fill|Stroke|BorderBrush|Color)="[^{"]` and confirm only
  `Transparent` (and `Colours.axaml`) remain.
- [x] **Step 2: Run** the solution build (0 warnings) and Desktop + Architecture → pass.
- [x] **Step 3: Commit** — `style(🎨): define the launcher's colours once`

---

## Task 5: Colour leaves the Domain and the view models

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Domain/Installations/InstallationValidity.cs` (remove `ColourToken`),
  `Desktop/Converters/ColourTokenToBrushConverter.cs` → replace with `ValidityToBrushConverter.cs`, `Desktop/Models/InstallationGridRowModel.cs`,
  `Desktop/ViewModels/Common/InstallationGridViewModel.cs`, `Desktop/ViewModels/Dialogs/InstallationInfoDialogViewModel.cs`,
  `Desktop/ViewModels/MainWindowViewModel.cs`, the views that bound the token, `Views/MainWindow.axaml`
- Create: `Desktop/Converters/ValidityToBrushConverter.cs`, `Desktop/Converters/ErrorToBrushConverter.cs`
- Test: Domain validity tests, Desktop converter and grid tests, `MainWindowViewModel` tests

**Interfaces:**
- Produces: `ValidityToBrushConverter` (`InstallationValidity` → the `AccentGreen` brush when `Valid`, else `AccentRed`, read from the application resources);
  `ErrorToBrushConverter` (`true` → `AccentRed`, `false` → `null`/unset); `InstallationGridRowModel` carries `InstallationValidity Validity` instead of the token;
  `InstallationInfoDialogViewModel.Validity` replaces `StatusColourToken`; `MainWindowViewModel.IsStatusError` (`bool`) replaces `StatusMessageSecondaryColour`.

- [x] **Step 1: Write the failing tests:** `ValidityToBrushConverter_Valid_IsAccentGreen` / `_Invalid_IsAccentRed` (compare `Color` values `#008000` /
  `#C00000`); `IsSelectionInvalid_FollowsValidity`; `BootFailure_SetsIsStatusError` and `SuccessfulBoot_ClearsIsStatusError`.
- [x] **Step 2: Run** → fail.
- [x] **Step 3: Implement**; keep `MainWindow.axaml` ≤ 100 lines.
- [x] **Step 4: Run** every test project → pass.
- [x] **Step 5: Commit** — `refactor(♻️): move status colours out of the domain and view models`

---

## Task 6: Architecture test — no literal colours

**Files:** create `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/NoLiteralColoursTests.cs`

- [x] **Step 1: Write** `Views_UseNamedColours`: for every `*.axaml` under `Source/Erdmier.ZooTycoonLauncher.Desktop/` except `Styles/Colours.axaml`, no
  `Foreground|Background|BorderBrush|Fill|Stroke|Color` attribute holds a value that does not start with `{` and is not `Transparent` (and `Fill` values `True`/
  `False` are booleans, not colours — exclude them); failures list `file:line`. And `Code_UsesNoBrushLiterals`: no `.cs` under Desktop or Domain contains
  `Brushes.` or `Colors.`. Locate the source tree the way `MainWindowSizeTests` does.
- [x] **Step 2: Prove it** — temporarily add `Foreground="Red"` to a view, see the test fail with that line, revert.
- [x] **Step 3: Run** Architecture → pass.
- [x] **Step 4: Commit** — `test(✅): forbid literal colours in views and code`

---

## Task 7: Documentation

- [x] **Step 1: SDD** per spec §5 (§5.1/§5.2 incl. `ColourToken`, §6.2 `Theme` column, §9.8 Options, §9.10 menu row, §9.11 removed with its one-line note, §9.12,
  §13.2, every other theme/dark/`ColourToken` mention, §13.5 delete DW-20/37/39), revision **1.13**.
- [x] **Step 2: conventions.md** §1: drop the Dark columns and the light/dark sentences, add `RuleDotted`, say colours live in `Styles/Colours.axaml` through
  `StaticResource`; update any `Settings` mention to `Options` (icon table row).
- [x] **Step 3: Verify** prose ≤ 180 characters; commit — `docs(📝): document the Options dialogue and the single colour set`

---

## Task 8: Check it in the running app (controller)

- [x] **Step 1:** `File → Options…` shows C1's layout and copy with the stored values; change both, `OK`; reopen to confirm they stuck; restart the launcher
  and confirm the startup choice took effect; restore the original values.
- [x] **Step 2:** Recapture Task 0's four screens and compare: only red text and the amber headline differ.
- [x] **Step 3:** Close the launcher.

## Verification

- [x] Clean build with 0 warnings; every test project passes.
- [x] Task 8 checked in the running app, or recorded in the ledger as left to the maintainer.
