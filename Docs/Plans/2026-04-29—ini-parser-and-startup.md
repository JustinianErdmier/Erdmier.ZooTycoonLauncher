# INI Parser & Startup Flow — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement the file locator, INI parser, launcher-config persistence, partial versioning, and startup orchestration services, plus DI wiring in `App.axaml.cs` and a
status-bar UI in `MainWindow` that exercises the full discover → parse → cache flow end-to-end.

**Architecture:** Stateless services injected via `Microsoft.Extensions.DependencyInjection`. File system access goes through `System.IO.Abstractions.IFileSystem`; registry access
through a hand-rolled `IRegistryReader`. `MainWindowViewModel` owns the in-memory `ZooIniModel` and current paths; `ILauncherConfigService` owns the persisted JSON config. A
dedicated `IStartupService` orchestrates the locate → parse → ensure-backup sequence and is invoked from `MainWindow.Loaded` via `MainWindowViewModel.InitializeAsync`. Round-trip
fidelity for `zoo.ini` is preserved via an `internal IniDocument` field on `ZooIniModel` populated by the parser.

**Tech Stack:** C# 13, .NET 10, Avalonia 11.3 (Classic theme), CommunityToolkit.Mvvm 8.x, Microsoft.Extensions.DependencyInjection 8.x, System.IO.Abstractions 21.x,
System.Text.Json (in-box).

**Reference:** [`2026-04-29—ini-parser-and-startup—design.md`](./2026-04-29—ini-parser-and-startup—design.md)

**Important conventions:**

- No tests in this milestone (per user direction). Code is written for testability (see §7 of the design doc) but the test project is a follow-up task.
- Each task ends with a logical commit point. **Do not commit unless the user explicitly asks** — list the suggested message and pause for approval.
- After every code-producing task, run `mcp__rider__build_solution` to confirm the solution still compiles. Stop and fix on failure.
- Existing class names confirmed: `AiSettings` (not `AISettings`), `UISettings`, `UserSettings`, `AdvancedSettings`, `DebugSettings`, `LanguageSettings`, `MapSettings`,
  `ZooIniModel`. Use these exactly.

---

## Task 1: Add NuGet packages

**Files:**

- Modify: `Source/Launcher/Launcher.csproj`

**Step 1.1: Edit `Launcher.csproj`** — add two `PackageReference` entries inside the existing `<ItemGroup>` that holds package references. Keep alphabetical-ish ordering with the
existing entries.

Add:

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
<PackageReference Include="System.IO.Abstractions" Version="21.0.29" />
```

(If newer compatible versions are available, prefer them; both are stable APIs.)

**Step 1.2: Verify build**

Run `mcp__rider__build_solution`. Expected: success, no errors.

**Step 1.3: Commit point** (do not auto-commit)

Suggested message: `chore: add Microsoft.Extensions.DependencyInjection and System.IO.Abstractions`

---

## Task 2: Add `RawDocument` field to `ZooIniModel`

**Files:**

- Modify: `Source/Launcher/Models/ZooIniModel.cs`

**Step 2.1: Add the internal property**

Insert after the `UnknownKeys` property (preserving the file's existing alphabetical-ish ordering after the linter's pass):

```csharp
/// <summary>
///     Raw line-by-line layout of the source <c> zoo.ini </c> file, used by <see cref="Services.IniParserService" /> to preserve comments, blank lines, and key ordering on
///     round-trip. <see langword="null" /> for models produced by <see cref="Services.IIniParserService.GetDefaults" />.
/// </summary>
internal IniDocument? RawDocument { get; set; }
```

**Step 2.2: Verify build**

Run `mcp__rider__build_solution`. Expected: failure — `IniDocument` is not yet defined. This is fine; the next task adds it. (If you'd prefer a clean build between tasks, swap
Tasks 2 and 3.)

**Step 2.3: Commit point**

Combine with Task 3 — do not commit yet.

---

## Task 3: Create `IniDocument` and line records

**Files:**

- Create: `Source/Launcher/Models/IniDocument.cs`

**Step 3.1: Write the file**

```csharp
using System.Collections.Generic;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Internal raw line-by-line representation of a parsed INI file. Used by <see cref="Services.IniParserService" /> to preserve comments, blank lines, and key ordering on round-trip writes.</summary>
internal sealed class IniDocument
{
    public List<IniLine> Lines { get; } = [];
}

/// <summary>Base type for a single line in an <see cref="IniDocument" />.</summary>
internal abstract record IniLine;

/// <summary>A section header line of the form <c> [SectionName] </c>.</summary>
/// <param name="Name"> The section name without brackets, normalised to its original casing. </param>
/// <param name="RawText"> The original line text, preserved verbatim for round-trip. </param>
internal sealed record IniSectionHeader(string Name, string RawText) : IniLine;

/// <summary>A key-value line of the form <c> Key=Value </c>.</summary>
/// <param name="Section"> The name of the section this key belongs to (without brackets). </param>
/// <param name="Key"> The key name, normalised to its original casing. </param>
/// <param name="Value"> The current value as a string. Updated by <see cref="Services.IniParserService.WriteAsync" /> when a known key's typed value has changed. </param>
/// <param name="RawText"> The original line text. Used as a template when re-emitting modified values so surrounding whitespace and casing are preserved. </param>
internal sealed record IniKeyValue(string Section, string Key, string Value, string RawText) : IniLine
{
    public IniKeyValue WithValue(string newValue) => this with { Value = newValue };
}

/// <summary>A comment line beginning with <c> ; </c>.</summary>
/// <param name="RawText"> The original line text, preserved verbatim. </param>
internal sealed record IniComment(string RawText) : IniLine;

/// <summary>A blank line.</summary>
internal sealed record IniBlank : IniLine;
```

**Step 3.2: Verify build**

Run `mcp__rider__build_solution`. Expected: success.

**Step 3.3: Commit point**

Suggested message: `feat(models): add IniDocument and ZooIniModel.RawDocument for round-trip layout`

---

## Task 4: Create `IniKeySpec` helper

**Files:**

- Create: `Source/Launcher/Models/IniKeySpec.cs`

**Step 4.1: Write the file**

```csharp
using System;
using System.Globalization;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Binds a single INI section + key to a typed property on <see cref="ZooIniModel" />. Used by <see cref="ZooIniDefaults.KnownKeys" /> as the registry of all keys the launcher understands.</summary>
internal sealed class IniKeySpec
{
    private IniKeySpec(string section, string key, Func<ZooIniModel, string> read, Action<ZooIniModel, string> write)
    {
        Section = section;
        Key = key;
        Read = read;
        Write = write;
    }

    /// <summary>Section name (without brackets), e.g. <c> "user" </c> or <c> "UI" </c>. Compared case-insensitively when matching INI lines.</summary>
    public string Section { get; }

    /// <summary>Key name, e.g. <c> "fullscreen" </c>. Compared case-insensitively when matching INI lines.</summary>
    public string Key { get; }

    /// <summary>Reads the current typed value from the model and serialises it to its INI string form.</summary>
    public Func<ZooIniModel, string> Read { get; }

    /// <summary>Parses an INI string value and assigns it to the model. Falls back to the typed property's default on parse failure.</summary>
    public Action<ZooIniModel, string> Write { get; }

    public static IniKeySpec Bool(string section, string key, Func<ZooIniModel, bool> get, Action<ZooIniModel, bool> set)
        => new(section, key,
            read: model => get(model) ? "1" : "0",
            write: (model, raw) => set(model, ParseBool(raw, fallback: get(model))));

    public static IniKeySpec Int(string section, string key, Func<ZooIniModel, int> get, Action<ZooIniModel, int> set, int? min = null, int? max = null)
        => new(section, key,
            read: model => get(model).ToString(CultureInfo.InvariantCulture),
            write: (model, raw) => set(model, ParseInt(raw, fallback: get(model), min, max)));

    public static IniKeySpec NullableInt(string section, string key, Func<ZooIniModel, int?> get, Action<ZooIniModel, int?> set)
        => new(section, key,
            read: model => get(model)?.ToString(CultureInfo.InvariantCulture) ?? "",
            write: (model, raw) => set(model, ParseNullableInt(raw)));

