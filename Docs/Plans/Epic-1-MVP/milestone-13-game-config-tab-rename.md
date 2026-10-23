# Game Config Tab Rename Implementation Plan

**Date:** 2026-10-23

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename the main window's **INI Config** tab to **Game Config**, so the launcher stops showing users a file-format name, and update every reference to the tab
to match.

**Architecture:** A wording change only. The tab is still a view of `zoo.ini` and never holds anything else. Code identifiers, folders and namespaces (`IniConfigTabViewModel`,
`Views/IniConfig/`, the `IniConfig` slice in Application) keep their names on purpose, because they accurately describe code that edits an INI file. The SDD records that
choice so nobody "fixes" the mismatch later.

**Tech Stack:** Avalonia 11.3 + Classic.Avalonia, xUnit + `Avalonia.Headless.XUnit`, Shouldly.

**Spec:** none. The maintainer agreed the scope in chat on 2026-10-23 (summarised under *Global Constraints*). Milestones 1, 2 and 5 also have no spec.

## Global Constraints

- The new tab name is exactly `Game Config` (capital G, capital C, one space).
- **Rename** every piece of prose that names the tab or its editor: UI copy, XML doc comments, `//` comments, log messages, test comments, and the current docs (SDD,
  `conventions.md`, the manual test plan). "the INI Config tab" → "the Game Config tab"; "the INI Config editor" → "the Game Config editor".
- **Keep** every code identifier, type, file, folder and namespace containing `IniConfig` (types, test names such as `GeneralTab_MissingOnlyIni_PointsAtTheIniTab`,
  `IniConfigTabIndex`, `GetIniConfigQuery`). No `rename_refactoring`, no file moves.
- **Keep** text that names the file or its format: `zoo.ini`, "INI section", "INI status", the *INI style* radio and menu item (it names the layout style), the
  `INI loaded` status text, "Restore Previous INI", "Fix INI", "No INI".
- **Options group box:** the Options dialogue's *INI editor* group box becomes exactly `Config editor`, in the XAML and wherever the current docs name that group box.
- **Keep** names of the code slice and of past milestones: "INI Config slice", "INI Config core (slice/spec)", "INI Config Core" (the manual test plan's section title for
  that milestone), and the SDD's `formerly "Historical INI Config"` note.
- **Leave untouched:** every past milestone plan, spec and ledger (milestones 1–12), the SDD revision-history rows before 1.17, the SDD deferred-work table's *Source*
  column, and everything under `Docs/UserInterfaceDesign/HiFiPrototype/` plus the GIF and `HiFiMockupScreenshots/` (a snapshot of the Claude Design project).
- **View menu:** `View → Config layout` stays exactly as it is. Do not add "Game" to it.
- British English; Markdown hard-wrapped at 180 characters (measure with PowerShell `[string].Length`); conventional commits with gitmoji.

## Review Focus

- **Leftover user-visible "INI Config".** Users should never see the old name. Task 1's headless tests pin the two XAML strings, and the existing General tab test pins the
  Cannot Play message; Task 2's grep pins the rest of `Source/`.
- **Over-renaming.** A reviewer should reject any change to an identifier, file name, or file-naming text listed under *Keep*. Task 2's grep shows the allowed survivors.
- **Tab index.** `PlayViewModel.IniConfigTabIndex` and `SelectedTabIndex` must keep working; changing only the header text cannot break them, and the existing
  `PlayViewModelTests` stay green.
- **Access key in the SDD.** §9.1 lists the tab as Alt-I, but no access key is wired up in the code. With General already on G, the SDD changes to Alt-C (the
  `C` of *Config*). This is a ruling, recorded in the ledger; nothing in the code changes.
- **Header width.** "Game Config" is one character longer than "INI Config" inside a fixed 720 px window. Check it in the running app (Task 1, step 6).

---

### Task 1: Rename the tab in the UI copy

**Files:**

- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/PlayView.axaml:10` (`<TabItem Header="INI Config">`)
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/OptionsDialogView.axaml:49` (`<controls:GroupBox Header="INI editor"`) and `:54`
  (`Text="Show settings in the INI Config tab:"`)
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/GeneralTabViewModel.cs:123` (the `MissingIniMessage` sentence)
- Create: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Views/GameConfigTabWordingTests.cs`
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Tabs/GeneralTabViewModelTests.cs:56`
- Create: `Docs/Ledgers/Epic-1-MVP/milestone-13-ledger.md`, and register it in `Erdmier.ZooTycoonLauncher.slnx` under `/Docs/Ledgers/Epic-1-MVP/` after
  `milestone-12-ledger.md`

**Interfaces:**

- Consumes: the designer (parameterless) constructors of `PlayViewModel` and `OptionsDialogViewModel`.
- Produces: nothing later tasks call.

- [x] **Step 1: Open the ledger.** Create `milestone-13-ledger.md` in the shape of `milestone-12-ledger.md` (title, `**Date:**`, plan link, branch
  `feat/milestone-13-game-config-tab-rename`), with ruling **R1 — Alt-C in the SDD** pre-filled from *Review Focus*. Register it in the `.slnx`.

- [x] **Step 2: Write the failing tests.** `GameConfigTabWordingTests` (`[ AvaloniaFact ]`, headless, following the setup in `MainWindowStyleMenuTests`) shows each view
  in a `Window` with its designer view model and runs `Dispatcher.UIThread.RunJobs()`:

```csharp
[ AvaloniaFact ]
public void PlayView_TabHeaders_ReadGeneralThenGameConfig()
{
    // PlayView in a Window, DataContext = new PlayViewModel()
    string?[] headers = view.GetLogicalDescendants().OfType<TabItem>().Select(tab => tab.Header as string).ToArray();

    headers.ShouldBe([ "General", "Game Config" ]);
}

[ AvaloniaFact ]
public void OptionsDialog_EditorStyleLine_NamesTheGameConfigTab()
{
    // OptionsDialogView in a Window, DataContext = new OptionsDialogViewModel()
    IEnumerable<string?> texts = view.GetLogicalDescendants().OfType<TextBlock>().Select(block => block.Text);

    texts.ShouldContain("Show settings in the Game Config tab:");
    texts.ShouldNotContain(text => text != null && text.Contains("INI Config"));
}

[ AvaloniaFact ]
public void OptionsDialog_EditorStyleGroup_IsHeadedConfigEditor()
{
    // OptionsDialogView in a Window, DataContext = new OptionsDialogViewModel()
    IEnumerable<object?> headers = view.GetLogicalDescendants().OfType<GroupBox>().Select(box => box.Header);

    headers.ShouldContain("Config editor");
    headers.ShouldNotContain("INI editor");
}
```

  In `GeneralTabViewModelTests.GeneralTab_MissingOnlyIni_PointsAtTheIniTab`, change the expected text to `"Use the Game Config tab to create or locate zoo.ini."`
  (keep the test's name).

- [x] **Step 3: Run the tests and confirm they fail.**
  Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit --filter "FullyQualifiedName~GameConfigTabWordingTests|FullyQualifiedName~GeneralTabViewModelTests"`
  Expected: four failures, each showing the old text (`INI Config` / `INI editor`) still in place.

- [x] **Step 4: Change the four strings.** The tab header becomes `Header="Game Config"`. The Options group box becomes `Header="Config editor"` and its line becomes
  `Show settings in the Game Config tab:`. The message ends `Use the Game Config tab to create or locate zoo.ini.` Nothing else in these files changes in this task.

- [x] **Step 5: Run the tests and confirm they pass.** Same command as step 3. Expected: all pass. Then run the whole `Desktop.Tests.Unit` project (it includes
  `ViewLoadTests` and `PlayViewModelTests`). Expected: all green.

