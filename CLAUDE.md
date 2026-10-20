# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Native Windows desktop launcher for **Zoo Tycoon (2001)**. Multi-installation discovery and management, exhaustive INI configuration per installation with full historical
versioning in SQLite, scenarios management, screen-mode enumeration with ZT1 filtering, and one-click launch. Windows-only (permanently — the game is a 32-bit Windows binary from
2001); .NET 10, Avalonia 11.3 + Classic.Avalonia theme (Windows 95/98 aesthetic).

The launcher is a **fresh-start rewrite**, built greenfield under `Source/` as a layered architecture (`Erdmier.ZooTycoonLauncher.{Domain, Application, Infrastructure,
Desktop}`). The prior single-project prototype (the *Ref* build, `Erdmier.ZooTycoonLauncherRef`) served as read-only inspiration for the early slices and was removed from the
repository in `8e0532b`. Where the SDD or an older plan mentions "the Ref build", it means that prototype; recover a file with `git show 8e0532b^:References/<path>` only if a
design question genuinely needs it, and never copy-paste from it.

## Authoritative documents

Work is organised into **epics** (e.g. `Epic-1-MVP`, the SDD's Phase 1) made of numbered **milestones**. A milestone has a plan, usually a design spec, and a ledger, all
sharing the same `milestone-<N>-<feature-name>` stem. Number milestones in creation order within the epic; a follow-up to milestone `<N>` that needs its own document is
`<N>.1`, `<N>.2`, …

- **SDD (authoritative spec):** [`Docs/Specs/1-zoo-tycoon-launcher-master-design.md`](./Docs/Specs/1-zoo-tycoon-launcher-master-design.md). When the SDD and any other
  document disagree, the SDD wins. **Keep it current:** when a milestone changes the architecture, the slice list, the view pairs or the folder layout, amend the SDD in the
  same milestone — never let the code and the spec drift apart.
- **Milestone specs:** under [`Docs/Specs/<Epic>/`](./Docs/Specs/) — e.g. [`Docs/Specs/Epic-1-MVP/`](./Docs/Specs/Epic-1-MVP/) — named
  `milestone-<N>-<feature-name>-design.md`, matching the plan it designs, with **no date prefix**; the date lives inside the file as a `**Date:** YYYY-MM-DD` line. The master
  SDD is the one spec that sits directly under `Docs/Specs/` rather than in an epic folder. Not every milestone needs a spec (milestones 1, 2 and 5 have none).
- **Milestone plans:** under [`Docs/Plans/<Epic>/`](./Docs/Plans/) — e.g. [`Docs/Plans/Epic-1-MVP/`](./Docs/Plans/Epic-1-MVP/) — named `milestone-<N>-<feature-name>.md`
  with **no date prefix**; the date lives inside the file as a `**Date:** YYYY-MM-DD` line under the title. Each plan is an executable task list. **Tick each task's checkbox (
  `- [ ]` → `- [x]`) in the plan file as it is completed**, as part of working the plan — not in a clean-up pass afterwards.
- **Plan ledgers:** under `Docs/Ledgers/<Epic>/`, named `milestone-<N>-ledger.md`, one per plan. Write the ledger *while* executing — it is the recovery map if context is
  lost — and clean it into a readable document when the plan finishes. It must record, at minimum: every **ruling** taken without the user and what it costs if wrong; **defects
  found in the plan itself**; **outstanding items** grouped by when they should be revisited; and any **incidents**. Milestones 1–8 predate this rule and have no
  ledger.
- **Keep the solution view in sync:** [`Erdmier.ZooTycoonLauncher.slnx`](./Erdmier.ZooTycoonLauncher.slnx) lists every file under `Docs/` (in the top-level `/Docs/…`
  solution folders, which mirror the on-disk path — never under `/SolutionItems/`) and the root-level files under `/SolutionItems/`. **Whenever such a file is created,
  renamed, moved or deleted, update the `.slnx` to match** in the same commit, keeping milestone documents in milestone order. The user reads these documents through Rider's
  solution view, where an unregistered file is invisible; Rider's auto-sync is a convenience, not a guarantee.
