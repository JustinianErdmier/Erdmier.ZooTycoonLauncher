# Foundations & Scaffolding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the four-project Clean Architecture solution skeleton (`Domain`, `Application`, `Infrastructure`, `Desktop`) with their four sibling test projects, so the rest of the milestone series has somewhere to land. Pin the architectural conventions (every-VM-has-a-View, one type per file, dependency direction) with NetArchTest rules from day one. End state: the launcher boots into an empty Classic.Avalonia window, `Launcher.db` is created with EF Core migrations, Serilog writes to `%LOCALAPPDATA%\ZooTycoonLauncher\Logs\Launcher.log`, and the four test projects all run green.

**Architecture:** Clean Architecture + VSA + CQRS via Mediator (martinothamar). `Desktop` (Avalonia 11 + Classic.Avalonia + CommunityToolkit.Mvvm) composes `Infrastructure` (EF Core SQLite + Serilog) at the composition root; `Application` exposes abstractions consumed by `Infrastructure` implementations; `Domain` carries POCO entities and Ardalis.SmartEnum enumerations. This plan only lays the rails — no installation or INI logic yet.

**Tech Stack:** .NET 10, C# 13, Avalonia 11.3, Classic.Avalonia 11.3, CommunityToolkit.Mvvm 8.4, Mediator (martinothamar) 2.x, ErrorOr, FluentValidation, Ardalis.SmartEnum, Serilog (file sink), Microsoft.EntityFrameworkCore.Sqlite 10.x (or current stable at implementation), System.IO.Abstractions, JetBrains.Annotations. Tests: xUnit, Shouldly, NSubstitute, NetArchTest.Rules.

**Reference SDD:** [`docs/superpowers/specs/2026-05-26-zoo-tycoon-launcher-design.md`](../specs/2026-05-26-zoo-tycoon-launcher-design.md). Where this plan says "per SDD §X.Y", read the linked section for the rationale; this plan only specifies the *what* and the *how*.

**Reference build:** the renamed `References/Erdmier.ZooTycoonLauncherRef/` project. It's read-only inspiration — *never* `git mv` or copy-paste from it. Cite a Ref file inline only as a "for inspiration" pointer; rewrite the moral equivalent fresh.

---

## Files Overview

This plan creates the solution skeleton plus the minimum surface to make it runnable. Listed in dependency order (a file in row N may depend on files from rows < N).

### Solution / build

| Path                                                | Responsibility                                                                    |
|-----------------------------------------------------|-----------------------------------------------------------------------------------|
| `Erdmier.ZooTycoonLauncher.slnx`                    | Modified: add eight new projects (4 source + 4 tests); retains the Ref project.   |
| `Directory.Packages.props`                          | Created: central package version management (CPM) for the whole solution.         |
| `Directory.Build.props`                             | Created: solution-wide `<Nullable>enable</Nullable>`, `<LangVersion>` etc.        |
| `.editorconfig`                                     | Modified (or created): style rules matching the maintainer's conventions.         |

### Domain

| Path                                                                       | Responsibility                                       |
|----------------------------------------------------------------------------|------------------------------------------------------|
| `Source/Erdmier.ZooTycoonLauncher.Domain/Erdmier.ZooTycoonLauncher.Domain.csproj` | Project file. No project references; minimal deps.   |
| `Source/Erdmier.ZooTycoonLauncher.Domain/GlobalUsings.cs`                  | Solution-wide usings for the Domain assembly.        |
| `Source/Erdmier.ZooTycoonLauncher.Domain/Installations/GameInstallation.cs`| POCO entity (SDD §5.1).                              |
| `Source/Erdmier.ZooTycoonLauncher.Domain/Installations/InstallationValidity.cs` | SmartEnum (SDD §5.2).                                |
| `Source/Erdmier.ZooTycoonLauncher.Domain/Settings/LauncherSettings.cs`     | POCO entity, single-row (SDD §5.1).                  |
| `Source/Erdmier.ZooTycoonLauncher.Domain/Settings/LauncherStartupPreference.cs` | SmartEnum (SDD §5.2).                                |
| `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniSnapshot.cs`      | POCO entity (SDD §5.1).                              |
| `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniValue.cs`         | POCO entity (SDD §5.1).                              |
| `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniSnapshotKind.cs`  | SmartEnum (SDD §5.2).                                |
| `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniSnapshotTrigger.cs` | SmartEnum (SDD §5.2).                                |
| `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniValueSource.cs`   | SmartEnum (SDD §5.2).                                |
| `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniValueKind.cs`     | SmartEnum (SDD §5.2).                                |

### Application

| Path                                                                                   | Responsibility                                          |
|----------------------------------------------------------------------------------------|---------------------------------------------------------|
| `Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj` | Project file. Reference: Domain.                        |
| `Source/Erdmier.ZooTycoonLauncher.Application/GlobalUsings.cs`                         | Solution-wide usings.                                   |
| `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IAppStorageLocations.cs` | Path resolution interface (SDD §4.7).                   |
| `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/ILauncherSettingsRepository.cs` | Settings read/write interface.                          |
| `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IInstallationRepository.cs` | Installation read/write interface (stubs only here).    |

### Infrastructure

| Path                                                                                       | Responsibility                                                  |
|--------------------------------------------------------------------------------------------|-----------------------------------------------------------------|
| `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj` | Project file. References: Application.                          |
| `Source/Erdmier.ZooTycoonLauncher.Infrastructure/GlobalUsings.cs`                          | Solution-wide usings.                                           |
| `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Storage/AppStorageLocations.cs`    | Implementation of `IAppStorageLocations`.                       |
| `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/LauncherDbContext.cs` | EF Core context for `Launcher.db`.                              |
| `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Configurations/LauncherSettingsConfiguration.cs` | `IEntityTypeConfiguration<LauncherSettings>` (single-row CHECK). |
| `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Configurations/GameInstallationConfiguration.cs` | `IEntityTypeConfiguration<GameInstallation>`.                   |
| `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Repositories/LauncherSettingsRepository.cs` | Concrete `ILauncherSettingsRepository`.                         |
| `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Repositories/InstallationRepository.cs` | Concrete `IInstallationRepository`.                             |
| `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Migrations/*`        | Initial migration generated by `dotnet ef migrations add`.      |
| `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Logging/SerilogConfiguration.cs`          | File-only sink under `IAppStorageLocations.LauncherLogPath`.    |
| `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Extensions/InfrastructureServiceCollectionExtensions.cs` | `AddInfrastructure(this IServiceCollection)`.                   |

### Desktop

