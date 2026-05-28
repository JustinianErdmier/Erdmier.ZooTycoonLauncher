# Installation Lifecycle (Core) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Land the headless half of the Installation Lifecycle MVP slice (SDD §7.2 + §8.5) — every Add / Edit / Delete / Fix / Verify / Locate / Set-Default operation exposed as a Mediator command or query in the Application layer, with EF Core + filesystem implementations in Infrastructure, all unit-and-integration tested. No Desktop dialogues yet — they ship as a follow-on plan once this core is solid.

**Architecture:** Vertical-slice CQRS via source-generated `Mediator`; handlers return `ErrorOr<T>`. FluentValidation validators run as a Mediator pipeline behaviour. Per-installation databases (`{installationId}.db`) get their own `InstallationDbContext` with `IniSnapshot` / `IniValue` configurations and an initial migration — the schema lands now so the future INI slice can populate it without retrofitting. INI snapshot capture on Add is stubbed by a `NullIniSnapshotService` whose only job is to log a deferred-work warning; the INI slice replaces it. Locator + verifier + registry reader are introduced behind interfaces so the future startup-flow slice can wire them into `BootHandler`.

**Tech Stack:** .NET 10, C# 13, EF Core 10 + SQLite, Mediator 2.1.7 (source-generated), FluentValidation 11.10, ErrorOr 2.0.1, Ardalis.SmartEnum 8.2, System.IO.Abstractions 21.0, CommunityToolkit.Mvvm messaging (interface only — VM consumers arrive in the UI slice).

---

## Scope notes

- **In scope.** All Application slices (Add, Update, Delete, Relocate, SetDefault, Verify, Locate, GetAll, GetById). All Infrastructure implementations (verifier, locator, registry reader, per-installation DB context + factory, repository extensions, null snapshot service). `InstallationDbContext` schema + initial migration. AddApplication() DI extension wired into `AppStartup`. New tests across Application.Tests.Unit and Infrastructure.Tests.Integration.
- **Out of scope.** Desktop dialogues (Installation Manager, Add / Edit / Info / Fix / Delete) — separate plan. INI parsing / snapshot writing — separate plan. Boot pipeline wiring (the startup state machine that *consumes* `IInstallationLocator` and `IInstallationVerifier`) — separate plan. Fix dialogue's no-INI sub-flows (`Create Default`, `Locate Manually`, `Copy From Another Installation`) — INI slice.
- **Deferred behaviours.** `AddInstallationHandler` step 4 ("parse `zoo.ini` and write `Original` + `Current` snapshots") is replaced with a call to `IIniSnapshotService.CaptureOriginalAsync` whose `NullIniSnapshotService` impl returns `Success` and emits a `Warning` log. When the INI slice lands it swaps in a real impl with no signature changes.

---

## Files overview

### Modify

- `Directory.Packages.props` — add three missing `<PackageVersion>` lines.
- `Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj` — add `Mediator.SourceGenerator` + `FluentValidation.DependencyInjectionExtensions`.
- `Source/Erdmier.ZooTycoonLauncher.Application/GlobalUsings.cs` — add new namespaces.
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IInstallationRepository.cs` — extend with Add / Update / Delete / GetById / ExistsByName / ExistsByPath / FindDefaultPromotionCandidate.
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/ILauncherSettingsRepository.cs` — no change (kept for context; defaults flow through `UpdateAsync`).
- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj` — no change.
- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/GlobalUsings.cs` — add new namespaces.
- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Extensions/InfrastructureServiceCollectionExtensions.cs` — register new services + add `MigrateInstallationDatabaseAsync` extension.
- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Repositories/InstallationRepository.cs` — implement extended interface.
- `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AppStartup.cs` — call `services.AddApplication()` in addition to `AddInfrastructure()` + `AddDesktop()`.

### Create — Application

- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IInstallationVerifier.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IInstallationLocator.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IRegistryReader.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniSnapshotService.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IInstallationDbContextFactory.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Behaviours/ValidationBehaviour.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Extensions/ApplicationServiceCollectionExtensions.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Messaging/InstallationAddedMessage.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Messaging/InstallationChangedMessage.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Messaging/InstallationDeletedMessage.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Messaging/DefaultInstallationChangedMessage.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/InstallationSummary.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/LocatedDirectory.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/VerificationResult.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/GetAll/GetAllInstallationsQuery.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/GetAll/GetAllInstallationsHandler.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/GetById/GetInstallationByIdQuery.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/GetById/GetInstallationByIdHandler.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Verify/VerifyInstallationQuery.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Verify/VerifyInstallationHandler.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Locate/LocateZooTycoonQuery.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Locate/LocateZooTycoonHandler.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Add/AddInstallationCommand.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Add/AddInstallationValidator.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Add/AddInstallationHandler.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Add/AddInstallationResult.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Update/UpdateInstallationCommand.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Update/UpdateInstallationValidator.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Update/UpdateInstallationHandler.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/SetDefault/SetDefaultInstallationCommand.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/SetDefault/SetDefaultInstallationHandler.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Delete/DeleteInstallationCommand.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Delete/DeleteInstallationHandler.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Relocate/RelocateInstallationCommand.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Relocate/RelocateInstallationValidator.cs`
- `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Relocate/RelocateInstallationHandler.cs`

### Create — Infrastructure

- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Discovery/InstallationVerifier.cs`
- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Discovery/InstallationLocator.cs`
- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Discovery/WindowsRegistryReader.cs`
- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/IniSnapshots/NullIniSnapshotService.cs`
- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/InstallationDbContext.cs`
- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/InstallationDbContextFactory.cs`
- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/InstallationDbContextDesignTimeFactory.cs`
- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/Configurations/IniSnapshotConfiguration.cs`
- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/Configurations/IniValueConfiguration.cs`
- `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/Migrations/*` (EF-generated)

### Create — Tests

- `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Common/TestMediator.cs` (test helper)
- `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/GetAllInstallationsHandlerTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/GetInstallationByIdHandlerTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/VerifyInstallationHandlerTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/LocateZooTycoonHandlerTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/AddInstallationHandlerTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/AddInstallationValidatorTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/UpdateInstallationHandlerTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/UpdateInstallationValidatorTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/SetDefaultInstallationHandlerTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/DeleteInstallationHandlerTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/RelocateInstallationHandlerTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/RelocateInstallationValidatorTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Discovery/InstallationVerifierTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Discovery/InstallationLocatorTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Persistence/InstallationDbContextTests.cs`
- `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Persistence/InstallationRepositoryTests.cs`

---

## Build & test commands

```powershell
# Clean + build (Source + Tests only; the References/Ref legacy project fails CPM — known issue)
dotnet build Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj
dotnet build Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj
dotnet build Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj

# Run a single test project
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj
dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/Erdmier.ZooTycoonLauncher.Tests.Architecture.csproj

# Add an EF migration for the per-installation context
dotnet ef migrations add InitialInstallationSchema `
  --project Source/Erdmier.ZooTycoonLauncher.Infrastructure `
  --context InstallationDbContext `
  --output-dir Persistence/Installation/Migrations
```

---

## Conventions (apply to every task)

- **PowerShell only** for shell commands.
- **British English** in identifiers, doc comments, commit messages (`Initialise`, `Behaviour`, `Colour`, `Minimise`).
- **One type per file**; folder mirrors namespace; no files at any project root except `GlobalUsings.cs`.
- **File-scoped namespaces.**
- **XML doc comments on every public member/type.** `<c>…</c>` tags carry no inside whitespace.
- **Spaced bracket attributes:** `[ STAThread ]`. **Do NOT add `[ UsedImplicitly ]`** anywhere — even on DI-registered classes, EF configurations, or designer-only types. The user adds it himself when ReSharper genuinely flags a type as dead.
- **UTC for every timestamp.** Storage and properties carry the `Utc` suffix.
- **Conventional commit + gitmoji:** `feat(✨)`, `fix(🐛)`, `refactor(♻️)`, `test(✅)`, `chore(🔧)`, `docs(📝)`, `style(🎨)`, `perf(⚡)`, `build(📦)`, `ci(👷)`.
- **Commit per task.** Each task ends with a single commit. Do not commit silently.
- **Source-gen Mediator.** Handlers implement `Mediator.IRequestHandler<TRequest, TResponse>` / `INotificationHandler<TNotification>`. `Microsoft.Extensions.DependencyInjection.MediatorDependencyInjectionExtensions.AddMediator(...)` is generated into the Application assembly by `Mediator.SourceGenerator`.
- **Repository writes go through `SaveChangesAsync(cancellationToken)`.** Don't use `EntityState` mutation directly except via the matching DbSet method (`Add`, `Update`, `Remove`).
- **Tests use Shouldly + NSubstitute + xUnit.** Avoid setup boilerplate when a single-line arrange is enough.

---

## Task 1: Central packages + Application csproj

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj`

- [ ] **Step 1: Confirm the three packages we need already have central pins**

`Directory.Packages.props` already has `Mediator.Abstractions`, `Mediator.SourceGenerator`, `FluentValidation`, and `FluentValidation.DependencyInjectionExtensions`. Verify by reading the file; if any is missing, add the line.

Expected pins (already in foundations):
```xml
<PackageVersion Include="Mediator.Abstractions" Version="2.1.7" />
<PackageVersion Include="Mediator.SourceGenerator" Version="2.1.7" />
<PackageVersion Include="FluentValidation" Version="11.10.0" />
<PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.10.0" />
```

- [ ] **Step 2: Extend the Application csproj**

Open `Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj` and add inside the first `<ItemGroup>` (the `PackageReference` list), keeping items alphabetically ordered:

```xml
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
<PackageReference Include="Mediator.SourceGenerator">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

The source generator must be `PrivateAssets="all"` so downstream assemblies don't double-reference the analyzer.

- [ ] **Step 3: Build the Application project**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj
```

Expected: SUCCEEDED.

- [ ] **Step 4: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj Directory.Packages.props
git commit -m "build(📦): wire Mediator source generator and FluentValidation DI into Application"
```

---

## Task 2: Application messaging types

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Messaging/InstallationAddedMessage.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Messaging/InstallationChangedMessage.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Messaging/InstallationDeletedMessage.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Messaging/DefaultInstallationChangedMessage.cs`

- [ ] **Step 1: Add `InstallationAddedMessage`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Messaging;

/// <summary>Published after a <see cref="GameInstallation" /> has been persisted to <c>Launcher.db</c>.</summary>
/// <param name="InstallationId">The newly created installation's identifier.</param>
public sealed record InstallationAddedMessage(Guid InstallationId);
```

- [ ] **Step 2: Add `InstallationChangedMessage`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Messaging;

/// <summary>Published after any mutable field on a <see cref="GameInstallation" /> changes (rename, relocation, verification update).</summary>
/// <param name="InstallationId">The affected installation's identifier.</param>
public sealed record InstallationChangedMessage(Guid InstallationId);
```

- [ ] **Step 3: Add `InstallationDeletedMessage`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Messaging;

/// <summary>Published after a <see cref="GameInstallation" /> row and its per-installation database have been removed.</summary>
/// <param name="InstallationId">The removed installation's identifier.</param>
public sealed record InstallationDeletedMessage(Guid InstallationId);
```

- [ ] **Step 4: Add `DefaultInstallationChangedMessage`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Messaging;

/// <summary>Published when <c>LauncherSettings.DefaultInstallationId</c> changes value, including transitions to and from <see langword="null" />.</summary>
/// <param name="NewDefaultInstallationId">The new default installation's identifier, or <see langword="null" /> when no installation is registered.</param>
public sealed record DefaultInstallationChangedMessage(Guid? NewDefaultInstallationId);
```

- [ ] **Step 5: Build + commit**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj
git add Source/Erdmier.ZooTycoonLauncher.Application/Common/Messaging
git commit -m "feat(✨): add installation lifecycle messenger messages"
```

---

## Task 3: Application common models

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/InstallationSummary.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/LocatedDirectory.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/VerificationResult.cs`

- [ ] **Step 1: Add `InstallationSummary`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>
/// Read-only projection of a <see cref="GameInstallation" /> row enriched with derived state — used by the Installation Manager
/// grid and the picker.
/// </summary>
/// <param name="Id">The installation's identifier.</param>
/// <param name="Name">The user-visible name.</param>
/// <param name="Path">The fully qualified directory path.</param>
/// <param name="Validity">The validity computed from <see cref="GameInstallation.HasExe" /> and <see cref="GameInstallation.HasIni" />.</param>
/// <param name="IsDefault"><see langword="true" /> when this row's <see cref="Id" /> equals <c>LauncherSettings.DefaultInstallationId</c>.</param>
/// <param name="AddedUtc">UTC timestamp the row was created.</param>
/// <param name="ModifiedUtc">UTC timestamp of the most recent mutable-field change, or <see langword="null" /> when the row has never been modified.</param>
/// <param name="LastPlayedUtc">UTC timestamp of the most recent successful <c>zoo.exe</c> launch, or <see langword="null" />.</param>
/// <param name="LastOpenedUtc">UTC timestamp the installation last became the active installation, or <see langword="null" />.</param>
public sealed record InstallationSummary(
    Guid Id,
    string Name,
    string Path,
    InstallationValidity Validity,
    bool IsDefault,
    DateTime AddedUtc,
    DateTime? ModifiedUtc,
    DateTime? LastPlayedUtc,
    DateTime? LastOpenedUtc);
```

- [ ] **Step 2: Add `LocatedDirectory`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>
/// Outcome of <see cref="IInstallationLocator.LocateAsync" /> — either a discovered directory containing <c>zoo.exe</c> or a
/// trail of probed locations explaining why nothing was found.
/// </summary>
/// <remarks>
/// The trail entries are surfaced by the No-Game-Installation-Found state (SDD §9.1) so the user can see what was checked and
/// why. Each entry's <see cref="LocationProbeAttempt.Failure" /> is one of the structured reasons defined on the type.
/// </remarks>
public sealed record LocatedDirectory(string? Path, IReadOnlyList<LocationProbeAttempt> Trail)
{
    /// <summary><see langword="true" /> when a directory containing <c>zoo.exe</c> was found.</summary>
    public bool Found => Path is not null;
}

/// <summary>One entry in the auto-locate trail — a location that was probed and either matched or did not.</summary>
/// <param name="Source">Which sub-locator produced the candidate (registry value name, hard-coded path, persisted setting).</param>
/// <param name="CandidatePath">The path that was probed (after normalisation), or <see langword="null" /> when the source itself had no value.</param>
/// <param name="Failure"><see langword="null" /> when the probe succeeded; otherwise a structured failure reason.</param>
public sealed record LocationProbeAttempt(string Source, string? CandidatePath, LocationProbeFailure? Failure);

/// <summary>Structured failure reason for a single <see cref="LocationProbeAttempt" />.</summary>
public enum LocationProbeFailure
{
    /// <summary>The source produced no candidate path (e.g. registry key absent, persisted last-known empty).</summary>
    NoValue = 0,

    /// <summary>The candidate directory does not exist.</summary>
    DirectoryMissing = 1,

    /// <summary>The candidate directory exists but does not contain <c>zoo.exe</c>.</summary>
    NoExe = 2,
}
```

