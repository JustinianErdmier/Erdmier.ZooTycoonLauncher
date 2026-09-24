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

    private readonly ILogger<PlayViewModel> _logger;

    private readonly Func<CancellationToken, Task> _rebootAsync;

    private bool _confirmLeaveInProgress;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The resolved active installation.</param>
    /// <param name="canPlay"><see langword="true" /> for the ReadyToPlay outcome; <see langword="false" /> for CannotPlay. Carried down into the tab view models.</param>
    /// <param name="rebootAsync">Delegate that re-issues the boot pipeline as a pointed boot at this installation (SDD §7.2.7), re-verifying it in place.</param>
    /// <param name="openInstallationManagerAsync">Opens the Installation Manager through the main window (Cannot Play's button).</param>
    /// <param name="lifecycle">Chrome service for requesting application shutdown.</param>
    /// <param name="dialogs">Chrome service for opening modeless dialogues.</param>
    /// <param name="mediator">The Mediator dispatcher (passed to the General tab).</param>
    /// <param name="logger">Logger for unexpected failures whilst activating the INI Config tab and whilst handling a launch outcome.</param>
    /// <param name="iniErrorMessage">The boot's INI synchronisation error, or <see langword="null" />; shown on both tabs.</param>
    public PlayViewModel(InstallationSummary           installation,
                         bool                          canPlay,
                         Func<CancellationToken, Task> rebootAsync,
                         Func<CancellationToken, Task> openInstallationManagerAsync,
                         IApplicationLifecycle         lifecycle,
                         IDialogService                dialogs,
                         IMediator                     mediator,
                         ILogger<PlayViewModel>        logger,
                         string?                       iniErrorMessage = null)
    {
        _rebootAsync = rebootAsync;
        _lifecycle   = lifecycle;
        _dialogs     = dialogs;
        _logger      = logger;

        CanPlay        = canPlay;
        InstallationId = installation.Id;

        GeneralTab   = new GeneralTabViewModel(installation, canPlay, mediator, openInstallationManagerAsync, iniErrorMessage);
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
        _logger      = NullLogger<PlayViewModel>.Instance;

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

    /// <summary>The identifier of the open installation — used by the main window to re-verify it after the Installation Manager reports a change.</summary>
    public Guid InstallationId { get; }

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

        if (_confirmLeaveInProgress)
        {
            // A second call arrived whilst the first prompt is still open — e.g. a Drifted launch outcome racing a close, Exit or Close Installation guard. Let the
            // first answer decide rather than stacking an identical prompt on top of it.
            return false;
        }

        _confirmLeaveInProgress = true;

        try
        {
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
        finally
        {
            _confirmLeaveInProgress = false;
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
        catch (Exception ex)
        {
            // ActivateAsync already turns a load failure into either the placeholder or a dialogue; this catch only stops an unexpected fault escaping the
            // fire-and-forget call.
            _logger.LogError(ex, "Unexpected failure whilst activating the INI Config tab.");
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
                // HasPendingChanges guards against edits typed whilst the launch was in flight: shutting down now would silently discard them, so the launcher stays open
                // and the edits remain on screen.
                case LaunchGameOutcome.Started when result.CloseAfterGameLaunch && !HasPendingChanges:
                    _lifecycle.RequestShutdown();

                    break;

                case LaunchGameOutcome.Started:
                    break;

                case LaunchGameOutcome.Drifted:
                    // The same in-flight-edit race applies here: confirm before rebooting, exactly as the pending-changes guard does elsewhere, so a reboot never overwrites
                    // edits typed whilst the launch was running. CancellationToken.None: the user already committed by clicking Launch, and there is no UI-level cancellation
                    // source here.
                    if (!await ConfirmLeaveAsync(CancellationToken.None))
                    {
                        break;
                    }

                    await _rebootAsync(CancellationToken.None);

                    break;

                case LaunchGameOutcome.StartFailed:
                    _dialogs.ShowLaunchError(result.FailureMessage ?? "Zoo Tycoon could not be launched.");

                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure whilst handling the launch outcome.");

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

    public Task<bool> ShowEditInstallationAsync(Guid installationId) => Task.FromResult(false);

    public Task ShowInstallationInfoAsync(Guid installationId) => Task.CompletedTask;

    public Task<bool> ShowDeleteInstallationAsync(Guid installationId) => Task.FromResult(false);

    public Task<bool> ShowFixInstallationAsync(Guid installationId) => Task.FromResult(false);

    public Task<string?> PickFolderAsync(string? startPath) => Task.FromResult<string?>(result: null);

    public Task<SaveChangesChoice> ShowSaveChangesPromptAsync() => Task.FromResult(SaveChangesChoice.Cancel);

    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
}