| Path                                                                                   | Responsibility                                                  |
|----------------------------------------------------------------------------------------|-----------------------------------------------------------------|
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj`    | Project file. References: Application, Infrastructure.          |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`                             | Solution-wide usings.                                           |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Program.cs`                                  | Entry point (AppBuilder).                                       |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/App.axaml` + `App.axaml.cs`                  | Avalonia application class + DI composition.                    |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs` | `AddDesktop(this IServiceCollection)`.                          |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/ViewLocator.cs`                  | `*ViewModel` → `*View` resolution.                              |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml` + `.cs`              | Window chrome only — placeholder ContentControl (SDD §9.2).     |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs`           | Hosts a placeholder string for now.                             |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/app.manifest`                                | Windows manifest (DPI awareness).                               |
| `Source/Erdmier.ZooTycoonLauncher.Desktop/Assets/.gitkeep`                             | Placeholder so the folder exists; icons added later.            |

### Tests

| Path                                                                                                | Responsibility                                                          |
|-----------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------|
| `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj` | xUnit project. Reference: Domain.                                       |
| `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/GlobalUsings.cs`                                 | xUnit + Shouldly + NSubstitute usings.                                  |
| `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Installations/InstallationValidityTests.cs`      | 2×2 truth table for `InstallationValidity.From`.                        |
| `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj` | xUnit project. Reference: Application.                                  |
| `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs`                            | Standard test usings.                                                   |
| `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj` | xUnit project. Reference: Infrastructure.                               |
| `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/GlobalUsings.cs`                  | Standard test usings.                                                   |
| `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Persistence/LauncherDbContextTests.cs` | Migrations apply cleanly on a fresh DB; single-row CHECK constraint fires. |
| `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/Erdmier.ZooTycoonLauncher.Tests.Architecture.csproj` | xUnit project. References: all four source projects.                    |
| `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/GlobalUsings.cs`                                | Standard test usings + NetArchTest.                                     |
| `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/DependencyDirectionTests.cs`                    | SDD §4.2 dependency direction (Desktop→App→Domain; Infra→App/Domain).   |
| `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/OneTypePerFileTests.cs`                         | Every public type's file name equals the type name.                     |
| `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/NoFilesAtAssemblyRootTests.cs`                  | Every source file lives under a subfolder mirroring its namespace.      |

---

## Build & test commands (used throughout)

- **PowerShell is the canonical shell.** Never invoke Bash for these commands.
- **Build:** `dotnet build Erdmier.ZooTycoonLauncher.slnx` (Rider's `mcp__rider__build_solution` if available is the maintainer's preferred path).
- **Clean + build (the SDD's canonical task-completion check):** `dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx`.
- **Run all tests:** `dotnet test Erdmier.ZooTycoonLauncher.slnx`.
- **Run one test project:** `dotnet test Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj`.
- **Run the app:** `dotnet run --project Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj`.
- **EF Core CLI (one-time):** `dotnet tool install --global dotnet-ef --version 10.0.*` (or current stable matching the EF Core package version).
- **EF Core migration add:** `dotnet ef migrations add <Name> --project Source/Erdmier.ZooTycoonLauncher.Infrastructure --startup-project Source/Erdmier.ZooTycoonLauncher.Desktop --context LauncherDbContext --output-dir Persistence/Launcher/Migrations`.

After every commit, run the clean+build to make sure nothing regressed; if a hook fails, fix the underlying cause and create a *new* commit (never `--amend`).

---

## Conventions (apply to every task)

These are SDD- and CLAUDERef.md-derived. Apply mechanically; an architecture test in Task 12 catches drift.

1. **One type per file. No exceptions.** Class, record, struct, interface, enum, delegate each in its own file, named after the type, in a folder mirroring the namespace.
2. **No files at any project root.** Every file lives under a subfolder mirroring its namespace (e.g. DI registration goes under `Common/Extensions/`, not at the project root).
3. **GlobalUsings.cs** per assembly consolidates `using` directives. Keep a local `using` only when truly necessary (namespace conflict, alias).
4. **British English** in code comments, XML doc text, identifier wording where there's a choice (`Minimise`, `Initialise`, `Behaviour`, `Colour`), commit messages, Markdown docs. Don't switch a US spelling already established by an external API surface (`System.IO`, `Color`, etc.).
5. **XML doc comments on every public member and type.** `<summary>`, `<param>`, `<returns>`, `<remarks>`, `<exception>` as appropriate. Plain `//` comments are for inline implementation notes only.
6. **`<c>…</c>` tags carry no inside whitespace.** Write `<c>zoo.ini</c>`, never `<c> zoo.ini </c>`.
7. **Spaced bracket style:** `[ STAThread ]` (the Ref launcher's established style — match it).
8. **File-scoped namespaces** everywhere.
9. **UTC for every timestamp.** Column names and properties carry the `Utc` suffix. Localisation only at the UI boundary.
10. **PascalCase + plural** for table names (`LauncherSettings`, `GameInstallations`).
11. **Commits:** conventional commit + gitmoji per CLAUDERef.md (e.g. `feat(✨):`, `fix(🐛):`, `chore(🔧):`, `refactor(♻️):`, `test(✅):`). Commit per logical task, not per file.

---

## Task 1: Solution scaffold + central package management

**Files:**
- Create: `Directory.Packages.props`
- Create: `Directory.Build.props`
- Modify: `Erdmier.ZooTycoonLauncher.slnx`

- [ ] **Step 1: Add `Directory.Build.props` at the repo root**

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Add `Directory.Packages.props` at the repo root**

This enables central package management (CPM). New projects don't include `Version=` on `<PackageReference>` — the version lives here.

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <!-- Avalonia -->
    <PackageVersion Include="Avalonia" Version="11.3.14" />
    <PackageVersion Include="Avalonia.Desktop" Version="11.3.14" />
    <PackageVersion Include="Avalonia.Fonts.Inter" Version="11.3.14" />
    <PackageVersion Include="AvaloniaUI.DiagnosticsSupport" Version="2.2.1" />
    <PackageVersion Include="Classic.Avalonia.Theme" Version="11.3.0.3" />
    <PackageVersion Include="Classic.Avalonia.Theme.DataGrid" Version="11.3.0.3" />
    <PackageVersion Include="Classic.Avalonia.Theme.Dock" Version="11.3.0.3" />

    <!-- MVVM -->
    <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.2" />

    <!-- Mediation / CQRS -->
    <PackageVersion Include="Mediator.Abstractions" Version="2.1.7" />
    <PackageVersion Include="Mediator.SourceGenerator" Version="2.1.7" />

    <!-- Result / Validation / Smart enums -->
    <PackageVersion Include="ErrorOr" Version="2.0.1" />
    <PackageVersion Include="FluentValidation" Version="11.10.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.10.0" />
    <PackageVersion Include="Ardalis.SmartEnum" Version="8.2.0" />

    <!-- EF Core (SQLite) -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0" />

    <!-- DI / Logging / IO -->
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
    <PackageVersion Include="Serilog" Version="4.2.0" />
    <PackageVersion Include="Serilog.Extensions.Hosting" Version="9.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />
    <PackageVersion Include="System.IO.Abstractions" Version="21.0.29" />

    <!-- Annotations -->
    <PackageVersion Include="JetBrains.Annotations" Version="2025.2.4" />

    <!-- Tests -->
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="Shouldly" Version="4.2.1" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
  </ItemGroup>
</Project>
```

> **Note:** version pins. EF Core 10.0.0 and the matching CLI tool may still be in preview at implementation time; if the GA package isn't available, fall back to the latest preview *and* note it in the commit message — don't silently drop to EF Core 9.x without confirming SQLite + .NET 10 compatibility.

- [ ] **Step 3: Modify the slnx — add empty placeholders for the eight new projects**

Replace the contents of `Erdmier.ZooTycoonLauncher.slnx` with:

```xml
<Solution>
    <Folder Name="/SolutionItems/">
        <File Path=".gitignore" />
        <File Path="CLAUDERef.md" />
        <File Path="Directory.Build.props" />
        <File Path="Directory.Packages.props" />
    </Folder>
    <Folder Name="/SolutionItems/docs/">
        <File Path="docs/SoftwareDesignDocumentRef.md" />
    </Folder>
    <Folder Name="/SolutionItems/docs/superpowers/specs/">
        <File Path="docs/superpowers/specs/2026-05-26-zoo-tycoon-launcher-design.md" />
    </Folder>
    <Folder Name="/SolutionItems/docs/superpowers/plans/">
        <File Path="docs/superpowers/plans/2026-05-26-foundations-and-scaffolding.md" />
    </Folder>
    <Folder Name="/Source/">
        <Project Path="Source/Erdmier.ZooTycoonLauncher.Domain/Erdmier.ZooTycoonLauncher.Domain.csproj" />
        <Project Path="Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj" />
        <Project Path="Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj" />
        <Project Path="Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj" />
    </Folder>
    <Folder Name="/Tests/">
        <Project Path="Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj" />
        <Project Path="Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj" />
        <Project Path="Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj" />
        <Project Path="Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/Erdmier.ZooTycoonLauncher.Tests.Architecture.csproj" />
    </Folder>
    <Folder Name="/References/">
        <Project Path="References/Erdmier.ZooTycoonLauncherRef/Erdmier.ZooTycoonLauncherRef.csproj" />
    </Folder>
</Solution>
```

The projects don't exist yet; the slnx references them so subsequent tasks "drop in" the projects without further slnx edits.

- [ ] **Step 4: Commit**

```powershell
git add Directory.Build.props Directory.Packages.props Erdmier.ZooTycoonLauncher.slnx
git commit -m "chore(🔧): add central package management and pre-register MVP projects in slnx"
```

The build will be broken at this point (the slnx references projects that don't exist yet). That's expected — subsequent tasks add them.

---

## Task 2: Domain project skeleton + entities + SmartEnums

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/Erdmier.ZooTycoonLauncher.Domain.csproj`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/GlobalUsings.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/Installations/GameInstallation.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/Installations/InstallationValidity.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/Settings/LauncherSettings.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/Settings/LauncherStartupPreference.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniSnapshot.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniValue.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniSnapshotKind.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniSnapshotTrigger.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniValueSource.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniValueKind.cs`

- [ ] **Step 1: Create the Domain csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <RootNamespace>Erdmier.ZooTycoonLauncher.Domain</RootNamespace>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Ardalis.SmartEnum" />
        <PackageReference Include="ErrorOr" />
        <PackageReference Include="JetBrains.Annotations" />
    </ItemGroup>
</Project>
```

- [ ] **Step 2: Add `GlobalUsings.cs`**

```csharp
global using Ardalis.SmartEnum;
global using ErrorOr;
global using JetBrains.Annotations;
```

- [ ] **Step 3: Add `InstallationValidity` SmartEnum (folder: `Installations/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.Installations;

/// <summary>
/// Classifies a <see cref="GameInstallation" /> by whether its required files are present on disk.
/// </summary>
/// <remarks>
/// Derived from <c>HasExe</c> and <c>HasIni</c> via <see cref="From" /> rather than stored, so the value
/// stays in sync with the row's current flags.
/// </remarks>
public sealed class InstallationValidity : SmartEnum<InstallationValidity>
{
    /// <summary>Both <c>zoo.exe</c> and <c>zoo.ini</c> are present; the installation is launchable.</summary>
    public static readonly InstallationValidity Valid             = new("Valid",             1, "Valid",                   "Green");

    /// <summary>The <c>zoo.exe</c> file is missing; the game cannot be launched.</summary>
    public static readonly InstallationValidity InvalidNoExe      = new("InvalidNoExe",      2, "Invalid — No EXE",         "Red");

    /// <summary>The <c>zoo.ini</c> file is missing; the launcher cannot configure or display INI state.</summary>
    public static readonly InstallationValidity InvalidNoIni      = new("InvalidNoIni",      3, "Invalid — No INI",         "Red");

    /// <summary>Both <c>zoo.exe</c> and <c>zoo.ini</c> are missing; the installation is wholly invalid.</summary>
    public static readonly InstallationValidity InvalidNoExeOrIni = new("InvalidNoExeOrIni", 4, "Invalid — No EXE or INI",  "Red");

    /// <summary>Display string shown in the Installation Management grid.</summary>
    public string DisplayName { get; }

    /// <summary>Colour token consumed by the XAML row template (Green for Valid, Red for the others).</summary>
    public string ColourToken { get; }

    private InstallationValidity(string name, int id, string displayName, string colourToken)
        : base(name, id)
    {
        DisplayName = displayName;
        ColourToken = colourToken;
    }

    /// <summary>
    /// Maps the <c>(hasExe, hasIni)</c> flag pair to the corresponding validity.
    /// </summary>
    /// <param name="hasExe"><see langword="true" /> when <c>zoo.exe</c> is present on disk.</param>
    /// <param name="hasIni"><see langword="true" /> when <c>zoo.ini</c> is present on disk.</param>
    /// <returns>The validity for the supplied flag pair.</returns>
    public static InstallationValidity From(bool hasExe, bool hasIni) =>
        (hasExe, hasIni) switch
        {
            (true,  true)  => Valid,
            (false, true)  => InvalidNoExe,
            (true,  false) => InvalidNoIni,
            (false, false) => InvalidNoExeOrIni,
        };
}
```

- [ ] **Step 4: Add `GameInstallation` entity (folder: `Installations/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.Installations;

/// <summary>
/// Represents a Zoo Tycoon installation tracked by the launcher.
/// </summary>
/// <remarks>
/// Identity is the <see cref="Id" /> Guid (Version 7) set by the application layer at creation time.
/// <see cref="Path" /> is <see langword="init" />-only — relocating an installation rewrites the row
/// in place rather than as a separate entity.
/// </remarks>
public sealed class GameInstallation
{
    /// <summary>The installation's identifier, assigned at creation via <c>Guid.CreateVersion7()</c>.</summary>
    public Guid Id { get; init; }

    /// <summary>The user-visible name; unique (case-insensitive) within the <c>GameInstallations</c> table.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The fully qualified directory path containing <c>zoo.exe</c>; unique (case-insensitive).</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>Indicates whether <c>zoo.exe</c> was present on disk at last verification.</summary>
    public bool HasExe { get; set; }

    /// <summary>Indicates whether <c>zoo.ini</c> was present on disk at last verification.</summary>
    public bool HasIni { get; set; }

    /// <summary>UTC timestamp when this installation was added to the launcher.</summary>
    public DateTime AddedUtc { get; init; }

    /// <summary>UTC timestamp of the most recent modification to any non-identity column.</summary>
    public DateTime? ModifiedUtc { get; set; }

    /// <summary>UTC timestamp when the Launch Game button most recently kicked off a successful process start.</summary>
    public DateTime? LastPlayedUtc { get; set; }

    /// <summary>UTC timestamp when this installation most recently became the active installation in the main window.</summary>
    public DateTime? LastOpenedUtc { get; set; }

    /// <summary>Computes the current <see cref="InstallationValidity" /> from <see cref="HasExe" /> and <see cref="HasIni" />.</summary>
    public InstallationValidity Validity => InstallationValidity.From(HasExe, HasIni);
}
```

- [ ] **Step 5: Add `LauncherStartupPreference` SmartEnum (folder: `Settings/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.Settings;

/// <summary>
/// Governs which installation, if any, the launcher resolves on start (SDD §3.1, §7.1).
/// </summary>
public sealed class LauncherStartupPreference : SmartEnum<LauncherStartupPreference>
{
    /// <summary>Open <see cref="LauncherSettings.DefaultInstallationId" /> on start (the standard behaviour).</summary>
    public static readonly LauncherStartupPreference DefaultInstallation    = new("DefaultInstallation",    1);

    /// <summary>Open the installation with the most recent <c>LastPlayedUtc</c>; fall back to default if none.</summary>
    public static readonly LauncherStartupPreference LastPlayedInstallation = new("LastPlayedInstallation", 2);

    /// <summary>Open the installation with the most recent <c>LastOpenedUtc</c>; fall back to default if none.</summary>
    public static readonly LauncherStartupPreference LastOpenedInstallation = new("LastOpenedInstallation", 3);

    /// <summary>Open no installation; present the <c>OpenGameInstallation</c> wireframe.</summary>
    public static readonly LauncherStartupPreference NoInstallation         = new("NoInstallation",         4);

    private LauncherStartupPreference(string name, int id) : base(name, id) { }
}
```

- [ ] **Step 6: Add `LauncherSettings` entity (folder: `Settings/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.Settings;

/// <summary>
/// The single-row launcher settings table (SDD §5.1).
/// </summary>
/// <remarks>
/// Persisted with a <c>CHECK (Id = 1)</c> constraint to enforce singleton storage.
/// </remarks>
public sealed class LauncherSettings
{
    /// <summary>Fixed primary key; always <c>1</c>.</summary>
    public int Id { get; init; } = 1;

    /// <summary>Startup-resolution preference; default is <see cref="LauncherStartupPreference.DefaultInstallation" />.</summary>
    public LauncherStartupPreference LauncherStartupPreference { get; set; } = LauncherStartupPreference.DefaultInstallation;

    /// <summary>When <see langword="true" />, the launcher closes after a successful Launch Game.</summary>
    public bool CloseAfterGameLaunch { get; set; }

    /// <summary>The id of the default installation (nullable when no installations are registered).</summary>
    public Guid? DefaultInstallationId { get; set; }
}
```

- [ ] **Step 7: Add `IniSnapshotKind`, `IniSnapshotTrigger`, `IniValueSource`, `IniValueKind` SmartEnums (folder: `IniSnapshots/`)**

`IniSnapshotKind.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>
/// The three kinds of INI snapshot (SDD §5.2).
/// </summary>
public sealed class IniSnapshotKind : SmartEnum<IniSnapshotKind>
{
    /// <summary>The first-ever parse of <c>zoo.ini</c> when the installation was added. Exactly one per installation.</summary>
    public static readonly IniSnapshotKind Original   = new("Original",   1);

    /// <summary>The launcher's belief about the on-disk values right now. Exactly one per installation.</summary>
    public static readonly IniSnapshotKind Current    = new("Current",    2);

    /// <summary>An archived prior <see cref="Current" /> snapshot. Unbounded per installation.</summary>
    public static readonly IniSnapshotKind Historical = new("Historical", 3);

    private IniSnapshotKind(string name, int id) : base(name, id) { }
}
```

`IniSnapshotTrigger.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>
/// Why a snapshot was captured (SDD §5.2).
/// </summary>
public sealed class IniSnapshotTrigger : SmartEnum<IniSnapshotTrigger>
{
    /// <summary>The installation was just added and its <c>zoo.ini</c> was parsed for the first time.</summary>
    public static readonly IniSnapshotTrigger OriginalImport = new("OriginalImport", 1);

    /// <summary>The user saved INI changes through the launcher's GUI.</summary>
    public static readonly IniSnapshotTrigger LauncherGui    = new("LauncherGui",    2);

    /// <summary>Manual-edit drift was detected on the next open; the prior <c>Current</c> is archived before adoption.</summary>
    public static readonly IniSnapshotTrigger Manual         = new("Manual",         3);

    private IniSnapshotTrigger(string name, int id) : base(name, id) { }
}
```

`IniValueSource.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>
/// How a particular value landed in its snapshot (SDD §5.1 — per-row source flag).
/// </summary>
public sealed class IniValueSource : SmartEnum<IniValueSource>
{
    /// <summary>The value was captured during the first-ever parse of <c>zoo.ini</c>.</summary>
    public static readonly IniValueSource OriginalImport = new("OriginalImport", 1);

    /// <summary>The value was written by the launcher's GUI in the most recent save.</summary>
    public static readonly IniValueSource LauncherGui    = new("LauncherGui",    2);

    /// <summary>The value was detected as a manual edit during drift detection on the next open.</summary>
    public static readonly IniValueSource Manual         = new("Manual",         3);

    private IniValueSource(string name, int id) : base(name, id) { }
}
```

`IniValueKind.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>
/// The strongly typed kind an INI value carries (SDD §5.2).
/// </summary>
/// <remarks>
/// Persisted as a string but parsed by the registry at read time. <see cref="Scenario" /> is reserved for
/// values in the <c>[scenario]</c> section, which always carry an integer that the Scenarios UI maps to
/// <c>Complete</c>/<c>Locked</c>.
/// </remarks>
public sealed class IniValueKind : SmartEnum<IniValueKind>
{
    /// <summary>A boolean (stored as <c>0</c>/<c>1</c>).</summary>
    public static readonly IniValueKind Bool        = new("Bool",        1);

    /// <summary>A non-nullable signed 32-bit integer.</summary>
    public static readonly IniValueKind Int         = new("Int",         2);

    /// <summary>A nullable signed 32-bit integer.</summary>
    public static readonly IniValueKind NullableInt = new("NullableInt", 3);

    /// <summary>A non-nullable string.</summary>
    public static readonly IniValueKind Str         = new("Str",         4);

    /// <summary>A nullable string.</summary>
    public static readonly IniValueKind NullableStr = new("NullableStr", 5);

    /// <summary>A scenario-section integer with <c>Complete</c>/<c>Locked</c> semantics.</summary>
    public static readonly IniValueKind Scenario    = new("Scenario",    6);

    private IniValueKind(string name, int id) : base(name, id) { }
}
```

- [ ] **Step 8: Add `IniValue` entity (folder: `IniSnapshots/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>
/// One INI value row in an <see cref="IniSnapshot" />.
/// </summary>
public sealed class IniValue
{
    /// <summary>Auto-increment surrogate key.</summary>
    public long Id { get; init; }

    /// <summary>The snapshot this row belongs to.</summary>
    public Guid SnapshotId { get; init; }

    /// <summary>The <c>[section]</c> the key is under, e.g. <c>user</c>, <c>ui</c>, <c>scenario</c>.</summary>
    public string Section { get; init; } = string.Empty;

    /// <summary>The key within the section, e.g. <c>showtipsatstartup</c>.</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>The value as a string (parsed by the registry per <see cref="ValueKind" />). Nullable for absent values.</summary>
    public string? Value { get; set; }

    /// <summary>The strongly typed kind of value carried here.</summary>
    public IniValueKind ValueKind { get; init; } = IniValueKind.Str;

    /// <summary>How this value got into its snapshot.</summary>
    public IniValueSource Source { get; set; } = IniValueSource.OriginalImport;
}
```

- [ ] **Step 9: Add `IniSnapshot` entity (folder: `IniSnapshots/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>
/// A point-in-time materialisation of every recognised <c>zoo.ini</c> setting plus the file's raw text
/// (SDD §5.1, §6.3).
/// </summary>
public sealed class IniSnapshot
{
    /// <summary>The snapshot's identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Which of the three kinds this snapshot is (<see cref="IniSnapshotKind" />).</summary>
    public IniSnapshotKind Kind { get; init; } = IniSnapshotKind.Current;

    /// <summary>What triggered the capture (<see cref="IniSnapshotTrigger" />).</summary>
    public IniSnapshotTrigger Trigger { get; init; } = IniSnapshotTrigger.OriginalImport;

    /// <summary>UTC timestamp of capture.</summary>
    public DateTime CapturedUtc { get; init; }

    /// <summary>The raw INI text at capture time, used to re-emit the file with comments and ordering preserved.</summary>
    public string StructureBlob { get; set; } = string.Empty;

    /// <summary>The EAV value rows belonging to this snapshot.</summary>
    public IList<IniValue> Values { get; init; } = new List<IniValue>();
}
```

- [ ] **Step 10: Clean + build**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Source/Erdmier.ZooTycoonLauncher.Domain/Erdmier.ZooTycoonLauncher.Domain.csproj
```

Expected: build SUCCEEDED for the Domain project. The slnx-level build will still fail (other projects not yet created).

- [ ] **Step 11: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Domain
git commit -m "feat(✨): add Domain project with entities and SmartEnums per SDD §5"
```

---

## Task 3: Domain unit tests — `InstallationValidity.From`

**Files:**
- Create: `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/GlobalUsings.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Installations/InstallationValidityTests.cs`

- [ ] **Step 1: Write the failing test (TDD: red first)**

`InstallationValidityTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.Installations;

public sealed class InstallationValidityTests
{
    [Theory]
    [InlineData(true,  true,  "Valid")]
    [InlineData(false, true,  "InvalidNoExe")]
    [InlineData(true,  false, "InvalidNoIni")]
    [InlineData(false, false, "InvalidNoExeOrIni")]
    public void From_MapsHasExeAndHasIniToCorrectValidity(bool hasExe, bool hasIni, string expectedName)
    {
        InstallationValidity result = InstallationValidity.From(hasExe, hasIni);

        result.Name.ShouldBe(expectedName);
    }

    [Fact]
    public void Valid_DisplayNameIsValid()
    {
        InstallationValidity.Valid.DisplayName.ShouldBe("Valid");
        InstallationValidity.Valid.ColourToken.ShouldBe("Green");
    }

    [Fact]
    public void InvalidVariants_AllUseRedColourToken()
    {
        InstallationValidity.InvalidNoExe.ColourToken.ShouldBe("Red");
        InstallationValidity.InvalidNoIni.ColourToken.ShouldBe("Red");
        InstallationValidity.InvalidNoExeOrIni.ColourToken.ShouldBe("Red");
    }
}
```

- [ ] **Step 2: Create the test csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <RootNamespace>Erdmier.ZooTycoonLauncher.Domain.Tests.Unit</RootNamespace>
        <IsPackable>false</IsPackable>
        <IsTestProject>true</IsTestProject>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="xunit" />
        <PackageReference Include="xunit.runner.visualstudio" />
        <PackageReference Include="Shouldly" />
        <PackageReference Include="NSubstitute" />
        <PackageReference Include="coverlet.collector" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\Source\Erdmier.ZooTycoonLauncher.Domain\Erdmier.ZooTycoonLauncher.Domain.csproj" />
    </ItemGroup>
</Project>
```

- [ ] **Step 3: Add `GlobalUsings.cs`**

```csharp
global using Erdmier.ZooTycoonLauncher.Domain.Installations;
global using Erdmier.ZooTycoonLauncher.Domain.Settings;
global using Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;
global using Shouldly;
global using Xunit;
```

- [ ] **Step 4: Run the tests**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj
```

Expected: 6 passed (the four `[InlineData]` rows + two `[Fact]` tests). All pass first time because the implementation was already in Task 2 — this task adds the regression net, not a new behaviour.

- [ ] **Step 5: Commit**

```powershell
git add Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit
git commit -m "test(✅): add Domain unit tests for InstallationValidity truth table"
```

---

## Task 4: Application project skeleton + abstractions

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/GlobalUsings.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IAppStorageLocations.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/ILauncherSettingsRepository.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IInstallationRepository.cs`

- [ ] **Step 1: Create the Application csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <RootNamespace>Erdmier.ZooTycoonLauncher.Application</RootNamespace>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="ErrorOr" />
        <PackageReference Include="FluentValidation" />
        <PackageReference Include="JetBrains.Annotations" />
        <PackageReference Include="Mediator.Abstractions" />
        <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
        <PackageReference Include="System.IO.Abstractions" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Erdmier.ZooTycoonLauncher.Domain\Erdmier.ZooTycoonLauncher.Domain.csproj" />
    </ItemGroup>
</Project>
```

> **Note:** `Microsoft.Extensions.DependencyInjection.Abstractions` is referenced (not the concrete `…DependencyInjection` package) — Application registers extension methods like `AddApplication(IServiceCollection)` but doesn't pull in the container implementation. Add it to `Directory.Packages.props` if not already present.

- [ ] **Step 2: Add the missing package version to `Directory.Packages.props`**

In the existing `<ItemGroup>` after the `Microsoft.Extensions.DependencyInjection` line, add:

```xml
<PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
```

- [ ] **Step 3: Add `GlobalUsings.cs`**

```csharp
global using System.IO.Abstractions;
global using ErrorOr;
global using Erdmier.ZooTycoonLauncher.Domain.Installations;
global using Erdmier.ZooTycoonLauncher.Domain.Settings;
global using Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;
global using JetBrains.Annotations;
global using Mediator;
global using Microsoft.Extensions.DependencyInjection;
```

- [ ] **Step 4: Add `IAppStorageLocations` (folder: `Common/Abstractions/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>
/// Resolves on-disk paths the launcher reads and writes (SDD §4.7).
/// </summary>
/// <remarks>
/// All paths are absolute. Implementations may compute paths from <c>%LOCALAPPDATA%</c> or, in tests,
/// from a temp directory; the application layer must not depend on either resolution strategy.
/// </remarks>
public interface IAppStorageLocations
{
    /// <summary>The application data root — usually <c>%LOCALAPPDATA%\ZooTycoonLauncher\</c>.</summary>
    string AppDataRoot { get; }

    /// <summary>The folder holding launcher and per-installation databases.</summary>
    string DataRoot { get; }

    /// <summary>The folder holding logs.</summary>
    string LogsRoot { get; }

    /// <summary>The fully qualified path to <c>Launcher.db</c>.</summary>
    string LauncherDatabasePath { get; }

    /// <summary>The fully qualified path to the rolling launcher log file.</summary>
    string LauncherLogPath { get; }

    /// <summary>The fully qualified path to a particular installation's database.</summary>
    /// <param name="installationId">The installation's id.</param>
    string InstallationDatabasePath(Guid installationId);

    /// <summary>The fully qualified path to a particular installation's log file.</summary>
    /// <param name="installationId">The installation's id.</param>
    string InstallationLogPath(Guid installationId);
}
```

- [ ] **Step 5: Add `ILauncherSettingsRepository` (folder: `Common/Abstractions/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>
/// Reads and writes the single <see cref="LauncherSettings" /> row.
/// </summary>
public interface ILauncherSettingsRepository
{
    /// <summary>
    /// Returns the current settings, creating the single row with defaults if it does not yet exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current <see cref="LauncherSettings" /> row.</returns>
    Task<LauncherSettings> GetAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists changes to the settings row.
    /// </summary>
    /// <param name="settings">The settings to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(LauncherSettings settings, CancellationToken cancellationToken);
}
```

- [ ] **Step 6: Add `IInstallationRepository` (folder: `Common/Abstractions/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>
/// Reads and writes <see cref="GameInstallation" /> rows in <c>Launcher.db</c>.
/// </summary>
/// <remarks>
/// Implementations in subsequent milestones will add membership operations (Add/Edit/Delete/Fix) — this
/// foundations milestone only exposes the read API needed by the placeholder boot pipeline.
/// </remarks>
public interface IInstallationRepository
{
    /// <summary>Returns every registered installation, ordered alphabetically (case-insensitive) by name.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The installations, or an empty list when none exist.</returns>
    Task<IReadOnlyList<GameInstallation>> GetAllAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 7: Build**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj
```

Expected: build SUCCEEDED.

- [ ] **Step 8: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Application Directory.Packages.props
git commit -m "feat(✨): add Application project with foundational abstractions"
```

---

## Task 5: Infrastructure project skeleton + `AppStorageLocations`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/GlobalUsings.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Storage/AppStorageLocations.cs`

- [ ] **Step 1: Create the Infrastructure csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <RootNamespace>Erdmier.ZooTycoonLauncher.Infrastructure</RootNamespace>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="JetBrains.Annotations" />
        <PackageReference Include="Microsoft.EntityFrameworkCore" />
        <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
        <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
        <PackageReference Include="Serilog" />
        <PackageReference Include="Serilog.Extensions.Hosting" />
        <PackageReference Include="Serilog.Sinks.File" />
        <PackageReference Include="System.IO.Abstractions" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Erdmier.ZooTycoonLauncher.Application\Erdmier.ZooTycoonLauncher.Application.csproj" />
    </ItemGroup>
</Project>
```

- [ ] **Step 2: Add `GlobalUsings.cs`**

```csharp
global using System.IO.Abstractions;
global using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
global using Erdmier.ZooTycoonLauncher.Domain.Installations;
global using Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;
global using Erdmier.ZooTycoonLauncher.Domain.Settings;
global using JetBrains.Annotations;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Serilog;
```

- [ ] **Step 3: Add `AppStorageLocations` (folder: `Common/Storage/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Common.Storage;

/// <summary>
/// Resolves on-disk paths under <c>%LOCALAPPDATA%\ZooTycoonLauncher\</c> (SDD §4.7).
/// </summary>
[UsedImplicitly]
public sealed class AppStorageLocations : IAppStorageLocations
{
    private const string AppFolderName = "ZooTycoonLauncher";

    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initialises a new instance using the supplied file system abstraction so paths can be redirected in tests.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction used to ensure directories exist.</param>
    public AppStorageLocations(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        AppDataRoot = _fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);
        DataRoot = _fileSystem.Path.Combine(AppDataRoot, "Data");
        LogsRoot = _fileSystem.Path.Combine(AppDataRoot, "Logs");
        LauncherDatabasePath = _fileSystem.Path.Combine(DataRoot, "Launcher.db");
        LauncherLogPath = _fileSystem.Path.Combine(LogsRoot, "Launcher.log");

        _fileSystem.Directory.CreateDirectory(DataRoot);
        _fileSystem.Directory.CreateDirectory(LogsRoot);
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.Combine(LogsRoot, "Installations"));
    }

    /// <inheritdoc />
    public string AppDataRoot { get; }

    /// <inheritdoc />
    public string DataRoot { get; }

    /// <inheritdoc />
    public string LogsRoot { get; }

    /// <inheritdoc />
    public string LauncherDatabasePath { get; }

    /// <inheritdoc />
    public string LauncherLogPath { get; }

    /// <inheritdoc />
    public string InstallationDatabasePath(Guid installationId) => _fileSystem.Path.Combine(DataRoot, $"{installationId}.db");

    /// <inheritdoc />
    public string InstallationLogPath(Guid installationId) => _fileSystem.Path.Combine(LogsRoot, "Installations", $"{installationId}.log");
}
```

- [ ] **Step 4: Build**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj
```

Expected: build SUCCEEDED.

- [ ] **Step 5: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure
git commit -m "feat(✨): add Infrastructure project with AppStorageLocations"
```

---

## Task 6: `LauncherDbContext` + entity configurations

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/LauncherDbContext.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Configurations/LauncherSettingsConfiguration.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Configurations/GameInstallationConfiguration.cs`

- [ ] **Step 1: Add `LauncherSettingsConfiguration`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Configurations;

/// <summary>
/// EF Core configuration for <see cref="LauncherSettings" /> — single row enforced by a <c>CHECK</c> constraint.
/// </summary>
[UsedImplicitly]
public sealed class LauncherSettingsConfiguration : Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<LauncherSettings>
{
    /// <inheritdoc />
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<LauncherSettings> builder)
    {
        builder.ToTable("LauncherSettings", b => b.HasCheckConstraint("CK_LauncherSettings_SingletonRow", "\"Id\" = 1"));
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.LauncherStartupPreference)
               .HasConversion(
                    static v => v.Name,
                    static v => LauncherStartupPreference.FromName(v, ignoreCase: false))
               .IsRequired();

        builder.Property(s => s.CloseAfterGameLaunch).IsRequired();
        builder.Property(s => s.DefaultInstallationId);
    }
}
```

- [ ] **Step 2: Add `GameInstallationConfiguration`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Configurations;

/// <summary>
/// EF Core configuration for <see cref="GameInstallation" /> — case-insensitive uniqueness on <c>Name</c> and <c>Path</c>.
/// </summary>
[UsedImplicitly]
public sealed class GameInstallationConfiguration : Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<GameInstallation>
{
    /// <inheritdoc />
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<GameInstallation> builder)
    {
        builder.ToTable("GameInstallations");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Name).IsRequired().UseCollation("NOCASE");
        builder.Property(i => i.Path).IsRequired().UseCollation("NOCASE");
        builder.Property(i => i.HasExe).IsRequired();
        builder.Property(i => i.HasIni).IsRequired();
        builder.Property(i => i.AddedUtc).IsRequired();
        builder.Property(i => i.ModifiedUtc);
        builder.Property(i => i.LastPlayedUtc);
        builder.Property(i => i.LastOpenedUtc);

        builder.HasIndex(i => i.Name).IsUnique();
        builder.HasIndex(i => i.Path).IsUnique();

        builder.Ignore(i => i.Validity);
    }
}
```

- [ ] **Step 3: Add `LauncherDbContext`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher;

/// <summary>
/// EF Core context for <c>Launcher.db</c> — launcher-global settings and the installation registry.
/// </summary>
[UsedImplicitly]
public sealed class LauncherDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    /// <summary>Initialises a new instance with the supplied options.</summary>
    /// <param name="options">The context options (connection string supplied via DI).</param>
    public LauncherDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<LauncherDbContext> options) : base(options) { }

    /// <summary>The single-row settings table.</summary>
    public Microsoft.EntityFrameworkCore.DbSet<LauncherSettings> LauncherSettings => Set<LauncherSettings>();

    /// <summary>The installation registry.</summary>
    public Microsoft.EntityFrameworkCore.DbSet<GameInstallation> GameInstallations => Set<GameInstallation>();

    /// <inheritdoc />
    protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LauncherDbContext).Assembly);
    }

    /// <summary>
    /// Adds the <see cref="LauncherDbContext" /> to the service collection wired to a file-backed SQLite database.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="databasePath">The fully qualified database path; usually <see cref="IAppStorageLocations.LauncherDatabasePath" />.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddLauncherDbContext(IServiceCollection services, string databasePath)
    {
        services.AddDbContext<LauncherDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));
        return services;
    }
}
```

- [ ] **Step 4: Build**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj
```