    public static IniKeySpec Str(string section, string key, Func<ZooIniModel, string> get, Action<ZooIniModel, string> set)
        => new(section, key,
            read: get,
            write: (model, raw) => set(model, raw));

    public static IniKeySpec NullableStr(string section, string key, Func<ZooIniModel, string?> get, Action<ZooIniModel, string?> set)
        => new(section, key,
            read: model => get(model) ?? "",
            write: (model, raw) => set(model, string.IsNullOrEmpty(raw) ? null : raw));

    private static bool ParseBool(string raw, bool fallback)
    {
        var trimmed = raw.Trim();
        return trimmed switch
        {
            "0" => false,
            "1" => true,
            _ => bool.TryParse(trimmed, out var parsed) ? parsed : fallback
        };
    }

    private static int ParseInt(string raw, int fallback, int? min, int? max)
    {
        if (!int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return fallback;

        if (min is not null && parsed < min) return fallback;
        if (max is not null && parsed > max) return fallback;
        return parsed;
    }

    private static int? ParseNullableInt(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }
}
```

**Step 4.2: Verify build**

Run `mcp__rider__build_solution`. Expected: success.

**Step 4.3: Commit point**

Hold for Task 5; commit them together.

---

## Task 5: Create `ZooIniDefaults` registry

**Files:**

- Create: `Source/Launcher/Models/ZooIniDefaults.cs`

**Step 5.1: Write the file**

```csharp
using System.Collections.Generic;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Single source of truth for every INI key the launcher understands. Maps each section + key to a typed property on <see cref="ZooIniModel" />.</summary>
internal static class ZooIniDefaults
{
    public static IReadOnlyList<IniKeySpec> KnownKeys { get; } =
    [
        // [user] — display, performance, and runtime state (SDD §9.1, §9.9)
        IniKeySpec.Bool("user", "fullscreen",            m => m.User.Fullscreen,            (m, v) => m.User.Fullscreen = v),
        IniKeySpec.Int ("user", "screenwidth",           m => m.User.ScreenWidth,           (m, v) => m.User.ScreenWidth = v, min: 1),
        IniKeySpec.Int ("user", "screenheight",          m => m.User.ScreenHeight,          (m, v) => m.User.ScreenHeight = v, min: 1),
        IniKeySpec.Int ("user", "UpdateRate",            m => m.User.UpdateRate,            (m, v) => m.User.UpdateRate = v, min: 1, max: 60),
        IniKeySpec.Int ("user", "DrawRate",              m => m.User.DrawRate,              (m, v) => m.User.DrawRate = v, min: 15, max: 120),
        IniKeySpec.NullableStr("user", "lastfile",       m => m.User.LastFile,              (m, v) => m.User.LastFile = v),
        IniKeySpec.Bool("user", "showUserEntityWarning", m => m.User.ShowUserEntityWarning, (m, v) => m.User.ShowUserEntityWarning = v),

        // [UI] — audio (SDD §9.3)
        IniKeySpec.Bool("UI", "noMenuMusic",           m => m.UI.NoMenuMusic,          (m, v) => m.UI.NoMenuMusic = v),
        IniKeySpec.Str ("UI", "menuMusic",             m => m.UI.MenuMusic,            (m, v) => m.UI.MenuMusic = v),
        IniKeySpec.Int ("UI", "menuMusicAttenuation",  m => m.UI.MenuMusicAttenuation, (m, v) => m.UI.MenuMusicAttenuation = v, min: 0, max: 10000),
        IniKeySpec.Int ("UI", "userAttenuation",       m => m.UI.UserAttenuation,      (m, v) => m.UI.UserAttenuation = v, min: 0, max: 10000),
        IniKeySpec.Bool("UI", "playMovie",             m => m.UI.PlayMovie,            (m, v) => m.UI.PlayMovie = v),
        IniKeySpec.Int ("UI", "movievolume1",          m => m.UI.MovieVolume1,         (m, v) => m.UI.MovieVolume1 = v, min: -10000, max: 0),
        IniKeySpec.Bool("UI", "playSecondMovie",       m => m.UI.PlaySecondMovie,      (m, v) => m.UI.PlaySecondMovie = v),
        IniKeySpec.Int ("UI", "movievolume2",          m => m.UI.MovieVolume2,         (m, v) => m.UI.MovieVolume2 = v, min: -10000, max: 0),

        // [UI] — gameplay cash (SDD §9.4)
        IniKeySpec.Int("UI", "MSStartingCash",  m => m.UI.MSStartingCash,  (m, v) => m.UI.MSStartingCash = v, min: 0, max: 10_000_000),
        IniKeySpec.Int("UI", "MSCashIncrement", m => m.UI.MSCashIncrement, (m, v) => m.UI.MSCashIncrement = v, min: 100, max: 1_000_000),
        IniKeySpec.Int("UI", "MSMinCash",       m => m.UI.MSMinCash,       (m, v) => m.UI.MSMinCash = v, min: 0, max: 10_000_000),
        IniKeySpec.Int("UI", "MSMaxCash",       m => m.UI.MSMaxCash,       (m, v) => m.UI.MSMaxCash = v, min: 0, max: 10_000_000),

        // [UI] — interface (SDD §9.5)
        IniKeySpec.Bool("UI", "useAlternateCursors",   m => m.UI.UseAlternateCursors,   (m, v) => m.UI.UseAlternateCursors = v),
        IniKeySpec.Int ("UI", "tooltipDelay",          m => m.UI.TooltipDelay,          (m, v) => m.UI.TooltipDelay = v, min: 0, max: 60),
        IniKeySpec.Int ("UI", "tooltipDuration",       m => m.UI.TooltipDuration,       (m, v) => m.UI.TooltipDuration = v, min: 0, max: 30000),
        IniKeySpec.Bool("UI", "MessageDisplay",        m => m.UI.MessageDisplay,        (m, v) => m.UI.MessageDisplay = v),
        IniKeySpec.Int ("UI", "mouseScrollThreshold",  m => m.UI.MouseScrollThreshold,  (m, v) => m.UI.MouseScrollThreshold = v, min: 0, max: 50),
        IniKeySpec.Int ("UI", "mouseScrollDelay",      m => m.UI.MouseScrollDelay,      (m, v) => m.UI.MouseScrollDelay = v, min: 0, max: 10),
        IniKeySpec.Int ("UI", "mouseScrollX",          m => m.UI.MouseScrollX,          (m, v) => m.UI.MouseScrollX = v, min: 1, max: 200),
        IniKeySpec.Int ("UI", "mouseScrollY",          m => m.UI.MouseScrollY,          (m, v) => m.UI.MouseScrollY = v, min: 1, max: 200),
        IniKeySpec.Int ("UI", "keyScrollX",            m => m.UI.KeyScrollX,            (m, v) => m.UI.KeyScrollX = v, min: 1, max: 200),
        IniKeySpec.Int ("UI", "keyScrollY",            m => m.UI.KeyScrollY,            (m, v) => m.UI.KeyScrollY = v, min: 1, max: 200),
        IniKeySpec.Int ("UI", "minimumMessageInterval",m => m.UI.MinimumMessageInterval,(m, v) => m.UI.MinimumMessageInterval = v, min: 0, max: 3600),
        IniKeySpec.Int ("UI", "helpType",              m => m.UI.HelpType,              (m, v) => m.UI.HelpType = v, min: 0, max: 2),

        // [UI] — runtime state preserved on round-trip (SDD §9.9)
        IniKeySpec.NullableInt("UI", "lastWindowX",                m => m.UI.LastWindowX,                  (m, v) => m.UI.LastWindowX = v),
        IniKeySpec.NullableInt("UI", "lastWindowY",                m => m.UI.LastWindowY,                  (m, v) => m.UI.LastWindowY = v),
        IniKeySpec.Bool       ("UI", "startedFirstTutorial",       m => m.UI.StartedFirstTutorial,         (m, v) => m.UI.StartedFirstTutorial = v),
        IniKeySpec.Bool       ("UI", "startedDinoTutorial",        m => m.UI.StartedDinoTutorial,          (m, v) => m.UI.StartedDinoTutorial = v),
        IniKeySpec.Bool       ("UI", "startedAquaTutorial",        m => m.UI.StartedAquaTutorial,          (m, v) => m.UI.StartedAquaTutorial = v),
        IniKeySpec.NullableInt("UI", "progresscalls",              m => m.UI.ProgressCalls,                (m, v) => m.UI.ProgressCalls = v),
        IniKeySpec.NullableInt("UI", "defaultEditCharLimit",       m => m.UI.DefaultEditCharLimit,         (m, v) => m.UI.DefaultEditCharLimit = v),
        IniKeySpec.NullableInt("UI", "completedExhibitAttenuation",m => m.UI.CompletedExhibitAttenuation,  (m, v) => m.UI.CompletedExhibitAttenuation = v),

        // [advanced] — graphics & 8-bit audio (SDD §9.2, §9.3)
        IniKeySpec.Int ("advanced", "level",         m => m.Advanced.Level,         (m, v) => m.Advanced.Level = v, min: 0, max: 4),
        IniKeySpec.Bool("advanced", "loadHalfAnims", m => m.Advanced.LoadHalfAnims, (m, v) => m.Advanced.LoadHalfAnims = v),
        IniKeySpec.Bool("advanced", "drag",          m => m.Advanced.Drag,          (m, v) => m.Advanced.Drag = v),
        IniKeySpec.Bool("advanced", "click",         m => m.Advanced.Click,         (m, v) => m.Advanced.Click = v),
        IniKeySpec.Bool("advanced", "normal",        m => m.Advanced.Normal,        (m, v) => m.Advanced.Normal = v),
        IniKeySpec.Bool("advanced", "use8BitSound",  m => m.Advanced.Use8BitSound,  (m, v) => m.Advanced.Use8BitSound = v),

        // [ai] (SDD §9.4)
        IniKeySpec.Int("ai", "maxGuests", m => m.AI.MaxGuests, (m, v) => m.AI.MaxGuests = v, min: 1, max: 10000),

        // [debug] (SDD §9.8)
        IniKeySpec.Bool("debug", "drawfps",      m => m.Debug.DrawFps,      (m, v) => m.Debug.DrawFps = v),
        IniKeySpec.Int ("debug", "drawfpsx",     m => m.Debug.DrawFpsX,     (m, v) => m.Debug.DrawFpsX = v, min: 0),
        IniKeySpec.Int ("debug", "drawfpsy",     m => m.Debug.DrawFpsY,     (m, v) => m.Debug.DrawFpsY = v, min: 0),
        IniKeySpec.Int ("debug", "logCutoff",    m => m.Debug.LogCutoff,    (m, v) => m.Debug.LogCutoff = v, min: 0, max: 5),
        IniKeySpec.Bool("debug", "sendLogfile",  m => m.Debug.SendLogfile,  (m, v) => m.Debug.SendLogfile = v),
        IniKeySpec.Bool("debug", "sendDebugger", m => m.Debug.SendDebugger, (m, v) => m.Debug.SendDebugger = v),

        // [language] (SDD §9.7)
        IniKeySpec.Int("language", "lang",    m => m.Language.Lang,    (m, v) => m.Language.Lang = v),
        IniKeySpec.Int("language", "sublang", m => m.Language.SubLang, (m, v) => m.Language.SubLang = v),

        // [Map] (SDD §9.6)
        IniKeySpec.Int("Map", "mapX", m => m.Map.MapX, (m, v) => m.Map.MapX = v, min: 1, max: 128),
        IniKeySpec.Int("Map", "mapY", m => m.Map.MapY, (m, v) => m.Map.MapY = v, min: 1, max: 128)
    ];
}
```

**Step 5.2: Verify build**

Run `mcp__rider__build_solution`. Expected: success.

**Step 5.3: Commit point**

Suggested message: `feat(models): add IniKeySpec and ZooIniDefaults registry of known INI keys`

---

## Task 6: Create `LauncherConfig` and `StartupResult` models

**Files:**

- Create: `Source/Launcher/Models/LauncherConfig.cs`
- Create: `Source/Launcher/Models/StartupResult.cs`

**Step 6.1: Write `LauncherConfig.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Persisted launcher-specific preferences, stored as JSON in <c> %AppData%\ZooTycoonLauncher\launcher.config </c>.</summary>
public sealed class LauncherConfig
{
    /// <summary>The directory containing <c> zoo.exe </c>, persisted across sessions so subsequent launches skip auto-discovery.</summary>
    public string? GameDirectory { get; set; }

