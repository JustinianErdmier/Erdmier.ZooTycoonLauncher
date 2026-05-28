using AppBoot = Erdmier.ZooTycoonLauncher.Application.Boot;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels;

/// <summary>The main window's view model. Dispatches <c>BootCommand</c> on load and routes the result to the active state view model via <c>ActiveContent</c> (SDD §9.2).</summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    public MainWindowViewModel(IMediator mediator) => _mediator = mediator;

    /// <summary>The currently active state or content view model; drives the main window's <c>ContentControl</c> via <see cref="Composition.ViewLocator" />.</summary>
    [ ObservableProperty ]
    public partial object? ActiveContent { get; set; }

    [ RelayCommand ]
    private async Task BootAsync(CancellationToken cancellationToken)
    {
        ActiveContent = new LookingForZooTycoonViewModel();

        ErrorOr<AppBoot.BootResult> result = await _mediator.Send(new AppBoot.BootCommand(), cancellationToken);

        ActiveContent = result.IsError ? new NoGameInstallationFoundViewModel(locatedCandidatePath: null) : RouteResult(result.Value);
    }

    private static ViewModelBase RouteResult(AppBoot.BootResult result) => result.Outcome switch
    {
        AppBoot.BootOutcome.ReadyToPlay             => new ReadyToPlayViewModel(result.ActiveInstallation!),
        AppBoot.BootOutcome.CannotPlay              => new CannotPlayViewModel(result.ActiveInstallation!),
        AppBoot.BootOutcome.NoGameInstallationFound => new NoGameInstallationFoundViewModel(result.LocatedCandidatePath),
        AppBoot.BootOutcome.OpenGameInstallation    => new OpenGameInstallationViewModel(),
        _                                           => new NoGameInstallationFoundViewModel(locatedCandidatePath: null)
    };
}