Expected: build SUCCEEDED.

- [ ] **Step 5: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence
git commit -m "feat(✨): add LauncherDbContext with LauncherSettings and GameInstallations configurations"
```

---

## Task 7: Generate initial EF Core migration for `Launcher.db`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Migrations/*` (generated by EF CLI; do not write by hand)

> **Why a placeholder Desktop project first?** EF Core's `dotnet ef` CLI needs a *startup project* with a `ServiceProvider`/`HostBuilder`. Adding a minimal Desktop entry point now is cheaper than mocking one up later.
>
> But we don't have Desktop yet at this point in the plan. **Resolution:** add a tiny `LauncherDbContextDesignTimeFactory` so the CLI can construct the context without a host. It lives next to the context, marked `[UsedImplicitly]`, and reads the database path from an environment variable or a fixed temp path so it never touches the user's real DB during design-time scaffolding.

- [ ] **Step 1: Add `LauncherDbContextDesignTimeFactory`**

`Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/LauncherDbContextDesignTimeFactory.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher;

/// <summary>
/// Design-time factory used by the EF Core CLI to construct <see cref="LauncherDbContext" /> outside the
/// running application — e.g. when running <c>dotnet ef migrations add</c>.
/// </summary>
/// <remarks>
/// Resolves the connection string from the <c>ZOOLAUNCHER_DESIGNTIME_DB</c> environment variable when set,
/// otherwise falls back to a fixed temp path. Never touches the user's real <c>Launcher.db</c>.
/// </remarks>
[UsedImplicitly]
public sealed class LauncherDbContextDesignTimeFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<LauncherDbContext>
{
    /// <inheritdoc />
    public LauncherDbContext CreateDbContext(string[] args)
    {
        string databasePath = Environment.GetEnvironmentVariable("ZOOLAUNCHER_DESIGNTIME_DB")
            ?? Path.Combine(Path.GetTempPath(), "ZooTycoonLauncher.DesignTime.db");

        Microsoft.EntityFrameworkCore.DbContextOptions<LauncherDbContext> options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<LauncherDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new LauncherDbContext(options);
    }
}
```