    /// <summary>Whether the launcher should minimise to the taskbar when the game is launched. Reserved for a future milestone; currently no UI reads this.</summary>
    public bool MinimiseOnLaunch { get; set; }
}
```

**Step 6.2: Write `StartupResult.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Outcome of <see cref="Services.IStartupService.InitializeAsync" />, capturing the located paths, parsed model, and any user-facing warning.</summary>
public sealed record StartupResult(
    StartupStatus Status,
    string? GameDirectory,
    string? ExePath,
    string? IniPath,
    ZooIniModel? Model,
    LauncherConfig Config,
    string? Warning
);

/// <summary>The category of startup outcome, used by <see cref="ViewModels.MainWindowViewModel" /> to decide which UI affordances to enable.</summary>
public enum StartupStatus
{
    /// <summary>Both <c> zoo.exe </c> and <c> zoo.ini </c> were found and the INI parsed successfully. All UI is enabled.</summary>
    Ready,

    /// <summary>Auto-discovery failed entirely. The user must locate the installation manually.</summary>
    GameDirectoryUnknown,

    /// <summary><c> zoo.exe </c> was located but <c> zoo.ini </c> is missing. Settings tabs are disabled; Launch is enabled.</summary>
    IniMissing,

    /// <summary><c> zoo.ini </c> parsed successfully but <c> zoo.exe </c> is missing. Settings tabs are enabled; Launch is disabled.</summary>
    ExeMissing,

    /// <summary><c> zoo.ini </c> exists but could not be read or parsed. Settings tabs are disabled.</summary>
    IniParseFailed
}
```

**Step 6.3: Verify build**

Run `mcp__rider__build_solution`. Expected: success.

**Step 6.4: Commit point**

Suggested message: `feat(models): add LauncherConfig and StartupResult/StartupStatus`

---

## Task 7: Create `IRegistryReader` and `WindowsRegistryReader`

**Files:**

- Create: `Source/Launcher/Services/IRegistryReader.cs`
- Create: `Source/Launcher/Services/WindowsRegistryReader.cs`

**Step 7.1: Write `IRegistryReader.cs`**

```csharp
namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Thin abstraction over <see cref="Microsoft.Win32.Registry" /> reads, introduced so that <see cref="FileLocatorService" /> remains testable without touching the real registry.</summary>
public interface IRegistryReader
{
    /// <summary>Reads a string value from <c> HKEY_LOCAL_MACHINE </c>, returning <see langword="null" /> if the subkey, value, or registry hive is unavailable.</summary>
    /// <param name="subKeyPath"> Backslash-separated path under <c> HKLM </c>, e.g. <c> "SOFTWARE\\Microsoft Games\\Zoo Tycoon\\1.0" </c>. </param>
    /// <param name="valueName"> Name of the value to read. </param>
    string? ReadHklmString(string subKeyPath, string valueName);
}
```

**Step 7.2: Write `WindowsRegistryReader.cs`**

```csharp
using System;

using Microsoft.Win32;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Production implementation of <see cref="IRegistryReader" /> backed by <see cref="Microsoft.Win32.Registry" />. Available only on Windows.</summary>
public sealed class WindowsRegistryReader : IRegistryReader
{
    /// <inheritdoc />
    public string? ReadHklmString(string subKeyPath, string valueName)
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(subKeyPath);
            return key?.GetValue(valueName) as string;
        }
        catch (Exception)
        {
            // Treat any registry access failure as "value absent". Caller will fall through to the next discovery strategy.
            return null;
        }
    }
}
```

**Step 7.3: Verify build**

Run `mcp__rider__build_solution`. Expected: success. (`Microsoft.Win32.Registry` ships in-box on Windows targets; no extra package needed.)

**Step 7.4: Commit point**

Suggested message: `feat(services): add IRegistryReader and WindowsRegistryReader`

---

## Task 8: Create `LauncherConfigService`

**Files:**

- Create: `Source/Launcher/Services/ILauncherConfigService.cs`
- Create: `Source/Launcher/Services/LauncherConfigService.cs`

**Step 8.1: Write `ILauncherConfigService.cs`**

```csharp
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Loads and saves the launcher's persisted JSON configuration in <c> %AppData%\ZooTycoonLauncher\launcher.config </c>.</summary>
public interface ILauncherConfigService
{
    /// <summary>Path to the JSON config file on disk. Stable for the lifetime of the service.</summary>
    string ConfigFilePath { get; }

