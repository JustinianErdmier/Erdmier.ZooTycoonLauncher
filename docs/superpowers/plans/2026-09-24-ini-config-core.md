# INI Config Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the INI Config tab real — parse `zoo.ini` into per-installation snapshots with tiered drift detection, edit every recognised user setting across seven sections,
and save atomically with history — replacing the `NullIniSnapshotService` stub.

**Architecture:** A pure Domain engine (`ZooIniDefaults` key registry, byte-faithful `IniDocument`, `IniDriftDetector`) feeds an Application-layer `IniReconciler` /
`IniSnapshotService` / `SaveIniHandler` that orchestrate SDD §8.2 through two new seams (`IIniFileStore`, `IIniSnapshotRepository` + transaction) implemented in
Infrastructure. The Desktop editor is registry-driven: a presentation catalogue, generic field view models resolved by the `ViewLocator`, one generic section view, and a
pending-changes guard wired into the main window.

**Tech Stack:** .NET 10, C# latest, Avalonia 11.3.14 + Classic.Avalonia 11.3.0.3, CommunityToolkit.Mvvm 8.4.2, Mediator 3.0.2 (source-generated), ErrorOr 2.1.1,
FluentValidation 12.1.1, Ardalis.SmartEnum 8.2.0, EF Core 10.0.9 + SQLite, System.IO.Abstractions 22.1.1, xUnit + Shouldly + NSubstitute.

**Spec:** [`docs/superpowers/specs/2026-09-24-ini-config-core-design.md`](../specs/2026-09-24-ini-config-core-design.md) — read it before starting any task. The SDD
([`2026-05-26-zoo-tycoon-launcher-design.md`](../specs/2026-05-26-zoo-tycoon-launcher-design.md)) remains authoritative for anything this plan does not cover.

## Global Constraints

- **Commits are pre-approved for this run.** Commit at the end of every task with the message given; CLAUDE.md's ask-before-commit rule is waived for this plan's task
  commits by the maintainer. Never commit `.claude/settings.local.json`.
- Commit format: `type(emoji): subject` (`feat`→✨, `fix`→🐛, `refactor`→♻️, `test`→✅, `docs`→📝, `chore`→🔧, `build`→📦), British English, body ending with the trailer
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Use a PowerShell here-string (`git commit -m @'…'@` with the closing `'@` at column 0).
- **Run shell commands via PowerShell**, never Bash. Build narrowly: `dotnet build Source/<Project>/<Project>.csproj`; test narrowly:
  `dotnet test Tests/<Project>/<Project>.csproj --filter "FullyQualifiedName~<Class>"`. The worktree is not open in Rider, so file locks should not occur; if one does,
  stop and report rather than investigating.
- `Directory.Build.props` sets `TreatWarningsAsErrors`, `Nullable`, `ImplicitUsings`: every nullable warning is a build error.
- **One type per file**, named after the type, in a folder mirroring its namespace; **no files at a project root**; file-scoped namespaces.
- Add `using` directives to the assembly's `GlobalUsings.cs` **only once the namespace exists** (a global using of an empty namespace is a build error). Keep a local `using` only
  for genuine conflicts (precedent: `LaunchGameHandler` keeps a local `using Microsoft.Extensions.Logging;`).
- XML doc comments (`<summary>`, `<param>`, `<returns>`) on every public type and member; `<c>…</c>` never has inside whitespace; British English everywhere (`Initialise`,
  `Behaviour`, `Colour`, `Serialise`).
- Attribute style is spaced: `[ Fact ]`, `[ ObservableProperty ]`, `[ RelayCommand ]`. **Do not add `[ UsedImplicitly ]`.**
- Separate consecutive statements of different kinds with a blank line (declaration, then invocation, etc.).
- UTC for every stored timestamp; localise only at the UI boundary.
- File access only through `System.IO.Abstractions.IFileSystem`.
- Source-generated MVVM (`[ ObservableProperty ]` on `partial` properties, `[ RelayCommand ]` on private methods); every XAML file declares `x:DataType`; every view model has a
  parameterless designer constructor delegating to `file`-scoped null objects.
- Every concrete public `*ViewModel` in Desktop has a sibling `*View.axaml` (+ `.axaml.cs`) in the parallel `Views/…` folder.
- Markdown files are hard-wrapped at 180 characters (tables, fenced code, headings, URLs exempt); verify with PowerShell `[string].Length`.
- No new NuGet packages. The new test project uses only centrally managed versions already in `Directory.Packages.props`.

## Review Focus

- **An emptied `zoo.ini` (0 bytes)** — first import succeeds with no rows, and Save appends `[section]` headers plus keys instead of failing. Pinned in Task 11.
- **A section header whose casing differs from the registry (`[ui]`)** — an inserted key lands in the existing section; no duplicate `[UI]` is created. Pinned in Task 2.
- **A non-nullable number box cleared to empty** — Save returns a readable validation error (shown in the error dialogue) and keeps the edits; nothing crashes. Pinned in
  Tasks 11 and 15.
- **Rapid tab switching** — a second INI-tab activation while the first load is still in flight must not create a second editor. Pinned in Task 16.
- **Non-ASCII bytes elsewhere in the file** (a Windows-1252 `lastfile=C:\Spiele\Zoo Müller.zoo`) — saving an unrelated key writes those bytes back unchanged. Pinned in
  Tasks 6 and 11.

---

## File map

```text
Source/Erdmier.ZooTycoonLauncher.Domain/
  IniKeys/        IniKeyId, IniKeyRole, IniKeySpec, ZooIniDefaults                               (Tasks 1, 3)
  IniDocuments/   IniDocument, IniLine, IniSectionHeader, IniKeyValue, IniComment, IniBlank      (Task 2)
  IniDrift/       IniDriftResult, IniDriftKind, IniDriftDetector                                        (Task 4)
  IniSnapshots/IniSnapshot.cs                                                                    (Task 7: CapturedUtc settable)
Source/Erdmier.ZooTycoonLauncher.Application/
  Common/Abstractions/  IIniFileStore, IIniSnapshotRepository, IIniSnapshotTransaction (Task 5); IIniSnapshotService (+LoadAsync, Task 8)
  Common/Models/        IniFileContent, IniValueChange, IniConfigResult                          (Task 5)
  IniConfig/Common/     IniErrors, IniReconciler, IniReconciliation, IniReconciliationOutcome (Task 5); IniSnapshotService (Task 8)
  IniConfig/Get/        GetIniConfigQuery, GetIniConfigHandler                                   (Task 10)
  IniConfig/Save/       SaveIniCommand, SaveIniValidator, SaveIniHandler                         (Task 11)
  Boot/BootResult.cs, Boot/BootHandler.cs, Game/Launch/LaunchGameHandler.cs, Installations/Add/AddInstallationHandler.cs   (Task 9)
Source/Erdmier.ZooTycoonLauncher.Infrastructure/
  IniConfig/IniFileStore.cs                                                                      (Task 6)
  Persistence/Installation/  IniSnapshotRepository, IniSnapshotTransaction, InstallationDbContextFactory (OpenAsync),
                             Configurations/IniSnapshotConfiguration (indexes), Migrations/*_AddSnapshotIndexes   (Task 7)
  IniSnapshots/NullIniSnapshotService.cs  (deleted, Task 8)
Source/Erdmier.ZooTycoonLauncher.Desktop/
  Models/IniConfig/          catalogue types + IniEditorCatalogue                                (Task 12)
  ViewModels/IniConfig/Fields/ + Views/IniConfig/Fields/                                         (Task 13)
  Composition/ (SaveChangesChoice, IDialogService, AvaloniaDialogService) + Dialogs prompt/error pairs   (Task 14)
  ViewModels/IniConfig/ (IniFieldGroup, IniFieldFactory, IniSectionViewModel, IniPlaceholderViewModel, IniEditorViewModel) + views (Task 15)
  ViewModels/Tabs/IniConfigTabViewModel.cs + Views/Tabs/IniConfigTabView.axaml                   (Task 16)
  ViewModels/Common/IPendingChangesGuard.cs, Boot/PlayViewModel, Tabs/GeneralTabViewModel + views (Task 17)
  ViewModels/MainWindowViewModel.cs, Views/MainWindow.axaml.cs                                   (Task 18)
Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/  (new project, Task 12)
docs/  SDD rev 1.7, conventions.md, manual-end-to-end-tests.md                                   (Task 19)
```

---

## Task 1: `IniKeyId`, `IniKeyRole`, `IniKeySpec`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniKeys/IniKeyId.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniKeys/IniKeyRole.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniKeys/IniKeySpec.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Domain/GlobalUsings.cs`
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/GlobalUsings.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/IniKeys/IniKeyIdTests.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/IniKeys/IniKeySpecTests.cs`

**Interfaces:**
- Consumes: existing `IniValueKind` SmartEnum (`Bool`, `Int`, `NullableInt`, `Str`, `NullableStr`, `Scenario`) in `Domain.IniSnapshots`.
- Produces:
  - `readonly record struct IniKeyId(string Section, string Key)` — case-insensitive `Equals` / `GetHashCode`; `ToString()` → `"[Section]/Key"`.
  - `sealed class IniKeyRole : SmartEnum<IniKeyRole>` with `UserSetting` (1), `GameManaged` (2).
  - `sealed record IniKeySpec(IniKeyId Id, IniValueKind Kind, string? DefaultValue, int? Min, int? Max, IniKeyRole Role)` with `bool IsValid(string? raw)`,
    `bool AreEquivalent(string? left, string? right)`, `string? EffectiveValue(string? raw)`.

- [ ] **Step 1: Add the Domain global usings**

Replace `Source/Erdmier.ZooTycoonLauncher.Domain/GlobalUsings.cs` with:

```csharp
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;

global using Ardalis.SmartEnum;

global using ErrorOr;

global using Erdmier.ZooTycoonLauncher.Domain.IniKeys;
global using Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

global using JetBrains.Annotations;
```

Append to `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/GlobalUsings.cs` (after the existing `Domain.IniSnapshots` line):

```csharp
global using Erdmier.ZooTycoonLauncher.Domain.IniKeys;
```

- [ ] **Step 2: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/IniKeys/IniKeyIdTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.IniKeys;

public sealed class IniKeyIdTests
{
    [ Fact ]
    public void Equals_IgnoresCaseOnSectionAndKey()
    {
        IniKeyId registry = new(Section: "UI", Key: "tooltipDelay");
        IniKeyId fromFile = new(Section: "ui", Key: "TOOLTIPDELAY");

        (registry == fromFile).ShouldBeTrue();
        registry.GetHashCode().ShouldBe(fromFile.GetHashCode());
    }

    [ Fact ]
    public void Equals_DistinguishesDifferentKeys()
    {
        IniKeyId left  = new(Section: "UI", Key: "keyScrollX");
        IniKeyId right = new(Section: "UI", Key: "keyScrollY");

        (left == right).ShouldBeFalse();
    }

    [ Fact ]
    public void ToString_FormatsAsBracketedSectionSlashKey()
    {
        IniKeyId id = new(Section: "UI", Key: "tooltipDelay");

        id.ToString().ShouldBe(expected: "[UI]/tooltipDelay");
    }

    [ Fact ]
    public void Default_HasAStableHashCode()
    {
        IniKeyId id = default;

        Should.NotThrow(() => id.GetHashCode());
    }
}
```

`Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/IniKeys/IniKeySpecTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.IniKeys;

public sealed class IniKeySpecTests
{
    private static readonly IniKeySpec BoolSpec = new(new IniKeyId(Section: "user", Key: "fullscreen"), IniValueKind.Bool, DefaultValue: "1", Min: null, Max: null,
                                                      IniKeyRole.UserSetting);

    private static readonly IniKeySpec IntSpec = new(new IniKeyId(Section: "user", Key: "UpdateRate"), IniValueKind.Int, DefaultValue: "15", Min: 1, Max: 60,
                                                     IniKeyRole.UserSetting);

    private static readonly IniKeySpec NullableIntSpec = new(new IniKeyId(Section: "UI", Key: "lastWindowX"), IniValueKind.NullableInt, DefaultValue: null, Min: null,
                                                             Max: null, IniKeyRole.GameManaged);

    private static readonly IniKeySpec StrSpec = new(new IniKeyId(Section: "UI", Key: "menuMusic"), IniValueKind.Str, DefaultValue: "sounds/mainmenu.wav", Min: null,
                                                     Max: null, IniKeyRole.UserSetting);

    private static readonly IniKeySpec NullableStrSpec = new(new IniKeyId(Section: "user", Key: "lastfile"), IniValueKind.NullableStr, DefaultValue: null, Min: null,
                                                             Max: null, IniKeyRole.GameManaged);

    [ Theory ]
    [ InlineData("0", true) ]
    [ InlineData("1", true) ]
    [ InlineData("TRUE", true) ]
    [ InlineData(" false ", true) ]
    [ InlineData("2", false) ]
    [ InlineData("", false) ]
    [ InlineData(null, false) ]
    public void IsValid_Bool(string? raw, bool expected) => BoolSpec.IsValid(raw).ShouldBe(expected);

    [ Theory ]
    [ InlineData("1", true) ]
    [ InlineData("60", true) ]
    [ InlineData(" 015 ", true) ]
    [ InlineData("0", false) ]
    [ InlineData("61", false) ]
    [ InlineData("abc", false) ]
    [ InlineData("", false) ]
    [ InlineData(null, false) ]
    public void IsValid_BoundedInt(string? raw, bool expected) => IntSpec.IsValid(raw).ShouldBe(expected);

    [ Theory ]
    [ InlineData(null, true) ]
    [ InlineData("", true) ]
    [ InlineData("-40", true) ]
    [ InlineData("x", false) ]
    public void IsValid_NullableInt(string? raw, bool expected) => NullableIntSpec.IsValid(raw).ShouldBe(expected);

    [ Theory ]
    [ InlineData("", true) ]
    [ InlineData("sounds/other.wav", true) ]
    [ InlineData(null, false) ]
    public void IsValid_Str(string? raw, bool expected) => StrSpec.IsValid(raw).ShouldBe(expected);

    [ Theory ]
    [ InlineData(null, true) ]
    [ InlineData("", true) ]
    [ InlineData(@"C:\Saves\zoo1.zoo", true) ]
    public void IsValid_NullableStr(string? raw, bool expected) => NullableStrSpec.IsValid(raw).ShouldBe(expected);

    [ Theory ]
    [ InlineData("1", "true", true) ]
    [ InlineData("0", "FALSE", true) ]
    [ InlineData("1", "0", false) ]
    [ InlineData("1", null, false) ]
    [ InlineData(null, null, true) ]
    public void AreEquivalent_Bool(string? left, string? right, bool expected) => BoolSpec.AreEquivalent(left, right).ShouldBe(expected);

    [ Theory ]
    [ InlineData("75", "075", true) ]
    [ InlineData(" 15", "15 ", true) ]
    [ InlineData("15", "16", false) ]
    [ InlineData("abc", "abc", true) ]
    [ InlineData("abc", "15", false) ]
    [ InlineData("15", null, false) ]
    public void AreEquivalent_Int(string? left, string? right, bool expected) => IntSpec.AreEquivalent(left, right).ShouldBe(expected);

    [ Theory ]
    [ InlineData("", null, true) ]
    [ InlineData("  ", null, true) ]
    [ InlineData("10", "010", true) ]
    [ InlineData("10", null, false) ]
    public void AreEquivalent_NullableInt(string? left, string? right, bool expected) => NullableIntSpec.AreEquivalent(left, right).ShouldBe(expected);

    [ Theory ]
    [ InlineData("a", " a ", true) ]
    [ InlineData("a", "A", false) ]
    [ InlineData("", null, false) ]
    public void AreEquivalent_Str(string? left, string? right, bool expected) => StrSpec.AreEquivalent(left, right).ShouldBe(expected);

    [ Theory ]
    [ InlineData("", null, true) ]
    [ InlineData("x", "x", true) ]
    [ InlineData("x", null, false) ]
    public void AreEquivalent_NullableStr(string? left, string? right, bool expected) => NullableStrSpec.AreEquivalent(left, right).ShouldBe(expected);

    [ Theory ]
    [ InlineData("30", "30") ]
    [ InlineData(" 30 ", "30") ]
    [ InlineData("99", "15") ]
    [ InlineData("abc", "15") ]
    [ InlineData(null, "15") ]
    public void EffectiveValue_Int_FallsBackToDefaultWhenInvalid(string? raw, string expected) => IntSpec.EffectiveValue(raw).ShouldBe(expected);

    [ Fact ]
    public void EffectiveValue_NullableKinds_KeepAbsentAsNull()
    {
        NullableIntSpec.EffectiveValue(raw: null).ShouldBeNull();
        NullableStrSpec.EffectiveValue(raw: null).ShouldBeNull();
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj --filter "FullyQualifiedName~IniKey"`
Expected: build FAILS — `IniKeyId`, `IniKeySpec`, `IniKeyRole` do not exist.

- [ ] **Step 4: Implement `IniKeyId`**

`Source/Erdmier.ZooTycoonLauncher.Domain/IniKeys/IniKeyId.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniKeys;

/// <summary>
///     Identifies one <c>zoo.ini</c> key by its section and key name. Equality and hashing ignore case on both parts, matching how the game reads the file (SDD §5.3).
/// </summary>
/// <param name="Section">The section name without brackets, e.g. <c>UI</c>.</param>
/// <param name="Key">The key name, e.g. <c>tooltipDelay</c>.</param>
public readonly record struct IniKeyId(string Section, string Key)
{
    /// <summary>Compares both parts case-insensitively.</summary>
    /// <param name="other">The id to compare with.</param>
    /// <returns><see langword="true" /> when section and key match ignoring case.</returns>
    public bool Equals(IniKeyId other)
        => string.Equals(Section, other.Section, StringComparison.OrdinalIgnoreCase)
           && string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns a hash code consistent with the case-insensitive <see cref="Equals(IniKeyId)" />.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
        => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(Section ?? string.Empty),
                            StringComparer.OrdinalIgnoreCase.GetHashCode(Key ?? string.Empty));

    /// <summary>Formats the id as <c>[Section]/Key</c>.</summary>
    /// <returns>The formatted id.</returns>
    public override string ToString() => $"[{Section}]/{Key}";
}
```

- [ ] **Step 5: Implement `IniKeyRole`**

`Source/Erdmier.ZooTycoonLauncher.Domain/IniKeys/IniKeyRole.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniKeys;

/// <summary>Whether an INI key is a setting the user edits or runtime state the game maintains itself (tiered drift, SDD §7.7).</summary>
public sealed class IniKeyRole : SmartEnum<IniKeyRole>
{
    /// <summary>Written by the game on exit (window position, last file, tutorial flags). Hidden from the editor; a change is adopted without archiving.</summary>
    public static readonly IniKeyRole GameManaged = new(name: "GameManaged", id: 2);

    /// <summary>A setting the user edits in the INI Config tab. A change made outside the launcher archives <c>Current</c> to <c>Historical</c>.</summary>
    public static readonly IniKeyRole UserSetting = new(name: "UserSetting", id: 1);

    private IniKeyRole(string name, int id)
        : base(name, id)
    { }
}
```

- [ ] **Step 6: Implement `IniKeySpec`**

`Source/Erdmier.ZooTycoonLauncher.Domain/IniKeys/IniKeySpec.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniKeys;

/// <summary>
///     Describes one recognised <c>zoo.ini</c> key: its kind, factory default, numeric bounds, and role. Values stay raw strings everywhere; this type supplies the kind-aware
///     rules for validating and comparing them (SDD §5.3).
/// </summary>
/// <param name="Id">The key's section and name, in the casing the launcher writes when it inserts the key.</param>
/// <param name="Kind">The value's kind.</param>
/// <param name="DefaultValue">The factory default in INI text form, or <see langword="null" /> when the key has none.</param>
/// <param name="Min">The inclusive lower bound for integer kinds, or <see langword="null" /> when unbounded.</param>
/// <param name="Max">The inclusive upper bound for integer kinds, or <see langword="null" /> when unbounded.</param>
/// <param name="Role">Whether the key is a user setting or game-managed runtime state.</param>
public sealed record IniKeySpec(IniKeyId Id, IniValueKind Kind, string? DefaultValue, int? Min, int? Max, IniKeyRole Role)
{
    /// <summary>Returns whether <paramref name="raw" /> is a valid value for this key. <see langword="null" /> means the key is absent from the file.</summary>
    /// <param name="raw">The raw value; surrounding whitespace is ignored.</param>
    /// <returns><see langword="true" /> when the value is valid for the key's kind and bounds.</returns>
    public bool IsValid(string? raw)
    {
        string? trimmed = raw?.Trim();

        if (Kind == IniValueKind.Bool)
        {
            return trimmed is not null && TryParseBool(trimmed, out bool _);
        }

        if (Kind == IniValueKind.Int)
        {
            return trimmed is not null && TryParseBoundedInt(trimmed);
        }

        if (Kind == IniValueKind.NullableInt)
        {
            return string.IsNullOrEmpty(trimmed) || TryParseBoundedInt(trimmed);
        }

        if (Kind == IniValueKind.Str)
        {
            return trimmed is not null;
        }

        return true;
    }

    /// <summary>
    ///     Returns whether two raw values denote the same setting: booleans by meaning (<c>1</c> ≡ <c>true</c>), integers numerically (<c>075</c> ≡ <c>75</c>), nullable kinds
    ///     treating empty as absent, everything else by ordinal comparison of the trimmed text.
    /// </summary>
    /// <param name="left">The first raw value, or <see langword="null" /> when absent.</param>
    /// <param name="right">The second raw value, or <see langword="null" /> when absent.</param>
    /// <returns><see langword="true" /> when the values are equivalent.</returns>
    public bool AreEquivalent(string? left, string? right)
    {
        string? a = left?.Trim();
        string? b = right?.Trim();

        if (Kind == IniValueKind.NullableInt
            || Kind == IniValueKind.NullableStr)
        {
            a = string.IsNullOrEmpty(a) ? null : a;
            b = string.IsNullOrEmpty(b) ? null : b;
        }

        if (a is null
            || b is null)
        {
            return a is null && b is null;
        }

        if (Kind == IniValueKind.Bool
            && TryParseBool(a, out bool leftBool)
            && TryParseBool(b, out bool rightBool))
        {
            return leftBool == rightBool;
        }

        if ((Kind == IniValueKind.Int || Kind == IniValueKind.NullableInt)
            && int.TryParse(a, NumberStyles.Integer, CultureInfo.InvariantCulture, out int leftInt)
            && int.TryParse(b, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rightInt))
        {
            return leftInt == rightInt;
        }

        return string.Equals(a, b, StringComparison.Ordinal);
    }

    /// <summary>Returns the value the editor should display: the trimmed raw value when valid, otherwise <see cref="DefaultValue" /> (the silent fallback of SDD §5.3).</summary>
    /// <param name="raw">The raw value, or <see langword="null" /> when absent.</param>
    /// <returns>The effective value.</returns>
    public string? EffectiveValue(string? raw) => IsValid(raw) ? raw?.Trim() : DefaultValue;

    private static bool TryParseBool(string value, out bool result)
    {
        switch (value)
        {
            case "0":
                result = false;

                return true;

            case "1":
                result = true;

                return true;

            default:
                return bool.TryParse(value, out result);
        }
    }

    private bool TryParseBoundedInt(string value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
           && (Min is null || parsed >= Min)
           && (Max is null || parsed <= Max);
}
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj --filter "FullyQualifiedName~IniKey"`
Expected: PASS (all `IniKeyIdTests` and `IniKeySpecTests`).

- [ ] **Step 8: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Domain Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit
git commit -m @'
feat(✨): add IniKeyId, IniKeyRole and IniKeySpec to the Domain

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 2: `IniDocument` — byte-faithful parse, render, and set

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniDocuments/IniLine.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniDocuments/IniSectionHeader.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniDocuments/IniKeyValue.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniDocuments/IniComment.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniDocuments/IniBlank.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniDocuments/IniDocument.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Domain/GlobalUsings.cs`, `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/GlobalUsings.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/IniDocuments/IniDocumentTests.cs`

**Interfaces:**
- Consumes: `IniKeyId` (Task 1).
- Produces:
  - `abstract record IniLine(string RawText, string LineEnding)`; `IniSectionHeader(string Name, string RawText, string LineEnding)`;
    `IniKeyValue(string Section, string Key, string Value, string RawText, string LineEnding)` with `IniKeyId Id` and `IniKeyValue WithValue(string newValue)`;
    `IniComment(string RawText, string LineEnding)`; `IniBlank(string RawText, string LineEnding)`.
  - `sealed class IniDocument`: `static IniDocument Parse(string text)`, `IReadOnlyList<IniLine> Lines`, `string Render()`,
    `bool TryGetValue(IniKeyId id, [ NotNullWhen(true) ] out string? value)`, `void SetValue(IniKeyId id, string value)`.

- [ ] **Step 1: Add the global usings**

Add `global using Erdmier.ZooTycoonLauncher.Domain.IniDocuments;` to `Source/Erdmier.ZooTycoonLauncher.Domain/GlobalUsings.cs` (after the `IniKeys` line) and to
`Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/GlobalUsings.cs`. Also add `global using System.Text;` to the Domain `GlobalUsings.cs` (after `System.Globalization`).

- [ ] **Step 2: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/IniDocuments/IniDocumentTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.IniDocuments;

public sealed class IniDocumentTests
{
    private const string Sample = "; Zoo Tycoon settings\r\n"
                                  + "[user]\r\n"
                                  + "fullscreen = 1\r\n"
                                  + "screenwidth=800\r\n"
                                  + "\r\n"
                                  + "[UI]\r\n"
                                  + "tooltipDelay=1   \r\n"
                                  + "garbage line\r\n"
                                  + "[mgr]\r\n"
                                  + "foo=bar\r\n";

    [ Theory ]
    [ InlineData("") ]
    [ InlineData("\r\n") ]
    [ InlineData(Sample) ]
    [ InlineData("[user]\nfullscreen=1\n") ]
    [ InlineData("[user]\r\nfullscreen=1\nscreenwidth=800\r\n") ]
    [ InlineData("[user]\r\nfullscreen=1") ]
    [ InlineData("\u00EF\u00BB\u00BF[user]\r\nfullscreen=1\r\n") ]
    [ InlineData("orphan=1\r\n[user]\r\n[user]\r\nfullscreen=0\r\n# hash comment\r\n=novalue\r\n[broken\r\n") ]
    [ InlineData("[user]\rfullscreen=1\r") ]
    public void Render_ReturnsTheParsedTextByteForByte(string text) => IniDocument.Parse(text).Render().ShouldBe(text);

    [ Fact ]
    public void Parse_ClassifiesEveryLine()
    {
        IReadOnlyList<IniLine> lines = IniDocument.Parse(Sample).Lines;

        lines[0].ShouldBeOfType<IniComment>();
        lines[1].ShouldBeOfType<IniSectionHeader>().Name.ShouldBe(expected: "user");
        lines[2].ShouldBeOfType<IniKeyValue>().Value.ShouldBe(expected: "1");
        lines[4].ShouldBeOfType<IniBlank>();
        lines[7].ShouldBeOfType<IniComment>().RawText.ShouldBe(expected: "garbage line");
    }

    [ Fact ]
    public void TryGetValue_IsCaseInsensitiveAndTrims()
    {
        IniDocument document = IniDocument.Parse(Sample);

        document.TryGetValue(new IniKeyId(Section: "USER", Key: "FullScreen"), out string? value).ShouldBeTrue();
        value.ShouldBe(expected: "1");
        document.TryGetValue(new IniKeyId(Section: "ui", Key: "tooltipdelay"), out string? delay).ShouldBeTrue();
        delay.ShouldBe(expected: "1");
    }

    [ Fact ]
    public void TryGetValue_FirstOccurrenceWins()
    {
        IniDocument document = IniDocument.Parse("[user]\r\nfullscreen=0\r\nfullscreen=1\r\n");

        document.TryGetValue(new IniKeyId(Section: "user", Key: "fullscreen"), out string? value).ShouldBeTrue();
        value.ShouldBe(expected: "0");
    }

    [ Fact ]
    public void TryGetValue_KeysBeforeAnySectionAreNotFoundUnderANamedSection()
    {
        IniDocument document = IniDocument.Parse("fullscreen=1\r\n[user]\r\n");

        document.TryGetValue(new IniKeyId(Section: "user", Key: "fullscreen"), out string? _).ShouldBeFalse();
    }

    [ Fact ]
    public void SetValue_ExistingKey_RewritesOnlyTheValueSpan()
    {
        IniDocument document = IniDocument.Parse(Sample);

        document.SetValue(new IniKeyId(Section: "user", Key: "FULLSCREEN"), value: "0");
        document.SetValue(new IniKeyId(Section: "UI", Key: "tooltipDelay"), value: "5");

        document.Render().ShouldBe(Sample.Replace(oldValue: "fullscreen = 1", newValue: "fullscreen = 0").Replace(oldValue: "tooltipDelay=1   ", newValue: "tooltipDelay=5   "));
    }

    [ Fact ]
    public void SetValue_EmptyValue_WritesKeyEquals()
    {
        IniDocument document = IniDocument.Parse("[user]\r\nlastfile=C:\\a.zoo\r\n");

        document.SetValue(new IniKeyId(Section: "user", Key: "lastfile"), value: "");

        document.Render().ShouldBe(expected: "[user]\r\nlastfile=\r\n");
    }

    [ Fact ]
    public void SetValue_MissingKey_InsertsAfterTheSectionsLastKey()
    {
        IniDocument document = IniDocument.Parse(Sample);

        document.SetValue(new IniKeyId(Section: "user", Key: "DrawRate"), value: "30");

        document.Render().ShouldBe(Sample.Replace(oldValue: "screenwidth=800\r\n", newValue: "screenwidth=800\r\nDrawRate=30\r\n"));
    }

    [ Fact ]
    public void SetValue_MissingKey_UsesTheExistingSectionEvenWhenItsCasingDiffers()
    {
        IniDocument document = IniDocument.Parse("[ui]\r\ntooltipDelay=1\r\n");

        document.SetValue(new IniKeyId(Section: "UI", Key: "keyScrollX"), value: "64");

        document.Render().ShouldBe(expected: "[ui]\r\ntooltipDelay=1\r\nkeyScrollX=64\r\n");
    }

    [ Fact ]
    public void SetValue_MissingSection_AppendsBlankLineHeaderAndKey()
    {
        IniDocument document = IniDocument.Parse("[user]\r\nfullscreen=1\r\n");

        document.SetValue(new IniKeyId(Section: "Map", Key: "mapX"), value: "100");

        document.Render().ShouldBe(expected: "[user]\r\nfullscreen=1\r\n\r\n[Map]\r\nmapX=100\r\n");
    }

    [ Fact ]
    public void SetValue_OnEmptyDocument_WritesSectionAndKeyWithCrlf()
    {
        IniDocument document = IniDocument.Parse(text: "");

        document.SetValue(new IniKeyId(Section: "user", Key: "fullscreen"), value: "0");

        document.Render().ShouldBe(expected: "[user]\r\nfullscreen=0\r\n");
    }

    [ Fact ]
    public void SetValue_DocumentWithoutTrailingNewline_TerminatesThePreviousLastLineAndKeepsTheShape()
    {
        IniDocument document = IniDocument.Parse("[user]\nfullscreen=1");

        document.SetValue(new IniKeyId(Section: "user", Key: "screenwidth"), value: "1024");

        document.Render().ShouldBe(expected: "[user]\nfullscreen=1\nscreenwidth=1024");
    }

    [ Fact ]
    public void SetValue_UsesTheDominantLineEnding()
    {
        IniDocument document = IniDocument.Parse("[user]\nfullscreen=1\nscreenwidth=800\n");

        document.SetValue(new IniKeyId(Section: "Map", Key: "mapY"), value: "90");

        document.Render().ShouldBe(expected: "[user]\nfullscreen=1\nscreenwidth=800\n\n[Map]\nmapY=90\n");
    }

    [ Fact ]
    public void SetValue_PreservesABomPreamble()
    {
        IniDocument document = IniDocument.Parse("\u00EF\u00BB\u00BF[user]\r\nfullscreen=1\r\n");

        document.SetValue(new IniKeyId(Section: "user", Key: "fullscreen"), value: "0");

        document.Render().ShouldBe(expected: "\u00EF\u00BB\u00BF[user]\r\nfullscreen=0\r\n");
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj --filter "FullyQualifiedName~IniDocumentTests"`
Expected: build FAILS — `IniDocument` does not exist.

- [ ] **Step 4: Implement the line records**

`Source/Erdmier.ZooTycoonLauncher.Domain/IniDocuments/IniLine.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniDocuments;

/// <summary>One physical line of a <c>zoo.ini</c> file, kept verbatim so the file round-trips byte for byte (SDD §8.1).</summary>
/// <param name="RawText">The line's text exactly as read, without its terminator.</param>
/// <param name="LineEnding">The line's own terminator: <c>"\r\n"</c>, <c>"\n"</c>, <c>"\r"</c>, or empty for a last line without one.</param>
public abstract record IniLine(string RawText, string LineEnding);
```

`Source/Erdmier.ZooTycoonLauncher.Domain/IniDocuments/IniSectionHeader.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniDocuments;

/// <summary>A <c>[section]</c> header line.</summary>
/// <param name="Name">The section name without brackets, trimmed.</param>
/// <param name="RawText">The line's text exactly as read.</param>
/// <param name="LineEnding">The line's own terminator.</param>
public sealed record IniSectionHeader(string Name, string RawText, string LineEnding) : IniLine(RawText, LineEnding);
```

`Source/Erdmier.ZooTycoonLauncher.Domain/IniDocuments/IniComment.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniDocuments;

/// <summary>A comment line (<c>;</c> or <c>#</c>), or any line the parser could not classify, kept verbatim.</summary>
/// <param name="RawText">The line's text exactly as read.</param>
/// <param name="LineEnding">The line's own terminator.</param>
public sealed record IniComment(string RawText, string LineEnding) : IniLine(RawText, LineEnding);
```

`Source/Erdmier.ZooTycoonLauncher.Domain/IniDocuments/IniBlank.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniDocuments;

/// <summary>A blank (empty or whitespace-only) line.</summary>
/// <param name="RawText">The line's text exactly as read (any whitespace is kept).</param>
/// <param name="LineEnding">The line's own terminator.</param>
public sealed record IniBlank(string RawText, string LineEnding) : IniLine(RawText, LineEnding);
```

`Source/Erdmier.ZooTycoonLauncher.Domain/IniDocuments/IniKeyValue.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniDocuments;

/// <summary>A <c>key=value</c> line. Everything after the first <c>=</c> is the value; inline comments are not recognised.</summary>
/// <param name="Section">The section the line sits under (empty for lines before any header).</param>
/// <param name="Key">The key, trimmed, in the file's casing.</param>
/// <param name="Value">The value, trimmed.</param>
/// <param name="RawText">The line's text exactly as read.</param>
/// <param name="LineEnding">The line's own terminator.</param>
public sealed record IniKeyValue(string Section, string Key, string Value, string RawText, string LineEnding) : IniLine(RawText, LineEnding)
{
    /// <summary>The line's section and key as an <see cref="IniKeyId" />.</summary>
    public IniKeyId Id => new(Section, Key);

    /// <summary>
    ///     Returns a copy whose value is <paramref name="newValue" />, rewriting only the value span: everything up to and including <c>=</c>, the whitespace after it, and any
    ///     trailing whitespace are kept.
    /// </summary>
    /// <param name="newValue">The new value text.</param>
    /// <returns>The rewritten line.</returns>
    public IniKeyValue WithValue(string newValue)
    {
        int    equals       = RawText.IndexOf(value: '=');
        string afterEquals  = RawText[(equals + 1)..];
        string trimmedStart = afterEquals.TrimStart();
        string leading      = afterEquals[..(afterEquals.Length - trimmedStart.Length)];
        string trailing     = trimmedStart[trimmedStart.TrimEnd().Length..];

        return this with
        {
            Value = newValue,
            RawText = RawText[..(equals + 1)] + leading + newValue + trailing
        };
    }
}
```

