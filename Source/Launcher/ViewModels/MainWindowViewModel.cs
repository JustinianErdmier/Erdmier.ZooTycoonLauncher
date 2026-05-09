using System.Collections.Generic;
using System.ComponentModel;
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

    private readonly ILauncherService _launcher;

    private readonly IShellService _shell;

    private readonly IStartupService _startup;

    public MainWindowViewModel(IStartupService startup, IFolderPicker folderPicker, IShellService shell, ILauncherService launcher, IniSettingsViewModel ini)
    {
        _startup      = startup;
        _folderPicker = folderPicker;
        _shell        = shell;
        _launcher     = launcher;
        Ini           = ini;

        // Bubble Ini.IsDirty changes up so HasPendingIniChanges + LaunchGameCommand.CanExecute() stay current.
        Ini.PropertyChanged += OnIniPropertyChanged;
    }

    /// <summary>Parameterless ctor used by the XAML designer only. Will be unused at runtime once DI is wired in <c>App.axaml.cs</c>.</summary>
    public MainWindowViewModel()
        : this(NullStartupService.Instance, NullFolderPicker.Instance, NullShellService.Instance, NullLauncherService.Instance, new IniSettingsViewModel())
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

    /// <summary>
    ///     Mirrors <see cref="IniSettingsViewModel.IsDirty" />. Drives the unsaved-changes warning above the Launch button and gates <see cref="CanLaunchGame" />. Bound to the
    ///     warning TextBlock's <c>IsVisible</c>.
    /// </summary>
    /// <remarks>
    ///     Computed (not <c>[ObservableProperty]</c>) so the source of truth stays on <see cref="IniSettingsViewModel" />. <see cref="OnIniPropertyChanged" /> raises
    ///     <c>PropertyChanged</c> for this property when <c>Ini.IsDirty</c> flips.
    /// </remarks>
    public bool HasPendingIniChanges => Ini.IsDirty;

    /// <summary>ViewModel for the INI Configurations tab. Receives the cached model on every successful locate operation via <see cref="ApplyResult" />.</summary>
    public IniSettingsViewModel Ini { get; }

    [ ObservableProperty ]
    public partial IReadOnlyList<IniDisplayEntry> IniEntries { get; set; } = [];

    [ ObservableProperty ]
    public partial string? IniPath { get; set; }

    [ ObservableProperty ]
    public partial bool IsBusy { get; set; }

    /// <summary>The cached in-memory <c>zoo.ini</c>. Set by <see cref="InitializeAsync" /> on successful parse.</summary>
    public ZooIniModel? Model { get; private set; }

    [ ObservableProperty ]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Runs the full startup flow. Called from <c>MainWindow.OnLoaded</c>.</summary>
    public async Task InitializeAsync()
    {
        IsBusy        = true;
        StatusMessage = "Locating Zoo Tycoon…";
        StartupResult result = await _startup.InitializeAsync();

        ApplyResult(result);

        IsBusy = false;
    }

    /// <summary>Opens File Explorer focused on <paramref name="path" />. Bound to the "Open" buttons next to the discovered file paths on the Home tab.</summary>
    /// <param name="path">
    ///     Absolute path to a file or directory to reveal. No-op when null/empty so we can bind directly to <see cref="ExePath" /> / <see cref="IniPath" /> without
    ///     null-checks at the binding site.
    /// </param>
    [ RelayCommand ]
    private void RevealInExplorer(string? path) => _shell.RevealInExplorer(path);

    /// <summary>Spawns <c>zoo.exe</c> via <see cref="ILauncherService" />. Bound to the General tab's "Launch Game" button.</summary>
    [ RelayCommand(CanExecute = nameof(CanLaunchGame)) ]
    private async Task LaunchGameAsync()
    {
        if (ExePath is null)
        {
            StatusMessage = "Cannot launch: zoo.exe location is unknown.";

            return;
        }

        StatusMessage = "Launching Zoo Tycoon…";
        LaunchResult result = await _launcher.LaunchAsync(ExePath);

        StatusMessage = result.Success ? "Game launched." : $"Launch failed: {result.ErrorMessage}";
    }

    /// <summary>
    ///     Gates <see cref="LaunchGameCommand" />. Re-evaluated when <see cref="HasExe" /> or <see cref="HasPendingIniChanges" /> changes (see <see cref="OnHasExeChanged(bool)" /> and
    ///     <see cref="OnIniPropertyChanged" />).
    /// </summary>
    private bool CanLaunchGame() => HasExe && !HasPendingIniChanges;

    private void OnIniPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IniSettingsViewModel.IsDirty))
        {
            return;
        }

        OnPropertyChanged(nameof(HasPendingIniChanges));
        LaunchGameCommand.NotifyCanExecuteChanged();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    // HasExe is an [ObservableProperty]; the source generator emits OnHasExeChanged to hook.
    partial void OnHasExeChanged(bool value) => LaunchGameCommand.NotifyCanExecuteChanged();

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

        Ini.ApplyModel(result.Model, result.IniPath);
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

file sealed class NullLauncherService : ILauncherService
{
    public static readonly NullLauncherService Instance = new();

    public Task<LaunchResult> LaunchAsync(string exePath) => Task.FromResult(new LaunchResult(Success: false, ErrorMessage: null));
}
