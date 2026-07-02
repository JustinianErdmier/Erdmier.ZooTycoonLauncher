using Avalonia.Media;

using AppBoot = Erdmier.ZooTycoonLauncher.Application.Boot;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels;

/// <summary>The main window's view model. Dispatches <c>BootCommand</c> when loaded and routes the result to the active state view model via <c>ActiveContent</c> (SDD §9.2).</summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private const double BootedWindowWidth = 720;

    private const double BootingWindowWidth = 480;

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

    /// <summary>Whether the launcher is currently running its boot sequence. Drives <see cref="WindowWidth" /> and is the single source of truth for the booting/booted distinction.</summary>
    [ ObservableProperty ]
    [ NotifyPropertyChangedFor(nameof(WindowWidth)) ]
    public partial bool IsBooting { get; set; } = true;

    [ ObservableProperty ]
    public partial string? StatusMessagePrimaryText { get; set; }

    [ ObservableProperty ]
    public partial IBrush? StatusMessageSecondaryColour { get; set; }

    [ ObservableProperty ]
    public partial string? StatusMessageSecondaryText { get; set; }

    /// <summary>The main window's width in device-independent pixels: narrow whilst booting, wide once booted. Derived from <see cref="IsBooting" /> so the two can never disagree.</summary>
    public double WindowWidth => IsBooting ? BootingWindowWidth : BootedWindowWidth;

    [ RelayCommand ]
    private async Task BootAsync(CancellationToken cancellationToken)
    {
        ActiveContent              = new LookingForZooTycoonViewModel();
        IsBooting                  = true;
        StatusMessagePrimaryText   = "Discovering installations…";
        StatusMessageSecondaryText = "Please wait…";

        // await Task.Delay(TimeSpan.FromSeconds(seconds: 2), cancellationToken);

        ErrorOr<AppBoot.BootResult> result = await _mediator.Send(new AppBoot.BootCommand(), cancellationToken);

        IsBooting = false;

        ActiveContent = result.IsError
                            ? new NoGameInstallationFoundViewModel(locatedCandidatePath: null, _dialogs, BootAsync)
                            : RouteResult(result.Value);

        UpdateStatusMessages(result);
    }

    private void UpdateStatusMessages(ErrorOr<AppBoot.BootResult> result)
    {
        StatusMessageSecondaryColour = null;

        if (result.IsError)
        {
            StatusMessagePrimaryText   = "Error whilst booting up launcher…";
            StatusMessageSecondaryText = string.Empty;

            return;
        }

        switch (result.Value.Outcome)
        {
            case AppBoot.BootOutcome.ReadyToPlay:
                StatusMessagePrimaryText = $"Ready — {result.Value.ActiveInstallation?.Name}";

                // TODO: Update with dynamic values once screen resolution is known.
                StatusMessageSecondaryText = "Display: 1920 × 1080";

                break;

            case AppBoot.BootOutcome.CannotPlay:
                StatusMessagePrimaryText     = "Cannot launch — invalid installation";
                StatusMessageSecondaryText   = "Fix required";
                StatusMessageSecondaryColour = Brushes.Red;

                break;

            case AppBoot.BootOutcome.NoGameInstallationFound:
                StatusMessagePrimaryText   = "No installations registered";
                StatusMessageSecondaryText = string.Empty;

                break;

            case AppBoot.BootOutcome.OpenGameInstallation:
                StatusMessagePrimaryText   = "Choose an installation to open";
                StatusMessageSecondaryText = string.Empty;

                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private ViewModelBase RouteResult(AppBoot.BootResult result)
        => result.Outcome switch
        {
            AppBoot.BootOutcome.ReadyToPlay => new ReadyToPlayViewModel(result.ActiveInstallation!,
                                                                        BootAsync,
                                                                        _lifecycle,
                                                                        _dialogs,
                                                                        _mediator),
            AppBoot.BootOutcome.CannotPlay => new CannotPlayViewModel(result.ActiveInstallation!, _mediator),
            AppBoot.BootOutcome.NoGameInstallationFound => new NoGameInstallationFoundViewModel(result.LocatedCandidatePath,
                                                                                                _dialogs,
                                                                                                BootAsync),
            AppBoot.BootOutcome.OpenGameInstallation => new OpenGameInstallationViewModel(_dialogs,
                                                                                          BootAsync),
            var _ => new NoGameInstallationFoundViewModel(locatedCandidatePath: null,
                                                          _dialogs,
                                                          BootAsync)
        };
}