Add the matching package version pin to `Directory.Packages.props` if not already there (the design-time factory references `Microsoft.EntityFrameworkCore.Design` via the type `IDesignTimeDbContextFactory`):

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

Add this `<PackageReference>` to the Infrastructure csproj (under the existing items). The `Directory.Packages.props` already pins the version.

- [ ] **Step 2: Install the EF CLI (one-time per machine)**

```powershell
dotnet tool install --global dotnet-ef --version 10.0.*
```

Expected: tool installed or already present. If the EF Core 10 CLI isn't yet GA, install the latest matching preview and note it in the migration commit.

- [ ] **Step 3: Generate the initial migration**

```powershell
dotnet ef migrations add InitialLauncherSchema `
  --project Source/Erdmier.ZooTycoonLauncher.Infrastructure `
  --context LauncherDbContext `
  --output-dir Persistence/Launcher/Migrations
```

Expected: three new files appear under `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Migrations/` — a timestamped `*_InitialLauncherSchema.cs`, a `*_InitialLauncherSchema.Designer.cs`, and `LauncherDbContextModelSnapshot.cs`.

- [ ] **Step 4: Inspect the generated migration**

Open the `*_InitialLauncherSchema.cs` Up method and confirm it:
- Creates the `LauncherSettings` table with `Id` PK and a `CHECK ("Id" = 1)` constraint.
- Creates the `GameInstallations` table with the configured columns, two unique indexes (`Name`, `Path`), both `NOCASE`-collated.

If the `CHECK` or `NOCASE` collation isn't reflected in the migration, the entity configurations need fixing — go back to Task 6, fix, re-run `dotnet ef migrations remove` then `add` again.

- [ ] **Step 5: Build to confirm migrations compile**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj
```

