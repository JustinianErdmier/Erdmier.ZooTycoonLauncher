using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.Services;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>Top-level orchestrating ViewModel. Owns the cached <see cref="ZooIniModel" /> and current paths, and exposes commands for manual file location.</summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFolderPicker _folderPicker;

    private readonly IShellService _shell;

    private readonly IStartupService _startup;

    public MainWindowViewModel(IStartupService startup, IFolderPicker folderPicker, IShellService shell)
    {
        _startup      = startup;
        _folderPicker = folderPicker;
        _shell        = shell;
    }

    /// <summary>Parameterless ctor used by the XAML designer only. Will be unused at runtime once DI is wired in <c> App.axaml.cs </c>.</summary>
    public MainWindowViewModel()
        : this(NullStartupService.Instance, NullFolderPicker.Instance, NullShellService.Instance)
    { }

    /// <summary>The cached persisted launcher config. Always non-null after <see cref="InitializeAsync" /> completes.</summary>
    public LauncherConfig Config { get; private set; } = new();

    [ ObservableProperty ]
    public partial string? ExePath { get; set; }

    [ ObservableProperty ]
    public partial string? GameDirectory { get; set; }

    [ ObservableProperty ]
    public partial bool HasExe { get; set; }

    [ ObservableProperty ]
    public partial bool HasIni { get; set; }

    [ ObservableProperty ]
    public partial IReadOnlyList<IniDisplayEntry> IniEntries { get; set; } = [];

    [ ObservableProperty ]
    public partial string? IniPath { get; set; }

    [ ObservableProperty ]
    public partial bool IsBusy { get; set; }

    /// <summary>The cached in-memory <c> zoo.ini </c>. Set by <see cref="InitializeAsync" /> on successful parse.</summary>
    public ZooIniModel? Model { get; private set; }

    [ ObservableProperty ]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Runs the full startup flow. Called from <c>MainWindow.OnLoaded</c>.</summary>
    public async Task InitializeAsync()
    {
        IsBusy        = true;
        StatusMessage = "Locating Zoo Tycoon…";
        StartupResult result = await _startup.InitializeAsync();

        await Task.Delay(TimeSpan.FromSeconds(seconds: 10));
        ApplyResult(result);

        IsBusy = false;
    }

    /// <summary>Opens File Explorer focused on <paramref name="path" />. Bound to the "Open" buttons next to the discovered file paths on the Home tab.</summary>
    /// <param name="path"> Absolute path to a file or directory to reveal. No-op when null/empty so we can bind directly to <see cref="ExePath" /> / <see cref="IniPath" /> without null-checks at the binding site. </param>
    [ RelayCommand ]
    private void RevealInExplorer(string? path) => _shell.RevealInExplorer(path);

    /// <summary>Opens the folder picker, then re-runs startup against the chosen directory. Bound to a "Locate Manually…" menu item.</summary>
    [ RelayCommand ]
    private async Task LocateManuallyAsync()
    {
        string? picked = await _folderPicker.PickFolderAsync(title: "Locate Zoo Tycoon installation directory");

        if (picked is null)
        {
            return;
        }

        IsBusy        = true;
        StatusMessage = "Verifying selected directory…";
        StartupResult result = await _startup.ApplyManualDirectoryAsync(picked);

        await Task.Delay(TimeSpan.FromSeconds(seconds: 10));
        ApplyResult(result);

        IsBusy = false;
    }

    private void ApplyResult(StartupResult result)
    {
        Model         = result.Model;
        Config        = result.Config;
        GameDirectory = result.GameDirectory;
        IniPath       = result.IniPath;
        ExePath       = result.ExePath;
        HasExe        = result.ExePath is not null;
        HasIni        = result.Model is not null;
        IniEntries    = BuildIniEntries(result.Model);

        StatusMessage = result.Status switch
        {
            StartupStatus.Ready                => "Ready.",
            StartupStatus.GameDirectoryUnknown => result.Warning ?? "Zoo Tycoon could not be located.",
            StartupStatus.IniMissing           => result.Warning ?? "Unable to find zoo.ini.",
            StartupStatus.ExeMissing           => result.Warning ?? "Unable to find zoo.exe.",
            StartupStatus.IniParseFailed       => result.Warning ?? "Failed to parse zoo.ini.",
            var _                              => string.Empty
        };
    }

    private static IReadOnlyList<IniDisplayEntry> BuildIniEntries(ZooIniModel? model)
    {
        if (model is null)
        {
            return [];
        }

        List<IniDisplayEntry> entries = new(ZooIniDefaults.KnownKeys.Count + model.UnknownKeys.Count);

        foreach (IniKeySpec spec in ZooIniDefaults.KnownKeys)
        {
            entries.Add(new IniDisplayEntry($"[{spec.Section}] {spec.Key}", spec.Read(model)));
        }

        foreach ((string compoundKey, string value) in model.UnknownKeys)
        {
            entries.Add(new IniDisplayEntry(compoundKey, value));
        }

        return entries;
    }
}

file sealed class NullStartupService : IStartupService
{
    public static readonly NullStartupService Instance = new();

    public Task<StartupResult> InitializeAsync() => Task.FromResult(EmptyResult());

    public Task<StartupResult> ApplyManualDirectoryAsync(string directoryPath) => Task.FromResult(EmptyResult());

    private static StartupResult EmptyResult()
        => new(StartupStatus.GameDirectoryUnknown, GameDirectory: null, ExePath: null, IniPath: null, Model: null, new LauncherConfig(), Warning: null);
}

file sealed class NullFolderPicker : IFolderPicker
{
    public static readonly NullFolderPicker Instance = new();

    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(result: null);
}

file sealed class NullShellService : IShellService
{
    public static readonly NullShellService Instance = new();

    public void RevealInExplorer(string? path)
    { }
}