- [x] **Step 6: Check the running app.** Launch the Desktop project against `SampleInstallations/Zoo Tycoon` and open an installation. Screenshot the tab strip (it should
  read *General | Game Config*, with the header on one line and not clipped). Screenshot `File → Options…` (a *Config editor* group box saying "Show settings in the Game
  Config tab:"). Close the app.

- [x] **Step 7: Commit.**

```bash
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/PlayView.axaml Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/OptionsDialogView.axaml Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/GeneralTabViewModel.cs Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Views/GameConfigTabWordingTests.cs Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Tabs/GeneralTabViewModelTests.cs Docs/Ledgers/Epic-1-MVP/milestone-13-ledger.md Erdmier.ZooTycoonLauncher.slnx
git commit -m "feat(✨): rename the INI Config tab to Game Config"
```

---

### Task 2: Rename the tab in code comments, XML docs and log messages

**Files (each line that names the tab or its editor; the grep in step 1 is the authoritative list):**

- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/IniConfigTabViewModel.cs` (XML summary; the four log messages at lines 163, 188, 223, 228)
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/PlayViewModel.cs`, `ViewModels/MainWindowViewModel.cs`, `ViewModels/Tabs/GeneralTabViewModel.cs`
  (lines 72, 120), `ViewModels/IniConfig/IniEditorViewModel.cs`, `ViewModels/IniConfig/IniPlaceholderViewModel.cs`, `ViewModels/Dialogs/OptionsDialogViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/IniConfigTabView.axaml.cs`, `Views/IniConfig/IniPlaceholderView.axaml.cs`,
  `Models/IniConfig/IniEditorCatalogue.cs`, `Models/IniConfig/IniEditorFriendlyCatalogue.cs`, `Models/IniConfig/IniDisplayText.cs`, `Composition/IIniEditorStyleState.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Domain/IniKeys/IniKeyRole.cs`, `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Get/GetIniConfigQuery.cs`
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Boot/PlayTestData.cs:30` (comment)

**Interfaces:** none. Comment, doc and log text only, so no compiled behaviour changes.

- [x] **Step 1: List the hits.** Run: `rg -n "INI Config" Source Tests --glob "!**/bin/**" --glob "!**/obj/**"`

- [x] **Step 2: Rewrite each hit that names the tab or its editor** to say *Game Config*, following *Global Constraints*. Leave the two "INI Config slice" mentions
  (`Application/Installations/Add/AddInstallationCommand.cs`, `Application/IniConfig/Common/IniErrors.cs`) alone: they name the code slice. Keep each edited line within the
  file's existing width.

- [x] **Step 3: Verify only the allowed survivors remain.** Run the step 1 command again. Expected: exactly two lines, both containing `INI Config slice`.

- [x] **Step 4: Build and test the touched projects.** Run `dotnet build` and then `dotnet test` on `Domain`, `Application` and `Desktop` and their unit-test projects, plus
  `Tests.Architecture`. Expected: no new warnings, all green.

- [x] **Step 5: Commit.** Stage only the files changed in step 2.
  Message: `docs(📝): call the tab Game Config in comments, docs and logs`

---

### Task 3: Bring the current documents up to date and close the milestone

**Files:**

- Modify: `Docs/Specs/1-zoo-tycoon-nexus-master-design.md`
- Modify: `Docs/UserInterfaceDesign/conventions.md:349` (the status-bar table's `(INI Config)` → `(Game Config)`)
- Modify: `Docs/TestPlans/manual-end-to-end-tests.md`
- Modify: `Docs/Ledgers/Epic-1-MVP/milestone-13-ledger.md`, this plan (ticks)

**Interfaces:** none.

- [x] **Step 1: SDD present-tense references.** Rename every reference to the tab outside the revision history and the deferred-work *Source* column, following *Global
  Constraints*. At the time of writing that means lines 140, 176, 184, 391 (the inline code comment), the §7.3 / §7.4 / §7.5 headings (919, 983, 1012), 1007, 1238, 1265,
  1267, 1282 (the quoted message must match Task 1 exactly), 1376 (§9.3 heading), 1381, 1404 (§9.3.1 heading), 1569 (the quoted Options line must match Task 1 exactly),
  1628, 1844, and DW-04's description (1896). On 1569 the group box's bold name becomes **Config editor**, and on 1725 *INI editor* group becomes *Config editor*
  group; 1721's "INI editor style" names the setting, not the group box, so it stays. On line 1267, change `(Alt-I)` to `(Alt-C)`. Keep line 637 ("INI Config core
  slice") and 1533 (the "formerly" note). Update any `§` anchor links whose heading text changed.

- [x] **Step 2: SDD naming note.** At the start of §9.3, add a short paragraph in this sense: *The tab is labelled* Game Config *for players, but its code keeps the*
  `IniConfig` *names (`IniConfigTabViewModel`, `Views/IniConfig/`, the Application `IniConfig` slice) on purpose: the tab only ever shows `zoo.ini`, so the code names
  describe it accurately. Do not rename them to match the label.*

- [x] **Step 3: SDD revision row.** Add version **1.17**, `23 October 2026`, `Justinian`: milestone 13 renames the INI Config tab to Game Config (§7.3–§7.5, §9.1, §9.3,
  §9.8 Options copy, whose group box becomes *Config editor*), moves its documented access key to Alt-C, and records in §9.3 why code identifiers keep `IniConfig`.

- [x] **Step 4: Manual test plan.** Rename references to the tab in instructions, expected results and run notes, including the §9 heading at line 1493 and the step under
  it, where the *INI editor* group box also becomes *Config editor*. Keep the `# INI Config Core` section title (it names the milestone), and keep any sentence describing what the
  *mock-up* shows, because the prototype still says INI Config (for example line 61's "the mockup renders … the General / INI Config tab strip"). Where a sentence compares the two,
  name each one correctly.

- [x] **Step 5: Verify.** Run:

```bash
rg -n "INI Config" Docs/Specs/1-zoo-tycoon-nexus-master-design.md Docs/UserInterfaceDesign/conventions.md Docs/TestPlans/manual-end-to-end-tests.md
```

  Check by eye that every survivor is a revision-history row, a *Source* cell, a slice or milestone name, the "formerly" note, or a mock-up description. Check the
  wrap of every edited Markdown line with PowerShell `[string].Length` (≤ 180).

- [x] **Step 6: Commit.** Message: `docs(📝): rename the tab to Game Config in the SDD, conventions and test plan`

- [x] **Step 7: Close the ledger.** Clean the ledger into its final shape (rulings, plan defects, outstanding items, incidents), tick this plan's boxes, and commit.
  Message: `docs(📝): close the milestone 13 ledger and plan`