- **Design reference:** the Claude Design hi-fi prototype, captured in
  [`Docs/UserInterfaceDesign/ZooTycoonLauncherHiFiUIPrototype.gif`](./Docs/UserInterfaceDesign/ZooTycoonLauncherHiFiUIPrototype.gif) and the `HiFiMockupScreenshots/`
  beside it, with recurring UI patterns documented in [`Docs/UserInterfaceDesign/conventions.md`](./Docs/UserInterfaceDesign/conventions.md). The SDD (§9), not the
  prototype, is the contract.
- **Manual test plan:** [`Docs/TestPlans/manual-end-to-end-tests.md`](./Docs/TestPlans/manual-end-to-end-tests.md).

## Executing a plan

Drive every plan with `superpowers:subagent-driven-development`. **Default to it without asking about the approach**; ask only for a "go" / "stop" to start.

- **Start on a feature branch.** When a plan starts and it doesn't already have a feature branch, create one named `feat/<milestone plan name>` — the plan's file stem, e.g.
  `feat/milestone-9-scenarios` for `milestone-9-scenarios.md`. If the current branch has uncommitted work, stop and ask the user whether to carry it over onto the new
  branch, or to commit and push it first.

- **Models:** dispatch implementers (including fix-round re-dispatches) on the latest version of **Sonnet** and every advisory or review seat — task reviewers, scoped re-reviews,
  the final whole-branch review, any advisor or escalation — on the latest version of **Opus**. Always name the model explicitly on dispatch; an omitted model silently inherits the
  session's.
- **Commit each task automatically** as it completes. Do not ask for per-task approval, and do not wait for a review of the work before committing — the task review happens
  against the committed diff. Say so in every implementer dispatch, or implementers following *Git commits* below will leave their work uncommitted.
- **Run semi-silently.** Do not narrate each task. Surface only: a real problem, a decision that needs the user's judgement, and anything the user must act on themselves. **Report
  once, at the end**, with what was built, the commits, the test position, and the rulings taken on their behalf.
- **Test the running app like a user.** Implementer and reviewer subagents may launch the launcher (`dotnet run --project Source/…Desktop/…Desktop.csproj`, or the `run`
  skill) and drive it as a user would — clicking through states, tabs and dialogues with computer use (desktop control, the `mcp__computer-use__*` tools) and taking
  screenshots — without asking first. Use it against *Sample installations* below, and close the app when done: a running launcher holds the Desktop build output (see
  *Build file-lock errors*) and counts as background work left running.
- **Fix review follow-ups before the PR.** After the final whole-branch review, run further reviewed fix waves for defects, polish, stale docs, mock-up mismatches and spec
  text that contradicts the implementation, rather than shipping them as a "known gaps" list. Stop once a wave's re-review has no Critical or Important findings. Only
  genuinely new features stay as follow-ups — they need their own milestone.
- **Finish with push + PR.** When the run is done on a feature branch, push the branch and open a pull request without asking — don't present the branch-completion menu.
- **Stop and ask** for anything else irreversible or outward-facing (a merge, a push to `main` or another shared branch, a publish), a security-sensitive action, or a plan
  defect with no sane path forward. Everything else is a ruling: decide it, record it in the ledger, keep going.
- **Never let a subagent leave background work running.** Prohibit background commands and filesystem-wide searches (`Get-ChildItem C:\ -Recurse`, `find /`) in every
  dispatch; a task notification saying an agent "stopped with background work of its own still running" is a signal to investigate, not boilerplate.

## Sample installations (read-only)

