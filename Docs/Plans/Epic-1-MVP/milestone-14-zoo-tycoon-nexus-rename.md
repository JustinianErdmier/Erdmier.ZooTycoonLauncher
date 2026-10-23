# Zoo Tycoon Nexus Rename Implementation Plan

**Date:** 2026-10-23

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename the application from *Zoo Tycoon Launcher* to **Zoo Tycoon Nexus**: the solution, every project and namespace, the window titles, the storage folder and
file names, and the current documents.

**Architecture:** A rename with one behaviour change. The code prefix `Erdmier.ZooTycoonLauncher` becomes `Erdmier.ZooTycoonNexus` everywhere it names a project, an
assembly or a namespace. The app keeps its data under `%LOCALAPPDATA%\ZooTycoonNexus\` instead of `ZooTycoonLauncher\`, and starts fresh there; it never reads or moves
the old folder (the maintainer moves the files by hand). The app is still *a* launcher, so "launcher" as an ordinary noun and the `Launcher*` role names in code stay.

**Tech Stack:** .NET 10, Avalonia 11.3 + Classic.Avalonia, EF Core + SQLite, xUnit + `Avalonia.Headless.XUnit`, Shouldly, `System.IO.Abstractions.TestingHelpers`.

**Spec:** none. The maintainer agreed the scope in chat on 2026-10-23 (summarised under *Global Constraints*).

## Global Constraints

- Product name, exactly: `Zoo Tycoon Nexus`. Code prefix, exactly: `Erdmier.ZooTycoonNexus` (projects become `Erdmier.ZooTycoonNexus.Domain`, `.Application`,
  `.Infrastructure`, `.Desktop`, and the test projects `Erdmier.ZooTycoonNexus.<Layer>.Tests.Unit`, `.Infrastructure.Tests.Integration`, `.Tests.Architecture`).
  Solution: `Erdmier.ZooTycoonNexus.slnx`.
- Storage, exactly: `%LOCALAPPDATA%\ZooTycoonNexus\` with `Data\Nexus.db`, `Logs\Nexus.log`; the per-installation names stay `Data\{installationId}.db` and
  `Logs\Installations\{installationId}.log`.
- **No data migration.** Nothing reads, copies, moves or mentions the old `%LOCALAPPDATA%\ZooTycoonLauncher\` folder at run time.
- **Keep** "launcher" as an ordinary noun in prose and UI copy ("the launcher opens", `Close launcher after the game starts`), and the `Launcher*` role names in code:
  `LauncherDbContext`, `LauncherSettings`, `LauncherOptions`, `ILauncherSettingsRepository`, `LauncherDatabasePath`, `LauncherLogPath`, the `Persistence/Launcher/`
  folder, and every EF migration's class name and ID (`InitialLauncherSchema`, `RemoveLauncherTheme`, …). Migration IDs must not change.
- Window titles keep their two-space padding: the main window reads `  Zoo Tycoon Nexus  V1.0.0`; the save-changes prompt reads `  Zoo Tycoon Nexus`.
- The repo is already `JustinianErdmier/Erdmier.ZooTycoonNexus`. The **local checkout folder** `D:\Source\Personal\Erdmier.ZooTycoonLauncher` keeps its name (the
  maintainer renames it after the merge).
- **Leave untouched:** milestone 1–13 plans, specs and ledgers (except the link-path repair in Task 3), the SDD revision rows before 1.18, everything under
  `Docs/UserInterfaceDesign/HiFiPrototype/`, `HiFiMockupScreenshots/` and `ZooTycoonLauncherHiFiUIPrototype.gif` (a snapshot of the Claude Design project), and
  `SampleInstallations/`.
- British English; Markdown hard-wrapped at 180 characters (measure with PowerShell `[string].Length`); conventional commits with gitmoji.

## Review Focus

- **A vacuous architecture test.** `OneTypePerFileTests` filters types with `Namespace.StartsWith("Erdmier.ZooTycoonLauncher")` and `ProjectPaths` uses the prefix
  `Erdmier.ZooTycoonLauncher.`. If either keeps the old prefix, the tests scan nothing and pass. Task 1 step 6 checks that they still find types.
- **Stale build output.** Untracked `bin/` and `obj/` folders hold old assembly names and `project.assets.json` paths. Task 1 deletes them before the first build.
- **Fresh start.** On first run the app creates `%LOCALAPPDATA%\ZooTycoonNexus\Data\Nexus.db`, applies every migration and boots as on a new machine. Task 2's
  `AppStorageLocationsTests` pins the paths; the existing `LauncherDbContextTests` apply the migrations; Task 2 step 7 runs the app.
- **XAML still compiles and loads.** `x:Class`, `xmlns:…="using:Erdmier.ZooTycoon…"` and `avares://` URIs carry the assembly name. `ViewLoadTests` loads every view.
- **Rider.** The `.idea/.idea.Erdmier.ZooTycoonLauncher/` folder follows the solution name. Rider must be closed during Task 1 and reopened on the new `.slnx`.