Expected: build SUCCEEDED.

- [ ] **Step 6: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure
git commit -m "feat(✨): add initial EF Core migration for Launcher.db"
```

---

## Task 8: Repository implementations + Serilog + `AddInfrastructure`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Repositories/LauncherSettingsRepository.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Repositories/InstallationRepository.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Logging/SerilogConfiguration.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Extensions/InfrastructureServiceCollectionExtensions.cs`

- [ ] **Step 1: Add `LauncherSettingsRepository`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ILauncherSettingsRepository" /> targeting <c>Launcher.db</c>.
/// </summary>
[UsedImplicitly]
public sealed class LauncherSettingsRepository : ILauncherSettingsRepository
{
    private readonly LauncherDbContext _context;

    /// <summary>Initialises a new instance with the supplied context.</summary>
    /// <param name="context">The launcher database context.</param>
    public LauncherSettingsRepository(LauncherDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<LauncherSettings> GetAsync(CancellationToken cancellationToken)
    {
        LauncherSettings? settings = await _context.LauncherSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new LauncherSettings();
            _context.LauncherSettings.Add(settings);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(LauncherSettings settings, CancellationToken cancellationToken)
    {
        _context.LauncherSettings.Update(settings);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 2: Add `InstallationRepository`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IInstallationRepository" /> targeting <c>Launcher.db</c>.
/// </summary>
[UsedImplicitly]
public sealed class InstallationRepository : IInstallationRepository
{
    private readonly LauncherDbContext _context;

    /// <summary>Initialises a new instance with the supplied context.</summary>
    /// <param name="context">The launcher database context.</param>
    public InstallationRepository(LauncherDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameInstallation>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<GameInstallation> installations = await _context.GameInstallations
            .OrderBy(i => i.Name.ToLower())
            .ToListAsync(cancellationToken);

        return installations;
    }
}
```

- [ ] **Step 3: Add `SerilogConfiguration` (folder: `Logging/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Logging;

/// <summary>
/// Centralised Serilog setup — file-only sink rolling daily under <see cref="IAppStorageLocations.LauncherLogPath" />.
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Builds a Serilog <see cref="LoggerConfiguration" /> targeting the launcher's rolling log file.
    /// </summary>
    /// <param name="locations">Resolved path locations.</param>
    /// <returns>A configured <see cref="LoggerConfiguration" />; call <c>.CreateLogger()</c> on the result.</returns>
    public static LoggerConfiguration Build(IAppStorageLocations locations) =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: locations.LauncherLogPath,
                rollingInterval: Serilog.RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
}
```

- [ ] **Step 4: Add `InfrastructureServiceCollectionExtensions` (folder: `Common/Extensions/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Common.Extensions;

/// <summary>
/// Composition-root extensions that register every Infrastructure service into a service collection.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers Infrastructure services — file system, storage locations, Serilog, EF Core, repositories.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IAppStorageLocations, AppStorageLocations>();

        services.AddSingleton<ILogger>(provider =>
        {
            IAppStorageLocations locations = provider.GetRequiredService<IAppStorageLocations>();
            return SerilogConfiguration.Build(locations).CreateLogger();
        });

        services.AddDbContext<LauncherDbContext>((provider, options) =>
        {
            IAppStorageLocations locations = provider.GetRequiredService<IAppStorageLocations>();
            options.UseSqlite($"Data Source={locations.LauncherDatabasePath}");
        });

        services.AddScoped<ILauncherSettingsRepository, LauncherSettingsRepository>();
        services.AddScoped<IInstallationRepository, InstallationRepository>();

        return services;
    }

    /// <summary>
    /// Runs EF Core migrations against <c>Launcher.db</c>. Call once at application start.
    /// </summary>
    /// <param name="serviceProvider">The composed service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task MigrateLauncherDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        LauncherDbContext context = scope.ServiceProvider.GetRequiredService<LauncherDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
```

- [ ] **Step 5: Build**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj
```

Expected: build SUCCEEDED.

- [ ] **Step 6: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure
git commit -m "feat(✨): add repositories, Serilog config, and AddInfrastructure composition"
```

---

## Task 9: Infrastructure integration test — migrations apply on a fresh DB

**Files:**
- Create: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/GlobalUsings.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Persistence/LauncherDbContextTests.cs`

- [ ] **Step 1: Write the failing test**

`LauncherDbContextTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Persistence;

public sealed class LauncherDbContextTests : IDisposable
{
    private readonly string _databasePath;

    public LauncherDbContextTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"zoolauncher-test-{Guid.NewGuid()}.db");
    }

    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    [Fact]
    public async Task Migrate_OnFreshDatabase_CreatesLauncherSettingsAndGameInstallationsTables()
    {
        await using LauncherDbContext context = BuildContext();

        await context.Database.MigrateAsync();

        bool launcherSettingsExists = await TableExistsAsync(context, "LauncherSettings");
        bool gameInstallationsExists = await TableExistsAsync(context, "GameInstallations");

        launcherSettingsExists.ShouldBeTrue();
        gameInstallationsExists.ShouldBeTrue();
    }

    [Fact]
    public async Task LauncherSettings_RejectsInsertWithIdNotEqualToOne()
    {
        await using LauncherDbContext context = BuildContext();
        await context.Database.MigrateAsync();

        LauncherSettings rogue = new() { Id = 2 };
        context.LauncherSettings.Add(rogue);

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Fact]
    public async Task GameInstallations_Name_IsCaseInsensitiveUnique()
    {
        await using LauncherDbContext context = BuildContext();
        await context.Database.MigrateAsync();

        Guid sharedPathRoot = Guid.NewGuid();
        context.GameInstallations.Add(new GameInstallation
        {
            Id = Guid.CreateVersion7(),
            Name = "Main",
            Path = $"C:/Games/A-{sharedPathRoot}",
            HasExe = true,
            HasIni = true,
            AddedUtc = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();

        context.GameInstallations.Add(new GameInstallation
        {
            Id = Guid.CreateVersion7(),
            Name = "main", // same name, different casing
            Path = $"C:/Games/B-{sharedPathRoot}",
            HasExe = true,
            HasIni = true,
            AddedUtc = DateTime.UtcNow,
        });

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    private LauncherDbContext BuildContext()
    {
        DbContextOptions<LauncherDbContext> options = new DbContextOptionsBuilder<LauncherDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;
        return new LauncherDbContext(options);
    }

    private static async Task<bool> TableExistsAsync(LauncherDbContext context, string tableName)
    {
        await using System.Data.Common.DbConnection connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using System.Data.Common.DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=$name";
        System.Data.Common.DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);
        object? result = await command.ExecuteScalarAsync();
        return result is not null;
    }
}
```

- [ ] **Step 2: Create the test csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <RootNamespace>Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration</RootNamespace>
        <IsPackable>false</IsPackable>
        <IsTestProject>true</IsTestProject>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="xunit" />
        <PackageReference Include="xunit.runner.visualstudio" />
        <PackageReference Include="Shouldly" />
        <PackageReference Include="NSubstitute" />
        <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
        <PackageReference Include="coverlet.collector" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\Source\Erdmier.ZooTycoonLauncher.Infrastructure\Erdmier.ZooTycoonLauncher.Infrastructure.csproj" />
    </ItemGroup>
</Project>
```

- [ ] **Step 3: Add `GlobalUsings.cs`**

```csharp
global using Erdmier.ZooTycoonLauncher.Domain.Installations;
global using Erdmier.ZooTycoonLauncher.Domain.Settings;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher;
global using Microsoft.EntityFrameworkCore;
global using Shouldly;
global using Xunit;
```

- [ ] **Step 4: Run the tests**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj
```

Expected: 3 passed. If the singleton CHECK or NOCASE collation isn't working, the test will tell you which one — fix in `LauncherSettingsConfiguration` / `GameInstallationConfiguration`, regenerate the migration (Task 7), and re-run.

- [ ] **Step 5: Commit**

```powershell
git add Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration
git commit -m "test(✅): add integration tests for LauncherDbContext migrations and constraints"
```

---

## Task 10: Application unit tests project (placeholder)

**Files:**
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Smoke/AssemblyLoadTests.cs`

> This milestone has no Application handlers yet (they show up in P2+). We still set up the test project so subsequent milestones can drop slice tests straight in. A single smoke test guards against an empty test runner.

- [ ] **Step 1: Create the csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <RootNamespace>Erdmier.ZooTycoonLauncher.Application.Tests.Unit</RootNamespace>
        <IsPackable>false</IsPackable>
        <IsTestProject>true</IsTestProject>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="xunit" />
        <PackageReference Include="xunit.runner.visualstudio" />
        <PackageReference Include="Shouldly" />
        <PackageReference Include="NSubstitute" />
        <PackageReference Include="coverlet.collector" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\Source\Erdmier.ZooTycoonLauncher.Application\Erdmier.ZooTycoonLauncher.Application.csproj" />
    </ItemGroup>
</Project>
```

- [ ] **Step 2: Add `GlobalUsings.cs`**

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
global using NSubstitute;
global using Shouldly;
global using Xunit;
```

- [ ] **Step 3: Add a smoke test**

`Smoke/AssemblyLoadTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Smoke;

public sealed class AssemblyLoadTests
{
    [Fact]
    public void ApplicationAssemblyLoadsAndExposesAbstractions()
    {
        Type abstraction = typeof(IAppStorageLocations);

        abstraction.Assembly.GetName().Name.ShouldBe("Erdmier.ZooTycoonLauncher.Application");
    }
}
```

- [ ] **Step 4: Run the test**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
```

Expected: 1 passed.

- [ ] **Step 5: Commit**

```powershell
git add Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m "test(✅): add Application unit tests project with assembly smoke test"
```

---

## Task 11: Architecture tests — dependency direction, one-type-per-file, no-files-at-root

**Files:**
- Create: `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/Erdmier.ZooTycoonLauncher.Tests.Architecture.csproj`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/GlobalUsings.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/DependencyDirectionTests.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/OneTypePerFileTests.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/NoFilesAtAssemblyRootTests.cs`

- [ ] **Step 1: Create the csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <RootNamespace>Erdmier.ZooTycoonLauncher.Tests.Architecture</RootNamespace>
        <IsPackable>false</IsPackable>
        <IsTestProject>true</IsTestProject>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="xunit" />
        <PackageReference Include="xunit.runner.visualstudio" />
        <PackageReference Include="Shouldly" />
        <PackageReference Include="NetArchTest.Rules" />
        <PackageReference Include="coverlet.collector" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\Source\Erdmier.ZooTycoonLauncher.Domain\Erdmier.ZooTycoonLauncher.Domain.csproj" />
        <ProjectReference Include="..\..\Source\Erdmier.ZooTycoonLauncher.Application\Erdmier.ZooTycoonLauncher.Application.csproj" />
        <ProjectReference Include="..\..\Source\Erdmier.ZooTycoonLauncher.Infrastructure\Erdmier.ZooTycoonLauncher.Infrastructure.csproj" />
    </ItemGroup>
</Project>
```

> The Desktop project reference will be added in Task 13 once Desktop exists. Architecture rules covering Desktop are also added in Task 13.

- [ ] **Step 2: Add `GlobalUsings.cs`**

```csharp
global using NetArchTest.Rules;
global using Shouldly;
global using Xunit;
```

- [ ] **Step 3: Add `DependencyDirectionTests`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

public sealed class DependencyDirectionTests
{
    private const string DomainAssembly         = "Erdmier.ZooTycoonLauncher.Domain";
    private const string ApplicationAssembly    = "Erdmier.ZooTycoonLauncher.Application";
    private const string InfrastructureAssembly = "Erdmier.ZooTycoonLauncher.Infrastructure";

    [Fact]
    public void Domain_DoesNotReferenceApplicationOrInfrastructure()
    {
        TestResult result = Types.InAssembly(typeof(Domain.Installations.GameInstallation).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationAssembly, InfrastructureAssembly)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructure()
    {
        TestResult result = Types.InAssembly(typeof(Application.Common.Abstractions.IAppStorageLocations).Assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Application_DoesNotReferenceEfCoreOrSerilogOrAvalonia()
    {
        TestResult result = Types.InAssembly(typeof(Application.Common.Abstractions.IAppStorageLocations).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Serilog", "Avalonia")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(BuildFailureMessage(result));
    }

    private static string BuildFailureMessage(TestResult result) =>
        result.FailingTypeNames is null
            ? "Dependency-direction rule violated."
            : $"Dependency-direction rule violated by:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", result.FailingTypeNames)}";
}
```

- [ ] **Step 4: Add `OneTypePerFileTests`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

public sealed class OneTypePerFileTests
{
    public static IEnumerable<object[]> SourceProjectAssemblies()
    {
        yield return [typeof(Domain.Installations.GameInstallation).Assembly];
        yield return [typeof(Application.Common.Abstractions.IAppStorageLocations).Assembly];
        yield return [typeof(Infrastructure.Common.Storage.AppStorageLocations).Assembly];
    }

    [Theory]
    [MemberData(nameof(SourceProjectAssemblies))]
    public void EveryPublicTypeHasAFileNamedAfterIt(System.Reflection.Assembly assembly)
    {
        string assemblyDirectory = Path.GetDirectoryName(assembly.Location)!;
        string projectDirectory = ResolveProjectDirectoryFromBinPath(assemblyDirectory, assembly.GetName().Name!);

        IEnumerable<Type> publicTypes = assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsNested && !t.IsCompilerGenerated());

        List<string> violations = new();
        foreach (Type type in publicTypes)
        {
            string expectedFileName = StripGenericArity(type.Name) + ".cs";
            string[] matches = Directory.GetFiles(projectDirectory, expectedFileName, SearchOption.AllDirectories);
            if (matches.Length == 0)
            {
                violations.Add($"{type.FullName} — no file named {expectedFileName} in {projectDirectory}");
            }
        }

        violations.ShouldBeEmpty(BuildFailureMessage(violations));
    }

    private static string ResolveProjectDirectoryFromBinPath(string binDir, string assemblyName)
    {
        DirectoryInfo? current = new DirectoryInfo(binDir);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, assemblyName + ".csproj");
            if (File.Exists(candidate))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not locate project directory for {assemblyName} starting from {binDir}.");
    }

    private static string StripGenericArity(string name)
    {
        int tickIndex = name.IndexOf('`');
        return tickIndex < 0 ? name : name[..tickIndex];
    }

    private static string BuildFailureMessage(IReadOnlyCollection<string> violations) =>
        violations.Count == 0
            ? string.Empty
            : $"One-type-per-file rule violated by:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", violations)}";
}

