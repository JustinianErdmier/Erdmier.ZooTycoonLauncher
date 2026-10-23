# Milestone 14 — Zoo Tycoon Nexus Rename: Ledger

**Date:** 2026-10-23

**Plan:** [`milestone-14-zoo-tycoon-nexus-rename.md`](../../Plans/Epic-1-MVP/milestone-14-zoo-tycoon-nexus-rename.md) · **Branch:** `feat/milestone-14-zoo-tycoon-nexus-rename`

The product is renamed **Zoo Tycoon Nexus**. The solution, every project and every namespace moved from `Erdmier.ZooTycoonLauncher.*` to `Erdmier.ZooTycoonNexus.*`; the
storage folder, the database and log names and the window titles followed, and the current documents were brought up to date. Task 1 renamed the code, Task 2 renamed the
storage and the user-facing name, and Task 3 updated the documents and closed this ledger.

## Rulings taken without the maintainer

- **R1:** subagent commits carry the implementer model's own `Co-Authored-By` trailer (Task 1's says Claude Sonnet 5.5, not Opus 5.5). Accepted, no history rewrite. Cost if wrong:
  cosmetic attribution.
- **R2:** the plain `ZooTycoonLauncher` storage-name survivors (the storage folder, the two design-time temp DB names and the Rider `dataSources.xml` URL) were left by Task 1 for
  Task 2. Done in Task 2. Cost if wrong: none.
- **R3 (Task 2):** the planned third `ProductNameTests` fact (the loading phrases) was dropped. `LookingForZooTycoonViewModel.ProgressPhrases` is private and only `CurrentPhrase`
  is public, advanced by a private timer tick, so no public surface exposes the messages. The text change (`Loading Nexus.db…`) is covered by the repo grep instead. Cost if
  wrong: one string without a test.
- **R4 (Task 2):** the Rider data source's display name was renamed `Launcher.db` → `Nexus.db` alongside its jdbc URL. Cost if wrong: none (the user's own IDE setting; the data
  source UUID is unchanged).
- **R5 (Task 2 fix round):** the design-time environment variables carried the old product name, so `ZOOLAUNCHER_DESIGNTIME_DB` and `ZOOLAUNCHER_INSTALLATION_DESIGNTIME_DB` became
  `ZOONEXUS_DESIGNTIME_DB` and `ZOONEXUS_INSTALLATION_DESIGNTIME_DB`. Cost if wrong: anyone with the old variable set locally must rename it.
- **R6 (Task 3):** the link-path repair in the milestone 1–13 plans and specs changed only the link target, `1-zoo-tycoon-launcher-master-design.md` →
  `1-zoo-tycoon-nexus-master-design.md`, and nothing else in those files, so the past documents still read as written and their links resolve. Cost if wrong: none; the
  alternative was 20 dead links.
- **R7 (Task 3):** the SDD, CLAUDE.md and the other current documents say `Nexus.log` where they said `Launcher.log`, keeping the SDD's existing "rolling daily" wording. The
  file on disk is `Nexus<yyyyMMdd>.log` (see the plan defects below). Cost if wrong: none.
- **R8 (Task 3):** the SDD's version field moved from the stale 1.16 straight to 1.18 (revision 1.17 never updated it) and its last-updated date to 23 October 2026. Cost if
  wrong: a version number.
- **R9 (Task 3):** the SDD's note that the Ref build kept its config at `%APPDATA%\ZooTycoonLauncher\launcher.config` was left as it is. It names the prototype's real, historical
  folder, so renaming it would make the sentence false. Cost if wrong: none.

## Task 1 outcome

Commit `9952116`. Every project, folder, `.csproj`, namespace and the `.slnx` / `.idea` folder was renamed with history kept (git sees 590 renames). Test counts: Domain 140,
Application 205, Infrastructure integration 62, Desktop 373, Architecture 27, with 0 warnings. The `OneTypePerFileTests` mutation proof: a planted second type failed the test,
then was reverted. The controller's and reviewer's reverse-rename check found that the ledger's `.slnx` entry was the only non-mechanical change.

## Task 2 outcome

Commit `a637a8d`. Tests written first and seen failing (old folder, `Launcher.db`/`Launcher.log`, `Zoo Tycoon Launcher` titles), then green: Infrastructure integration 64,
Desktop unit 375, Architecture 27, Application unit 205, Domain unit 140, all passing with 0 build warnings. The `git grep` for the old product name, folder and file names
over `Source`, `Tests` and `.idea` has one hit: the negative assertion in `AppStorageLocationsTests` (see the plan defects below).

