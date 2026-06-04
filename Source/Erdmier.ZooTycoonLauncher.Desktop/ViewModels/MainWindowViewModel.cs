using AppBoot = Erdmier.ZooTycoonLauncher.Application.Boot;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels;

/// <summary>The main window's view model. Dispatches <c>BootCommand</c> on load and routes the result to the active state view model via <c>ActiveContent</c> (SDD §9.2).</summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;

    private readonly IApplicationLifecycle _lifecycle;

    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="lifecycle">Chrome service for requesting application shutdown.</param>
    /// <param name="dialogs">Chrome service for opening modeless dialogues.</param>
    public MainWindowViewModel(IMediator mediator, IApplicationLifecycle lifecycle, IDialogService dialogs)
    {
        _mediator  = mediator;
        _lifecycle = lifecycle;
        _dialogs   = dialogs;
    }

    /// <summary>The currently active state or content view model; drives the main window's <c>ContentControl</c> via <see cref="Composition.ViewLocator" />.</summary>
    [ ObservableProperty ]
    public partial object? ActiveContent { get; set; }

    [ RelayCommand ]
    private async Task BootAsync(CancellationToken cancellationToken)
    {
        ActiveContent = new LookingForZooTycoonViewModel();

        ErrorOr<AppBoot.BootResult> result = await _mediator.Send(new AppBoot.BootCommand(), cancellationToken);

        ActiveContent = result.IsError
            ? new NoGameInstallationFoundViewModel(locatedCandidatePath: null, _dialogs, rebootAsync: BootAsync)
            : RouteResult(result.Value);
    }

    private ViewModelBase RouteResult(AppBoot.BootResult result)
        => result.Outcome switch
        {
            AppBoot.BootOutcome.ReadyToPlay             => new ReadyToPlayViewModel(result.ActiveInstallation!,
                                                                                    rebootAsync: BootAsync,
                                                                                    _lifecycle,
                                                                                    _dialogs,
                                                                                    _mediator),
            AppBoot.BootOutcome.CannotPlay              => new CannotPlayViewModel(result.ActiveInstallation!, _mediator),
            AppBoot.BootOutcome.NoGameInstallationFound => new NoGameInstallationFoundViewModel(result.LocatedCandidatePath,
                                                                                                _dialogs,
                                                                                                rebootAsync: BootAsync),
            AppBoot.BootOutcome.OpenGameInstallation    => new OpenGameInstallationViewModel(_dialogs,
                                                                                             rebootAsync: BootAsync),
            var _                                       => new NoGameInstallationFoundViewModel(locatedCandidatePath: null,
                                                                                                _dialogs,
                                                                                                rebootAsync: BootAsync),
        };
}
