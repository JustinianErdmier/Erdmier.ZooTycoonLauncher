# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Native Windows desktop launcher for **Zoo Tycoon (2001)**. Multi-installation discovery and management, exhaustive INI configuration per installation with full historical versioning in SQLite, scenarios management, screen-mode enumeration with ZT1 filtering, and one-click launch. Windows-only (permanently — the game is a 32-bit Windows binary from 2001); .NET 10, C# 13, Avalonia 11.3 + Classic.Avalonia theme (Windows 95/98 aesthetic).

The repository is in the middle of a **fresh-start rewrite**. The new layered architecture (`Erdmier.ZooTycoonLauncher.{Domain, Application, Infrastructure, Desktop}`) is being built greenfield under `Source/`; the prior single-project prototype is preserved read-only as `References/Erdmier.ZooTycoonLauncherRef/` for visual inspiration only. **Never copy-paste from the Ref assembly** — reimplement the moral equivalent fresh against the new conventions.

## Authoritative documents

- **SDD (authoritative spec):** [`docs/superpowers/specs/2026-05-26-zoo-tycoon-launcher-design.md`](./docs/superpowers/specs/2026-05-26-zoo-tycoon-launcher-design.md). When the SDD and any other document disagree, the SDD wins.
- **Milestone plans:** under [`docs/superpowers/plans/`](./docs/superpowers/plans/), named `YYYY-MM-DD-<feature-name>.md`. Each plan is an executable task list — use the `superpowers:executing-plans` or `superpowers:subagent-driven-development` skill to drive them. **Default to subagent-driven** without asking about the approach; ask only for a "go" / "stop" to start.
- **Reference build (read-only):** [`References/Erdmier.ZooTycoonLauncherRef/`](./References/Erdmier.ZooTycoonLauncherRef/). Cite Ref files inline as "for inspiration" when their design informed the new design; rewrite, never `git mv`.

## Common commands

> **Always run shell commands via PowerShell — never Bash.**

- **Build:** prefer `mcp__rider__build_solution`. Fallback: `dotnet build Erdmier.ZooTycoonLauncher.slnx`.
- **Build a single project** (narrow build to sidestep file locks — see *Build file-lock errors*): `dotnet build Source/<Project>/<Project>.csproj`, e.g. `dotnet build Source/Erdmier.ZooTycoonLauncher.Domain/Erdmier.ZooTycoonLauncher.Domain.csproj`.
- **Validate task completion:** clean, then build. `dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx`.
- **Run all tests:** `dotnet test Erdmier.ZooTycoonLauncher.slnx`.
- **Run the launcher:** `dotnet run --project Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj`.
- **Add an EF Core migration:** `dotnet ef migrations add <Name> --project Source/Erdmier.ZooTycoonLauncher.Infrastructure --context <ContextName> --output-dir Persistence/<scope>/Migrations`.

## Build file-lock errors

Builds may fail with a **file lock error** (an output assembly or DLL that cannot be written because it is "in use"). Almost 10 times out of 10 this is **not** because the user has the launcher running — it is an artefact of the **Avalonia Preview plugin in Rider** holding a handle on the built output. Do the following, in order:

1. **Build only what is relevant.** If a change touched a single project, build just that project and run only its corresponding test project — e.g. a Domain-only change needs only the Domain project built and `Erdmier.ZooTycoonLauncher.Domain.Tests.Unit` run. A narrow build usually sidesteps the locked Desktop output entirely.
2. **Only escalate to a full build when genuinely needed.** If a true build of the whole solution (or the Desktop project specifically) really is required, first confirm the lock is a genuine problem and not just the preview-plugin artefact.
3. **When unsure, stop and ask the user.** If you cannot safely determine whether the lock can be ignored, ask — the user can resolve or clarify it quickly.

**Never** get sidetracked troubleshooting the lock, running excessive diagnostic commands, or looping on retries. This is a known, benign artefact; a single question to the user is always cheaper than an investigation.

## Git commits

Use the **conventional commit** format with a **gitmoji** in the type scope: `type(emoji): subject`.

Type→gitmoji map: `feat`→✨, `fix`→🐛, `refactor`→♻️, `style`→🎨, `docs`→📝, `chore`→🔧, `perf`→⚡, `test`→✅, `build`→📦, `ci`→👷.

Commit per logical task, not per file.

**After each completed task, automatically ask the user whether to commit** (via `AskUserQuestion` or an inline yes/no). Propose the conventional-commit message you would use, list the files that would be staged, and wait for explicit approval before running `git commit`. Never commit silently and never commit on the user's behalf without that per-task confirmation.

## Architecture (summary)

