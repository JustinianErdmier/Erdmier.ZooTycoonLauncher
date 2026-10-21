# INI Display Polish Implementation Plan

**Date:** 2026-10-21

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the INI editor and the Restore Previous INI dialogue read the way a non-technical player expects — cash as dollars, diff values as the editor shows
them, the language codes driven only by the picker — and tidy two pieces of dialogue chrome.

**Architecture:** Desktop only. Two new presentation flags on `IniFieldDescriptor` (`IsCurrency`, `IsReadOnly`) set in `IniEditorCatalogue`; one shared cash
format (`IniCurrencyFormat`) used by the number row and by a new diff formatter (`IniDisplayText`) that turns an effective INI value into the text its editor row
shows. The rest is XAML.

**Tech Stack:** .NET 10, Avalonia 11.3 + Classic.Avalonia, CommunityToolkit.Mvvm, xUnit 2.9 + Shouldly + NSubstitute.

**Spec:** none — the five items and their decisions were agreed in chat on 2026-10-21 and are recorded below as *Decisions*. The SDD (§9.3, §9.7) wins wherever
this plan is silent. Background: the [milestone-9.1 ledger](../../Ledgers/Epic-1-MVP/milestone-9.1-ledger.md).

## Decisions

1. **Diff values match the editor.** Each `Current` / `Snapshot` cell shows the value the way its editor row shows it: a choice by its option label (`fullscreen`
   → `Fullscreen` / `Windowed`, `level` → `2 – Balance`); a check-box key as `true` / `false`; a cash key as `$10,000`; everything else verbatim. `(not set)` stays.
   The `Key` column is unchanged. **Inverted toggles show the key's own truth** (maintainer's ruling): `[UI]/noMenuMusic` reads `true` when the INI holds `1`
   (music off), even though the editor's box reads "Play menu music". A value the formatter cannot map (no catalogue entry, no matching option, unparseable) is shown
   verbatim.
2. **History headers are inert.** The history grid's column headers can be neither clicked nor resized.
3. **Prompt titles clear the icon.** Every `ClassicWindow` title starts with two spaces so it clears the title-bar icon; the Confirmation, Error and Save-changes
   prompts lack them today. (`LaunchErrorView` is a plain OS `Window` and is left alone.)
4. **`lang` and `sublang` are read-only.** The curated picker is the only way to change them in the GUI. A pair the picker does not list still shows both numbers with
   the picker blank. Reset, Restore defaults and Restore previous keep updating both rows. A read-only number row hides its spinner and shows the Win9x read-only grey.
5. **Cash displays as dollars.** `MSStartingCash`, `MSCashIncrement`, `MSMinCash` and `MSMaxCash` display as `$1,000,000` — always `$` and `,` grouping, no
   decimals, whatever the OS culture. Typing `250000` or `$250,000` both commit `250000`; a typed decimal is rejected
   (the box reverts). `zoo.ini` keeps the plain integer. Hints become `$0–$10,000,000` and
   `$100–$1,000,000`.

## Global Constraints

