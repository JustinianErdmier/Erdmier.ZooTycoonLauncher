using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.Services;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>Top-level orchestrating ViewModel. Owns the active installation, cached <see cref="ZooIniModel" />, and commands for installation management.</summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDialogService _dialog;

    private readonly IFolderPicker _folderPicker;

    private readonly IInstallationService _installations;

    private readonly ILauncherService _launcher;

    private readonly IShellService _shell;

    private readonly IStartupService _startup;

    /// <summary>Initialises a new instance of <see cref="MainWindowViewModel" /> with all required services.</summary>
    /// <param name="startup">Startup service that orchestrates the multi-installation launch flow.</param>
    /// <param name="installations">Installation service used by the picker and Manage dialog.</param>
    /// <param name="dialog">Dialog service used to show modal pickers, alerts, and confirmations.</param>
    /// <param name="folderPicker">Folder-picker shim used by the manual locate command.</param>
    /// <param name="shell">Shell service used to reveal located paths in File Explorer.</param>
    /// <param name="launcher">Game launcher used to spawn <c>zoo.exe</c>.</param>
    /// <param name="ini">ViewModel for the INI Configurations tab.</param>
    public MainWindowViewModel(IStartupService      startup,
                               IInstallationService installations,
                               IDialogService       dialog,
                               IFolderPicker        folderPicker,
                               IShellService        shell,
                               ILauncherService     launcher,
                               IniSettingsViewModel ini)
    {
        _startup       = startup;
        _installations = installations;
        _dialog        = dialog;
        _folderPicker  = folderPicker;
        _shell         = shell;
        _launcher      = launcher;
        Ini            = ini;

        // Bubble Ini.IsDirty changes up so HasPendingIniChanges + LaunchGameCommand.CanExecute() stay current.
        Ini.PropertyChanged += OnIniPropertyChanged;
    }

    /// <summary>Parameterless ctor used by the XAML designer only. Will be unused at runtime once DI is wired in <c>App.axaml.cs</c>.</summary>
    public MainWindowViewModel()
        : this(NullStartupService.Instance,
               NullInstallationService.Instance,
               NullDialogService.Instance,
               NullFolderPicker.Instance,
               NullShellService.Instance,
               NullLauncherService.Instance,
               new IniSettingsViewModel())
    { }

    /// <summary>The active Zoo Tycoon installation for this session. <see langword="null" /> when no installation is open.</summary>
    [ ObservableProperty ]
    public partial Installation? ActiveInstallation { get; set; }

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

    /// <summary><see langword="true" /> when at least one installation is registered. Gates <see cref="ChangeInstallationCommand" />.</summary>
    [ ObservableProperty ]
    public partial bool HasInstallations { get; set; }

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

    // NOTE: This implementation is temporary. Once the status bar is polished, this will be reworked or removed.
    public bool IsStatusBarVisible => !(!string.IsNullOrEmpty(StatusMessage) && StatusMessage.StartsWith(value: "Ready.", StringComparison.OrdinalIgnoreCase));

    /// <summary>The cached in-memory <c>zoo.ini</c>. Set by <see cref="InitializeAsync" /> on successful parse.</summary>
    public ZooIniModel? Model { get; private set; }

    [ ObservableProperty ]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Runs the full startup flow, handling picker and invalid-installation dialogs as needed. Called from <c>MainWindow.OnLoaded</c>.</summary>
    public async Task InitializeAsync()
    {
        IsBusy        = true;
        StatusMessage = "Locating Zoo Tycoon…";

        StartupResult result = await _startup.InitializeAsync();

        if (result.Status == StartupStatus.AwaitingUserSelection)
        {
            result = await HandlePickerAsync(result);
        }

        if (result.Status == StartupStatus.AllInstallationsInvalid
            || result.InvalidInstallations.Count > 0)
        {
            result = await HandleInvalidInstallationsAsync(result);
        }

        ApplyResult(result);
        IsBusy = false;
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(IsStatusBarVisible));

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
    ///     Gates <see cref="LaunchGameCommand" />. Re-evaluated when <see cref="HasExe" /> or <see cref="HasPendingIniChanges" /> changes (see <see cref="OnHasExeChanged(bool)" />
    ///     and <see cref="OnIniPropertyChanged" />).
    /// </summary>
    private bool CanLaunchGame() => HasExe && !HasPendingIniChanges;

    /// <summary>Opens the installation picker so the user can switch to a different registered installation.</summary>
    [ RelayCommand(CanExecute = nameof(HasInstallations)) ]
    private async Task ChangeInstallationAsync()
    {
        IReadOnlyList<Installation> all    = await _installations.GetAllAsync();
        Installation?               picked = await _dialog.ShowPickerAsync(all);

        if (picked is null)
        {
            return;
        }

        IsBusy        = true;
        StatusMessage = "Opening installation…";
        StartupResult result = await _startup.OpenInstallationByIdAsync(picked.Id);
        ApplyResult(result);
        IsBusy = false;
    }

    /// <summary>Opens the Manage Installations dialog and refreshes <see cref="HasInstallations" /> when it closes.</summary>
    [ RelayCommand ]
    private async Task ManageInstallationsAsync()
    {
        await _dialog.ShowManageAsync();

        // Refresh HasInstallations after the dialog closes in case the user added or removed entries.
        IReadOnlyList<Installation> all = await _installations.GetAllAsync();
        HasInstallations = all.Count > 0;
        ChangeInstallationCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Opens the folder picker, registers the chosen directory, and re-runs startup. Bound to the File menu's "Add Installation…" item.</summary>
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

    /// <summary>Shows the picker for the user to choose an installation; opens it on selection or returns the original result on cancel.</summary>
    private async Task<StartupResult> HandlePickerAsync(StartupResult result)
    {
        IReadOnlyList<Installation> all    = await _installations.GetAllAsync();
        Installation?               picked = await _dialog.ShowPickerAsync(all);

        if (picked is null)
        {
            return result; // User cancelled — keep AwaitingUserSelection.
        }

        return await _startup.OpenInstallationByIdAsync(picked.Id);
    }

    /// <summary>
    ///     Shows the combined invalid-installations alert; on close, re-evaluates the installation list and opens the most-recently
    ///     used valid installation, or falls back to <see cref="StartupStatus.GameDirectoryUnknown" /> if none remain.
    /// </summary>
    private async Task<StartupResult> HandleInvalidInstallationsAsync(StartupResult result)
    {
        await _dialog.ShowInvalidInstallationsAlertAsync(result.InvalidInstallations);

        if (result.Status != StartupStatus.AllInstallationsInvalid)
        {
            return result; // Partial invalid — active installation is already set.
        }

        // Find the best valid installation after the user applied Fix/Remove/Ignore.
        IReadOnlyList<Installation> all       = await _installations.GetAllAsync();
        Installation?               bestValid = all.Where(i => i.IsValid).OrderByDescending(i => i.LastOpened).FirstOrDefault();

        if (bestValid is not null)
        {
            return await _startup.OpenInstallationByIdAsync(bestValid.Id);
        }

        // All installations are still invalid or were removed — fall back to GameDirectoryUnknown.
        return new StartupResult(StartupStatus.GameDirectoryUnknown,
                                 GameDirectory: null,
                                 ExePath: null,
                                 IniPath: null,
                                 Model: null,
                                 result.Config,
                                 Warning: "No valid installations. Use Manage Installations to add one.",
                                 ActiveInstallation: null,
                                 InvalidInstallations: []);
    }

    /// <summary>Copies the supplied <see cref="StartupResult" /> into the bindable VM properties and updates the status message.</summary>
    private void ApplyResult(StartupResult result)
    {
        Model              = result.Model;
        Config             = result.Config;
        ActiveInstallation = result.ActiveInstallation;
        GameDirectory      = result.GameDirectory;
        IniPath            = result.IniPath;
        ExePath            = result.ExePath;
        HasExe             = result.ExePath is not null;
        HasIni             = result.Model is not null;
        HasInstallations   = result.Config.Installations.Count > 0;
        IniEntries         = BuildIniEntries(result.Model);

        StatusMessage = result.Status switch
        {
            StartupStatus.Ready                   => "Ready.",
            StartupStatus.GameDirectoryUnknown    => result.Warning ?? "Zoo Tycoon could not be located.",
            StartupStatus.IniMissing              => result.Warning ?? "Unable to find zoo.ini.",
            StartupStatus.ExeMissing              => result.Warning ?? "Unable to find zoo.exe.",
            StartupStatus.IniParseFailed          => result.Warning ?? "Failed to parse zoo.ini.",
            StartupStatus.AwaitingUserSelection   => "Select an installation to continue.",
            StartupStatus.AllInstallationsInvalid => result.Warning ?? "All registered installations are invalid.",
            var _                                 => string.Empty
        };

        Ini.ApplyModel(result.Model, result.IniPath);
    }

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

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnHasInstallationsChanged(bool value) => ChangeInstallationCommand.NotifyCanExecuteChanged();

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

    public Task<StartupResult> OpenInstallationByIdAsync(Guid id) => Task.FromResult(EmptyResult());

    private static StartupResult EmptyResult()
        => new(StartupStatus.GameDirectoryUnknown,
               GameDirectory: null,
               ExePath: null,
               IniPath: null,
               Model: null,
               new LauncherConfig(),
               Warning: null,
               ActiveInstallation: null,
               InvalidInstallations: []);
}