internal static class TypeExtensions
{
    public static bool IsCompilerGenerated(this Type type) =>
        type.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false).Length > 0;
}
```

- [ ] **Step 5: Add `NoFilesAtAssemblyRootTests`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

public sealed class NoFilesAtAssemblyRootTests
{
    public static IEnumerable<object[]> SourceProjectDirectories()
    {
        yield return [ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Domain")];
        yield return [ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Application")];
        yield return [ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Infrastructure")];
    }

    [Theory]
    [MemberData(nameof(SourceProjectDirectories))]
    public void NoCsFileSitsAtProjectRoot(string projectDirectory)
    {
        string[] rootCsFiles = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(p => !Path.GetFileName(p).Equals("GlobalUsings.cs", StringComparison.Ordinal))
            .ToArray();

        rootCsFiles.ShouldBeEmpty(
            $"Files at project root in {Path.GetFileName(projectDirectory)}:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", rootCsFiles.Select(Path.GetFileName))}");
    }

    private static string ResolveProjectDirectory(string assemblyName)
    {
        DirectoryInfo current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "Source", assemblyName);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not locate Source/{assemblyName} from {AppContext.BaseDirectory}.");
    }
}
```

- [ ] **Step 6: Run the architecture tests**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/Erdmier.ZooTycoonLauncher.Tests.Architecture.csproj
```

Expected: all tests pass. If any one-type-per-file or no-files-at-root violations show up, fix the offending files (move them into the appropriate subfolder, split combined files) and re-run.

- [ ] **Step 7: Commit**

```powershell
git add Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture
git commit -m "test(✅): pin dependency direction, one-type-per-file, and no-files-at-root architecture rules"
```

---

## Task 12: Desktop project skeleton + Avalonia bootstrap + Classic.Avalonia theme

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Program.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/App.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/App.axaml.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/app.manifest`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Assets/.gitkeep`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/ViewLocator.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/ViewModelBase.cs`