- Branch `feat/milestone-9.2-ini-display-polish` (exists, cut from `main` at `710c5a2`). **Commit every task automatically** with the message given; do not wait for
  review first. Commits are unsigned (`git -c commit.gpgsign=false commit`), message ending with the trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`;
  stage only the task's files.
- Commit format `type(emoji): subject` (`feat`→✨, `fix`→🐛, `style`→🎨, `test`→✅, `docs`→📝), British English.
- **PowerShell** for `dotnet` and `git`; commit messages may go through a Git Bash heredoc. Build and test narrowly:
  `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj --filter "FullyQualifiedName~<Class>"`. On a
  file-lock error, stop and report.
- No background commands, no filesystem-wide searches. **Never write, rename or delete anything under `SampleInstallations/`** (the app may).
- `TreatWarningsAsErrors` and `Nullable` are on. One type per file; no files at a project root; file-scoped namespaces; XML docs on every public type and member;
  `<c>…</c>` without inside whitespace; spaced attributes (`[ Fact ]`); `[ ObservableProperty ]` on `partial` properties; never add `[ UsedImplicitly ]`; blank line
  between statements of different kinds; named arguments on literals in tests; designer constructors keep compiling; every XAML file keeps its `x:DataType`.
- Markdown wrapped at 180 characters (measured with PowerShell `.Length`).
- The ledger `Docs/Ledgers/Epic-1-MVP/milestone-9.2-ledger.md` is written by the controller during the run.

## Review Focus

1. **A non-English OS culture** (`de-DE`, decimal comma) — cash still reads `$1,000,000` and `$250,000` still parses. Pinned in Task 1.
2. **A cash value typed with decimals** (`$1,000.75`) — rejected, so the box reverts and never a decimal reaches `zoo.ini`. Pinned in Task 1.
3. **A stored bool spelled `True` / ` 1 `, or a choice stored as ` 1`** — the diff still shows the label / `true`, not the raw text. Pinned in Task 2.
4. **An `lang`/`sublang` pair the picker does not list** — both numbers show, the picker is blank, and the rows still cannot be typed into. Pinned in Task 3.
5. **Choosing a picker entry after a mismatched pair loaded** — both read-only rows update and become dirty. Pinned in Task 3.

---

## Task 1: Cash display format (decision 5)

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniCurrencyFormat.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniFieldDescriptor.cs`, `IniEditorCatalogue.cs`,
  `ViewModels/IniConfig/Fields/IniNumberFieldViewModel.cs`, `Views/IniConfig/Fields/IniNumberFieldView.axaml`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/IniConfig/IniEditorCatalogueTests.cs`, `IniConfig/Fields/IniFieldViewModelTests.cs`

**Interfaces:**
- Produces: `IniFieldDescriptor.IsCurrency { get; init; }` (`bool`, default `false`).
- Produces: `public static class IniCurrencyFormat` with `string FormatString` (`"$#,0"`), `NumberFormatInfo NumberFormat` (a read-only clone of the invariant
  culture's with `CurrencySymbol = "$"`), `NumberStyles ParsingNumberStyle` (`NumberStyles.Currency`), and `string Format(int value)`
  (`value.ToString(FormatString, NumberFormat)`).
- Produces: on `IniNumberFieldViewModel`, `string FormatString`, `NumberFormatInfo NumberFormat` and `NumberStyles ParsingNumberStyle` — the currency trio when the
  descriptor `IsCurrency`, otherwise `"0"`, `CultureInfo.CurrentCulture.NumberFormat` and `NumberStyles.Integer` (today's behaviour).

- [x] **Step 1: Write the failing tests.**
  - `IniEditorCatalogueTests.CashRows_AreCurrency`: the descriptors whose `IsCurrency` is true are exactly `[UI]/MSStartingCash`, `MSCashIncrement`, `MSMinCash`,
    `MSMaxCash`; their hints are `$0–$10,000,000`, `$100–$1,000,000`, `$0–$10,000,000`, `$0–$10,000,000`.
  - `IniFieldViewModelTests.Number_Cash_FormatsAsDollars`: under `CultureInfo.CurrentCulture = de-DE` (restore it in `finally`), a cash row's
    `1000000m.ToString(row.FormatString, row.NumberFormat)` is `$1,000,000`; `decimal.TryParse("$250,000", row.ParsingNumberStyle, row.NumberFormat, out d)` gives
    `250000`; `"250000"` parses to the same.
  - `IniFieldViewModelTests.Number_Cash_RejectsDecimalInput`: with the row's style and format, `decimal.TryParse` is false for `$1,000.75`, `1000.5`, `-500` and
    `(500)`, and true for `$1,000` and ` $250,000 ` (giving `1000` and `250000`).
  - `IniFieldViewModelTests.Number_NonCash_KeepsThePlainFormat`: `UpdateRate`'s `FormatString` is `0` and `ParsingNumberStyle` is `NumberStyles.Integer`.
- [x] **Step 2: Run** `--filter "FullyQualifiedName~IniEditorCatalogueTests|FullyQualifiedName~IniFieldViewModelTests"` → the new tests fail to compile or fail.
- [x] **Step 3: Implement** the interfaces above. In the catalogue, add a `Cash(section, key, hint, help)` factory beside `Number` that sets `IsCurrency = true`, and
  use it for the four rows with the new hints. In `IniNumberFieldView.axaml`, bind `FormatString`, `NumberFormat` and `ParsingNumberStyle` to the view model.
- [x] **Step 4: Run** the same filter, then the whole Desktop test project → pass.
- [x] **Step 5: Commit** — `feat(✨): show the INI cash settings as dollars`

---

## Task 2: Diff values as the editor shows them (decision 1)

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniDisplayText.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniEditorCatalogue.cs`, `ViewModels/Dialogs/RestorePreviousIniDialogViewModel.cs` (`ToDiffRow`)
- Test: create `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/IniConfig/IniDisplayTextTests.cs`; modify `Dialogs/RestorePreviousIniDialogViewModelTests.cs`