Clean Architecture + Vertical Slice Architecture + CQRS via source-generated `Mediator` (martinothamar). Handlers return `ErrorOr<T>`; expected failures are values, not exceptions. FluentValidation validators run as a Mediator pipeline behaviour. POCO entities; Ardalis.SmartEnum for richer enumerations. EF Core + SQLite, code-first migrations. Serilog file sink under `%LOCALAPPDATA%\ZooTycoonLauncher\Logs\`.

```text
Source/
├── Erdmier.ZooTycoonLauncher.Domain/         # POCO entities, smart enums, INI key registry
├── Erdmier.ZooTycoonLauncher.Application/    # VSA slices, CQRS, abstractions
├── Erdmier.ZooTycoonLauncher.Infrastructure/ # EF Core + SQLite, INI parser, Win32 P/Invoke, Serilog
└── Erdmier.ZooTycoonLauncher.Desktop/        # Avalonia views, view models, DI composition root
Tests/
├── Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/
├── Erdmier.ZooTycoonLauncher.Application.Tests.Unit/
├── Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/
└── Erdmier.ZooTycoonLauncher.Tests.Architecture/
References/
└── Erdmier.ZooTycoonLauncherRef/             # Legacy single-project, read-only inspiration
```

Dependency direction: `Desktop → Application → Domain`; `Infrastructure → Application/Domain`; `Desktop` composes `Infrastructure` only at the composition-root namespace (`Erdmier.ZooTycoonLauncher.Desktop.Composition`). An architecture test in `Tests.Architecture` enforces this.

Storage layout under `%LOCALAPPDATA%\ZooTycoonLauncher\`: `Data\Launcher.db` (settings + installation registry) and `Data\{installationId}.db` (per-installation INI snapshots with `Original` / `Current` / `Historical` tables). Logs at `Logs\Launcher.log` + `Logs\Installations\{installationId}.log`. Full detail in SDD §4.7, §6.

Presentation: Avalonia 11 + Classic.Avalonia (Windows 95/98 chrome) + CommunityToolkit.Mvvm. Views are decomposed into small `UserControl` + `ViewModel` pairs the way Blazor / React / Angular use components: per main-window state, per tab, per INI section. `MainWindow.axaml` is chrome only and capped at ≤ 100 lines by an architecture test. See SDD §9.2.

## Conventions (strict; architecture tests enforce many of these)

- **One type per file. No exceptions.** Class, record, struct, interface, enum, delegate each in its own file, named after the type, in a folder mirroring the namespace.
- **No files at any project root.** Every file lives under a subfolder mirroring its namespace (e.g. DI registration goes under `Common/Extensions/`).
- **`GlobalUsings.cs`** per assembly consolidates `using` directives. Keep a local `using` only when truly necessary (namespace conflict, alias).
- **File-scoped namespaces** everywhere.
- **British English everywhere** — code comments, XML doc text, identifier wording where there's a choice (`Minimise`, `Initialise`, `Behaviour`, `Colour`), commit messages, Markdown docs. Don't switch a US spelling already established by an external API surface (`System.IO`, `Color`, etc.).
- **XML doc comments on every public member and type** (`///`-prefixed `<summary>`, `<param>`, `<returns>`, `<remarks>`, `<exception>` as appropriate). Plain `//` comments are for inline implementation notes only.
- **`<c>…</c>` tags carry no inside whitespace.** Write `<c>zoo.ini</c>`, never `<c> zoo.ini </c>`. Same applies to `<code>…</code>`.
- **Spaced bracket style:** `[ STAThread ]`, `[ UsedImplicitly ]`. Match the established style.
- **Vertical spacing:** separate consecutive statements of different kinds with a blank line (e.g. an object instantiation followed by a method call, or a local declaration followed by a member/static invocation). Keep like statements grouped, with blank lines between groups.
- **UTC for every timestamp.** Column names and properties carry the `Utc` suffix. Localisation only at the UI boundary.
- **PascalCase + plural** for table names (`LauncherSettings`, `GameInstallations`, `IniValues`).
- **File system access** goes through `System.IO.Abstractions.IFileSystem`. **Registry access** goes through `IRegistryReader`. **Display modes** go through `IScreenModeEnumerator`. Keep it that way for testability.
- **Source-generated MVVM:** `[ObservableProperty]` on `partial` properties, `[RelayCommand]` on private async methods. Don't hand-roll INPC.
- **Compiled bindings:** every XAML file declares `x:DataType`.
- **Designer constructors:** view models expose a parameterless constructor that delegates to `file`-scoped null-object implementations for the XAML designer only; mark them `[ UsedImplicitly ]`.
- **File writes that touch user data** (config, INI) use the temp-file + `File.Move(overwrite: true)` pattern. Don't write in place. Wrap in an EF transaction following SDD §8.2 ordering (archive `Current` → write file → replace `Current`).
- **JetBrains.Annotations** is referenced; mark designer/runtime-only types with `[ UsedImplicitly ]` to keep ReSharper/Rider quiet.
- **Test project naming:** `<AssemblyName>.Tests.<Category>` under `Tests/` (Unit, Integration, Architecture). Solution-spanning architecture tests use the root name (`Erdmier.ZooTycoonLauncher.Tests.Architecture`).
- **View composition pair rule:** every public `*ViewModel` class in `Desktop` has a sibling `*View.axaml` file under the parallel `Views/…` folder, and vice versa. `MainWindow` is the documented exception (chrome only).