    /// <summary>Reads the config from disk. Returns a fresh <see cref="LauncherConfig" /> with default values if the file does not exist, is empty, or fails to parse.</summary>
    Task<LauncherConfig> LoadAsync();

    /// <summary>Writes the config to disk atomically (temp file + rename). Creates the parent directory if necessary.</summary>
    Task SaveAsync(LauncherConfig config);
}
```

**Step 8.2: Write `LauncherConfigService.cs`**

```csharp
using System;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="ILauncherConfigService" />
public sealed class LauncherConfigService : ILauncherConfigService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IFileSystem _fileSystem;

    /// <param name="fileSystem"> Abstraction over the file system; the production wiring binds this to <see cref="FileSystem" />. </param>
    /// <param name="appDataRoot"> Root directory under which <c> ZooTycoonLauncher\launcher.config </c> is stored; the production wiring binds this to <see cref="Environment.GetFolderPath(Environment.SpecialFolder)" /> with <see cref="Environment.SpecialFolder.ApplicationData" />. </param>
    public LauncherConfigService(IFileSystem fileSystem, string appDataRoot)
    {
        _fileSystem = fileSystem;
        ConfigFilePath = _fileSystem.Path.Combine(appDataRoot, "ZooTycoonLauncher", "launcher.config");
    }

    /// <inheritdoc />
    public string ConfigFilePath { get; }

    /// <inheritdoc />
    public async Task<LauncherConfig> LoadAsync()
    {
        if (!_fileSystem.File.Exists(ConfigFilePath))
            return new LauncherConfig();

        try
        {
            await using var stream = _fileSystem.File.OpenRead(ConfigFilePath);
            if (stream.Length == 0) return new LauncherConfig();

            var config = await JsonSerializer.DeserializeAsync<LauncherConfig>(stream, SerializerOptions);
            return config ?? new LauncherConfig();
        }
        catch (Exception)
        {
            // Corrupt or unreadable file: treat as first run rather than throwing. The next SaveAsync overwrites it.
            return new LauncherConfig();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(LauncherConfig config)
    {
        var directory = _fileSystem.Path.GetDirectoryName(ConfigFilePath)!;
        _fileSystem.Directory.CreateDirectory(directory);

        var tempPath = ConfigFilePath + ".tmp";
        await using (var stream = _fileSystem.File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, config, SerializerOptions);
        }

        _fileSystem.File.Move(tempPath, ConfigFilePath, overwrite: true);
    }
}
```

**Step 8.3: Verify build**

Run `mcp__rider__build_solution`. Expected: success.

**Step 8.4: Commit point**

Suggested message: `feat(services): add LauncherConfigService for persisted JSON config`

---

## Task 9: Create `FileLocatorService`

**Files:**

- Create: `Source/Launcher/Services/IFileLocatorService.cs`
- Create: `Source/Launcher/Services/FileLocatorService.cs`

**Step 9.1: Write `IFileLocatorService.cs`** (per SDD §6.1)

```csharp
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Locates <c> zoo.exe </c> and <c> zoo.ini </c> on the host file system.</summary>
public interface IFileLocatorService
{
    /// <summary>Attempts to locate <c> zoo.exe </c> and <c> zoo.ini </c> automatically by checking the persisted launcher config, common installation paths, and the Windows Registry, in that order.</summary>
    Task<LocatorResult> LocateFilesAsync();

    /// <summary>Verifies that the supplied directory contains <c> zoo.exe </c> (and optionally <c> zoo.ini </c>) and returns a <see cref="LocatorResult" /> reflecting what was found.</summary>
    /// <param name="directoryPath"> A directory chosen manually by the user via the folder-picker dialogue. </param>
    Task<LocatorResult> LocateFilesAsync(string directoryPath);
}
```

**Step 9.2: Write `FileLocatorService.cs`**

```csharp
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IFileLocatorService" />
public sealed class FileLocatorService : IFileLocatorService
{
    private const string ExeFileName = "zoo.exe";
    private const string IniFileName = "zoo.ini";

    // Default installation paths checked before the registry. Order matters: we try the 32-bit-on-64-bit path first because Zoo Tycoon (2001) is a 32-bit title and is most often found there.
    private static readonly string[] DefaultInstallPaths =
    [
        @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
        @"C:\Program Files\Microsoft Games\Zoo Tycoon"
    ];

    private static readonly (string SubKey, string ValueName)[] RegistryProbes =
    [
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0",            "Install Path"),
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0",            "Install_Path"),
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0",            "InstallPath"),
        (@"SOFTWARE\Microsoft Games\Zoo Tycoon\1.0",            "Path"),
        (@"SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0","Install Path"),
        (@"SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0","Install_Path"),
        (@"SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0","InstallPath"),
        (@"SOFTWARE\WOW6432Node\Microsoft Games\Zoo Tycoon\1.0","Path")
    ];

    private readonly IFileSystem _fileSystem;
    private readonly IRegistryReader _registry;
    private readonly ILauncherConfigService _config;

    public FileLocatorService(IFileSystem fileSystem, IRegistryReader registry, ILauncherConfigService config)
    {
        _fileSystem = fileSystem;
        _registry = registry;
        _config = config;
    }

    /// <inheritdoc />
    public async Task<LocatorResult> LocateFilesAsync()
    {
        var config = await _config.LoadAsync();

        // (1) Persisted launcher config.
        if (!string.IsNullOrWhiteSpace(config.GameDirectory))
        {
            var fromConfig = await LocateFilesAsync(config.GameDirectory);
            if (fromConfig.ExeFound) return fromConfig;
        }

        // (2) Common default installation paths — checked before the registry because filesystem probes are cheaper than registry queries.
        foreach (var path in DefaultInstallPaths)
        {
            if (!_fileSystem.Directory.Exists(path)) continue;
            var fromDefault = await LocateFilesAsync(path);
            if (fromDefault.ExeFound) return fromDefault;
        }

        // (3) Registry — both native and WOW6432 hives, several plausible value names per build.
        foreach (var directory in EnumerateRegistryCandidates())
        {
            var fromRegistry = await LocateFilesAsync(directory);
            if (fromRegistry.ExeFound) return fromRegistry;
        }

        return new LocatorResult(ExeFound: false, IniFound: false, ExePath: null, IniPath: null, GameDirectory: null);
    }

    /// <inheritdoc />
    public Task<LocatorResult> LocateFilesAsync(string directoryPath)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || !_fileSystem.Directory.Exists(directoryPath))
                return new LocatorResult(false, false, null, null, null);

            var exePath = _fileSystem.Path.Combine(directoryPath, ExeFileName);
            var iniPath = _fileSystem.Path.Combine(directoryPath, IniFileName);

            var exeFound = _fileSystem.File.Exists(exePath);
            var iniFound = _fileSystem.File.Exists(iniPath);

            return new LocatorResult(
                ExeFound: exeFound,
                IniFound: iniFound,
                ExePath: exeFound ? exePath : null,
                IniPath: iniFound ? iniPath : null,
                GameDirectory: exeFound || iniFound ? directoryPath : null);
        });
    }

    private IEnumerable<string> EnumerateRegistryCandidates()
    {
        foreach (var (subKey, valueName) in RegistryProbes)
        {
            var value = _registry.ReadHklmString(subKey, valueName);
            if (!string.IsNullOrWhiteSpace(value) && _fileSystem.Directory.Exists(value))
                yield return value;
        }
    }
}
```

**Step 9.3: Verify build**

Run `mcp__rider__build_solution`. Expected: success.

**Step 9.4: Commit point**

Suggested message: `feat(services): add FileLocatorService with config/filesystem/registry discovery chain`

---

## Task 10: Create `IniParserService`

**Files:**

- Create: `Source/Launcher/Services/IIniParserService.cs`
- Create: `Source/Launcher/Services/IniParserService.cs`

**Step 10.1: Write `IIniParserService.cs`** (per SDD §6.2)

```csharp
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Reads <c> zoo.ini </c> from disk into a typed <see cref="ZooIniModel" /> and writes a <see cref="ZooIniModel" /> back to disk, preserving comments and key ordering on round-trip.</summary>
public interface IIniParserService
{
    /// <summary>Reads and parses <c> zoo.ini </c> from <paramref name="iniFilePath" />.</summary>
    /// <exception cref="System.IO.IOException"> Thrown if the file cannot be read. Callers (typically <see cref="StartupService" />) translate this into <see cref="StartupStatus.IniParseFailed" />. </exception>
    Task<ZooIniModel> ReadAsync(string iniFilePath);

