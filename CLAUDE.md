# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Avalonia 11.3 desktop launcher for **Zoo Tycoon (2001)**. Discovers `zoo.exe` / `zoo.ini`, parses the INI into a strongly typed model, persists launcher state, and (eventually) edits + relaunches. Windows-only, .NET 10, C# 13, Classic Avalonia theme. Single project: `Source/Launcher/Launcher.csproj` under solution `Erdmier.ZooTycoonLauncher.slnx`.

The authoritative spec is [`SoftwareDesignDocument.md`](./SoftwareDesignDocument.md). Implementation plans live in `Docs/Plans/` (named `YYYY-MM-DD-<feature>.md` with a sibling `-design.md`). When working from a plan, use the `superpowers:executing-plans` skill — plans expect that workflow.

## Common commands

- **Build**: prefer `mcp__rider__build_solution` (the plan files reference it as the canonical build check). `dotnet build Erdmier.ZooTycoonLauncher.slnx` works as a fallback.
- **Run**: `dotnet run --project Source/Launcher/Launcher.csproj`.
- **Tests**: none yet. The current milestone explicitly defers them; code is written to be testable (services behind interfaces, `IFileSystem` abstraction, hand-rolled `IRegistryReader`) but no test project exists.

## Architecture

MVVM with constructor-injected services via `Microsoft.Extensions.DependencyInjection`. The DI container is built in `App.OnFrameworkInitializationCompleted` and exposed as `App.Services` so `MainWindow.OnLoaded` can hand the live `TopLevel` to `AvaloniaFolderPicker.SetTopLevel` (the picker can't be constructed until the window exists).

**Startup flow** (entry: `MainWindow.OnLoaded` → `MainWindowViewModel.InitializeAsync` → `IStartupService.InitializeAsync`):

1. `LauncherConfigService` loads `%AppData%\ZooTycoonLauncher\launcher.config` (JSON, atomic temp-file + rename writes).
2. `FileLocatorService.LocateFilesAsync()` probes for `zoo.exe`/`zoo.ini` in this order: persisted `GameDirectory` → hard-coded `Program Files [(x86)]\Microsoft Games\Zoo Tycoon` → eight `HKLM\...\Microsoft Games\Zoo Tycoon\1.0` registry value-name variants. Returns the first directory containing `zoo.exe`.
3. If `zoo.ini` is found, `IniParserService.ReadAsync` tokenises it into an `IniDocument` (`IniLine` records: `IniSectionHeader`/`IniKeyValue`/`IniComment`/`IniBlank`) and folds known keys into a `ZooIniModel`. Unknown keys are stashed verbatim in `ZooIniModel.UnknownKeys` keyed `"Section.Key"`.
4. `VersioningService.EnsureOriginalBackupAsync` copies `zoo.ini` → `zoo.ini.original` if absent (one-time pristine snapshot).
5. If the located directory differs from the persisted `GameDirectory`, the config is rewritten.
6. `StartupService` collapses everything into a `StartupResult` with a `StartupStatus` enum (`Ready`, `GameDirectoryUnknown`, `IniMissing`, `ExeMissing`, `IniParseFailed`); the VM maps this to `HasExe` / `HasIni` flags and a status-bar message. Exceptions bubbling out of the parser/locator are translated to statuses inside `StartupService.CompleteAsync`; `MainWindow.OnLoaded` swallows anything still leaking through so a bad disk read can't crash the launcher.

**Key registry — `Models/ZooIniDefaults.cs`**: single source of truth for every INI key the launcher understands. Each entry is an `IniKeySpec` (`Bool`/`Int`/`NullableInt`/`Str`/`NullableStr` factories) binding `[section]/key` → strongly typed property on `ZooIniModel.{User,UI,Advanced,AI,Debug,Language,Map}` with optional min/max validation. **Adding a new INI key means adding a row in `ZooIniDefaults`, a property on the matching submodel, and (if the key has numeric ranges) a pair of `Min`/`Max` constants in `IniRanges` that both `ZooIniDefaults` and the XAML `NumericUpDown` reference.** Section/key matching is case-insensitive; round-trip writes preserve original casing, comments, blanks, and key ordering by re-emitting from the cached `IniDocument`. Out-of-range or unparseable values silently fall back to the property's current value (intentional — cited at SDD §10).

**View resolution**: `ViewLocator` (registered as an `Application.DataTemplates` entry in `App.axaml`) maps a `*ViewModel` type to a `*View` type by string-replacing `"ViewModel"` → `"View"` and `Activator.CreateInstance`-ing it. Don't rename one half without the other.

## Conventions

- `<RootNamespace>Erdmier.ZooTycoonLauncher.Launcher</RootNamespace>` — every namespace lives under that root. `Nullable` is enabled solution-wide.
- `AvaloniaUseCompiledBindingsByDefault` is on; XAML must declare `x:DataType` (see `MainWindow.axaml`) for bindings to compile.
- `CommunityToolkit.Mvvm` source generators are used (`[ObservableProperty]` on `partial` properties, `[RelayCommand]` on private async methods). Don't hand-roll INPC.
- File system access goes through `System.IO.Abstractions.IFileSystem`, not `System.IO` directly. Registry access goes through `IRegistryReader`. Keep it that way for testability.
- ViewModels expose a parameterless constructor that delegates to a `file`-scoped null-object implementation (e.g. `NullStartupService`) — this is for the XAML designer only and is unused at runtime.
- File writes that touch user data (config, INI) use the temp-file-then-`Move(overwrite: true)` pattern. Don't write in place.
- `JetBrains.Annotations` is referenced; mark designer/runtime-only types with `[UsedImplicitly]` to keep ReSharper/Rider quiet.