- [ ] **Step 5: Implement `IniDocument`**

`Source/Erdmier.ZooTycoonLauncher.Domain/IniDocuments/IniDocument.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniDocuments;

/// <summary>
///     A line-preserving model of <c>zoo.ini</c>. Parsing never loses information — every line keeps its raw text and its own terminator, and a leading UTF-8 byte-order mark is
///     kept as a preamble — so <see cref="Render" /> of an unedited document returns the original text exactly (SDD §8.1). Section and key matching is case-insensitive.
/// </summary>
public sealed class IniDocument
{
    private const string Utf8ByteOrderMarkAsLatin1 = "\u00EF\u00BB\u00BF";

    private readonly List<IniLine> _lines;

    private readonly string _preamble;

    private IniDocument(string preamble, List<IniLine> lines)
    {
        _preamble = preamble;
        _lines    = lines;
    }

    /// <summary>The document's lines in file order.</summary>
    public IReadOnlyList<IniLine> Lines => _lines;

    /// <summary>Parses INI text. Never fails: unclassifiable lines are kept verbatim as comments.</summary>
    /// <param name="text">The file's text (decoded as Latin-1 by the file store).</param>
    /// <returns>The parsed document.</returns>
    public static IniDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        string preamble = string.Empty;

        if (text.StartsWith(Utf8ByteOrderMarkAsLatin1, StringComparison.Ordinal))
        {
            preamble = Utf8ByteOrderMarkAsLatin1;
            text     = text[Utf8ByteOrderMarkAsLatin1.Length..];
        }

        List<IniLine> lines    = [];
        string        section  = string.Empty;
        int           position = 0;

        while (position < text.Length)
        {
            int    end = text.IndexOfAny(['\r', '\n'], position);
            string raw;
            string ending;

            if (end < 0)
            {
                raw      = text[position..];
                ending   = string.Empty;
                position = text.Length;
            }
            else
            {
                raw      = text[position..end];
                ending   = text[end] == '\r' && end + 1 < text.Length && text[end + 1] == '\n' ? "\r\n" : text[end].ToString();
                position = end + ending.Length;
            }

            IniLine line = Classify(raw, ending, section);

            if (line is IniSectionHeader header)
            {
                section = header.Name;
            }

            lines.Add(line);
        }

        return new IniDocument(preamble, lines);
    }

    /// <summary>Renders the document back to text: the preamble, then every line's raw text followed by its own terminator.</summary>
    /// <returns>The INI text.</returns>
    public string Render()
    {
        StringBuilder builder = new(_preamble);

        foreach (IniLine line in _lines)
        {
            builder.Append(line.RawText)
                   .Append(line.LineEnding);
        }

        return builder.ToString();
    }

    /// <summary>Returns the trimmed value of the first line matching <paramref name="id" /> (case-insensitive).</summary>
    /// <param name="id">The key to find.</param>
    /// <param name="value">The value when found.</param>
    /// <returns><see langword="true" /> when the key is present.</returns>
    public bool TryGetValue(IniKeyId id, [ NotNullWhen(true) ] out string? value)
    {
        foreach (IniLine line in _lines)
        {
            if (line is IniKeyValue keyValue
                && keyValue.Id == id)
            {
                value = keyValue.Value;

                return true;
            }
        }

        value = null;

        return false;
    }

    /// <summary>
    ///     Sets a key's value. An existing key has only its value span rewritten; a missing key is inserted after the last key of the section's first occurrence; a missing section
    ///     is appended (blank line, header, key). Inserted lines use the document's dominant line ending.
    /// </summary>
    /// <param name="id">The key to set; its casing is used when the key or section has to be inserted.</param>
    /// <param name="value">The new value; empty writes <c>key=</c>.</param>
    public void SetValue(IniKeyId id, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        int existing = _lines.FindIndex(line => line is IniKeyValue keyValue && keyValue.Id == id);

        if (existing >= 0)
        {
            _lines[existing] = ((IniKeyValue)_lines[existing]).WithValue(value);

            return;
        }

        string ending = DominantLineEnding();
        int    header = _lines.FindIndex(line => line is IniSectionHeader sectionHeader && string.Equals(sectionHeader.Name, id.Section, StringComparison.OrdinalIgnoreCase));

        if (header >= 0)
        {
            int insertAfter = header;

            for (int index = header + 1; index < _lines.Count && _lines[index] is not IniSectionHeader; index++)
            {
                if (_lines[index] is IniKeyValue)
                {
                    insertAfter = index;
                }
            }

            string sectionName = ((IniSectionHeader)_lines[header]).Name;

            InsertAfter(insertAfter, new IniKeyValue(sectionName, id.Key, value, $"{id.Key}={value}", ending), ending);

            return;
        }

        bool endsWithoutTerminator = _lines.Count > 0 && _lines[^1].LineEnding.Length == 0;

        if (endsWithoutTerminator)
        {
            _lines[^1] = _lines[^1] with
            {
                LineEnding = ending
            };
        }

        if (_lines.Count > 0)
        {
            _lines.Add(new IniBlank(string.Empty, ending));
        }

        _lines.Add(new IniSectionHeader(id.Section, $"[{id.Section}]", ending));
        _lines.Add(new IniKeyValue(id.Section, id.Key, value, $"{id.Key}={value}", endsWithoutTerminator ? string.Empty : ending));
    }

    private static IniLine Classify(string raw, string ending, string section)
    {
        string trimmed = raw.Trim();

        if (trimmed.Length == 0)
        {
            return new IniBlank(raw, ending);
        }

        if (trimmed[0] == '[')
        {
            int close = trimmed.IndexOf(value: ']');

            if (close > 0)
            {
                return new IniSectionHeader(trimmed[1..close].Trim(), raw, ending);
            }
        }

        if (trimmed[0] is ';' or '#')
        {
            return new IniComment(raw, ending);
        }

        int equals = raw.IndexOf(value: '=');

        if (equals >= 0)
        {
            string key = raw[..equals].Trim();

            if (key.Length > 0)
            {
                return new IniKeyValue(section, key, raw[(equals + 1)..].Trim(), raw, ending);
            }
        }

        return new IniComment(raw, ending);
    }

    private string DominantLineEnding()
    {
        int crlf = _lines.Count(line => line.LineEnding == "\r\n");
        int lf   = _lines.Count(line => line.LineEnding == "\n");
        int cr   = _lines.Count(line => line.LineEnding == "\r");

        if (lf > crlf
            && lf >= cr)
        {
            return "\n";
        }

        return cr > crlf && cr > lf ? "\r" : "\r\n";
    }

    private void InsertAfter(int index, IniKeyValue line, string ending)
    {
        // Only the last line can lack a terminator. Give it one, and let the inserted line become the new unterminated last line so the file keeps its shape.
        if (_lines[index].LineEnding.Length == 0)
        {
            _lines[index] = _lines[index] with
            {
                LineEnding = ending
            };

            line = line with
            {
                LineEnding = string.Empty
            };
        }

        _lines.Insert(index + 1, line);
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj --filter "FullyQualifiedName~IniDocumentTests"`
Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Domain Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit
git commit -m @'
feat(✨): add the byte-faithful IniDocument line model

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 3: `ZooIniDefaults` — the key registry

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniKeys/ZooIniDefaults.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/IniKeys/ZooIniDefaultsTests.cs`

**Interfaces:**
- Consumes: `IniKeyId`, `IniKeyRole`, `IniKeySpec` (Task 1); `IniDocument`, `IniKeyValue` (Task 2).
- Produces: `static class ZooIniDefaults` with `IReadOnlyList<string> Sections`, `IReadOnlyList<IniKeySpec> Keys`,
  `bool TryGet(IniKeyId id, [ NotNullWhen(true) ] out IniKeySpec? spec)`, `IReadOnlyDictionary<IniKeyId, string?> ExtractValues(IniDocument document)` (keys are the
  registry's `spec.Id`, only recognised keys present in the document, first occurrence wins).

- [ ] **Step 1: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/IniKeys/ZooIniDefaultsTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.IniKeys;

public sealed class ZooIniDefaultsTests
{
    [ Fact ]
    public void Registry_Has56KeysOf46AreUserSettings()
    {
        ZooIniDefaults.Keys.Count.ShouldBe(expected: 56);
        ZooIniDefaults.Keys.Count(spec => spec.Role == IniKeyRole.UserSetting).ShouldBe(expected: 46);
        ZooIniDefaults.Keys.Count(spec => spec.Role == IniKeyRole.GameManaged).ShouldBe(expected: 10);
    }

    [ Fact ]
    public void Sections_AreInSddOrder() => ZooIniDefaults.Sections.ShouldBe(["user", "UI", "advanced", "ai", "debug", "language", "Map"]);

    [ Fact ]
    public void Keys_HaveUniqueIds() => ZooIniDefaults.Keys.Select(spec => spec.Id).Distinct().Count().ShouldBe(ZooIniDefaults.Keys.Count);

    [ Fact ]
    public void Keys_OnlyUseListedSections() => ZooIniDefaults.Keys.ShouldAllBe(spec => ZooIniDefaults.Sections.Contains(spec.Id.Section));

    [ Fact ]
    public void Keys_DefaultsAreValidUnderTheirOwnSpec() => ZooIniDefaults.Keys.Where(spec => spec.DefaultValue is not null).ShouldAllBe(spec => spec.IsValid(spec.DefaultValue));

    [ Fact ]
    public void Keys_MinNeverExceedsMax() => ZooIniDefaults.Keys.Where(spec => spec.Min is not null && spec.Max is not null).ShouldAllBe(spec => spec.Min <= spec.Max);

    [ Theory ]
    [ InlineData("user", "lastfile") ]
    [ InlineData("user", "showUserEntityWarning") ]
    [ InlineData("UI", "lastWindowX") ]
    [ InlineData("UI", "lastWindowY") ]
    [ InlineData("UI", "startedFirstTutorial") ]
    [ InlineData("UI", "startedDinoTutorial") ]
    [ InlineData("UI", "startedAquaTutorial") ]
    [ InlineData("UI", "progresscalls") ]
    [ InlineData("UI", "defaultEditCharLimit") ]
    [ InlineData("UI", "completedExhibitAttenuation") ]
    public void GameManagedKeys_AreClassifiedAsSuch(string section, string key)
    {
        ZooIniDefaults.TryGet(new IniKeyId(section, key), out IniKeySpec? spec).ShouldBeTrue();
        spec.Role.ShouldBe(IniKeyRole.GameManaged);
    }

    [ Fact ]
    public void TryGet_IsCaseInsensitiveAndReturnsRegistryCasing()
    {
        ZooIniDefaults.TryGet(new IniKeyId(Section: "ui", Key: "MSSTARTINGCASH"), out IniKeySpec? spec).ShouldBeTrue();
        spec.Id.Section.ShouldBe(expected: "UI");
        spec.Id.Key.ShouldBe(expected: "MSStartingCash");
        spec.DefaultValue.ShouldBe(expected: "70000");
    }

    [ Fact ]
    public void TryGet_UnknownKey_ReturnsFalse() => ZooIniDefaults.TryGet(new IniKeyId(Section: "scenario", Key: "ag"), out IniKeySpec? _).ShouldBeFalse();

    [ Fact ]
    public void ExtractValues_ReturnsOnlyPresentRecognisedKeysKeyedByRegistryId()
    {
        IniDocument document = IniDocument.Parse("[USER]\r\nFullScreen=0\r\nfullscreen=1\r\n[scenario]\r\nag=0\r\n[UI]\r\nlastWindowX=\r\n");

        IReadOnlyDictionary<IniKeyId, string?> values = ZooIniDefaults.ExtractValues(document);

        values.Count.ShouldBe(expected: 2);
        values[new IniKeyId(Section: "user", Key: "fullscreen")].ShouldBe(expected: "0");
        values.Keys.ShouldContain(id => id.Section == "user" && id.Key == "fullscreen");
        values[new IniKeyId(Section: "UI", Key: "lastWindowX")].ShouldBe(expected: "");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj --filter "FullyQualifiedName~ZooIniDefaultsTests"`
Expected: build FAILS — `ZooIniDefaults` does not exist.

- [ ] **Step 3: Implement the registry**

`Source/Erdmier.ZooTycoonLauncher.Domain/IniKeys/ZooIniDefaults.cs` (values carried over from the Ref build's `ZooIniDefaults` / `IniRanges` / section models — for inspiration:
`References/Erdmier.ZooTycoonLauncherRef/Models/` — reimplemented here):

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniKeys;

/// <summary>
///     The single source of truth for every <c>zoo.ini</c> key the launcher recognises: 56 keys across seven sections, 46 user settings and 10 game-managed keys (SDD §5.3).
///     <c>[scenario]</c> is deliberately absent until the Phase 0 research (SDD §7.8) lands.
/// </summary>
public static class ZooIniDefaults
{
    /// <summary>The recognised sections, in the SDD §9.3 display order.</summary>
    public static IReadOnlyList<string> Sections { get; } = ["user", "UI", "advanced", "ai", "debug", "language", "Map"];

    /// <summary>Every recognised key, grouped by section in <see cref="Sections" /> order.</summary>
    public static IReadOnlyList<IniKeySpec> Keys { get; } =
    [
        Bool(section: "user", key: "fullscreen", defaultValue: true),
        Int(section: "user", key: "screenwidth", defaultValue: 800, min: 1, max: 16384),
        Int(section: "user", key: "screenheight", defaultValue: 600, min: 1, max: 16384),
        Int(section: "user", key: "UpdateRate", defaultValue: 15, min: 1, max: 60),
        Int(section: "user", key: "DrawRate", defaultValue: 60, min: 15, max: 120),
        GameManagedNullableStr(section: "user", key: "lastfile"),
        Bool(section: "user", key: "showUserEntityWarning", defaultValue: false, IniKeyRole.GameManaged),

        Bool(section: "UI", key: "noMenuMusic", defaultValue: false),
        Str(section: "UI", key: "menuMusic", defaultValue: "sounds/mainmenu.wav"),
        Int(section: "UI", key: "menuMusicAttenuation", defaultValue: 1500, min: 0, max: 10000),
        Int(section: "UI", key: "userAttenuation", defaultValue: 0, min: 0, max: 10000),
        Bool(section: "UI", key: "playMovie", defaultValue: false),
        Int(section: "UI", key: "movievolume1", defaultValue: -1000, min: -10000, max: 0),
        Bool(section: "UI", key: "playSecondMovie", defaultValue: false),
        Int(section: "UI", key: "movievolume2", defaultValue: -1000, min: -10000, max: 0),
        Int(section: "UI", key: "MSStartingCash", defaultValue: 70000, min: 0, max: 10000000),
        Int(section: "UI", key: "MSCashIncrement", defaultValue: 5000, min: 100, max: 1000000),
        Int(section: "UI", key: "MSMinCash", defaultValue: 10000, min: 0, max: 10000000),
        Int(section: "UI", key: "MSMaxCash", defaultValue: 500000, min: 0, max: 10000000),
        Bool(section: "UI", key: "useAlternateCursors", defaultValue: false),
        Int(section: "UI", key: "tooltipDelay", defaultValue: 1, min: 0, max: 60),
        Int(section: "UI", key: "tooltipDuration", defaultValue: 3000, min: 0, max: 30000),
        Bool(section: "UI", key: "MessageDisplay", defaultValue: true),
        Int(section: "UI", key: "mouseScrollThreshold", defaultValue: 1, min: 0, max: 50),
        Int(section: "UI", key: "mouseScrollDelay", defaultValue: 1, min: 0, max: 10),
        Int(section: "UI", key: "mouseScrollX", defaultValue: 27, min: 1, max: 200),
        Int(section: "UI", key: "mouseScrollY", defaultValue: 27, min: 1, max: 200),
        Int(section: "UI", key: "keyScrollX", defaultValue: 64, min: 1, max: 200),
        Int(section: "UI", key: "keyScrollY", defaultValue: 64, min: 1, max: 200),
        Int(section: "UI", key: "minimumMessageInterval", defaultValue: 60, min: 0, max: 3600),
        Int(section: "UI", key: "helpType", defaultValue: 1, min: 0, max: 2),
        GameManagedNullableInt(section: "UI", key: "lastWindowX"),
        GameManagedNullableInt(section: "UI", key: "lastWindowY"),
        Bool(section: "UI", key: "startedFirstTutorial", defaultValue: false, IniKeyRole.GameManaged),
        Bool(section: "UI", key: "startedDinoTutorial", defaultValue: false, IniKeyRole.GameManaged),
        Bool(section: "UI", key: "startedAquaTutorial", defaultValue: false, IniKeyRole.GameManaged),
        GameManagedNullableInt(section: "UI", key: "progresscalls"),
        GameManagedNullableInt(section: "UI", key: "defaultEditCharLimit"),
        GameManagedNullableInt(section: "UI", key: "completedExhibitAttenuation"),

        Int(section: "advanced", key: "level", defaultValue: 2, min: 0, max: 4),
        Bool(section: "advanced", key: "loadHalfAnims", defaultValue: false),
        Bool(section: "advanced", key: "drag", defaultValue: false),
        Bool(section: "advanced", key: "click", defaultValue: false),
        Bool(section: "advanced", key: "normal", defaultValue: false),
        Bool(section: "advanced", key: "use8BitSound", defaultValue: false),

        Int(section: "ai", key: "maxGuests", defaultValue: 1000, min: 1, max: 10000),

        Bool(section: "debug", key: "drawfps", defaultValue: false),
        Int(section: "debug", key: "drawfpsx", defaultValue: 720, min: 0, max: 16384),
        Int(section: "debug", key: "drawfpsy", defaultValue: 20, min: 0, max: 16384),
        Int(section: "debug", key: "logCutoff", defaultValue: 1, min: 0, max: 5),
        Bool(section: "debug", key: "sendLogfile", defaultValue: true),
        Bool(section: "debug", key: "sendDebugger", defaultValue: true),

        Int(section: "language", key: "lang", defaultValue: 9, min: 0, max: 65535),
        Int(section: "language", key: "sublang", defaultValue: 1, min: 0, max: 65535),

        Int(section: "Map", key: "mapX", defaultValue: 75, min: 1, max: 128),
        Int(section: "Map", key: "mapY", defaultValue: 75, min: 1, max: 128)
    ];

    // Declared after Keys so the static initialisers run in the right order.
    private static readonly Dictionary<IniKeyId, IniKeySpec> ById = Keys.ToDictionary(spec => spec.Id);

    /// <summary>Looks a key up case-insensitively.</summary>
    /// <param name="id">The key to find.</param>
    /// <param name="spec">The key's spec, in registry casing, when recognised.</param>
    /// <returns><see langword="true" /> when the key is recognised.</returns>
    public static bool TryGet(IniKeyId id, [ NotNullWhen(true) ] out IniKeySpec? spec) => ById.TryGetValue(id, out spec);

    /// <summary>
    ///     Returns the raw value of every recognised key present in <paramref name="document" />, keyed by the registry's <see cref="IniKeySpec.Id" />. When a key appears more
    ///     than once the first occurrence wins. Unrecognised keys are ignored — they stay in the document's text only.
    /// </summary>
    /// <param name="document">The parsed file.</param>
    /// <returns>The recognised values present in the file.</returns>
    public static IReadOnlyDictionary<IniKeyId, string?> ExtractValues(IniDocument document)
    {
        Dictionary<IniKeyId, string?> values = [];

        foreach (IniKeyValue line in document.Lines.OfType<IniKeyValue>())
        {
            if (ById.TryGetValue(line.Id, out IniKeySpec? spec)
                && !values.ContainsKey(spec.Id))
            {
                values[spec.Id] = line.Value;
            }
        }

        return values;
    }

    private static IniKeySpec Bool(string section, string key, bool defaultValue, IniKeyRole? role = null)
        => new(new IniKeyId(section, key), IniValueKind.Bool, defaultValue ? "1" : "0", Min: null, Max: null, role ?? IniKeyRole.UserSetting);

    private static IniKeySpec Int(string section, string key, int defaultValue, int min, int max)
        => new(new IniKeyId(section, key), IniValueKind.Int, defaultValue.ToString(CultureInfo.InvariantCulture), min, max, IniKeyRole.UserSetting);

    private static IniKeySpec Str(string section, string key, string defaultValue)
        => new(new IniKeyId(section, key), IniValueKind.Str, defaultValue, Min: null, Max: null, IniKeyRole.UserSetting);

    private static IniKeySpec GameManagedNullableInt(string section, string key)
        => new(new IniKeyId(section, key), IniValueKind.NullableInt, DefaultValue: null, Min: null, Max: null, IniKeyRole.GameManaged);

    private static IniKeySpec GameManagedNullableStr(string section, string key)
        => new(new IniKeyId(section, key), IniValueKind.NullableStr, DefaultValue: null, Min: null, Max: null, IniKeyRole.GameManaged);
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj --filter "FullyQualifiedName~ZooIniDefaultsTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Domain Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit
git commit -m @'
feat(✨): add the ZooIniDefaults key registry

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 4: Tiered drift detection

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniDrift/IniDriftKind.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniDrift/IniDriftResult.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Domain/IniDrift/IniDriftDetector.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Domain/GlobalUsings.cs`, `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/GlobalUsings.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/IniDrift/IniDriftDetectorTests.cs`

**Interfaces:**
- Consumes: `ZooIniDefaults`, `IniKeySpec`, `IniKeyRole`, `IniKeyId` (Tasks 1, 3).
- Produces: `sealed class IniDriftKind : SmartEnum<IniDriftKind>` (`None` 1, `GameManagedOnly` 2, `UserSettings` 3);
  `sealed record IniDriftResult(IniDriftKind Kind, IReadOnlyList<IniKeyId> ChangedKeys)`;
  `static class IniDriftDetector` with `IniDriftResult Detect(IReadOnlyDictionary<IniKeyId, string?> currentValues, IReadOnlyDictionary<IniKeyId, string?> diskValues)`
  (`ChangedKeys` uses registry ids, in registry order).

- [ ] **Step 1: Add the global usings**

Add `global using Erdmier.ZooTycoonLauncher.Domain.IniDrift;` to both `GlobalUsings.cs` files (Domain and Domain.Tests.Unit). The result record is deliberately named
`IniDriftResult`, not `IniDrift`, so it never collides with the `…IniDrift` namespaces.

- [ ] **Step 2: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/IniDrift/IniDriftDetectorTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.IniDrift;

public sealed class IniDriftDetectorTests
{
    private static readonly IniKeyId ScreenWidth = new(Section: "user", Key: "screenwidth");

    private static readonly IniKeyId LastWindowX = new(Section: "UI", Key: "lastWindowX");

    private static readonly IniKeyId LastFile = new(Section: "user", Key: "lastfile");

    [ Fact ]
    public void Detect_IdenticalValues_IsNone()
    {
        Dictionary<IniKeyId, string?> values = new() { [ScreenWidth] = "800", [LastWindowX] = "10" };

        IniDriftResult drift = IniDriftDetector.Detect(values, new Dictionary<IniKeyId, string?>(values));

        drift.Kind.ShouldBe(IniDriftKind.None);
        drift.ChangedKeys.ShouldBeEmpty();
    }

    [ Fact ]
    public void Detect_EquivalentButTextuallyDifferentValues_IsNone()
    {
        Dictionary<IniKeyId, string?> current = new() { [ScreenWidth] = "800" };
        Dictionary<IniKeyId, string?> disk    = new() { [new IniKeyId(Section: "USER", Key: "SCREENWIDTH")] = " 0800 " };

        IniDriftDetector.Detect(current, disk).Kind.ShouldBe(IniDriftKind.None);
    }

    [ Fact ]
    public void Detect_OnlyGameManagedChanged_IsGameManagedOnly()
    {
        Dictionary<IniKeyId, string?> current = new() { [ScreenWidth] = "800", [LastWindowX] = "10" };
        Dictionary<IniKeyId, string?> disk    = new() { [ScreenWidth] = "800", [LastWindowX] = "250", [LastFile] = @"C:\a.zoo" };

        IniDriftResult drift = IniDriftDetector.Detect(current, disk);

        drift.Kind.ShouldBe(IniDriftKind.GameManagedOnly);
        drift.ChangedKeys.ShouldBe([LastFile, LastWindowX]);
    }

    [ Fact ]
    public void Detect_AnyUserSettingChanged_IsUserSettingsAndListsEveryChange()
    {
        Dictionary<IniKeyId, string?> current = new() { [ScreenWidth] = "800", [LastWindowX] = "10" };
        Dictionary<IniKeyId, string?> disk    = new() { [ScreenWidth] = "1024", [LastWindowX] = "20" };

        IniDriftResult drift = IniDriftDetector.Detect(current, disk);

        drift.Kind.ShouldBe(IniDriftKind.UserSettings);
        drift.ChangedKeys.ShouldBe([ScreenWidth, LastWindowX]);
    }

    [ Fact ]
    public void Detect_UserSettingRemovedFromTheFile_IsUserSettings()
    {
        Dictionary<IniKeyId, string?> current = new() { [ScreenWidth] = "800" };

        IniDriftDetector.Detect(current, new Dictionary<IniKeyId, string?>()).Kind.ShouldBe(IniDriftKind.UserSettings);
    }

    [ Fact ]
    public void Detect_UserSettingAddedToTheFile_IsUserSettings()
    {
        Dictionary<IniKeyId, string?> disk = new() { [ScreenWidth] = "800" };

        IniDriftDetector.Detect(new Dictionary<IniKeyId, string?>(), disk).Kind.ShouldBe(IniDriftKind.UserSettings);
    }

    [ Fact ]
    public void Detect_UnrecognisedKeysAreIgnored()
    {
        Dictionary<IniKeyId, string?> disk = new() { [new IniKeyId(Section: "scenario", Key: "ag")] = "0" };

        IniDriftDetector.Detect(new Dictionary<IniKeyId, string?>(), disk).Kind.ShouldBe(IniDriftKind.None);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj --filter "FullyQualifiedName~IniDriftDetectorTests"`
Expected: build FAILS — the drift types do not exist.

- [ ] **Step 4: Implement the drift types**

`Source/Erdmier.ZooTycoonLauncher.Domain/IniDrift/IniDriftKind.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniDrift;

/// <summary>How the on-disk <c>zoo.ini</c> differs from the <c>Current</c> snapshot's recognised values (tiered drift, SDD §7.7).</summary>
public sealed class IniDriftKind : SmartEnum<IniDriftKind>
{
    /// <summary>Only game-managed keys changed; the change is adopted without archiving.</summary>
    public static readonly IniDriftKind GameManagedOnly = new(name: "GameManagedOnly", id: 2);

    /// <summary>No recognised value changed.</summary>
    public static readonly IniDriftKind None = new(name: "None", id: 1);

    /// <summary>At least one user setting changed; <c>Current</c> is archived before the change is adopted.</summary>
    public static readonly IniDriftKind UserSettings = new(name: "UserSettings", id: 3);

    private IniDriftKind(string name, int id)
        : base(name, id)
    { }
}
```

`Source/Erdmier.ZooTycoonLauncher.Domain/IniDrift/IniDriftResult.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniDrift;

/// <summary>The outcome of comparing <c>Current</c> with the on-disk values.</summary>
/// <param name="Kind">The drift tier.</param>
/// <param name="ChangedKeys">Every recognised key whose value differs, in registry order and casing.</param>
public sealed record IniDriftResult(IniDriftKind Kind, IReadOnlyList<IniKeyId> ChangedKeys);
```

`Source/Erdmier.ZooTycoonLauncher.Domain/IniDrift/IniDriftDetector.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Domain.IniDrift;

/// <summary>Classifies the difference between two sets of recognised INI values (SDD §7.7, tiered drift).</summary>
public static class IniDriftDetector
{
    /// <summary>
    ///     Compares every registry key with <see cref="IniKeySpec.AreEquivalent" /> (a missing key counts as <see langword="null" />). Any changed user setting makes the drift
    ///     <see cref="IniDriftKind.UserSettings" />; only changed game-managed keys make it <see cref="IniDriftKind.GameManagedOnly" />. Unrecognised keys are never compared.
    /// </summary>
    /// <param name="currentValues">The <c>Current</c> snapshot's values.</param>
    /// <param name="diskValues">The values extracted from the file on disk.</param>
    /// <returns>The drift tier and every changed key.</returns>
    public static IniDriftResult Detect(IReadOnlyDictionary<IniKeyId, string?> currentValues, IReadOnlyDictionary<IniKeyId, string?> diskValues)
    {
        List<IniKeyId> changed            = [];
        bool           userSettingChanged = false;

        foreach (IniKeySpec spec in ZooIniDefaults.Keys)
        {
            string? current = Find(currentValues, spec.Id);
            string? disk    = Find(diskValues, spec.Id);

            if (spec.AreEquivalent(current, disk))
            {
                continue;
            }

            changed.Add(spec.Id);

            userSettingChanged |= spec.Role == IniKeyRole.UserSetting;
        }

        IniDriftKind kind = changed.Count == 0
                                ? IniDriftKind.None
                                : userSettingChanged
                                    ? IniDriftKind.UserSettings
                                    : IniDriftKind.GameManagedOnly;

        return new IniDriftResult(kind, changed);
    }

    // IniKeyId equality is case-insensitive, so a plain lookup works whatever casing the caller's dictionary holds.
    private static string? Find(IReadOnlyDictionary<IniKeyId, string?> values, IniKeyId id) => values.TryGetValue(id, out string? value) ? value : null;
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.csproj`
Expected: PASS — every Domain test, including the existing `InstallationValidityTests`.

- [ ] **Step 6: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Domain Tests/Erdmier.ZooTycoonLauncher.Domain.Tests.Unit
git commit -m @'
feat(✨): add tiered INI drift detection

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 5: Application seams, errors, and `IniReconciler`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniFileStore.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniSnapshotRepository.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniSnapshotTransaction.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/IniFileContent.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/IniValueChange.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/IniConfigResult.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniErrors.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniReconciliationOutcome.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniReconciliation.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniReconciler.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/GlobalUsings.cs`, `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/IniTestData.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/IniReconcilerTests.cs`

**Interfaces:**
- Consumes: `IniDocument`, `ZooIniDefaults`, `IniDriftDetector`, `IniDriftResult`, `IniDriftKind`, `IniKeyId`, `IniKeySpec` (Tasks 1–4); existing `IniSnapshot`, `IniValue`,
  `IniSnapshotKind`, `IniSnapshotTrigger`, `IniValueSource`, `IniValueKind`.
- Produces (exact signatures — Tasks 6–11 and 15–16 rely on them):
  - `interface IIniFileStore` — `Task<IniFileContent?> ReadAsync(string installationPath, CancellationToken cancellationToken)`,
    `Task<DateTime> WriteAsync(string installationPath, string text, CancellationToken cancellationToken)`,
    `Task DeleteOrphanedTempFilesAsync(string installationPath, CancellationToken cancellationToken)`.
  - `interface IIniSnapshotRepository` — `Task<IIniSnapshotTransaction> BeginAsync(Guid installationId, CancellationToken cancellationToken)`.
  - `interface IIniSnapshotTransaction : IAsyncDisposable` — `Task<IniSnapshot?> GetCurrentAsync(CancellationToken)`, `Task AddAsync(IniSnapshot, CancellationToken)`,
    `Task ArchiveCurrentAsync(IniSnapshotTrigger trigger, DateTime capturedUtc, CancellationToken)`,
    `Task UpdateCurrentAsync(string structureBlob, IReadOnlyList<IniValueChange> changes, DateTime capturedUtc, CancellationToken)`, `Task CommitAsync(CancellationToken)`.
  - `sealed record IniFileContent(string Text, DateTime LastWriteUtc)`; `sealed record IniValueChange(IniKeyId Id, string? Value, IniValueSource Source)`;
    `sealed record IniConfigResult(IReadOnlyDictionary<IniKeyId, string?> Values, DateTime FileLastWriteUtc)`.
  - `static class IniErrors` — `InstallationNotFound(Guid)`, `Missing(string path)`, `ReadFailed(string message)`, `WriteFailed(string message)`, `StoreFailed(string message)`,
    each returning `Error` with codes `Installation.NotFound`, `Ini.Missing`, `Ini.ReadFailed`, `Ini.WriteFailed`, `Ini.StoreFailed`.
  - `enum IniReconciliationOutcome { Unchanged, FirstImport, AdoptedSilently, ArchivedAndAdopted }`;
    `sealed record IniReconciliation(IReadOnlyDictionary<IniKeyId, string?> Values, IniReconciliationOutcome Outcome)`.
  - `sealed class IniReconciler` (public) — `Task<IniReconciliation> ReconcileAsync(IIniSnapshotTransaction transaction, string diskText, DateTime nowUtc, CancellationToken)`.
  - Test helper `IniTestData` (`Now`, `Text(params string[])`, `Sample`, `CurrentSnapshot(string)`, `Transaction(IniSnapshot?)`).

- [ ] **Step 1: Write the seams and models (no behaviour yet)**

`Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniFileStore.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Reads and atomically writes an installation's <c>zoo.ini</c> as Latin-1 text, so every byte round-trips unchanged (SDD §7.7, §8.1).</summary>
public interface IIniFileStore
{
    /// <summary>Reads <c>zoo.ini</c> from the installation folder.</summary>
    /// <param name="installationPath">The installation folder.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file's text and last-write time, or <see langword="null" /> when the file is absent.</returns>
    Task<IniFileContent?> ReadAsync(string installationPath, CancellationToken cancellationToken);

    /// <summary>Writes <c>zoo.ini</c> atomically: a temp file in the same folder, then <c>Move(overwrite: true)</c>. Throws on failure after removing the temp file.</summary>
    /// <param name="installationPath">The installation folder.</param>
    /// <param name="text">The full file text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The written file's last-write time (UTC).</returns>
    Task<DateTime> WriteAsync(string installationPath, string text, CancellationToken cancellationToken);

    /// <summary>Deletes <c>zoo.ini.tmp.*</c> files left behind by an interrupted write. Files that cannot be deleted are logged and skipped.</summary>
    /// <param name="installationPath">The installation folder.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteOrphanedTempFilesAsync(string installationPath, CancellationToken cancellationToken);
}
```

`Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniSnapshotRepository.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Opens a transaction over an installation's snapshot database (<c>{installationId}.db</c>) so the SDD §8.2 ordering can be expressed in the Application layer.</summary>
public interface IIniSnapshotRepository
{
    /// <summary>Opens (migrating if needed) the installation's database and begins a transaction.</summary>
    /// <param name="installationId">The installation's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The open transaction. Dispose it; disposing without <see cref="IIniSnapshotTransaction.CommitAsync" /> rolls back.</returns>
    Task<IIniSnapshotTransaction> BeginAsync(Guid installationId, CancellationToken cancellationToken);
}
```

`Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniSnapshotTransaction.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>One open transaction over an installation's snapshot database. Disposing without <see cref="CommitAsync" /> rolls every change back.</summary>
public interface IIniSnapshotTransaction : IAsyncDisposable
{
    /// <summary>Returns the <c>Current</c> snapshot with its values, or <see langword="null" /> when the database has never been imported.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IniSnapshot?> GetCurrentAsync(CancellationToken cancellationToken);