Besides the default install at `C:\Program Files (x86)\Microsoft Games\Zoo Tycoon\`, the repo holds three installation copies under `SampleInstallations/` (`Zoo Tycoon`,
`Zoo Tycoon - Copy`, `Zoo Tycoon - Copy (2)`). Git ignores the folder, but Claude Code may read any file in it and use the copies as installations when testing the app.

**Claude Code must never write, rename or delete anything under `SampleInstallations/` — read only, no exceptions.** The rule binds Claude Code's own actions (file edits,
shell commands, subagents): never touch these files directly, not even to reset or "repair" a copy. It does not bind the launcher under test — the app saving INI edits, running
Fix, or the game rewriting `zoo.ini` is expected behaviour, so drive those flows through the app freely.

## Common commands

> **Always run shell commands via PowerShell — never Bash.**

- **Build:** prefer `mcp__rider__build_solution`. Fallback: `dotnet build Erdmier.ZooTycoonLauncher.slnx`.
- **Build a single project** (narrow build to sidestep file locks — see *Build file-lock errors*): `dotnet build Source/<Project>/<Project>.csproj`, e.g.
  `dotnet build Source/Erdmier.ZooTycoonLauncher.Domain/Erdmier.ZooTycoonLauncher.Domain.csproj`.
- **Validate task completion:** clean, then build. `dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx`.
- **Add an EF Core migration:**
  `dotnet ef migrations add <Name> --project Source/Erdmier.ZooTycoonLauncher.Infrastructure --context <ContextName> --output-dir Persistence/<scope>/Migrations`.

## Build file-lock errors

Builds may fail with a **file lock error** (an output assembly or DLL that cannot be written because it is "in use"). Almost 10 times out of 10 this is **not** because the user has
the launcher running — it is an artefact of the **Avalonia Preview plugin in Rider** holding a handle on the built output. Do the following, in order:

1. **Build only what is relevant.** If a change touched a single project, build just that project and run only its corresponding test project — e.g. a Domain-only change needs only
   the Domain project built and `Erdmier.ZooTycoonLauncher.Domain.Tests.Unit` run. A narrow build usually sidesteps the locked Desktop output entirely.
2. **Only escalate to a full build when genuinely needed.** If a true build of the whole solution (or the Desktop project specifically) really is required, first confirm the lock
   is a genuine problem and not just the preview-plugin artefact.
3. **When unsure, stop and ask the user.** If you cannot safely determine whether the lock can be ignored, ask — the user can resolve or clarify it quickly.

**Never** get sidetracked troubleshooting the lock, running excessive diagnostic commands, or looping on retries. This is a known, benign artefact; a single question to the user is
always cheaper than an investigation.

## Git commits

Use the **conventional commit** format with a **gitmoji** in the type scope: `type(emoji): subject`.

Type→gitmoji map: `feat`→✨, `fix`→🐛, `refactor`→♻️, `style`→🎨, `docs`→📝, `chore`→🔧, `perf`→⚡, `test`→✅, `build`→📦, `ci`→👷.

Commit per logical task, not per file.

**During a plan run, commit each task automatically** (see *Executing a plan*). **Outside a plan run, after each completed task, automatically ask the user whether to
commit** (via `AskUserQuestion` or an inline yes/no): propose the conventional-commit message you would use, list the files that would be staged, and wait for explicit approval
before running `git commit`. Either way, stage only the files the task touched; never `git add -A` over a tree you have not inspected.

## Architecture (summary)

Clean Architecture + Vertical Slice Architecture + CQRS via source-generated `Mediator` (martinothamar). Handlers return `ErrorOr<T>`; expected failures are values, not exceptions.
FluentValidation validators run as a Mediator pipeline behaviour. POCO entities; Ardalis.SmartEnum for richer enumerations. EF Core + SQLite, code-first migrations. Serilog file
sink under `%LOCALAPPDATA%\ZooTycoonLauncher\Logs\`.

Dependency direction: `Desktop → Application → Domain`; `Infrastructure → Application/Domain`; `Desktop` composes `Infrastructure` only at the composition-root namespace (
`Erdmier.ZooTycoonLauncher.Desktop.Composition`). An architecture test in `Tests.Architecture` enforces this.

Storage layout under `%LOCALAPPDATA%\ZooTycoonLauncher\`: `Data\Launcher.db` (settings + installation registry) and `Data\{installationId}.db` (per-installation INI snapshots with
`Original` / `Current` / `Historical` tables). Logs at `Logs\Launcher.log` + `Logs\Installations\{installationId}.log`. Full detail in SDD §4.7, §6.

Presentation: Avalonia 11 + Classic.Avalonia (Windows 95/98 chrome) + CommunityToolkit.Mvvm. Views are decomposed into small `UserControl` + `ViewModel` pairs the way Blazor /
React / Angular use components: per main-window state, per tab, per INI section. `MainWindow.axaml` is chrome only and capped at ≤ 100 lines by an architecture test. See SDD §9.2.

## Conventions (strict; architecture tests enforce many of these)

- **One type per file. No exceptions.** Class, record, struct, interface, enum, delegate each in its own file, named after the type, in a folder mirroring the namespace.
- **No files at any project root.** Every file lives under a subfolder mirroring its namespace (e.g. DI registration goes under `Common/Extensions/`).
- **`GlobalUsings.cs`** per assembly consolidates `using` directives. Keep a local `using` only when truly necessary (namespace conflict, alias).
- **File-scoped namespaces** everywhere.
- **British English everywhere** — code comments, XML doc text, identifier wording where there's a choice (`Minimise`, `Initialise`, `Behaviour`, `Colour`), commit messages,
  Markdown docs. Don't switch a US spelling already established by an external API surface (`System.IO`, `Color`, etc.).
- **Markdown hard-wrap at 180 characters.** When writing *or* editing any Markdown file, manually wrap prose at a maximum of 180 characters per line. The user's
  IDE hard-wraps at 180 but does not auto-repair the stray paragraph breaks that unwrapped edits leave behind, so emitting pre-wrapped Markdown saves a manual
  clean-up every time. Wrap prose and bullet text; leave fenced code blocks, tables, headings, and bare URLs intact on their own lines (never break a URL). To *verify* the wrap,
  measure line length with PowerShell (`[string].Length`) — not `awk length` or `wc -c`, which count bytes, so this repo's `—`, `×`, `…` (multi-byte UTF-8) inflate the count and
  raise false over-limit hits.
- **XML doc comments on every public member and type** (`///`-prefixed `<summary>`, `<param>`, `<returns>`, `<remarks>`, `<exception>` as appropriate). Plain `//` comments are for
  inline implementation notes only.
