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

    private readonly ILogger<MainWindowViewModel> _logger;

    private readonly IMediator _mediator;

    private readonly IMessenger _messenger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="lifecycle">Chrome service for requesting application shutdown.</param>
    /// <param name="dialogs">Chrome service for opening modeless and modal dialogues.</param>
    /// <param name="messenger">The CommunityToolkit messenger — passed to freshly-built picker grids so they can subscribe to installation-change notifications.</param>
    /// <param name="logger">Logger for unexpected boot-dispatch and picker-initialisation failures.</param>
    public MainWindowViewModel(IMediator                    mediator,
                               IApplicationLifecycle        lifecycle,
                               IDialogService               dialogs,
                               IMessenger                   messenger,
                               ILogger<MainWindowViewModel> logger)
    {
        _mediator  = mediator;
        _lifecycle = lifecycle;
        _dialogs   = dialogs;
        _messenger = messenger;
        _logger    = logger;
    }

    /// <summary>The currently active state or content view model; drives the main window's <c>ContentControl</c> via <see cref="Composition.ViewLocator" />.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(OpenInstallationPickerCommand)) ]
    [ NotifyCanExecuteChangedFor(nameof(CloseInstallationCommand)) ]
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
    [ NotifyCanExecuteChangedFor(nameof(OpenInstallationPickerCommand)) ]
    [ NotifyCanExecuteChangedFor(nameof(CloseInstallationCommand)) ]
    public partial bool IsBooting { get; set; } = true;

    [ ObservableProperty ]
    public partial string? StatusMessagePrimaryText { get; set; }

    [ ObservableProperty ]
    public partial IBrush? StatusMessageSecondaryColour { get; set; }

    [ ObservableProperty ]
    public partial string? StatusMessageSecondaryText { get; set; }

    /// <summary>The main window's width in device-independent pixels: narrow whilst booting, wide once booted. Derived from <see cref="IsBooting" /> so the two can never disagree.</summary>
    public double WindowWidth => IsBooting ? BootingWindowWidth : BootedWindowWidth;

    /// <summary>Set once the user has confirmed leaving unsaved edits, so the window's close handler does not ask a second time.</summary>
    public bool IsCloseConfirmed { get; set; }

    /// <summary>Whether the active content holds unsaved edits.</summary>
    public bool HasPendingChanges => ActiveContent is IPendingChangesGuard { HasPendingChanges: true };

    /// <summary>
    ///     Asks the active content whether the window may close (SDD §7.3.2). Used by the window's close handler. A failure is logged and keeps the window open, so
    ///     unsaved edits are never lost to an unhandled fault.
    /// </summary>
    /// <returns><see langword="true" /> when the window may close.</returns>
    public async Task<bool> ConfirmCloseAsync()
    {
        try
        {
            return await ConfirmLeaveActiveContentAsync(CancellationToken.None);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected failure whilst confirming the window close.");

            return false;
        }
    }

    [ RelayCommand ]
    private Task BootAsync(CancellationToken cancellationToken) => RunBootAsync(installationId: null, cancellationToken);

    // Pointed boot (SDD §7.2.7 — the picker's Open button): boots the given installation directly, bypassing the startup preference and default resolution.
    private Task OpenInstallationAsync(Guid installationId, CancellationToken cancellationToken) => RunBootAsync(installationId, cancellationToken);

    // File → "Installation Manager…" (SDD §9.10): opens the modal manager, then refreshes whichever state is active — but only when the manager reports a change — so
    // installations added there are reflected immediately without an unnecessary reload or locator rescan when the user opened the manager and changed nothing. Reloads
    // the picker grid when OpenGameInstallationViewModel is active, or re-runs the normal boot when NoGameInstallationFoundViewModel is active (mirroring that state's own
    // post-Add reboot), so a first installation added via the manager is picked up without requiring a restart. Play and CannotPlay need no refresh — neither displays the
    // installation list, and the Manager's own Info/Edit/Delete/Fix commands are still stubs.
    [ RelayCommand ]
    private async Task ManageInstallationsAsync(CancellationToken cancellationToken)
    {
        bool changed = await _dialogs.ShowInstallationManagerAsync();

        if (!changed)
        {
            return;
        }

        if (ActiveContent is OpenGameInstallationViewModel picker)
        {
            await picker.Grid.LoadAsync(cancellationToken);
        }
        else if (ActiveContent is NoGameInstallationFoundViewModel)
        {
            await RunBootAsync(installationId: null, cancellationToken);
        }
    }

    // File → "Open Installation…" (SDD §9.10). Interim behaviour until a later milestone implements the SDD's "opens the Installation Manager focused on Open": switches
    // the main window to the picker from any booted state, so an installation can be opened without changing the startup preference. Disabled whilst already on the picker.
    // Asks about unsaved INI edits first.
    [ RelayCommand(CanExecute = nameof(CanOpenInstallationPicker)) ]
    private async Task OpenInstallationPickerAsync(CancellationToken cancellationToken)
    {
        if (!await ConfirmLeaveActiveContentAsync(cancellationToken))
        {
            return;
        }

        await ShowPickerAsync(cancellationToken);
    }

    private bool CanOpenInstallationPicker() => !IsBooting && ActiveContent is not OpenGameInstallationViewModel;

    // File → "Close Installation" (SDD §9.10): closes the open installation by returning to the picker — the state with no installation open (SDD §9.1). Disabled when no
    // installation is open, i.e. whenever the Play view (Ready to Play / Cannot Play) is not the active content. Asks about unsaved INI edits first.
    [ RelayCommand(CanExecute = nameof(CanCloseInstallation)) ]
    private async Task CloseInstallationAsync(CancellationToken cancellationToken)
    {
        if (!await ConfirmLeaveActiveContentAsync(cancellationToken))
        {
            return;
        }

        await ShowPickerAsync(cancellationToken);
    }

    private bool CanCloseInstallation() => !IsBooting && ActiveContent is PlayViewModel;

    // File → "Exit" (SDD §9.10). Asks about unsaved INI edits first; on approval marks the close as confirmed so MainWindow's close handler does not ask again.
    [ RelayCommand ]
    private async Task ExitAsync(CancellationToken cancellationToken)
    {
        if (!await ConfirmLeaveActiveContentAsync(cancellationToken))
        {
            return;
        }

        IsCloseConfirmed = true;

        _lifecycle.RequestShutdown();
    }

    private Task<bool> ConfirmLeaveActiveContentAsync(CancellationToken cancellationToken)
        => ActiveContent is IPendingChangesGuard guard ? guard.ConfirmLeaveAsync(cancellationToken) : Task.FromResult(true);

    // Shared by Open Installation… and Close Installation. Loads the fresh picker's grid before swapping it in (the outgoing state view model is then disposed by
    // OnActiveContentChanged), mirroring RunBootAsync's load-then-show order. A load failure is logged and leaves the current state untouched.
    private async Task ShowPickerAsync(CancellationToken cancellationToken)
    {
        OpenGameInstallationViewModel picker = CreatePicker();

        try
        {
            await picker.InitialiseAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            picker.Dispose();

            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogError(ex, "Unexpected failure whilst loading the installation picker.");

            StatusMessagePrimaryText = "Could not load the installation list…";

            return;
        }

        ActiveContent = picker;

        SetPickerStatusMessages();
    }

    private async Task RunBootAsync(Guid? installationId, CancellationToken cancellationToken)
    {
        ActiveContent              = new LookingForZooTycoonViewModel();
        IsBooting                  = true;
        StatusMessagePrimaryText   = installationId is null ? "Discovering installations…" : "Opening installation…";
        StatusMessageSecondaryText = "Please wait…";

        ViewModelBase? content = null;

        try
        {
            ErrorOr<AppBoot.BootResult> result = await _mediator.Send(new AppBoot.BootCommand(installationId), cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Boot failed for installation {InstallationId}: {Errors}",
                                   installationId,
                                   string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}")));

                ShowBootFailure(builtContent: null);

                return;
            }

            content = RouteResult(result.Value);

            if (content is OpenGameInstallationViewModel picker)
            {
                await picker.InitialiseAsync(cancellationToken);
            }

            IsBooting     = false;
            ActiveContent = content;

            // Handed over to ActiveContent above: it is now live and must not be disposed again if something below still throws (e.g. UpdateStatusMessages).
            content = null;

            UpdateStatusMessages(result.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Disposes content built but never handed to ActiveContent (e.g. the picker's grid already registered with the messenger before cancellation was
            // observed). IsBooting is left as-is: nothing cancels a boot today — if something ever does, the window would stay on the Looking state, so revisit this.
            (content as IDisposable)?.Dispose();

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure whilst booting the launcher for installation {InstallationId}.", installationId);

            ShowBootFailure(content);
        }
    }

    // Shown both when the boot dispatch reports an ErrorOr error and when it or the picker's initialisation throws unexpectedly, so the two failure paths share one
    // implementation. Disposes builtContent when a state view model was constructed but never shown as ActiveContent — e.g. the picker's grid already registered with the
    // messenger before InitialiseAsync threw.
    private void ShowBootFailure(ViewModelBase? builtContent)
    {
        (builtContent as IDisposable)?.Dispose();

        IsBooting     = false;
        ActiveContent = new NoGameInstallationFoundViewModel(locatedCandidatePath: null, _dialogs, BootAsync);

        StatusMessagePrimaryText     = "Error whilst booting up launcher…";
        StatusMessageSecondaryText   = string.Empty;
        StatusMessageSecondaryColour = null;
    }

    private void UpdateStatusMessages(AppBoot.BootResult result)
    {
        StatusMessageSecondaryColour = null;

        switch (result.Outcome)
        {
            case AppBoot.BootOutcome.ReadyToPlay:
                StatusMessagePrimaryText = $"Ready — {result.ActiveInstallation?.Name}";

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
                SetPickerStatusMessages();

                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void SetPickerStatusMessages()
    {
        StatusMessagePrimaryText     = "Choose an installation to open";
        StatusMessageSecondaryText   = string.Empty;
        StatusMessageSecondaryColour = null;
    }

    // Single construction point for the picker so boot routing and the File menu build it identically: a fresh grid per picker (the picker owns and disposes it) and the
    // pointed-boot callback for its Open command.
    private OpenGameInstallationViewModel CreatePicker()
        => new(new InstallationGridViewModel(_mediator, _messenger), _dialogs, OpenInstallationAsync);

    private ViewModelBase RouteResult(AppBoot.BootResult result)
        => result.Outcome switch
        {
            AppBoot.BootOutcome.ReadyToPlay => new PlayViewModel(result.ActiveInstallation!,
                                                                 canPlay: true,
                                                                 ct => RunBootAsync(result.ActiveInstallation!.Id, ct),
                                                                 _lifecycle,
                                                                 _dialogs,
                                                                 _mediator,
                                                                 iniErrorMessage: result.IniErrorMessage),
            AppBoot.BootOutcome.CannotPlay => new PlayViewModel(result.ActiveInstallation!,
                                                                canPlay: false,
                                                                ct => RunBootAsync(result.ActiveInstallation!.Id, ct),
                                                                _lifecycle,
                                                                _dialogs,
                                                                _mediator,
                                                                iniErrorMessage: result.IniErrorMessage),
            AppBoot.BootOutcome.NoGameInstallationFound => new NoGameInstallationFoundViewModel(result.LocatedCandidatePath,
                                                                                                _dialogs,
                                                                                                BootAsync),
            AppBoot.BootOutcome.OpenGameInstallation => CreatePicker(),
            var _ => new NoGameInstallationFoundViewModel(locatedCandidatePath: null,
                                                          _dialogs,
                                                          BootAsync)
        };
}