**Interfaces:**
- Consumes: `IniCurrencyFormat.Format(int)`, `IniFieldDescriptor.IsCurrency` (Task 1); `IniKeySpec.TryParseBool(string?, out bool)`; `IniKeySpec.AreEquivalent`.
- Produces: `IniEditorCatalogue.TryGetField(IniKeyId id, [ NotNullWhen(true) ] out IniFieldDescriptor? field)` — the keyed descriptor for `id`.
- Produces: `public static class IniDisplayText` with `string Format(IniKeyId id, string value)`.

`Format` rules, by the descriptor's `Control`: `Choice` → the label of the option whose `Raw` is `AreEquivalent` to `value` under the key's spec; `Toggle` →
`"true"` / `"false"` from `TryParseBool` (**ignore `Inverted`**); `Number` with `IsCurrency` → `IniCurrencyFormat.Format` of the parsed integer; anything else, or a
value that does not map, → `value` unchanged.

- [x] **Step 1: Write the failing tests** in `IniDisplayTextTests` (one `[ Theory ]` or several `[ Fact ]`s):
  `[user]/fullscreen` `1` → `Fullscreen`, `0` → `Windowed`; `[advanced]/level` `2` → `2 – Balance`; `[UI]/helpType` ` 1` → `Standard`; `[UI]/playMovie` `1` →
  `true`, `0` → `false`, `True` → `true`; `[UI]/noMenuMusic` `1` → `true`; `[UI]/MSStartingCash` `10000` → `$10,000`; `[user]/screenwidth` `800` → `800`;
  `[UI]/menuMusic` `x.mp3` → `x.mp3`; `[user]/fullscreen` `7` → `7`; an unknown `[nope]/key` `abc` → `abc`.
  In `RestorePreviousIniDialogViewModelTests`, add `Initialise_DiffShowsEditorValues`: a diff row for `[user]/fullscreen` current `1`, snapshot `0` reads
  `CurrentText: "Fullscreen"`, `SnapshotText: "Windowed"`; the existing `(not set)` assertion stays.
- [x] **Step 2: Run** `--filter "FullyQualifiedName~IniDisplayTextTests|FullyQualifiedName~RestorePreviousIniDialogViewModelTests"` → fail.
- [x] **Step 3: Implement** `TryGetField` (a dictionary built once from `Sections`) and `IniDisplayText.Format`; in `ToDiffRow`, pass both values through
  `IniDisplayText.Format(row.Id, …)`, keeping `NotSet` for a null `Current`.
- [x] **Step 4: Run** the same filter, then the whole Desktop test project → pass.
- [x] **Step 5: Commit** — `feat(✨): show Restore Previous INI diff values as the editor shows them`

---

## Task 3: Read-only language codes (decision 4)

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniFieldDescriptor.cs`, `IniEditorCatalogue.cs`,
  `ViewModels/IniConfig/Fields/IniNumberFieldViewModel.cs`, `Views/IniConfig/Fields/IniNumberFieldView.axaml`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/IniConfig/IniEditorCatalogueTests.cs`, `IniConfig/Fields/IniFieldViewModelTests.cs`

**Interfaces:**
- Produces: `IniFieldDescriptor.IsReadOnly { get; init; }` (`bool`, default `false`); `IniNumberFieldViewModel.IsReadOnly` (from the descriptor).

New help copy: the picker — `Game language. Choosing an entry sets both LANGID and SUBLANGID below.`; `lang` — `Windows primary language identifier (LANGID). Set
by the language picker above.`; `sublang` — `Windows sub-language identifier (SUBLANGID). Set by the language picker above.`