- [ ] **Step 1: Create the Desktop csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <RootNamespace>Erdmier.ZooTycoonLauncher.Desktop</RootNamespace>
        <AssemblyName>Erdmier.ZooTycoonLauncher.Desktop</AssemblyName>
        <ApplicationManifest>app.manifest</ApplicationManifest>
        <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    </PropertyGroup>

    <ItemGroup>
        <AvaloniaResource Include="Assets\**" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia" />
        <PackageReference Include="Avalonia.Desktop" />
        <PackageReference Include="Avalonia.Fonts.Inter" />
        <PackageReference Include="AvaloniaUI.DiagnosticsSupport">
            <IncludeAssets Condition="'$(Configuration)' != 'Debug'">None</IncludeAssets>
            <PrivateAssets Condition="'$(Configuration)' != 'Debug'">All</PrivateAssets>
        </PackageReference>
        <PackageReference Include="Classic.Avalonia.Theme" />
        <PackageReference Include="Classic.Avalonia.Theme.DataGrid" />
        <PackageReference Include="Classic.Avalonia.Theme.Dock" />
        <PackageReference Include="CommunityToolkit.Mvvm" />
        <PackageReference Include="JetBrains.Annotations" />
        <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Erdmier.ZooTycoonLauncher.Application\Erdmier.ZooTycoonLauncher.Application.csproj" />
        <ProjectReference Include="..\Erdmier.ZooTycoonLauncher.Infrastructure\Erdmier.ZooTycoonLauncher.Infrastructure.csproj" />
    </ItemGroup>
</Project>
```

- [ ] **Step 2: Add `app.manifest`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
    <assemblyIdentity version="1.0.0.0" name="Erdmier.ZooTycoonLauncher.Desktop"/>
    <application xmlns="urn:schemas-microsoft-com:asm.v3">
        <windowsSettings>
            <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true</dpiAware>
        </windowsSettings>
    </application>
</assembly>
```

- [ ] **Step 3: Add `Assets/.gitkeep`** (empty file)

- [ ] **Step 4: Add `GlobalUsings.cs`**

```csharp
global using Avalonia;
global using Avalonia.Controls;
global using Avalonia.Markup.Xaml;
global using CommunityToolkit.Mvvm.ComponentModel;
global using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Common.Extensions;
global using JetBrains.Annotations;
global using Microsoft.Extensions.DependencyInjection;
```

