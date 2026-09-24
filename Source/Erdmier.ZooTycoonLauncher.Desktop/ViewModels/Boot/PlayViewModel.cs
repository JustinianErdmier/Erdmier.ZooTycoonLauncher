namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>
///     The view model for the playable states — the active installation is open and its tabs are shown. A single <see cref="CanPlay" /> flag distinguishes the
///     <c>ReadyToPlay</c> outcome (the game can be launched) from the <c>CannotPlay</c> outcome (the installation is invalid or synchronisation failed); the two share an
///     identical layout, so there is one view and one view model. The flag is carried down into the tab view models, which render the difference. Routes launch outcomes from
///     the General tab to chrome capabilities. Implements <see cref="IPendingChangesGuard" /> for the INI Config tab's unsaved edits. SDD §7.10, §9.2.
/// </summary>
public sealed partial class PlayViewModel : ViewModelBase, IPendingChangesGuard
{
    private const int IniConfigTabIndex = 1;

    private readonly IDialogService _dialogs;

    private readonly IApplicationLifecycle _lifecycle;

    private readonly Func<CancellationToken, Task> _rebootAsync;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The resolved active installation.</param>
    /// <param name="canPlay"><see langword="true" /> for the ReadyToPlay outcome; <see langword="false" /> for CannotPlay. Carried down into the tab view models.</param>
    /// <param name="rebootAsync">Delegate that re-issues the boot pipeline as a pointed boot at this installation (SDD §7.2.7), re-verifying it in place.</param>
    /// <param name="lifecycle">Chrome service for requesting application shutdown.</param>
    /// <param name="dialogs">Chrome service for opening modeless dialogues.</param>
    /// <param name="mediator">The Mediator dispatcher (passed to the General tab).</param>
    /// <param name="iniErrorMessage">The boot's INI synchronisation error, or <see langword="null" />; shown on both tabs.</param>
    public PlayViewModel(InstallationSummary           installation,
                         bool                          canPlay,
                         Func<CancellationToken, Task> rebootAsync,
                         IApplicationLifecycle         lifecycle,
                         IDialogService                dialogs,
                         IMediator                     mediator,
                         string?                       iniErrorMessage = null)
    {
        _rebootAsync = rebootAsync;
        _lifecycle   = lifecycle;
        _dialogs     = dialogs;

        CanPlay = canPlay;

        GeneralTab   = new GeneralTabViewModel(installation, canPlay, mediator, iniErrorMessage);
        IniConfigTab = new IniConfigTabViewModel(installation, iniErrorMessage, mediator, dialogs);

        GeneralTab.LaunchOutcomeRaised += OnLaunchOutcomeRaised;
        IniConfigTab.PropertyChanged   += OnIniConfigTabPropertyChanged;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public PlayViewModel()
    {
        _rebootAsync = static _ => Task.CompletedTask;
        _lifecycle   = new NoOpApplicationLifecycle();
        _dialogs     = new NoOpDialogService();

        CanPlay = true;

        GeneralTab   = new GeneralTabViewModel();
        IniConfigTab = new IniConfigTabViewModel();

        GeneralTab.LaunchOutcomeRaised += OnLaunchOutcomeRaised;
        IniConfigTab.PropertyChanged   += OnIniConfigTabPropertyChanged;
    }

    /// <summary>General tab view model.</summary>
    public GeneralTabViewModel GeneralTab { get; }

    /// <inheritdoc />
    public bool HasPendingChanges => IniConfigTab.HasPendingChanges;

    /// <summary>INI Config tab view model.</summary>
    public IniConfigTabViewModel IniConfigTab { get; }

    /// <summary><see langword="true" /> when the active installation can be launched (the ReadyToPlay outcome); <see langword="false" /> for CannotPlay.</summary>
    public bool CanPlay { get; }

    /// <summary>The selected tab (0 = General, 1 = INI Config). Selecting the INI Config tab (re)loads it from disk.</summary>
    [ ObservableProperty ]
    public partial int SelectedTabIndex { get; set; }

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

    private async void OnLaunchOutcomeRaised(object? sender, LaunchGameResult result)
    {
        try
        {
            switch (result.Outcome)
            {
                case LaunchGameOutcome.Started when result.CloseAfterGameLaunch:
                    _lifecycle.RequestShutdown();

                    break;

                case LaunchGameOutcome.Started:
                    break;

                case LaunchGameOutcome.Drifted:
                    // CancellationToken.None: a drift-triggered reboot should always complete; the user already committed by clicking Launch, and there is no UI-level cancellation
                    // source here.
                    await _rebootAsync(CancellationToken.None);

                    break;

                case LaunchGameOutcome.StartFailed:
                    _dialogs.ShowLaunchError(result.FailureMessage ?? "Zoo Tycoon could not be launched.");

                    break;
            }
        }
        catch (Exception ex)
        {
            // Nested guard: ShowLaunchError can itself throw (e.g. Avalonia visual-tree failure); an uncaught throw here would escape async void to the synchronisation context and
            // crash the process. Swallow the secondary failure — the original error is already lost.
            try
            {
                _dialogs.ShowLaunchError($"The launcher could not refresh installation state: {ex.Message}");
            }
            catch
            {
                // Intentionally empty.
            }
        }
    }
}

file sealed class NoOpApplicationLifecycle : IApplicationLifecycle
{
    public void RequestShutdown()
    { }
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
