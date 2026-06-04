namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;

/// <summary>The view model for the General tab inside the ReadyToPlay and CannotPlay states. Owns the Launch Game command. SDD §7.10, §9.2.</summary>
public sealed partial class GeneralTabViewModel : ViewModelBase
{
    private readonly Guid _installationId;

    private readonly IMediator? _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The installation whose general information is displayed.</param>
    /// <param name="mediator">The Mediator dispatcher.</param>
    public GeneralTabViewModel(InstallationSummary installation, IMediator mediator)
        : this(installation)
        => _mediator = mediator;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public GeneralTabViewModel()
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

    private GeneralTabViewModel(InstallationSummary installation)
    {
        _installationId  = installation.Id;
        InstallationName = installation.Name;
        InstallationPath = installation.Path;
        CanLaunch        = installation.Validity == InstallationValidity.Valid;
    }

    /// <summary>Raised after the launch command receives a result. <see cref="Boot.ReadyToPlayViewModel" /> subscribes and routes outcomes to chrome capabilities.</summary>
    public event EventHandler<LaunchGameResult>? LaunchOutcomeRaised;

    /// <summary><see langword="true" /> when the installation summary was valid at boot; the just-in-time verification inside the handler catches drift that happens after boot.</summary>
    public bool CanLaunch { get; }

    /// <summary>The installation's user-visible name.</summary>
    public string InstallationName { get; }

    /// <summary>The installation's directory path.</summary>
    public string InstallationPath { get; }

    /// <summary><see langword="true" /> while a launch is in flight; disables the button to prevent double-dispatch.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(LaunchCommand)) ]
    public partial bool IsBusy { get; set; }

    [ RelayCommand(CanExecute = nameof(CanExecuteLaunch)) ]
    private async Task LaunchAsync(CancellationToken cancellationToken)
    {
        if (_mediator is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            ErrorOr<LaunchGameResult> result =
                await _mediator.Send(new LaunchGameCommand(_installationId), cancellationToken);

            LaunchGameResult outcome = result.IsError
                ? new LaunchGameResult(LaunchGameOutcome.StartFailed, CloseAfterGameLaunch: false, result.FirstError.Description)
                : result.Value;

            LaunchOutcomeRaised?.Invoke(this, outcome);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteLaunch() => CanLaunch && !IsBusy && _mediator is not null;
}
