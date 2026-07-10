namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;

/// <summary>The view model for the General tab inside the ReadyToPlay and CannotPlay states. Owns the Launch Game command. SDD §7.10, §9.2.</summary>
public sealed partial class GeneralTabViewModel : ViewModelBase
{
    private readonly Guid _installationId;

    private readonly IMediator? _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The installation whose general information is displayed.</param>
    /// <param name="canPlay"><see langword="true" /> when the owning <see cref="Boot.PlayViewModel" /> is in the ReadyToPlay state; drives <see cref="CanPlay" />.</param>
    /// <param name="mediator">The Mediator dispatcher.</param>
    public GeneralTabViewModel(InstallationSummary installation, bool canPlay, IMediator mediator)
        : this(installation, canPlay)
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
                                       LastOpenedUtc: null),
               canPlay: true)
    { }

    private GeneralTabViewModel(InstallationSummary installation, bool canPlay)
    {
        _installationId  = installation.Id;
        InstallationName = installation.Name;
        InstallationPath = installation.Path;

        InstallationLastPlayed = FormatLastPlayed(installation.LastPlayedUtc);

        CanPlay = canPlay;

        HasExe = installation.Validity.HasExe;
        HasIni = installation.Validity.HasIni;
    }

    /// <summary>
    ///     <see langword="true" /> when the owning <see cref="Boot.PlayViewModel" /> resolved to the ReadyToPlay state at boot; the just-in-time verification inside the handler
    ///     catches drift that happens after boot.
    /// </summary>
    public bool CanPlay { get; }

    /// <summary><see langword="true" /> when <c>zoo.exe</c> is present in the installation's directory; drives the EXE status row.</summary>
    public bool HasExe { get; }

    /// <summary><see langword="true" /> when <c>zoo.ini</c> is present in the installation's directory; drives the INI status row.</summary>
    public bool HasIni { get; }

    /// <summary>The installation's last played date, formatted for display; refreshed in place after a successful launch stamps a new value.</summary>
    [ ObservableProperty ]
    public partial string InstallationLastPlayed { get; set; }

    /// <summary>The installation's user-visible name.</summary>
    public string InstallationName { get; }

    /// <summary>The installation's directory path.</summary>
    public string InstallationPath { get; }

    /// <summary><see langword="true" /> while a launch is in flight; disables the button to prevent double-dispatch.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(LaunchCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary>
    ///     <see langword="true" /> when the installation cannot be played because both <c>zoo.exe</c> and <c>zoo.ini</c> are missing; drives the "missing both files" CannotPlay
    ///     message. Maps to <see cref="InstallationValidity.InvalidNoExeOrIni" />.
    /// </summary>
    public bool IsMissingBothExeAndIni => !HasExe && !HasIni;

    /// <summary>
    ///     <see langword="true" /> when the installation cannot be played because <c>zoo.exe</c> is missing while <c>zoo.ini</c> is present; drives the "missing executable"
    ///     CannotPlay message. Maps to <see cref="InstallationValidity.InvalidNoExe" />.
    /// </summary>
    public bool IsMissingOnlyExe => !HasExe && HasIni;

    /// <summary>
    ///     <see langword="true" /> when the installation cannot be played because <c>zoo.ini</c> is missing while <c>zoo.exe</c> is present; drives the "missing configuration"
    ///     CannotPlay message. Maps to <see cref="InstallationValidity.InvalidNoIni" />.
    /// </summary>
    public bool IsMissingOnlyIni => HasExe && !HasIni;

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

            if (outcome is { Outcome: LaunchGameOutcome.Started, LastPlayedUtc: { } lastPlayedUtc })
            {
                InstallationLastPlayed = FormatLastPlayed(lastPlayedUtc);
            }

            LaunchOutcomeRaised?.Invoke(this, outcome);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteLaunch() => CanPlay && !IsBusy && _mediator is not null;

    /// <summary>Formats a nullable UTC last-played timestamp for display, localising to the user's timezone or rendering "Never" when unset.</summary>
    /// <param name="lastPlayedUtc">The UTC timestamp to format, or <see langword="null" /> when the installation has never been played.</param>
    /// <returns>The localised <c>dd MMMM yyyy HH:mm</c> string, or <c>"Never"</c> when <paramref name="lastPlayedUtc" /> is <see langword="null" />.</returns>
    private static string FormatLastPlayed(DateTime? lastPlayedUtc)
        => lastPlayedUtc?.ToLocalTime()
                         .ToString(format: "dd MMMM yyyy HH:mm")
           ?? "Never";

    /// <summary>Raised after the launch command receives a result. <see cref="Boot.ReadyToPlayViewModel" /> subscribes and routes outcomes to chrome capabilities.</summary>
    public event EventHandler<LaunchGameResult>? LaunchOutcomeRaised;
}