Step 7 app check: the built Desktop exe was started with `Start-Process` and captured through user32. The title bar reads `Zoo Tycoon Nexus  V1.0.0`. `ZooTycoonNexus\Data\Nexus.db`
(with its `-shm`/`-wal`) and `ZooTycoonNexus\Logs\Nexus<date>.log` were created, and appeared both under `%LOCALAPPDATA%` and under the Claude package's
`LocalCache\Local\` (the two views show the same files). Serilog's rolling sink inserts the date, so the log file is `Nexus20261023.log`, not a literal `Nexus.log` (unchanged
behaviour from `Launcher<date>.log`). The process was stopped. Nothing under `%LOCALAPPDATA%\ZooTycoonLauncher\` was touched.

## Task 3 outcome

Commit `e464bda` renamed the SDD to `1-zoo-tycoon-nexus-master-design.md` and repaired every link to it. The SDD, CLAUDE.md and `conventions.md` take the new product name,
project prefix, storage folder and file names; the SDD gains revision 1.18 and a rewritten §1.2 naming paragraph. The manual test plan named no project, storage path or
`Launcher.db`, so it needed no change. The verification grep over the current documents leaves only revision rows before 1.18, `ZooTycoonLauncherRef`,
`ZooTycoonLauncherHiFiUIPrototype.gif`, the Ref build's historical `launcher.config` path (R9) and the 1.18 row's own text.

## Defects found in the plan

- **Task 2 Step 5 grep cannot be empty.** Step 1's negative assertion (`directory.Contains("ZooTycoonLauncher")` in `AppStorageLocationsTests`) needs the literal, so the grep over
  `Tests` always hits it. That assertion is the one allowed survivor; the literal is kept so the test's intent stays readable.
- **Task 2 log file name.** The plan says `Logs\Nexus.log`, but Serilog's rolling sink writes `Nexus<yyyyMMdd>.log`. The configured base name is `Nexus.log`, as `Launcher.log` was
  before. Task 3's documents describe it the same way the SDD already does.
- **Task 3 Step 6 grep omits the Ref config path.** The SDD's `%APPDATA%\ZooTycoonLauncher\launcher.config` sentence matches the grep but is correct as it stands (R9).
- **Task 3 "returns nothing" grep cannot be empty.** `git grep -n "1-zoo-tycoon-launcher-master-design"` always hits the milestone 14 plan and R6 above, because they describe the
  rename by naming the old file. Those are the only allowed survivors.

## Incidents

- **Task 2's app check wrote to the real profile (expected side effect).** It created `%LOCALAPPDATA%\ZooTycoonNexus\` (`Nexus.db`, its `-wal` / `-shm`, a log) in the
  maintainer's real profile, and in the Claude package's redirected `LocalCache`. Step 1 of the data-move item under *Outstanding items* clears it.

## Outstanding items

For the maintainer, after the merge:

- Move the old data files, if the history is wanted (the app does not migrate them; otherwise it starts fresh and the old `%LOCALAPPDATA%\ZooTycoonLauncher\` folder can be
  deleted). In this order:
  1. With the app closed, delete the `%LOCALAPPDATA%\ZooTycoonNexus\` folder that this milestone's test run created (it holds a fresh `Nexus.db` and possibly `-wal` /
     `-shm` files; dropping the old database in beside another database's `-wal` risks corruption).
  2. Rename (or copy) the whole `%LOCALAPPDATA%\ZooTycoonLauncher\` folder to `%LOCALAPPDATA%\ZooTycoonNexus\`.
  3. Inside it, rename `Data\Launcher.db` to `Data\Nexus.db`, together with any `Launcher.db-wal` / `Launcher.db-shm` (or check none exist).
  4. Leave every `Data\{installationId}.db` and `Logs\Installations\` exactly as they are — the registry in `Nexus.db` points at them; moving only `Launcher.db` would
     silently start fresh per-installation history.
  5. Optionally rename old `Logs\Launcher*.log` files to `Nexus*.log`.
- Rename the local checkout folder `D:\Source\Personal\Erdmier.ZooTycoonLauncher` to match the repository, `Erdmier.ZooTycoonNexus`.
- Reopen Rider on `Erdmier.ZooTycoonNexus.slnx`.
- Rename any local `ZOOLAUNCHER_DESIGNTIME_DB` or `ZOOLAUNCHER_INSTALLATION_DESIGNTIME_DB` environment variables to the `ZOONEXUS_` names (R5).

For a later milestone:

- The brand artwork still reads "ZTL" (`Source/Erdmier.ZooTycoonNexus.Desktop/Assets/Images/ZTL_Main-Logo.png` and the `ZTL_*` asset names used by `MainWindow.axaml` and
  the prompt views). New art and asset names are for when the §9.9 About dialogue is built.
- Rename the product in the Claude Design project and re-import the prototype snapshot.
