namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>The view model for the ReadyToPlay state — the active installation is valid and the game can be launched. Routes launch outcomes from the General tab to chrome capabilities. SDD §7.10.</summary>
public sealed class ReadyToPlayViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;

    private readonly IApplicationLifecycle _lifecycle;

    private readonly Func<CancellationToken, Task> _rebootAsync;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The resolved active installation.</param>
    /// <param name="rebootAsync">Delegate that re-issues the boot pipeline (typically <c>MainWindowViewModel.BootAsync</c>).</param>
    /// <param name="lifecycle">Chrome service for requesting application shutdown.</param>
    /// <param name="dialogs">Chrome service for opening modeless dialogues.</param>
    /// <param name="mediator">The Mediator dispatcher (passed to the General tab).</param>
    public ReadyToPlayViewModel(InstallationSummary           installation,
                                Func<CancellationToken, Task> rebootAsync,
                                IApplicationLifecycle         lifecycle,
                                IDialogService                dialogs,
                                IMediator                     mediator)
    {
        _rebootAsync     = rebootAsync;
        _lifecycle       = lifecycle;
        _dialogs         = dialogs;

        InstallationName = installation.Name;
        InstallationPath = installation.Path;
        IsDefault        = installation.IsDefault;

        GeneralTab   = new GeneralTabViewModel(installation, mediator);
        IniConfigTab = new IniConfigTabViewModel();
        ScenariosTab = new ScenariosTabViewModel();

        GeneralTab.LaunchOutcomeRaised += OnLaunchOutcomeRaised;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public ReadyToPlayViewModel()
        : this(new InstallationSummary(Guid.Empty,
                                       Name: "Designer Installation",
                                       Path: @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
                                       InstallationValidity.Valid,
                                       IsDefault: true,
                                       DateTime.UtcNow,
                                       ModifiedUtc: null,
                                       LastPlayedUtc: null,
                                       LastOpenedUtc: null))
    { }

    private ReadyToPlayViewModel(InstallationSummary installation)
    {
        _rebootAsync     = static _ => Task.CompletedTask;
        _lifecycle       = new NoOpApplicationLifecycle();
        _dialogs         = new NoOpDialogService();

        InstallationName = installation.Name;
        InstallationPath = installation.Path;
        IsDefault        = installation.IsDefault;

        GeneralTab   = new GeneralTabViewModel();
        IniConfigTab = new IniConfigTabViewModel();
        ScenariosTab = new ScenariosTabViewModel();

        GeneralTab.LaunchOutcomeRaised += OnLaunchOutcomeRaised;
    }

    /// <summary>General tab view model.</summary>
    public GeneralTabViewModel GeneralTab { get; }

    /// <summary>INI Config tab view model.</summary>
    public IniConfigTabViewModel IniConfigTab { get; }

    /// <summary>The installation's user-visible name.</summary>
    public string InstallationName { get; }

    /// <summary>The installation's directory path.</summary>
    public string InstallationPath { get; }

    /// <summary><see langword="true" /> when this is the default installation.</summary>
    public bool IsDefault { get; }

    /// <summary>Scenarios tab view model.</summary>
    public ScenariosTabViewModel ScenariosTab { get; }

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
                    await _rebootAsync(CancellationToken.None);
                    break;

                case LaunchGameOutcome.StartFailed:
                    _dialogs.ShowLaunchError(result.FailureMessage ?? "Zoo Tycoon could not be launched.");
                    break;
            }
        }
        catch (Exception ex)
        {
            _dialogs.ShowLaunchError($"The launcher could not refresh installation state: {ex.Message}");
        }
    }
}

file sealed class NoOpApplicationLifecycle : IApplicationLifecycle
{
    public void RequestShutdown() { }
}

file sealed class NoOpDialogService : IDialogService
{
    public void ShowLaunchError(string message) { }
}