    /// <summary>Writes <paramref name="model" /> back to <paramref name="iniFilePath" />, preserving the original layout if the model was previously read from disk.</summary>
    Task WriteAsync(string iniFilePath, ZooIniModel model);

    /// <summary>Returns a fresh <see cref="ZooIniModel" /> populated with the documented factory defaults.</summary>
    ZooIniModel GetDefaults();
}
```

**Step 10.2: Write `IniParserService.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IIniParserService" />
public sealed class IniParserService : IIniParserService
{
    private readonly IFileSystem _fileSystem;

    public IniParserService(IFileSystem fileSystem) => _fileSystem = fileSystem;

    /// <inheritdoc />
    public async Task<ZooIniModel> ReadAsync(string iniFilePath)
    {
        var lines = await _fileSystem.File.ReadAllLinesAsync(iniFilePath);
        var document = Tokenize(lines);
        var model = new ZooIniModel { RawDocument = document };

        var knownLookup = ZooIniDefaults.KnownKeys
            .ToDictionary(spec => (spec.Section.ToLowerInvariant(), spec.Key.ToLowerInvariant()), spec => spec);

        foreach (var keyValue in document.Lines.OfType<IniKeyValue>())
        {
            var lookupKey = (keyValue.Section.ToLowerInvariant(), keyValue.Key.ToLowerInvariant());
            if (knownLookup.TryGetValue(lookupKey, out var spec))
            {
                spec.Write(model, keyValue.Value);
            }
            else
            {
                // Preserve unknown keys verbatim so they round-trip on write. Stored as "Section.Key" => Value.
                model.UnknownKeys[$"{keyValue.Section}.{keyValue.Key}"] = keyValue.Value;
            }
        }

        return model;
    }

    /// <inheritdoc />
    public async Task WriteAsync(string iniFilePath, ZooIniModel model)
    {
        var document = model.RawDocument ?? BuildFreshDocument(model);
        var updatedLines = MergeModelIntoDocument(document, model);

        var content = new StringBuilder();
        foreach (var line in updatedLines)
            content.AppendLine(RenderLine(line));

        var tempPath = iniFilePath + ".tmp";
        await _fileSystem.File.WriteAllTextAsync(tempPath, content.ToString());
        _fileSystem.File.Move(tempPath, iniFilePath, overwrite: true);
    }

    /// <inheritdoc />
    public ZooIniModel GetDefaults() => new();

    private static IniDocument Tokenize(string[] lines)
    {
        var document = new IniDocument();
        var currentSection = string.Empty;

        foreach (var raw in lines)
        {
            var trimmed = raw.Trim();

            if (trimmed.Length == 0)
            {
                document.Lines.Add(new IniBlank());
                continue;
            }

            if (trimmed.StartsWith(';') || trimmed.StartsWith('#'))
            {
                document.Lines.Add(new IniComment(raw));
                continue;
            }

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                var name = trimmed[1..^1].Trim();
                currentSection = name;
                document.Lines.Add(new IniSectionHeader(name, raw));
                continue;
            }

            var separator = raw.IndexOf('=');
            if (separator < 0)
            {
                // Line we cannot classify as comment/section/kv — treat it as a comment to preserve it on round-trip.
                document.Lines.Add(new IniComment(raw));
                continue;
            }

            var key = raw[..separator].Trim();
            var value = raw[(separator + 1)..].TrimEnd();
            document.Lines.Add(new IniKeyValue(currentSection, key, value, raw));
        }

        return document;
    }

    private static IniDocument BuildFreshDocument(ZooIniModel model)
    {
        var document = new IniDocument();

        foreach (var sectionGroup in ZooIniDefaults.KnownKeys.GroupBy(spec => spec.Section, StringComparer.OrdinalIgnoreCase))
        {
            document.Lines.Add(new IniSectionHeader(sectionGroup.Key, $"[{sectionGroup.Key}]"));
            foreach (var spec in sectionGroup)
            {
                var value = spec.Read(model);
                var raw = $"{spec.Key}={value}";
                document.Lines.Add(new IniKeyValue(sectionGroup.Key, spec.Key, value, raw));
            }
            document.Lines.Add(new IniBlank());
        }

        foreach (var (compoundKey, value) in model.UnknownKeys)
        {
            // Compound key is "Section.Key" — split once on the first dot.
            var dot = compoundKey.IndexOf('.');
            if (dot <= 0) continue;

            var section = compoundKey[..dot];
            var key = compoundKey[(dot + 1)..];

            document.Lines.Add(new IniSectionHeader(section, $"[{section}]"));
            document.Lines.Add(new IniKeyValue(section, key, value, $"{key}={value}"));
            document.Lines.Add(new IniBlank());
        }

        return document;
    }

    private static IReadOnlyList<IniLine> MergeModelIntoDocument(IniDocument document, ZooIniModel model)
    {
        var knownLookup = ZooIniDefaults.KnownKeys
            .ToDictionary(spec => (spec.Section.ToLowerInvariant(), spec.Key.ToLowerInvariant()), spec => spec);

        // Rewrite values of known keys in place; collect which ones we have already emitted so we can append missing ones.
        var emittedKnown = new HashSet<(string Section, string Key)>();
        var emittedUnknown = new HashSet<string>(StringComparer.Ordinal);
        var rewritten = new List<IniLine>(document.Lines.Count);

        foreach (var line in document.Lines)
        {
            if (line is IniKeyValue kv)
            {
                var lookupKey = (kv.Section.ToLowerInvariant(), kv.Key.ToLowerInvariant());
                if (knownLookup.TryGetValue(lookupKey, out var spec))
                {
                    var newValue = spec.Read(model);
                    rewritten.Add(newValue == kv.Value ? kv : kv.WithValue(newValue));
                    emittedKnown.Add((spec.Section, spec.Key));
                    continue;
                }

                var compound = $"{kv.Section}.{kv.Key}";
                if (model.UnknownKeys.TryGetValue(compound, out var unknownValue))
                {
                    rewritten.Add(unknownValue == kv.Value ? kv : kv.WithValue(unknownValue));
                    emittedUnknown.Add(compound);
                    continue;
                }

                // Unknown key that is no longer in the model (e.g. user deleted it) — drop it.
                continue;
            }

            rewritten.Add(line);
        }

        // Append any known keys that weren't present in the source file. Group them by section, appending after the last existing line of that section, or at the end if the section is missing entirely.
        var missingKnown = ZooIniDefaults.KnownKeys
            .Where(spec => !emittedKnown.Contains((spec.Section, spec.Key)))
            .ToList();
        AppendMissingKeys(rewritten, missingKnown.Select(spec => (spec.Section, spec.Key, spec.Read(model))));

        // Append unknown keys that exist in the model but weren't in the source file (rare — only if a caller mutates UnknownKeys directly).
        var missingUnknown = model.UnknownKeys
            .Where(pair => !emittedUnknown.Contains(pair.Key))
            .Select(pair =>
            {
                var dot = pair.Key.IndexOf('.');
                return dot > 0
                    ? (Section: pair.Key[..dot], Key: pair.Key[(dot + 1)..], Value: pair.Value)
                    : (Section: string.Empty, Key: pair.Key, Value: pair.Value);
            })
            .Where(triple => triple.Section.Length > 0);
        AppendMissingKeys(rewritten, missingUnknown);

        return rewritten;
    }

    private static void AppendMissingKeys(List<IniLine> lines, IEnumerable<(string Section, string Key, string Value)> missing)
    {
        foreach (var group in missing.GroupBy(triple => triple.Section, StringComparer.OrdinalIgnoreCase))
        {
            var section = group.Key;
            var insertionIndex = FindSectionEnd(lines, section);

            if (insertionIndex < 0)
            {
                // Section header not present — append at end of file.
                if (lines.Count > 0 && lines[^1] is not IniBlank)
                    lines.Add(new IniBlank());
                lines.Add(new IniSectionHeader(section, $"[{section}]"));
                foreach (var (_, key, value) in group)
                    lines.Add(new IniKeyValue(section, key, value, $"{key}={value}"));
            }
            else
            {
                var keysToInsert = group
                    .Select(triple => (IniLine)new IniKeyValue(section, triple.Key, triple.Value, $"{triple.Key}={triple.Value}"))
                    .ToList();
                lines.InsertRange(insertionIndex, keysToInsert);
            }
        }
    }

    private static int FindSectionEnd(IReadOnlyList<IniLine> lines, string section)
    {
        var headerIndex = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i] is IniSectionHeader header && string.Equals(header.Name, section, StringComparison.OrdinalIgnoreCase))
            {
                headerIndex = i;
                break;
            }
        }

        if (headerIndex < 0) return -1;

        for (var i = headerIndex + 1; i < lines.Count; i++)
        {
            if (lines[i] is IniSectionHeader)
                return i;
        }

        return lines.Count;
    }

    private static string RenderLine(IniLine line) => line switch
    {
        IniKeyValue kv => RenderKeyValue(kv),
        IniSectionHeader section => section.RawText,
        IniComment comment => comment.RawText,
        IniBlank => string.Empty,
        _ => string.Empty
    };

    private static string RenderKeyValue(IniKeyValue kv)
    {
        // If the raw text still represents the current value, emit it verbatim to preserve original whitespace/casing. Otherwise rebuild as "Key=Value".
        var rawSeparator = kv.RawText.IndexOf('=');
        if (rawSeparator > 0)
        {
            var rawValuePart = kv.RawText[(rawSeparator + 1)..].TrimEnd();
            if (rawValuePart == kv.Value) return kv.RawText;
        }
        return $"{kv.Key}={kv.Value}";
    }
}
```

**Step 10.3: Verify build**

Run `mcp__rider__build_solution`. Expected: success.

**Step 10.4: Commit point**

Suggested message: `feat(services): add IniParserService with round-trip layout preservation`

---

## Task 11: Create `VersioningService` (partial)

**Files:**

- Create: `Source/Launcher/Services/IVersioningService.cs`
- Create: `Source/Launcher/Services/VersioningService.cs`

**Step 11.1: Write `IVersioningService.cs`** (per SDD §6.3)

```csharp
using System.Threading.Tasks;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Manages the <c> zoo.ini.original </c> and <c> zoo.ini.undo </c> backup files.</summary>
/// <remarks>This milestone implements only <see cref="EnsureOriginalBackupAsync" />, <see cref="OriginalBackupExists" />, and <see cref="UndoSnapshotExists" />. The remaining members throw <see cref="System.NotImplementedException" /> and will be filled in when the full versioning task is undertaken.</remarks>
public interface IVersioningService
{
    /// <summary>Called once on first launch. Creates <c> zoo.ini.original </c> alongside <paramref name="iniFilePath" /> if it does not already exist.</summary>
    Task EnsureOriginalBackupAsync(string iniFilePath);

