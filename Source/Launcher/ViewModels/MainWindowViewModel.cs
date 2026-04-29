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
