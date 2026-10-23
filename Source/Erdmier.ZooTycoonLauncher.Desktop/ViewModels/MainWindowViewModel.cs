using Avalonia.Media;

using AppBoot = Erdmier.ZooTycoonLauncher.Application.Boot;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels;

/// <summary>The main window's view model. Dispatches <c>BootCommand</c> when loaded and routes the result to the active state view model via <c>ActiveContent</c> (SDD §9.2).</summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private const double BootedWindowWidth = 720;

    private const double BootingWindowWidth = 480;

    private const string SaveOptionsFailedTitle = "Cannot Save Options";

    private readonly IDialogService _dialogs;

    private readonly ILogger<InstallationGridViewModel> _gridLogger;

    private readonly IApplicationLifecycle _lifecycle;

    private readonly ILogger<MainWindowViewModel> _logger;

    private readonly IMediator _mediator;

    private readonly IMessenger _messenger;

    private readonly IIniEditorStyleState _styleState;

    private readonly ILogger<IniEditorViewModel> _iniEditorLogger;

    private readonly ILogger<IniPlaceholderViewModel> _iniPlaceholderLogger;

    private readonly ILogger<IniConfigTabViewModel> _iniTabLogger;

    private readonly ILogger<PlayViewModel> _playLogger;

    private bool _hasLoadedStyle;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="lifecycle">Chrome service for requesting application shutdown.</param>
    /// <param name="dialogs">Chrome service for opening modeless and modal dialogues.</param>
    /// <param name="messenger">The CommunityToolkit messenger — passed to freshly-built picker grids so they can subscribe to installation-change notifications.</param>
    /// <param name="logger">Logger for unexpected boot-dispatch and picker-initialisation failures.</param>
    /// <param name="gridLogger">Logger passed to freshly-built picker grids so their message-driven reload failures are recorded.</param>
    /// <param name="playLogger">Logger passed to freshly-built <see cref="PlayViewModel" /> instances so their last-resort catches are recorded.</param>
    /// <param name="iniTabLogger">Logger passed on to the Game Config tab of each <see cref="PlayViewModel" />.</param>
    /// <param name="iniEditorLogger">Logger passed on to the INI editor of each <see cref="PlayViewModel" />.</param>
    /// <param name="iniPlaceholderLogger">Logger passed on to the INI tab's No INI placeholder of each <see cref="PlayViewModel" />.</param>
    /// <param name="styleState">The shared INI editor style, passed on to the Game Config tab of each <see cref="PlayViewModel" />.</param>
    public MainWindowViewModel(IMediator                          mediator,
                               IApplicationLifecycle              lifecycle,
                               IDialogService                     dialogs,
                               IMessenger                         messenger,
                               ILogger<MainWindowViewModel>       logger,
                               ILogger<InstallationGridViewModel> gridLogger,
                               ILogger<PlayViewModel>             playLogger,
                               ILogger<IniConfigTabViewModel>     iniTabLogger,
                               ILogger<IniEditorViewModel>        iniEditorLogger,
                               ILogger<IniPlaceholderViewModel>   iniPlaceholderLogger,
                               IIniEditorStyleState               styleState)
    {
        _mediator   = mediator;
        _lifecycle  = lifecycle;
        _dialogs    = dialogs;
        _messenger  = messenger;
        _logger     = logger;
        _gridLogger = gridLogger;
        _playLogger = playLogger;

        _iniTabLogger         = iniTabLogger;
        _iniEditorLogger      = iniEditorLogger;
        _iniPlaceholderLogger = iniPlaceholderLogger;
        _styleState           = styleState;

        // The view model lives as long as the application, as does the style state, so the subscription is never released.
        _styleState.StyleChanged += (_, _) => RaiseStyleMenuChecks();

        RaiseStyleMenuChecks();
    }

    /// <summary>The currently active state or content view model; drives the main window's <c>ContentControl</c> via <see cref="Composition.ViewLocator" />.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(OpenInstallationPickerCommand)) ]
    [ NotifyCanExecuteChangedFor(nameof(CloseInstallationCommand)) ]
    public partial object? ActiveContent { get; set; }

    // Disposes the outgoing state view model when it holds disposable resources (the picker's grid, and PlayViewModel, whose Game Config tab follows the
    // shared editor style), so it unregisters from the messenger and the style state as soon as the main window navigates away from it.
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

    /// <summary><see langword="true" /> when the secondary status message reports an error; the view shows it in the error colour.</summary>
    [ ObservableProperty ]
    public partial bool IsStatusError { get; set; }

    [ ObservableProperty ]
    public partial string? StatusMessageSecondaryText { get; set; }

    /// <summary>The main window's width in device-independent pixels: narrow whilst booting, wide once booted. Derived from <see cref="IsBooting" /> so the two can never disagree.</summary>
    public double WindowWidth => IsBooting ? BootingWindowWidth : BootedWindowWidth;

    /// <summary>Set once the user has confirmed leaving unsaved edits, so the window's close handler does not ask a second time.</summary>
    public bool IsCloseConfirmed { get; set; }

    /// <summary>Whether the active content holds unsaved edits.</summary>
    public bool HasPendingChanges => ActiveContent is IPendingChangesGuard { HasPendingChanges: true };

    /// <summary>Asks the active content whether the window may close (SDD §7.3.2). Used by the window's close handler.</summary>
    /// <returns>
    ///     <see langword="true" /> when the window may close. A failure whilst confirming is logged and treated as <see langword="false" />, so the window stays open and the
    ///     edits are kept.
    /// </returns>
    public Task<bool> ConfirmCloseAsync() => ConfirmLeaveActiveContentAsync(CancellationToken.None);

    [ RelayCommand ]
    private Task BootAsync(CancellationToken cancellationToken) => RunBootAsync(installationId: null, cancellationToken);

    // Pointed boot (SDD §7.2.7 — the picker's Open button): boots the given installation directly, bypassing the startup preference and default resolution.
    private Task OpenInstallationAsync(Guid installationId, CancellationToken cancellationToken) => RunBootAsync(installationId, cancellationToken);

    // Two of the manager's entry points route here: the File menu's "Installation Manager…" item, and Cannot Play's "Open Installation Manager…" button (the picker's
    // own Manage button opens the manager directly, since the picker needs no follow-up — see its ManageAsync). Acts only when the manager reports a change. Three
    // cases: a Play state (Ready to Play or Cannot Play) re-verifies the open installation with a pointed boot — a rename shows, Cannot Play becomes Ready after a fix,
    // and a deleted installation falls back to the normal resolution (SDD §7.2.4); No Game Installation Found re-runs the normal boot, so a first installation added
    // via the manager is picked up without requiring a restart; the picker needs nothing, since its grid refreshes itself from the change messages.
    [ RelayCommand ]
    private async Task ManageInstallationsAsync(CancellationToken cancellationToken)
    {
        bool changed = await _dialogs.ShowInstallationManagerAsync();

        if (!changed)
        {
            return;
        }

        switch (ActiveContent)
        {
            // Pointed boot of the open installation. The boot rebuilds the Play view, so unsaved INI edits are confirmed first (SDD §7.3.2); declining keeps the edits and
            // the view as it is.
            case PlayViewModel play:
                if (!await ConfirmLeaveActiveContentAsync(cancellationToken))
                {
                    break;
                }

                await RunBootAsync(play.InstallationId, cancellationToken);

                break;

            // Normal boot.
            case NoGameInstallationFoundViewModel:
                await RunBootAsync(installationId: null, cancellationToken);

                break;
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

    // File → "Options…": opens the modal Options dialogue. Available in every booted state (the menu is hidden whilst booting) and needs no unsaved-edit guard, since it
    // changes nothing about the open installation.
    [ RelayCommand ]
    private Task OpenOptionsAsync() => _dialogs.ShowOptionsAsync();

    /// <summary>
    ///     View → Config layout → Friendly style: whether the menu item is checked. Bound two-way because the radio item writes its own check on click; it follows the INI editor
    ///     style in force, including changes made elsewhere such as Options.
    /// </summary>
    [ ObservableProperty ]
    public partial bool IsFriendlyStyle { get; set; }

    /// <summary>View → Config layout → INI style: whether the menu item is checked. Bound two-way like <see cref="IsFriendlyStyle" />.</summary>
    [ ObservableProperty ]
    public partial bool IsIniStyle { get; set; }

    /// <summary><see langword="true" /> while a style choice is being stored; both style commands are disabled meanwhile so a second choice is not lost.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(ChooseFriendlyStyleCommand)) ]
    [ NotifyCanExecuteChangedFor(nameof(ChooseIniStyleCommand)) ]
    public partial bool IsStyleSaving { get; set; }

    [ RelayCommand(CanExecute = nameof(CanChooseStyle)) ]
    private Task ChooseFriendlyStyleAsync(CancellationToken cancellationToken) => ChooseStyleAsync(IniEditorStyle.Friendly, cancellationToken);

    [ RelayCommand(CanExecute = nameof(CanChooseStyle)) ]
    private Task ChooseIniStyleAsync(CancellationToken cancellationToken) => ChooseStyleAsync(IniEditorStyle.Ini, cancellationToken);

    private bool CanChooseStyle() => !IsStyleSaving;

    // View → Config layout (spec §6.6): stores the chosen style through the same load-then-save path as Options, changing only the style, and applies it to the shared state
    // only once the save has succeeded. A failed save is reported and changes nothing. A radio item writes its own check on click, so afterwards both check properties are
    // assigned from the real style: the binding then sees a changed value and pushes it back, which re-raising an unchanged value would not do.
    private async Task ChooseStyleAsync(IniEditorStyle style, CancellationToken cancellationToken)
    {
        IsStyleSaving = true;

        try
        {
            ErrorOr<LauncherOptions> loaded = await _mediator.Send(new GetLauncherOptionsQuery(), cancellationToken);

            if (loaded.IsError)
            {
                _logger.LogWarning("Failed to read the launcher options: {Error}", loaded.FirstError.Description);

                await _dialogs.ShowErrorAsync(SaveOptionsFailedTitle, loaded.FirstError.Description);

                return;
            }

            LauncherOptions options = loaded.Value;

            // Compared with what is stored, not with the in-memory style, so a stale in-memory value can never swallow the choice. Choosing what is already stored saves nothing
            // but brings the shared state into line with it.
            if (options.EditorStyle == style)
            {
                _styleState.Style = style;

                return;
            }

            ErrorOr<Success> saved = await _mediator.Send(new SaveLauncherOptionsCommand(options.StartupPreference, options.CloseAfterGameLaunch, style), cancellationToken);

            if (saved.IsError)
            {
                _logger.LogWarning("Failed to save the launcher options: {Error}", saved.FirstError.Description);

                await _dialogs.ShowErrorAsync(SaveOptionsFailedTitle, saved.FirstError.Description);

                return;
            }

            _styleState.Style = style;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to save the launcher options.");

            await _dialogs.ShowErrorAsync(SaveOptionsFailedTitle, InstallationDialogMessages.UnexpectedFailure);
        }
        finally
        {
            IsStyleSaving = false;

            RaiseStyleMenuChecks();
        }
    }

    private void RaiseStyleMenuChecks()
    {
        IsFriendlyStyle = _styleState.Style == IniEditorStyle.Friendly;
        IsIniStyle      = _styleState.Style == IniEditorStyle.Ini;
    }

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

    // Guards every exit from the active content — Exit, Close Installation, Open Installation…, the window's close handler (via ConfirmCloseAsync), and the Installation
    // Manager's reboot of the open installation. A failure whilst confirming unsaved INI changes is logged and treated as "may not leave", so unsaved edits are never lost
    // to an unhandled fault.
    private async Task<bool> ConfirmLeaveActiveContentAsync(CancellationToken cancellationToken)
    {
        if (ActiveContent is not IPendingChangesGuard guard)
        {
            return true;
        }

        try
        {
            return await guard.ConfirmLeaveAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected failure whilst confirming unsaved INI changes.");

            return false;
        }
    }

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

    // The boot does not otherwise load the launcher options, so the stored editor style is read once here, after the Looking state and status text are set and before any editor
    // exists: the View menu's bullet is then right in every state, whether or not an INI editor ever opens. A failure keeps the style already in force; the Game Config tab's own lazy load stays harmless beside this one.
    private async Task LoadStoredStyleAsync(CancellationToken cancellationToken)
    {
        if (_hasLoadedStyle)
        {
            return;
        }

        _hasLoadedStyle = true;

        // Anything that sets the state while the query is in flight (an Options save, a menu choice) is newer than what the query reads, so it wins.
        int versionBeforeQuery = _styleState.Version;

        try
        {
            ErrorOr<LauncherOptions> options = await _mediator.Send(new GetLauncherOptionsQuery(), cancellationToken);

            if (options.IsError)
            {
                _logger.LogError("Could not read the stored INI editor style at start-up: {Error}", options.FirstError.Description);
            }
            else if (options.Value is { } value
                     && _styleState.Version == versionBeforeQuery)
            {
                _styleState.Style = value.EditorStyle;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not read the stored INI editor style at start-up.");
        }
    }

    private async Task RunBootAsync(Guid? installationId, CancellationToken cancellationToken, int initialTabIndex = 0)
    {
        ActiveContent              = new LookingForZooTycoonViewModel();
        IsBooting                  = true;
        StatusMessagePrimaryText   = installationId is null ? "Discovering installations…" : "Opening installation…";
        StatusMessageSecondaryText = "Please wait…";

        await LoadStoredStyleAsync(cancellationToken);

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

            content = RouteResult(result.Value, initialTabIndex);

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

        StatusMessagePrimaryText   = "Error whilst booting up launcher…";
        StatusMessageSecondaryText = string.Empty;
        IsStatusError              = false;
    }

    private void UpdateStatusMessages(AppBoot.BootResult result)
    {
        IsStatusError = false;

        switch (result.Outcome)
        {
            case AppBoot.BootOutcome.ReadyToPlay:
                StatusMessagePrimaryText = $"Ready — {result.ActiveInstallation?.Name}";

                // TODO: Update with dynamic values once screen resolution is known.
                StatusMessageSecondaryText = "Display: 1920 × 1080";

                break;

            case AppBoot.BootOutcome.CannotPlay:
                StatusMessagePrimaryText   = "Cannot launch — invalid installation";
                StatusMessageSecondaryText = "Fix required";
                IsStatusError              = true;

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
        StatusMessagePrimaryText   = "Choose an installation to open";
        StatusMessageSecondaryText = string.Empty;
        IsStatusError              = false;
    }

    // Single construction point for the picker so boot routing and the File menu build it identically: a fresh grid per picker (the picker owns and disposes it) and the
    // pointed-boot callback for its Open command.
    private OpenGameInstallationViewModel CreatePicker()
        => new(new InstallationGridViewModel(_mediator, _messenger, _gridLogger), _dialogs, OpenInstallationAsync);

    private ViewModelBase RouteResult(AppBoot.BootResult result, int initialTabIndex)
        => result.Outcome switch
        {
            AppBoot.BootOutcome.ReadyToPlay => new PlayViewModel(result.ActiveInstallation!,
                                                                 canPlay: true,
                                                                 ct => RunBootAsync(result.ActiveInstallation!.Id, ct),
                                                                 ManageInstallationsAsync,
                                                                 _lifecycle,
                                                                 _dialogs,
                                                                 _mediator,
                                                                 _playLogger,
                                                                 _iniTabLogger,
                                                                 _iniEditorLogger,
                                                                 _iniPlaceholderLogger,
                                                                 ct => RunBootAsync(result.ActiveInstallation!.Id, ct, PlayViewModel.IniConfigTabIndex),
                                                                 _styleState,
                                                                 iniErrorMessage: result.IniErrorMessage,
                                                                 writeAccess: result.WriteAccess,
                                                                 initialTabIndex: initialTabIndex),
            AppBoot.BootOutcome.CannotPlay => new PlayViewModel(result.ActiveInstallation!,
                                                                canPlay: false,
                                                                ct => RunBootAsync(result.ActiveInstallation!.Id, ct),
                                                                ManageInstallationsAsync,
                                                                _lifecycle,
                                                                _dialogs,
                                                                _mediator,
                                                                _playLogger,
                                                                _iniTabLogger,
                                                                _iniEditorLogger,
                                                                _iniPlaceholderLogger,
                                                                ct => RunBootAsync(result.ActiveInstallation!.Id, ct, PlayViewModel.IniConfigTabIndex),
                                                                _styleState,
                                                                iniErrorMessage: result.IniErrorMessage,
                                                                writeAccess: result.WriteAccess,
                                                                initialTabIndex: initialTabIndex),
            AppBoot.BootOutcome.NoGameInstallationFound => new NoGameInstallationFoundViewModel(result.LocatedCandidatePath,
                                                                                                _dialogs,
                                                                                                BootAsync),
            AppBoot.BootOutcome.OpenGameInstallation => CreatePicker(),
            var _ => new NoGameInstallationFoundViewModel(locatedCandidatePath: null,
                                                          _dialogs,
                                                          BootAsync)
        };
}