- [x] **Step 1: Write the failing tests.**
  - `IniEditorCatalogueTests.OnlyTheLanguageCodes_AreReadOnly`: the descriptors with `IsReadOnly` are exactly `[language]/lang` and `[language]/sublang`.
  - `IniFieldViewModelTests.LanguagePicker_UnlistedPair_ShowsBothCodesAndNoSelection`: load `lang` `9`, `sublang` `7`; both rows are `IsReadOnly`, show `9` and `7`,
    and `SelectedOption` is null.
  - `IniFieldViewModelTests.LanguagePicker_ChoosingAfterUnlistedPair_WritesBothReadOnlyRows`: from that state, set `SelectedOption` to `German (Germany)`; the rows
    read `7` / `1` and both are dirty.
- [x] **Step 2: Run** `--filter "FullyQualifiedName~IniEditorCatalogueTests|FullyQualifiedName~IniFieldViewModelTests"` → fail.
- [x] **Step 3: Implement.** Set `IsReadOnly = true` on the two catalogue rows (and the new help copy); expose it on the number view model; in the view bind
  `NumericUpDown.IsReadOnly` and set `ShowButtonSpinner` to its negation. Confirm in the running app (or by reading the Classic template) that the inner text box picks
  up `App.axaml`'s read-only grey; if it does not, extend that style's selector to cover a read-only `NumericUpDown`'s text box.
- [x] **Step 4: Run** the same filter, then the whole Desktop test project → pass.
- [x] **Step 5: Commit** — `feat(✨): make the INI language codes read-only behind the picker`

---

## Task 4: Dialogue chrome (decisions 2, 3) and documentation

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/RestorePreviousIniDialogView.axaml`, `ConfirmationPromptView.axaml`, `ErrorMessageView.axaml`,
  `SaveChangesPromptView.axaml`
- Modify: `Docs/Specs/1-zoo-tycoon-launcher-master-design.md`, `Docs/UserInterfaceDesign/conventions.md`

- [x] **Step 1: History headers.** In the history `DataGrid`, set `CanUserResizeColumns="False"` and, in its existing `DataGridColumnHeader` style, set
  `IsHitTestVisible` to `False` so a click neither depresses nor resizes a header.
- [x] **Step 2: Titles.** `ConfirmationPromptView` and `ErrorMessageView`: `Title="{Binding Title, StringFormat='  {0}'}"`; `SaveChangesPromptView`:
  `Title="  Zoo Tycoon Launcher"`.
- [x] **Step 3: SDD.** §9.7 *Diff viewer*: add that cells show values as the editor does (decision 1, including the inverted-key rule) and that the snapshot grid's
  headers are inert; §9.3: add a sentence after the `[UI]` sub-header paragraph covering the cash display (decision 5) and the read-only language codes (decision 4).
  Bump to revision **1.11** (Document control *Version* and *Last updated* 21 October 2026, and a revision-history row summarising milestone 9.2).
- [x] **Step 4: conventions.md.** Under §4.1, after the icon table, add: every `ClassicWindow` title starts with two spaces so the text clears the title-bar icon
  (Classic.Avalonia leaves no gap), including titles bound from a view model.
- [x] **Step 5: Build and test.** `dotnet build Erdmier.ZooTycoonLauncher.slnx` (0 warnings), then every test project → pass. Markdown lines touched ≤ 180.
- [x] **Step 6: Commit** — `style(🎨): make the history headers inert and space the prompt titles; document milestone 9.2`

---

## Task 5: Check it in the running app (controller)

Run by the controller, which has desktop control; implementers do not.

- [x] **Step 1: Drive** the launcher against a sample installation: the `[UI]` cash rows read `$…` and accept `250000`; the `[language]` codes are grey with no
  spinner and change only through the picker; the Restore dialogue's diff reads `Fullscreen` / `true` / `$…`, its history headers ignore clicks; the Restore
  confirmation's title clears its icon. Close the launcher afterwards.
- [x] **Step 2: Refresh** `Docs/UserInterfaceDesign/ImplementationScreenshots/RestorePreviousIniDialogue.png` if the diff changed visibly.

## Verification

- [ ] Clean build with 0 warnings; every test project passes.
- [ ] Task 5 checked in the running app, or recorded in the ledger as left to the maintainer.