- [ ] **Step 5: Add `ViewModelBase` (folder: `ViewModels/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels;

/// <summary>
/// Base class for all view models — extends <see cref="ObservableObject" /> with shared utilities.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject;
```

- [ ] **Step 6: Add `MainWindowViewModel` (folder: `ViewModels/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels;

/// <summary>
/// The main window's view model. In this foundations milestone it only carries a placeholder banner string;
/// subsequent milestones replace the banner with the state-dispatch surface specified by SDD §9.2.
/// </summary>
[UsedImplicitly]
public sealed partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>Placeholder banner string rendered until the state surface lands in P3.</summary>
    [ObservableProperty]
    public partial string Banner { get; set; } = "Zoo Tycoon Launcher — foundations build";
}
```

- [ ] **Step 7: Add `ViewLocator` (folder: `Composition/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>
/// Maps each <c>*ViewModel</c> instance to its corresponding <c>*View</c> by name (SDD §4.3, §9.2).
/// </summary>
public sealed class ViewLocator : Avalonia.Controls.Templates.IDataTemplate
{
    /// <inheritdoc />
    public Control? Build(object? data)
    {
        if (data is null)
        {
            return null;
        }

        string viewModelTypeName = data.GetType().FullName ?? string.Empty;
        string viewTypeName = viewModelTypeName.Replace("ViewModel", "View", StringComparison.Ordinal);
        Type? viewType = Type.GetType(viewTypeName);

        if (viewType is null)
        {
            return new TextBlock { Text = $"View not found: {viewTypeName}" };
        }

        return (Control)Activator.CreateInstance(viewType)!;
    }

    /// <inheritdoc />
    public bool Match(object? data) => data is ViewModels.ViewModelBase;
}
```

- [ ] **Step 8: Add `App.axaml`**

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:composition="using:Erdmier.ZooTycoonLauncher.Desktop.Composition"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.App"
             RequestedThemeVariant="Default">
    <Application.DataTemplates>
        <composition:ViewLocator />
    </Application.DataTemplates>

    <Application.Styles>
        <ClassicTheme xmlns="https://classic.avaloniaui.net" />
        <StyleInclude Source="avares://Classic.Avalonia.Theme.DataGrid/DataGrid.axaml" />
        <StyleInclude Source="avares://Classic.Avalonia.Theme.Dock/Dock.axaml" />
    </Application.Styles>
</Application>
```

- [ ] **Step 9: Add `App.axaml.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop;

/// <summary>
/// The Avalonia application class. Wires DI in <see cref="OnFrameworkInitializationCompleted" />.
/// </summary>
public sealed partial class App : Avalonia.Application
{
    /// <summary>The composed service provider; <see langword="null" /> until framework initialisation completes.</summary>
    public static IServiceProvider? Services { get; private set; }

    /// <inheritdoc />
    public override void Initialize() => Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        ServiceCollection services = new();
        services.AddInfrastructure();
        services.AddDesktop();

        ServiceProvider provider = services.BuildServiceProvider();
        Services = provider;

        // Run migrations synchronously here — the application can't usefully start without the DB.
        provider.MigrateLauncherDatabaseAsync(CancellationToken.None).GetAwaiter().GetResult();

        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            ViewModels.MainWindowViewModel viewModel = provider.GetRequiredService<ViewModels.MainWindowViewModel>();
            desktop.MainWindow = new Views.MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 10: Add `DesktopServiceCollectionExtensions` (folder: `Composition/`)**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>
/// Composition-root extensions that register every Desktop view model into a service collection.
/// </summary>
public static class DesktopServiceCollectionExtensions
{
    /// <summary>Registers Desktop view models.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddDesktop(this IServiceCollection services)
    {
        services.AddTransient<ViewModels.MainWindowViewModel>();
        return services;
    }
}
```

- [ ] **Step 11: Add `Views/MainWindow.axaml`**

```xml
<ClassicWindow xmlns="https://github.com/avaloniaui"
               xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
               xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels"
               xmlns:classic="https://classic.avaloniaui.net"
               x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.MainWindow"
               x:DataType="vm:MainWindowViewModel"
               Title="Zoo Tycoon Launcher"
               Width="800"
               Height="600">
    <Grid>
        <TextBlock Text="{Binding Banner}"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center"
                   FontSize="20" />
    </Grid>
</ClassicWindow>
```

> **Note:** the actual element name for `ClassicWindow` depends on Classic.Avalonia's exposed namespace; if `<ClassicWindow>` doesn't resolve at build time, replace the root with `<classic:ClassicWindow …>` and add the `xmlns:classic="https://classic.avaloniaui.net"` namespace. The author of Classic.Avalonia ships either name conventionally; pick whichever the package exposes.

- [ ] **Step 12: Add `Views/MainWindow.axaml.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views;

/// <summary>
/// The application's main window — chrome only. State views are hosted by subsequent milestones via a ContentControl per SDD §9.2.
/// </summary>
public sealed partial class MainWindow : Avalonia.Controls.Window
{
    /// <summary>Initialises a new instance.</summary>
    public MainWindow() => Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
}
```

> **Note:** the base type (`Window` vs `ClassicWindow`) must match whichever element the XAML root resolves to. If you used `<classic:ClassicWindow>` in Step 11, change `: Avalonia.Controls.Window` here to whatever Classic.Avalonia's window type fully qualifies as (likely `Classic.CommonControls.ClassicWindow` or similar — confirm by reading the Classic.Avalonia 11.3.0.3 README in NuGet).

- [ ] **Step 13: Add `Program.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop;

/// <summary>
/// Application entry point. Standard Avalonia bootstrap.
/// </summary>
public static class Program
{
    /// <summary>The Windows STA entry point.</summary>
    [ STAThread ]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>Builds the Avalonia <see cref="AppBuilder" />.</summary>
    /// <returns>The configured app builder.</returns>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
                  .UsePlatformDetect()
                  .WithInterFont()
                  .LogToTrace();
}
```

- [ ] **Step 14: Build the whole solution**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: every project builds. If the Classic.Avalonia `<ClassicWindow>` root element doesn't resolve, troubleshoot per the notes in Steps 11/12 (probably needs `xmlns:classic` and `classic:ClassicWindow` plus the matching base type).

- [ ] **Step 15: Run the launcher**

```powershell
dotnet run --project Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj
```

Expected:
- A Windows-95-styled window opens with the title "Zoo Tycoon Launcher" and the banner "Zoo Tycoon Launcher — foundations build" centred.
- `%LOCALAPPDATA%\ZooTycoonLauncher\Data\Launcher.db` exists on disk.
- `%LOCALAPPDATA%\ZooTycoonLauncher\Logs\Launcher.log` exists (it may be empty if nothing has logged yet).
- Closing the window cleanly exits the process with code 0.

If the Classic theme renders as Fluent (i.e. you don't see Windows-95 chrome), the theme isn't loading — check `App.axaml` for the correct `<ClassicTheme>` element name and namespace.

- [ ] **Step 16: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop
git commit -m "feat(✨): add Desktop project with Avalonia bootstrap and Classic.Avalonia theme"
```

---

## Task 13: Extend architecture tests to cover Desktop

**Files:**
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/Erdmier.ZooTycoonLauncher.Tests.Architecture.csproj`
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/DependencyDirectionTests.cs`
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/OneTypePerFileTests.cs`
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/NoFilesAtAssemblyRootTests.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/MainWindowSizeTests.cs`

- [ ] **Step 1: Add the Desktop project reference**

In `Erdmier.ZooTycoonLauncher.Tests.Architecture.csproj`, inside the existing `<ItemGroup>` of `ProjectReference`, add:

```xml
<ProjectReference Include="..\..\Source\Erdmier.ZooTycoonLauncher.Desktop\Erdmier.ZooTycoonLauncher.Desktop.csproj" />
```

- [ ] **Step 2: Add the Desktop assembly to the existing `OneTypePerFileTests.SourceProjectAssemblies` member data**

Add this line to the `yield return` block:

```csharp
yield return [typeof(Erdmier.ZooTycoonLauncher.Desktop.App).Assembly];
```

- [ ] **Step 3: Add the Desktop directory to `NoFilesAtAssemblyRootTests.SourceProjectDirectories` member data**

Add this line:

```csharp
yield return [ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Desktop")];
```

- [ ] **Step 4: Add `Desktop`-specific dependency rules to `DependencyDirectionTests`**

Append these two methods:

```csharp
[Fact]
public void Desktop_OnlyReferencesInfrastructureFromCompositionNamespace()
{
    TestResult result = Types.InAssembly(typeof(Erdmier.ZooTycoonLauncher.Desktop.App).Assembly)
        .That()
        .DoNotResideInNamespace("Erdmier.ZooTycoonLauncher.Desktop.Composition")
        .ShouldNot()
        .HaveDependencyOn(InfrastructureAssembly)
        .GetResult();

    result.IsSuccessful.ShouldBeTrue(BuildFailureMessage(result));
}
```

- [ ] **Step 5: Add `MainWindowSizeTests` for the SDD §11.4 cap**

```csharp
namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

public sealed class MainWindowSizeTests
{
    /// <summary>SDD §11.4 caps MainWindow.axaml at 100 lines to keep it host-only.</summary>
    [Fact]
    public void MainWindowAxaml_DoesNotExceed100Lines()
    {
        string projectRoot = ResolveDesktopProjectDirectory();
        string mainWindowPath = Path.Combine(projectRoot, "Views", "MainWindow.axaml");

        File.Exists(mainWindowPath).ShouldBeTrue($"Expected MainWindow.axaml at {mainWindowPath}.");

        int lineCount = File.ReadAllLines(mainWindowPath).Length;

        lineCount.ShouldBeLessThanOrEqualTo(100, $"MainWindow.axaml has {lineCount} lines — cap is 100 (SDD §11.4).");
    }

    private static string ResolveDesktopProjectDirectory()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "Source", "Erdmier.ZooTycoonLauncher.Desktop");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not locate Source/Erdmier.ZooTycoonLauncher.Desktop from {AppContext.BaseDirectory}.");
    }
}
```

> The every-VM-has-a-View architecture test (SDD §11.4) lands when P3 adds the state view models — at this point MainWindowViewModel is the only Desktop VM and it has its matching MainWindow.axaml, so the test would be trivial; defer to P3.

- [ ] **Step 6: Run all architecture tests**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/Erdmier.ZooTycoonLauncher.Tests.Architecture.csproj
```

Expected: all tests pass. If `Desktop_OnlyReferencesInfrastructureFromCompositionNamespace` fails, find the offending file and either move it under `Composition/` or refactor it to not depend on Infrastructure.

- [ ] **Step 7: Commit**

```powershell
git add Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture
git commit -m "test(✅): extend architecture tests to cover Desktop project"
```

---

## Task 14: Full clean+build+test sanity sweep

- [ ] **Step 1: Clean the solution**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx
```

Expected: clean SUCCEEDED for all projects.

- [ ] **Step 2: Build the solution**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: build SUCCEEDED, zero warnings (`TreatWarningsAsErrors=true` would have failed otherwise).

- [ ] **Step 3: Run every test project**

```powershell
dotnet test Erdmier.ZooTycoonLauncher.slnx
```

Expected: all four test projects discover and run. Summary should show:
- Domain.Tests.Unit: 6 passed (InstallationValidity truth table + colour tokens).
- Application.Tests.Unit: 1 passed (assembly smoke).
- Infrastructure.Tests.Integration: 3 passed (migrations, singleton CHECK, NOCASE).
- Tests.Architecture: 7+ passed (depending on the Theory member-data expansions).

- [ ] **Step 4: Run the launcher one last time**

```powershell
dotnet run --project Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj
```

Expected: Windows-95-styled window opens, banner reads "Zoo Tycoon Launcher — foundations build", `Launcher.db` exists with `LauncherSettings` and `GameInstallations` tables (verify with an external SQLite browser if curious), `Launcher.log` exists. Close cleanly.

- [ ] **Step 5: Final foundations commit (no code changes — just a tag-worthy milestone marker)**

There's no code to commit at this step; the prior 13 task commits cover everything. If you prefer a dated checkpoint commit on top of the series:

```powershell
git commit --allow-empty -m "chore(🔧): foundations milestone complete — solution builds, tests green, app launches"
```

(Skip this step if you don't want an empty commit.)

---

## Self-review

**Spec coverage** (SDD § → which task covers it):

| SDD reference | Plan task |
|---|---|
| §4.1 architectural style | T1 (CPM packages), T2-T5 (per-layer projects) |
| §4.2 solution structure | T1 (slnx), T2/T4/T5/T12 (projects), T3/T9/T10/T11/T13 (tests) |
| §4.3 presentation | T12 (Avalonia + Classic.Avalonia + ViewLocator) |
| §4.4 mediation | T1 (Mediator packages pinned); first actual handler arrives in P2 |
| §4.5 cross-cutting libraries | T1 (all pinned in CPM) |
| §4.6 dependencies and licensing | T1 (CPM) |
| §4.7 storage locations | T5 (`AppStorageLocations`) |
| §5.1 entities | T2 (`GameInstallation`, `LauncherSettings`, `IniSnapshot`, `IniValue`) |
| §5.2 SmartEnums | T2 (every SmartEnum) |
| §6.1-6.3 persistence | T6/T7 (`LauncherDbContext`, configurations, initial migration), T8 (repos) |
| §6.4 migrations | T7 (generation), T8 (`MigrateLauncherDatabaseAsync`) |
| §11.1-11.4 testing | T3 (Domain unit), T9 (Infra integration), T10 (App unit placeholder), T11/T13 (Architecture) |
| §11.4 architecture tests | T11 (dependency direction, one-type-per-file, no-files-at-root); T13 (Desktop scope + MainWindow size cap) |
| §9.2 view composition | T12 (`ViewLocator` + folder layout); state views land in P3 |

**Gaps acknowledged (deliberate — handled in later milestones):**
- INI snapshot tables and `InstallationDbContext` — P4.
- The boot pipeline, `BootCommand`/`BootHandler`, Mediator wiring — P2/P3.
- Installation locator (registry + Program Files) — P2.
- The five main-window state UserControls and the every-VM-has-a-View architecture test — P3.

**Placeholder scan:** every code step contains complete code; every command step contains an exact command. The only "TBC" is in EF Core version pinning (Task 1), which is a deliberate calibration note with a clear escape hatch (prerelease + commit-message note).

**Type consistency:** method signatures and property names used across tasks match — `InstallationValidity.From(bool, bool)`, `AppStorageLocations(IFileSystem)`, `LauncherDbContext(DbContextOptions<LauncherDbContext>)`, `AddInfrastructure(this IServiceCollection)`, `AddDesktop(this IServiceCollection)`, `MigrateLauncherDatabaseAsync(this IServiceProvider, CancellationToken)`.

---

## Plan complete

Saved to `docs/superpowers/plans/2026-05-26-foundations-and-scaffolding.md`. Two execution options:

1. **Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration.
2. **Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints.

Per your standing preference, I'll default to **Subagent-Driven** when you say go. Which approach?