    /// <summary>Copies the current <c> zoo.ini </c> to <c> zoo.ini.undo </c> before a save operation. Not yet implemented.</summary>
    Task CreateUndoSnapshotAsync(string iniFilePath);

    /// <summary>Restores <c> zoo.ini </c> from <c> zoo.ini.undo </c>. Not yet implemented.</summary>
    Task<bool> RestoreUndoAsync(string iniFilePath);

    /// <summary>Restores <c> zoo.ini </c> from <c> zoo.ini.original </c>. Not yet implemented.</summary>
    Task<bool> RestoreOriginalAsync(string iniFilePath);

    bool UndoSnapshotExists(string iniFilePath);
    bool OriginalBackupExists(string iniFilePath);
}
```

**Step 11.2: Write `VersioningService.cs`**

```csharp
using System;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IVersioningService" />
public sealed class VersioningService : IVersioningService
{
    private const string OriginalSuffix = ".original";
    private const string UndoSuffix     = ".undo";

    private readonly IFileSystem _fileSystem;

    public VersioningService(IFileSystem fileSystem) => _fileSystem = fileSystem;

    /// <inheritdoc />
    public Task EnsureOriginalBackupAsync(string iniFilePath)
    {
        return Task.Run(() =>
        {
            var originalPath = iniFilePath + OriginalSuffix;
            if (_fileSystem.File.Exists(originalPath)) return;
            if (!_fileSystem.File.Exists(iniFilePath)) return;
            _fileSystem.File.Copy(iniFilePath, originalPath);
        });
    }

    /// <inheritdoc />
    public bool OriginalBackupExists(string iniFilePath) =>
        _fileSystem.File.Exists(iniFilePath + OriginalSuffix);

    /// <inheritdoc />
    public bool UndoSnapshotExists(string iniFilePath) =>
        _fileSystem.File.Exists(iniFilePath + UndoSuffix);

    /// <inheritdoc />
    public Task CreateUndoSnapshotAsync(string iniFilePath) =>
        throw new NotImplementedException("Undo snapshot is implemented in a future milestone.");

    /// <inheritdoc />
    public Task<bool> RestoreUndoAsync(string iniFilePath) =>
        throw new NotImplementedException("Restore undo is implemented in a future milestone.");

    /// <inheritdoc />
    public Task<bool> RestoreOriginalAsync(string iniFilePath) =>
        throw new NotImplementedException("Restore original is implemented in a future milestone.");
}
```

**Step 11.3: Verify build**

Run `mcp__rider__build_solution`. Expected: success.

**Step 11.4: Commit point**

Suggested message: `feat(services): add VersioningService with EnsureOriginalBackupAsync`

---

## Task 12: Create `StartupService`

**Files:**

- Create: `Source/Launcher/Services/IStartupService.cs`
- Create: `Source/Launcher/Services/StartupService.cs`

**Step 12.1: Write `IStartupService.cs`**

```csharp
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Orchestrates the launcher's startup flow: load config, locate game files, parse <c> zoo.ini </c>, ensure the original backup exists, and persist the discovered game directory.</summary>
public interface IStartupService
{
    /// <summary>Runs the full auto-discovery startup sequence and returns a populated <see cref="StartupResult" />.</summary>
    Task<StartupResult> InitializeAsync();

    /// <summary>Re-runs the parse / backup / persist phases against a directory chosen manually by the user.</summary>
    Task<StartupResult> ApplyManualDirectoryAsync(string directoryPath);
}
```

**Step 12.2: Write `StartupService.cs`**

```csharp
using System;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IStartupService" />
public sealed class StartupService : IStartupService
{
    private readonly ILauncherConfigService _config;
    private readonly IFileLocatorService _locator;
    private readonly IIniParserService _parser;
    private readonly IVersioningService _versioning;

    public StartupService(
        ILauncherConfigService config,
        IFileLocatorService locator,
        IIniParserService parser,
        IVersioningService versioning)
    {
        _config = config;
        _locator = locator;
        _parser = parser;
        _versioning = versioning;
    }

    /// <inheritdoc />
    public async Task<StartupResult> InitializeAsync()
    {
        var config = await _config.LoadAsync();
        var locator = await _locator.LocateFilesAsync();
        return await CompleteAsync(config, locator);
    }

    /// <inheritdoc />
    public async Task<StartupResult> ApplyManualDirectoryAsync(string directoryPath)
    {
        var config = await _config.LoadAsync();
        var locator = await _locator.LocateFilesAsync(directoryPath);
        return await CompleteAsync(config, locator);
    }