- **`<c>…</c>` tags carry no inside whitespace.** Write `<c>zoo.ini</c>`, never `<c> zoo.ini </c>`. Same applies to `<code>…</code>`.
- **Spaced bracket style:** `[ STAThread ]`. Match the established style.
- **Vertical spacing:** separate consecutive statements of different kinds with a blank line (e.g. an object instantiation followed by a method call, or a local declaration
  followed by a member/static invocation). Keep like statements grouped, with blank lines between groups.
- **UTC for every timestamp.** Column names and properties carry the `Utc` suffix. Localisation only at the UI boundary.
- **PascalCase + plural** for table names (`LauncherSettings`, `GameInstallations`, `IniValues`).
- **File system access** goes through `System.IO.Abstractions.IFileSystem`. **Registry access** goes through `IRegistryReader`. **Display modes** go through
  `IScreenModeEnumerator`. Keep it that way for testability.
- **Source-generated MVVM:** `[ObservableProperty]` on `partial` properties, `[RelayCommand]` on private async methods. Don't hand-roll INPC.
- **Compiled bindings:** every XAML file declares `x:DataType`.
- **Designer constructors:** view models expose a parameterless constructor that delegates to `file`-scoped null-object implementations for the XAML designer only.
- **File writes that touch user data** (config, INI) use the temp-file + `File.Move(overwrite: true)` pattern. Don't write in place. Wrap in an EF transaction following SDD §8.2
  ordering (archive `Current` → write file → replace `Current`).
- **Don't add `[ UsedImplicitly ]` proactively.** JetBrains.Annotations is referenced, but the user adds the attribute themselves when ReSharper/Rider actually complains.
  Don't speculatively mark designer-only types, DI-resolved types, or anything else that "looks dead but isn't" — and when a plan or spec shows `[UsedImplicitly]`, omit just
  that attribute and copy the rest of the type verbatim. If an analyser warning explicitly demands it on a specific member, propose it inline and let the user decide.
- **View composition pair rule:** every public `*ViewModel` class in `Desktop` has a sibling `*View.axaml` file under the parallel `Views/…` folder, and vice versa. `MainWindow` is
  the documented exception (chrome only).