file sealed class NullInstallationService : IInstallationService
{
    public static readonly NullInstallationService Instance = new();

    public Task<bool> ValidateAsync(string gameDirectory) => Task.FromResult(false);

    public Task RevalidateAllAsync() => Task.CompletedTask;

    public Task<IReadOnlyList<Installation>> GetAllAsync() => Task.FromResult<IReadOnlyList<Installation>>([]);

    public Task<Installation> AddAsync(string gameDirectory, string? name = null)
        => Task.FromResult(new Installation { GameDirectory = gameDirectory, Name = name });

    public Task RemoveAsync(Guid id) => Task.CompletedTask;

    public Task UpdateAsync(Guid id, string? name = null, string? gameDirectory = null) => Task.CompletedTask;

    public Task SetLastOpenedAsync(Guid id) => Task.CompletedTask;

    public Task<LocatorResult> DiscoverAsync()
        => Task.FromResult(new LocatorResult(ExeFound: false, IniFound: false, ExePath: null, IniPath: null, GameDirectory: null));
}

file sealed class NullDialogService : IDialogService
{
    public static readonly NullDialogService Instance = new();

    public Task<Installation?> ShowPickerAsync(IEnumerable<Installation> installations) => Task.FromResult<Installation?>(result: null);

    public Task ShowManageAsync() => Task.CompletedTask;

    public Task ShowInvalidInstallationsAlertAsync(IReadOnlyList<Installation> invalid) => Task.CompletedTask;

    public Task<bool> ConfirmAsync(string message, string title = "Confirm") => Task.FromResult(false);

    public Task<string?> ShowInputAsync(string prompt, string title, string? defaultValue = null) => Task.FromResult<string?>(result: null);
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