    private async Task<StartupResult> CompleteAsync(LauncherConfig config, LocatorResult locator)
    {
        if (!locator.ExeFound && !locator.IniFound)
        {
            return new StartupResult(
                Status: StartupStatus.GameDirectoryUnknown,
                GameDirectory: null,
                ExePath: null,
                IniPath: null,
                Model: null,
                Config: config,
                Warning: "Could not locate Zoo Tycoon. Use File → Locate Manually…");
        }

        ZooIniModel? model = null;
        string? parseWarning = null;
        if (locator.IniFound)
        {
            try
            {
                model = await _parser.ReadAsync(locator.IniPath!);
                await _versioning.EnsureOriginalBackupAsync(locator.IniPath!);
            }
            catch (Exception ex)
            {
                return new StartupResult(
                    Status: StartupStatus.IniParseFailed,
                    GameDirectory: locator.GameDirectory,
                    ExePath: locator.ExePath,
                    IniPath: locator.IniPath,
                    Model: null,
                    Config: config,
                    Warning: $"Failed to read zoo.ini: {ex.Message}");
            }
        }
        else
        {
            parseWarning = "zoo.ini not found in the game directory. Settings cannot be edited until it is created.";
        }

        // Persist the discovered directory if it changed.
        if (locator.GameDirectory is not null && !string.Equals(locator.GameDirectory, config.GameDirectory, StringComparison.OrdinalIgnoreCase))
        {
            config.GameDirectory = locator.GameDirectory;
            await _config.SaveAsync(config);
        }

        var status = (locator.ExeFound, locator.IniFound) switch
        {
            (true, true)  => StartupStatus.Ready,
            (true, false) => StartupStatus.IniMissing,
            (false, true) => StartupStatus.ExeMissing,
            _             => StartupStatus.GameDirectoryUnknown
        };

        var warning = status switch
        {
            StartupStatus.Ready       => null,
            StartupStatus.IniMissing  => parseWarning,
            StartupStatus.ExeMissing  => "zoo.exe not found in the game directory. Launching is disabled.",
            _                         => null
        };

        return new StartupResult(
            Status: status,
            GameDirectory: locator.GameDirectory,
            ExePath: locator.ExePath,
            IniPath: locator.IniPath,
            Model: model,
            Config: config,
            Warning: warning);
    }
}
```

**Step 12.3: Verify build**

Run `mcp__rider__build_solution`. Expected: success.

**Step 12.4: Commit point**

Suggested message: `feat(services): add StartupService orchestrating discover → parse → backup → persist`

---

## Task 13: Create `IFolderPicker` and Avalonia implementation

**Files:**

- Create: `Source/Launcher/Services/IFolderPicker.cs`
- Create: `Source/Launcher/Services/AvaloniaFolderPicker.cs`

**Step 13.1: Write `IFolderPicker.cs`**

```csharp
using System.Threading.Tasks;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Abstraction over Avalonia's folder-picker dialogue, kept off <see cref="ViewModels.MainWindowViewModel" /> so the VM is testable without a UI host.</summary>
public interface IFolderPicker
{
    /// <summary>Opens a folder picker. Returns the selected directory path, or <see langword="null" /> if the user cancelled.</summary>
    /// <param name="title"> Window title for the picker dialogue. </param>
    Task<string?> PickFolderAsync(string title);
}
```

**Step 13.2: Write `AvaloniaFolderPicker.cs`**

```csharp
using System;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IFolderPicker" />
/// <remarks>The implementation depends on a live <see cref="TopLevel" /> (the main window). The TopLevel is supplied via <see cref="SetTopLevel" /> after <see cref="Views.MainWindow" /> loads.</remarks>
public sealed class AvaloniaFolderPicker : IFolderPicker
{
    private TopLevel? _topLevel;

    /// <summary>Wires the picker to a live window. Called from <c> MainWindow.OnLoaded </c>.</summary>
    public void SetTopLevel(TopLevel topLevel) => _topLevel = topLevel;

    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(string title)
    {
        if (_topLevel is null)
            throw new InvalidOperationException("AvaloniaFolderPicker has not been bound to a TopLevel. Did MainWindow.OnLoaded forget to call SetTopLevel?");

        var folders = await _topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        if (folders.Count == 0) return null;

        var path = folders[0].TryGetLocalPath();
        return string.IsNullOrEmpty(path) ? null : path;
    }
}
```

**Step 13.3: Verify build**

Run `mcp__rider__build_solution`. Expected: success.

**Step 13.4: Commit point**

Suggested message: `feat(services): add IFolderPicker and AvaloniaFolderPicker for manual locate command`

---

## Task 14: Rewrite `MainWindowViewModel`

**Files:**

- Modify: `Source/Launcher/ViewModels/MainWindowViewModel.cs`

**Step 14.1: Replace the file's contents**

```csharp
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.Services;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>Top-level orchestrating ViewModel. Owns the cached <see cref="ZooIniModel" /> and current paths, and exposes commands for manual file location.</summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IStartupService _startup;
    private readonly IFolderPicker _folderPicker;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _hasIni;
    [ObservableProperty] private bool _hasExe;
    [ObservableProperty] private string? _gameDirectory;
    [ObservableProperty] private string? _iniPath;
    [ObservableProperty] private string? _exePath;

    public MainWindowViewModel(IStartupService startup, IFolderPicker folderPicker)
    {
        _startup = startup;
        _folderPicker = folderPicker;
    }

    /// <summary>Parameterless ctor used by the XAML designer only. Will be unused at runtime once DI is wired in <c> App.axaml.cs </c>.</summary>
    public MainWindowViewModel() : this(NullStartupService.Instance, NullFolderPicker.Instance) { }

    /// <summary>The cached in-memory <c> zoo.ini </c>. Set by <see cref="InitializeAsync" /> on successful parse.</summary>
    public ZooIniModel? Model { get; private set; }

    /// <summary>The cached persisted launcher config. Always non-null after <see cref="InitializeAsync" /> completes.</summary>
    public LauncherConfig Config { get; private set; } = new();

    /// <summary>Runs the full startup flow. Called from <c> MainWindow.OnLoaded </c>.</summary>
    public async Task InitializeAsync()
    {
        IsBusy = true;
        StatusMessage = "Locating Zoo Tycoon…";
        var result = await _startup.InitializeAsync();
        ApplyResult(result);
        IsBusy = false;
    }

    /// <summary>Opens the folder picker, then re-runs startup against the chosen directory. Bound to a "Locate Manually…" menu item.</summary>
    [RelayCommand]
    private async Task LocateManuallyAsync()
    {
        var picked = await _folderPicker.PickFolderAsync("Locate Zoo Tycoon installation directory");
        if (picked is null) return;

        IsBusy = true;
        StatusMessage = "Verifying selected directory…";
        var result = await _startup.ApplyManualDirectoryAsync(picked);
        ApplyResult(result);
        IsBusy = false;
    }

    private void ApplyResult(StartupResult result)
    {
        Model = result.Model;
        Config = result.Config;
        GameDirectory = result.GameDirectory;
        IniPath = result.IniPath;
        ExePath = result.ExePath;
        HasExe = result.ExePath is not null;
        HasIni = result.Model is not null;

        StatusMessage = result.Status switch
        {
            StartupStatus.Ready                => $"Ready. Game directory: {result.GameDirectory}",
            StartupStatus.GameDirectoryUnknown => result.Warning ?? "Zoo Tycoon could not be located.",
            StartupStatus.IniMissing           => result.Warning ?? "zoo.ini not found.",
            StartupStatus.ExeMissing           => result.Warning ?? "zoo.exe not found.",
            StartupStatus.IniParseFailed       => result.Warning ?? "Failed to parse zoo.ini.",
            _                                  => ""
        };
    }
}

// Stub fallbacks used only by the XAML designer's parameterless ctor. The real DI-resolved instance never sees these.
file sealed class NullStartupService : IStartupService
{
    public static readonly NullStartupService Instance = new();
    public Task<StartupResult> InitializeAsync() => Task.FromResult(EmptyResult());
    public Task<StartupResult> ApplyManualDirectoryAsync(string directoryPath) => Task.FromResult(EmptyResult());
    private static StartupResult EmptyResult() =>
        new(StartupStatus.GameDirectoryUnknown, null, null, null, null, new LauncherConfig(), null);
}

