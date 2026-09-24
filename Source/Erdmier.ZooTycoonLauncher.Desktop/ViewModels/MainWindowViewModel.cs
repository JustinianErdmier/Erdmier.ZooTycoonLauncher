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

    private readonly IMessenger _messenger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="lifecycle">Chrome service for requesting application shutdown.</param>
    /// <param name="dialogs">Chrome service for opening modeless and modal dialogues.</param>
    /// <param name="messenger">The CommunityToolkit messenger — passed to freshly-built picker grids so they can subscribe to installation-change notifications.</param>
    public MainWindowViewModel(IMediator mediator, IApplicationLifecycle lifecycle, IDialogService dialogs, IMessenger messenger)
    {
        _mediator  = mediator;
        _lifecycle = lifecycle;
        _dialogs   = dialogs;
        _messenger = messenger;
    }

    /// <summary>The currently active state or content view model; drives the main window's <c>ContentControl</c> via <see cref="Composition.ViewLocator" />.</summary>
    [ ObservableProperty ]
    public partial object? ActiveContent { get; set; }

    // Disposes the outgoing state view model when it holds disposable resources (currently only OpenGameInstallationViewModel's grid), so it unregisters from the
    // messenger as soon as the main window navigates away from it.
    partial void OnActiveContentChanged(object? oldValue, object? newValue)
    {
        if (oldValue is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

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
    private Task BootAsync(CancellationToken cancellationToken) => RunBootAsync(installationId: null, cancellationToken);

    // Pointed boot (SDD §7.2.7 — the picker's Open button): boots the given installation directly, bypassing the startup preference and default resolution.
    private Task OpenInstallationAsync(Guid installationId, CancellationToken cancellationToken) => RunBootAsync(installationId, cancellationToken);

    // File → "Installation Manager…" (SDD §9.10): opens the modal manager, then refreshes whichever state is active so installations added or removed there are reflected
    // immediately — reloads the picker grid when OpenGameInstallationViewModel is active, or re-runs the normal boot when NoGameInstallationFoundViewModel is active
    // (mirroring that state's own post-Add reboot), so a first installation added via the manager is picked up without requiring a restart. Play and CannotPlay need no
    // refresh — neither displays the installation list, and the Manager's own Info/Edit/Delete/Fix commands are still stubs.
    [ RelayCommand ]
    private async Task ManageInstallationsAsync(CancellationToken cancellationToken)
    {
        await _dialogs.ShowInstallationManagerAsync();

        if (ActiveContent is OpenGameInstallationViewModel picker)
        {
            await picker.Grid.LoadAsync(cancellationToken);
        }
        else if (ActiveContent is NoGameInstallationFoundViewModel)
        {
            await RunBootAsync(installationId: null, cancellationToken);
        }
    }

    private async Task RunBootAsync(Guid? installationId, CancellationToken cancellationToken)
    {
        ActiveContent              = new LookingForZooTycoonViewModel();
        IsBooting                  = true;
        StatusMessagePrimaryText   = "Discovering installations…";
        StatusMessageSecondaryText = "Please wait…";

        // await Task.Delay(TimeSpan.FromSeconds(seconds: 2), cancellationToken);

        ErrorOr<AppBoot.BootResult> result = await _mediator.Send(new AppBoot.BootCommand(installationId), cancellationToken);

        IsBooting = false;

        ViewModelBase content = result.IsError
                                    ? new NoGameInstallationFoundViewModel(locatedCandidatePath: null, _dialogs, BootAsync)
                                    : RouteResult(result.Value);

        if (content is OpenGameInstallationViewModel picker)
        {
            await picker.InitialiseAsync(cancellationToken);
        }

        ActiveContent = content;

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
            AppBoot.BootOutcome.ReadyToPlay => new PlayViewModel(result.ActiveInstallation!,
                                                                 canPlay: true,
                                                                 ct => RunBootAsync(result.ActiveInstallation!.Id, ct),
                                                                 _lifecycle,
                                                                 _dialogs,
                                                                 _mediator),
            AppBoot.BootOutcome.CannotPlay => new PlayViewModel(result.ActiveInstallation!,
                                                                canPlay: false,
                                                                ct => RunBootAsync(result.ActiveInstallation!.Id, ct),
                                                                _lifecycle,
                                                                _dialogs,
                                                                _mediator),
            AppBoot.BootOutcome.NoGameInstallationFound => new NoGameInstallationFoundViewModel(result.LocatedCandidatePath,
                                                                                                _dialogs,
                                                                                                BootAsync),
            AppBoot.BootOutcome.OpenGameInstallation => new OpenGameInstallationViewModel(new InstallationGridViewModel(_mediator, _messenger),
                                                                                          _dialogs,
                                                                                          OpenInstallationAsync),
            var _ => new NoGameInstallationFoundViewModel(locatedCandidatePath: null,
                                                          _dialogs,
                                                          BootAsync)
        };
}
