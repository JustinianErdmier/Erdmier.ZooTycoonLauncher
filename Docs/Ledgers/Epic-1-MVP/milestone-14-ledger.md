# Milestone 14 — Zoo Tycoon Nexus Rename: Ledger

**Date:** 2026-10-23

**Plan:** [`milestone-14-zoo-tycoon-nexus-rename.md`](../../Plans/Epic-1-MVP/milestone-14-zoo-tycoon-nexus-rename.md) · **Branch:** `feat/milestone-14-zoo-tycoon-nexus-rename`

The product is renamed **Zoo Tycoon Nexus**. The solution, every project and every namespace move from `Erdmier.ZooTycoonLauncher.*` to `Erdmier.ZooTycoonNexus.*`; the
storage folder, the database and log names, the window titles and the documents follow in later tasks.

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

## Task 2 outcome

Commit `a637a8d`. Tests written first and seen failing (old folder, `Launcher.db`/`Launcher.log`, `Zoo Tycoon Launcher` titles), then green: Infrastructure integration 64,
Desktop unit 375, Architecture 27, Application unit 205, Domain unit 140, all passing with 0 build warnings. The `git grep` for the old product name, folder and file names
over `Source`, `Tests` and `.idea` is empty.

Step 7 app check: the built Desktop exe was started with `Start-Process` and captured through user32. The title bar reads `Zoo Tycoon Nexus  V1.0.0`. `ZooTycoonNexus\Data\Nexus.db`
(with its `-shm`/`-wal`) and `ZooTycoonNexus\Logs\Nexus<date>.log` were created, and appeared both under `%LOCALAPPDATA%` and under the Claude package's
`LocalCache\Local\` (the two views show the same files). Serilog's rolling sink inserts the date, so the log file is `Nexus20261023.log`, not a literal `Nexus.log` (unchanged
behaviour from `Launcher<date>.log`). The process was stopped. Nothing under `%LOCALAPPDATA%\ZooTycoonLauncher\` was touched.

## Defects found in the plan

_None yet._

## Incidents

_None yet._

## Outstanding items

_None yet._