    /// <summary>Inserts a snapshot with its values (used by the first import: <c>Original</c>, then <c>Current</c>).</summary>
    /// <param name="snapshot">The snapshot to insert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(IniSnapshot snapshot, CancellationToken cancellationToken);

    /// <summary>Copies the <c>Current</c> snapshot's structure blob and every value row into a new <c>Historical</c> snapshot.</summary>
    /// <param name="trigger">Why the archive happened.</param>
    /// <param name="capturedUtc">The archive time (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ArchiveCurrentAsync(IniSnapshotTrigger trigger, DateTime capturedUtc, CancellationToken cancellationToken);

    /// <summary>Updates the <c>Current</c> snapshot in place: its structure blob, its <c>CapturedUtc</c>, and only the value rows named in <paramref name="changes" />.</summary>
    /// <param name="structureBlob">The new raw file text.</param>
    /// <param name="changes">The rows to insert, update, or (when <see cref="IniValueChange.Value" /> is <see langword="null" />) delete.</param>
    /// <param name="capturedUtc">The update time (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateCurrentAsync(string structureBlob, IReadOnlyList<IniValueChange> changes, DateTime capturedUtc, CancellationToken cancellationToken);

    /// <summary>Commits the transaction.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CommitAsync(CancellationToken cancellationToken);
}
```

`Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/IniFileContent.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>The text of <c>zoo.ini</c> as read from disk.</summary>
/// <param name="Text">The file's text, decoded as Latin-1.</param>
/// <param name="LastWriteUtc">The file's last-write time (UTC).</param>
public sealed record IniFileContent(string Text, DateTime LastWriteUtc);
```

`Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/IniValueChange.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>One value row to change in the <c>Current</c> snapshot.</summary>
/// <param name="Id">The key.</param>
/// <param name="Value">The new raw value, or <see langword="null" /> when the key has left the file (the row is deleted).</param>
/// <param name="Source">How the value got there.</param>
public sealed record IniValueChange(IniKeyId Id, string? Value, IniValueSource Source);
```

`Source/Erdmier.ZooTycoonLauncher.Application/Common/Models/IniConfigResult.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>The recognised values of an installation's <c>zoo.ini</c>, as reconciled with the file on disk.</summary>
/// <param name="Values">Raw values keyed by registry id; a key absent from the file is absent here.</param>
/// <param name="FileLastWriteUtc">The file's last-write time (UTC), shown in the editor footer.</param>
public sealed record IniConfigResult(IReadOnlyDictionary<IniKeyId, string?> Values, DateTime FileLastWriteUtc);
```

- [ ] **Step 2: Add the global usings**

In `Source/Erdmier.ZooTycoonLauncher.Application/GlobalUsings.cs`, add after `global using Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;`:

```csharp
global using Erdmier.ZooTycoonLauncher.Domain.IniDocuments;
global using Erdmier.ZooTycoonLauncher.Domain.IniDrift;
global using Erdmier.ZooTycoonLauncher.Domain.IniKeys;
```

and after `global using Erdmier.ZooTycoonLauncher.Application.Game.Launch;`:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;
```

In `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs`, add after `global using Erdmier.ZooTycoonLauncher.Application.Game.Launch;`:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;
global using Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;
```

and after `global using Erdmier.ZooTycoonLauncher.Domain.Settings;`:

```csharp
global using Erdmier.ZooTycoonLauncher.Domain.IniDocuments;
global using Erdmier.ZooTycoonLauncher.Domain.IniKeys;
global using Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;
```

- [ ] **Step 3: Write the test helper and the failing reconciler tests**

`Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/IniTestData.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

internal static class IniTestData
{
    public static readonly DateTime Now = new(year: 2026, month: 9, day: 24, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly string Sample = Text("; Zoo Tycoon settings",
                                                "[user]",
                                                "fullscreen=1",
                                                "screenwidth=800",
                                                "lastfile=C:\\Spiele\\Zoo M\u00FCller.zoo",
                                                "",
                                                "[UI]",
                                                "tooltipDelay=1",
                                                "lastWindowX=10",
                                                "",
                                                "[scenario]",
                                                "ag=0");

    public static string Text(params string[] lines) => string.Join(separator: "\r\n", lines) + "\r\n";

    public static IniSnapshot CurrentSnapshot(string text)
    {
        Guid id = Guid.CreateVersion7();

        return new IniSnapshot
        {
            Id            = id,
            Kind          = IniSnapshotKind.Current,
            Trigger       = IniSnapshotTrigger.OriginalImport,
            CapturedUtc   = new DateTime(year: 2026, month: 1, day: 1, hour: 0, minute: 0, second: 0, DateTimeKind.Utc),
            StructureBlob = text,
            Values = ZooIniDefaults.ExtractValues(IniDocument.Parse(text))
                                   .Select(pair => new IniValue
                                   {
                                       SnapshotId = id,
                                       Section    = pair.Key.Section,
                                       Key        = pair.Key.Key,
                                       Value      = pair.Value,
                                       ValueKind  = ZooIniDefaults.TryGet(pair.Key, out IniKeySpec? spec) ? spec.Kind : IniValueKind.Str,
                                       Source     = IniValueSource.OriginalImport
                                   })
                                   .ToList()
        };
    }

    public static IIniSnapshotTransaction Transaction(IniSnapshot? current)
    {
        IIniSnapshotTransaction transaction = Substitute.For<IIniSnapshotTransaction>();

        transaction.GetCurrentAsync(Arg.Any<CancellationToken>())
                   .Returns(current);

        return transaction;
    }
}
```

`Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/IniReconcilerTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class IniReconcilerTests
{
    private readonly IniReconciler _reconciler = new();

    [ Fact ]
    public async Task Reconcile_NoCurrent_ImportsOriginalThenCurrent()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(current: null);
        List<IniSnapshot>       added       = [];

        transaction.When(t => t.AddAsync(Arg.Any<IniSnapshot>(), Arg.Any<CancellationToken>()))
                   .Do(call => added.Add(call.Arg<IniSnapshot>()));

        IniReconciliation result = await _reconciler.ReconcileAsync(transaction, IniTestData.Sample, IniTestData.Now, CancellationToken.None);

        result.Outcome.ShouldBe(IniReconciliationOutcome.FirstImport);
        added.Select(snapshot => snapshot.Kind).ShouldBe([IniSnapshotKind.Original, IniSnapshotKind.Current]);
        added.ShouldAllBe(snapshot => snapshot.Trigger == IniSnapshotTrigger.OriginalImport && snapshot.StructureBlob == IniTestData.Sample);
        added[1].Values.Count.ShouldBe(expected: 5);
        added[1].Values.ShouldAllBe(value => value.Source == IniValueSource.OriginalImport && value.SnapshotId == added[1].Id);
        added[0].Id.ShouldNotBe(added[1].Id);
        result.Values[new IniKeyId(Section: "user", Key: "screenwidth")].ShouldBe(expected: "800");

        await transaction.DidNotReceive().ArchiveCurrentAsync(Arg.Any<IniSnapshotTrigger>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Reconcile_NoDriftAndIdenticalText_WritesNothing()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(IniTestData.CurrentSnapshot(IniTestData.Sample));

        IniReconciliation result = await _reconciler.ReconcileAsync(transaction, IniTestData.Sample, IniTestData.Now, CancellationToken.None);

        result.Outcome.ShouldBe(IniReconciliationOutcome.Unchanged);

        await transaction.DidNotReceive().AddAsync(Arg.Any<IniSnapshot>(), Arg.Any<CancellationToken>());
        await transaction.DidNotReceive().ArchiveCurrentAsync(Arg.Any<IniSnapshotTrigger>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await transaction.DidNotReceive().UpdateCurrentAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<IniValueChange>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Reconcile_GameManagedChange_AdoptsWithoutArchiving()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(IniTestData.CurrentSnapshot(IniTestData.Sample));
        string                  disk        = IniTestData.Sample.Replace(oldValue: "lastWindowX=10", newValue: "lastWindowX=250");

        IniReconciliation result = await _reconciler.ReconcileAsync(transaction, disk, IniTestData.Now, CancellationToken.None);

        result.Outcome.ShouldBe(IniReconciliationOutcome.AdoptedSilently);

        await transaction.DidNotReceive().ArchiveCurrentAsync(Arg.Any<IniSnapshotTrigger>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());

        await transaction.Received(requiredNumberOfCalls: 1)
                         .UpdateCurrentAsync(disk,
                                             Arg.Is<IReadOnlyList<IniValueChange>>(changes => changes.Count == 1
                                                                                             && changes[0].Id == new IniKeyId("UI", "lastWindowX")
                                                                                             && changes[0].Value == "250"
                                                                                             && changes[0].Source == IniValueSource.Manual),
                                             IniTestData.Now,
                                             Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Reconcile_UserSettingChange_ArchivesAsManualThenAdopts()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(IniTestData.CurrentSnapshot(IniTestData.Sample));
        string                  disk        = IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024");

        IniReconciliation result = await _reconciler.ReconcileAsync(transaction, disk, IniTestData.Now, CancellationToken.None);

        result.Outcome.ShouldBe(IniReconciliationOutcome.ArchivedAndAdopted);
        result.Values[new IniKeyId(Section: "user", Key: "screenwidth")].ShouldBe(expected: "1024");

        Received.InOrder(() =>
        {
            transaction.ArchiveCurrentAsync(IniSnapshotTrigger.Manual, IniTestData.Now, Arg.Any<CancellationToken>());
            transaction.UpdateCurrentAsync(disk, Arg.Any<IReadOnlyList<IniValueChange>>(), IniTestData.Now, Arg.Any<CancellationToken>());
        });
    }

    [ Fact ]
    public async Task Reconcile_TextOnlyChange_UpdatesTheBlobWithNoRowChanges()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(IniTestData.CurrentSnapshot(IniTestData.Sample));
        string                  disk        = IniTestData.Sample.Replace(oldValue: "ag=0", newValue: "ag=1");

        IniReconciliation result = await _reconciler.ReconcileAsync(transaction, disk, IniTestData.Now, CancellationToken.None);

        result.Outcome.ShouldBe(IniReconciliationOutcome.AdoptedSilently);

        await transaction.DidNotReceive().ArchiveCurrentAsync(Arg.Any<IniSnapshotTrigger>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());

        await transaction.Received(requiredNumberOfCalls: 1)
                         .UpdateCurrentAsync(disk, Arg.Is<IReadOnlyList<IniValueChange>>(changes => changes.Count == 0), IniTestData.Now, Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Reconcile_KeyRemovedFromTheFile_ProducesANullChange()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(IniTestData.CurrentSnapshot(IniTestData.Sample));
        string                  disk        = IniTestData.Sample.Replace(oldValue: "lastWindowX=10\r\n", newValue: "");

        await _reconciler.ReconcileAsync(transaction, disk, IniTestData.Now, CancellationToken.None);

        await transaction.Received(requiredNumberOfCalls: 1)
                         .UpdateCurrentAsync(disk,
                                             Arg.Is<IReadOnlyList<IniValueChange>>(changes => changes.Count == 1 && changes[0].Value == null),
                                             IniTestData.Now,
                                             Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj --filter "FullyQualifiedName~IniReconcilerTests"`
Expected: build FAILS — `IniReconciler`, `IniReconciliation`, `IniReconciliationOutcome` do not exist (and `Application.IniConfig.Common` is an empty namespace until Step 5).

- [ ] **Step 5: Implement the errors, outcome, result, and reconciler**

`Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniErrors.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;

/// <summary>The expected failures of the INI Config slice. Descriptions are user-readable because the Desktop layer shows them verbatim.</summary>
public static class IniErrors
{
    /// <summary>The installation id does not resolve to a row.</summary>
    /// <param name="installationId">The missing id.</param>
    /// <returns>A not-found error coded <c>Installation.NotFound</c>.</returns>
    public static Error InstallationNotFound(Guid installationId) => Error.NotFound(code: "Installation.NotFound", $"No installation with id {installationId}.");

    /// <summary><c>zoo.ini</c> is absent from the installation folder.</summary>
    /// <param name="installationPath">The installation folder.</param>
    /// <returns>A not-found error coded <c>Ini.Missing</c>.</returns>
    public static Error Missing(string installationPath) => Error.NotFound(code: "Ini.Missing", $"zoo.ini was not found in \"{installationPath}\".");

    /// <summary>Reading <c>zoo.ini</c> failed.</summary>
    /// <param name="message">The operating system's message.</param>
    /// <returns>A failure coded <c>Ini.ReadFailed</c>.</returns>
    public static Error ReadFailed(string message) => Error.Failure(code: "Ini.ReadFailed", $"zoo.ini could not be read: {message}");

    /// <summary>Writing <c>zoo.ini</c> failed.</summary>
    /// <param name="message">The operating system's message.</param>
    /// <returns>A failure coded <c>Ini.WriteFailed</c>.</returns>
    public static Error WriteFailed(string message) => Error.Failure(code: "Ini.WriteFailed", $"zoo.ini could not be saved: {message}");

    /// <summary>The installation's snapshot database failed.</summary>
    /// <param name="message">The underlying exception's message.</param>
    /// <returns>An unexpected error coded <c>Ini.StoreFailed</c>.</returns>
    public static Error StoreFailed(string message) => Error.Unexpected(code: "Ini.StoreFailed", $"The installation's settings history could not be opened: {message}");
}
```

`Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniReconciliationOutcome.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;

/// <summary>What <see cref="IniReconciler" /> did to bring <c>Current</c> in line with the file on disk.</summary>
public enum IniReconciliationOutcome
{
    /// <summary>Nothing differed; nothing was written.</summary>
    Unchanged,

    /// <summary>The database had no <c>Current</c> snapshot; <c>Original</c> and <c>Current</c> were imported.</summary>
    FirstImport,

    /// <summary>Only game-managed or unrecognised content changed; <c>Current</c> was updated without archiving.</summary>
    AdoptedSilently,

    /// <summary>A user setting changed outside the launcher; <c>Current</c> was archived (<c>Manual</c>) and then updated.</summary>
    ArchivedAndAdopted
}
```

`Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniReconciliation.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;

/// <summary>The result of reconciling <c>Current</c> with the file on disk.</summary>
/// <param name="Values">The recognised values now held by <c>Current</c> (the on-disk values).</param>
/// <param name="Outcome">What the reconciliation did.</param>
public sealed record IniReconciliation(IReadOnlyDictionary<IniKeyId, string?> Values, IniReconciliationOutcome Outcome);
```

`Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniReconciler.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;

/// <summary>
///     Brings the <c>Current</c> snapshot in line with the on-disk <c>zoo.ini</c> inside a caller-owned transaction, applying tiered drift (SDD §7.7): a first import when the
///     database is empty, archive-then-adopt for user-setting drift, and silent adoption for game-managed or unrecognised changes.
/// </summary>
public sealed class IniReconciler
{
    /// <summary>Reconciles <c>Current</c> with <paramref name="diskText" />. Does not commit.</summary>
    /// <param name="transaction">The open snapshot transaction.</param>
    /// <param name="diskText">The file's text as read from disk.</param>
    /// <param name="nowUtc">The timestamp for anything captured (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The on-disk values and what was done.</returns>
    public async Task<IniReconciliation> ReconcileAsync(IIniSnapshotTransaction transaction, string diskText, DateTime nowUtc, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<IniKeyId, string?> diskValues = ZooIniDefaults.ExtractValues(IniDocument.Parse(diskText));

        IniSnapshot? current = await transaction.GetCurrentAsync(cancellationToken);

        if (current is null)
        {
            await transaction.AddAsync(CreateImportSnapshot(IniSnapshotKind.Original, diskText, diskValues, nowUtc), cancellationToken);
            await transaction.AddAsync(CreateImportSnapshot(IniSnapshotKind.Current, diskText, diskValues, nowUtc), cancellationToken);

            return new IniReconciliation(diskValues, IniReconciliationOutcome.FirstImport);
        }

        Dictionary<IniKeyId, string?> currentValues = current.Values.ToDictionary(value => new IniKeyId(value.Section, value.Key), value => value.Value);

        IniDriftResult drift       = IniDriftDetector.Detect(currentValues, diskValues);
        bool           textChanged = !string.Equals(current.StructureBlob, diskText, StringComparison.Ordinal);

        if (drift.Kind == IniDriftKind.None
            && !textChanged)
        {
            return new IniReconciliation(diskValues, IniReconciliationOutcome.Unchanged);
        }

        bool archive = drift.Kind == IniDriftKind.UserSettings;

        if (archive)
        {
            await transaction.ArchiveCurrentAsync(IniSnapshotTrigger.Manual, nowUtc, cancellationToken);
        }

        List<IniValueChange> changes = drift.ChangedKeys
                                            .Select(id => new IniValueChange(id, diskValues.GetValueOrDefault(id), IniValueSource.Manual))
                                            .ToList();

        await transaction.UpdateCurrentAsync(diskText, changes, nowUtc, cancellationToken);

        return new IniReconciliation(diskValues, archive ? IniReconciliationOutcome.ArchivedAndAdopted : IniReconciliationOutcome.AdoptedSilently);
    }

    private static IniSnapshot CreateImportSnapshot(IniSnapshotKind kind, string text, IReadOnlyDictionary<IniKeyId, string?> values, DateTime nowUtc)
    {
        Guid id = Guid.CreateVersion7();

        return new IniSnapshot
        {
            Id            = id,
            Kind          = kind,
            Trigger       = IniSnapshotTrigger.OriginalImport,
            CapturedUtc   = nowUtc,
            StructureBlob = text,
            Values = values.Select(pair => new IniValue
                           {
                               SnapshotId = id,
                               Section    = pair.Key.Section,
                               Key        = pair.Key.Key,
                               Value      = pair.Value,
                               ValueKind  = ZooIniDefaults.TryGet(pair.Key, out IniKeySpec? spec) ? spec.Kind : IniValueKind.Str,
                               Source     = IniValueSource.OriginalImport
                           })
                           .ToList()
        };
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj`
Expected: PASS — the new `IniReconcilerTests` and every existing Application test.

- [ ] **Step 7: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Application Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m @'
feat(✨): add the INI snapshot seams and the tiered-drift reconciler

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 6: `IniFileStore` — Latin-1 atomic file access

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/IniConfig/IniFileStore.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/GlobalUsings.cs`, `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/GlobalUsings.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/IniConfig/IniFileStoreTests.cs`

**Interfaces:**
- Consumes: `IIniFileStore`, `IniFileContent` (Task 5); `IFileSystem`; Serilog `ILogger`.
- Produces: `public sealed class IniFileStore : IIniFileStore` with constructor `IniFileStore(IFileSystem fileSystem, ILogger logger)` (Serilog `ILogger`). Registered in Task 8.

- [ ] **Step 1: Add the global usings**

Add `global using System.Text;` and `global using Erdmier.ZooTycoonLauncher.Infrastructure.IniConfig;` to `Source/Erdmier.ZooTycoonLauncher.Infrastructure/GlobalUsings.cs`
(`System.Text` at the top with `System.IO.Abstractions`; the `IniConfig` line after `Infrastructure.Game`). Add
`global using Erdmier.ZooTycoonLauncher.Infrastructure.IniConfig;` to `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/GlobalUsings.cs` after the
`Infrastructure.Game` line.

- [ ] **Step 2: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/IniConfig/IniFileStoreTests.cs`:

```csharp
using System.IO.Abstractions;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.IniConfig;

public sealed class IniFileStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"zoolauncher-ini-test-{Guid.NewGuid():N}");

    private readonly IniFileStore _store = new(new FileSystem(), Substitute.For<Serilog.ILogger>());

    public IniFileStoreTests() => Directory.CreateDirectory(_directory);

    private string IniPath => Path.Combine(_directory, path2: "zoo.ini");

    public void Dispose()
    {
        if (File.Exists(IniPath))
        {
            File.SetAttributes(IniPath, FileAttributes.Normal);
        }

        Directory.Delete(_directory, recursive: true);
    }

    [ Fact ]
    public async Task Read_AbsentFile_ReturnsNull() => (await _store.ReadAsync(_directory, CancellationToken.None)).ShouldBeNull();

    [ Fact ]
    public async Task ReadThenWrite_RoundTripsAll256ByteValues()
    {
        byte[] original = Enumerable.Range(start: 0, count: 256).Select(value => (byte)value).ToArray();

        await File.WriteAllBytesAsync(IniPath, original);

        IniFileContent? content = await _store.ReadAsync(_directory, CancellationToken.None);

        content.ShouldNotBeNull();

        await _store.WriteAsync(_directory, content.Text, CancellationToken.None);

        (await File.ReadAllBytesAsync(IniPath)).ShouldBe(original);
    }

    [ Fact ]
    public async Task Write_LeavesNoTempFileAndReturnsTheLastWriteTime()
    {
        DateTime lastWrite = await _store.WriteAsync(_directory, text: "[user]\r\nfullscreen=0\r\n", CancellationToken.None);

        (await File.ReadAllTextAsync(IniPath)).ShouldBe(expected: "[user]\r\nfullscreen=0\r\n");
        lastWrite.ShouldBe(File.GetLastWriteTimeUtc(IniPath));
        Directory.GetFiles(_directory, searchPattern: "zoo.ini.tmp.*").ShouldBeEmpty();
    }

    [ Fact ]
    public async Task Write_OntoAReadOnlyFile_ThrowsAndRemovesTheTempFile()
    {
        await File.WriteAllTextAsync(IniPath, contents: "[user]\r\nfullscreen=1\r\n");

        File.SetAttributes(IniPath, FileAttributes.ReadOnly);

        await Should.ThrowAsync<UnauthorizedAccessException>(() => _store.WriteAsync(_directory, text: "changed", CancellationToken.None));

        Directory.GetFiles(_directory, searchPattern: "zoo.ini.tmp.*").ShouldBeEmpty();
        (await File.ReadAllTextAsync(IniPath)).ShouldBe(expected: "[user]\r\nfullscreen=1\r\n");
    }

    [ Fact ]
    public async Task DeleteOrphanedTempFiles_RemovesOnlyTempFiles()
    {
        await File.WriteAllTextAsync(IniPath, contents: "x");
        await File.WriteAllTextAsync(Path.Combine(_directory, path2: "zoo.ini.tmp.0123456789abcdef"), contents: "x");
        await File.WriteAllTextAsync(Path.Combine(_directory, path2: "zoo.ini.bak"), contents: "x");

        await _store.DeleteOrphanedTempFilesAsync(_directory, CancellationToken.None);

        Directory.GetFiles(_directory).Select(Path.GetFileName).Order().ShouldBe(["zoo.ini", "zoo.ini.bak"]);
    }

    [ Fact ]
    public async Task DeleteOrphanedTempFiles_MissingDirectory_DoesNothing()
        => await Should.NotThrowAsync(() => _store.DeleteOrphanedTempFilesAsync(Path.Combine(_directory, path2: "missing"), CancellationToken.None));
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run:

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj --filter "FullyQualifiedName~IniFileStoreTests"
```
Expected: build FAILS — `IniFileStore` does not exist.

- [ ] **Step 4: Implement `IniFileStore`**

`Source/Erdmier.ZooTycoonLauncher.Infrastructure/IniConfig/IniFileStore.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.IniConfig;

/// <summary>
///     <see cref="IIniFileStore" /> over <see cref="IFileSystem" />. Text is decoded and encoded as Latin-1, which maps every byte to exactly one character and back, so a round trip
///     is byte-identical whatever code page the file was written in. Writes go to a temp file in the same folder and are moved over <c>zoo.ini</c> (SDD §7.7, §8.2).
/// </summary>
public sealed class IniFileStore : IIniFileStore
{
    private const string FileName = "zoo.ini";

    private const string TempFilePrefix = "zoo.ini.tmp.";

    private readonly IFileSystem _fileSystem;

    private readonly ILogger _logger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="fileSystem">File-system abstraction.</param>
    /// <param name="logger">The Serilog logger.</param>
    public IniFileStore(IFileSystem fileSystem, ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger     = logger;
    }

    /// <inheritdoc />
    public async Task<IniFileContent?> ReadAsync(string installationPath, CancellationToken cancellationToken)
    {
        string path = _fileSystem.Path.Combine(installationPath, FileName);

        if (!_fileSystem.File.Exists(path))
        {
            return null;
        }

        byte[] bytes = await _fileSystem.File.ReadAllBytesAsync(path, cancellationToken);

        return new IniFileContent(Encoding.Latin1.GetString(bytes), _fileSystem.File.GetLastWriteTimeUtc(path));
    }

    /// <inheritdoc />
    public async Task<DateTime> WriteAsync(string installationPath, string text, CancellationToken cancellationToken)
    {
        string path     = _fileSystem.Path.Combine(installationPath, FileName);
        string tempPath = _fileSystem.Path.Combine(installationPath, TempFilePrefix + Guid.NewGuid().ToString(format: "N"));

        try
        {
            await _fileSystem.File.WriteAllBytesAsync(tempPath, Encoding.Latin1.GetBytes(text), cancellationToken);

            // The temp file sits in the same folder, so this is a same-volume rename — atomic on NTFS.
            _fileSystem.File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            TryDelete(tempPath);

            throw;
        }

        return _fileSystem.File.GetLastWriteTimeUtc(path);
    }

    /// <inheritdoc />
    public Task DeleteOrphanedTempFilesAsync(string installationPath, CancellationToken cancellationToken)
    {
        if (!_fileSystem.Directory.Exists(installationPath))
        {
            return Task.CompletedTask;
        }

        foreach (string orphan in _fileSystem.Directory.EnumerateFiles(installationPath, TempFilePrefix + "*"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            TryDelete(orphan);
        }

        return Task.CompletedTask;
    }

    private void TryDelete(string path)
    {
        try
        {
            if (_fileSystem.File.Exists(path))
            {
                _fileSystem.File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Warning(ex, messageTemplate: "Could not delete the INI temp file {Path}", path);
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run:

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj --filter "FullyQualifiedName~IniFileStoreTests"
```
Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Infrastructure Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration
git commit -m @'
feat(✨): add the Latin-1 atomic IniFileStore

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 7: Snapshot repository, transaction, and the missing indexes

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniSnapshot.cs` (`CapturedUtc` → `{ get; set; }`)
- Modify: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/InstallationDbContextFactory.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/Configurations/IniSnapshotConfiguration.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/IniSnapshotRepository.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/IniSnapshotTransaction.cs`
- Create (generated): `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/Migrations/<timestamp>_AddSnapshotIndexes.cs` (+ `.Designer.cs`), and the
  updated `InstallationDbContextModelSnapshot.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/GlobalUsings.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Persistence/IniSnapshotRepositoryTests.cs`

**Interfaces:**
- Consumes: `IIniSnapshotRepository`, `IIniSnapshotTransaction`, `IniValueChange` (Task 5); `ZooIniDefaults`, `IniKeyId` (Tasks 1, 3).
- Produces: `public sealed class IniSnapshotRepository : IIniSnapshotRepository` with constructor `IniSnapshotRepository(InstallationDbContextFactory factory)`;
  `internal sealed class IniSnapshotTransaction`; `InstallationDbContextFactory.OpenAsync(Guid, CancellationToken)` (internal). Registered in Task 8.

- [ ] **Step 1: Make `CapturedUtc` settable**

In `Source/Erdmier.ZooTycoonLauncher.Domain/IniSnapshots/IniSnapshot.cs` change `public DateTime CapturedUtc { get; init; }` to `public DateTime CapturedUtc { get; set; }` and its
summary to `/// <summary>UTC timestamp of capture; on the <c>Current</c> snapshot, the last time the launcher changed it.</summary>`.

- [ ] **Step 2: Add the global usings**

Add to `Source/Erdmier.ZooTycoonLauncher.Infrastructure/GlobalUsings.cs`: `global using Erdmier.ZooTycoonLauncher.Domain.IniKeys;` (after `Domain.IniSnapshots`) and
`global using Microsoft.EntityFrameworkCore.Storage;` (after `Microsoft.EntityFrameworkCore.Design`).

- [ ] **Step 3: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Persistence/IniSnapshotRepositoryTests.cs`:

```csharp
using System.Data.Common;
using System.IO.Abstractions;

using Erdmier.ZooTycoonLauncher.Domain.IniKeys;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Persistence;

public sealed class IniSnapshotRepositoryTests : IDisposable
{
    private static readonly DateTime Captured = new(year: 2026, month: 9, day: 24, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"zoolauncher-snapshots-{Guid.NewGuid():N}");

    private readonly Guid _installationId = Guid.CreateVersion7();

    private readonly IniSnapshotRepository _repository;

    public IniSnapshotRepositoryTests()
    {
        Directory.CreateDirectory(_directory);

        IAppStorageLocations locations = Substitute.For<IAppStorageLocations>();

        locations.InstallationDatabasePath(Arg.Any<Guid>())
                 .Returns(call => Path.Combine(_directory, $"{call.Arg<Guid>()}.db"));

        _repository = new IniSnapshotRepository(new InstallationDbContextFactory(locations, new FileSystem()));
    }

    private string DatabasePath => Path.Combine(_directory, $"{_installationId}.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        Directory.Delete(_directory, recursive: true);
    }

    [ Fact ]
    public async Task FirstImport_RoundTripsTheCurrentSnapshot()
    {
        await ImportAsync();

        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        IniSnapshot? current = await transaction.GetCurrentAsync(CancellationToken.None);

        current.ShouldNotBeNull();
        current.StructureBlob.ShouldBe(expected: "[user]\r\nscreenwidth=800\r\n");
        current.Values.Single().Value.ShouldBe(expected: "800");
    }

    [ Fact ]
    public async Task ArchiveCurrent_CopiesBlobRowsAndSourcesIntoANewHistoricalSnapshot()
    {
        await ImportAsync();

        await using (IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None))
        {
            await transaction.ArchiveCurrentAsync(IniSnapshotTrigger.Manual, Captured.AddHours(1), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        await using InstallationDbContext context = OpenContext();

        List<IniSnapshot> snapshots = await context.Snapshots.Include(snapshot => snapshot.Values).ToListAsync();

        IniSnapshot historical = snapshots.Single(snapshot => snapshot.Kind == IniSnapshotKind.Historical);

        historical.Trigger.ShouldBe(IniSnapshotTrigger.Manual);
        historical.CapturedUtc.ShouldBe(Captured.AddHours(1));
        historical.StructureBlob.ShouldBe(expected: "[user]\r\nscreenwidth=800\r\n");
        historical.Values.Single().Source.ShouldBe(IniValueSource.OriginalImport);
        snapshots.Count.ShouldBe(expected: 3);
    }

    [ Fact ]
    public async Task UpdateCurrent_UpdatesInsertsAndDeletesRowsInPlace()
    {
        await ImportAsync(("user", "lastfile", @"C:\a.zoo"));

        await using (IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None))
        {
            IReadOnlyList<IniValueChange> changes =
            [
                new(new IniKeyId(Section: "USER", Key: "SCREENWIDTH"), Value: "1024", IniValueSource.LauncherGui),
                new(new IniKeyId(Section: "user", Key: "DrawRate"), Value: "30", IniValueSource.LauncherGui),
                new(new IniKeyId(Section: "user", Key: "lastfile"), Value: null, IniValueSource.Manual)
            ];

            await transaction.UpdateCurrentAsync(structureBlob: "new text", changes, Captured.AddDays(1), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        await using InstallationDbContext context = OpenContext();

        IniSnapshot current = (await context.Snapshots.Include(snapshot => snapshot.Values).ToListAsync()).Single(snapshot => snapshot.Kind == IniSnapshotKind.Current);

        current.StructureBlob.ShouldBe(expected: "new text");
        current.CapturedUtc.ShouldBe(Captured.AddDays(1));
        current.Values.Select(value => $"{value.Key}={value.Value}:{value.Source}").Order().ShouldBe(["DrawRate=30:LauncherGui", "screenwidth=1024:LauncherGui"]);
        current.Values.Single(value => value.Key == "DrawRate").ValueKind.ShouldBe(IniValueKind.Int);
    }

    [ Fact ]
    public async Task Dispose_WithoutCommit_RollsBack()
    {
        await using (IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None))
        {
            await transaction.AddAsync(Snapshot(IniSnapshotKind.Current), CancellationToken.None);
        }

        await using IIniSnapshotTransaction check = await _repository.BeginAsync(_installationId, CancellationToken.None);

        (await check.GetCurrentAsync(CancellationToken.None)).ShouldBeNull();
    }

    [ Fact ]
    public async Task PartialUniqueIndexes_RejectASecondCurrent()
    {
        await ImportAsync();

        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        await Should.ThrowAsync<DbUpdateException>(() => transaction.AddAsync(Snapshot(IniSnapshotKind.Current), CancellationToken.None));
    }

    [ Fact ]
    public async Task Migration_UpgradesADatabaseCreatedByTheInitialSchema()
    {
        await using (InstallationDbContext context = OpenContext())
        {
            await context.GetService<IMigrator>().MigrateAsync(targetMigration: "20260528001444_InitialInstallationSchema");
        }

        await using (IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None))
        {
            await transaction.CommitAsync(CancellationToken.None);
        }

        await using InstallationDbContext verify = OpenContext();

        (await IndexExistsAsync(verify, indexName: "IX_Snapshots_Kind_Original")).ShouldBeTrue();
        (await IndexExistsAsync(verify, indexName: "IX_Snapshots_Kind_Current")).ShouldBeTrue();
        (await IndexExistsAsync(verify, indexName: "IX_Snapshots_Kind_CapturedUtc")).ShouldBeTrue();
    }

    private async Task ImportAsync(params (string Section, string Key, string Value)[] extraValues)
    {
        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        await transaction.AddAsync(Snapshot(IniSnapshotKind.Original, extraValues), CancellationToken.None);
        await transaction.AddAsync(Snapshot(IniSnapshotKind.Current, extraValues), CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
    }

    private static IniSnapshot Snapshot(IniSnapshotKind kind, params (string Section, string Key, string Value)[] extraValues)
    {
        Guid id = Guid.CreateVersion7();

        List<IniValue> values =
        [
            new() { SnapshotId = id, Section = "user", Key = "screenwidth", Value = "800", ValueKind = IniValueKind.Int, Source = IniValueSource.OriginalImport }
        ];

        values.AddRange(extraValues.Select(extra => new IniValue
        {
            SnapshotId = id,
            Section    = extra.Section,
            Key        = extra.Key,
            Value      = extra.Value,
            ValueKind  = IniValueKind.NullableStr,
            Source     = IniValueSource.OriginalImport
        }));

        return new IniSnapshot
        {
            Id            = id,
            Kind          = kind,
            Trigger       = IniSnapshotTrigger.OriginalImport,
            CapturedUtc   = Captured,
            StructureBlob = "[user]\r\nscreenwidth=800\r\n",
            Values        = values
        };
    }

    private InstallationDbContext OpenContext()
        => new(new DbContextOptionsBuilder<InstallationDbContext>().UseSqlite($"Data Source={DatabasePath}")
                                                                    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                                                                    .Options);

    private static async Task<bool> IndexExistsAsync(InstallationDbContext context, string indexName)
    {
        await using DbConnection connection = context.Database.GetDbConnection();

        await connection.OpenAsync();

        await using DbCommand command = connection.CreateCommand();

        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'index' AND name = $name";

        DbParameter parameter = command.CreateParameter();

        parameter.ParameterName = "$name";
        parameter.Value         = indexName;

        command.Parameters.Add(parameter);

        return await command.ExecuteScalarAsync() is not null;
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run:

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj --filter "FullyQualifiedName~IniSnapshotRepositoryTests"
```
Expected: build FAILS — `IniSnapshotRepository` does not exist.

- [ ] **Step 5: Add `OpenAsync` to the factory**

In `InstallationDbContextFactory.cs`, replace `CreateAsync` with the two methods below (keep `DeleteAsync` and the nested `Handle` class unchanged):

```csharp
    /// <inheritdoc />
    public async Task<IInstallationDbContextHandle> CreateAsync(Guid installationId, CancellationToken cancellationToken)
        => new Handle(await OpenAsync(installationId, cancellationToken));

    /// <summary>Creates (when absent) or opens the per-installation database, runs migrations, and returns the open context. The caller owns and disposes it.</summary>
    /// <param name="installationId">The installation's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The open, migrated context.</returns>
    internal async Task<InstallationDbContext> OpenAsync(Guid installationId, CancellationToken cancellationToken)
    {
        string databasePath = _locations.InstallationDatabasePath(installationId);

        DbContextOptions<InstallationDbContext> options = new DbContextOptionsBuilder<InstallationDbContext>()
                                                          .UseSqlite($"Data Source={databasePath}")
                                                          .Options;

        InstallationDbContext context = new(options);

        try
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        catch
        {
            await context.DisposeAsync();

            throw;
        }

        return context;
    }
```

- [ ] **Step 6: Implement the transaction and the repository**

`Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/IniSnapshotTransaction.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation;

/// <summary>An open EF Core transaction over one installation's snapshot database. Owns the context and the transaction; disposing without committing rolls back.</summary>
internal sealed class IniSnapshotTransaction : IIniSnapshotTransaction
{
    private readonly InstallationDbContext _context;

    private readonly IDbContextTransaction _transaction;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="context">The open context (owned).</param>
    /// <param name="transaction">The begun transaction (owned).</param>
    public IniSnapshotTransaction(InstallationDbContext context, IDbContextTransaction transaction)
    {
        _context     = context;
        _transaction = transaction;
    }

    /// <inheritdoc />
    public Task<IniSnapshot?> GetCurrentAsync(CancellationToken cancellationToken)
        => _context.Snapshots
                   .Include(snapshot => snapshot.Values)
                   .SingleOrDefaultAsync(snapshot => snapshot.Kind == IniSnapshotKind.Current, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(IniSnapshot snapshot, CancellationToken cancellationToken)
    {
        _context.Snapshots.Add(snapshot);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ArchiveCurrentAsync(IniSnapshotTrigger trigger, DateTime capturedUtc, CancellationToken cancellationToken)
    {
        IniSnapshot current = await RequireCurrentAsync(cancellationToken);
        Guid        id      = Guid.CreateVersion7();

        IniSnapshot historical = new()
        {
            Id            = id,
            Kind          = IniSnapshotKind.Historical,
            Trigger       = trigger,
            CapturedUtc   = capturedUtc,
            StructureBlob = current.StructureBlob,
            Values = current.Values.Select(value => new IniValue
                            {
                                SnapshotId = id,
                                Section    = value.Section,
                                Key        = value.Key,
                                Value      = value.Value,
                                ValueKind  = value.ValueKind,
                                Source     = value.Source
                            })
                            .ToList()
        };

        _context.Snapshots.Add(historical);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateCurrentAsync(string structureBlob, IReadOnlyList<IniValueChange> changes, DateTime capturedUtc, CancellationToken cancellationToken)
    {
        IniSnapshot current = await RequireCurrentAsync(cancellationToken);

        current.StructureBlob = structureBlob;
        current.CapturedUtc   = capturedUtc;

        foreach (IniValueChange change in changes)
        {
            IniValue? row = current.Values.FirstOrDefault(value => new IniKeyId(value.Section, value.Key) == change.Id);

            if (change.Value is null)
            {
                if (row is not null)
                {
                    current.Values.Remove(row);
                    _context.IniValues.Remove(row);
                }

                continue;
            }

            if (row is null)
            {
                current.Values.Add(new IniValue
                {
                    SnapshotId = current.Id,
                    Section    = change.Id.Section,
                    Key        = change.Id.Key,
                    Value      = change.Value,
                    ValueKind  = ZooIniDefaults.TryGet(change.Id, out IniKeySpec? spec) ? spec.Kind : IniValueKind.Str,
                    Source     = change.Source
                });

                continue;
            }

            row.Value  = change.Value;
            row.Source = change.Source;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken) => _transaction.CommitAsync(cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Disposing an uncommitted EF Core transaction rolls it back.
        await _transaction.DisposeAsync();
        await _context.DisposeAsync();
    }

    private async Task<IniSnapshot> RequireCurrentAsync(CancellationToken cancellationToken)
        => await GetCurrentAsync(cancellationToken) ?? throw new InvalidOperationException(message: "The installation's database has no Current snapshot.");
}
```

If running the tests later reports that `snapshot.Kind == IniSnapshotKind.Current` "could not be translated", replace the body of `GetCurrentAsync` with
`_context.Snapshots.FromSqlRaw("SELECT * FROM \"Snapshots\" WHERE \"Kind\" = 'Current'").Include(snapshot => snapshot.Values).SingleOrDefaultAsync(cancellationToken)` —
the value converter stores the SmartEnum's `Name`.

`Source/Erdmier.ZooTycoonLauncher.Infrastructure/Persistence/Installation/IniSnapshotRepository.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation;

/// <summary><see cref="IIniSnapshotRepository" /> over the per-installation <see cref="InstallationDbContext" />.</summary>
public sealed class IniSnapshotRepository : IIniSnapshotRepository
{
    private readonly InstallationDbContextFactory _factory;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="factory">The per-installation context factory.</param>
    public IniSnapshotRepository(InstallationDbContextFactory factory) => _factory = factory;

    /// <inheritdoc />
    public async Task<IIniSnapshotTransaction> BeginAsync(Guid installationId, CancellationToken cancellationToken)
    {
        InstallationDbContext context = await _factory.OpenAsync(installationId, cancellationToken);

        try
        {
            IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            return new IniSnapshotTransaction(context, transaction);
        }
        catch
        {
            await context.DisposeAsync();

            throw;
        }
    }
}
```

- [ ] **Step 7: Add the indexes and generate the migration**

In `IniSnapshotConfiguration.Configure`, add after the `StructureBlob` property block (before `HasMany`):

```csharp
        builder.HasIndex(s => s.Kind, name: "IX_Snapshots_Kind_Original")
               .IsUnique()
               .HasFilter(sql: "\"Kind\" = 'Original'");

        builder.HasIndex(s => s.Kind, name: "IX_Snapshots_Kind_Current")
               .IsUnique()
               .HasFilter(sql: "\"Kind\" = 'Current'");

        builder.HasIndex(s => new
               {
                   s.Kind,
                   s.CapturedUtc
               },
               name: "IX_Snapshots_Kind_CapturedUtc")
               .IsDescending(false, true);
```

Update the class summary to mention that the two partial unique indexes enforce "at most one `Original` and one `Current`".

Then run:

```powershell
dotnet ef migrations add AddSnapshotIndexes --project Source/Erdmier.ZooTycoonLauncher.Infrastructure --context InstallationDbContext --output-dir Persistence/Installation/Migrations
```

Open the generated `*_AddSnapshotIndexes.cs`. `Up` must contain exactly three `CreateIndex` calls (the names above) and `Down` the three matching `DropIndex` calls. If it contains
any other operation, stop and report it instead of continuing.

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj`
Expected: PASS — `IniSnapshotRepositoryTests` plus every existing Infrastructure test.

- [ ] **Step 9: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Domain Source/Erdmier.ZooTycoonLauncher.Infrastructure Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration
git commit -m @'
feat(✨): add the INI snapshot repository and the missing snapshot indexes

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 8: `IniSnapshotService` replaces `NullIniSnapshotService`

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Abstractions/IIniSnapshotService.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniSnapshotService.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Common/Extensions/ApplicationServiceCollectionExtensions.cs`
- Delete: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/IniSnapshots/NullIniSnapshotService.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/Common/Extensions/InfrastructureServiceCollectionExtensions.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Infrastructure/GlobalUsings.cs` (remove `global using Erdmier.ZooTycoonLauncher.Infrastructure.IniSnapshots;`)
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/IniSnapshotServiceTests.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Composition/IniSnapshotCompositionTests.cs`

**Interfaces:**
- Consumes: Task 5 seams and `IniReconciler`; Task 6 `IniFileStore`; Task 7 `IniSnapshotRepository`, `InstallationDbContextFactory`.
- Produces: `IIniSnapshotService.LoadAsync(GameInstallation installation, CancellationToken cancellationToken) → Task<ErrorOr<IniConfigResult>>`;
  `public sealed class IniSnapshotService : IIniSnapshotService` with constructor
  `IniSnapshotService(TimeProvider clock, IIniFileStore files, ILogger<IniSnapshotService> logger, IniReconciler reconciler, IIniSnapshotRepository snapshots)`.

- [ ] **Step 1: Extend the interface**

Replace `IIniSnapshotService.cs` with:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Capture, synchronisation, and load surface for per-installation INI snapshots (SDD §7.1, §7.3, §7.7). Implemented by <c>IniSnapshotService</c>.</summary>
public interface IIniSnapshotService
{
    /// <summary>
    ///     Imports <c>zoo.ini</c> into a freshly created installation database (<c>Original</c> + <c>Current</c>). No-op when <see cref="GameInstallation.HasIni" /> is
    ///     <see langword="false" />.
    /// </summary>
    /// <param name="installation">The newly created installation row.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success on import or no-op; a typed error on read or store failure.</returns>
    Task<ErrorOr<Success>> CaptureOriginalAsync(GameInstallation installation, CancellationToken cancellationToken);

    /// <summary>
    ///     Reconciles the <c>Current</c> snapshot with the file on disk using tiered drift (first import when empty). No-op when <see cref="GameInstallation.HasIni" /> is
    ///     <see langword="false" />.
    /// </summary>
    /// <param name="installation">The installation to synchronise.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success on synchronisation or no-op; a typed error on read or store failure.</returns>
    Task<ErrorOr<Success>> SynchroniseAsync(GameInstallation installation, CancellationToken cancellationToken);

    /// <summary>Synchronises, then returns the reconciled values and the file's last-write time for the editor.</summary>
    /// <param name="installation">The installation to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The values, or <c>Ini.Missing</c> / <c>Ini.ReadFailed</c> / <c>Ini.StoreFailed</c>.</returns>
    Task<ErrorOr<IniConfigResult>> LoadAsync(GameInstallation installation, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/IniSnapshotServiceTests.cs`:

```csharp
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute.ExceptionExtensions;

namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class IniSnapshotServiceTests
{
    private readonly IIniFileStore _files = Substitute.For<IIniFileStore>();

    private readonly GameInstallation _installation = new()
    {
        Id       = Guid.CreateVersion7(),
        Name     = "Main",
        Path     = @"C:\ZT",
        HasExe   = true,
        HasIni   = true,
        AddedUtc = IniTestData.Now
    };

    private readonly IIniSnapshotRepository _snapshots = Substitute.For<IIniSnapshotRepository>();

    private IniSnapshotService CreateService()
        => new(new FakeTimeProvider(IniTestData.Now), _files, NullLogger<IniSnapshotService>.Instance, new IniReconciler(), _snapshots);

    [ Fact ]
    public async Task Synchronise_NoIni_IsANoOp()
    {
        _installation.HasIni = false;

        ErrorOr<Success> result = await CreateService().SynchroniseAsync(_installation, CancellationToken.None);

        result.IsError.ShouldBeFalse();

        await _files.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Load_NoIni_ReturnsMissing()
    {
        _installation.HasIni = false;

        ErrorOr<IniConfigResult> result = await CreateService().LoadAsync(_installation, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.Missing");
    }

    [ Fact ]
    public async Task Load_FileAbsent_ReturnsMissing()
    {
        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns((IniFileContent?)null);

        ErrorOr<IniConfigResult> result = await CreateService().LoadAsync(_installation, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.Missing");
    }

    [ Fact ]
    public async Task Load_ReadThrowsIoException_ReturnsReadFailed()
    {
        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .ThrowsAsync(new IOException(message: "locked"));

        ErrorOr<IniConfigResult> result = await CreateService().LoadAsync(_installation, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.ReadFailed");
        result.FirstError.Description.ShouldContain(expected: "locked");
    }

    [ Fact ]
    public async Task Load_RepositoryThrows_ReturnsStoreFailed()
    {
        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(IniTestData.Sample, IniTestData.Now));

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .ThrowsAsync(new InvalidOperationException(message: "database is corrupt"));

        ErrorOr<IniConfigResult> result = await CreateService().LoadAsync(_installation, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.StoreFailed");
    }

    [ Fact ]
    public async Task Load_Success_CleansOrphansFirstThenCommitsAndReturnsTheValues()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(current: null);

        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(IniTestData.Sample, IniTestData.Now.AddMinutes(-5)));

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(transaction);

        ErrorOr<IniConfigResult> result = await CreateService().LoadAsync(_installation, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.FileLastWriteUtc.ShouldBe(IniTestData.Now.AddMinutes(-5));
        result.Value.Values[new IniKeyId(Section: "user", Key: "screenwidth")].ShouldBe(expected: "800");

        Received.InOrder(() =>
        {
            _files.DeleteOrphanedTempFilesAsync(_installation.Path, Arg.Any<CancellationToken>());
            _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>());
            transaction.AddAsync(Arg.Any<IniSnapshot>(), Arg.Any<CancellationToken>());
            transaction.AddAsync(Arg.Any<IniSnapshot>(), Arg.Any<CancellationToken>());
            transaction.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [ Fact ]
    public async Task CaptureOriginal_ImportsAndCommits()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(current: null);

        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(IniTestData.Sample, IniTestData.Now));

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(transaction);

        ErrorOr<Success> result = await CreateService().CaptureOriginalAsync(_installation, CancellationToken.None);

        result.IsError.ShouldBeFalse();

        await transaction.Received(requiredNumberOfCalls: 1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
```

`Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Composition/IniSnapshotCompositionTests.cs`:

```csharp
using Erdmier.ZooTycoonLauncher.Application.Common.Extensions;
using Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;
using Erdmier.ZooTycoonLauncher.Infrastructure.Common.Extensions;

using Microsoft.Extensions.DependencyInjection;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Composition;

/// <summary>Composition guard: the real INI snapshot service and its seams resolve from the composed container, and the placeholder is gone.</summary>
public sealed class IniSnapshotCompositionTests
{
    [ Fact ]
    public void AddApplicationAndInfrastructure_ResolveTheRealIniServices()
    {
        ServiceCollection services = new();

        services.AddInfrastructure();
        services.AddApplication();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope   scope    = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IIniSnapshotService>().ShouldBeOfType<IniSnapshotService>();
        scope.ServiceProvider.GetRequiredService<IIniFileStore>().ShouldBeOfType<IniFileStore>();
        scope.ServiceProvider.GetRequiredService<IIniSnapshotRepository>().ShouldBeOfType<IniSnapshotRepository>();
        scope.ServiceProvider.GetRequiredService<IInstallationDbContextFactory>().ShouldBeOfType<InstallationDbContextFactory>();
    }

    [ Fact ]
    public void NullIniSnapshotService_NoLongerExists()
        => typeof(IniFileStore).Assembly.GetTypes().ShouldNotContain(type => type.Name == "NullIniSnapshotService");
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run:

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj --filter "FullyQualifiedName~IniSnapshotServiceTests"
```
Expected: build FAILS — `IniSnapshotService` does not exist (the Infrastructure project also fails to build because `NullIniSnapshotService` no longer implements the extended
interface; that is fixed in Step 5).

- [ ] **Step 4: Implement `IniSnapshotService`**

`Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Common/IniSnapshotService.cs`:

```csharp
using Microsoft.Extensions.Logging;

namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;

/// <summary>
///     The real <see cref="IIniSnapshotService" />: reads <c>zoo.ini</c>, then reconciles the <c>Current</c> snapshot with it in one transaction (first import, tiered drift). Read
///     failures and snapshot-store failures are returned as errors so a bad file or database degrades the installation to Cannot Play instead of failing the boot.
/// </summary>
public sealed class IniSnapshotService : IIniSnapshotService
{
    private readonly TimeProvider _clock;

    private readonly IIniFileStore _files;

    private readonly ILogger<IniSnapshotService> _logger;

    private readonly IniReconciler _reconciler;

    private readonly IIniSnapshotRepository _snapshots;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="clock">Time provider for UTC timestamps.</param>
    /// <param name="files">The <c>zoo.ini</c> file store.</param>
    /// <param name="logger">Logger for outcomes and failures.</param>
    /// <param name="reconciler">The tiered-drift reconciler.</param>
    /// <param name="snapshots">The snapshot repository.</param>
    public IniSnapshotService(TimeProvider                clock,
                              IIniFileStore               files,
                              ILogger<IniSnapshotService> logger,
                              IniReconciler               reconciler,
                              IIniSnapshotRepository      snapshots)
    {
        _clock      = clock;
        _files      = files;
        _logger     = logger;
        _reconciler = reconciler;
        _snapshots  = snapshots;
    }

    /// <inheritdoc />
    public Task<ErrorOr<Success>> CaptureOriginalAsync(GameInstallation installation, CancellationToken cancellationToken)
        => SynchroniseAsync(installation, cancellationToken);

    /// <inheritdoc />
    public async Task<ErrorOr<Success>> SynchroniseAsync(GameInstallation installation, CancellationToken cancellationToken)
    {
        if (!installation.HasIni)
        {
            return Result.Success;
        }

        ErrorOr<IniConfigResult> loaded = await LoadAsync(installation, cancellationToken);

        return loaded.IsError ? loaded.Errors : Result.Success;
    }

    /// <inheritdoc />
    public async Task<ErrorOr<IniConfigResult>> LoadAsync(GameInstallation installation, CancellationToken cancellationToken)
    {
        if (!installation.HasIni)
        {
            return IniErrors.Missing(installation.Path);
        }

        IniFileContent? content;

        try
        {
            await _files.DeleteOrphanedTempFilesAsync(installation.Path, cancellationToken);

            content = await _files.ReadAsync(installation.Path, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, message: "Reading zoo.ini failed for {InstallationId}", installation.Id);

            return IniErrors.ReadFailed(ex.Message);
        }

        if (content is null)
        {
            return IniErrors.Missing(installation.Path);
        }

        try
        {
            await using IIniSnapshotTransaction transaction = await _snapshots.BeginAsync(installation.Id, cancellationToken);

            IniReconciliation reconciliation = await _reconciler.ReconcileAsync(transaction,
                                                                                content.Text,
                                                                                _clock.GetUtcNow()
                                                                                      .UtcDateTime,
                                                                                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            if (reconciliation.Outcome != IniReconciliationOutcome.Unchanged)
            {
                _logger.LogInformation(message: "INI snapshot reconciled for {InstallationId}: {Outcome}", installation.Id, reconciliation.Outcome);
            }

            return new IniConfigResult(reconciliation.Values, content.LastWriteUtc);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, message: "The INI snapshot store failed for {InstallationId}", installation.Id);

            return IniErrors.StoreFailed(ex.Message);
        }
    }
}
```

- [ ] **Step 5: Swap the registrations and delete the placeholder**

In `ApplicationServiceCollectionExtensions.AddApplication`, add before `return services;`:

```csharp
        services.AddSingleton<IniReconciler>();
        services.AddScoped<IIniSnapshotService, IniSnapshotService>();
```

and update its summary to mention "the INI snapshot service". Delete `Source/Erdmier.ZooTycoonLauncher.Infrastructure/IniSnapshots/NullIniSnapshotService.cs` (and the now-empty
`IniSnapshots` folder) and remove `global using Erdmier.ZooTycoonLauncher.Infrastructure.IniSnapshots;` from the Infrastructure `GlobalUsings.cs`. In
`InfrastructureServiceCollectionExtensions.AddInfrastructure`, replace

```csharp
        services.AddSingleton<IInstallationDbContextFactory, InstallationDbContextFactory>();
        services.AddScoped<IIniSnapshotService, NullIniSnapshotService>();
```

with

```csharp
        services.AddSingleton<InstallationDbContextFactory>();
        services.AddSingleton<IInstallationDbContextFactory>(provider => provider.GetRequiredService<InstallationDbContextFactory>());
        services.AddSingleton<IIniSnapshotRepository, IniSnapshotRepository>();
        services.AddSingleton<IIniFileStore, IniFileStore>();
```

In the same file, change both comments that cite `NullIniSnapshotService` as the example Serilog consumer to cite `IniFileStore`, and change "INI snapshot placeholder" in the
method summary to "INI file store and snapshot repository".

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj`
then `dotnet test Tests/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration/Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.csproj`
then `dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/Erdmier.ZooTycoonLauncher.Tests.Architecture.csproj`
Expected: all PASS.

- [ ] **Step 7: Commit**

```powershell
git add -A Source Tests
git commit -m @'
feat(✨): replace the null INI snapshot service with the real implementation

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 9: Boot and launch alignment

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Boot/BootResult.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Boot/BootHandler.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Game/Launch/LaunchGameHandler.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Application/Installations/Add/AddInstallationHandler.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Boot/BootHandlerTests.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Game/Launch/LaunchGameHandlerTests.cs`

**Interfaces:**
- Consumes: `IIniSnapshotService.SynchroniseAsync` (unchanged signature).
- Produces: `BootResult(BootOutcome Outcome, InstallationSummary? ActiveInstallation, string? LocatedCandidatePath, string? IniErrorMessage = null)` — Tasks 17–18 read
  `IniErrorMessage`.

- [ ] **Step 1: Write the failing tests**

Append to `BootHandlerTests` (inside the class):

```csharp
    [ Fact ]
    public async Task Handle_ReturnsCannotPlayWithoutSynchronising_WhenIniMissing()
    {
        Guid id = Guid.CreateVersion7();

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = id
                });

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Main",
            Path     = @"C:\ZT",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: false));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        BootHandler handler = new(settings, installations, verifier, Substitute.For<IInstallationLocator>(), snapshots, TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.Value.Outcome.ShouldBe(BootOutcome.CannotPlay);
        result.Value.IniErrorMessage.ShouldBeNull();
        row.HasIni.ShouldBeFalse();

        await snapshots.DidNotReceive().SynchroniseAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_CarriesTheIniErrorMessage_WhenSynchroniseFails()
    {
        Guid id = Guid.CreateVersion7();

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = id
                });

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Main",
            Path     = @"C:\ZT",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(row, Arg.Any<CancellationToken>())
                 .Returns(Error.Failure(code: "Ini.ReadFailed", description: "zoo.ini could not be read: locked"));

        BootHandler handler = new(settings, installations, verifier, Substitute.For<IInstallationLocator>(), snapshots, TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.Value.Outcome.ShouldBe(BootOutcome.CannotPlay);
        result.Value.IniErrorMessage.ShouldBe(expected: "zoo.ini could not be read: locked");
    }
```

Append to `LaunchGameHandlerTests` (inside the class):

```csharp
    [ Fact ]
    public async Task Handle_IniMissingAtLaunch_ReturnsDriftedWithoutLaunching()
    {
        Guid             id    = Guid.CreateVersion7();
        FakeTimeProvider clock = new(new DateTimeOffset(year: 2026, month: 9, day: 24, hour: 12, minute: 0, second: 0, TimeSpan.Zero));

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Main",
            Path     = @"C:\Games\Zoo",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: false));

        IProcessLauncher launcher = Substitute.For<IProcessLauncher>();

        LaunchGameHandler handler = new(clock, installations, NullLogger<LaunchGameHandler>.Instance, launcher, Substitute.For<ILauncherSettingsRepository>(), verifier);

        ErrorOr<LaunchGameResult> result = await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

        result.Value.Outcome.ShouldBe(LaunchGameOutcome.Drifted);
        row.HasIni.ShouldBeFalse();

        await launcher.DidNotReceive().LaunchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj --filter "FullyQualifiedName~BootHandlerTests|FullyQualifiedName~LaunchGameHandlerTests"
```
Expected: build FAILS — `BootResult.IniErrorMessage` does not exist.

- [ ] **Step 3: Implement the changes**

`BootResult.cs` — add the parameter and document it:

```csharp
/// <summary>Result returned by <see cref="BootHandler" /> after running the SDD §7.1.1 state machine.</summary>
/// <param name="Outcome">The terminal state.</param>
/// <param name="ActiveInstallation">The installation to display, or <see langword="null" /> when none was resolved.</param>
/// <param name="LocatedCandidatePath">
///     Non-null when <see cref="BootOutcome.NoGameInstallationFound" /> is returned because <see cref="IInstallationLocator" /> found a candidate
///     directory but the Add Installation dialogue is deferred; surfaces the discovery to the user.
/// </param>
/// <param name="IniErrorMessage">
///     Non-null when the outcome is <see cref="BootOutcome.CannotPlay" /> because synchronising <c>zoo.ini</c> failed; the user-readable error description, shown on both tabs.
/// </param>
public sealed record BootResult(BootOutcome Outcome, InstallationSummary? ActiveInstallation, string? LocatedCandidatePath, string? IniErrorMessage = null);
```

`BootHandler.VerifyAsync` — replace the block from `if (!result.HasExe)` through the `syncResult.IsError` check with:

```csharp
        if (!result.HasExe
            || !result.HasIni)
        {
            // No EXE or no INI: the game cannot launch (SDD §7.1.2). Synchronisation is skipped — there is nothing to parse without zoo.ini.
            return new BootResult(BootOutcome.CannotPlay, Project(row, settings), LocatedCandidatePath: null);
        }

        ErrorOr<Success> syncResult = await _iniSnapshots.SynchroniseAsync(row, cancellationToken);

        if (syncResult.IsError)
        {
            return new BootResult(BootOutcome.CannotPlay, Project(row, settings), LocatedCandidatePath: null, syncResult.FirstError.Description);
        }
```

and change the `iniSnapshots` constructor parameter's doc from "(stub until the INI Config slice)" to "INI snapshot service".

`LaunchGameHandler.Handle` — replace `if (!result.HasExe)` with:

```csharp
        if (!result.HasExe
            || !result.HasIni)
```

and add above it the comment `// Without zoo.exe there is nothing to start; without zoo.ini the launcher will not start the game (SDD §7.1.2). Either way the caller re-boots.`

`AddInstallationHandler.Handle` — replace the comment inside `if (snapshotResult.IsError)` with:

```csharp
            // The installation is persisted; a capture failure is non-fatal. Its database stays empty, and the next synchronise (at boot or when the INI tab opens) retries
            // the first import. IniSnapshotService has already logged the failure.
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj`
Expected: PASS — every Application test.

- [ ] **Step 5: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Application Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m @'
fix(🐛): resolve a missing zoo.ini to Cannot Play and carry INI sync errors

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 10: `GetIniConfigQuery`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Get/GetIniConfigQuery.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Get/GetIniConfigHandler.cs`
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs` (add `global using Erdmier.ZooTycoonLauncher.Application.IniConfig.Get;`)
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/GetIniConfigHandlerTests.cs`

**Interfaces:**
- Consumes: `IIniSnapshotService.LoadAsync` (Task 8), `IniErrors` (Task 5).
- Produces: `sealed record GetIniConfigQuery(Guid InstallationId) : IQuery<ErrorOr<IniConfigResult>>` — sent by the Desktop in Tasks 15–16.

- [ ] **Step 1: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/GetIniConfigHandlerTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class GetIniConfigHandlerTests
{
    [ Fact ]
    public async Task Handle_UnknownInstallation_ReturnsNotFound()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        GetIniConfigHandler handler = new(installations, Substitute.For<IIniSnapshotService>());

        ErrorOr<IniConfigResult> result = await handler.Handle(new GetIniConfigQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Installation.NotFound");
    }

    [ Fact ]
    public async Task Handle_DelegatesToLoad()
    {
        GameInstallation row = new()
        {
            Id       = Guid.CreateVersion7(),
            Name     = "Main",
            Path     = @"C:\ZT",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = IniTestData.Now
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(row.Id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IniConfigResult     expected  = new(new Dictionary<IniKeyId, string?>(), IniTestData.Now);
        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.LoadAsync(row, Arg.Any<CancellationToken>())
                 .Returns(expected);

        GetIniConfigHandler handler = new(installations, snapshots);

        ErrorOr<IniConfigResult> result = await handler.Handle(new GetIniConfigQuery(row.Id), CancellationToken.None);

        result.Value.ShouldBeSameAs(expected);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj --filter "FullyQualifiedName~GetIniConfigHandlerTests"
```
Expected: build FAILS — `GetIniConfigQuery` does not exist (and `Application.IniConfig.Get` is an empty namespace until Step 3).

- [ ] **Step 3: Implement the query and handler**

`Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Get/GetIniConfigQuery.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Get;

/// <summary>Reconciles an installation's <c>Current</c> snapshot with the file on disk and returns its recognised values for the INI Config editor.</summary>
/// <param name="InstallationId">The installation's identifier.</param>
public sealed record GetIniConfigQuery(Guid InstallationId) : IQuery<ErrorOr<IniConfigResult>>;
```

`Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Get/GetIniConfigHandler.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Get;

/// <summary>Handler for <see cref="GetIniConfigQuery" />.</summary>
public sealed class GetIniConfigHandler : IQueryHandler<GetIniConfigQuery, ErrorOr<IniConfigResult>>
{
    private readonly IInstallationRepository _installations;

    private readonly IIniSnapshotService _snapshots;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="snapshots">INI snapshot service.</param>
    public GetIniConfigHandler(IInstallationRepository installations, IIniSnapshotService snapshots)
    {
        _installations = installations;
        _snapshots     = snapshots;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<IniConfigResult>> Handle(GetIniConfigQuery query, CancellationToken cancellationToken)
    {
        GameInstallation? installation = await _installations.GetByIdAsync(query.InstallationId, cancellationToken);

        if (installation is null)
        {
            return IniErrors.InstallationNotFound(query.InstallationId);
        }

        return await _snapshots.LoadAsync(installation, cancellationToken);
    }
}
```

Add `global using Erdmier.ZooTycoonLauncher.Application.IniConfig.Get;` to the Application.Tests.Unit `GlobalUsings.cs`.

- [ ] **Step 4: Run the tests to verify they pass**

Run:

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj --filter "FullyQualifiedName~GetIniConfigHandlerTests"
```
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Application Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m @'
feat(✨): add the GetIniConfig query

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 11: `SaveIniCommand` — validator and merge-on-save handler

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Save/SaveIniCommand.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Save/SaveIniValidator.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Save/SaveIniHandler.cs`
- Modify: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/GlobalUsings.cs` (add `global using Erdmier.ZooTycoonLauncher.Application.IniConfig.Save;`)
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/SaveIniValidatorTests.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/SaveIniHandlerTests.cs`

**Interfaces:**
- Consumes: Task 5 seams, `IniReconciler`, `IniErrors`; `IniDocument`, `ZooIniDefaults` (Tasks 2–3).
- Produces: `sealed record SaveIniCommand(Guid InstallationId, IReadOnlyDictionary<IniKeyId, string> Edits) : ICommand<ErrorOr<IniConfigResult>>` — sent by the Desktop editor in
  Task 15. `SaveIniHandler` constructor:

  ```csharp
  SaveIniHandler(TimeProvider clock, IIniFileStore files, IInstallationRepository installations, ILogger<SaveIniHandler> logger, IniReconciler reconciler, IIniSnapshotRepository snapshots)
  ```

- [ ] **Step 1: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/SaveIniValidatorTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class SaveIniValidatorTests
{
    private readonly SaveIniValidator _validator = new();

    [ Theory ]
    [ InlineData("user", "screenwidth", "1024") ]
    [ InlineData("UI", "menuMusic", "sounds/caf\u00E9.wav") ]
    [ InlineData("UI", "menuMusic", "") ]
    [ InlineData("user", "fullscreen", "0") ]
    public void Validate_AcceptsValidEdits(string section, string key, string value) => Validate(section, key, value).IsValid.ShouldBeTrue();

    [ Theory ]
    [ InlineData("scenario", "ag", "0") ]
    [ InlineData("user", "lastfile", @"C:\a.zoo") ]
    [ InlineData("user", "UpdateRate", "0") ]
    [ InlineData("user", "UpdateRate", "fast") ]
    [ InlineData("user", "screenwidth", "") ]
    [ InlineData("UI", "menuMusic", "a\r\nb") ]
    [ InlineData("UI", "menuMusic", "\u20AC.wav") ]
    public void Validate_RejectsInvalidEdits(string section, string key, string value)
    {
        ValidationResult result = Validate(section, key, value);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldAllBe(error => !string.IsNullOrWhiteSpace(error.ErrorMessage));
    }

    [ Fact ]
    public void Validate_RejectsAnEmptyIdAndNoEdits()
    {
        ValidationResult result = _validator.Validate(new SaveIniCommand(Guid.Empty, new Dictionary<IniKeyId, string>()));

        result.Errors.Count.ShouldBe(expected: 2);
    }

    private ValidationResult Validate(string section, string key, string value)
        => _validator.Validate(new SaveIniCommand(Guid.CreateVersion7(), new Dictionary<IniKeyId, string> { [new IniKeyId(section, key)] = value }));
}
```

`Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/IniConfig/SaveIniHandlerTests.cs`:

```csharp
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute.ExceptionExtensions;

namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class SaveIniHandlerTests
{
    private static readonly DateTime DiskLastWrite = IniTestData.Now.AddHours(-1);

    private static readonly DateTime NewLastWrite = IniTestData.Now.AddSeconds(1);

    private static readonly IniKeyId ScreenWidth = new(Section: "user", Key: "screenwidth");

    private readonly IIniFileStore _files = Substitute.For<IIniFileStore>();

    private readonly GameInstallation _installation = new()
    {
        Id       = Guid.CreateVersion7(),
        Name     = "Main",
        Path     = @"C:\ZT",
        HasExe   = true,
        HasIni   = true,
        AddedUtc = IniTestData.Now
    };

    private readonly IInstallationRepository _installations = Substitute.For<IInstallationRepository>();

    private readonly IIniSnapshotRepository _snapshots = Substitute.For<IIniSnapshotRepository>();

    private IIniSnapshotTransaction _transaction = IniTestData.Transaction(IniTestData.CurrentSnapshot(IniTestData.Sample));

    private string? _written;

    public SaveIniHandlerTests()
    {
        _installations.GetByIdAsync(_installation.Id, Arg.Any<CancellationToken>())
                      .Returns(_installation);

        _files.WriteAsync(_installation.Path, Arg.Do<string>(text => _written = text), Arg.Any<CancellationToken>())
              .Returns(NewLastWrite);

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(_ => _transaction);
    }

    [ Fact ]
    public async Task Save_FollowsTheSdd82Order()
    {
        DiskHolds(IniTestData.Sample);

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "1024"));

        result.IsError.ShouldBeFalse();

        Received.InOrder(() =>
        {
            _transaction.ArchiveCurrentAsync(IniSnapshotTrigger.LauncherGui, IniTestData.Now, Arg.Any<CancellationToken>());
            _files.WriteAsync(_installation.Path, Arg.Any<string>(), Arg.Any<CancellationToken>());
            _transaction.UpdateCurrentAsync(Arg.Any<string>(),
                                            Arg.Is<IReadOnlyList<IniValueChange>>(changes => changes.Count == 1
                                                                                            && changes[0].Id == ScreenWidth
                                                                                            && changes[0].Value == "1024"
                                                                                            && changes[0].Source == IniValueSource.LauncherGui),
                                            IniTestData.Now,
                                            Arg.Any<CancellationToken>());
            _transaction.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [ Fact ]
    public async Task Save_ChangesOnlyTheEditedLineAndKeepsNonAsciiBytes()
    {
        DiskHolds(IniTestData.Sample);

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "1024"));

        _written.ShouldBe(IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024"));
        _written.ShouldContain(expected: "Zoo M\u00FCller.zoo");
        result.Value.Values[ScreenWidth].ShouldBe(expected: "1024");
        result.Value.FileLastWriteUtc.ShouldBe(NewLastWrite);
    }

    [ Fact ]
    public async Task Save_MergesOntoTheOnDiskTextAndArchivesExternalDriftFirst()
    {
        string disk = IniTestData.Sample.Replace(oldValue: "tooltipDelay=1", newValue: "tooltipDelay=5").Replace(oldValue: "lastWindowX=10", newValue: "lastWindowX=300");

        DiskHolds(disk);

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "1024"));

        _written.ShouldBe(disk.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024"));
        result.Value.Values[new IniKeyId(Section: "UI", Key: "tooltipDelay")].ShouldBe(expected: "5");

        Received.InOrder(() =>
        {
            _transaction.ArchiveCurrentAsync(IniSnapshotTrigger.Manual, IniTestData.Now, Arg.Any<CancellationToken>());
            _transaction.ArchiveCurrentAsync(IniSnapshotTrigger.LauncherGui, IniTestData.Now, Arg.Any<CancellationToken>());
        });
    }

    [ Fact ]
    public async Task Save_WriteFails_RollsBackAndReturnsWriteFailed()
    {
        DiskHolds(IniTestData.Sample);

        _files.WriteAsync(_installation.Path, Arg.Any<string>(), Arg.Any<CancellationToken>())
              .ThrowsAsync(new UnauthorizedAccessException(message: "Access to the path is denied."));

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "1024"));

        result.FirstError.Code.ShouldBe(expected: "Ini.WriteFailed");
        result.FirstError.Description.ShouldContain(expected: "denied");

        await _transaction.DidNotReceive().UpdateCurrentAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<IniValueChange>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(requiredNumberOfCalls: 1).DisposeAsync();
    }

    [ Fact ]
    public async Task Save_NoOpEdits_CommitWithoutWriting()
    {
        DiskHolds(IniTestData.Sample);

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "0800"));

        result.Value.FileLastWriteUtc.ShouldBe(DiskLastWrite);

        await _files.DidNotReceive().WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _transaction.Received(requiredNumberOfCalls: 1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Save_EmptiedFile_ImportsThenAppendsTheSectionAndKey()
    {
        _transaction = IniTestData.Transaction(current: null);

        DiskHolds(text: "");

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "1024"));

        result.IsError.ShouldBeFalse();
        _written.ShouldBe(expected: "[user]\r\nscreenwidth=1024\r\n");

        await _transaction.Received(requiredNumberOfCalls: 2).AddAsync(Arg.Any<IniSnapshot>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Save_EditKeyInAnyCasing_InsertsWithRegistryCasing()
    {
        DiskHolds(IniTestData.Sample);

        ErrorOr<IniConfigResult> result = await SaveAsync((new IniKeyId(Section: "USER", Key: "drawrate"), "30"));

        // DrawRate is missing from [user], so it is inserted after that section's last key (lastfile=…).
        _written.ShouldBe(IniTestData.Sample.Replace(oldValue: "Zoo M\u00FCller.zoo\r\n", newValue: "Zoo M\u00FCller.zoo\r\nDrawRate=30\r\n"));
        result.Value.Values[new IniKeyId(Section: "user", Key: "DrawRate")].ShouldBe(expected: "30");
    }

    [ Fact ]
    public async Task Save_FileMissing_ReturnsMissing()
    {
        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns((IniFileContent?)null);

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "1024"));

        result.FirstError.Code.ShouldBe(expected: "Ini.Missing");
    }

    [ Fact ]
    public async Task Save_UnknownInstallation_ReturnsNotFound()
    {
        SaveIniHandler handler = CreateHandler();

        ErrorOr<IniConfigResult> result = await handler.Handle(new SaveIniCommand(Guid.CreateVersion7(), new Dictionary<IniKeyId, string> { [ScreenWidth] = "1024" }),
                                                               CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Installation.NotFound");
    }

    private void DiskHolds(string text)
        => _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
                 .Returns(new IniFileContent(text, DiskLastWrite));

    private async Task<ErrorOr<IniConfigResult>> SaveAsync(params (IniKeyId Id, string Value)[] edits)
        => await CreateHandler().Handle(new SaveIniCommand(_installation.Id, edits.ToDictionary(edit => edit.Id, edit => edit.Value)), CancellationToken.None);

    private SaveIniHandler CreateHandler()
        => new(new FakeTimeProvider(IniTestData.Now), _files, _installations, NullLogger<SaveIniHandler>.Instance, new IniReconciler(), _snapshots);
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj --filter "FullyQualifiedName~SaveIni"`
Expected: build FAILS — `SaveIniCommand` does not exist.

- [ ] **Step 3: Implement the command, validator, and handler**

`Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Save/SaveIniCommand.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Save;

/// <summary>Saves the user's INI edits: archive <c>Current</c>, write <c>zoo.ini</c> atomically, update <c>Current</c> — merged onto the file as it is on disk (SDD §7.3.2, §8.2).</summary>
/// <param name="InstallationId">The installation's identifier.</param>
/// <param name="Edits">The edited keys and their new raw values; an empty value clears the key (<c>key=</c>).</param>
public sealed record SaveIniCommand(Guid InstallationId, IReadOnlyDictionary<IniKeyId, string> Edits) : ICommand<ErrorOr<IniConfigResult>>;
```

`Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Save/SaveIniValidator.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Save;

/// <summary>FluentValidation rules for <see cref="SaveIniCommand" />. Messages are shown verbatim in the editor's error dialogue.</summary>
public sealed class SaveIniValidator : AbstractValidator<SaveIniCommand>
{
    /// <summary>Initialises a new instance.</summary>
    public SaveIniValidator()
    {
        RuleFor(command => command.InstallationId)
            .NotEmpty()
            .WithMessage(errorMessage: "An installation must be specified.");

        RuleFor(command => command.Edits)
            .NotEmpty()
            .WithMessage(errorMessage: "There are no changes to save.");

        RuleForEach(command => command.Edits)
            .Custom((edit, context) =>
            {
                if (!ZooIniDefaults.TryGet(edit.Key, out IniKeySpec? spec))
                {
                    context.AddFailure($"{edit.Key} is not a setting the launcher recognises.");

                    return;
                }

                if (spec.Role != IniKeyRole.UserSetting)
                {
                    context.AddFailure($"{spec.Id} is managed by the game and cannot be edited.");

                    return;
                }

                if (!spec.IsValid(edit.Value))
                {
                    context.AddFailure($"\"{edit.Value}\" is not a valid value for {spec.Id}.");

                    return;
                }

                if (edit.Value.Any(character => character is '\r' or '\n'))
                {
                    context.AddFailure($"{spec.Id} cannot contain a line break.");
                }
                else if (edit.Value.Any(character => character > '\u00FF'))
                {
                    context.AddFailure($"{spec.Id} contains a character zoo.ini cannot store.");
                }
            });
    }
}
```

`Source/Erdmier.ZooTycoonLauncher.Application/IniConfig/Save/SaveIniHandler.cs`:

```csharp
using Microsoft.Extensions.Logging;

namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Save;

/// <summary>
///     Handler for <see cref="SaveIniCommand" />. Follows SDD §8.2 inside one transaction, but builds the new file from the text on disk rather than from the stored blob, after
///     first reconciling any external drift — so values the game (or a hand edit) wrote since the editor loaded are never overwritten unless the user edited that key.
/// </summary>
public sealed class SaveIniHandler : ICommandHandler<SaveIniCommand, ErrorOr<IniConfigResult>>
{
    private readonly TimeProvider _clock;

    private readonly IIniFileStore _files;

    private readonly IInstallationRepository _installations;

    private readonly ILogger<SaveIniHandler> _logger;

    private readonly IniReconciler _reconciler;

    private readonly IIniSnapshotRepository _snapshots;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="clock">Time provider for UTC timestamps.</param>
    /// <param name="files">The <c>zoo.ini</c> file store.</param>
    /// <param name="installations">Installation repository.</param>
    /// <param name="logger">Logger for write failures.</param>
    /// <param name="reconciler">The tiered-drift reconciler.</param>
    /// <param name="snapshots">The snapshot repository.</param>
    public SaveIniHandler(TimeProvider            clock,
                          IIniFileStore           files,
                          IInstallationRepository installations,
                          ILogger<SaveIniHandler> logger,
                          IniReconciler           reconciler,
                          IIniSnapshotRepository  snapshots)
    {
        _clock         = clock;
        _files         = files;
        _installations = installations;
        _logger        = logger;
        _reconciler    = reconciler;
        _snapshots     = snapshots;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<IniConfigResult>> Handle(SaveIniCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? installation = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (installation is null)
        {
            return IniErrors.InstallationNotFound(command.InstallationId);
        }

        IniFileContent? content;

        try
        {
            content = await _files.ReadAsync(installation.Path, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return IniErrors.ReadFailed(ex.Message);
        }

        if (content is null)
        {
            return IniErrors.Missing(installation.Path);
        }

        DateTime nowUtc = _clock.GetUtcNow()
                                .UtcDateTime;

        await using IIniSnapshotTransaction transaction = await _snapshots.BeginAsync(installation.Id, cancellationToken);

        IniReconciliation reconciliation = await _reconciler.ReconcileAsync(transaction, content.Text, nowUtc, cancellationToken);

        Dictionary<IniKeyId, string> edits = [];

        foreach ((IniKeyId id, string value) in command.Edits)
        {
            // The validator guarantees the key is recognised; normalising to the registry id gives inserted keys the registry's casing.
            IniKeySpec spec = ZooIniDefaults.TryGet(id, out IniKeySpec? found) ? found : throw new InvalidOperationException($"Unrecognised key {id}.");

            if (!spec.AreEquivalent(value, reconciliation.Values.GetValueOrDefault(spec.Id)))
            {
                edits[spec.Id] = value;
            }
        }

        if (edits.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);

            return new IniConfigResult(reconciliation.Values, content.LastWriteUtc);
        }

        await transaction.ArchiveCurrentAsync(IniSnapshotTrigger.LauncherGui, nowUtc, cancellationToken);

        IniDocument document = IniDocument.Parse(content.Text);

        foreach ((IniKeyId id, string value) in edits)
        {
            document.SetValue(id, value);
        }

        string text = document.Render();

        DateTime lastWriteUtc;

        try
        {
            lastWriteUtc = await _files.WriteAsync(installation.Path, text, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Returning disposes the transaction uncommitted, rolling back the archive: the file and the database both keep their previous state.
            _logger.LogWarning(ex, message: "Writing zoo.ini failed for {InstallationId}", installation.Id);

            return IniErrors.WriteFailed(ex.Message);
        }

        List<IniValueChange> changes = edits.Select(edit => new IniValueChange(edit.Key, edit.Value, IniValueSource.LauncherGui))
                                            .ToList();

        await transaction.UpdateCurrentAsync(text, changes, nowUtc, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        Dictionary<IniKeyId, string?> values = new(reconciliation.Values);

        foreach ((IniKeyId id, string value) in edits)
        {
            values[id] = value;
        }

        return new IniConfigResult(values, lastWriteUtc);
    }
}
```

Add `global using Erdmier.ZooTycoonLauncher.Application.IniConfig.Save;` to the Application.Tests.Unit `GlobalUsings.cs`.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit/Erdmier.ZooTycoonLauncher.Application.Tests.Unit.csproj`
Expected: PASS — every Application test.

- [ ] **Step 5: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Application Tests/Erdmier.ZooTycoonLauncher.Application.Tests.Unit
git commit -m @'
feat(✨): add the SaveIni command with merge-on-save and atomic writes

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 12: Desktop test project and the presentation catalogue

**Files:**
- Create: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj`
- Create: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/GlobalUsings.cs`
- Modify: `Erdmier.ZooTycoonLauncher.slnx` (register the project under `/Tests/`)
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniControlKind.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniChoiceOption.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniLanguageOption.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniFieldDescriptor.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniFieldGroupDescriptor.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniSectionDescriptor.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniEditorCatalogue.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/IniConfig/IniEditorCatalogueTests.cs`

**Interfaces:**
- Consumes: `ZooIniDefaults`, `IniKeyId`, `IniKeySpec`, `IniKeyRole` (Tasks 1, 3).
- Produces:
  - `enum IniControlKind { Toggle, Number, Text, Choice, LanguagePicker }`; `sealed record IniChoiceOption(string Raw, string Label)`;
    `sealed record IniLanguageOption(int Lang, int SubLang, string Label)`.
  - `sealed record IniFieldDescriptor(IniKeyId? Id, string Label, IniControlKind Control, string? Hint, string Help)` with init properties `string? Caption`, `bool Inverted`,
    `IReadOnlyList<IniChoiceOption> Options` (default empty).
  - `sealed record IniFieldGroupDescriptor(string? SubHeader, IReadOnlyList<IniFieldDescriptor> Fields)`;
    `sealed record IniSectionDescriptor(string Section, string Descriptor, string? Footnote, IReadOnlyList<IniFieldGroupDescriptor> Groups)`.
  - `static class IniEditorCatalogue` — `IReadOnlyList<IniSectionDescriptor> Sections`, `IReadOnlyList<IniLanguageOption> Languages`, `IniKeyId LanguageId`
    (`[language]/lang`), `IniKeyId SubLanguageId` (`[language]/sublang`).

- [ ] **Step 1: Create the test project**

`Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <RootNamespace>Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit</RootNamespace>
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
        <ProjectReference Include="..\..\Source\Erdmier.ZooTycoonLauncher.Desktop\Erdmier.ZooTycoonLauncher.Desktop.csproj" />
    </ItemGroup>
</Project>
```

`Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/GlobalUsings.cs`:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Common.Models;
global using Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;
global using Erdmier.ZooTycoonLauncher.Domain.IniKeys;

global using NSubstitute;

global using Shouldly;

global using Xunit;
```

In `Erdmier.ZooTycoonLauncher.slnx`, add inside `<Folder Name="/Tests/">`, after the Application.Tests.Unit line:

```xml
        <Project Path="Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj" />
```

- [ ] **Step 2: Add the Desktop global usings**

Add to `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`: `global using System.Diagnostics.CodeAnalysis;` (with the other `System.*` lines),
`global using Erdmier.ZooTycoonLauncher.Domain.IniKeys;` (after `Domain.Installations`), and `global using Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;` (after
`Desktop.Models`).

- [ ] **Step 3: Write the failing catalogue tests**

`Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/IniConfig/IniEditorCatalogueTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.IniConfig;

public sealed class IniEditorCatalogueTests
{
    private static IEnumerable<IniFieldDescriptor> AllFields => IniEditorCatalogue.Sections.SelectMany(section => section.Groups).SelectMany(group => group.Fields);

    [ Fact ]
    public void Sections_FollowTheRegistryOrder() => IniEditorCatalogue.Sections.Select(section => section.Section).ShouldBe(ZooIniDefaults.Sections);

    [ Fact ]
    public void EveryUserSetting_HasExactlyOneDescriptor()
    {
        foreach (IniKeySpec spec in ZooIniDefaults.Keys.Where(spec => spec.Role == IniKeyRole.UserSetting))
        {
            AllFields.Count(field => field.Id == spec.Id).ShouldBe(expected: 1, customMessage: spec.Id.ToString());
        }
    }

    [ Fact ]
    public void EveryKeyedDescriptor_NamesAUserSettingInItsOwnSection()
    {
        foreach (IniSectionDescriptor section in IniEditorCatalogue.Sections)
        {
            foreach (IniFieldDescriptor field in section.Groups.SelectMany(group => group.Fields).Where(field => field.Id is not null))
            {
                ZooIniDefaults.TryGet(field.Id!.Value, out IniKeySpec? spec).ShouldBeTrue(field.Label);
                spec!.Role.ShouldBe(IniKeyRole.UserSetting, field.Label);
                spec.Id.Section.ShouldBe(section.Section, field.Label);
                field.Label.ShouldBe(spec.Id.Key);
            }
        }
    }

    [ Fact ]
    public void OnlyLanguagePickers_HaveNoId()
        => AllFields.Where(field => field.Id is null).ShouldAllBe(field => field.Control == IniControlKind.LanguagePicker);

    [ Fact ]
    public void ChoiceOptions_AreValidForTheirKey()
    {
        foreach (IniFieldDescriptor field in AllFields.Where(field => field.Control == IniControlKind.Choice))
        {
            ZooIniDefaults.TryGet(field.Id!.Value, out IniKeySpec? spec).ShouldBeTrue();
            field.Options.ShouldNotBeEmpty(field.Label);

            foreach (IniChoiceOption option in field.Options)
            {
                spec!.IsValid(option.Raw).ShouldBeTrue($"{field.Label}: {option.Raw}");
            }
        }
    }

    [ Fact ]
    public void EveryDescriptor_HasHelpText() => AllFields.ShouldAllBe(field => !string.IsNullOrWhiteSpace(field.Help));

    [ Fact ]
    public void Languages_AreInRange() => IniEditorCatalogue.Languages.ShouldAllBe(language => language.Lang >= 0 && language.Lang <= 65535 && language.SubLang >= 0);

    [ Fact ]
    public void UiSection_HasTheThreeSubHeaders()
        => IniEditorCatalogue.Sections.Single(section => section.Section == "UI").Groups.Select(group => group.SubHeader).ShouldBe(["Audio", "Gameplay (cash)", "Interface"]);
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj`
Expected: build FAILS — the catalogue types do not exist.

- [ ] **Step 5: Implement the catalogue types**

`Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniControlKind.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>The input control an INI row renders.</summary>
public enum IniControlKind
{
    /// <summary>A caption check box over a <c>Bool</c> key (optionally inverted).</summary>
    Toggle,

    /// <summary>A <c>NumericUpDown</c> over an integer key, bounded by the key's spec.</summary>
    Number,

    /// <summary>A <c>TextBox</c> over a string key.</summary>
    Text,

    /// <summary>A <c>ComboBox</c> of labelled raw values.</summary>
    Choice,

    /// <summary>The curated language combo that drives the <c>lang</c> and <c>sublang</c> rows; it has no key of its own.</summary>
    LanguagePicker
}
```

`Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniChoiceOption.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>One entry of a choice row.</summary>
/// <param name="Raw">The INI value written when the entry is chosen.</param>
/// <param name="Label">The text shown in the combo.</param>
public sealed record IniChoiceOption(string Raw, string Label);
```

`Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniLanguageOption.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>One curated Windows language (for inspiration: the Ref build's <c>IniSettingsViewModel.LanguageOptions</c>).</summary>
/// <param name="Lang">The Windows LANGID written to <c>[language]/lang</c>.</param>
/// <param name="SubLang">The Windows SUBLANGID written to <c>[language]/sublang</c>.</param>
/// <param name="Label">The text shown in the combo.</param>
public sealed record IniLanguageOption(int Lang, int SubLang, string Label);
```

`Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniFieldDescriptor.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>How one INI row is presented (SDD §9.3, <c>conventions.md</c> §3).</summary>
/// <param name="Id">The key the row edits, or <see langword="null" /> for the derived language picker.</param>
/// <param name="Label">The row label — the literal INI key.</param>
/// <param name="Control">The input control.</param>
/// <param name="Hint">The muted sub-line (units, range, shape), or <see langword="null" />.</param>
/// <param name="Help">The help text shown as the row tooltip and in the editor footer.</param>
public sealed record IniFieldDescriptor(IniKeyId? Id, string Label, IniControlKind Control, string? Hint, string Help)
{
    /// <summary>The caption beside a toggle's check box.</summary>
    public string? Caption { get; init; }

    /// <summary>Whether a toggle shows the opposite of the stored value (<c>noMenuMusic</c> reads "Play menu music").</summary>
    public bool Inverted { get; init; }

    /// <summary>The entries of a choice row.</summary>
    public IReadOnlyList<IniChoiceOption> Options { get; init; } = [];
}
```

`Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniFieldGroupDescriptor.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>A run of rows inside a section, optionally under a sub-header.</summary>
/// <param name="SubHeader">The sub-header (only <c>[UI]</c> uses them), or <see langword="null" />.</param>
/// <param name="Fields">The rows, in display order.</param>
public sealed record IniFieldGroupDescriptor(string? SubHeader, IReadOnlyList<IniFieldDescriptor> Fields);
```

`Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniSectionDescriptor.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>How one INI section is presented in the editor.</summary>
/// <param name="Section">The section name, in registry casing.</param>
/// <param name="Descriptor">The muted caption above the form pane.</param>
/// <param name="Footnote">Muted prose under the rows, or <see langword="null" />.</param>
/// <param name="Groups">The row groups, in display order.</param>
public sealed record IniSectionDescriptor(string Section, string Descriptor, string? Footnote, IReadOnlyList<IniFieldGroupDescriptor> Groups);
```

- [ ] **Step 6: Implement the catalogue**

`Source/Erdmier.ZooTycoonLauncher.Desktop/Models/IniConfig/IniEditorCatalogue.cs` (help text rewritten from the Ref build's `Resources/IniTooltips.axaml`, for inspiration):

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>
///     The INI Config editor's presentation catalogue: for every recognised user setting, its control, hint, and help text, grouped by section (SDD §9.3). Game-managed keys
///     have no entry, so they never render.
/// </summary>
public static class IniEditorCatalogue
{
    /// <summary>The <c>[language]/lang</c> key driven by the language picker.</summary>
    public static readonly IniKeyId LanguageId = new(Section: "language", Key: "lang");

    /// <summary>The <c>[language]/sublang</c> key driven by the language picker.</summary>
    public static readonly IniKeyId SubLanguageId = new(Section: "language", Key: "sublang");

    /// <summary>The curated languages offered by the language picker.</summary>
    public static IReadOnlyList<IniLanguageOption> Languages { get; } =
    [
        new(Lang: 9, SubLang: 1, Label: "English (United States)"),
        new(Lang: 9, SubLang: 2, Label: "English (United Kingdom)"),
        new(Lang: 7, SubLang: 1, Label: "German (Germany)"),
        new(Lang: 12, SubLang: 1, Label: "French (France)"),
        new(Lang: 10, SubLang: 3, Label: "Spanish (Modern)"),
        new(Lang: 16, SubLang: 1, Label: "Italian (Italy)"),
        new(Lang: 17, SubLang: 1, Label: "Japanese"),
        new(Lang: 22, SubLang: 1, Label: "Portuguese (Brazil)"),
        new(Lang: 19, SubLang: 1, Label: "Dutch (Netherlands)"),
        new(Lang: 29, SubLang: 1, Label: "Swedish (Sweden)")
    ];

    /// <summary>The seven sections, in SDD §9.3 order.</summary>
    public static IReadOnlyList<IniSectionDescriptor> Sections { get; } =
    [
        new(Section: "user",
            Descriptor: "Display and performance",
            Footnote: null,
            [
                new(SubHeader: null,
                    [
                        Choice(section: "user", key: "fullscreen", hint: "Display mode", help: "Run the game full screen or in a window.",
                               [new IniChoiceOption(Raw: "1", Label: "Fullscreen"), new IniChoiceOption(Raw: "0", Label: "Windowed")]),
                        Number(section: "user", key: "screenwidth", hint: "px", help: "Horizontal resolution in pixels. Must be a mode your display adapter supports."),
                        Number(section: "user", key: "screenheight", hint: "px", help: "Vertical resolution in pixels. Must be a mode your display adapter supports."),
                        Number(section: "user", key: "UpdateRate", hint: "1–60 ticks/sec",
                               help: "Game-logic ticks per second. Higher values make play smoother at the cost of CPU time."),
                        Number(section: "user", key: "DrawRate", hint: "15–120 FPS", help: "Frame-rate cap. Lower values reduce the load on the graphics card.")
                    ])
            ]),
        new(Section: "UI",
            Descriptor: "Audio, gameplay, and interface",
            Footnote: null,
            [
                new(SubHeader: "Audio",
                    [
                        Toggle(section: "UI", key: "noMenuMusic", caption: "Play menu music",
                               help: "Play the main-menu music. The INI key is inverted: noMenuMusic=1 silences it.", inverted: true, hint: "Inverted in INI"),
                        Text(section: "UI", key: "menuMusic", hint: "Relative to the game folder",
                             help: "Path to the main-menu music file, relative to the installation folder."),
                        Number(section: "UI", key: "menuMusicAttenuation", hint: "0–10000", help: "Attenuation applied to the menu music. Higher values are quieter."),
                        Number(section: "UI", key: "userAttenuation", hint: "0–10000", help: "Attenuation applied to every game sound. Higher values are quieter."),
                        Toggle(section: "UI", key: "playMovie", caption: "Enabled", help: "Play the first intro movie when the game starts."),
                        Number(section: "UI", key: "movievolume1", hint: "-10000 silent → 0 full",
                               help: "Volume of the first intro movie: 0 is full volume, -10000 is silent."),
                        Toggle(section: "UI", key: "playSecondMovie", caption: "Enabled", help: "Play the second intro movie when the game starts."),
                        Number(section: "UI", key: "movievolume2", hint: "-10000 silent → 0 full",
                               help: "Volume of the second intro movie: 0 is full volume, -10000 is silent.")
                    ]),
                new(SubHeader: "Gameplay (cash)",
                    [
                        Number(section: "UI", key: "MSStartingCash", hint: "0–10,000,000", help: "Cash available at the start of a new game."),
                        Number(section: "UI", key: "MSCashIncrement", hint: "100–1,000,000", help: "Denomination in which cash amounts are awarded."),
                        Number(section: "UI", key: "MSMinCash", hint: "0–10,000,000", help: "Minimum cash the player may hold."),
                        Number(section: "UI", key: "MSMaxCash", hint: "0–10,000,000", help: "Maximum cash the player may hold.")
                    ]),
                new(SubHeader: "Interface",
                    [
                        Toggle(section: "UI", key: "useAlternateCursors", caption: "Monochrome cursors",
                               help: "Use monochrome cursors. Recommended when the cursor flickers or disappears."),
                        Number(section: "UI", key: "tooltipDelay", hint: "0–60 s", help: "Seconds to hover before an in-game tooltip appears."),
                        Number(section: "UI", key: "tooltipDuration", hint: "0–30000 ms", help: "Milliseconds an in-game tooltip stays visible."),
                        Toggle(section: "UI", key: "MessageDisplay", caption: "Show in-game messages", help: "Show in-game notification messages."),
                        Number(section: "UI", key: "mouseScrollThreshold", hint: "0–50 px",
                               help: "Distance from the screen edge, in pixels, at which mouse-edge scrolling starts."),
                        Number(section: "UI", key: "mouseScrollDelay", hint: "0–10", help: "Delay before mouse-edge scrolling begins once the cursor reaches the edge."),
                        Number(section: "UI", key: "mouseScrollX", hint: "1–200", help: "Horizontal mouse-edge scroll speed."),
                        Number(section: "UI", key: "mouseScrollY", hint: "1–200", help: "Vertical mouse-edge scroll speed."),
                        Number(section: "UI", key: "keyScrollX", hint: "1–200", help: "Horizontal keyboard scroll speed."),
                        Number(section: "UI", key: "keyScrollY", hint: "1–200", help: "Vertical keyboard scroll speed."),
                        Number(section: "UI", key: "minimumMessageInterval", hint: "0–3600 s", help: "Minimum seconds between repeated notifications of the same kind."),
                        Choice(section: "UI", key: "helpType", hint: "In-game help",
                               help: "How much in-game help the game shows. Verbose adds extra explanatory text to most controls.",
                               [
                                   new IniChoiceOption(Raw: "0", Label: "Off"),
                                   new IniChoiceOption(Raw: "1", Label: "Standard"),
                                   new IniChoiceOption(Raw: "2", Label: "Verbose")
                               ])
                    ])
            ]),
        new(Section: "advanced",
            Descriptor: "Graphics quality and 8-bit audio",
            Footnote: null,
            [
                new(SubHeader: null,
                    [
                        Choice(section: "advanced", key: "level", hint: "Quality preset",
                               help: "Overall quality preset. Lower values favour quality, higher values favour speed; Paused stops the renderer.",
                               [
                                   new IniChoiceOption(Raw: "0", Label: "0 – Total Quality"),
                                   new IniChoiceOption(Raw: "1", Label: "1 – Quality"),
                                   new IniChoiceOption(Raw: "2", Label: "2 – Balance"),
                                   new IniChoiceOption(Raw: "3", Label: "3 – Speed"),
                                   new IniChoiceOption(Raw: "4", Label: "4 – Paused")
                               ]),
                        Toggle(section: "advanced", key: "loadHalfAnims", caption: "Reduced-detail animations",
                               help: "Load reduced-detail animation sets to help older hardware."),
                        Toggle(section: "advanced", key: "drag", caption: "Drop quality on drag", help: "Lower the rendering quality while objects are being dragged."),
                        Toggle(section: "advanced", key: "click", caption: "Drop quality on click", help: "Lower the rendering quality during click operations."),
                        Toggle(section: "advanced", key: "normal", caption: "Drop quality in normal play", help: "Lower the rendering quality during normal play."),
                        Toggle(section: "advanced", key: "use8BitSound", caption: "Force 8-bit audio", help: "Force 8-bit audio output. Can help on older sound hardware.")
                    ])
            ]),
        new(Section: "ai",
            Descriptor: "AI behaviour limits",
            Footnote: "Raising this above the stock 1,000 limit can make guest pathing thrash on the original engine; values above 2,500 need a community AI patch.",
            [
                new(SubHeader: null,
                    [
                        Number(section: "ai", key: "maxGuests", hint: "1–10000",
                               help: "Maximum number of guests allowed in the zoo at once. Higher values cost more CPU time.")
                    ])
            ]),
        new(Section: "debug",
            Descriptor: "Diagnostic logging and FPS overlay",
            Footnote: null,
            [
                new(SubHeader: null,
                    [
                        Toggle(section: "debug", key: "drawfps", caption: "Show FPS counter", help: "Show a frame-rate counter overlay while playing."),
                        Number(section: "debug", key: "drawfpsx", hint: "0–16384 px", help: "Horizontal position of the frame-rate overlay, in pixels."),
                        Number(section: "debug", key: "drawfpsy", hint: "0–16384 px", help: "Vertical position of the frame-rate overlay, in pixels."),
                        Number(section: "debug", key: "logCutoff", hint: "0 verbose → 5 silent", help: "Logging cut-off. Lower values write more detail."),
                        Toggle(section: "debug", key: "sendLogfile", caption: "Write zoo.log", help: "Write the game's log to zoo.log in the installation folder."),
                        Toggle(section: "debug", key: "sendDebugger", caption: "OutputDebugString", help: "Send the game's log to an attached debugger.")
                    ])
            ]),
        new(Section: "language",
            Descriptor: "Windows LANGID / SUBLANGID",
            Footnote: null,
            [
                new(SubHeader: null,
                    [
                        new IniFieldDescriptor(Id: null, Label: "lang / sublang", IniControlKind.LanguagePicker, Hint: "Curated LANGID + SUBLANGID",
                                               Help: "Game language. Choosing an entry sets both LANGID and SUBLANGID below; other combinations can be entered directly."),
                        Number(section: "language", key: "lang", hint: "Windows LANGID 0–65535", help: "Windows primary language identifier (LANGID)."),
                        Number(section: "language", key: "sublang", hint: "Windows SUBLANGID 0–65535", help: "Windows sub-language identifier (SUBLANGID).")
                    ])
            ]),
        new(Section: "Map",
            Descriptor: "Default zoo dimensions",
            Footnote: "Default dimensions for new zoos. Doubling both values quadruples world memory; large maps may stutter on the original engine.",
            [
                new(SubHeader: null,
                    [
                        Number(section: "Map", key: "mapX", hint: "1–128 tiles wide", help: "Width of new zoos, in tiles."),
                        Number(section: "Map", key: "mapY", hint: "1–128 tiles tall", help: "Height of new zoos, in tiles.")
                    ])
            ])
    ];

    private static IniFieldDescriptor Choice(string section, string key, string hint, string help, IReadOnlyList<IniChoiceOption> options)
        => new(new IniKeyId(section, key), key, IniControlKind.Choice, hint, help)
        {
            Options = options
        };

    private static IniFieldDescriptor Number(string section, string key, string hint, string help) => new(new IniKeyId(section, key), key, IniControlKind.Number, hint, help);

    private static IniFieldDescriptor Text(string section, string key, string hint, string help) => new(new IniKeyId(section, key), key, IniControlKind.Text, hint, help);

    private static IniFieldDescriptor Toggle(string section, string key, string caption, string help, bool inverted = false, string? hint = null)
        => new(new IniKeyId(section, key), key, IniControlKind.Toggle, hint, help)
        {
            Caption  = caption,
            Inverted = inverted
        };
}
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj`
Expected: PASS.

- [ ] **Step 8: Commit**

```powershell
git add Erdmier.ZooTycoonLauncher.slnx Source/Erdmier.ZooTycoonLauncher.Desktop Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit
git commit -m @'
feat(✨): add the INI editor catalogue and the Desktop unit-test project

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 13: Field view models and field views

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/Fields/IniFieldViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/Fields/IniKeyedFieldViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/Fields/IniToggleFieldViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/Fields/IniNumberFieldViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/Fields/IniTextFieldViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/Fields/IniChoiceFieldViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/Fields/IniLanguageFieldViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/IniConfig/Fields/IniToggleFieldView.axaml` (+ `.axaml.cs`), `IniNumberFieldView`, `IniTextFieldView`,
  `IniChoiceFieldView`, `IniLanguageFieldView` (each `.axaml` + `.axaml.cs`)
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`, `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/GlobalUsings.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/IniConfig/Fields/IniFieldViewModelTests.cs`

**Interfaces:**
- Consumes: catalogue types (Task 12); `IniKeySpec`, `ZooIniDefaults` (Tasks 1, 3).
- Produces (Tasks 15–17 rely on these exact members):
  - `abstract partial class IniFieldViewModel : ViewModelBase` — `IniKeyId? Id`, `string Label`, `string? Hint`, `bool HasHint`, `string Help`,
    `[ ObservableProperty ] bool IsHelpActive`, `virtual bool IsDirty`, `virtual void Load(string? raw)`, `virtual void Reset()`,
    `virtual bool TryGetEdit([ NotNullWhen(true) ] out string? raw)`. A change to `IsDirty` is raised as `PropertyChanged(nameof(IsDirty))`.
  - `abstract class IniKeyedFieldViewModel : IniFieldViewModel` — `IniKeySpec Spec`, `string? Baseline`.
  - `IniToggleFieldViewModel` (`Caption`, `Inverted`, `IsChecked`), `IniNumberFieldViewModel` (`decimal? Value`, `decimal Minimum`, `decimal Maximum`),
    `IniTextFieldViewModel` (`string Value`), `IniChoiceFieldViewModel` (`Options`, `IniChoiceOption? SelectedOption`) — each with constructor `(IniFieldDescriptor descriptor)`.
  - `IniLanguageFieldViewModel(IniFieldDescriptor descriptor, IniNumberFieldViewModel language, IniNumberFieldViewModel subLanguage, IReadOnlyList<IniLanguageOption> options)` —
    `Options`, `IniLanguageOption? SelectedOption`.

- [ ] **Step 1: Add the global usings**

Add `global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;` to `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs` (after
`Desktop.ViewModels.Dialogs`) and to `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/GlobalUsings.cs`.

- [ ] **Step 2: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/IniConfig/Fields/IniFieldViewModelTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.IniConfig.Fields;

public sealed class IniFieldViewModelTests
{
    private static IniFieldDescriptor Descriptor(string section, string key)
        => IniEditorCatalogue.Sections.SelectMany(s => s.Groups).SelectMany(g => g.Fields).Single(field => field.Id == new IniKeyId(section, key));

    [ Fact ]
    public void Toggle_TracksDirtyAgainstTheBaseline()
    {
        IniToggleFieldViewModel field = new(Descriptor(section: "advanced", key: "drag"));

        field.Load(raw: "1");

        field.IsChecked.ShouldBeTrue();
        field.IsDirty.ShouldBeFalse();

        field.IsChecked = false;

        field.IsDirty.ShouldBeTrue();
        field.TryGetEdit(out string? edit).ShouldBeTrue();
        edit.ShouldBe(expected: "0");

        field.IsChecked = true;

        field.IsDirty.ShouldBeFalse();
        field.TryGetEdit(out string? _).ShouldBeFalse();
    }

    [ Fact ]
    public void Toggle_Inverted_ShowsTheOppositeAndWritesTheFaithfulValue()
    {
        IniToggleFieldViewModel field = new(Descriptor(section: "UI", key: "noMenuMusic"));

        field.Load(raw: "0");

        field.Caption.ShouldBe(expected: "Play menu music");
        field.IsChecked.ShouldBeTrue();

        field.IsChecked = false;

        field.TryGetEdit(out string? edit).ShouldBeTrue();
        edit.ShouldBe(expected: "1");
    }

    [ Fact ]
    public void Toggle_TrueTextIsEquivalentToOne()
    {
        IniToggleFieldViewModel field = new(Descriptor(section: "advanced", key: "drag"));

        field.Load(raw: "true");

        field.IsChecked.ShouldBeTrue();
        field.IsDirty.ShouldBeFalse();
    }

    [ Fact ]
    public void Number_InvalidBaseline_ShowsTheDefaultAndStaysClean()
    {
        IniNumberFieldViewModel field = new(Descriptor(section: "user", key: "screenwidth"));

        field.Load(raw: "abc");

        field.Value.ShouldBe(expected: 800m);
        field.IsDirty.ShouldBeFalse();
        field.TryGetEdit(out string? _).ShouldBeFalse();
    }

    [ Fact ]
    public void Number_EditsAndBoundsComeFromTheSpec()
    {
        IniNumberFieldViewModel field = new(Descriptor(section: "user", key: "UpdateRate"));

        field.Load(raw: "15");

        field.Minimum.ShouldBe(expected: 1m);
        field.Maximum.ShouldBe(expected: 60m);

        field.Value = 30m;

        field.TryGetEdit(out string? edit).ShouldBeTrue();
        edit.ShouldBe(expected: "30");

        field.Value = 15m;

        field.IsDirty.ShouldBeFalse();
    }

    [ Fact ]
    public void Number_ClearedBox_IsAnEmptyEdit()
    {
        IniNumberFieldViewModel field = new(Descriptor(section: "user", key: "screenwidth"));

        field.Load(raw: "800");

        field.Value = null;

        field.TryGetEdit(out string? edit).ShouldBeTrue();
        edit.ShouldBe(expected: "");
    }

    [ Fact ]
    public void Text_AbsentKey_ShowsTheDefault()
    {
        IniTextFieldViewModel field = new(Descriptor(section: "UI", key: "menuMusic"));

        field.Load(raw: null);

        field.Value.ShouldBe(expected: "sounds/mainmenu.wav");
        field.IsDirty.ShouldBeFalse();
    }

    [ Fact ]
    public void Choice_SelectsTheEquivalentOptionAndEditsItsRaw()
    {
        IniChoiceFieldViewModel field = new(Descriptor(section: "user", key: "fullscreen"));

        field.Load(raw: "true");

        field.SelectedOption!.Label.ShouldBe(expected: "Fullscreen");
        field.IsDirty.ShouldBeFalse();

        field.SelectedOption = field.Options.Single(option => option.Label == "Windowed");

        field.TryGetEdit(out string? edit).ShouldBeTrue();
        edit.ShouldBe(expected: "0");
    }

    [ Fact ]
    public void Choice_Level_ShowsTheLabelledPreset()
    {
        IniChoiceFieldViewModel field = new(Descriptor(section: "advanced", key: "level"));

        field.Load(raw: "3");

        field.SelectedOption!.Label.ShouldBe(expected: "3 – Speed");
    }

    [ Fact ]
    public void Reset_RestoresTheBaseline()
    {
        IniNumberFieldViewModel field = new(Descriptor(section: "Map", key: "mapX"));

        field.Load(raw: "90");

        field.Value = 100m;
        field.Reset();

        field.Value.ShouldBe(expected: 90m);
        field.IsDirty.ShouldBeFalse();
    }

    [ Fact ]
    public void IsDirtyChanges_RaisePropertyChanged()
    {
        IniToggleFieldViewModel field   = new(Descriptor(section: "advanced", key: "drag"));
        List<string?>           changes = [];

        field.Load(raw: "0");

        field.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        field.IsChecked = true;

        changes.ShouldContain(nameof(IniFieldViewModel.IsDirty));
    }

    [ Fact ]
    public void LanguagePicker_DrivesAndFollowsTheTwoNumberRows()
    {
        IniNumberFieldViewModel language    = new(Descriptor(section: "language", key: "lang"));
        IniNumberFieldViewModel subLanguage = new(Descriptor(section: "language", key: "sublang"));

        IniLanguageFieldViewModel picker = new(IniEditorCatalogue.Sections.Single(s => s.Section == "language").Groups[0].Fields[0],
                                               language,
                                               subLanguage,
                                               IniEditorCatalogue.Languages);

        language.Load(raw: "9");
        subLanguage.Load(raw: "1");

        picker.SelectedOption!.Label.ShouldBe(expected: "English (United States)");

        picker.SelectedOption = IniEditorCatalogue.Languages.Single(option => option.Label == "French (France)");

        language.Value.ShouldBe(expected: 12m);
        subLanguage.Value.ShouldBe(expected: 1m);
        language.IsDirty.ShouldBeTrue();

        language.Value = 99m;

        picker.SelectedOption.ShouldBeNull();
        picker.IsDirty.ShouldBeFalse();
        picker.TryGetEdit(out string? _).ShouldBeFalse();
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj --filter "FullyQualifiedName~IniFieldViewModelTests"`
Expected: build FAILS — the field view models do not exist.

- [ ] **Step 4: Implement the two abstract bases**

`Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/Fields/IniFieldViewModel.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;

/// <summary>
///     Base of every INI editor row. Rendered by <c>IniSectionView</c>'s standard row (label + hint on the left, the field's own view on the right); the <c>ViewLocator</c> resolves
///     the concrete field's view. Abstract, so it has no view of its own.
/// </summary>
public abstract partial class IniFieldViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance from its catalogue entry.</summary>
    /// <param name="descriptor">The row's presentation.</param>
    protected IniFieldViewModel(IniFieldDescriptor descriptor)
    {
        Id    = descriptor.Id;
        Label = descriptor.Label;
        Hint  = descriptor.Hint;
        Help  = descriptor.Help;
    }

    /// <summary>The key the row edits, or <see langword="null" /> for a derived row (the language picker).</summary>
    public IniKeyId? Id { get; }

    /// <summary>The row label — the literal INI key.</summary>
    public string Label { get; }

    /// <summary>The muted sub-line, or <see langword="null" />.</summary>
    public string? Hint { get; }

    /// <summary>Whether <see cref="Hint" /> has text.</summary>
    public bool HasHint => !string.IsNullOrEmpty(Hint);

    /// <summary>The help text: the row tooltip and the footer line while the row is hovered or focused.</summary>
    public string Help { get; }

    /// <summary>Whether the row is hovered or holds focus; set by <c>IniSectionView</c>, observed by the editor to drive the footer help line.</summary>
    [ ObservableProperty ]
    public partial bool IsHelpActive { get; set; }

    /// <summary>Whether the row's value differs from what was loaded.</summary>
    public virtual bool IsDirty => false;

    /// <summary>Sets the row's baseline from the loaded raw value and resets the control to it.</summary>
    /// <param name="raw">The raw value from <c>Current</c>, or <see langword="null" /> when the key is absent.</param>
    public virtual void Load(string? raw)
    { }

    /// <summary>Discards the row's edit, returning the control to the baseline.</summary>
    public virtual void Reset()
    { }

    /// <summary>Returns the row's edit as a raw INI value when the row is dirty.</summary>
    /// <param name="raw">The raw value to save.</param>
    /// <returns><see langword="true" /> when there is an edit.</returns>
    public virtual bool TryGetEdit([ NotNullWhen(true) ] out string? raw)
    {
        raw = null;

        return false;
    }
}
```

`Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/Fields/IniKeyedFieldViewModel.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;

/// <summary>
///     Base of every row bound to one registry key. Keeps the loaded raw value as the baseline, shows <see cref="IniKeySpec.EffectiveValue" /> (the default when the stored value
///     is invalid), and is dirty only when the control's value is not <see cref="IniKeySpec.AreEquivalent" /> to that effective baseline. Abstract, so it has no view.
/// </summary>
public abstract class IniKeyedFieldViewModel : IniFieldViewModel
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The row's presentation; its id must be a recognised key.</param>
    /// <exception cref="ArgumentException">The descriptor does not name a recognised key.</exception>
    protected IniKeyedFieldViewModel(IniFieldDescriptor descriptor)
        : base(descriptor)
    {
        if (descriptor.Id is not { } id
            || !ZooIniDefaults.TryGet(id, out IniKeySpec? spec))
        {
            throw new ArgumentException($"The descriptor '{descriptor.Label}' does not name a recognised INI key.", nameof(descriptor));
        }

        Spec = spec;
    }

    /// <summary>The key's registry spec.</summary>
    public IniKeySpec Spec { get; }

    /// <summary>The raw value last loaded, or <see langword="null" /> when the key is absent from the file.</summary>
    public string? Baseline { get; private set; }

    /// <inheritdoc />
    public override bool IsDirty => !Spec.AreEquivalent(CurrentRaw, Spec.EffectiveValue(Baseline));

    /// <summary>The control's current value as INI text, or <see langword="null" /> when the control is empty.</summary>
    protected abstract string? CurrentRaw { get; }

    /// <inheritdoc />
    public override void Load(string? raw)
    {
        Baseline = raw;

        Reset();
    }

    /// <inheritdoc />
    public override void Reset()
    {
        ApplyValue(Spec.EffectiveValue(Baseline));

        NotifyValueChanged();
    }

    /// <inheritdoc />
    public override bool TryGetEdit([ NotNullWhen(true) ] out string? raw)
    {
        if (!IsDirty)
        {
            raw = null;

            return false;
        }

        raw = CurrentRaw ?? string.Empty;

        return true;
    }

    /// <summary>Sets the control from an effective raw value.</summary>
    /// <param name="effectiveValue">The value to show.</param>
    protected abstract void ApplyValue(string? effectiveValue);

    /// <summary>Raises <see cref="IniFieldViewModel.IsDirty" />; derived rows call it whenever their control value changes.</summary>
    protected void NotifyValueChanged() => OnPropertyChanged(nameof(IsDirty));
}
```

- [ ] **Step 5: Implement the concrete fields**

Each concrete field has a designer constructor built from a fixed catalogue entry, then `Load(raw: null)`.

`IniToggleFieldViewModel.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;

/// <summary>A caption check box over a <c>Bool</c> key. <see cref="Inverted" /> rows show the opposite of the stored value.</summary>
public sealed partial class IniToggleFieldViewModel : IniKeyedFieldViewModel
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The row's presentation.</param>
    public IniToggleFieldViewModel(IniFieldDescriptor descriptor)
        : base(descriptor)
    {
        Caption  = descriptor.Caption ?? string.Empty;
        Inverted = descriptor.Inverted;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniToggleFieldViewModel()
        : this(new IniFieldDescriptor(new IniKeyId(Section: "advanced", Key: "loadHalfAnims"), Label: "loadHalfAnims", IniControlKind.Toggle, Hint: null,
                                      Help: "Load reduced-detail animation sets to help older hardware.")
        {
            Caption = "Reduced-detail animations"
        })
        => Load(raw: null);

    /// <summary>The caption beside the check box.</summary>
    public string Caption { get; }

    /// <summary>Whether the check box shows the opposite of the stored value.</summary>
    public bool Inverted { get; }

    /// <summary>The check box state as the user sees it.</summary>
    [ ObservableProperty ]
    public partial bool IsChecked { get; set; }

    /// <inheritdoc />
    protected override string CurrentRaw => IsChecked != Inverted ? "1" : "0";

    /// <inheritdoc />
    protected override void ApplyValue(string? effectiveValue) => IsChecked = IsTrue(effectiveValue) != Inverted;

    partial void OnIsCheckedChanged(bool value) => NotifyValueChanged();

    private static bool IsTrue(string? value)
    {
        string? trimmed = value?.Trim();

        return trimmed == "1" || (bool.TryParse(trimmed, out bool parsed) && parsed);
    }
}
```

`IniNumberFieldViewModel.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;

/// <summary>A <c>NumericUpDown</c> over an integer key, bounded by the key's spec.</summary>
public sealed partial class IniNumberFieldViewModel : IniKeyedFieldViewModel
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The row's presentation.</param>
    public IniNumberFieldViewModel(IniFieldDescriptor descriptor)
        : base(descriptor)
    { }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniNumberFieldViewModel()
        : this(new IniFieldDescriptor(new IniKeyId(Section: "user", Key: "UpdateRate"), Label: "UpdateRate", IniControlKind.Number, Hint: "1–60 ticks/sec",
                                      Help: "Game-logic ticks per second."))
        => Load(raw: null);

    /// <summary>The lower bound (the spec's <c>Min</c>, or <see cref="int.MinValue" />).</summary>
    public decimal Minimum => Spec.Min ?? int.MinValue;

    /// <summary>The upper bound (the spec's <c>Max</c>, or <see cref="int.MaxValue" />).</summary>
    public decimal Maximum => Spec.Max ?? int.MaxValue;

    /// <summary>The number shown; <see langword="null" /> when the box is empty.</summary>
    [ ObservableProperty ]
    public partial decimal? Value { get; set; }

    /// <inheritdoc />
    protected override string? CurrentRaw
        => Value is { } value
               ? decimal.ToInt32(decimal.Truncate(value))
                        .ToString(CultureInfo.InvariantCulture)
               : null;

    /// <inheritdoc />
    protected override void ApplyValue(string? effectiveValue)
        => Value = int.TryParse(effectiveValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : null;

    partial void OnValueChanged(decimal? value) => NotifyValueChanged();
}
```

`IniTextFieldViewModel.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;

/// <summary>A <c>TextBox</c> over a string key.</summary>
public sealed partial class IniTextFieldViewModel : IniKeyedFieldViewModel
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The row's presentation.</param>
    public IniTextFieldViewModel(IniFieldDescriptor descriptor)
        : base(descriptor)
    { }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniTextFieldViewModel()
        : this(new IniFieldDescriptor(new IniKeyId(Section: "UI", Key: "menuMusic"), Label: "menuMusic", IniControlKind.Text, Hint: "Relative to the game folder",
                                      Help: "Path to the main-menu music file."))
        => Load(raw: null);

    /// <summary>The text shown.</summary>
    [ ObservableProperty ]
    public partial string Value { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override string CurrentRaw => Value;

    /// <inheritdoc />
    protected override void ApplyValue(string? effectiveValue) => Value = effectiveValue ?? string.Empty;

    partial void OnValueChanged(string value) => NotifyValueChanged();
}
```

`IniChoiceFieldViewModel.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;

/// <summary>A <c>ComboBox</c> of labelled raw values (<c>fullscreen</c>, <c>level</c>, <c>helpType</c>).</summary>
public sealed partial class IniChoiceFieldViewModel : IniKeyedFieldViewModel
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The row's presentation, including its options.</param>
    public IniChoiceFieldViewModel(IniFieldDescriptor descriptor)
        : base(descriptor)
        => Options = descriptor.Options;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniChoiceFieldViewModel()
        : this(new IniFieldDescriptor(new IniKeyId(Section: "user", Key: "fullscreen"), Label: "fullscreen", IniControlKind.Choice, Hint: "Display mode",
                                      Help: "Run the game full screen or in a window.")
        {
            Options = [new IniChoiceOption(Raw: "1", Label: "Fullscreen"), new IniChoiceOption(Raw: "0", Label: "Windowed")]
        })
        => Load(raw: null);

    /// <summary>The entries offered.</summary>
    public IReadOnlyList<IniChoiceOption> Options { get; }

    /// <summary>The chosen entry; <see langword="null" /> only when no entry matches.</summary>
    [ ObservableProperty ]
    public partial IniChoiceOption? SelectedOption { get; set; }

    /// <inheritdoc />
    protected override string? CurrentRaw => SelectedOption?.Raw;

    /// <inheritdoc />
    protected override void ApplyValue(string? effectiveValue) => SelectedOption = Options.FirstOrDefault(option => Spec.AreEquivalent(option.Raw, effectiveValue));

    partial void OnSelectedOptionChanged(IniChoiceOption? value) => NotifyValueChanged();
}
```

`IniLanguageFieldViewModel.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;

/// <summary>
///     The curated language combo. It has no key of its own: choosing an entry writes the <c>lang</c> and <c>sublang</c> rows, and it shows the entry matching their pair (none
///     when the pair is not curated). It is never dirty — change tracking lives on the two number rows.
/// </summary>
public sealed class IniLanguageFieldViewModel : IniFieldViewModel
{
    private readonly IniNumberFieldViewModel _language;

    private readonly IniNumberFieldViewModel _subLanguage;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The row's presentation.</param>
    /// <param name="language">The <c>[language]/lang</c> row.</param>
    /// <param name="subLanguage">The <c>[language]/sublang</c> row.</param>
    /// <param name="options">The curated languages.</param>
    public IniLanguageFieldViewModel(IniFieldDescriptor descriptor, IniNumberFieldViewModel language, IniNumberFieldViewModel subLanguage, IReadOnlyList<IniLanguageOption> options)
        : base(descriptor)
    {
        _language    = language;
        _subLanguage = subLanguage;

        Options = options;

        _language.PropertyChanged    += OnSourcePropertyChanged;
        _subLanguage.PropertyChanged += OnSourcePropertyChanged;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniLanguageFieldViewModel()
        : this(new IniFieldDescriptor(Id: null, Label: "lang / sublang", IniControlKind.LanguagePicker, Hint: "Curated LANGID + SUBLANGID", Help: "Game language."),
               new IniNumberFieldViewModel(new IniFieldDescriptor(IniEditorCatalogue.LanguageId, Label: "lang", IniControlKind.Number, Hint: null, Help: "LANGID")),
               new IniNumberFieldViewModel(new IniFieldDescriptor(IniEditorCatalogue.SubLanguageId, Label: "sublang", IniControlKind.Number, Hint: null, Help: "SUBLANGID")),
               IniEditorCatalogue.Languages)
    { }

    /// <summary>The curated languages.</summary>
    public IReadOnlyList<IniLanguageOption> Options { get; }

    /// <summary>The curated entry matching the two rows, or <see langword="null" />. Setting a non-null entry writes both rows.</summary>
    public IniLanguageOption? SelectedOption
    {
        get => Options.FirstOrDefault(option => _language.Value == option.Lang && _subLanguage.Value == option.SubLang);
        set
        {
            if (value is null)
            {
                return;
            }

            _language.Value    = value.Lang;
            _subLanguage.Value = value.SubLang;
        }
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(IniNumberFieldViewModel.Value))
        {
            OnPropertyChanged(nameof(SelectedOption));
        }
    }
}
```

- [ ] **Step 6: Implement the field views**

Each view is a `UserControl` holding only its input. Code-behind pattern (repeat for all five, changing the class name and the `<see cref>`):

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig.Fields;

/// <summary>View for <see cref="IniToggleFieldViewModel" />: a caption check box.</summary>
public sealed partial class IniToggleFieldView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public IniToggleFieldView() => AvaloniaXamlLoader.Load(this);
}
```

`IniToggleFieldView.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig.Fields.IniToggleFieldView"
             x:DataType="vm:IniToggleFieldViewModel">
    <CheckBox Content="{Binding Caption}"
              IsChecked="{Binding IsChecked}" />
</UserControl>
```

`IniNumberFieldView.axaml` (code-behind summary: "a bounded integer spinner"):

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig.Fields.IniNumberFieldView"
             x:DataType="vm:IniNumberFieldViewModel">
    <NumericUpDown FormatString="0"
                   HorizontalAlignment="Left"
                   Increment="1"
                   Maximum="{Binding Maximum}"
                   Minimum="{Binding Minimum}"
                   ParsingNumberStyle="Integer"
                   Value="{Binding Value}"
                   Width="120" />
</UserControl>
```

`IniTextFieldView.axaml` (code-behind summary: "a single-line text box"):

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig.Fields.IniTextFieldView"
             x:DataType="vm:IniTextFieldViewModel">
    <TextBox MaxWidth="300"
             Text="{Binding Value}" />
</UserControl>
```

`IniChoiceFieldView.axaml` (code-behind summary: "a combo of labelled values"):

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields"
             xmlns:models="using:Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig.Fields.IniChoiceFieldView"
             x:DataType="vm:IniChoiceFieldViewModel">
    <ComboBox ItemsSource="{Binding Options}"
              MaxWidth="260"
              MinWidth="160"
              SelectedItem="{Binding SelectedOption}">
        <ComboBox.ItemTemplate>
            <DataTemplate x:DataType="models:IniChoiceOption">
                <TextBlock Text="{Binding Label}" />
            </DataTemplate>
        </ComboBox.ItemTemplate>
    </ComboBox>
</UserControl>
```

`IniLanguageFieldView.axaml` (code-behind summary: "the curated language combo"):

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields"
             xmlns:models="using:Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig.Fields.IniLanguageFieldView"
             x:DataType="vm:IniLanguageFieldViewModel">
    <ComboBox ItemsSource="{Binding Options}"
              MaxWidth="260"
              MinWidth="200"
              SelectedItem="{Binding SelectedOption}">
        <ComboBox.ItemTemplate>
            <DataTemplate x:DataType="models:IniLanguageOption">
                <TextBlock Text="{Binding Label}" />
            </DataTemplate>
        </ComboBox.ItemTemplate>
    </ComboBox>
</UserControl>
```

- [ ] **Step 7: Run the tests and build the Desktop project**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj`
Expected: PASS (the build also compiles the five new XAML files — a compiled-binding error fails here).

- [ ] **Step 8: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit
git commit -m @'
feat(✨): add the INI editor field view models and views

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 14: Save prompt and error dialogues

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/SaveChangesChoice.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaDialogService.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/SaveChangesPromptViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/ErrorMessageViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/SaveChangesPromptView.axaml` (+ `.axaml.cs`)
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/ErrorMessageView.axaml` (+ `.axaml.cs`)
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/PlayViewModel.cs` (the file-scoped `NoOpDialogService`)
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Dialogs/DialogViewModelTests.cs`

**Interfaces:**
- Produces: `enum SaveChangesChoice { Yes, No, Cancel }`; `IDialogService.ShowSaveChangesPromptAsync() → Task<SaveChangesChoice>`;
  `IDialogService.ShowErrorAsync(string title, string message) → Task`. Tasks 15–18 call both.

- [ ] **Step 1: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Dialogs/DialogViewModelTests.cs`:

```csharp
using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Dialogs;

public sealed class DialogViewModelTests
{
    [ Fact ]
    public void SaveChangesPrompt_AsksAboutZooIni() => new SaveChangesPromptViewModel().Message.ShouldBe(expected: "Do you want to save the changes to zoo.ini?");

    [ Fact ]
    public void ErrorMessage_KeepsTitleAndMessage()
    {
        ErrorMessageViewModel viewModel = new(title: "Cannot Save zoo.ini", message: "Access denied.");

        viewModel.Title.ShouldBe(expected: "Cannot Save zoo.ini");
        viewModel.Message.ShouldBe(expected: "Access denied.");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj --filter "FullyQualifiedName~DialogViewModelTests"`
Expected: build FAILS.

- [ ] **Step 3: Implement the choice, the view models, and the service members**

`Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/SaveChangesChoice.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>The answer to the "Do you want to save the changes to <c>zoo.ini</c>?" prompt.</summary>
public enum SaveChangesChoice
{
    /// <summary>Save, then continue.</summary>
    Yes,

    /// <summary>Discard the changes, then continue.</summary>
    No,

    /// <summary>Stay, keeping the changes.</summary>
    Cancel
}
```

`SaveChangesPromptViewModel.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>The view model for the Win95-style save-changes prompt.</summary>
public sealed class SaveChangesPromptViewModel : ViewModelBase
{
    /// <summary>The question asked.</summary>
    public string Message => "Do you want to save the changes to zoo.ini?";
}
```

`ErrorMessageViewModel.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>The view model for the modal error message box.</summary>
public sealed class ErrorMessageViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="title">The window title.</param>
    /// <param name="message">The message, shown verbatim.</param>
    public ErrorMessageViewModel(string title, string message)
    {
        Title   = title;
        Message = message;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public ErrorMessageViewModel()
        : this(title: "Cannot Save zoo.ini", message: "zoo.ini could not be saved: Access to the path is denied.")
    { }

    /// <summary>The window title.</summary>
    public string Title { get; }

    /// <summary>The message, shown verbatim.</summary>
    public string Message { get; }
}
```

Add to `IDialogService`:

```csharp
    /// <summary>Asks the Win95-style "Do you want to save the changes to <c>zoo.ini</c>?" question (SDD §7.3.2).</summary>
    /// <returns>The answer; closing the prompt from its title bar answers <see cref="SaveChangesChoice.Cancel" />.</returns>
    Task<SaveChangesChoice> ShowSaveChangesPromptAsync();

    /// <summary>Shows a modal error message box owned by the active window.</summary>
    /// <param name="title">The window title.</param>
    /// <param name="message">The message, shown verbatim.</param>
    Task ShowErrorAsync(string title, string message);
```

Add to `AvaloniaDialogService`:

```csharp
    /// <inheritdoc />
    public async Task<SaveChangesChoice> ShowSaveChangesPromptAsync()
    {
        Window? owner = ResolveOwner();

        if (owner is null)
        {
            return SaveChangesChoice.Cancel;
        }

        SaveChangesPromptView view = new()
        {
            DataContext = new SaveChangesPromptViewModel()
        };

        SaveChangesChoice? choice = await view.ShowDialog<SaveChangesChoice?>(owner);

        return choice ?? SaveChangesChoice.Cancel;
    }

    /// <inheritdoc />
    public async Task ShowErrorAsync(string title, string message)
    {
        ErrorMessageView view = new()
        {
            DataContext = new ErrorMessageViewModel(title, message)
        };

        Window? owner = ResolveOwner();

        if (owner is null)
        {
            view.Show();

            return;
        }

        await view.ShowDialog(owner);
    }
```

In `PlayViewModel.cs`, add to the file-scoped `NoOpDialogService`:

```csharp
    public Task<SaveChangesChoice> ShowSaveChangesPromptAsync() => Task.FromResult(SaveChangesChoice.Cancel);

    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
```

- [ ] **Step 4: Implement the two windows**

`SaveChangesPromptView.axaml` — a `ClassicWindow` in the `AddInstallationDialogView` style, with `LaunchErrorView`'s caption-button styles (copy the two `Style` selectors into
`<ClassicWindow.Styles>`):

```xml
<ClassicWindow xmlns="https://github.com/avaloniaui"
               xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
               xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs"
               x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs.SaveChangesPromptView"
               x:DataType="vm:SaveChangesPromptViewModel"
               CanResize="False"
               Icon="/Assets/BrandIcons/ZTL_Title-Icon.png"
               ShowInTaskbar="False"
               SizeToContent="WidthAndHeight"
               Title="Zoo Tycoon Launcher"
               WindowStartupLocation="CenterOwner">
    <ClassicWindow.Styles>
        <Style Selector="CaptionButtons /template/ Button#PART_MinimizeButton">
            <Setter Property="IsVisible"
                    Value="False" />
        </Style>
        <Style Selector="CaptionButtons /template/ Button#PART_RestoreButton">
            <Setter Property="IsVisible"
                    Value="False" />
        </Style>
    </ClassicWindow.Styles>

    <StackPanel Margin="14"
                Spacing="14">
        <DockPanel HorizontalSpacing="10">
            <Image DockPanel.Dock="Left"
                   Height="32"
                   Source="/Assets/Win95Icons/warning32.gif"
                   VerticalAlignment="Center"
                   Width="32" />

            <TextBlock MaxWidth="320"
                       Text="{Binding Message}"
                       TextWrapping="Wrap"
                       VerticalAlignment="Center" />
        </DockPanel>

        <StackPanel HorizontalAlignment="Center"
                    Orientation="Horizontal"
                    Spacing="6">
            <Button Click="OnYesClick"
                    Content="Yes"
                    IsDefault="True"
                    MinWidth="75" />

            <Button Click="OnNoClick"
                    Content="No"
                    MinWidth="75" />

            <Button Click="OnCancelClick"
                    Content="Cancel"
                    IsCancel="True"
                    MinWidth="75" />
        </StackPanel>
    </StackPanel>
</ClassicWindow>
```

`SaveChangesPromptView.axaml.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>The Win95-style save-changes prompt. Closes with the chosen <see cref="SaveChangesChoice" />; closing from the title bar yields no result (treated as Cancel).</summary>
public sealed partial class SaveChangesPromptView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public SaveChangesPromptView() => AvaloniaXamlLoader.Load(this);

    private void OnYesClick(object? sender, RoutedEventArgs e) => Close(SaveChangesChoice.Yes);

    private void OnNoClick(object? sender, RoutedEventArgs e) => Close(SaveChangesChoice.No);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(SaveChangesChoice.Cancel);
}
```

`ErrorMessageView.axaml` — same window chrome and caption styles as above, with `x:DataType="vm:ErrorMessageViewModel"`, `Title="{Binding Title}"`, `Width="420"`,
`SizeToContent="Height"` (instead of `WidthAndHeight`), and this body:

```xml
    <StackPanel Margin="14"
                Spacing="14">
        <DockPanel HorizontalSpacing="10">
            <Image DockPanel.Dock="Left"
                   Height="32"
                   Source="/Assets/Win95Icons/error32.gif"
                   VerticalAlignment="Top"
                   Width="32" />

            <TextBlock Text="{Binding Message}"
                       TextWrapping="Wrap" />
        </DockPanel>

        <Button Click="OnOkClick"
                Content="OK"
                HorizontalAlignment="Center"
                IsDefault="True"
                MinWidth="75" />
    </StackPanel>
```

`ErrorMessageView.axaml.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>A modal Win95-style error message box.</summary>
public sealed partial class ErrorMessageView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public ErrorMessageView() => AvaloniaXamlLoader.Load(this);

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close();
}
```

- [ ] **Step 5: Run the tests and build**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj`
Expected: PASS (and the Desktop project builds, including both new windows).

- [ ] **Step 6: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit
git commit -m @'
feat(✨): add the save-changes prompt and the error message dialogue

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 15: The editor — sections, placeholder, and `IniEditorViewModel`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/IniFieldGroup.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/IniFieldFactory.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/IniSectionViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/IniPlaceholderViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/IniEditorViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/IniConfig/IniSectionView.axaml` (+ `.axaml.cs`)
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/IniConfig/IniPlaceholderView.axaml` (+ `.axaml.cs`)
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/IniConfig/IniEditorView.axaml` (+ `.axaml.cs`)
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`, `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/GlobalUsings.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/IniConfig/IniEditorTestData.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/IniConfig/IniEditorViewModelTests.cs`

**Interfaces:**
- Consumes: Task 12 catalogue, Task 13 fields, Task 14 `IDialogService.ShowErrorAsync`; `GetIniConfigQuery` (Task 10), `SaveIniCommand` (Task 11), `IniConfigResult` (Task 5).
- Produces (Tasks 16–17 rely on these):
  - `sealed class IniFieldGroup(string? SubHeader, IReadOnlyList<IniFieldViewModel> Fields)` with `HasSubHeader`.
  - `static class IniFieldFactory` — `IReadOnlyList<IniSectionViewModel> CreateSections()`, `IReadOnlyList<IniFieldGroup> CreateGroups(IniSectionDescriptor section)`.
  - `IniSectionViewModel(IniSectionDescriptor descriptor, IReadOnlyList<IniFieldGroup> groups)` — `Section`, `Header` (`"[user]"`), `Descriptor`, `Footnote`, `HasFootnote`,
    `Groups`, `IEnumerable<IniFieldViewModel> Fields`, `IsDirty`.
  - `IniPlaceholderViewModel` — `Headline`, `Body`, `HasBody`, `ShowWarningIcon`; factories `Loading()`, `NoIni()`, `Unreadable(string message)`.
  - `IniEditorViewModel(Guid installationId, IMediator mediator, IDialogService dialogs, IniConfigResult initial)` — `Sections`, `SelectedSection`, `IsBusy`,
    `FileLastWriteUtc`, `ActiveHelp`, `HasPendingChanges`, `FooterText`, `IsFooterHelp`, `IsFooterDirty`, `IsFooterSaved`, `SaveCommand`, `RevertCommand`,
    `void Load(IniConfigResult result)`, `void DiscardChanges()`, `Task<bool> TrySaveAsync(CancellationToken cancellationToken)`.

- [ ] **Step 1: Add the global usings**

Add to `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`: `global using Erdmier.ZooTycoonLauncher.Application.IniConfig.Get;` and
`global using Erdmier.ZooTycoonLauncher.Application.IniConfig.Save;` (after `Application.Game.Launch`), and
`global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;` (after `Desktop.ViewModels.Dialogs`). Add to the Desktop.Tests.Unit `GlobalUsings.cs`:

```csharp
global using Erdmier.ZooTycoonLauncher.Application.IniConfig.Get;
global using Erdmier.ZooTycoonLauncher.Application.IniConfig.Save;
global using Erdmier.ZooTycoonLauncher.Desktop.Composition;
global using Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.IniConfig;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;
global using Erdmier.ZooTycoonLauncher.Domain.Installations;

global using ErrorOr;

global using Mediator;
```

- [ ] **Step 2: Write the test helper and the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/IniConfig/IniEditorTestData.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.IniConfig;

internal static class IniEditorTestData
{
    public static readonly DateTime LastWrite = new(year: 2026, month: 5, day: 24, hour: 19, minute: 2, second: 0, DateTimeKind.Utc);

    public static IniConfigResult Result(params (string Section, string Key, string? Value)[] values)
        => new(values.ToDictionary(value => new IniKeyId(value.Section, value.Key), value => value.Value), LastWrite);

    public static T Field<T>(IniEditorViewModel editor, string section, string key)
        where T : IniFieldViewModel
        => editor.Sections.SelectMany(s => s.Fields).OfType<T>().Single(field => field.Id == new IniKeyId(section, key));

    public static void ReturnsForGet(IMediator mediator, ErrorOr<IniConfigResult> result)
        => mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                   .Returns(new ValueTask<ErrorOr<IniConfigResult>>(result));

    public static void ReturnsForSave(IMediator mediator, ErrorOr<IniConfigResult> result)
        => mediator.Send(Arg.Any<SaveIniCommand>(), Arg.Any<CancellationToken>())
                   .Returns(new ValueTask<ErrorOr<IniConfigResult>>(result));

    public static InstallationSummary Installation(bool hasIni = true)
        => new(Guid.CreateVersion7(), Name: "Main", Path: @"C:\ZT", InstallationValidity.From(hasExe: true, hasIni), IsDefault: true, LastWrite, ModifiedUtc: null,
               LastPlayedUtc: null, LastOpenedUtc: null);
}
```

`Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/IniConfig/IniEditorViewModelTests.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.IniConfig;

public sealed class IniEditorViewModelTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private IniEditorViewModel CreateEditor()
        => new(Guid.CreateVersion7(), _mediator, _dialogs, IniEditorTestData.Result(("user", "screenwidth", "800"), ("user", "fullscreen", "1")));

    [ Fact ]
    public void Constructor_BuildsTheSevenSectionsSelectsTheFirstAndLoadsValues()
    {
        IniEditorViewModel editor = CreateEditor();

        editor.Sections.Select(section => section.Header).ShouldBe(["[user]", "[UI]", "[advanced]", "[ai]", "[debug]", "[language]", "[Map]"]);
        editor.SelectedSection.ShouldBeSameAs(editor.Sections[0]);
        editor.Sections.SelectMany(section => section.Fields).Count().ShouldBe(expected: 47);
        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value.ShouldBe(expected: 800m);
        editor.HasPendingChanges.ShouldBeFalse();
        editor.SaveCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public void Footer_PrefersHelpThenDirtyThenSaved()
    {
        IniEditorViewModel      editor = CreateEditor();
        IniNumberFieldViewModel width  = IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth");

        editor.IsFooterSaved.ShouldBeTrue();
        editor.FooterText.ShouldStartWith(expected: "All changes saved · Last write: ");

        width.Value = 1024m;

        editor.IsFooterDirty.ShouldBeTrue();
        editor.FooterText.ShouldBe(expected: "● Unsaved changes");
        editor.SaveCommand.CanExecute(parameter: null).ShouldBeTrue();

        width.IsHelpActive = true;

        editor.IsFooterHelp.ShouldBeTrue();
        editor.FooterText.ShouldBe(width.Help);

        width.IsHelpActive = false;

        editor.FooterText.ShouldBe(expected: "● Unsaved changes");
    }

    [ Fact ]
    public async Task Save_Success_SendsOnlyTheEditsAndResetsTheBaseline()
    {
        IniEditorViewModel editor = CreateEditor();
        SaveIniCommand?    sent   = null;

        _mediator.Send(Arg.Do<SaveIniCommand>(command => sent = command), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(new IniConfigResult(new Dictionary<IniKeyId, string?>
                 {
                     [new IniKeyId(Section: "user", Key: "screenwidth")] = "1024"
                 }, IniEditorTestData.LastWrite.AddDays(1))));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        (await editor.TrySaveAsync(CancellationToken.None)).ShouldBeTrue();

        sent!.Edits.ShouldBe(new Dictionary<IniKeyId, string> { [new IniKeyId(Section: "user", Key: "screenwidth")] = "1024" });
        editor.HasPendingChanges.ShouldBeFalse();
        editor.FileLastWriteUtc.ShouldBe(IniEditorTestData.LastWrite.AddDays(1));
    }

    [ Fact ]
    public async Task Save_ValidationFailure_ShowsTheErrorAndKeepsTheEdits()
    {
        IniEditorViewModel editor = CreateEditor();

        IniEditorTestData.ReturnsForSave(_mediator, Error.Validation(code: "Edits", description: "\"\" is not a valid value for [user]/screenwidth."));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = null;

        (await editor.TrySaveAsync(CancellationToken.None)).ShouldBeFalse();

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Save zoo.ini", message: "\"\" is not a valid value for [user]/screenwidth.");
        editor.HasPendingChanges.ShouldBeTrue();
    }

    [ Fact ]
    public async Task Save_Throws_ShowsTheExceptionMessage()
    {
        IniEditorViewModel editor = CreateEditor();

        _mediator.Send(Arg.Any<SaveIniCommand>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<IniConfigResult>>>(_ => throw new InvalidOperationException(message: "database is locked"));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        (await editor.TrySaveAsync(CancellationToken.None)).ShouldBeFalse();

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Save zoo.ini", message: "database is locked");
        editor.IsBusy.ShouldBeFalse();
    }

    [ Fact ]
    public async Task Revert_ReloadsFromDisk()
    {
        IniEditorViewModel editor = CreateEditor();

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "640")));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        await editor.RevertCommand.ExecuteAsync(parameter: null);

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value.ShouldBe(expected: 640m);
        editor.HasPendingChanges.ShouldBeFalse();
    }

    [ Fact ]
    public void DiscardChanges_ResetsEveryField()
    {
        IniEditorViewModel editor = CreateEditor();

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;
        IniEditorTestData.Field<IniToggleFieldViewModel>(editor, section: "advanced", key: "drag").IsChecked = true;

        editor.DiscardChanges();

        editor.HasPendingChanges.ShouldBeFalse();
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj --filter "FullyQualifiedName~IniEditorViewModelTests"`
Expected: build FAILS — the editor types do not exist.

- [ ] **Step 4: Implement the group, factory, section, and placeholder**

`IniFieldGroup.cs` (a plain model rendered inline by `IniSectionView`, so it is deliberately not a view model and has no view):

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>A run of rows inside a section, optionally under a sub-header. A plain model rendered inline by <c>IniSectionView</c>; not a view model.</summary>
public sealed class IniFieldGroup
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="subHeader">The sub-header, or <see langword="null" />.</param>
    /// <param name="fields">The rows.</param>
    public IniFieldGroup(string? subHeader, IReadOnlyList<IniFieldViewModel> fields)
    {
        SubHeader = subHeader;
        Fields    = fields;
    }

    /// <summary>The sub-header, or <see langword="null" />.</summary>
    public string? SubHeader { get; }

    /// <summary>Whether <see cref="SubHeader" /> has text.</summary>
    public bool HasSubHeader => !string.IsNullOrEmpty(SubHeader);

    /// <summary>The rows, in display order.</summary>
    public IReadOnlyList<IniFieldViewModel> Fields { get; }
}
```

`IniFieldFactory.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>Builds the editor's section view models and their field rows from <see cref="IniEditorCatalogue" />.</summary>
public static class IniFieldFactory
{
    /// <summary>Creates one section view model per catalogue section, in catalogue order.</summary>
    /// <returns>The sections.</returns>
    public static IReadOnlyList<IniSectionViewModel> CreateSections()
        => IniEditorCatalogue.Sections.Select(section => new IniSectionViewModel(section, CreateGroups(section)))
                             .ToList();

    /// <summary>Creates the field rows of one section, wiring the language picker to its two number rows.</summary>
    /// <param name="section">The section's catalogue entry.</param>
    /// <returns>The row groups.</returns>
    public static IReadOnlyList<IniFieldGroup> CreateGroups(IniSectionDescriptor section)
    {
        Dictionary<IniKeyId, IniFieldViewModel> keyed = section.Groups
                                                               .SelectMany(group => group.Fields)
                                                               .Where(descriptor => descriptor.Id is not null)
                                                               .ToDictionary(descriptor => descriptor.Id!.Value, CreateKeyedField);

        return section.Groups
                      .Select(group => new IniFieldGroup(group.SubHeader,
                                                         group.Fields
                                                              .Select(descriptor => descriptor.Id is { } id ? keyed[id] : CreateLanguagePicker(descriptor, keyed))
                                                              .ToList()))
                      .ToList();
    }

    private static IniFieldViewModel CreateKeyedField(IniFieldDescriptor descriptor)
        => descriptor.Control switch
        {
            IniControlKind.Toggle => new IniToggleFieldViewModel(descriptor),
            IniControlKind.Number => new IniNumberFieldViewModel(descriptor),
            IniControlKind.Text   => new IniTextFieldViewModel(descriptor),
            IniControlKind.Choice => new IniChoiceFieldViewModel(descriptor),
            var control           => throw new InvalidOperationException($"'{descriptor.Label}' has control {control}, which needs no key.")
        };

    private static IniLanguageFieldViewModel CreateLanguagePicker(IniFieldDescriptor descriptor, IReadOnlyDictionary<IniKeyId, IniFieldViewModel> keyed)
        => new(descriptor,
               (IniNumberFieldViewModel)keyed[IniEditorCatalogue.LanguageId],
               (IniNumberFieldViewModel)keyed[IniEditorCatalogue.SubLanguageId],
               IniEditorCatalogue.Languages);
}
```

`IniSectionViewModel.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>One INI section in the editor's form pane (SDD §9.2.1 Layer 3 — one generic section pair for every section).</summary>
public sealed class IniSectionViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The section's catalogue entry.</param>
    /// <param name="groups">The section's row groups.</param>
    public IniSectionViewModel(IniSectionDescriptor descriptor, IReadOnlyList<IniFieldGroup> groups)
    {
        Section    = descriptor.Section;
        Descriptor = descriptor.Descriptor;
        Footnote   = descriptor.Footnote;
        Groups     = groups;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniSectionViewModel()
        : this(IniEditorCatalogue.Sections[0], IniFieldFactory.CreateGroups(IniEditorCatalogue.Sections[0]))
    { }

    /// <summary>The section name, in registry casing.</summary>
    public string Section { get; }

    /// <summary>The section as written in the file and the section list, e.g. <c>[user]</c>.</summary>
    public string Header => $"[{Section}]";

    /// <summary>The muted caption above the form pane.</summary>
    public string Descriptor { get; }

    /// <summary>Muted prose under the rows, or <see langword="null" />.</summary>
    public string? Footnote { get; }

    /// <summary>Whether <see cref="Footnote" /> has text.</summary>
    public bool HasFootnote => !string.IsNullOrEmpty(Footnote);

    /// <summary>The row groups.</summary>
    public IReadOnlyList<IniFieldGroup> Groups { get; }

    /// <summary>Every row in the section.</summary>
    public IEnumerable<IniFieldViewModel> Fields => Groups.SelectMany(group => group.Fields);

    /// <summary>Whether any row has an edit.</summary>
    public bool IsDirty => Fields.Any(field => field.IsDirty);
}
```

`IniPlaceholderViewModel.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>The INI Config tab's non-editor sub-state: loading, no INI present, or unreadable INI (SDD §9.3.1).</summary>
public sealed class IniPlaceholderViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="headline">The bold headline.</param>
    /// <param name="body">The muted explanation (may be empty).</param>
    /// <param name="showWarningIcon">Whether to show the warning icon.</param>
    public IniPlaceholderViewModel(string headline, string body, bool showWarningIcon)
    {
        Headline        = headline;
        Body            = body;
        ShowWarningIcon = showWarningIcon;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniPlaceholderViewModel()
        : this(headline: "No INI present", body: "This installation has no zoo.ini on disk, so its settings cannot be edited.", showWarningIcon: true)
    { }

    /// <summary>The bold headline.</summary>
    public string Headline { get; }

    /// <summary>The muted explanation.</summary>
    public string Body { get; }

    /// <summary>Whether <see cref="Body" /> has text.</summary>
    public bool HasBody => Body.Length > 0;

    /// <summary>Whether to show the warning icon.</summary>
    public bool ShowWarningIcon { get; }

    /// <summary>Shown while the first load is in flight.</summary>
    /// <returns>The placeholder.</returns>
    public static IniPlaceholderViewModel Loading() => new(headline: "Loading zoo.ini…", body: string.Empty, showWarningIcon: false);

    /// <summary>Shown when the installation has no <c>zoo.ini</c>.</summary>
    /// <returns>The placeholder.</returns>
    public static IniPlaceholderViewModel NoIni()
        => new(headline: "No INI present", body: "This installation has no zoo.ini on disk, so its settings cannot be edited.", showWarningIcon: true);

    /// <summary>Shown when <c>zoo.ini</c> or its settings history could not be read.</summary>
    /// <param name="message">The error description.</param>
    /// <returns>The placeholder.</returns>
    public static IniPlaceholderViewModel Unreadable(string message) => new(headline: "zoo.ini could not be read", message, showWarningIcon: true);
}
```

- [ ] **Step 5: Implement `IniEditorViewModel`**

`Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/IniConfig/IniEditorViewModel.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>
///     The INI Config editor (SDD §9.3): a section list, the selected section's form, and a footer whose label shows the active row's help, then the unsaved-changes marker, then
///     the saved state. Save sends only the dirty rows; Revert reloads from disk.
/// </summary>
public sealed partial class IniEditorViewModel : ViewModelBase
{
    private const string ErrorTitle = "Cannot Save zoo.ini";

    private readonly IDialogService _dialogs;

    private readonly Guid _installationId;

    private readonly IMediator? _mediator;

    private IniFieldViewModel? _helpField;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installationId">The installation being edited.</param>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">Chrome service for the error dialogue.</param>
    /// <param name="initial">The values to load.</param>
    public IniEditorViewModel(Guid installationId, IMediator mediator, IDialogService dialogs, IniConfigResult initial)
        : this(installationId, dialogs, initial)
        => _mediator = mediator;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniEditorViewModel()
        : this(Guid.Empty, new NoOpDialogService(), new IniConfigResult(new Dictionary<IniKeyId, string?>(), DateTime.UtcNow))
    { }

    // Mediator-less core (the GeneralTabViewModel pattern): the designer leaves _mediator null, which disables Save and Revert.
    private IniEditorViewModel(Guid installationId, IDialogService dialogs, IniConfigResult initial)
    {
        _installationId = installationId;
        _dialogs        = dialogs;

        Sections        = IniFieldFactory.CreateSections();
        SelectedSection = Sections[0];

        foreach (IniFieldViewModel field in AllFields)
        {
            field.PropertyChanged += OnFieldPropertyChanged;
        }

        Load(initial);
    }

    /// <summary>The seven sections, in SDD §9.3 order.</summary>
    public IReadOnlyList<IniSectionViewModel> Sections { get; }

    /// <summary>The section shown in the form pane.</summary>
    [ ObservableProperty ]
    public partial IniSectionViewModel SelectedSection { get; set; }

    /// <summary><see langword="true" /> while a save or reload is in flight; disables the pane and the footer commands.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    [ NotifyCanExecuteChangedFor(nameof(RevertCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary>The file's last-write time (UTC), shown localised in the footer.</summary>
    [ ObservableProperty ]
    public partial DateTime FileLastWriteUtc { get; set; }

    /// <summary>The hovered or focused row's help, or <see langword="null" />.</summary>
    [ ObservableProperty ]
    public partial string? ActiveHelp { get; set; }

    /// <summary>Whether any row has an edit.</summary>
    public bool HasPendingChanges => Sections.Any(section => section.IsDirty);

    /// <summary>The footer label's text.</summary>
    public string FooterText
        => ActiveHelp
           ?? (HasPendingChanges
                   ? "● Unsaved changes"
                   : $"All changes saved · Last write: {FileLastWriteUtc.ToLocalTime().ToString(format: "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}");

    /// <summary>Whether the footer shows help (italic).</summary>
    public bool IsFooterHelp => ActiveHelp is not null;

    /// <summary>Whether the footer shows the unsaved-changes marker (maroon, bold).</summary>
    public bool IsFooterDirty => ActiveHelp is null && HasPendingChanges;

    /// <summary>Whether the footer shows the saved state (muted).</summary>
    public bool IsFooterSaved => ActiveHelp is null && !HasPendingChanges;

    private IEnumerable<IniFieldViewModel> AllFields => Sections.SelectMany(section => section.Fields);

    /// <summary>Loads values into every row (resetting edits) and updates the last-write time.</summary>
    /// <param name="result">The values from <c>GetIniConfigQuery</c> or <c>SaveIniCommand</c>.</param>
    public void Load(IniConfigResult result)
    {
        FileLastWriteUtc = result.FileLastWriteUtc;

        foreach (IniFieldViewModel field in AllFields)
        {
            if (field.Id is { } id)
            {
                field.Load(result.Values.GetValueOrDefault(id));
            }
        }

        RaiseDirtyChanged();
    }

    /// <summary>Discards every edit, returning each row to its baseline.</summary>
    public void DiscardChanges()
    {
        foreach (IniFieldViewModel field in AllFields)
        {
            field.Reset();
        }

        RaiseDirtyChanged();
    }

    /// <summary>Saves the dirty rows. On failure shows the error dialogue and keeps the edits.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when nothing is left unsaved.</returns>
    public async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        if (!HasPendingChanges)
        {
            return true;
        }

        if (_mediator is null)
        {
            return false;
        }

        Dictionary<IniKeyId, string> edits = [];

        foreach (IniFieldViewModel field in AllFields)
        {
            if (field.Id is { } id
                && field.TryGetEdit(out string? raw))
            {
                edits[id] = raw;
            }
        }

        IsBusy = true;

        try
        {
            ErrorOr<IniConfigResult> result = await _mediator.Send(new SaveIniCommand(_installationId, edits), cancellationToken);

            if (result.IsError)
            {
                await _dialogs.ShowErrorAsync(ErrorTitle, result.FirstError.Description);

                return false;
            }

            Load(result.Value);

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _dialogs.ShowErrorAsync(ErrorTitle, ex.Message);

            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnActiveHelpChanged(string? value) => RaiseFooterChanged();

    partial void OnFileLastWriteUtcChanged(DateTime value) => RaiseFooterChanged();

    [ RelayCommand(CanExecute = nameof(CanSaveOrRevert)) ]
    private Task SaveAsync(CancellationToken cancellationToken) => TrySaveAsync(cancellationToken);

    [ RelayCommand(CanExecute = nameof(CanSaveOrRevert)) ]
    private async Task RevertAsync(CancellationToken cancellationToken)
    {
        if (_mediator is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            ErrorOr<IniConfigResult> result = await _mediator.Send(new GetIniConfigQuery(_installationId), cancellationToken);

            if (result.IsError)
            {
                await _dialogs.ShowErrorAsync(title: "Cannot Reload zoo.ini", result.FirstError.Description);

                return;
            }

            Load(result.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _dialogs.ShowErrorAsync(title: "Cannot Reload zoo.ini", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSaveOrRevert() => HasPendingChanges && !IsBusy && _mediator is not null;

    private void OnFieldPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (sender is not IniFieldViewModel field)
        {
            return;
        }

        if (eventArgs.PropertyName == nameof(IniFieldViewModel.IsDirty))
        {
            RaiseDirtyChanged();

            return;
        }

        if (eventArgs.PropertyName != nameof(IniFieldViewModel.IsHelpActive))
        {
            return;
        }

        if (field.IsHelpActive)
        {
            _helpField = field;
            ActiveHelp = field.Help;
        }
        else if (ReferenceEquals(_helpField, field))
        {
            _helpField = null;
            ActiveHelp = null;
        }
    }

    private void RaiseDirtyChanged()
    {
        OnPropertyChanged(nameof(HasPendingChanges));

        RaiseFooterChanged();

        SaveCommand.NotifyCanExecuteChanged();
        RevertCommand.NotifyCanExecuteChanged();
    }

    private void RaiseFooterChanged()
    {
        OnPropertyChanged(nameof(FooterText));
        OnPropertyChanged(nameof(IsFooterHelp));
        OnPropertyChanged(nameof(IsFooterDirty));
        OnPropertyChanged(nameof(IsFooterSaved));
    }
}

file sealed class NoOpDialogService : IDialogService
{
    public void ShowLaunchError(string message)
    { }

    public Task<AddInstallationResult?> ShowAddInstallationAsync(string? prefilledPath) => Task.FromResult<AddInstallationResult?>(result: null);

    public Task<bool> ShowInstallationManagerAsync() => Task.FromResult(false);

    public Task<string?> PickFolderAsync(string? startPath) => Task.FromResult<string?>(result: null);

    public Task<SaveChangesChoice> ShowSaveChangesPromptAsync() => Task.FromResult(SaveChangesChoice.Cancel);

    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
}
```

Note: the constructor calls `Load` (which calls `RaiseDirtyChanged`, which touches `SaveCommand`) — the source-generated command properties are lazily created, so this is safe.

- [ ] **Step 6: Implement the three views**

`IniSectionView.axaml` — the only place the standard INI row is laid out (`conventions.md` §3):

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig"
             xmlns:fields="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig.IniSectionView"
             x:DataType="vm:IniSectionViewModel">
    <StackPanel Spacing="6">
        <StackPanel Orientation="Horizontal"
                    Spacing="6">
            <TextBlock FontFamily="Lucida Console, Courier New, monospace"
                       FontWeight="Bold"
                       Text="{Binding Header}" />

            <TextBlock Foreground="Gray"
                       Text="section of zoo.ini" />
        </StackPanel>

        <Border Background="Silver"
                Height="1" />

        <ItemsControl ItemsSource="{Binding Groups}">
            <ItemsControl.ItemTemplate>
                <DataTemplate x:DataType="vm:IniFieldGroup">
                    <StackPanel Margin="0 4 0 0"
                                Spacing="6">
                        <TextBlock FontFamily="Lucida Console, Courier New, monospace"
                                   FontWeight="Bold"
                                   Foreground="#404040"
                                   IsVisible="{Binding HasSubHeader}"
                                   Text="{Binding SubHeader}" />

                        <ItemsControl ItemsSource="{Binding Fields}">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate x:DataType="fields:IniFieldViewModel">
                                    <Grid Background="Transparent"
                                          ColumnDefinitions="190,*"
                                          GotFocus="OnRowGotFocus"
                                          LostFocus="OnRowLostFocus"
                                          Margin="0 2"
                                          PointerEntered="OnRowPointerEntered"
                                          PointerExited="OnRowPointerExited"
                                          ToolTip.Tip="{Binding Help}">
                                        <StackPanel Grid.Column="0">
                                            <TextBlock FontFamily="Lucida Console, Courier New, monospace"
                                                       Text="{Binding Label}" />

                                            <TextBlock FontFamily="Lucida Console, Courier New, monospace"
                                                       Foreground="Gray"
                                                       IsVisible="{Binding HasHint}"
                                                       Text="{Binding Hint}" />
                                        </StackPanel>

                                        <ContentControl Content="{Binding}"
                                                        Grid.Column="1"
                                                        HorizontalAlignment="Left"
                                                        MinWidth="0"
                                                        VerticalAlignment="Top" />
                                    </Grid>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </StackPanel>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>

        <TextBlock Foreground="Gray"
                   IsVisible="{Binding HasFootnote}"
                   Margin="0 6 0 0"
                   Text="{Binding Footnote}"
                   TextWrapping="Wrap" />
    </StackPanel>
</UserControl>
```

`IniSectionView.axaml.cs`:

```csharp
using Avalonia.Input;

namespace Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig;

/// <summary>
///     View for <see cref="IniSectionViewModel" />. Owns the standard INI row; the row template's handlers mark the row's field as help-active while it is hovered or focused, which
///     drives the editor footer's help line (SDD §9.3.2). Wired once in the template, so no row hand-rolls it.
/// </summary>
public sealed partial class IniSectionView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public IniSectionView() => AvaloniaXamlLoader.Load(this);

    private static void SetHelpActive(object? sender, bool isActive)
    {
        if (sender is Control { DataContext: IniFieldViewModel field })
        {
            field.IsHelpActive = isActive;
        }
    }

    private void OnRowGotFocus(object? sender, GotFocusEventArgs e) => SetHelpActive(sender, isActive: true);

    private void OnRowLostFocus(object? sender, RoutedEventArgs e) => SetHelpActive(sender, isActive: false);

    private void OnRowPointerEntered(object? sender, PointerEventArgs e) => SetHelpActive(sender, isActive: true);

    private void OnRowPointerExited(object? sender, PointerEventArgs e) => SetHelpActive(sender, isActive: false);
}
```

If the XAML compiler rejects event handlers inside the `DataTemplate`, remove the four attributes from the `Grid`, give the inner `ItemsControl` the attribute
`ContainerPrepared="OnRowContainerPrepared"`, and attach the handlers to the container in code-behind (the container's `DataContext` is the field, so `SetHelpActive` works
unchanged). Keep `ToolTip.Tip` on the `Grid`.

```csharp
    private void OnRowContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        e.Container.PointerEntered += OnRowPointerEntered;
        e.Container.PointerExited  += OnRowPointerExited;
        e.Container.GotFocus       += OnRowGotFocus;
        e.Container.LostFocus      += OnRowLostFocus;
    }
```

`IniPlaceholderView.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig"
             xmlns:controls="clr-namespace:Classic.Avalonia.Theme.Controls;assembly=Classic.Avalonia.Theme"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig.IniPlaceholderView"
             x:DataType="vm:IniPlaceholderViewModel">
    <controls:GroupBox Header="INI status"
                       Margin="8"
                       Padding="10 15"
                       VerticalAlignment="Top">
        <DockPanel HorizontalSpacing="10">
            <Image DockPanel.Dock="Left"
                   Height="32"
                   IsVisible="{Binding ShowWarningIcon}"
                   Source="/Assets/Win95Icons/warning32.gif"
                   VerticalAlignment="Top"
                   Width="32" />

            <StackPanel Spacing="4">
                <TextBlock FontWeight="Bold"
                           Text="{Binding Headline}" />

                <TextBlock Foreground="Gray"
                           IsVisible="{Binding HasBody}"
                           Text="{Binding Body}"
                           TextWrapping="Wrap" />
            </StackPanel>
        </DockPanel>
    </controls:GroupBox>
</UserControl>
```

`IniEditorView.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig.IniEditorView"
             x:DataType="vm:IniEditorViewModel">
    <Grid ColumnDefinitions="170,*"
          ColumnSpacing="8"
          Margin="8"
          RowDefinitions="Auto,300,Auto"
          RowSpacing="4">
        <TextBlock Foreground="Gray"
                   Grid.Column="0"
                   Grid.Row="0"
                   Text="Section" />

        <TextBlock Foreground="Gray"
                   Grid.Column="1"
                   Grid.Row="0"
                   Text="{Binding SelectedSection.Descriptor}" />

        <ListBox Grid.Column="0"
                 Grid.Row="1"
                 IsEnabled="{Binding !IsBusy}"
                 ItemsSource="{Binding Sections}"
                 SelectedItem="{Binding SelectedSection}">
            <ListBox.ItemTemplate>
                <DataTemplate x:DataType="vm:IniSectionViewModel">
                    <TextBlock FontFamily="Lucida Console, Courier New, monospace"
                               Text="{Binding Header}" />
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <Border Background="White"
                BorderBrush="Gray"
                BorderThickness="1"
                Grid.Column="1"
                Grid.Row="1"
                IsEnabled="{Binding !IsBusy}">
            <ScrollViewer HorizontalScrollBarVisibility="Disabled">
                <ContentControl Content="{Binding SelectedSection}"
                                Margin="10 8" />
            </ScrollViewer>
        </Border>

        <DockPanel Grid.Column="0"
                   Grid.ColumnSpan="2"
                   Grid.Row="2">
            <StackPanel DockPanel.Dock="Right"
                        Orientation="Horizontal"
                        Spacing="6">
                <Button Content="Undo"
                        IsEnabled="False"
                        MinWidth="75" />

                <Button Content="Restore defaults"
                        IsEnabled="False"
                        MinWidth="75" />

                <Button Command="{Binding SaveCommand}"
                        Content="Save"
                        IsDefault="True"
                        MinWidth="75" />

                <Button Command="{Binding RevertCommand}"
                        Content="Revert"
                        MinWidth="75" />
            </StackPanel>

            <Panel VerticalAlignment="Center">
                <TextBlock FontStyle="Italic"
                           IsVisible="{Binding IsFooterHelp}"
                           Text="{Binding FooterText}"
                           TextTrimming="CharacterEllipsis" />

                <TextBlock FontWeight="Bold"
                           Foreground="Maroon"
                           IsVisible="{Binding IsFooterDirty}"
                           Text="{Binding FooterText}" />

                <TextBlock Foreground="Gray"
                           IsVisible="{Binding IsFooterSaved}"
                           Text="{Binding FooterText}" />
            </Panel>
        </DockPanel>
    </Grid>
</UserControl>
```

Code-behind for `IniPlaceholderView` and `IniEditorView` follows the field-view pattern (`public sealed partial class …View : UserControl` with
`AvaloniaXamlLoader.Load(this)` and a one-line summary).

- [ ] **Step 7: Run the tests and build**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj`
Expected: PASS (the Desktop project, including the three new views, compiles as part of the run).

- [ ] **Step 8: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit
git commit -m @'
feat(✨): add the INI editor with sections, footer help, save and revert

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 16: `IniConfigTabViewModel` — sub-states and activation

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/IniConfigTabViewModel.cs` (replace the skeleton)
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/IniConfigTabView.axaml`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Tabs/IniConfigTabViewModelTests.cs`

**Interfaces:**
- Consumes: Task 15 `IniEditorViewModel`, `IniPlaceholderViewModel`; `GetIniConfigQuery` (Task 10).
- Produces: `IniConfigTabViewModel(InstallationSummary installation, string? iniErrorMessage, IMediator mediator, IDialogService dialogs)` — `ViewModelBase Content`,
  `bool HasPendingChanges` (raised via `PropertyChanged`), `Task ActivateAsync(CancellationToken)`, `Task<bool> SaveAsync(CancellationToken)`, `void DiscardChanges()`.
  Task 17 constructs it from `PlayViewModel`.

- [ ] **Step 1: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Tabs/IniConfigTabViewModelTests.cs`:

```csharp
using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Tabs;

public sealed class IniConfigTabViewModelTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [ Fact ]
    public async Task NoIni_ShowsThePlaceholderAndNeverLoads()
    {
        IniConfigTabViewModel tab = new(IniEditorTestData.Installation(hasIni: false), iniErrorMessage: null, _mediator, _dialogs);

        await tab.ActivateAsync(CancellationToken.None);

        tab.Content.ShouldBeOfType<IniPlaceholderViewModel>().Headline.ShouldBe(expected: "No INI present");

        await _mediator.DidNotReceive().Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task IniErrorMessage_ShowsUnreadableThenRetriesOnActivation()
    {
        IniConfigTabViewModel tab = new(IniEditorTestData.Installation(), iniErrorMessage: "zoo.ini could not be read: locked", _mediator, _dialogs);

        tab.Content.ShouldBeOfType<IniPlaceholderViewModel>().Body.ShouldBe(expected: "zoo.ini could not be read: locked");

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await tab.ActivateAsync(CancellationToken.None);

        tab.Content.ShouldBeOfType<IniEditorViewModel>();
    }

    [ Fact ]
    public async Task Fresh_ShowsLoadingThenTheEditor()
    {
        IniConfigTabViewModel tab = new(IniEditorTestData.Installation(), iniErrorMessage: null, _mediator, _dialogs);

        tab.Content.ShouldBeOfType<IniPlaceholderViewModel>().Headline.ShouldBe(expected: "Loading zoo.ini…");

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await tab.ActivateAsync(CancellationToken.None);

        tab.Content.ShouldBeOfType<IniEditorViewModel>();
    }

    [ Fact ]
    public async Task FailedLoad_ShowsUnreadableWithTheDescription()
    {
        IniConfigTabViewModel tab = new(IniEditorTestData.Installation(), iniErrorMessage: null, _mediator, _dialogs);

        IniEditorTestData.ReturnsForGet(_mediator, Error.Unexpected(code: "Ini.StoreFailed", description: "The installation's settings history could not be opened: x"));

        await tab.ActivateAsync(CancellationToken.None);

        tab.Content.ShouldBeOfType<IniPlaceholderViewModel>().Body.ShouldBe(expected: "The installation's settings history could not be opened: x");
    }

    [ Fact ]
    public async Task ActivationWhileDirty_DoesNotReload()
    {
        IniConfigTabViewModel tab = new(IniEditorTestData.Installation(), iniErrorMessage: null, _mediator, _dialogs);

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await tab.ActivateAsync(CancellationToken.None);

        IniEditorTestData.Field<IniNumberFieldViewModel>((IniEditorViewModel)tab.Content, section: "user", key: "screenwidth").Value = 1024m;

        await tab.ActivateAsync(CancellationToken.None);

        await _mediator.Received(requiredNumberOfCalls: 1).Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>());
        tab.HasPendingChanges.ShouldBeTrue();
    }

    [ Fact ]
    public async Task ConcurrentActivation_LoadsOnceAndCreatesOneEditor()
    {
        IniConfigTabViewModel                        tab     = new(IniEditorTestData.Installation(), iniErrorMessage: null, _mediator, _dialogs);
        TaskCompletionSource<ErrorOr<IniConfigResult>> pending = new();

        _mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(pending.Task));

        Task first  = tab.ActivateAsync(CancellationToken.None);
        Task second = tab.ActivateAsync(CancellationToken.None);

        second.ShouldBeSameAs(first);

        pending.SetResult(IniEditorTestData.Result(("user", "screenwidth", "800")));

        await first;

        await _mediator.Received(requiredNumberOfCalls: 1).Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>());
        tab.Content.ShouldBeOfType<IniEditorViewModel>();
    }

    [ Fact ]
    public async Task HasPendingChanges_IsForwardedWithANotification()
    {
        IniConfigTabViewModel tab     = new(IniEditorTestData.Installation(), iniErrorMessage: null, _mediator, _dialogs);
        List<string?>         changes = [];

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await tab.ActivateAsync(CancellationToken.None);

        tab.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        IniEditorTestData.Field<IniNumberFieldViewModel>((IniEditorViewModel)tab.Content, section: "user", key: "screenwidth").Value = 1024m;

        changes.ShouldContain(nameof(IniConfigTabViewModel.HasPendingChanges));

        tab.DiscardChanges();

        tab.HasPendingChanges.ShouldBeFalse();
    }

    [ Fact ]
    public async Task SaveAsync_WithoutAnEditor_HasNothingToSave()
        => (await new IniConfigTabViewModel(IniEditorTestData.Installation(hasIni: false), iniErrorMessage: null, _mediator, _dialogs).SaveAsync(CancellationToken.None))
           .ShouldBeTrue();
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj --filter "FullyQualifiedName~IniConfigTabViewModelTests"`
Expected: build FAILS — the tab has no such constructor.

- [ ] **Step 3: Implement the tab view model**

Replace `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/IniConfigTabViewModel.cs` with:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;

/// <summary>
///     The INI Config tab (SDD §9.3.1). Hosts either the editor or a placeholder (loading / no INI / unreadable). Each activation reloads from disk unless edits are pending, so the
///     editor reflects anything the game wrote while the launcher stayed open; a failed load is retried on the next activation.
/// </summary>
public sealed partial class IniConfigTabViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;

    private readonly bool _hasIni;

    private readonly Guid _installationId;

    private readonly IMediator? _mediator;

    private Task? _activation;

    private IniEditorViewModel? _editor;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The open installation.</param>
    /// <param name="iniErrorMessage">The boot's INI synchronisation error, or <see langword="null" />.</param>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">Chrome service for the editor's error dialogue.</param>
    public IniConfigTabViewModel(InstallationSummary installation, string? iniErrorMessage, IMediator mediator, IDialogService dialogs)
        : this(installation.Id, installation.Validity.HasIni, dialogs, InitialContent(installation.Validity.HasIni, iniErrorMessage))
        => _mediator = mediator;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniConfigTabViewModel()
        : this(Guid.Empty, hasIni: true, new NoOpDialogService(), new IniEditorViewModel())
    { }

    private IniConfigTabViewModel(Guid installationId, bool hasIni, IDialogService dialogs, ViewModelBase content)
    {
        _installationId = installationId;
        _hasIni         = hasIni;
        _dialogs        = dialogs;

        Content = content;
    }

    /// <summary>The editor or a placeholder.</summary>
    [ ObservableProperty ]
    public partial ViewModelBase Content { get; set; }

    /// <summary>Whether the editor holds unsaved edits.</summary>
    public bool HasPendingChanges => _editor?.HasPendingChanges ?? false;

    /// <summary>Loads (or reloads) from disk when the tab becomes selected. Skipped when there is no INI or edits are pending; concurrent calls share one load.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The in-flight load, or a completed task when skipped.</returns>
    public Task ActivateAsync(CancellationToken cancellationToken)
    {
        if (!_hasIni
            || _mediator is null
            || HasPendingChanges)
        {
            return Task.CompletedTask;
        }

        if (_activation is { IsCompleted: false })
        {
            return _activation;
        }

        _activation = LoadAsync(_mediator, cancellationToken);

        return _activation;
    }

    /// <summary>Saves the editor's edits (used by the pending-changes guard).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when nothing is left unsaved.</returns>
    public Task<bool> SaveAsync(CancellationToken cancellationToken) => _editor?.TrySaveAsync(cancellationToken) ?? Task.FromResult(true);

    /// <summary>Discards the editor's edits (used by the pending-changes guard).</summary>
    public void DiscardChanges() => _editor?.DiscardChanges();

    private static ViewModelBase InitialContent(bool hasIni, string? iniErrorMessage)
        => !hasIni
               ? IniPlaceholderViewModel.NoIni()
               : iniErrorMessage is not null
                   ? IniPlaceholderViewModel.Unreadable(iniErrorMessage)
                   : IniPlaceholderViewModel.Loading();

    private async Task LoadAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        try
        {
            ErrorOr<IniConfigResult> result = await mediator.Send(new GetIniConfigQuery(_installationId), cancellationToken);

            if (result.IsError)
            {
                Content = IniPlaceholderViewModel.Unreadable(result.FirstError.Description);

                return;
            }

            if (_editor is null)
            {
                _editor = new IniEditorViewModel(_installationId, mediator, _dialogs, result.Value);

                _editor.PropertyChanged += OnEditorPropertyChanged;
            }
            else if (!_editor.HasPendingChanges)
            {
                // The user may have started editing while the reload was in flight; never overwrite those edits.
                _editor.Load(result.Value);
            }

            Content = _editor;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Content = IniPlaceholderViewModel.Unreadable(ex.Message);
        }
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(IniEditorViewModel.HasPendingChanges))
        {
            OnPropertyChanged(nameof(HasPendingChanges));
        }
    }
}

file sealed class NoOpDialogService : IDialogService
{
    public void ShowLaunchError(string message)
    { }

    public Task<AddInstallationResult?> ShowAddInstallationAsync(string? prefilledPath) => Task.FromResult<AddInstallationResult?>(result: null);

    public Task<bool> ShowInstallationManagerAsync() => Task.FromResult(false);

    public Task<string?> PickFolderAsync(string? startPath) => Task.FromResult<string?>(result: null);

    public Task<SaveChangesChoice> ShowSaveChangesPromptAsync() => Task.FromResult(SaveChangesChoice.Cancel);

    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
}
```

- [ ] **Step 4: Update the view**

Replace the body of `IniConfigTabView.axaml` (keep the root element and its attributes) with:

```xml
    <ContentControl Content="{Binding Content}" />
```

- [ ] **Step 5: Run the tests**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj`
Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit
git commit -m @'
feat(✨): host the INI editor and its placeholders in the INI Config tab

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 17: Pending-changes guard on `PlayViewModel`, launch lock, and INI errors on the General tab

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Common/IPendingChangesGuard.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/PlayViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Tabs/GeneralTabViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/PlayView.axaml`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Tabs/GeneralTabView.axaml`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Boot/PlayViewModelTests.cs`

**Interfaces:**
- Consumes: Task 14 `ShowSaveChangesPromptAsync`; Task 16 tab members.
- Produces: `interface IPendingChangesGuard { bool HasPendingChanges { get; } Task<bool> ConfirmLeaveAsync(CancellationToken cancellationToken); }`; a `PlayViewModel`
  that implements it, gains `int SelectedTabIndex`, and has this constructor (Task 18 uses the guard):

  ```csharp
  PlayViewModel(InstallationSummary installation, bool canPlay, Func<CancellationToken, Task> rebootAsync, IApplicationLifecycle lifecycle, IDialogService dialogs,
                IMediator mediator, string? iniErrorMessage = null)
  ```

  `GeneralTabViewModel` gains `HasPendingIniChanges`, `IniErrorMessage`, `IsIniUnreadable` and a fourth constructor parameter `string? iniErrorMessage = null`.

- [ ] **Step 1: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Boot/PlayViewModelTests.cs`:

```csharp
using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Boot;

public sealed class PlayViewModelTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [ Fact ]
    public async Task ConfirmLeave_WithoutChanges_ProceedsWithoutPrompting()
    {
        PlayViewModel play = Create();

        (await play.ConfirmLeaveAsync(CancellationToken.None)).ShouldBeTrue();

        await _dialogs.DidNotReceive().ShowSaveChangesPromptAsync();
    }

    [ Fact ]
    public async Task PendingChanges_LockLaunch()
    {
        PlayViewModel play = await CreateWithPendingEditAsync();

        play.HasPendingChanges.ShouldBeTrue();
        play.GeneralTab.HasPendingIniChanges.ShouldBeTrue();
        play.GeneralTab.LaunchCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task Yes_SaveSucceeds_Proceeds()
    {
        PlayViewModel play = await CreateWithPendingEditAsync();

        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.Yes);

        IniEditorTestData.ReturnsForSave(_mediator, IniEditorTestData.Result(("user", "screenwidth", "1024")));

        (await play.ConfirmLeaveAsync(CancellationToken.None)).ShouldBeTrue();

        play.HasPendingChanges.ShouldBeFalse();
        play.GeneralTab.LaunchCommand.CanExecute(parameter: null).ShouldBeTrue();
    }

    [ Fact ]
    public async Task Yes_SaveFails_Stays()
    {
        PlayViewModel play = await CreateWithPendingEditAsync();

        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.Yes);

        IniEditorTestData.ReturnsForSave(_mediator, Error.Failure(code: "Ini.WriteFailed", description: "zoo.ini could not be saved: denied"));

        (await play.ConfirmLeaveAsync(CancellationToken.None)).ShouldBeFalse();

        play.HasPendingChanges.ShouldBeTrue();
    }

    [ Fact ]
    public async Task No_DiscardsAndProceeds()
    {
        PlayViewModel play = await CreateWithPendingEditAsync();

        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.No);

        (await play.ConfirmLeaveAsync(CancellationToken.None)).ShouldBeTrue();

        play.HasPendingChanges.ShouldBeFalse();
    }

    [ Fact ]
    public async Task Cancel_Stays()
    {
        PlayViewModel play = await CreateWithPendingEditAsync();

        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.Cancel);

        (await play.ConfirmLeaveAsync(CancellationToken.None)).ShouldBeFalse();

        play.HasPendingChanges.ShouldBeTrue();
    }

    [ Fact ]
    public void IniErrorMessage_ReachesBothTabs()
    {
        PlayViewModel play = Create(canPlay: false, iniErrorMessage: "zoo.ini could not be read: locked");

        play.GeneralTab.IsIniUnreadable.ShouldBeTrue();
        play.GeneralTab.IniErrorMessage.ShouldBe(expected: "zoo.ini could not be read: locked");
        play.IniConfigTab.Content.ShouldBeOfType<IniPlaceholderViewModel>().Body.ShouldBe(expected: "zoo.ini could not be read: locked");
    }

    private PlayViewModel Create(bool canPlay = true, string? iniErrorMessage = null)
        => new(IniEditorTestData.Installation(), canPlay, _ => Task.CompletedTask, Substitute.For<IApplicationLifecycle>(), _dialogs, _mediator, iniErrorMessage);

    private async Task<PlayViewModel> CreateWithPendingEditAsync()
    {
        PlayViewModel play = Create();

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await play.IniConfigTab.ActivateAsync(CancellationToken.None);

        IniEditorTestData.Field<IniNumberFieldViewModel>((IniEditorViewModel)play.IniConfigTab.Content, section: "user", key: "screenwidth").Value = 1024m;

        return play;
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj --filter "FullyQualifiedName~PlayViewModelTests"`
Expected: build FAILS — `ConfirmLeaveAsync`, `HasPendingIniChanges`, and the new constructor parameter do not exist.

- [ ] **Step 3: Add the guard interface**

`Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Common/IPendingChangesGuard.cs`:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common;

/// <summary>Implemented by main-window content that can hold unsaved edits, so the window can ask before closing or switching away from it (SDD §7.3.2).</summary>
public interface IPendingChangesGuard
{
    /// <summary>Whether unsaved edits exist.</summary>
    bool HasPendingChanges { get; }

    /// <summary>Asks the user what to do with unsaved edits, acting on the answer.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when the caller may proceed (no edits, saved, or discarded); <see langword="false" /> to stay.</returns>
    Task<bool> ConfirmLeaveAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Update `GeneralTabViewModel`**

1. The public constructor becomes (document the new parameter: "The boot's INI synchronisation error, or <see langword="null" />."):

   ```csharp
   public GeneralTabViewModel(InstallationSummary installation, bool canPlay, IMediator mediator, string? iniErrorMessage = null)
       : this(installation, canPlay, iniErrorMessage)
       => _mediator = mediator;
   ```
2. The designer constructor passes `iniErrorMessage: null` to the private constructor.
3. The private constructor gains `string? iniErrorMessage` and sets `IniErrorMessage = iniErrorMessage;` after `HasIni`.
4. Add these members (next to the other `IsMissing…` flags):

```csharp
    /// <summary>The boot's INI synchronisation error description, or <see langword="null" />.</summary>
    public string? IniErrorMessage { get; }

    /// <summary>
    ///     <see langword="true" /> when both files are present but the installation cannot be played because <c>zoo.ini</c> could not be read; drives the fourth CannotPlay
    ///     message.
    /// </summary>
    public bool IsIniUnreadable => HasExe && HasIni && !CanPlay;

    /// <summary><see langword="true" /> while the INI Config tab holds unsaved edits; disables Launch Game (SDD §7.3.2).</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(LaunchCommand)) ]
    public partial bool HasPendingIniChanges { get; set; }
```

5. `CanExecuteLaunch` becomes `CanPlay && !IsBusy && !HasPendingIniChanges && _mediator is not null`.

In `GeneralTabView.axaml`, after the `IsMissingBothExeAndIni` `TextBlock`, add:

```xml
                    <TextBlock Foreground="Gray"
                               IsVisible="{Binding IsIniUnreadable}"
                               TextWrapping="Wrap">
                        <Run Text="The launcher could not read zoo.ini. " />
                        <Run Text="{Binding IniErrorMessage}" />
                    </TextBlock>

                    <TextBlock Foreground="Gray"
                               IsVisible="{Binding HasPendingIniChanges}"
                               Text="Save or revert your INI changes to launch."
                               TextWrapping="Wrap" />
```

- [ ] **Step 5: Update `PlayViewModel` and `PlayView`**

In `PlayViewModel.cs`:

1. Declare the class `public sealed partial class PlayViewModel : ViewModelBase, IPendingChangesGuard` and extend its summary with: "Implements
   <see cref="IPendingChangesGuard" /> for the INI Config tab's unsaved edits."
2. Add `private const int IniConfigTabIndex = 1;` above the fields.
3. Add the constructor parameter `string? iniErrorMessage = null` (last; document it as "The boot's INI synchronisation error, or <see langword="null" />; shown on both
   tabs."), and construct the tabs and subscriptions as:

```csharp
        GeneralTab   = new GeneralTabViewModel(installation, canPlay, mediator, iniErrorMessage);
        IniConfigTab = new IniConfigTabViewModel(installation, iniErrorMessage, mediator, dialogs);

        GeneralTab.LaunchOutcomeRaised += OnLaunchOutcomeRaised;
        IniConfigTab.PropertyChanged   += OnIniConfigTabPropertyChanged;
```

   (the designer constructor keeps `new IniConfigTabViewModel()` and also subscribes `IniConfigTab.PropertyChanged += OnIniConfigTabPropertyChanged;`).
4. Add the members:

```csharp
    /// <summary>The selected tab (0 = General, 1 = INI Config). Selecting the INI Config tab (re)loads it from disk.</summary>
    [ ObservableProperty ]
    public partial int SelectedTabIndex { get; set; }

    /// <inheritdoc />
    public bool HasPendingChanges => IniConfigTab.HasPendingChanges;

    /// <inheritdoc />
    public async Task<bool> ConfirmLeaveAsync(CancellationToken cancellationToken)
    {
        if (!HasPendingChanges)
        {
            return true;
        }

        SaveChangesChoice choice = await _dialogs.ShowSaveChangesPromptAsync();

        switch (choice)
        {
            case SaveChangesChoice.Yes:
                // A failed save has already shown its error dialogue; stay so the edits are not lost.
                return await IniConfigTab.SaveAsync(cancellationToken);

            case SaveChangesChoice.No:
                IniConfigTab.DiscardChanges();

                return true;

            default:
                return false;
        }
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (value == IniConfigTabIndex)
        {
            _ = ActivateIniConfigTabAsync();
        }
    }

    private async Task ActivateIniConfigTabAsync()
    {
        try
        {
            await IniConfigTab.ActivateAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            // ActivateAsync already turns load failures into its placeholder; this only stops an unexpected fault escaping the fire-and-forget call.
        }
    }

    private void OnIniConfigTabPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName != nameof(IniConfigTabViewModel.HasPendingChanges))
        {
            return;
        }

        GeneralTab.HasPendingIniChanges = IniConfigTab.HasPendingChanges;

        OnPropertyChanged(nameof(HasPendingChanges));
    }
```

In `PlayView.axaml`, change `<TabControl>` to `<TabControl SelectedIndex="{Binding SelectedTabIndex}">`.

- [ ] **Step 6: Run the tests**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj`
Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit
git commit -m @'
feat(✨): guard unsaved INI edits and lock launching while they exist

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 18: Main-window guard wiring

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Boot/FakePendingChangesGuard.cs`
- Test: `Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Boot/MainWindowViewModelGuardTests.cs`

**Interfaces:**
- Consumes: `IPendingChangesGuard`, `PlayViewModel(…, string? iniErrorMessage)` (Task 17); `BootResult.IniErrorMessage` (Task 9).
- Produces: `MainWindowViewModel.IsCloseConfirmed` (get/set), `HasPendingChanges`, `Task<bool> ConfirmCloseAsync()`; `ExitCommand` becomes asynchronous (same name).

- [ ] **Step 1: Write the failing tests**

`Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Boot/FakePendingChangesGuard.cs`:

```csharp
using Erdmier.ZooTycoonLauncher.Desktop.ViewModels;
using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Boot;

internal sealed class FakePendingChangesGuard(bool hasPendingChanges, bool allowLeave) : ViewModelBase, IPendingChangesGuard
{
    public int Prompts { get; private set; }

    public bool HasPendingChanges { get; } = hasPendingChanges;

    public Task<bool> ConfirmLeaveAsync(CancellationToken cancellationToken)
    {
        Prompts++;

        return Task.FromResult(allowLeave);
    }
}
```

`Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Boot/MainWindowViewModelGuardTests.cs`:

```csharp
using CommunityToolkit.Mvvm.Messaging;

using Erdmier.ZooTycoonLauncher.Desktop.ViewModels;

using Microsoft.Extensions.Logging.Abstractions;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Boot;

public sealed class MainWindowViewModelGuardTests
{
    private readonly IApplicationLifecycle _lifecycle = Substitute.For<IApplicationLifecycle>();

    [ Fact ]
    public async Task Exit_GuardDeclines_StaysOpen()
    {
        MainWindowViewModel window = Create();

        window.ActiveContent = new FakePendingChangesGuard(hasPendingChanges: true, allowLeave: false);

        await window.ExitCommand.ExecuteAsync(parameter: null);

        _lifecycle.DidNotReceive().RequestShutdown();
        window.IsCloseConfirmed.ShouldBeFalse();
    }

    [ Fact ]
    public async Task Exit_GuardAllows_ConfirmsTheCloseAndShutsDown()
    {
        MainWindowViewModel window = Create();

        window.ActiveContent = new FakePendingChangesGuard(hasPendingChanges: true, allowLeave: true);

        await window.ExitCommand.ExecuteAsync(parameter: null);

        _lifecycle.Received(requiredNumberOfCalls: 1).RequestShutdown();
        window.IsCloseConfirmed.ShouldBeTrue();
    }

    [ Fact ]
    public async Task Exit_WithoutAGuard_ShutsDown()
    {
        MainWindowViewModel window = Create();

        await window.ExitCommand.ExecuteAsync(parameter: null);

        _lifecycle.Received(requiredNumberOfCalls: 1).RequestShutdown();
    }

    [ Fact ]
    public void HasPendingChanges_ReflectsTheActiveGuard()
    {
        MainWindowViewModel window = Create();

        window.HasPendingChanges.ShouldBeFalse();

        window.ActiveContent = new FakePendingChangesGuard(hasPendingChanges: true, allowLeave: true);

        window.HasPendingChanges.ShouldBeTrue();
    }

    [ Fact ]
    public async Task OpenInstallation_GuardDeclines_KeepsTheCurrentContent()
    {
        MainWindowViewModel     window = Create();
        FakePendingChangesGuard guard  = new(hasPendingChanges: true, allowLeave: false);

        window.IsBooting     = false;
        window.ActiveContent = guard;

        await window.OpenInstallationPickerCommand.ExecuteAsync(parameter: null);

        window.ActiveContent.ShouldBeSameAs(guard);
        guard.Prompts.ShouldBe(expected: 1);
    }

    [ Fact ]
    public async Task ConfirmClose_DelegatesToTheGuard()
    {
        MainWindowViewModel window = Create();

        window.ActiveContent = new FakePendingChangesGuard(hasPendingChanges: true, allowLeave: false);

        (await window.ConfirmCloseAsync()).ShouldBeFalse();
    }

    private MainWindowViewModel Create()
        => new(Substitute.For<IMediator>(), _lifecycle, Substitute.For<IDialogService>(), new WeakReferenceMessenger(), NullLogger<MainWindowViewModel>.Instance);
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```powershell
dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj --filter "FullyQualifiedName~MainWindowViewModelGuardTests"
```
Expected: build FAILS — `IsCloseConfirmed`, `HasPendingChanges`, `ConfirmCloseAsync` do not exist and `ExitCommand` is synchronous.

- [ ] **Step 3: Update `MainWindowViewModel`**

1. Add after `WindowWidth`:

```csharp
    /// <summary>Set once the user has confirmed leaving unsaved edits, so the window's close handler does not ask a second time.</summary>
    public bool IsCloseConfirmed { get; set; }

    /// <summary>Whether the active content holds unsaved edits.</summary>
    public bool HasPendingChanges => ActiveContent is IPendingChangesGuard { HasPendingChanges: true };

    /// <summary>Asks the active content whether the window may close (SDD §7.3.2). Used by the window's close handler.</summary>
    /// <returns><see langword="true" /> when the window may close.</returns>
    public Task<bool> ConfirmCloseAsync() => ConfirmLeaveActiveContentAsync(CancellationToken.None);
```

2. Replace the Open Installation…, Close Installation, and Exit commands with:

```csharp
    [ RelayCommand(CanExecute = nameof(CanOpenInstallationPicker)) ]
    private async Task OpenInstallationPickerAsync(CancellationToken cancellationToken)
    {
        if (!await ConfirmLeaveActiveContentAsync(cancellationToken))
        {
            return;
        }

        await ShowPickerAsync(cancellationToken);
    }
```

```csharp
    [ RelayCommand(CanExecute = nameof(CanCloseInstallation)) ]
    private async Task CloseInstallationAsync(CancellationToken cancellationToken)
    {
        if (!await ConfirmLeaveActiveContentAsync(cancellationToken))
        {
            return;
        }

        await ShowPickerAsync(cancellationToken);
    }
```

```csharp
    // File → "Exit" (SDD §9.10). Asks about unsaved INI edits first; on approval marks the close as confirmed so MainWindow's close handler does not ask again.
    [ RelayCommand ]
    private async Task ExitAsync(CancellationToken cancellationToken)
    {
        if (!await ConfirmLeaveActiveContentAsync(cancellationToken))
        {
            return;
        }

        IsCloseConfirmed = true;

        _lifecycle.RequestShutdown();
    }

    private Task<bool> ConfirmLeaveActiveContentAsync(CancellationToken cancellationToken)
        => ActiveContent is IPendingChangesGuard guard ? guard.ConfirmLeaveAsync(cancellationToken) : Task.FromResult(true);
```

   Keep each command's existing explanatory comment above it, adding "Asks about unsaved INI edits first." to the Open Installation… and Close Installation comments.
3. In `RouteResult`, pass `iniErrorMessage: result.IniErrorMessage` as the last argument of both `PlayViewModel` constructions.

- [ ] **Step 4: Intercept the window close**

Add to `MainWindow.axaml.cs` (inside the class):

```csharp
    /// <inheritdoc />
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (e.Cancel
            || DataContext is not MainWindowViewModel { IsCloseConfirmed: false, HasPendingChanges: true } viewModel)
        {
            return;
        }

        // Closing is synchronous, so cancel now, ask asynchronously, and close again once the user has answered.
        e.Cancel = true;

        _ = ConfirmAndCloseAsync(viewModel);
    }

    private async Task ConfirmAndCloseAsync(MainWindowViewModel viewModel)
    {
        try
        {
            if (!await viewModel.ConfirmCloseAsync())
            {
                return;
            }

            viewModel.IsCloseConfirmed = true;

            Close();
        }
        catch (Exception)
        {
            // A failed prompt keeps the window open; the unsaved edits are still in memory.
        }
    }
```

`MainWindow.axaml` does not change.

- [ ] **Step 5: Run the tests and the architecture tests**

Run: `dotnet test Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.csproj`
then `dotnet test Tests/Erdmier.ZooTycoonLauncher.Tests.Architecture/Erdmier.ZooTycoonLauncher.Tests.Architecture.csproj`
Expected: PASS (`MainWindow.axaml` stays at 93 lines).

- [ ] **Step 6: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop Tests/Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit
git commit -m @'
feat(✨): ask before closing or switching away from unsaved INI edits

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Task 19: Documentation — SDD revision 1.7, conventions, manual test stubs

**Files:**
- Modify: `docs/superpowers/specs/2026-05-26-zoo-tycoon-launcher-design.md`
- Modify: `docs/user-interface-design/conventions.md`
- Modify: `docs/test-plans/manual-end-to-end-tests.md`

Every edited Markdown paragraph is hard-wrapped at 180 characters. Verify with:

```powershell
$inCode = $false; $n = 0; Get-Content <file> -Encoding UTF8 | ForEach-Object { $n++; if ($_ -match '^\s*```') { $inCode = -not $inCode; return }; if (-not $inCode -and $_ -notmatch '^\s*\|' -and $_.Length -gt 180) { "{0}: {1}" -f $n, $_.Length } }
```

(no output means every prose line fits).

- [ ] **Step 1: Amend the SDD**

Make each change below; keep the SDD's voice and British English.

1. **Document control:** Version `1.7`; Last updated `24 September 2026`.
2. **Revision history:** add a `1.7 | 24 September 2026 | Justinian |` row: "INI Config core slice (spec `2026-09-24-ini-config-core-design.md`). §5.3 replaces the typed
   `ZooIniModel` with a key registry (`IniKeyId`, `IniKeyRole`, `IniKeySpec`, `ZooIniDefaults`); raw values are stored as found and the editor falls back to defaults at
   display time. Drift is tiered (§7.1.3, §7.7, glossary): game-managed and unrecognised changes are adopted silently; only user-setting changes archive `Current`. Save
   (§7.3.2, §8.2) re-reads `zoo.ini` and merges edits onto the on-disk text after reconciling external drift in the same transaction. §8.1 records Latin-1 byte fidelity,
   per-line line endings, and no inline-comment parsing. §4.2 swaps `IIniReader` / `IIniWriter` for `IIniFileStore` / `IIniSnapshotRepository`. §9.2.1–§9.2.2 use one generic
   section view pair under `IniConfig/`. §7.4 / §7.5 defer to §9.3.1 and describe the interim placeholder. §9.3.2 sources help from the Desktop catalogue. §6.3 notes the
   `AddSnapshotIndexes` migration and defers `InstallationMetadata`. §11 adds `Desktop.Tests.Unit`; §14.5 adds the VirtualStore and edits-while-running risks. Drafted by Claude
   during the INI Config core implementation run."
3. **§4.2:** in the interface list, replace `` `IIniReader`, `IIniWriter`, `` with `` `IIniFileStore`, ``.
4. **§5.1:** in the `IniSnapshot` code block, `CapturedUtc { get; init; }` becomes `{ get; set; }`; add a note bullet: "`Current` is updated in place; its `CapturedUtc` records
   the last change."
5. **§5.3:** replace the section body with:

   "The Domain layer ships a single static registry — `ZooIniDefaults` — listing every recognised key as an `IniKeySpec`: an `IniKeyId` (section + key, compared
   case-insensitively), an `IniValueKind`, a factory default in INI text form, optional `Min` / `Max` bounds, and an `IniKeyRole` (`UserSetting` or `GameManaged`). 56 keys
   across `[user]`, `[UI]`, `[advanced]`, `[ai]`, `[debug]`, `[language]`, and `[Map]` are recognised; `[scenario]` joins after the Phase 0 research. Values are never parsed
   into a typed model: they stay raw strings everywhere (file, EAV rows, editor baselines), and `IniKeySpec` supplies the kind-aware rules — `IsValid`, `AreEquivalent`
   (`1` ≡ `true`, `075` ≡ `75`, empty ≡ absent for nullable kinds), and `EffectiveValue` (the raw value when valid, otherwise the default). An out-of-range or unparseable
   value is therefore preserved on disk and in history; the editor shows the default in its place and never rewrites the key unless the user edits it. Adding a key is one
   registry line plus one Desktop catalogue line (`IniEditorCatalogue`), which carries the control, hint, and help text."
6. **§6.3:** after the index list add: "The `Original` / `Current` partial unique indexes and the `(Kind, CapturedUtc DESC)` index arrive in migration `AddSnapshotIndexes`
   (INI Config core slice). `InstallationMetadata` is deferred until a slice reads it."
7. **§7.1.3:** in the sequence diagram, rename the `Reader` participant to `Files as IIniFileStore` (its message becomes `ReadAsync(path)` returning `text + LastWriteUtc`),
   replace `Parse->>Parse: Compare to Current snapshot` and the `alt` block with:

```text
    Parse->>Parse: Parse text, extract recognised values, compare to Current (tiered drift)
    alt User-setting drift
        Parse->>Parse: Copy Current -> new Historical (Trigger=Manual)
        Parse->>Parse: Update changed Current rows (Source=Manual) + StructureBlob
    else Game-managed or unrecognised change only
        Parse->>Parse: Update Current rows + StructureBlob, no archive
    else No difference
        Note over Parse: No DB writes
    end
```

   and add below the diagram: "A database with no `Current` snapshot (a fresh or never-imported installation) is imported instead: `Original` + `Current`, both
   `OriginalImport`."
8. **§7.3.2:** replace the numbered Save steps with:
   "1. Read `zoo.ini` from disk. 2. Open a transaction on the per-installation DB. 3. Reconcile `Current` with the on-disk text (tiered drift, §7.1.3) — external user-setting
   changes are archived as `Manual` first. 4. Drop edits that already match. 5. Copy `Current` into a new `Historical` snapshot (`Trigger = LauncherGui`). 6. Apply the edits
   to the **on-disk** document (`IniDocument.SetValue`) and render it. 7. Write `zoo.ini` via temp file + `File.Move(overwrite: true)`. 8. Update the edited `Current` rows
   (`Source = LauncherGui`) and `Current.StructureBlob`. 9. Commit." Keep the pending-changes bullets above, and add after them: "The prompt is the Win95 *"Do you want to
   save the changes to `zoo.ini`?"* `Yes` / `No` / `Cancel`."
9. **§7.4:** insert at the top: "> **MVP note.** §9.3.1 supersedes this section's button list: the No INI Present box offers `Create zoo.ini from defaults` and
   `Locate existing zoo.ini`; Copy From Another Installation is not in the MVP. Until the INI recovery slice lands, the tab shows a message-only placeholder."
10. **§7.5:** insert at the top: "> **MVP note.** Not materialised (§9.3.1). A read or snapshot-store failure resolves to Cannot Play with the error shown on the General tab
    and in a `zoo.ini could not be read` placeholder on the INI tab; the recovery actions below are future work."
11. **§7.7:** in the "File replaced, DB commit failed" row, "detects drift" becomes "detects user-setting drift"; below the table add: "Drift is tiered (§7.1.3): changed
    user settings archive; game-managed keys (window position, last file, tutorial flags, counters) and unrecognised content are adopted without an archive."
12. **§8.1:** replace the second and third paragraphs ("Folding into `ZooIniModel`…" and "Serialisation: …") with: "The file is read and written as Latin-1, which maps every
    byte to one character and back, so a round trip is byte-identical whatever code page the file uses. Each line keeps its own terminator (CRLF, LF, CR, or none on the last
    line) and a leading UTF-8 byte-order mark is kept as a preamble. Everything after the first `=` is the value; inline comments are not recognised. Recognised values are
    extracted with `ZooIniDefaults.ExtractValues` (first occurrence wins; unrecognised keys stay in the text only). `SetValue` rewrites only an existing key's value span; a
    missing key is inserted after the last key of its section, and a missing section is appended, using the document's dominant line ending."
13. **§8.2:** replace the pseudo-code block with the nine steps from item 8 (as a `text` block), and change the closing `CorruptedSource` paragraph to note it applies to the
    future Restore dialogue.
14. **§9.2.1 Layer 3:** replace the eight-file listing and the paragraph after it with: "`IniEditorView` hosts one generic `IniSectionView` / `IniSectionViewModel` pair for
    every section. The rows come from the Desktop catalogue (`IniEditorCatalogue`) and are field view models (`IniToggleFieldViewModel`, `IniNumberFieldViewModel`,
    `IniTextFieldViewModel`, `IniChoiceFieldViewModel`, `IniLanguageFieldViewModel`), each with its own small view resolved by the `ViewLocator`; `IniSectionView` owns the
    standard row layout. Scenarios will get its own bespoke pair. Pending edits roll up field → section → editor → tab."
15. **§9.2.2:** in the folder tree replace the `IniStates/` and `IniSections/` lines with `├── IniConfig/   IniEditorView, IniSectionView, IniPlaceholderView (+ Fields/)`.
16. **§9.3.1:** under NoIniPresent add: "Until the INI recovery slice lands, this is a message-only placeholder (no buttons)."
17. **§9.3.2:** replace "sourced verbatim from `Resources/IniTooltips.axaml` in the Ref build" with "from the Desktop catalogue (`IniEditorCatalogue`, rewritten from the Ref
    build's tooltips)", and add: "The row's hover and focus events set the field's `IsHelpActive`; the editor footer shows that field's help. `IIniHelpRegistry` and
    `IStatusBarSink` are not used."
18. **§11:** add "### 11.x `Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit`" (renumber the following subsections) with: "View-model logic without an Avalonia runtime: INI
    field view models, the editor (change tracking, footer, save / revert), the INI tab host, the pending-changes guard on `PlayViewModel` and `MainWindowViewModel`, and a
    catalogue-integrity check against `ZooIniDefaults`." In §11.1 replace the `IniKeySpec` factories bullet with "`IniKeySpec` — `IsValid`, `AreEquivalent`, `EffectiveValue`
    per kind; `ZooIniDefaults` — registry integrity; `IniDriftDetector` — tier classification."
19. **§14.5:** add rows 9 and 10: "UAC VirtualStore redirection | Risk (open) | A Program Files install may have the game reading a VirtualStore copy of `zoo.ini`; the launcher
    assumes it can write in place. Candidate future slice." and "Edits saved while ZT1 is running | Risk (accepted) | The game may overwrite them on exit; the next activation
    shows what it wrote, and the launcher's version is archived as `Manual` drift."

- [ ] **Step 2: Update `conventions.md`**

1. §3.3 item 1: tip text comes from `IniFieldDescriptor.Help` in `IniEditorCatalogue` (`Desktop/Models/IniConfig/`), not `IIniHelpRegistry`.
2. §3.3 item 2 and 3: the row template in `IniSectionView` sets the field's `IsHelpActive` on `PointerEntered` / `PointerExited` and `GotFocus` / `LostFocus`; the editor footer
   (not the main-window status bar) shows the help line. Replace the last sentence ("A shared `IniRowBehavior`…") with: "The wiring lives once in `IniSectionView`'s row
   template; rows never hand-roll it."
3. §6: replace the section body with: "The INI editor's footer label carries help: the hovered or focused row's `Help` (italic), otherwise `● Unsaved changes` (maroon, bold),
   otherwise `All changes saved · Last write: <local time>` (muted). There is no `IStatusBarSink` or `IIniHelpRegistry`; the main-window status bar is unaffected (SDD §9.3.2)."
4. §9: delete the sentence "Cell 1 string in italic + `AccentMaroon` while the INI editor has unsaved changes (override from `IniConfigTabViewModel`)." and add "The INI
   editor's own footer shows unsaved-change state (§6)."

- [ ] **Step 3: Stub the manual test sections**

In `docs/test-plans/manual-end-to-end-tests.md`, insert **immediately before** `# Notes & Future Tasks/Goals` (do not touch that section or any existing section) a new
top-level heading `# INI Config Core` with eight stub sections, numbered from 1, titled exactly:

1. `First Import: an installation registered before the INI slice opens with an empty snapshot database`
2. `Edit and Save: one change per control kind, file changes only on the edited lines`
3. `Revert: pending edits discarded and values reloaded from disk`
4. `Game Session Drift: launcher open while ZT1 runs, then the INI tab is re-activated`
5. `Pending-Changes Guard: close, Exit, Close Installation, and Open Installation… with Yes / No / Cancel`
6. `Launch Lock: Launch Game disabled while INI edits are pending`
7. `No INI: zoo.ini renamed away, boot resolves to Cannot Play`
8. `Write Refused: zoo.ini read-only, Save shows the error and keeps the edits`

Each stub section has only these sub-headings and prompt quotes, copied from the file's existing template (sections 1–22), with no filled-in text:

```markdown
### Testing Strategy

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

#### Actual Implementation

#### Alignment

### Shortcomings

### Notes/Thoughts
```

- [ ] **Step 4: Verify the wrap and commit**

Run the 180-character check (above) on all three files; fix any line it reports. Then:

```powershell
git add docs/superpowers/specs/2026-05-26-zoo-tycoon-launcher-design.md docs/user-interface-design/conventions.md docs/test-plans/manual-end-to-end-tests.md
git commit -m @'
docs(📝): amend the SDD to v1.7 for the INI Config core slice

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

## Final verification

- [ ] **Clean, build, and test the whole solution**

```powershell
dotnet clean Erdmier.ZooTycoonLauncher.slnx; dotnet build Erdmier.ZooTycoonLauncher.slnx; dotnet test Erdmier.ZooTycoonLauncher.slnx
```

Expected: the build has zero warnings (warnings are errors) and every test project passes — Domain, Application, Infrastructure, Desktop, Architecture.

- [ ] **Check the spec's coverage**

Walk the spec's §3.1 list and §9 test list; every item maps to a task above. Report anything missed rather than silently adding scope.