- [ ] **Step 3: Add `VerificationResult`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>
/// Outcome of <see cref="IInstallationVerifier.VerifyAsync" /> — the <c>HasExe</c> / <c>HasIni</c> flags computed against the supplied path.
/// </summary>
/// <param name="DirectoryExists"><see langword="true" /> when the supplied directory exists on disk.</param>
/// <param name="HasExe"><see langword="true" /> when <c>zoo.exe</c> is present in the directory.</param>
/// <param name="HasIni"><see langword="true" /> when <c>zoo.ini</c> is present in the directory.</param>
public sealed record VerificationResult(bool DirectoryExists, bool HasExe, bool HasIni)
{
    /// <summary>The <see cref="InstallationValidity" /> implied by the flags; falls back to <see cref="InstallationValidity.InvalidNoExeOrIni" /> when the directory is missing.</summary>
    public InstallationValidity Validity => DirectoryExists ? InstallationValidity.From(HasExe, HasIni) : InstallationValidity.InvalidNoExeOrIni;
}
```

- [ ] **Step 4: Build + commit**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj
git add Source/Erdmier.ZooTycoonLauncher.Application/Common/Models
git commit -m "feat(✨): add InstallationSummary, LocatedDirectory and VerificationResult models"
```

---

## Task 4: Application common abstractions

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IInstallationVerifier.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IInstallationLocator.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IRegistryReader.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniSnapshotService.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IInstallationDbContextFactory.cs`

- [ ] **Step 1: Add `IInstallationVerifier`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Probes an installation directory for <c>zoo.exe</c> and <c>zoo.ini</c> and reports the resulting flags.</summary>
public interface IInstallationVerifier
{
    /// <summary>Probes <paramref name="path" /> for the launcher's required files.</summary>
    /// <param name="path">The fully qualified installation directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="VerificationResult" /> describing the directory and the two flag values.</returns>
    Task<VerificationResult> VerifyAsync(string path, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Add `IInstallationLocator`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>
/// Walks the registry, hard-coded Program Files paths, and any persisted last-known directory looking for a folder that
/// contains <c>zoo.exe</c>.
/// </summary>
/// <remarks>SDD §8.5 catalogues the search order; this interface stays unaware of <c>LauncherSettings</c> — the application layer threads any persisted last-known directory through <see cref="LocateAsync" />.</remarks>
public interface IInstallationLocator
{
    /// <summary>Walks the search order and returns the first directory containing <c>zoo.exe</c>.</summary>
    /// <param name="persistedLastKnownPath">A previously persisted path to probe first, or <see langword="null" /> on first run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The discovered directory and the trail of probed locations.</returns>
    Task<LocatedDirectory> LocateAsync(string? persistedLastKnownPath, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Add `IRegistryReader`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Reads string values out of the Windows registry. Implementations are platform-specific.</summary>
/// <remarks>The interface lives here so <see cref="IInstallationLocator" /> implementations can be unit-tested against a fake reader without touching the real registry.</remarks>
public interface IRegistryReader
{
    /// <summary>Reads the string value at <paramref name="valueName" /> under <paramref name="keyPath" /> in <see cref="RegistryHive.LocalMachine" />.</summary>
    /// <param name="keyPath">The key path under HKLM (e.g. <c>SOFTWARE\Microsoft\Microsoft Games\Zoo Tycoon\1.0</c>).</param>
    /// <param name="valueName">The value name (e.g. <c>InstallPath</c>).</param>
    /// <returns>The value, or <see langword="null" /> when the key or value is absent.</returns>
    string? ReadLocalMachineString(string keyPath, string valueName);
}
```

- [ ] **Step 4: Add `IIniSnapshotService`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>
/// Capture and synchronisation surface for per-installation INI snapshots (SDD §7.3 / §8.2). The Installation Lifecycle slice
/// only invokes <see cref="CaptureOriginalAsync" />; the INI Config slice expands the surface.
/// </summary>
/// <remarks>
/// The Infrastructure layer ships a <c>NullIniSnapshotService</c> in this slice that returns <see cref="ErrorOr.Result.Success" />
/// and logs a warning. The real implementation lands in the INI Config slice with no signature change.
/// </remarks>
public interface IIniSnapshotService
{
    /// <summary>
    /// Reads <c>zoo.ini</c> for the supplied installation and writes the <c>Original</c> + <c>Current</c> snapshots in one
    /// transaction. No-op when <see cref="GameInstallation.HasIni" /> is <see langword="false" />.
    /// </summary>
    /// <param name="installation">The newly created installation row.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success on capture or no-op; a typed error on parse or persist failure.</returns>
    Task<ErrorOr<Success>> CaptureOriginalAsync(GameInstallation installation, CancellationToken cancellationToken);
}
```

- [ ] **Step 5: Add `IInstallationDbContextFactory`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>
/// Resolves a per-installation EF Core context targeting <c>{installationId}.db</c>, creating the database file and applying
/// migrations on first use.
/// </summary>
/// <remarks>
/// The factory is the Application-layer seam over <c>Microsoft.EntityFrameworkCore.DbContext</c>; consumers receive an opaque
/// <see cref="IAsyncDisposable" />-shaped handle so the EF type does not leak into Application code.
/// </remarks>
public interface IInstallationDbContextFactory
{
    /// <summary>
    /// Creates (when absent) or opens the per-installation database for <paramref name="installationId" />, runs migrations,
    /// and returns a handle wrapping the open context.
    /// </summary>
    /// <param name="installationId">The installation's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An opened handle. Dispose to release the context and its connection.</returns>
    Task<IInstallationDbContextHandle> CreateAsync(Guid installationId, CancellationToken cancellationToken);

    /// <summary>Deletes the per-installation database file (and any sidecar journal) for <paramref name="installationId" />. Safe to call when the file is absent.</summary>
    /// <param name="installationId">The installation's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid installationId, CancellationToken cancellationToken);
}

/// <summary>
/// Opaque handle around an open per-installation context. Application code keeps the handle alive only as long as it needs the
/// context; dispose releases the underlying EF resources.
/// </summary>
public interface IInstallationDbContextHandle : IAsyncDisposable;
```

- [ ] **Step 6: Build + commit**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj
git add Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions
git commit -m "feat(✨): add installation lifecycle abstractions in Application"
```

---

## Task 5: Extend `IInstallationRepository`

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IInstallationRepository.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/GlobalUsings.cs`

- [ ] **Step 1: Extend the interface**

Replace the contents of `IInstallationRepository.cs` with the full surface this slice needs:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Reads and writes <see cref="GameInstallation" /> rows in <c>Launcher.db</c>.</summary>
public interface IInstallationRepository
{
    /// <summary>Returns every registered installation, ordered alphabetically (case-insensitive) by name.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The installations, or an empty list when none exist.</returns>
    Task<IReadOnlyList<GameInstallation>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Returns the installation with the supplied identifier, or <see langword="null" /> when no such row exists.</summary>
    /// <param name="installationId">The installation's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<GameInstallation?> GetByIdAsync(Guid installationId, CancellationToken cancellationToken);

    /// <summary>Inserts a new installation row and persists immediately.</summary>
    /// <param name="installation">The installation to persist; <see cref="GameInstallation.Id" /> and <see cref="GameInstallation.AddedUtc" /> must be set by the caller.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(GameInstallation installation, CancellationToken cancellationToken);

    /// <summary>Persists mutable changes (<c>Name</c>, <c>HasExe</c>, <c>HasIni</c>, the timestamps) on the supplied row.</summary>
    /// <param name="installation">The tracked installation with pending changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(GameInstallation installation, CancellationToken cancellationToken);

    /// <summary>Removes the installation with the supplied identifier. Safe to call when no such row exists (no-op).</summary>
    /// <param name="installationId">The installation's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid installationId, CancellationToken cancellationToken);

    /// <summary>Case-insensitive existence check on <see cref="GameInstallation.Name" />, optionally excluding a row by id.</summary>
    /// <param name="name">The name to test.</param>
    /// <param name="excludeId">When supplied, the row with this id is excluded from the comparison (used by Edit).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when another row already uses this name.</returns>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken);

    /// <summary>Case-insensitive existence check on <see cref="GameInstallation.Path" />, optionally excluding a row by id.</summary>
    /// <param name="path">The path to test.</param>
    /// <param name="excludeId">When supplied, the row with this id is excluded from the comparison (used by Relocate).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when another row already uses this path.</returns>
    Task<bool> ExistsByPathAsync(string path, Guid? excludeId, CancellationToken cancellationToken);

    /// <summary>
    /// Picks the row that should be promoted to default — the alphabetically-first remaining row (case-insensitive on <see cref="GameInstallation.Name" />) — or <see langword="null" /> when no rows remain.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<GameInstallation?> FindDefaultPromotionCandidateAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Extend `GlobalUsings.cs` (Application)**

Add these lines (keep the existing block style with blank-line groups):

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
global using Erdmier.ZooTycoonLauncher.Application.Common.Models;
global using Erdmier.ZooTycoonLauncher.Application.Common.Messaging;
```

- [ ] **Step 3: Build**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj
```

Expected: the Infrastructure project won't yet compile against the extended interface — that's resolved in Task 7. Build only the Application csproj for now.

- [ ] **Step 4: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IInstallationRepository.cs Source/Erdmier.ZooTycoonLauncher.Application/GlobalUsings.cs
git commit -m "feat(✨): extend IInstallationRepository with Add/Update/Delete/exists/default-promotion surface"
```

---

## Task 6: ValidationBehaviour + ApplicationServiceCollectionExtensions

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Behaviours/ValidationBehaviour.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Extensions/ApplicationServiceCollectionExtensions.cs`

- [ ] **Step 1: Add `ValidationBehaviour`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Behaviours;

/// <summary>
/// Mediator pipeline behaviour that runs every registered <see cref="IValidator{T}" /> for the incoming message before
/// invoking the next handler. When any validator produces failures, the pipeline short-circuits with an <see cref="ErrorOr{T}" />
/// value carrying one validation <see cref="Error" /> per failure.
/// </summary>
/// <typeparam name="TMessage">The Mediator message type (command or query).</typeparam>
/// <typeparam name="TResponse">The handler's response type; must be <see cref="IErrorOr" /> so we can short-circuit cleanly.</typeparam>
public sealed class ValidationBehaviour<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : IErrorOr
{
    private readonly IEnumerable<IValidator<TMessage>> _validators;

    /// <summary>Initialises a new instance with the validators resolved from DI.</summary>
    /// <param name="validators">Every registered validator for <typeparamref name="TMessage" />. Empty when no validation rules exist for this message.</param>
    public ValidationBehaviour(IEnumerable<IValidator<TMessage>> validators) => _validators = validators;

    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(message, cancellationToken);
        }

        ValidationContext<TMessage> context = new(message);

        ValidationResult[] results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        List<Error> errors = results.SelectMany(r => r.Errors)
                                    .Where(f => f is not null)
                                    .Select(f => Error.Validation(code: f.PropertyName, description: f.ErrorMessage))
                                    .ToList();

        if (errors.Count == 0)
        {
            return await next(message, cancellationToken);
        }

        // TResponse is a closed ErrorOr<T> at every call site; we need a runtime constructor because there is no compile-time
        // path to construct the closed generic from a List<Error>. ErrorOr<T> exposes a public ctor (List<Error>).
        Type responseType = typeof(TResponse);
        return (TResponse)Activator.CreateInstance(responseType, errors)!;
    }
}
```

- [ ] **Step 2: Add `ApplicationServiceCollectionExtensions`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Extensions;

/// <summary>Composition-root extensions that register Application-layer services (Mediator + FluentValidation pipeline).</summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers <c>Mediator</c> (source-generated dispatcher), every <see cref="IValidator{T}" /> in the Application
    /// assembly, and the <see cref="ValidationBehaviour{TMessage,TResponse}" /> pipeline.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly, includeInternalTypes: true);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        return services;
    }
}
```

- [ ] **Step 3: Extend `GlobalUsings.cs` (Application)**

Add to the existing file (grouped per house style):

```csharp
global using FluentValidation;
global using FluentValidation.Results;
```

- [ ] **Step 4: Build**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj
```

Expected: SUCCEEDED. The `AddMediator` extension is generated into the Application assembly by `Mediator.SourceGenerator`. If the build complains about a missing `AddMediator`, confirm the source generator analyser assets are referenced (Task 1 Step 2).

- [ ] **Step 5: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Application
git commit -m "feat(✨): wire Mediator pipeline with FluentValidation behaviour"
```

---

## Task 7: Extend `InstallationRepository` impl + integration tests

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Repositories/InstallationRepository.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Persistence/InstallationRepositoryTests.cs`

- [ ] **Step 1: Write failing tests first**

`InstallationRepositoryTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Persistence;

public sealed class InstallationRepositoryTests : IDisposable
{
    private readonly string _databasePath;
    private readonly LauncherDbContext _context;
    private readonly InstallationRepository _repository;