---

### Task 1: Rename the solution, projects and namespaces

**Precondition (maintainer):** Rider is closed for the whole task. The controller asks for this before dispatching Task 1.

**Files:**

- Rename: `Erdmier.ZooTycoonLauncher.slnx` → `Erdmier.ZooTycoonNexus.slnx` (and every project path inside it)
- Rename: each folder and `.csproj` under `Source/` and `Tests/` from `Erdmier.ZooTycoonLauncher.*` to `Erdmier.ZooTycoonNexus.*`
- Rename: `.idea/.idea.Erdmier.ZooTycoonLauncher/` → `.idea/.idea.Erdmier.ZooTycoonNexus/`, and update the project paths in its `.idea/avalonia.xml`
- Modify: every `.cs`, `.axaml`, `.csproj`, `app.manifest` and `.slnx` that contains `Erdmier.ZooTycoonLauncher` (namespaces, `using`s, `GlobalUsings.cs`, `x:Class`,
  `xmlns`, `avares://` URIs, `<RootNamespace>`, `<AssemblyName>`, `<ProjectReference>`, `assemblyIdentity`, migration `.Designer.cs` / `ModelSnapshot` namespaces)
- Modify: `Tests/…Tests.Architecture/Common/ProjectPaths.cs` (`Prefix`) and `OneTypePerFileTests.cs` (`StartsWith`)
- Create: `Docs/Ledgers/Epic-1-MVP/milestone-14-ledger.md`, registered in the `.slnx` under `/Docs/Ledgers/Epic-1-MVP/` after `milestone-13-ledger.md`

**Interfaces:**

- Produces: the new project paths (`Source/Erdmier.ZooTycoonNexus.<Layer>/Erdmier.ZooTycoonNexus.<Layer>.csproj`) and namespaces every later task uses.

- [ ] **Step 1: Open the ledger.** Create `milestone-14-ledger.md` in the shape of `milestone-13-ledger.md` (title, `**Date:**`, plan link, branch
  `feat/milestone-14-zoo-tycoon-nexus-rename`). Rulings go in as they are taken.

- [ ] **Step 2: Clear build output.** Delete every `bin/` and `obj/` folder under `Source/` and `Tests/` (untracked; they hold the old names).

- [ ] **Step 3: Rename folders and files.** Rename each project folder and its `.csproj`, the `.slnx` and the `.idea` folder, with `git mv` so history follows. Only
  `Erdmier.ZooTycoonLauncher` changes in each name.

- [ ] **Step 4: Replace the prefix in file contents.** Replace `Erdmier.ZooTycoonLauncher` with `Erdmier.ZooTycoonNexus` in every tracked file under `Source/`, `Tests/`,
  `.idea/` and in the `.slnx` (project entries only; its `Docs/` entries keep their paths until Task 3). Write files back as UTF-8 **without** adding a BOM where there
  was none, and never through a double-quoted PowerShell string (backticks in content would be expanded). Do not touch `Docs/`, `CLAUDE.md` or `SampleInstallations/`.

