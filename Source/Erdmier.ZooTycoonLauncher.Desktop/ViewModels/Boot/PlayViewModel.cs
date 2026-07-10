namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>
///     The view model for the playable states — the active installation is open and its tabs are shown. A single <see cref="CanPlay" /> flag distinguishes the
///     <c>ReadyToPlay</c> outcome (the game can be launched) from the <c>CannotPlay</c> outcome (the installation is invalid or synchronisation failed); the two share an
///     identical layout, so there is one view and one view model. The flag is carried down into the tab view models, which render the difference. Routes launch outcomes from
///     the General tab to chrome capabilities. SDD §7.10, §9.2.
/// </summary>
public sealed class PlayViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;

    private readonly IApplicationLifecycle _lifecycle;

    private readonly Func<CancellationToken, Task> _rebootAsync;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The resolved active installation.</param>
    /// <param name="canPlay"><see langword="true" /> for the ReadyToPlay outcome; <see langword="false" /> for CannotPlay. Carried down into the tab view models.</param>
    /// <param name="rebootAsync">Delegate that re-issues the boot pipeline (typically <c>MainWindowViewModel.BootAsync</c>).</param>
    /// <param name="lifecycle">Chrome service for requesting application shutdown.</param>
    /// <param name="dialogs">Chrome service for opening modeless dialogues.</param>
    /// <param name="mediator">The Mediator dispatcher (passed to the General tab).</param>
    public PlayViewModel(InstallationSummary           installation,
                         bool                          canPlay,
                         Func<CancellationToken, Task> rebootAsync,
                         IApplicationLifecycle         lifecycle,
                         IDialogService                dialogs,
                         IMediator                     mediator)
    {
        _rebootAsync = rebootAsync;
        _lifecycle   = lifecycle;
        _dialogs     = dialogs;

        CanPlay = canPlay;

        GeneralTab   = new GeneralTabViewModel(installation, canPlay, mediator);
        IniConfigTab = new IniConfigTabViewModel();

        GeneralTab.LaunchOutcomeRaised += OnLaunchOutcomeRaised;
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
    }

    /// <summary>General tab view model.</summary>
    public GeneralTabViewModel GeneralTab { get; }

    /// <summary>INI Config tab view model.</summary>
    public IniConfigTabViewModel IniConfigTab { get; }

    /// <summary><see langword="true" /> when the active installation can be launched (the ReadyToPlay outcome); <see langword="false" /> for CannotPlay.</summary>
    public bool CanPlay { get; }

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

    public Task<string?> PickFolderAsync(string? startPath) => Task.FromResult<string?>(result: null);
}