    public InstallationRepositoryTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"zoolauncher-repo-{Guid.NewGuid()}.db");

        DbContextOptions<LauncherDbContext> options = new DbContextOptionsBuilder<LauncherDbContext>()
                                                     .UseSqlite($"Data Source={_databasePath}")
                                                     .Options;

        _context = new LauncherDbContext(options);
        _context.Database.Migrate();

        _repository = new InstallationRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    [Fact]
    public async Task AddAsync_PersistsRow()
    {
        GameInstallation installation = NewInstallation("Main", @"C:\Games\Zoo Tycoon");

        await _repository.AddAsync(installation, CancellationToken.None);

        GameInstallation? read = await _repository.GetByIdAsync(installation.Id, CancellationToken.None);
        read.ShouldNotBeNull();
        read.Name.ShouldBe("Main");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRowsAlphabeticallyCaseInsensitive()
    {
        await _repository.AddAsync(NewInstallation("zebra",    @"C:\Games\A-zebra"),    CancellationToken.None);
        await _repository.AddAsync(NewInstallation("Antelope", @"C:\Games\B-antelope"), CancellationToken.None);
        await _repository.AddAsync(NewInstallation("buffalo",  @"C:\Games\C-buffalo"),  CancellationToken.None);

        IReadOnlyList<GameInstallation> all = await _repository.GetAllAsync(CancellationToken.None);

        all.Select(i => i.Name).ShouldBe(["Antelope", "buffalo", "zebra"]);
    }

    [Fact]
    public async Task ExistsByNameAsync_IsCaseInsensitive_AndHonoursExcludeId()
    {
        GameInstallation main = NewInstallation("Main", @"C:\Games\Main");
        await _repository.AddAsync(main, CancellationToken.None);

        (await _repository.ExistsByNameAsync("main", excludeId: null,    CancellationToken.None)).ShouldBeTrue();
        (await _repository.ExistsByNameAsync("MAIN", excludeId: main.Id, CancellationToken.None)).ShouldBeFalse();
        (await _repository.ExistsByNameAsync("Other", excludeId: null,   CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsByPathAsync_IsCaseInsensitive_AndHonoursExcludeId()
    {
        GameInstallation main = NewInstallation("Main", @"C:\Games\Main");
        await _repository.AddAsync(main, CancellationToken.None);

        (await _repository.ExistsByPathAsync(@"c:\games\main",     excludeId: null,    CancellationToken.None)).ShouldBeTrue();
        (await _repository.ExistsByPathAsync(@"C:\GAMES\MAIN",     excludeId: main.Id, CancellationToken.None)).ShouldBeFalse();
        (await _repository.ExistsByPathAsync(@"C:\Games\Other",    excludeId: null,    CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateAsync_PersistsMutableFields()
    {
        GameInstallation row = NewInstallation("Original", @"C:\Games\Original");
        await _repository.AddAsync(row, CancellationToken.None);

        row.Name = "Renamed";
        row.HasExe = false;
        row.ModifiedUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(row, CancellationToken.None);

        GameInstallation? read = await _repository.GetByIdAsync(row.Id, CancellationToken.None);
        read.ShouldNotBeNull();
        read.Name.ShouldBe("Renamed");
        read.HasExe.ShouldBeFalse();
        read.ModifiedUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemovesRow_AndIsIdempotent()
    {
        GameInstallation row = NewInstallation("Doomed", @"C:\Games\Doomed");
        await _repository.AddAsync(row, CancellationToken.None);

        await _repository.DeleteAsync(row.Id, CancellationToken.None);
        (await _repository.GetByIdAsync(row.Id, CancellationToken.None)).ShouldBeNull();

        await Should.NotThrowAsync(async () => await _repository.DeleteAsync(row.Id, CancellationToken.None));
    }

    [Fact]
    public async Task FindDefaultPromotionCandidateAsync_ReturnsAlphabeticallyFirstByCaseInsensitiveName()
    {
        await _repository.AddAsync(NewInstallation("zebra",    @"C:\Games\zebra"),    CancellationToken.None);
        await _repository.AddAsync(NewInstallation("antelope", @"C:\Games\antelope"), CancellationToken.None);
        await _repository.AddAsync(NewInstallation("Buffalo",  @"C:\Games\Buffalo"),  CancellationToken.None);

        GameInstallation? winner = await _repository.FindDefaultPromotionCandidateAsync(CancellationToken.None);

        winner.ShouldNotBeNull();
        winner.Name.ShouldBe("antelope");
    }

    [Fact]
    public async Task FindDefaultPromotionCandidateAsync_ReturnsNullWhenTableEmpty()
    {
        GameInstallation? winner = await _repository.FindDefaultPromotionCandidateAsync(CancellationToken.None);
        winner.ShouldBeNull();
    }

    private static GameInstallation NewInstallation(string name, string path) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name,
        Path = path,
        HasExe = true,
        HasIni = true,
        AddedUtc = DateTime.UtcNow,
    };
}
```

Extend `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/GlobalUsings.cs`:

```csharp
global using Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Repositories;
global using Microsoft.Data.Sqlite;
```

- [ ] **Step 2: Run the tests to confirm they fail**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj --filter "FullyQualifiedName~InstallationRepositoryTests"
```

Expected: compile errors — the repo doesn't implement the new methods yet.

- [ ] **Step 3: Implement the extended repository**

Replace the contents of `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Repositories/InstallationRepository.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Repositories;

/// <summary>EF Core implementation of <see cref="IInstallationRepository" /> targeting <c>Launcher.db</c>.</summary>
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

    /// <inheritdoc />
    public Task<GameInstallation?> GetByIdAsync(Guid installationId, CancellationToken cancellationToken) =>
        _context.GameInstallations.FirstOrDefaultAsync(i => i.Id == installationId, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(GameInstallation installation, CancellationToken cancellationToken)
    {
        _context.GameInstallations.Add(installation);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(GameInstallation installation, CancellationToken cancellationToken)
    {
        _context.GameInstallations.Update(installation);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid installationId, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _context.GameInstallations.FirstOrDefaultAsync(i => i.Id == installationId, cancellationToken);

        if (row is null)
        {
            return;
        }

        _context.GameInstallations.Remove(row);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken) =>
        _context.GameInstallations
                .Where(i => excludeId == null || i.Id != excludeId)
                .AnyAsync(i => i.Name == name, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsByPathAsync(string path, Guid? excludeId, CancellationToken cancellationToken) =>
        _context.GameInstallations
                .Where(i => excludeId == null || i.Id != excludeId)
                .AnyAsync(i => i.Path == path, cancellationToken);

    /// <inheritdoc />
    public Task<GameInstallation?> FindDefaultPromotionCandidateAsync(CancellationToken cancellationToken) =>
        _context.GameInstallations
                .OrderBy(i => i.Name.ToLower())
                .FirstOrDefaultAsync(cancellationToken);
}
```

The case-insensitive equality on `Name` and `Path` relies on the `NOCASE` collation configured in foundations Task 6.

- [ ] **Step 4: Run tests to confirm green**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj
```

Expected: 3 (existing) + 8 (new) = 11 passed.

- [ ] **Step 5: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Launcher/Repositories/InstallationRepository.cs Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration
git commit -m "feat(✨): implement extended IInstallationRepository surface with integration tests"
```

---

## Task 8: `WindowsRegistryReader`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Discovery/WindowsRegistryReader.cs`

No integration test — `Microsoft.Win32.Registry` cannot be sandboxed and the locator's tests in Task 10 use a fake `IRegistryReader`.

- [ ] **Step 1: Add `WindowsRegistryReader`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Discovery;

/// <summary>
/// Windows-only implementation of <see cref="IRegistryReader" /> backed by <c>Microsoft.Win32.Registry</c>. Returns
/// <see langword="null" /> for any missing key, missing value, non-string value, or access error.
/// </summary>
public sealed class WindowsRegistryReader : IRegistryReader
{
    /// <inheritdoc />
    public string? ReadLocalMachineString(string keyPath, string valueName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            using Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath, writable: false);

            return key?.GetValue(valueName) as string;
        }
        catch (System.Security.SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj
```

Expected: SUCCEEDED. If CA1416 is reported as a warning-as-error, add `<SupportedOSPlatform>windows</SupportedOSPlatform>` inside the Infrastructure csproj's `<PropertyGroup>` and rebuild.

- [ ] **Step 3: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure/Discovery/WindowsRegistryReader.cs
git commit -m "feat(✨): add WindowsRegistryReader for installation auto-locator"
```

---

## Task 9: `InstallationVerifier` + integration tests

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Discovery/InstallationVerifier.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Discovery/InstallationVerifierTests.cs`

- [ ] **Step 1: Pin `System.IO.Abstractions.TestingHelpers` and reference it in the Integration test csproj**

Add to `Directory.Packages.props` under the existing DI/Logging/IO group:

```xml
<PackageVersion Include="System.IO.Abstractions.TestingHelpers" Version="21.0.29" />
```

Add to the Integration test csproj's `<ItemGroup>` of `PackageReference`:

```xml
<PackageReference Include="System.IO.Abstractions.TestingHelpers" />
```

Extend `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/GlobalUsings.cs`:

```csharp
global using System.IO.Abstractions.TestingHelpers;
global using Erdmier.ZooTycoonLauncher.Application.Common.Models;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Discovery;
```

- [ ] **Step 2: Write the failing test**

`InstallationVerifierTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Discovery;

public sealed class InstallationVerifierTests
{
    [Fact]
    public async Task VerifyAsync_ReportsAllPresent_WhenExeAndIniExist()
    {
        MockFileSystem fs = new();
        fs.AddDirectory(@"C:\Games\Zoo");
        fs.AddFile(@"C:\Games\Zoo\zoo.exe", new MockFileData([0x4D, 0x5A]));
        fs.AddFile(@"C:\Games\Zoo\zoo.ini", new MockFileData("[user]\n"));

        InstallationVerifier verifier = new(fs);

        VerificationResult result = await verifier.VerifyAsync(@"C:\Games\Zoo", CancellationToken.None);

        result.DirectoryExists.ShouldBeTrue();
        result.HasExe.ShouldBeTrue();
        result.HasIni.ShouldBeTrue();
        result.Validity.ShouldBe(InstallationValidity.Valid);
    }

    [Fact]
    public async Task VerifyAsync_ReportsMissingDirectory()
    {
        MockFileSystem fs = new();
        InstallationVerifier verifier = new(fs);

        VerificationResult result = await verifier.VerifyAsync(@"C:\Missing", CancellationToken.None);

        result.DirectoryExists.ShouldBeFalse();
        result.HasExe.ShouldBeFalse();
        result.HasIni.ShouldBeFalse();
        result.Validity.ShouldBe(InstallationValidity.InvalidNoExeOrIni);
    }

    [Fact]
    public async Task VerifyAsync_DetectsMissingIni()
    {
        MockFileSystem fs = new();
        fs.AddDirectory(@"C:\Games\Zoo");
        fs.AddFile(@"C:\Games\Zoo\zoo.exe", new MockFileData([0x4D, 0x5A]));

        InstallationVerifier verifier = new(fs);

        VerificationResult result = await verifier.VerifyAsync(@"C:\Games\Zoo", CancellationToken.None);

        result.HasExe.ShouldBeTrue();
        result.HasIni.ShouldBeFalse();
        result.Validity.ShouldBe(InstallationValidity.InvalidNoIni);
    }
}
```

- [ ] **Step 3: Run the test to confirm it fails**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj --filter "FullyQualifiedName~InstallationVerifierTests"
```

Expected: compile errors (the `InstallationVerifier` class doesn't exist yet).

- [ ] **Step 4: Implement `InstallationVerifier`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Discovery;

/// <summary>
/// File-system-backed implementation of <see cref="IInstallationVerifier" />. Probes the supplied directory for
/// <c>zoo.exe</c> and <c>zoo.ini</c>.
/// </summary>
public sealed class InstallationVerifier : IInstallationVerifier
{
    private const string ExeFileName = "zoo.exe";
    private const string IniFileName = "zoo.ini";

    private readonly IFileSystem _fileSystem;

    /// <summary>Initialises a new instance using the supplied file-system abstraction.</summary>
    /// <param name="fileSystem">The file-system abstraction.</param>
    public InstallationVerifier(IFileSystem fileSystem) => _fileSystem = fileSystem;

    /// <inheritdoc />
    public Task<VerificationResult> VerifyAsync(string path, CancellationToken cancellationToken)
    {
        if (!_fileSystem.Directory.Exists(path))
        {
            return Task.FromResult(new VerificationResult(DirectoryExists: false, HasExe: false, HasIni: false));
        }

        bool hasExe = _fileSystem.File.Exists(_fileSystem.Path.Combine(path, ExeFileName));
        bool hasIni = _fileSystem.File.Exists(_fileSystem.Path.Combine(path, IniFileName));

        return Task.FromResult(new VerificationResult(DirectoryExists: true, HasExe: hasExe, HasIni: hasIni));
    }
}
```

- [ ] **Step 5: Run tests**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj
```

Expected: all green (11 prior + 3 new = 14).

- [ ] **Step 6: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure/Discovery/InstallationVerifier.cs Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration Directory.Packages.props
git commit -m "feat(✨): add InstallationVerifier with file-system-backed probing"
```

---

## Task 10: `InstallationLocator` + integration tests

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Discovery/InstallationLocator.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Discovery/InstallationLocatorTests.cs`

- [ ] **Step 1: Write the failing tests**

`InstallationLocatorTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Discovery;

public sealed class InstallationLocatorTests
{
    [Fact]
    public async Task LocateAsync_PrefersPersistedLastKnownPath()
    {
        MockFileSystem fs = new();
        fs.AddFile(@"C:\Persisted\Zoo\zoo.exe", new MockFileData([0x4D, 0x5A]));
        fs.AddFile(@"C:\Program Files\Microsoft Games\Zoo Tycoon\zoo.exe", new MockFileData([0x4D, 0x5A]));

        IRegistryReader registry = Substitute.For<IRegistryReader>();

        InstallationLocator locator = new(fs, registry);

        LocatedDirectory result = await locator.LocateAsync(@"C:\Persisted\Zoo", CancellationToken.None);

        result.Found.ShouldBeTrue();
        result.Path.ShouldBe(@"C:\Persisted\Zoo");
    }

    [Fact]
    public async Task LocateAsync_FallsBackToProgramFiles_WhenPersistedPathInvalid()
    {
        MockFileSystem fs = new();
        fs.AddFile(@"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon\zoo.exe", new MockFileData([0x4D, 0x5A]));

        IRegistryReader registry = Substitute.For<IRegistryReader>();

        InstallationLocator locator = new(fs, registry);

        LocatedDirectory result = await locator.LocateAsync(persistedLastKnownPath: null, CancellationToken.None);

        result.Found.ShouldBeTrue();
        result.Path.ShouldBe(@"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon");
    }

    [Fact]
    public async Task LocateAsync_ReadsRegistry_WhenFilesystemPathsAbsent()
    {
        MockFileSystem fs = new();
        fs.AddFile(@"C:\Games\Custom\zoo.exe", new MockFileData([0x4D, 0x5A]));

        IRegistryReader registry = Substitute.For<IRegistryReader>();
        registry.ReadLocalMachineString(@"SOFTWARE\Microsoft\Microsoft Games\Zoo Tycoon\1.0", "InstallPath")
                .Returns(@"C:\Games\Custom");

        InstallationLocator locator = new(fs, registry);

        LocatedDirectory result = await locator.LocateAsync(persistedLastKnownPath: null, CancellationToken.None);

        result.Found.ShouldBeTrue();
        result.Path.ShouldBe(@"C:\Games\Custom");
    }

    [Fact]
    public async Task LocateAsync_ReturnsTrailWhenNothingFound()
    {
        MockFileSystem fs = new();
        IRegistryReader registry = Substitute.For<IRegistryReader>();

        InstallationLocator locator = new(fs, registry);

        LocatedDirectory result = await locator.LocateAsync(persistedLastKnownPath: @"C:\Persisted", CancellationToken.None);

        result.Found.ShouldBeFalse();
        result.Path.ShouldBeNull();
        result.Trail.ShouldNotBeEmpty();
        result.Trail.ShouldContain(a => a.Source == "Persisted last-known"     && a.Failure == LocationProbeFailure.DirectoryMissing);
        result.Trail.ShouldContain(a => a.Source.StartsWith("C:\\Program Files") && a.Failure == LocationProbeFailure.DirectoryMissing);
        result.Trail.ShouldContain(a => a.Source.StartsWith("HKLM\\")            && a.Failure == LocationProbeFailure.NoValue);
    }
}
```

- [ ] **Step 2: Implement `InstallationLocator`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Discovery;

/// <summary>
/// Implementation of <see cref="IInstallationLocator" /> that walks (in order): the persisted last-known directory, the two
/// hard-coded Program Files paths, then eight registry value-name variants under
/// <c>HKLM\SOFTWARE\Microsoft\Microsoft Games\Zoo Tycoon\1.0</c>. The first directory containing <c>zoo.exe</c> wins.
/// </summary>
public sealed class InstallationLocator : IInstallationLocator
{
    private const string ZooKeyPath = @"SOFTWARE\Microsoft\Microsoft Games\Zoo Tycoon\1.0";
    private const string ExeFileName = "zoo.exe";

    private static readonly string[] HardCodedProgramFilesPaths =
    [
        @"C:\Program Files\Microsoft Games\Zoo Tycoon",
        @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
    ];

    private static readonly string[] RegistryValueNames =
    [
        "InstallPath", "InstallDir", "InstallLocation", "Path", "GameDir", "GamePath", "Install_Dir", "Install_Path",
    ];

    private readonly IFileSystem _fileSystem;
    private readonly IRegistryReader _registry;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="fileSystem">The file-system abstraction.</param>
    /// <param name="registry">The registry abstraction.</param>
    public InstallationLocator(IFileSystem fileSystem, IRegistryReader registry)
    {
        _fileSystem = fileSystem;
        _registry = registry;
    }

    /// <inheritdoc />
    public Task<LocatedDirectory> LocateAsync(string? persistedLastKnownPath, CancellationToken cancellationToken)
    {
        List<LocationProbeAttempt> trail = new();

        if (TryProbe(source: "Persisted last-known", candidate: persistedLastKnownPath, trail, out string? hit))
        {
            return Task.FromResult(new LocatedDirectory(hit, trail));
        }

        foreach (string candidate in HardCodedProgramFilesPaths)
        {
            if (TryProbe(source: candidate, candidate, trail, out hit))
            {
                return Task.FromResult(new LocatedDirectory(hit, trail));
            }
        }

        foreach (string valueName in RegistryValueNames)
        {
            string? raw = _registry.ReadLocalMachineString(ZooKeyPath, valueName);

            if (TryProbe(source: $"HKLM\\{ZooKeyPath}\\{valueName}", candidate: raw, trail, out hit))
            {
                return Task.FromResult(new LocatedDirectory(hit, trail));
            }
        }

        return Task.FromResult(new LocatedDirectory(Path: null, trail));
    }

    private bool TryProbe(string source, string? candidate, List<LocationProbeAttempt> trail, out string? hit)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            trail.Add(new LocationProbeAttempt(source, CandidatePath: null, Failure: LocationProbeFailure.NoValue));
            hit = null;
            return false;
        }

        string normalised;

        try
        {
            normalised = _fileSystem.Path.GetFullPath(candidate);
        }
        catch (Exception)
        {
            trail.Add(new LocationProbeAttempt(source, candidate, LocationProbeFailure.DirectoryMissing));
            hit = null;
            return false;
        }

        if (!_fileSystem.Directory.Exists(normalised))
        {
            trail.Add(new LocationProbeAttempt(source, normalised, LocationProbeFailure.DirectoryMissing));
            hit = null;
            return false;
        }

        if (!_fileSystem.File.Exists(_fileSystem.Path.Combine(normalised, ExeFileName)))
        {
            trail.Add(new LocationProbeAttempt(source, normalised, LocationProbeFailure.NoExe));
            hit = null;
            return false;
        }

        trail.Add(new LocationProbeAttempt(source, normalised, Failure: null));
        hit = normalised;
        return true;
    }
}
```

- [ ] **Step 3: Run tests**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj --filter "FullyQualifiedName~InstallationLocatorTests"
```

Expected: 4 passed.

- [ ] **Step 4: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure/Discovery/InstallationLocator.cs Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Discovery
git commit -m "feat(✨): add InstallationLocator with persisted/program-files/registry search order"
```

---

## Task 11: `InstallationDbContext` + entity configurations + initial migration

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/InstallationDbContext.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/InstallationDbContextDesignTimeFactory.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/Configurations/IniSnapshotConfiguration.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/Configurations/IniValueConfiguration.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/Migrations/*` (EF-generated)
- Create: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Persistence/InstallationDbContextTests.cs`

- [ ] **Step 1: Add `IniSnapshotConfiguration`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation.Configurations;

/// <summary>
/// EF Core configuration for <see cref="IniSnapshot" />. Each per-installation DB owns a single <c>Original</c> snapshot, a
/// single <c>Current</c> snapshot, and zero or more <c>Historical</c> snapshots.
/// </summary>
public sealed class IniSnapshotConfiguration : Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<IniSnapshot>
{
    /// <inheritdoc />
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<IniSnapshot> builder)
    {
        builder.ToTable("Snapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Kind)
               .HasConversion(
                    static v => v.Name,
                    static v => IniSnapshotKind.FromName(v, ignoreCase: false))
               .IsRequired();

        builder.Property(s => s.Trigger)
               .HasConversion(
                    static v => v.Name,
                    static v => IniSnapshotTrigger.FromName(v, ignoreCase: false))
               .IsRequired();

        builder.Property(s => s.CapturedUtc).IsRequired();
        builder.Property(s => s.StructureBlob).IsRequired();

        builder.HasMany(s => s.Values)
               .WithOne()
               .HasForeignKey(v => v.SnapshotId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: Add `IniValueConfiguration`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation.Configurations;

/// <summary>
/// EF Core configuration for <see cref="IniValue" />. A row is identified by an auto-increment <c>long</c>; the
/// <c>(SnapshotId, Section, Key)</c> triple is unique within a snapshot.
/// </summary>
public sealed class IniValueConfiguration : Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<IniValue>
{
    /// <inheritdoc />
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<IniValue> builder)
    {
        builder.ToTable("IniValues");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedOnAdd();

        builder.Property(v => v.SnapshotId).IsRequired();
        builder.Property(v => v.Section).IsRequired().UseCollation("NOCASE");
        builder.Property(v => v.Key).IsRequired().UseCollation("NOCASE");
        builder.Property(v => v.Value);

        builder.Property(v => v.ValueKind)
               .HasConversion(
                    static v => v.Name,
                    static v => IniValueKind.FromName(v, ignoreCase: false))
               .IsRequired();

        builder.Property(v => v.Source)
               .HasConversion(
                    static v => v.Name,
                    static v => IniValueSource.FromName(v, ignoreCase: false))
               .IsRequired();

        builder.HasIndex(v => new { v.SnapshotId, v.Section, v.Key }).IsUnique();
    }
}
```

- [ ] **Step 3: Add `InstallationDbContext`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation;

/// <summary>
/// EF Core context for a per-installation database (<c>{installationId}.db</c>) — owns the <c>Snapshots</c> and
/// <c>IniValues</c> tables.
/// </summary>
public sealed class InstallationDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    /// <summary>Initialises a new instance with the supplied options.</summary>
    /// <param name="options">The context options (connection string supplied via the factory).</param>
    public InstallationDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<InstallationDbContext> options) : base(options) { }

    /// <summary>The per-installation snapshot table.</summary>
    public Microsoft.EntityFrameworkCore.DbSet<IniSnapshot> Snapshots => Set<IniSnapshot>();

    /// <summary>The flattened EAV value table; rows belong to exactly one <see cref="IniSnapshot" />.</summary>
    public Microsoft.EntityFrameworkCore.DbSet<IniValue> IniValues => Set<IniValue>();

    /// <inheritdoc />
    protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InstallationDbContext).Assembly);
    }
}
```

- [ ] **Step 4: Add `InstallationDbContextDesignTimeFactory`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation;

/// <summary>
/// Design-time factory used by the EF Core CLI to construct <see cref="InstallationDbContext" /> outside the running
/// application (e.g. <c>dotnet ef migrations add</c>). Targets a fixed temp file; never touches a user-owned DB.
/// </summary>
public sealed class InstallationDbContextDesignTimeFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<InstallationDbContext>
{
    /// <inheritdoc />
    public InstallationDbContext CreateDbContext(string[] args)
    {
        string databasePath = Environment.GetEnvironmentVariable("ZOOLAUNCHER_INSTALLATION_DESIGNTIME_DB")
                              ?? Path.Combine(Path.GetTempPath(), "ZooTycoonLauncher.Installation.DesignTime.db");

        Microsoft.EntityFrameworkCore.DbContextOptions<InstallationDbContext> options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<InstallationDbContext>()
                                                                                       .UseSqlite($"Data Source={databasePath}")
                                                                                       .Options;

        return new InstallationDbContext(options);
    }
}
```

- [ ] **Step 5: Generate the initial migration**

```powershell
dotnet ef migrations add InitialInstallationSchema `
  --project Source/Erdmier.ZooTycoonLauncher.Infrastructure `
  --context InstallationDbContext `
  --output-dir Persistence/Installation/Migrations
```

Expected: three new files under `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/Migrations/` — `<timestamp>_InitialInstallationSchema.cs`, `<timestamp>_InitialInstallationSchema.Designer.cs`, and `InstallationDbContextModelSnapshot.cs`. Inspect the `Up` method and confirm: `Snapshots` table created with the `Kind`/`Trigger` text columns and `StructureBlob` column; `IniValues` table created with the `NOCASE`-collated `Section` + `Key` columns and the `(SnapshotId, Section, Key)` unique index.

- [ ] **Step 6: Add integration test for the migration**

`InstallationDbContextTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Persistence;

public sealed class InstallationDbContextTests : IDisposable
{
    private readonly string _databasePath;

    public InstallationDbContextTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"zoolauncher-install-test-{Guid.NewGuid()}.db");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    [Fact]
    public async Task Migrate_OnFreshDatabase_CreatesSnapshotsAndIniValuesTables()
    {
        await using InstallationDbContext context = BuildContext();

        await context.Database.MigrateAsync();

        bool snapshotsExists = await TableExistsAsync(context, "Snapshots");
        bool valuesExists = await TableExistsAsync(context, "IniValues");

        snapshotsExists.ShouldBeTrue();
        valuesExists.ShouldBeTrue();
    }

    [Fact]
    public async Task IniValues_SectionAndKey_AreCaseInsensitiveUnique()
    {
        await using InstallationDbContext context = BuildContext();
        await context.Database.MigrateAsync();

        Guid snapshotId = Guid.CreateVersion7();

        IniSnapshot snapshot = new()
        {
            Id = snapshotId,
            Kind = IniSnapshotKind.Original,
            Trigger = IniSnapshotTrigger.OriginalImport,
            CapturedUtc = DateTime.UtcNow,
            StructureBlob = "[user]\n",
        };

        context.Snapshots.Add(snapshot);
        context.IniValues.Add(new IniValue
        {
            SnapshotId = snapshotId,
            Section = "user",
            Key = "ShowToolTips",
            Value = "1",
            ValueKind = IniValueKind.Bool,
            Source = IniValueSource.OriginalImport,
        });
        await context.SaveChangesAsync();

        context.IniValues.Add(new IniValue
        {
            SnapshotId = snapshotId,
            Section = "USER",                // same section
            Key = "showtooltips",            // same key, different casing
            Value = "0",
            ValueKind = IniValueKind.Bool,
            Source = IniValueSource.OriginalImport,
        });

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    private InstallationDbContext BuildContext()
    {
        DbContextOptions<InstallationDbContext> options = new DbContextOptionsBuilder<InstallationDbContext>()
                                                         .UseSqlite($"Data Source={_databasePath}")
                                                         .Options;
        return new InstallationDbContext(options);
    }

    private static async Task<bool> TableExistsAsync(InstallationDbContext context, string tableName)
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

Extend Integration test `GlobalUsings.cs`:

```csharp
global using Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation;
```

- [ ] **Step 7: Build + run tests**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj
dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj
```

Expected: SUCCEEDED + 16 passed.

- [ ] **Step 8: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration
git commit -m "feat(✨): add per-installation EF context with Snapshots and IniValues schema"
```

---

## Task 12: `InstallationDbContextFactory` + null INI snapshot service

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/InstallationDbContextFactory.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/IniSnapshots/NullIniSnapshotService.cs`

- [ ] **Step 1: Add `InstallationDbContextFactory`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation;

/// <summary>
/// File-system-backed implementation of <see cref="IInstallationDbContextFactory" />. Creates (when absent) or opens the
/// per-installation database under <c>{DataRoot}\{installationId}.db</c>, runs migrations, and wraps the context in a
/// disposable handle.
/// </summary>
public sealed class InstallationDbContextFactory : IInstallationDbContextFactory
{
    private readonly IAppStorageLocations _locations;
    private readonly IFileSystem _fileSystem;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="locations">Path locations.</param>
    /// <param name="fileSystem">File-system abstraction (only used for delete; EF Core owns its own IO for read/write).</param>
    public InstallationDbContextFactory(IAppStorageLocations locations, IFileSystem fileSystem)
    {
        _locations = locations;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public async Task<IInstallationDbContextHandle> CreateAsync(Guid installationId, CancellationToken cancellationToken)
    {
        string databasePath = _locations.InstallationDatabasePath(installationId);

        DbContextOptions<InstallationDbContext> options = new DbContextOptionsBuilder<InstallationDbContext>()
                                                         .UseSqlite($"Data Source={databasePath}")
                                                         .Options;

        InstallationDbContext context = new(options);

        await context.Database.MigrateAsync(cancellationToken);

        return new Handle(context);
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid installationId, CancellationToken cancellationToken)
    {
        SqliteConnection.ClearAllPools();

        string databasePath = _locations.InstallationDatabasePath(installationId);

        if (_fileSystem.File.Exists(databasePath))
        {
            _fileSystem.File.Delete(databasePath);
        }

        foreach (string suffix in new[] { "-wal", "-shm", "-journal" })
        {
            string sidecar = databasePath + suffix;

            if (_fileSystem.File.Exists(sidecar))
            {
                _fileSystem.File.Delete(sidecar);
            }
        }

        return Task.CompletedTask;
    }

    private sealed class Handle : IInstallationDbContextHandle
    {
        private readonly InstallationDbContext _context;

        public Handle(InstallationDbContext context) => _context = context;

        public ValueTask DisposeAsync() => _context.DisposeAsync();
    }
}
```

Note: `Microsoft.Data.Sqlite` is already a transitive dependency from `Microsoft.EntityFrameworkCore.Sqlite`. Add a `global using Microsoft.Data.Sqlite;` to the Infrastructure `GlobalUsings.cs` if it is not already present.

- [ ] **Step 2: Add `NullIniSnapshotService`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.IniSnapshots;

/// <summary>
/// Placeholder <see cref="IIniSnapshotService" /> shipped during the Installation Lifecycle slice. The INI Config slice
/// replaces it with a real implementation that parses <c>zoo.ini</c> and writes the <c>Original</c> + <c>Current</c>
/// snapshots.
/// </summary>
public sealed class NullIniSnapshotService : IIniSnapshotService
{
    private readonly ILogger _logger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="logger">The Serilog logger.</param>
    public NullIniSnapshotService(ILogger logger) => _logger = logger;

    /// <inheritdoc />
    public Task<ErrorOr<Success>> CaptureOriginalAsync(GameInstallation installation, CancellationToken cancellationToken)
    {
        if (installation.HasIni)
        {
            _logger.Warning(
                "INI snapshot capture for {InstallationId} ({Name}) is deferred — replace NullIniSnapshotService when the INI Config slice lands.",
                installation.Id,
                installation.Name);
        }

        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }
}
```

Add to Application `GlobalUsings.cs`:

```csharp
global using ErrorOr;
```

(It is already there; confirm.)

Add to Infrastructure `GlobalUsings.cs`:

```csharp
global using Erdmier.ZooTycoonLauncher.Infrastructure.IniSnapshots;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation;
global using ErrorOr;
global using Microsoft.Data.Sqlite;
```

- [ ] **Step 3: Build**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj
```

Expected: SUCCEEDED.

- [ ] **Step 4: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure
git commit -m "feat(✨): add InstallationDbContextFactory and NullIniSnapshotService"
```

---

## Task 13: Extend `AddInfrastructure` to register new services

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Extensions/InfrastructureServiceCollectionExtensions.cs`

- [ ] **Step 1: Extend the extension class**

Replace the whole file with:

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Common.Extensions;

/// <summary>Composition-root extensions that register every Infrastructure service into a service collection.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registers Infrastructure services — file system, storage locations, Serilog, EF Core, repositories, locator/verifier/registry, INI snapshot placeholder.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IAppStorageLocations, AppStorageLocations>();

        services.AddSingleton<ILogger>(provider =>
        {
            IAppStorageLocations locations = provider.GetRequiredService<IAppStorageLocations>();

            return SerilogConfiguration.Build(locations)
                                       .CreateLogger();
        });

        services.AddDbContext<LauncherDbContext>((provider, options) =>
        {
            IAppStorageLocations locations = provider.GetRequiredService<IAppStorageLocations>();
            options.UseSqlite($"Data Source={locations.LauncherDatabasePath}");
        });

        services.AddScoped<ILauncherSettingsRepository, LauncherSettingsRepository>();
        services.AddScoped<IInstallationRepository, InstallationRepository>();

        services.AddSingleton<IRegistryReader, WindowsRegistryReader>();
        services.AddSingleton<IInstallationVerifier, InstallationVerifier>();
        services.AddSingleton<IInstallationLocator, InstallationLocator>();

        services.AddSingleton<IInstallationDbContextFactory, InstallationDbContextFactory>();
        services.AddScoped<IIniSnapshotService, NullIniSnapshotService>();

        return services;
    }

    /// <summary>Runs EF Core migrations against <c>Launcher.db</c>. Call once the application starts.</summary>
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

- [ ] **Step 2: Build whole solution**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj
```

Expected: SUCCEEDED.

- [ ] **Step 3: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Extensions/InfrastructureServiceCollectionExtensions.cs
git commit -m "feat(✨): register installation lifecycle services in AddInfrastructure"
```

---

## Task 14: `GetAllInstallationsQuery` + `GetInstallationByIdQuery`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/GetAll/GetAllInstallationsQuery.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/GetAll/GetAllInstallationsHandler.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/GetById/GetInstallationByIdQuery.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/GetById/GetInstallationByIdHandler.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/GetAllInstallationsHandlerTests.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/GetInstallationByIdHandlerTests.cs`

- [ ] **Step 1: Add the GetAll query + handler**

`GetAllInstallationsQuery.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.GetAll;

/// <summary>Returns every registered installation as <see cref="InstallationSummary" /> projections with default-installation flag resolved.</summary>
public sealed record GetAllInstallationsQuery : IQuery<ErrorOr<IReadOnlyList<InstallationSummary>>>;
```

`GetAllInstallationsHandler.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.GetAll;

/// <summary>Handler for <see cref="GetAllInstallationsQuery" />.</summary>
public sealed class GetAllInstallationsHandler : IQueryHandler<GetAllInstallationsQuery, ErrorOr<IReadOnlyList<InstallationSummary>>>
{
    private readonly IInstallationRepository _installations;
    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="settings">Launcher settings repository.</param>
    public GetAllInstallationsHandler(IInstallationRepository installations, ILauncherSettingsRepository settings)
    {
        _installations = installations;
        _settings = settings;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<IReadOnlyList<InstallationSummary>>> Handle(GetAllInstallationsQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<GameInstallation> rows = await _installations.GetAllAsync(cancellationToken);
        LauncherSettings settings = await _settings.GetAsync(cancellationToken);

        IReadOnlyList<InstallationSummary> summaries = rows.Select(row => new InstallationSummary(
                                                               Id:            row.Id,
                                                               Name:          row.Name,
                                                               Path:          row.Path,
                                                               Validity:      row.Validity,
                                                               IsDefault:     settings.DefaultInstallationId == row.Id,
                                                               AddedUtc:      row.AddedUtc,
                                                               ModifiedUtc:   row.ModifiedUtc,
                                                               LastPlayedUtc: row.LastPlayedUtc,
                                                               LastOpenedUtc: row.LastOpenedUtc))
                                                           .ToList();

        return ErrorOrFactory.From(summaries);
    }
}
```

- [ ] **Step 2: Add the GetById query + handler**

`GetInstallationByIdQuery.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.GetById;

/// <summary>Returns a single installation as an <see cref="InstallationSummary" /> projection.</summary>
/// <param name="InstallationId">The installation's identifier.</param>
public sealed record GetInstallationByIdQuery(Guid InstallationId) : IQuery<ErrorOr<InstallationSummary>>;
```

`GetInstallationByIdHandler.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.GetById;

/// <summary>Handler for <see cref="GetInstallationByIdQuery" />.</summary>
public sealed class GetInstallationByIdHandler : IQueryHandler<GetInstallationByIdQuery, ErrorOr<InstallationSummary>>
{
    private readonly IInstallationRepository _installations;
    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="settings">Launcher settings repository.</param>
    public GetInstallationByIdHandler(IInstallationRepository installations, ILauncherSettingsRepository settings)
    {
        _installations = installations;
        _settings = settings;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<InstallationSummary>> Handle(GetInstallationByIdQuery query, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(query.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", description: $"No installation with id {query.InstallationId}.");
        }

        LauncherSettings settings = await _settings.GetAsync(cancellationToken);

        return new InstallationSummary(
            Id:            row.Id,
            Name:          row.Name,
            Path:          row.Path,
            Validity:      row.Validity,
            IsDefault:     settings.DefaultInstallationId == row.Id,
            AddedUtc:      row.AddedUtc,
            ModifiedUtc:   row.ModifiedUtc,
            LastPlayedUtc: row.LastPlayedUtc,
            LastOpenedUtc: row.LastOpenedUtc);
    }
}
```

- [ ] **Step 3: Add handler unit tests**

Extend the Application unit test csproj if needed — confirm it references the Application project (it does, from foundations).

`GetAllInstallationsHandlerTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class GetAllInstallationsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSummariesWithDefaultFlag()
    {
        Guid defaultId = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns(new List<GameInstallation>
                     {
                         new() { Id = defaultId, Name = "Main",  Path = @"C:\Games\Main",  HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow },
                         new() { Id = Guid.CreateVersion7(), Name = "Other", Path = @"C:\Games\Other", HasExe = true, HasIni = false, AddedUtc = DateTime.UtcNow },
                     });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { DefaultInstallationId = defaultId });

        GetAllInstallationsHandler handler = new(installations, settings);

        ErrorOr<IReadOnlyList<InstallationSummary>> result = await handler.Handle(new GetAllInstallationsQuery(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Count.ShouldBe(2);
        result.Value.Single(s => s.Name == "Main").IsDefault.ShouldBeTrue();
        result.Value.Single(s => s.Name == "Other").IsDefault.ShouldBeFalse();
        result.Value.Single(s => s.Name == "Other").Validity.ShouldBe(InstallationValidity.InvalidNoIni);
    }
}
```

`GetInstallationByIdHandlerTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class GetInstallationByIdHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsNotFound_WhenInstallationMissing()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((GameInstallation?)null);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        GetInstallationByIdHandler handler = new(installations, settings);

        ErrorOr<InstallationSummary> result = await handler.Handle(new GetInstallationByIdQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ReturnsSummary_WhenFound()
    {
        Guid id = Guid.CreateVersion7();
        GameInstallation row = new() { Id = id, Name = "Main", Path = @"C:\Games\Main", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
        settings.GetAsync(Arg.Any<CancellationToken>()).Returns(new LauncherSettings { DefaultInstallationId = id });

        GetInstallationByIdHandler handler = new(installations, settings);

        ErrorOr<InstallationSummary> result = await handler.Handle(new GetInstallationByIdQuery(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.IsDefault.ShouldBeTrue();
    }
}
```

Extend Application unit test `GlobalUsings.cs`:

```csharp
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

global using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
global using Erdmier.ZooTycoonLauncher.Application.Common.Models;
global using Erdmier.ZooTycoonLauncher.Application.Installations.GetAll;
global using Erdmier.ZooTycoonLauncher.Application.Installations.GetById;
global using Erdmier.ZooTycoonLauncher.Domain.Installations;
global using Erdmier.ZooTycoonLauncher.Domain.Settings;
global using ErrorOr;
```

(Keep the existing `NSubstitute`, `Shouldly`, `Xunit` lines; merge new entries alphabetically.)

- [ ] **Step 4: Build + test**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
```

Expected: 1 (existing) + 3 (new) = 4 passed.

- [ ] **Step 5: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Application/Installations/GetAll Source/Erdmier.ZooTycoonLauncher.Application/Installations/GetById Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m "feat(✨): add GetAllInstallationsQuery and GetInstallationByIdQuery with handlers"
```

---

## Task 15: `VerifyInstallationQuery`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Verify/VerifyInstallationQuery.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Verify/VerifyInstallationHandler.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/VerifyInstallationHandlerTests.cs`

- [ ] **Step 1: Add the query + handler**

`VerifyInstallationQuery.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Verify;

/// <summary>
/// Re-runs <see cref="IInstallationVerifier" /> for the supplied installation, persists any change to <c>HasExe</c>,
/// <c>HasIni</c>, <c>ModifiedUtc</c>, and returns the updated <see cref="VerificationResult" />.
/// </summary>
/// <param name="InstallationId">The installation's identifier.</param>
public sealed record VerifyInstallationQuery(Guid InstallationId) : IQuery<ErrorOr<VerificationResult>>;
```

`VerifyInstallationHandler.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Verify;

/// <summary>Handler for <see cref="VerifyInstallationQuery" />.</summary>
public sealed class VerifyInstallationHandler : IQueryHandler<VerifyInstallationQuery, ErrorOr<VerificationResult>>
{
    private readonly IInstallationRepository _installations;
    private readonly IInstallationVerifier _verifier;
    private readonly TimeProvider _clock;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="verifier">File-system verifier.</param>
    /// <param name="clock">Time provider for the <c>ModifiedUtc</c> stamp.</param>
    public VerifyInstallationHandler(IInstallationRepository installations, IInstallationVerifier verifier, TimeProvider clock)
    {
        _installations = installations;
        _verifier = verifier;
        _clock = clock;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<VerificationResult>> Handle(VerifyInstallationQuery query, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(query.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", description: $"No installation with id {query.InstallationId}.");
        }

        VerificationResult result = await _verifier.VerifyAsync(row.Path, cancellationToken);

        if (row.HasExe != result.HasExe || row.HasIni != result.HasIni)
        {
            row.HasExe = result.HasExe;
            row.HasIni = result.HasIni;
            row.ModifiedUtc = _clock.GetUtcNow().UtcDateTime;

            await _installations.UpdateAsync(row, cancellationToken);
        }

        return result;
    }
}
```

- [ ] **Step 2: Register `TimeProvider` in `AddInfrastructure`**

Open the Infrastructure DI extensions and add `services.AddSingleton(TimeProvider.System);` after the `IFileSystem` registration. Build the Infrastructure project to confirm.

- [ ] **Step 3: Add the handler test**

`VerifyInstallationHandlerTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class VerifyInstallationHandlerTests
{
    [Fact]
    public async Task Handle_PersistsFlagChange_AndStampsModifiedUtc()
    {
        DateTime fakeNow = new(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc);
        FakeTimeProvider clock = new(fakeNow);

        GameInstallation row = new()
        {
            Id = Guid.CreateVersion7(),
            Name = "Main",
            Path = @"C:\Games\Main",
            HasExe = false,
            HasIni = false,
            AddedUtc = DateTime.UtcNow,
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(row.Id, Arg.Any<CancellationToken>()).Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        VerifyInstallationHandler handler = new(installations, verifier, clock);

        ErrorOr<VerificationResult> result = await handler.Handle(new VerifyInstallationQuery(row.Id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        row.HasExe.ShouldBeTrue();
        row.HasIni.ShouldBeTrue();
        row.ModifiedUtc.ShouldBe(fakeNow);
        await installations.Received(1).UpdateAsync(row, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoPersist_WhenFlagsUnchanged()
    {
        GameInstallation row = new()
        {
            Id = Guid.CreateVersion7(),
            Name = "Main",
            Path = @"C:\Games\Main",
            HasExe = true,
            HasIni = true,
            AddedUtc = DateTime.UtcNow,
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(row.Id, Arg.Any<CancellationToken>()).Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        VerifyInstallationHandler handler = new(installations, verifier, TimeProvider.System);

        await handler.Handle(new VerifyInstallationQuery(row.Id), CancellationToken.None);

        await installations.DidNotReceive().UpdateAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>());
    }
}

internal sealed class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FakeTimeProvider(DateTime utcNow) => _now = new DateTimeOffset(utcNow, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;
}
```

Extend Application unit test `GlobalUsings.cs`:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Installations.Verify;
```

- [ ] **Step 4: Build + test + commit**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
```

Expected: 6 passed.

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Application/Installations/Verify Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Extensions/InfrastructureServiceCollectionExtensions.cs Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m "feat(✨): add VerifyInstallationQuery handler that persists drift in HasExe/HasIni"
```

---

## Task 16: `LocateZooTycoonQuery`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Locate/LocateZooTycoonQuery.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Locate/LocateZooTycoonHandler.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/LocateZooTycoonHandlerTests.cs`

- [ ] **Step 1: Add the query + handler**

`LocateZooTycoonQuery.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Locate;

/// <summary>
/// Walks <see cref="IInstallationLocator" /> and returns the discovered directory plus the full probe trail. Always succeeds —
/// "no candidate found" is a value, not an error.
/// </summary>
public sealed record LocateZooTycoonQuery : IQuery<ErrorOr<LocatedDirectory>>;
```

`LocateZooTycoonHandler.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Locate;

/// <summary>Handler for <see cref="LocateZooTycoonQuery" />.</summary>
public sealed class LocateZooTycoonHandler : IQueryHandler<LocateZooTycoonQuery, ErrorOr<LocatedDirectory>>
{
    private readonly IInstallationLocator _locator;
    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="locator">The locator.</param>
    /// <param name="settings">Settings repository (consulted for the persisted last-known directory; that field is not modelled in <see cref="LauncherSettings" /> today, so the handler passes <see langword="null" /> for the persisted candidate).</param>
    public LocateZooTycoonHandler(IInstallationLocator locator, ILauncherSettingsRepository settings)
    {
        _locator = locator;
        _settings = settings;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<LocatedDirectory>> Handle(LocateZooTycoonQuery query, CancellationToken cancellationToken)
    {
        // The SDD treats the persisted last-known directory as a future addition to LauncherSettings. Until that field lands,
        // pass null and rely on the Program Files + registry trail. The locator is forward-compatible — any future addition is
        // a one-line change here.
        _ = await _settings.GetAsync(cancellationToken);

        LocatedDirectory located = await _locator.LocateAsync(persistedLastKnownPath: null, cancellationToken);

        return located;
    }
}
```

- [ ] **Step 2: Add the handler test**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class LocateZooTycoonHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsLocatorResult()
    {
        IInstallationLocator locator = Substitute.For<IInstallationLocator>();
        locator.LocateAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
               .Returns(new LocatedDirectory(@"C:\Games\Found", new[] { new LocationProbeAttempt("S", @"C:\Games\Found", null) }));

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
        settings.GetAsync(Arg.Any<CancellationToken>()).Returns(new LauncherSettings());

        LocateZooTycoonHandler handler = new(locator, settings);

        ErrorOr<LocatedDirectory> result = await handler.Handle(new LocateZooTycoonQuery(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Found.ShouldBeTrue();
        result.Value.Path.ShouldBe(@"C:\Games\Found");
    }
}
```

Extend Application unit test `GlobalUsings.cs`:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Installations.Locate;
```

- [ ] **Step 3: Build + test + commit**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
git add Source/Erdmier.ZooTycoonLauncher.Application/Installations/Locate Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m "feat(✨): add LocateZooTycoonQuery handler"
```

---

## Task 17: `AddInstallationCommand` (the big one)

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Add/AddInstallationCommand.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Add/AddInstallationValidator.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Add/AddInstallationHandler.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Add/AddInstallationResult.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/AddInstallationHandlerTests.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/AddInstallationValidatorTests.cs`

The handler implements SDD §7.2.1 steps 1-5 with deferred snapshot capture (`IIniSnapshotService.CaptureOriginalAsync` is invoked unconditionally and the `NullIniSnapshotService` no-ops). On success, the handler returns `AddInstallationResult` describing the newly created installation.

- [ ] **Step 1: Add the command + result**

`AddInstallationCommand.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Add;

/// <summary>
/// Persists a new <see cref="GameInstallation" />, creates the per-installation database, runs migrations, and (deferred to the
/// INI Config slice) captures the <c>Original</c> snapshot. See SDD §7.2.1.
/// </summary>
/// <param name="Name">User-visible name; trimmed by the handler; case-insensitive uniqueness enforced.</param>
/// <param name="Path">Absolute installation directory.</param>
/// <param name="MakeDefault"><see langword="true" /> to set this installation as the launcher default after persisting.</param>
public sealed record AddInstallationCommand(string Name, string Path, bool MakeDefault) : ICommand<ErrorOr<AddInstallationResult>>;
```

`AddInstallationResult.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Add;

/// <summary>Outcome of a successful <see cref="AddInstallationCommand" />.</summary>
/// <param name="InstallationId">The newly created installation's identifier.</param>
/// <param name="Validity">The validity computed from the verifier's result.</param>
/// <param name="BecameDefault"><see langword="true" /> when the new installation was promoted to default (either explicitly via <see cref="AddInstallationCommand.MakeDefault" /> or implicitly because it is the first registered installation).</param>
public sealed record AddInstallationResult(Guid InstallationId, InstallationValidity Validity, bool BecameDefault);
```

- [ ] **Step 2: Add the validator**

`AddInstallationValidator.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Add;

/// <summary>FluentValidation rules for <see cref="AddInstallationCommand" />.</summary>
public sealed class AddInstallationValidator : AbstractValidator<AddInstallationCommand>
{
    private readonly IInstallationRepository _installations;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Repository used for the uniqueness checks.</param>
    public AddInstallationValidator(IInstallationRepository installations)
    {
        _installations = installations;

        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name?.Trim()))
            .WithMessage("Name cannot be whitespace.")
            .MustAsync(NameIsUniqueAsync)
            .WithMessage("Another installation already uses this name.");

        RuleFor(c => c.Path)
            .NotEmpty()
            .WithMessage("Path is required.")
            .MustAsync(PathIsUniqueAsync)
            .WithMessage("Another installation already uses this folder.");
    }

    private async Task<bool> NameIsUniqueAsync(string name, CancellationToken cancellationToken)
    {
        string trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return true;
        }

        return !await _installations.ExistsByNameAsync(trimmed, excludeId: null, cancellationToken);
    }

    private async Task<bool> PathIsUniqueAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        return !await _installations.ExistsByPathAsync(path, excludeId: null, cancellationToken);
    }
}
```

- [ ] **Step 3: Add the handler**

`AddInstallationHandler.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Add;

/// <summary>Handler for <see cref="AddInstallationCommand" />. Implements SDD §7.2.1 steps 1-5 with deferred snapshot capture.</summary>
public sealed class AddInstallationHandler : ICommandHandler<AddInstallationCommand, ErrorOr<AddInstallationResult>>
{
    private readonly IInstallationRepository _installations;
    private readonly ILauncherSettingsRepository _settings;
    private readonly IInstallationVerifier _verifier;
    private readonly IInstallationDbContextFactory _dbFactory;
    private readonly IIniSnapshotService _snapshots;
    private readonly TimeProvider _clock;
    private readonly ILogger _logger;

    /// <summary>Initialises a new instance.</summary>
    public AddInstallationHandler(
        IInstallationRepository installations,
        ILauncherSettingsRepository settings,
        IInstallationVerifier verifier,
        IInstallationDbContextFactory dbFactory,
        IIniSnapshotService snapshots,
        TimeProvider clock,
        ILogger logger)
    {
        _installations = installations;
        _settings = settings;
        _verifier = verifier;
        _dbFactory = dbFactory;
        _snapshots = snapshots;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<AddInstallationResult>> Handle(AddInstallationCommand command, CancellationToken cancellationToken)
    {
        string trimmedName = command.Name.Trim();

        VerificationResult verification = await _verifier.VerifyAsync(command.Path, cancellationToken);

        if (!verification.DirectoryExists)
        {
            return Error.Validation(code: "Installation.PathMissing", description: $"The folder \"{command.Path}\" does not exist.");
        }

        IReadOnlyList<GameInstallation> existing = await _installations.GetAllAsync(cancellationToken);
        bool isFirst = existing.Count == 0;
        bool becameDefault = command.MakeDefault || isFirst;

        GameInstallation row = new()
        {
            Id = Guid.CreateVersion7(),
            Name = trimmedName,
            Path = command.Path,
            HasExe = verification.HasExe,
            HasIni = verification.HasIni,
            AddedUtc = _clock.GetUtcNow().UtcDateTime,
        };

        await _installations.AddAsync(row, cancellationToken);

        if (becameDefault)
        {
            LauncherSettings settings = await _settings.GetAsync(cancellationToken);
            settings.DefaultInstallationId = row.Id;

            await _settings.UpdateAsync(settings, cancellationToken);
        }

        // Provision the per-installation database — the file is created and migrations applied here so the INI slice can drop
        // straight in without retrofitting.
        await using (IInstallationDbContextHandle handle = await _dbFactory.CreateAsync(row.Id, cancellationToken))
        {
            // Handle disposed immediately — we just need the DB file on disk with schema applied.
        }

        ErrorOr<Success> snapshotResult = await _snapshots.CaptureOriginalAsync(row, cancellationToken);

        if (snapshotResult.IsError)
        {
            _logger.Warning("Snapshot capture failed for {InstallationId}: {Errors}", row.Id, string.Join("; ", snapshotResult.Errors.Select(e => e.Description)));
            // The installation is persisted; surface the snapshot failure but do not roll back. The INI Config slice's real
            // service will treat snapshot failure as a transition into the CorruptedIni state instead of an outright error.
        }

        return new AddInstallationResult(row.Id, verification.Validity, becameDefault);
    }
}
```

- [ ] **Step 4: Add handler tests**

`AddInstallationHandlerTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class AddInstallationHandlerTests
{
    [Fact]
    public async Task Handle_PersistsRow_AndCreatesPerInstallationDb()
    {
        AddInstallationCommand command = new(Name: "  Main  ", Path: @"C:\Games\Main", MakeDefault: false);
        FakeTimeProvider clock = new(new DateTime(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc));

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<GameInstallation>());

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
        settings.GetAsync(Arg.Any<CancellationToken>()).Returns(new LauncherSettings());

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();
        dbFactory.CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns(Substitute.For<IInstallationDbContextHandle>());

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();
        snapshots.CaptureOriginalAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        ILogger logger = Substitute.For<ILogger>();

        AddInstallationHandler handler = new(installations, settings, verifier, dbFactory, snapshots, clock, logger);

        ErrorOr<AddInstallationResult> result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.BecameDefault.ShouldBeTrue();             // First installation auto-promotes
        result.Value.Validity.ShouldBe(InstallationValidity.Valid);

        await installations.Received(1).AddAsync(Arg.Is<GameInstallation>(i => i.Name == "Main" && i.Path == command.Path && i.HasExe && i.HasIni), Arg.Any<CancellationToken>());
        await dbFactory.Received(1).CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await snapshots.Received(1).CaptureOriginalAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsValidationError_WhenDirectoryMissing()
    {
        AddInstallationCommand command = new(Name: "Main", Path: @"C:\Missing", MakeDefault: false);

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: false, HasExe: false, HasIni: false));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();
        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();
        ILogger logger = Substitute.For<ILogger>();

        AddInstallationHandler handler = new(installations, settings, verifier, dbFactory, snapshots, TimeProvider.System, logger);

        ErrorOr<AddInstallationResult> result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Installation.PathMissing");
        await installations.DidNotReceive().AddAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotPromoteToDefault_WhenAlreadyHaveInstallations_AndMakeDefaultFalse()
    {
        AddInstallationCommand command = new(Name: "Second", Path: @"C:\Games\Second", MakeDefault: false);

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns(new[] { new GameInstallation { Id = Guid.CreateVersion7(), Name = "First", Path = @"C:\Games\First", AddedUtc = DateTime.UtcNow } });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
        settings.GetAsync(Arg.Any<CancellationToken>()).Returns(new LauncherSettings());

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();
        dbFactory.CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Substitute.For<IInstallationDbContextHandle>());
        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();
        snapshots.CaptureOriginalAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>()).Returns(Result.Success);
        ILogger logger = Substitute.For<ILogger>();

        AddInstallationHandler handler = new(installations, settings, verifier, dbFactory, snapshots, TimeProvider.System, logger);

        ErrorOr<AddInstallationResult> result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.BecameDefault.ShouldBeFalse();
        await settings.DidNotReceive().UpdateAsync(Arg.Any<LauncherSettings>(), Arg.Any<CancellationToken>());
    }
}
```

`AddInstallationValidatorTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class AddInstallationValidatorTests
{
    [Fact]
    public async Task RejectsBlankName()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        AddInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new AddInstallationCommand(Name: "   ", Path: @"C:\Games\Main", MakeDefault: false));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(f => f.PropertyName == "Name");
    }

    [Fact]
    public async Task RejectsDuplicateName()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.ExistsByNameAsync("Main", null, Arg.Any<CancellationToken>()).Returns(true);

        AddInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new AddInstallationCommand(Name: "Main", Path: @"C:\Games\Main", MakeDefault: false));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(f => f.PropertyName == "Name");
    }

    [Fact]
    public async Task AcceptsUniqueRow()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        AddInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new AddInstallationCommand(Name: "Main", Path: @"C:\Games\Main", MakeDefault: false));

        result.IsValid.ShouldBeTrue();
    }
}
```

Extend Application unit test `GlobalUsings.cs`:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Installations.Add;
global using FluentValidation;
global using FluentValidation.Results;
global using Serilog;
```

- [ ] **Step 5: Build + test + commit**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
git add Source/Erdmier.ZooTycoonLauncher.Application/Installations/Add Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m "feat(✨): add AddInstallationCommand with validator and per-install DB provisioning"
```

---

## Task 18: `UpdateInstallationCommand` (rename + default toggle)

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Update/UpdateInstallationCommand.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Update/UpdateInstallationValidator.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Update/UpdateInstallationHandler.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/UpdateInstallationHandlerTests.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/UpdateInstallationValidatorTests.cs`

SDD §7.2.3 — Edit Installation. The command renames an installation and optionally promotes it to default; it never unsets the default flag (SDD: "the launcher always has a default when at least one installation exists"; the SetDefault command handles the reverse direction by promoting another row).

- [ ] **Step 1: Add the command**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Update;

/// <summary>
/// Updates the mutable fields on an existing installation: <c>Name</c> and (optionally) the default flag. Path changes go
/// through <see cref="RelocateInstallationCommand" /> instead.
/// </summary>
/// <param name="InstallationId">The installation's identifier.</param>
/// <param name="Name">New user-visible name.</param>
/// <param name="MakeDefault">
/// <see langword="true" /> to promote this installation to default. Set to <see langword="false" /> to leave the default
/// untouched (the dialogue cannot un-tick default — see SDD §7.2.3 step 3).
/// </param>
public sealed record UpdateInstallationCommand(Guid InstallationId, string Name, bool MakeDefault) : ICommand<ErrorOr<Success>>;
```

- [ ] **Step 2: Add the validator**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Update;

/// <summary>FluentValidation rules for <see cref="UpdateInstallationCommand" />.</summary>
public sealed class UpdateInstallationValidator : AbstractValidator<UpdateInstallationCommand>
{
    private readonly IInstallationRepository _installations;

    /// <summary>Initialises a new instance.</summary>
    public UpdateInstallationValidator(IInstallationRepository installations)
    {
        _installations = installations;

        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name?.Trim()))
            .WithMessage("Name cannot be whitespace.")
            .MustAsync(NameIsUniqueAsync)
            .WithMessage("Another installation already uses this name.");
    }

    private async Task<bool> NameIsUniqueAsync(UpdateInstallationCommand command, string name, CancellationToken cancellationToken)
    {
        string trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return true;
        }

        return !await _installations.ExistsByNameAsync(trimmed, excludeId: command.InstallationId, cancellationToken);
    }
}
```

- [ ] **Step 3: Add the handler**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Update;

/// <summary>Handler for <see cref="UpdateInstallationCommand" />.</summary>
public sealed class UpdateInstallationHandler : ICommandHandler<UpdateInstallationCommand, ErrorOr<Success>>
{
    private readonly IInstallationRepository _installations;
    private readonly ILauncherSettingsRepository _settings;
    private readonly TimeProvider _clock;

    /// <summary>Initialises a new instance.</summary>
    public UpdateInstallationHandler(IInstallationRepository installations, ILauncherSettingsRepository settings, TimeProvider clock)
    {
        _installations = installations;
        _settings = settings;
        _clock = clock;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<Success>> Handle(UpdateInstallationCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", description: $"No installation with id {command.InstallationId}.");
        }

        row.Name = command.Name.Trim();
        row.ModifiedUtc = _clock.GetUtcNow().UtcDateTime;

        await _installations.UpdateAsync(row, cancellationToken);

        if (command.MakeDefault)
        {
            LauncherSettings settings = await _settings.GetAsync(cancellationToken);

            if (settings.DefaultInstallationId != row.Id)
            {
                settings.DefaultInstallationId = row.Id;

                await _settings.UpdateAsync(settings, cancellationToken);
            }
        }

        return Result.Success;
    }
}
```