- [ ] **Step 5: Verify no old prefix remains in code.**
  Run: `git grep -n "Erdmier.ZooTycoonLauncher" -- Source Tests .idea Erdmier.ZooTycoonNexus.slnx`
  Expected: no output. (A plain `ZooTycoonLauncher` still appears in the storage folder name in `AppStorageLocations.cs` and in XML docs that name
  `%LOCALAPPDATA%\ZooTycoonLauncher\`; Task 2 changes those, test-first.)

- [ ] **Step 6: Build and test everything.**
  Run: `dotnet build Erdmier.ZooTycoonNexus.slnx` then `dotnet test Erdmier.ZooTycoonNexus.slnx`.
  Expected: 0 warnings, 0 errors; Domain 140, Application 205, Infrastructure integration (all), Desktop 373 and Architecture 27 pass — the same counts as on `main`.
  Then prove the architecture tests are not vacuous: temporarily add a second public type to any Domain file, run `Tests.Architecture`, see `OneTypePerFileTests` fail,
  and revert the change (do not commit it). Record the failing output in the report.

- [ ] **Step 7: Commit.** Stage the renames and edits (`git add` the new paths and the deletions of the old ones; never `git add -A` across the repo).
  Message: `refactor(♻️): rename the solution and projects to Erdmier.ZooTycoonNexus`

---

### Task 2: Name the app Zoo Tycoon Nexus and move its storage

**Files:**

- Create: `Tests/Erdmier.ZooTycoonNexus.Infrastructure.Tests.Integration/Storage/AppStorageLocationsTests.cs`
- Create: `Tests/Erdmier.ZooTycoonNexus.Desktop.Tests.Unit/Views/ProductNameTests.cs`
- Modify: `Source/Erdmier.ZooTycoonNexus.Infrastructure/Common/Storage/AppStorageLocations.cs` (`AppFolderName`, the two file names, its XML summary)
- Modify: `Source/Erdmier.ZooTycoonNexus.Desktop/Views/MainWindow.axaml:10`, `Views/Dialogs/SaveChangesPromptView.axaml:11` (titles)
- Modify: `Source/Erdmier.ZooTycoonNexus.Desktop/ViewModels/Boot/LookingForZooTycoonViewModel.cs:33` (`"Loading Launcher.db…"` → `"Loading Nexus.db…"`)
- Modify: every XML doc comment that names `Launcher.db` or `%LOCALAPPDATA%\ZooTycoonLauncher\` (find them with
  `git grep -n "Launcher\.db\|Launcher\.log\|ZooTycoonLauncher" -- Source Tests`)

**Interfaces:**

- Consumes: Task 1's project paths and namespaces.
- Produces: `IAppStorageLocations` unchanged in shape; `LauncherDatabasePath` now ends `\ZooTycoonNexus\Data\Nexus.db`, `LauncherLogPath` ends `\ZooTycoonNexus\Logs\Nexus.log`.

- [ ] **Step 1: Write the failing tests.**

`AppStorageLocationsTests` (`[ Fact ]`, constructs `new AppStorageLocations(new MockFileSystem())`):

```csharp
locations.AppDataRoot.ShouldBe(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZooTycoonNexus"));
locations.LauncherDatabasePath.ShouldBe(Path.Combine(locations.AppDataRoot, "Data", "Nexus.db"));
locations.LauncherLogPath.ShouldBe(Path.Combine(locations.AppDataRoot, "Logs", "Nexus.log"));
locations.InstallationDatabasePath(id).ShouldBe(Path.Combine(locations.AppDataRoot, "Data", $"{id}.db"));
locations.InstallationLogPath(id).ShouldBe(Path.Combine(locations.AppDataRoot, "Logs", "Installations", $"{id}.log"));
```

  Name it `Paths_LiveUnderTheZooTycoonNexusFolder`. Add `Construction_CreatesTheFoldersInTheGivenFileSystemOnly`: the mock file system holds the `Data`, `Logs` and
  `Logs\Installations` directories under `AppDataRoot`, and no path under it contains `ZooTycoonLauncher`.

`ProductNameTests` (`[ AvaloniaFact ]`, headless, set up like `MainWindowStyleMenuTests`): `MainWindow_Title_NamesZooTycoonNexus` asserts
`window.Title.ShouldBe("  Zoo Tycoon Nexus  V1.0.0")`; `SaveChangesPrompt_Title_NamesZooTycoonNexus` asserts the prompt view's `Title` is `"  Zoo Tycoon Nexus"`
(build it with its designer view model). `LookingForZooTycoonViewModel`'s status text is covered by a third fact asserting its messages contain `"Loading Nexus.db…"`
and none contains `"Launcher.db"` — use whatever public surface exposes those messages today; if none does, drop this fact and note it in the report.

- [ ] **Step 2: Run them and confirm they fail.**
  Run: `dotnet test Tests/Erdmier.ZooTycoonNexus.Infrastructure.Tests.Integration --filter "FullyQualifiedName~AppStorageLocationsTests"` and
  `dotnet test Tests/Erdmier.ZooTycoonNexus.Desktop.Tests.Unit --filter "FullyQualifiedName~ProductNameTests"`.
  Expected: failures showing `ZooTycoonLauncher`, `Launcher.db` / `Launcher.log` and `Zoo Tycoon Launcher`.

- [ ] **Step 3: Make the changes.** `AppFolderName = "ZooTycoonNexus"`; file names `"Nexus.db"` and `"Nexus.log"`; the two titles; the loading text; every XML doc that
  names the storage folder or `Launcher.db` / `Launcher.log` now names `ZooTycoonNexus` / `Nexus.db` / `Nexus.log`. Keep the property names (`LauncherDatabasePath`,
  `LauncherLogPath`).

- [ ] **Step 4: Run the tests and confirm they pass.** The same two commands; then the whole `Infrastructure.Tests.Integration` and `Desktop.Tests.Unit` projects.
  Expected: all green, 0 warnings.

- [ ] **Step 5: Check nothing still names the old product or files in code.**
  Run: `git grep -n -i "Zoo Tycoon Launcher\|ZooTycoonLauncher\|Launcher\.db\|Launcher\.log" -- Source Tests`
  Expected: no output.

- [ ] **Step 6: Commit.** Message: `feat(✨): name the app Zoo Tycoon Nexus and keep its data under ZooTycoonNexus`

- [ ] **Step 7: Check the running app.** Launch `Source/Erdmier.ZooTycoonNexus.Desktop` (it starts fresh, as on a new machine). Capture the window — computer use cannot
  resolve this app by name, so capture it through user32 (`GetWindowRect` + `Graphics.CopyFromScreen`) — and confirm the title bar reads *Zoo Tycoon Nexus V1.0.0*.
  Confirm a `ZooTycoonNexus\Data\Nexus.db` and `ZooTycoonNexus\Logs\Nexus.log` now exist (a shell-launched app may write under the Claude package's redirected
  `LocalCache` rather than the real `%LOCALAPPDATA%`; look in both and report where). Close the app. Record the outcome in the ledger.

---

### Task 3: Bring the current documents up to date and close the milestone

**Files:**

- Rename: `Docs/Specs/1-zoo-tycoon-launcher-master-design.md` → `Docs/Specs/1-zoo-tycoon-nexus-master-design.md`
- Modify: the SDD; `CLAUDE.md`; `Docs/UserInterfaceDesign/conventions.md`; `Docs/TestPlans/manual-end-to-end-tests.md`; `Erdmier.ZooTycoonNexus.slnx` (the SDD's entry)
- Modify (link paths only): every milestone 1–13 plan or spec that links `1-zoo-tycoon-launcher-master-design.md`
- Modify: `Docs/Ledgers/Epic-1-MVP/milestone-14-ledger.md`, this plan (ticks)

**Interfaces:** none.

- [ ] **Step 1: Rename the SDD** with `git mv`, update its `.slnx` entry, and repair every link to it: `git grep -l "1-zoo-tycoon-launcher-master-design"` lists them.
  In milestone 1–13 documents change only the link path, never the prose around it; record that as a ruling in the ledger.

- [ ] **Step 2: SDD content.** Product name `Zoo Tycoon Launcher` → `Zoo Tycoon Nexus` (title line 1, the metadata table, §1, §2, §9.1 title bar
  `Zoo Tycoon Nexus V<version>`, §9.9 `About Zoo Tycoon Nexus`, and every other place it names the product); `Erdmier.ZooTycoonLauncher` → `Erdmier.ZooTycoonNexus` in
  project, namespace and path references; storage `%LOCALAPPDATA%\ZooTycoonLauncher\` → `%LOCALAPPDATA%\ZooTycoonNexus\`, `Launcher.db` → `Nexus.db`, `Launcher.log` →
  `Nexus.log` (§4.7, §6, and elsewhere). Rewrite the §1 naming paragraph (about line 78) so it says, in this sense: *Zoo Tycoon Nexus. The application is a launcher in the
  established game-launcher sense, but its name is* Zoo Tycoon Nexus*; "launcher" stays the ordinary word for what it is, and code that names that role
  (`LauncherDbContext`, `LauncherSettings`, `LauncherOptions`) keeps it.* Keep revision rows before 1.18 and the `Ref` prototype's name (`Erdmier.ZooTycoonLauncherRef`)
  as they are.

- [ ] **Step 3: SDD revision row.** Add **1.18**, `23 October 2026`, `Justinian`: milestone 14 renames the application Zoo Tycoon Nexus — solution, projects and
  namespaces `Erdmier.ZooTycoonNexus.*`, storage `%LOCALAPPDATA%\ZooTycoonNexus\` with `Nexus.db` / `Nexus.log` and no migration of the old folder — and moves this
  document to `1-zoo-tycoon-nexus-master-design.md`. List the sections changed.

- [ ] **Step 4: CLAUDE.md.** The project description names Zoo Tycoon Nexus; every `Erdmier.ZooTycoonLauncher` project, solution and command path becomes
  `Erdmier.ZooTycoonNexus`; the SDD link follows the rename; storage paths and log paths use `ZooTycoonNexus` / `Nexus.db`. Keep `Erdmier.ZooTycoonLauncherRef` (a
  historical name), the GIF's file name, and any wording about the local checkout folder.

- [ ] **Step 5: conventions.md and the manual test plan.** Project paths, `xmlns` examples and the SDD link follow the rename; `Zoo Tycoon Launcher` as a window title
  example (conventions.md about line 334) becomes `Zoo Tycoon Nexus`; any storage path or `Launcher.db` mention in the test plan becomes `ZooTycoonNexus` / `Nexus.db`.
  Keep `ZooTycoonLauncherHiFiUIPrototype.gif` and every mock-up description.

- [ ] **Step 6: Verify.** Run:

```bash
git grep -n -i "Zoo Tycoon Launcher\|ZooTycoonLauncher\|Launcher\.db\|Launcher\.log\|zoo-tycoon-launcher" -- CLAUDE.md Docs/Specs/1-zoo-tycoon-nexus-master-design.md Docs/UserInterfaceDesign/conventions.md Docs/TestPlans Erdmier.ZooTycoonNexus.slnx
```

  Expected: only revision rows before 1.18, `ZooTycoonLauncherRef`, `ZooTycoonLauncherHiFiUIPrototype.gif`, and the 1.18 row itself. Then
  `git grep -n "1-zoo-tycoon-launcher-master-design"` returns nothing. Check the wrap of every edited Markdown line (≤ 180).

- [ ] **Step 7: Commit.** Message: `docs(📝): rename the app to Zoo Tycoon Nexus in the SDD, CLAUDE.md and current docs`

- [ ] **Step 8: Close the ledger.** Clean it into its final shape (rulings with costs, plan defects, outstanding items grouped by when to revisit — including, for the
  maintainer after the merge: move and rename the old data files, rename the local checkout folder, reopen Rider on `Erdmier.ZooTycoonNexus.slnx` — and incidents), tick
  this plan's boxes, and commit. Message: `docs(📝): close the milestone 14 ledger and plan`