file sealed class NullFolderPicker : IFolderPicker
{
    public static readonly NullFolderPicker Instance = new();
    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(null);
}
```

**Step 14.2: Verify build**

Run `mcp__rider__build_solution`. Expected: success. (CommunityToolkit.Mvvm source generators emit the property bodies and the `LocateManuallyCommand`.)

**Step 14.3: Commit point**

Suggested message: `feat(viewmodels): rewrite MainWindowViewModel for startup orchestration`

---

## Task 15: Update `MainWindow.axaml` (status bar, tab/launch gating)

**Files:**

- Modify: `Source/Launcher/Views/MainWindow.axaml`

**Step 15.1: Edit the AXAML**

In the existing file (D:\Source\Personal\Erdmier.ZooTycoonLauncher\Source\Launcher\Views\MainWindow.axaml):

1. **Add a "Locate Manually…" menu item** under the existing `File` menu, before `Load New INI`:
   ```xml
   <MenuItem Header="Locate Manually…" Command="{Binding LocateManuallyCommand}" />
   <Separator />
   ```

2. **Bind the Launch Game menu item's IsEnabled** to `HasExe`:
   ```xml
   <MenuItem Header="Launch Game" IsEnabled="{Binding HasExe}" />
   ```

3. **Bind the TabControl's IsEnabled** to `HasIni`:
   ```xml
   <TabControl IsEnabled="{Binding HasIni}">
     <!-- existing tab items unchanged -->
   </TabControl>
   ```

4. **Add a status bar at the bottom of the DockPanel** (just before its closing tag), with `DockPanel.Dock="Bottom"`:
   ```xml
   <Border DockPanel.Dock="Bottom" Padding="8,4" BorderThickness="0,1,0,0">
     <Grid ColumnDefinitions="Auto,8,*">
       <ProgressBar Grid.Column="0"
                    IsIndeterminate="{Binding IsBusy}"
                    IsVisible="{Binding IsBusy}"
                    Width="120" />
       <TextBlock Grid.Column="2"
                  Text="{Binding StatusMessage}"
                  VerticalAlignment="Center" />
     </Grid>
   </Border>
   ```

**Step 15.2: Verify build**

Run `mcp__rider__build_solution`. Expected: success.

**Step 15.3: Commit point**

Hold for Task 16; combined commit.

---

## Task 16: Update `MainWindow.axaml.cs` (Loaded handler, picker wiring)

**Files:**

- Modify: `Source/Launcher/Views/MainWindow.axaml.cs`

**Step 16.1: Replace the file's contents**

```csharp
using System;

using Avalonia.Controls;
using Avalonia.Interactivity;

using Erdmier.ZooTycoonLauncher.Launcher.Services;
using Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

namespace Erdmier.ZooTycoonLauncher.Launcher.Views;

/// <summary>Code-behind for the main window. Triggers the VM's async startup once the window is loaded and binds the Avalonia folder-picker shim to the live <see cref="TopLevel" />.</summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (App.Services?.GetService(typeof(IFolderPicker)) is AvaloniaFolderPicker picker)
        {
            picker.SetTopLevel(this);
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            try
            {
                await viewModel.InitializeAsync();
            }
            catch (Exception)
            {
                // The startup service catches all expected exceptions and translates them to StartupStatus values; if anything still leaks through, swallow it here so a single bad disk read can't crash the launcher on startup.
            }
        }
    }
}
```

**Step 16.2: Verify build**

Run `mcp__rider__build_solution`. Expected: failure — `App.Services` does not yet exist. The next task adds it. (Alternatively, sequence Task 17 before this one if a clean build
between tasks is preferred.)

**Step 16.3: Commit point**

Combined with Task 17.

---

## Task 17: Wire DI in `App.axaml.cs`

**Files:**

- Modify: `Source/Launcher/App.axaml.cs`

**Step 17.1: Replace the file's contents**

```csharp
using System;
using System.IO.Abstractions;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Erdmier.ZooTycoonLauncher.Launcher.Services;
using Erdmier.ZooTycoonLauncher.Launcher.ViewModels;
using Erdmier.ZooTycoonLauncher.Launcher.Views;

using Microsoft.Extensions.DependencyInjection;

namespace Erdmier.ZooTycoonLauncher.Launcher;

/// <summary>
///     Represents the entry point of the application. Sets up the DI container, resolves <see cref="MainWindowViewModel" />, and assigns it as the main window's data context.
/// </summary>
public class App : Application
{
    /// <summary>The application-wide service provider. Exposed as a static so that <see cref="Views.MainWindow" /> can resolve the folder-picker shim after it loads.</summary>
    public static IServiceProvider? Services { get; private set; }

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        Services = BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IRegistryReader, WindowsRegistryReader>();

        services.AddSingleton<ILauncherConfigService>(sp => new LauncherConfigService(
            sp.GetRequiredService<IFileSystem>(),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));

        services.AddSingleton<IFileLocatorService, FileLocatorService>();
        services.AddSingleton<IIniParserService, IniParserService>();
        services.AddSingleton<IVersioningService, VersioningService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<IFolderPicker, AvaloniaFolderPicker>();

        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}
```

**Step 17.2: Verify build**

Run `mcp__rider__build_solution`. Expected: success.

**Step 17.3: Commit point**

Suggested message: `feat(app): wire DI container and bind status bar to startup flow`

---

## Task 18: Smoke test

**Step 18.1: Build the solution**

Run `mcp__rider__build_solution`. Expected: success with no warnings related to our changes.

**Step 18.2: Run the application**

Use `mcp__rider__execute_run_configuration` (configuration name is whichever Rider auto-generates from `Launcher.csproj` — typically `Launcher: Launcher`) with `waitForExit=false`
to launch the app. Verify in the running window:

- The status bar appears at the bottom.
- If Zoo Tycoon is installed in a default path or the registry, status reads `Ready. Game directory: …` and the tabs are enabled.
- If the game cannot be located, status reads `Could not locate Zoo Tycoon. Use File → Locate Manually…` and the tabs are disabled.
- Selecting `File → Locate Manually…` opens a folder picker; choosing a directory containing `zoo.exe` updates the status to `Ready` (assuming `zoo.ini` is present).

If a real Zoo Tycoon installation isn't available, fabricate one for the smoke test:

1. Create a temp directory like `C:\Temp\ZTSmoke\`.
2. Place an empty file named `zoo.exe` inside (it just needs to exist).
3. Place a small text file `zoo.ini` next to it with content like:
   ```
   [user]
   fullscreen=1
   screenwidth=1280
   ```
4. Use `File → Locate Manually…` to point the launcher at `C:\Temp\ZTSmoke\` and confirm status flips to `Ready`.
5. Verify `%AppData%\ZooTycoonLauncher\launcher.config` was created with `gameDirectory` = `C:\Temp\ZTSmoke`.
6. Verify `C:\Temp\ZTSmoke\zoo.ini.original` was created.
7. Quit the app, re-launch it: status should immediately read `Ready` without needing the manual locate (proving the persisted config is being used).

**Step 18.3: Final commit point**

If anything was tweaked during smoke testing, commit those tweaks. Suggested message: `chore: smoke test fixes`. If nothing changed, no commit needed.

---

## Summary

End state after Task 17:

- 17 new/modified files under `Source/Launcher/`
- `Launcher.csproj` references two additional NuGet packages
- The launcher boots, runs the discovery → parse → backup pipeline, and the status bar reflects every `StartupStatus` outcome
- Settings tabs and the Launch Game menu item gate correctly off `HasIni` / `HasExe`
- A "Locate Manually…" menu item drives the manual-discovery path through `IFolderPicker`
- All services are testable: file I/O behind `IFileSystem`, registry behind `IRegistryReader`, AppData root injected, UI dialog behind `IFolderPicker`

Out-of-scope items remaining (future milestones):

- Tests
- Settings tab ViewModels and Views
- Save / Undo / Full Reset commands; full versioning service
- Launch command (`ILauncherService`)
- Read-only status surface for SDD §9.9 runtime keys
- `MinimiseOnLaunch` UI