- [ ] **Step 4: Add the tests**

`UpdateInstallationHandlerTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class UpdateInstallationHandlerTests
{
    [Fact]
    public async Task Handle_RenamesRow_AndStampsModifiedUtc()
    {
        Guid id = Guid.CreateVersion7();
        DateTime fakeNow = new(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc);
        GameInstallation row = new() { Id = id, Name = "Original", Path = @"C:\Games\Main", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        UpdateInstallationHandler handler = new(installations, settings, new FakeTimeProvider(fakeNow));

        ErrorOr<Success> result = await handler.Handle(new UpdateInstallationCommand(id, "Renamed", MakeDefault: false), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        row.Name.ShouldBe("Renamed");
        row.ModifiedUtc.ShouldBe(fakeNow);
        await settings.DidNotReceive().UpdateAsync(Arg.Any<LauncherSettings>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PromotesToDefault_WhenMakeDefaultTrue()
    {
        Guid id = Guid.CreateVersion7();
        GameInstallation row = new() { Id = id, Name = "Main", Path = @"C:\Games\Main", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        LauncherSettings stored = new() { DefaultInstallationId = null };

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
        settings.GetAsync(Arg.Any<CancellationToken>()).Returns(stored);

        UpdateInstallationHandler handler = new(installations, settings, TimeProvider.System);

        await handler.Handle(new UpdateInstallationCommand(id, "Main", MakeDefault: true), CancellationToken.None);

        stored.DefaultInstallationId.ShouldBe(id);
        await settings.Received(1).UpdateAsync(stored, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenInstallationMissing()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((GameInstallation?)null);

        UpdateInstallationHandler handler = new(installations, Substitute.For<ILauncherSettingsRepository>(), TimeProvider.System);

        ErrorOr<Success> result = await handler.Handle(new UpdateInstallationCommand(Guid.CreateVersion7(), "Whatever", false), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }
}
```

`UpdateInstallationValidatorTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class UpdateInstallationValidatorTests
{
    [Fact]
    public async Task ExcludesSelfFromNameUniquenessCheck()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.ExistsByNameAsync("Main", id, Arg.Any<CancellationToken>()).Returns(false);

        UpdateInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new UpdateInstallationCommand(id, "Main", MakeDefault: false));

        result.IsValid.ShouldBeTrue();
    }
}
```

Extend Application unit test `GlobalUsings.cs`:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Installations.Update;
```

- [ ] **Step 5: Build + test + commit**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
git add Source/Erdmier.ZooTycoonLauncher.Application/Installations/Update Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m "feat(✨): add UpdateInstallationCommand (rename + optional default promotion)"
```

---

## Task 19: `SetDefaultInstallationCommand`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/SetDefault/SetDefaultInstallationCommand.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/SetDefault/SetDefaultInstallationHandler.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/SetDefaultInstallationHandlerTests.cs`

A focused command for "make this installation the default" — used by the Installation Manager dialogue's keyboard / context shortcut and by the boot pipeline's default-promotion path (§7.1.2).

- [ ] **Step 1: Add the command + handler**

`SetDefaultInstallationCommand.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.SetDefault;

/// <summary>Sets <c>LauncherSettings.DefaultInstallationId</c> to the supplied installation's id.</summary>
/// <param name="InstallationId">The installation to promote to default.</param>
public sealed record SetDefaultInstallationCommand(Guid InstallationId) : ICommand<ErrorOr<Success>>;
```

`SetDefaultInstallationHandler.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.SetDefault;

/// <summary>Handler for <see cref="SetDefaultInstallationCommand" />.</summary>
public sealed class SetDefaultInstallationHandler : ICommandHandler<SetDefaultInstallationCommand, ErrorOr<Success>>
{
    private readonly IInstallationRepository _installations;
    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    public SetDefaultInstallationHandler(IInstallationRepository installations, ILauncherSettingsRepository settings)
    {
        _installations = installations;
        _settings = settings;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<Success>> Handle(SetDefaultInstallationCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", description: $"No installation with id {command.InstallationId}.");
        }

        LauncherSettings settings = await _settings.GetAsync(cancellationToken);

        if (settings.DefaultInstallationId == row.Id)
        {
            return Result.Success;
        }

        settings.DefaultInstallationId = row.Id;
        await _settings.UpdateAsync(settings, cancellationToken);

        return Result.Success;
    }
}
```

- [ ] **Step 2: Add the test**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class SetDefaultInstallationHandlerTests
{
    [Fact]
    public async Task Handle_PromotesRowToDefault()
    {
        Guid id = Guid.CreateVersion7();
        GameInstallation row = new() { Id = id, Name = "Promoted", Path = @"C:\Games\Promoted", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        LauncherSettings settings = new() { DefaultInstallationId = Guid.CreateVersion7() };
        ILauncherSettingsRepository settingsRepo = Substitute.For<ILauncherSettingsRepository>();
        settingsRepo.GetAsync(Arg.Any<CancellationToken>()).Returns(settings);

        SetDefaultInstallationHandler handler = new(installations, settingsRepo);

        ErrorOr<Success> result = await handler.Handle(new SetDefaultInstallationCommand(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        settings.DefaultInstallationId.ShouldBe(id);
        await settingsRepo.Received(1).UpdateAsync(settings, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoOp_WhenAlreadyDefault()
    {
        Guid id = Guid.CreateVersion7();
        GameInstallation row = new() { Id = id, Name = "Already", Path = @"C:\Games\Already", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        LauncherSettings settings = new() { DefaultInstallationId = id };
        ILauncherSettingsRepository settingsRepo = Substitute.For<ILauncherSettingsRepository>();
        settingsRepo.GetAsync(Arg.Any<CancellationToken>()).Returns(settings);

        SetDefaultInstallationHandler handler = new(installations, settingsRepo);

        await handler.Handle(new SetDefaultInstallationCommand(id), CancellationToken.None);

        await settingsRepo.DidNotReceive().UpdateAsync(Arg.Any<LauncherSettings>(), Arg.Any<CancellationToken>());
    }
}
```

Extend `GlobalUsings.cs`:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Installations.SetDefault;
```

- [ ] **Step 3: Build + test + commit**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
git add Source/Erdmier.ZooTycoonLauncher.Application/Installations/SetDefault Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m "feat(✨): add SetDefaultInstallationCommand"
```

---

## Task 20: `DeleteInstallationCommand` with default-promotion

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Delete/DeleteInstallationCommand.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Delete/DeleteInstallationHandler.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/DeleteInstallationHandlerTests.cs`

SDD §7.2.4. The handler removes the row, deletes the per-installation database, and (when the removed row was the default) promotes the alphabetically-first remaining row.

- [ ] **Step 1: Add the command + handler**

`DeleteInstallationCommand.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>Removes the installation, deletes its per-installation database, and (when needed) promotes a new default.</summary>
/// <param name="InstallationId">The installation to remove.</param>
public sealed record DeleteInstallationCommand(Guid InstallationId) : ICommand<ErrorOr<DeleteInstallationResult>>;

/// <summary>Outcome of a <see cref="DeleteInstallationCommand" />.</summary>
/// <param name="RemovedWasDefault"><see langword="true" /> when the removed row was the launcher default before removal.</param>
/// <param name="NewDefaultInstallationId">When <see cref="RemovedWasDefault" /> is <see langword="true" />, the id of the promoted replacement, or <see langword="null" /> when no installations remain.</param>
public sealed record DeleteInstallationResult(bool RemovedWasDefault, Guid? NewDefaultInstallationId);
```

`DeleteInstallationHandler.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>Handler for <see cref="DeleteInstallationCommand" />.</summary>
public sealed class DeleteInstallationHandler : ICommandHandler<DeleteInstallationCommand, ErrorOr<DeleteInstallationResult>>
{
    private readonly IInstallationRepository _installations;
    private readonly ILauncherSettingsRepository _settings;
    private readonly IInstallationDbContextFactory _dbFactory;

    /// <summary>Initialises a new instance.</summary>
    public DeleteInstallationHandler(IInstallationRepository installations, ILauncherSettingsRepository settings, IInstallationDbContextFactory dbFactory)
    {
        _installations = installations;
        _settings = settings;
        _dbFactory = dbFactory;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<DeleteInstallationResult>> Handle(DeleteInstallationCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", description: $"No installation with id {command.InstallationId}.");
        }

        LauncherSettings settings = await _settings.GetAsync(cancellationToken);
        bool removedWasDefault = settings.DefaultInstallationId == row.Id;

        await _installations.DeleteAsync(row.Id, cancellationToken);

        Guid? newDefaultId = null;

        if (removedWasDefault)
        {
            GameInstallation? promotion = await _installations.FindDefaultPromotionCandidateAsync(cancellationToken);
            newDefaultId = promotion?.Id;
            settings.DefaultInstallationId = newDefaultId;

            await _settings.UpdateAsync(settings, cancellationToken);
        }

        await _dbFactory.DeleteAsync(row.Id, cancellationToken);

        return new DeleteInstallationResult(removedWasDefault, newDefaultId);
    }
}
```

- [ ] **Step 2: Add the tests**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class DeleteInstallationHandlerTests
{
    [Fact]
    public async Task Handle_RemovesRow_AndDeletesPerInstallationDb()
    {
        Guid id = Guid.CreateVersion7();
        GameInstallation row = new() { Id = id, Name = "Doomed", Path = @"C:\Games\Doomed", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
        settings.GetAsync(Arg.Any<CancellationToken>()).Returns(new LauncherSettings { DefaultInstallationId = Guid.CreateVersion7() });

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();

        DeleteInstallationHandler handler = new(installations, settings, dbFactory);

        ErrorOr<DeleteInstallationResult> result = await handler.Handle(new DeleteInstallationCommand(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.RemovedWasDefault.ShouldBeFalse();
        await installations.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
        await dbFactory.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PromotesAlphabeticallyFirstRow_WhenDefaultRemoved()
    {
        Guid removedId = Guid.CreateVersion7();
        Guid promotedId = Guid.CreateVersion7();

        GameInstallation row = new() { Id = removedId, Name = "Removed", Path = @"C:\Games\Removed", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };
        GameInstallation promotion = new() { Id = promotedId, Name = "Promoted", Path = @"C:\Games\Promoted", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(removedId, Arg.Any<CancellationToken>()).Returns(row);
        installations.FindDefaultPromotionCandidateAsync(Arg.Any<CancellationToken>()).Returns(promotion);

        LauncherSettings settings = new() { DefaultInstallationId = removedId };
        ILauncherSettingsRepository settingsRepo = Substitute.For<ILauncherSettingsRepository>();
        settingsRepo.GetAsync(Arg.Any<CancellationToken>()).Returns(settings);

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();

        DeleteInstallationHandler handler = new(installations, settingsRepo, dbFactory);

        ErrorOr<DeleteInstallationResult> result = await handler.Handle(new DeleteInstallationCommand(removedId), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.RemovedWasDefault.ShouldBeTrue();
        result.Value.NewDefaultInstallationId.ShouldBe(promotedId);
        settings.DefaultInstallationId.ShouldBe(promotedId);
        await settingsRepo.Received(1).UpdateAsync(settings, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetsDefaultToNull_WhenNoInstallationsRemain()
    {
        Guid removedId = Guid.CreateVersion7();
        GameInstallation row = new() { Id = removedId, Name = "Last", Path = @"C:\Games\Last", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(removedId, Arg.Any<CancellationToken>()).Returns(row);
        installations.FindDefaultPromotionCandidateAsync(Arg.Any<CancellationToken>()).Returns((GameInstallation?)null);

        LauncherSettings settings = new() { DefaultInstallationId = removedId };
        ILauncherSettingsRepository settingsRepo = Substitute.For<ILauncherSettingsRepository>();
        settingsRepo.GetAsync(Arg.Any<CancellationToken>()).Returns(settings);

        DeleteInstallationHandler handler = new(installations, settingsRepo, Substitute.For<IInstallationDbContextFactory>());

        ErrorOr<DeleteInstallationResult> result = await handler.Handle(new DeleteInstallationCommand(removedId), CancellationToken.None);

        result.Value.RemovedWasDefault.ShouldBeTrue();
        result.Value.NewDefaultInstallationId.ShouldBeNull();
        settings.DefaultInstallationId.ShouldBeNull();
    }
}
```

Extend `GlobalUsings.cs`:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Installations.Delete;
```

- [ ] **Step 3: Build + test + commit**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
git add Source/Erdmier.ZooTycoonLauncher.Application/Installations/Delete Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m "feat(✨): add DeleteInstallationCommand with default-promotion and per-install DB cleanup"
```

---

## Task 21: `RelocateInstallationCommand`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Relocate/RelocateInstallationCommand.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Relocate/RelocateInstallationValidator.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Relocate/RelocateInstallationHandler.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/RelocateInstallationHandlerTests.cs`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Installations/RelocateInstallationValidatorTests.cs`

SDD §7.2.5 — Fix Installation, `InvalidNoExe` path. Updates the `Path`, recomputes `HasExe` / `HasIni`, stamps `ModifiedUtc`. INI-related Fix sub-flows (`Create Default`, `Locate Manually`, `Copy From Another Installation`) defer to the INI slice.

Because `GameInstallation.Path` is `init`-only, the handler removes the old row and inserts a new one with the same `Id` and `AddedUtc` but the new `Path`. EF Core's `Remove`/`Add` within a single `SaveChangesAsync` produces an UPDATE on the SQLite side when the primary key matches — but we keep a fallback delete-then-add for clarity and to make the intent explicit.

Actually — looking again at the existing `LauncherSettingsRepository.UpdateAsync` path, EF Core's `Update` works on any tracked entity. The `init`-only restriction is *C# language-level*, not EF Core; the runtime can mutate the property via setter accessor. We will use reflection sparingly via a private helper, or instead model the relocation as "remove + insert with same Id" within a single transaction. For simplicity in this slice, do **delete-then-add** explicitly — the per-installation DB is preserved because we never call `_dbFactory.DeleteAsync` here.

- [ ] **Step 1: Add the command**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Relocate;

/// <summary>
/// Points an existing installation at a new folder, recomputes <c>HasExe</c> / <c>HasIni</c>, and stamps
/// <c>ModifiedUtc</c>. Implements SDD §7.2.5 for the EXE-relocation case.
/// </summary>
/// <param name="InstallationId">The installation's identifier.</param>
/// <param name="NewPath">The new fully qualified directory path.</param>
public sealed record RelocateInstallationCommand(Guid InstallationId, string NewPath) : ICommand<ErrorOr<RelocateInstallationResult>>;

/// <summary>Outcome of <see cref="RelocateInstallationCommand" />.</summary>
/// <param name="NewValidity">The validity computed after the move.</param>
public sealed record RelocateInstallationResult(InstallationValidity NewValidity);
```

- [ ] **Step 2: Add the validator**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Relocate;

/// <summary>FluentValidation rules for <see cref="RelocateInstallationCommand" />.</summary>
public sealed class RelocateInstallationValidator : AbstractValidator<RelocateInstallationCommand>
{
    private readonly IInstallationRepository _installations;

    /// <summary>Initialises a new instance.</summary>
    public RelocateInstallationValidator(IInstallationRepository installations)
    {
        _installations = installations;

        RuleFor(c => c.NewPath)
            .NotEmpty()
            .WithMessage("Path is required.")
            .MustAsync((command, path, cancellationToken) => PathIsUniqueAsync(command, path, cancellationToken))
            .WithMessage("Another installation already uses this folder.");
    }

    private async Task<bool> PathIsUniqueAsync(RelocateInstallationCommand command, string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        return !await _installations.ExistsByPathAsync(path, excludeId: command.InstallationId, cancellationToken);
    }
}
```

- [ ] **Step 3: Add the handler**

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Installations.Relocate;

/// <summary>Handler for <see cref="RelocateInstallationCommand" />.</summary>
public sealed class RelocateInstallationHandler : ICommandHandler<RelocateInstallationCommand, ErrorOr<RelocateInstallationResult>>
{
    private readonly IInstallationRepository _installations;
    private readonly IInstallationVerifier _verifier;
    private readonly TimeProvider _clock;

    /// <summary>Initialises a new instance.</summary>
    public RelocateInstallationHandler(IInstallationRepository installations, IInstallationVerifier verifier, TimeProvider clock)
    {
        _installations = installations;
        _verifier = verifier;
        _clock = clock;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<RelocateInstallationResult>> Handle(RelocateInstallationCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", description: $"No installation with id {command.InstallationId}.");
        }

        VerificationResult verification = await _verifier.VerifyAsync(command.NewPath, cancellationToken);

        if (!verification.DirectoryExists)
        {
            return Error.Validation(code: "Installation.PathMissing", description: $"The folder \"{command.NewPath}\" does not exist.");
        }

        // GameInstallation.Path is init-only — model the relocation as remove + add with the same Id and AddedUtc.
        GameInstallation relocated = new()
        {
            Id = row.Id,
            Name = row.Name,
            Path = command.NewPath,
            HasExe = verification.HasExe,
            HasIni = verification.HasIni,
            AddedUtc = row.AddedUtc,
            ModifiedUtc = _clock.GetUtcNow().UtcDateTime,
            LastPlayedUtc = row.LastPlayedUtc,
            LastOpenedUtc = row.LastOpenedUtc,
        };

        await _installations.DeleteAsync(row.Id, cancellationToken);
        await _installations.AddAsync(relocated, cancellationToken);

        return new RelocateInstallationResult(verification.Validity);
    }
}
```

- [ ] **Step 4: Add tests**

`RelocateInstallationHandlerTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class RelocateInstallationHandlerTests
{
    [Fact]
    public async Task Handle_PointsRowAtNewPath_AndRecomputesFlags()
    {
        Guid id = Guid.CreateVersion7();
        GameInstallation row = new() { Id = id, Name = "Main", Path = @"C:\Games\Old", HasExe = false, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(@"C:\Games\New", Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        DateTime fakeNow = new(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc);

        RelocateInstallationHandler handler = new(installations, verifier, new FakeTimeProvider(fakeNow));

        ErrorOr<RelocateInstallationResult> result = await handler.Handle(new RelocateInstallationCommand(id, @"C:\Games\New"), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.NewValidity.ShouldBe(InstallationValidity.Valid);
        await installations.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
        await installations.Received(1).AddAsync(Arg.Is<GameInstallation>(i => i.Id == id && i.Path == @"C:\Games\New" && i.ModifiedUtc == fakeNow), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsValidationError_WhenNewDirectoryMissing()
    {
        Guid id = Guid.CreateVersion7();
        GameInstallation row = new() { Id = id, Name = "Main", Path = @"C:\Games\Old", AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(@"C:\Missing", Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: false, HasExe: false, HasIni: false));

        RelocateInstallationHandler handler = new(installations, verifier, TimeProvider.System);

        ErrorOr<RelocateInstallationResult> result = await handler.Handle(new RelocateInstallationCommand(id, @"C:\Missing"), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Installation.PathMissing");
        await installations.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
```

`RelocateInstallationValidatorTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class RelocateInstallationValidatorTests
{
    [Fact]
    public async Task ExcludesSelfFromPathUniquenessCheck()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.ExistsByPathAsync(@"C:\Games\Self", id, Arg.Any<CancellationToken>()).Returns(false);

        RelocateInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new RelocateInstallationCommand(id, @"C:\Games\Self"));

        result.IsValid.ShouldBeTrue();
    }
}
```

Extend `GlobalUsings.cs`:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Installations.Relocate;
```

- [ ] **Step 5: Build + test + commit**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
git add Source/Erdmier.ZooTycoonLauncher.Application/Installations/Relocate Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m "feat(✨): add RelocateInstallationCommand for the Fix-EXE dialogue flow"
```

---

## Task 22: Wire `AddApplication()` into `AppStartup`

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AppStartup.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`

- [ ] **Step 1: Extend `AppStartup`**

Replace the contents of `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AppStartup.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>
///     Composition-root helper that wires all layers into a <see cref="ServiceProvider" /> and runs any startup side effects (e.g. EF Core migrations) that must complete before
///     the application presents its first window.
/// </summary>
internal static class AppStartup
{
    /// <summary>Builds the fully-composed <see cref="ServiceProvider" /> and runs startup tasks.</summary>
    /// <returns>The ready-to-use <see cref="ServiceProvider" />.</returns>
    internal static ServiceProvider BuildAndInitialise()
    {
        ServiceCollection services = new();

        services.AddInfrastructure();
        services.AddApplication();
        services.AddDesktop();

        ServiceProvider provider = services.BuildServiceProvider();

        // Run migrations synchronously — the application can't usefully start without the DB.
        provider.MigrateLauncherDatabaseAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();

        return provider;
    }
}
```

- [ ] **Step 2: Extend Desktop `GlobalUsings.cs`**

Add (kept in the existing alphabetical group):

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Common.Extensions;
```

- [ ] **Step 3: Build + run the launcher to confirm boot still succeeds**

```powershell
dotnet build Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj
Remove-Item -Force "$env:LOCALAPPDATA\ZooTycoonLauncher\Data\Launcher.db" -ErrorAction SilentlyContinue
Start-Job -ScriptBlock { dotnet run --project D:\Source\Personal\Erdmier.ZooTycoonLauncher\Source\Erdmier.ZooTycoonLauncher.Desktop\Erdmier.ZooTycoonLauncher.Desktop.csproj --no-build } -Name launcher
Start-Sleep -Seconds 15
$db = Test-Path "$env:LOCALAPPDATA\ZooTycoonLauncher\Data\Launcher.db"
Stop-Job -Name launcher -ErrorAction SilentlyContinue; Remove-Job -Name launcher -Force -ErrorAction SilentlyContinue
"db=$db"
```

Expected: `db=True`.

- [ ] **Step 4: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher
git commit -m "feat(✨): wire AddApplication into AppStartup so Mediator dispatches at runtime"
```

---

## Task 23: Final sanity sweep

- [ ] **Step 1: Clean + build Source + Tests**

```powershell
dotnet clean Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj
dotnet clean Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj
dotnet clean Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj
dotnet build Source/Erdmier.ZooTycoonLauncher.Application/Erdmier.ZooTycoonLauncher.Application.csproj
dotnet build Source/Erdmier.ZooTycoonLauncher.Infrastructure/Erdmier.ZooTycoonLauncher.Infrastructure.csproj
dotnet build Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj
```

Expected: SUCCEEDED, zero warnings, zero errors.

- [ ] **Step 2: Run every test project**

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj
dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj
dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/Erdmier.ZooTycoonLauncher.Tests.Architecture.csproj
```

Expected totals:
- Domain.Tests.Unit: 6 passed (unchanged).
- Application.Tests.Unit: ~16 passed (1 existing + 15 new across the slices).
- Infrastructure.Tests.Integration: ~16 passed (3 existing + 13 new).
- Tests.Architecture: 13 passed (unchanged; the existing rules cover the new files automatically).

- [ ] **Step 3: Boot verification**

```powershell
Remove-Item -Force "$env:LOCALAPPDATA\ZooTycoonLauncher\Data\Launcher.db" -ErrorAction SilentlyContinue
Start-Job -ScriptBlock { dotnet run --project D:\Source\Personal\Erdmier.ZooTycoonLauncher\Source\Erdmier.ZooTycoonLauncher.Desktop\Erdmier.ZooTycoonLauncher.Desktop.csproj --no-build } -Name launcher
Start-Sleep -Seconds 15
$db = Test-Path "$env:LOCALAPPDATA\ZooTycoonLauncher\Data\Launcher.db"
$log = Test-Path "$env:LOCALAPPDATA\ZooTycoonLauncher\Logs"
Stop-Job -Name launcher -ErrorAction SilentlyContinue; Remove-Job -Name launcher -Force -ErrorAction SilentlyContinue
"db=$db log=$log"
```

Expected: `db=True log=True`. The launcher still opens its placeholder banner window — the new Application/Infrastructure surface is wired but no Desktop dialogue consumes it yet.

- [ ] **Step 4: Milestone marker commit (empty)**

```powershell
git commit --allow-empty -m "chore(🔧): installation lifecycle core complete — solution builds, tests green, app boots"
```

---

## Self-review

**Spec coverage (SDD §7.2 + §8.5):**

| Requirement | Task |
|---|---|
| §7.2.1 Add Installation — verify, persist row, set default for first row, create per-install DB, capture Original (deferred) | Task 17 |
| §7.2.1 Auto-locator via registry + Program Files + persisted last-known | Tasks 8 + 10 + 16 |
| §7.2.2 Installation Manager (read API + default-flag projection) | Task 14 |
| §7.2.3 Edit Installation (rename + default promote) | Task 18 |
| §7.2.4 Delete Installation with default-promotion + per-install DB cleanup | Task 20 |
| §7.2.5 Fix Installation (EXE relocation only; INI sub-flows deferred to INI slice) | Task 21 |
| §7.2.6 Info dialogue (read-only) — relies on Task 14 projection | Task 14 |
| §7.2.7 Picker — out of scope (boot/startup-flow slice consumes Task 14) | n/a |
| §8.5 Auto-locator algorithm + trail | Task 10 |
| Per-installation DB provisioning | Tasks 11 + 12 |
| Verifier abstraction | Task 9 |
| Messaging types (Added/Changed/Deleted/DefaultChanged) | Task 2 |
| Mediator + FluentValidation pipeline | Task 6 |

**Out-of-scope, deferred to other plans:**
- INI parser, snapshot persistence, `IIniSnapshotService` real impl — INI Config slice.
- Boot pipeline / state machine that consumes `IInstallationLocator` / `IInstallationVerifier` — startup-flow slice.
- Desktop dialogues — UI slice.

**Placeholder scan:** None. Every step has the actual code or command.

**Type consistency:**
- `IInstallationRepository.GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `ExistsByNameAsync`, `ExistsByPathAsync`, `FindDefaultPromotionCandidateAsync` are defined in Task 5 and consumed verbatim in Tasks 14, 15, 17, 18, 19, 20, 21.
- `VerificationResult(DirectoryExists, HasExe, HasIni)` is defined in Task 3 and consumed verbatim in Tasks 9, 15, 17, 21.
- `LocatedDirectory(Path, Trail)`, `LocationProbeAttempt(Source, CandidatePath, Failure)`, `LocationProbeFailure` enum — defined in Task 3, consumed in Task 10 + Task 16.
- `InstallationSummary` shape — defined in Task 3, consumed unchanged in Task 14.
- `IIniSnapshotService.CaptureOriginalAsync(GameInstallation, CancellationToken)` — defined in Task 4, stubbed in Task 12, called in Task 17.
- `IInstallationDbContextFactory.CreateAsync(Guid, CancellationToken)` / `DeleteAsync(Guid, CancellationToken)` — defined in Task 4, implemented in Task 12, called in Tasks 17 + 20.
- `FakeTimeProvider` is shared across handler tests — declared once in `VerifyInstallationHandlerTests.cs` (Task 15) and reused in Tasks 17, 18, 21. The visibility is `internal` and the namespace is `Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations`; co-location keeps the helper next to its first consumer.

**Convention reminders for the implementer:**
- Do not add `[ UsedImplicitly ]` to *any* new class in this plan, even on DI-registered handlers, validators, and EF configurations. The user adds the attribute himself when ReSharper flags a type.
- Match the existing house style for attribute brackets where attributes *are* used (`[ STAThread ]` spaced).
- Every new file lives under a folder that mirrors its namespace; the only allowed project-root file remains `GlobalUsings.cs`.
- Tests use Shouldly + NSubstitute + xUnit per the foundations precedent.

---

## Plan complete

Save and commit this plan, then execute via `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans`.
