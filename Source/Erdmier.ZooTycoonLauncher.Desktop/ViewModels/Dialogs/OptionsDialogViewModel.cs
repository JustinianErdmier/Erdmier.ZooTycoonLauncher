namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     View model for the modal Options dialogue (<c>File → Options…</c>): what the launcher opens at start-up and whether it closes after the game starts. Loads the
///     stored values in <see cref="InitialiseAsync" />; <see cref="OkCommand" /> saves both in one <see cref="SaveLauncherOptionsCommand" /> and raises
///     <see cref="CloseRequested" />, <see cref="CancelCommand" /> raises it without saving.
/// </summary>
public sealed partial class OptionsDialogViewModel : ViewModelBase
{
    private const string SaveFailedTitle = "Cannot Save Options";

    private readonly IDialogService _dialogs;

    private readonly ILogger<OptionsDialogViewModel> _logger;

    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">The dialogue service, used to report a failed save.</param>
    /// <param name="logger">Logger for load and save failures.</param>
    public OptionsDialogViewModel(IMediator mediator, IDialogService dialogs, ILogger<OptionsDialogViewModel> logger)
    {
        _mediator = mediator;
        _dialogs  = dialogs;
        _logger   = logger;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public OptionsDialogViewModel()
        : this(null!, null!, NullLogger<OptionsDialogViewModel>.Instance)
    { }

    /// <summary>Whether a user-initiated close (the title-bar button) is allowed: <see langword="false" /> while a save is in flight.</summary>
    public bool CanClose => !IsBusy;

    /// <summary>Whether the launcher closes itself once the game has started.</summary>
    [ ObservableProperty ]
    public partial bool CloseAfterGameLaunch { get; set; }

    /// <summary>The load error, or <see langword="null" /> when the options loaded. Save failures are shown in an error box instead.</summary>
    [ ObservableProperty ]
    public partial string? ErrorMessage { get; set; }

    /// <summary><see langword="true" /> while a save is in flight.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(OkCommand)) ]
    [ NotifyCanExecuteChangedFor(nameof(CancelCommand)) ]
    [ NotifyPropertyChangedFor(nameof(CanClose)) ]
    public partial bool IsBusy { get; set; }

    /// <summary><see langword="true" /> once the stored options have loaded; OK stays disabled until then, so a failed load never overwrites the stored values.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(OkCommand)) ]
    public partial bool IsLoaded { get; set; }

    /// <summary>What the launcher opens at start-up. The four <c>Is…</c> radio properties are views of this value.</summary>
    [ ObservableProperty ]
    [ NotifyPropertyChangedFor(nameof(IsDefaultInstallation)) ]
    [ NotifyPropertyChangedFor(nameof(IsLastPlayedInstallation)) ]
    [ NotifyPropertyChangedFor(nameof(IsLastOpenedInstallation)) ]
    [ NotifyPropertyChangedFor(nameof(IsNoInstallation)) ]
    public partial LauncherStartupPreference StartupPreference { get; set; } = LauncherStartupPreference.DefaultInstallation;

    /// <summary>Radio: restore the default installation. Setting <see langword="false" /> is ignored — the radio group clears the previous choice that way.</summary>
    public bool IsDefaultInstallation
    {
        get => StartupPreference == LauncherStartupPreference.DefaultInstallation;
        set => Choose(value, LauncherStartupPreference.DefaultInstallation);
    }

    /// <summary>Radio: restore the installation last played. Setting <see langword="false" /> is ignored.</summary>
    public bool IsLastPlayedInstallation
    {
        get => StartupPreference == LauncherStartupPreference.LastPlayedInstallation;
        set => Choose(value, LauncherStartupPreference.LastPlayedInstallation);
    }

    /// <summary>Radio: restore the installation last opened in the launcher. Setting <see langword="false" /> is ignored.</summary>
    public bool IsLastOpenedInstallation
    {
        get => StartupPreference == LauncherStartupPreference.LastOpenedInstallation;
        set => Choose(value, LauncherStartupPreference.LastOpenedInstallation);
    }

    /// <summary>Radio: open no installation and show the picker. Setting <see langword="false" /> is ignored.</summary>
    public bool IsNoInstallation
    {
        get => StartupPreference == LauncherStartupPreference.NoInstallation;
        set => Choose(value, LauncherStartupPreference.NoInstallation);
    }

    /// <summary>Loads the stored options. Must be awaited by the dialogue service before the window is shown.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_mediator is null)
        {
            return;
        }

        try
        {
            ErrorOr<LauncherOptions> result = await _mediator.Send(new GetLauncherOptionsQuery(), cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Failed to read the launcher options: {Error}", result.FirstError.Description);

                ErrorMessage = InstallationDialogMessages.UnexpectedFailure;

                return;
            }

            StartupPreference    = result.Value.StartupPreference;
            CloseAfterGameLaunch = result.Value.CloseAfterGameLaunch;
            IsLoaded             = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to read the launcher options.");

            ErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteOk)) ]
    private async Task OkAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;

        try
        {
            ErrorOr<Success> result = await _mediator.Send(new SaveLauncherOptionsCommand(StartupPreference, CloseAfterGameLaunch), cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Failed to save the launcher options: {Error}", result.FirstError.Description);

                await _dialogs.ShowErrorAsync(SaveFailedTitle, result.FirstError.Description);

                return;
            }

            // Release the busy flag first so CanClose is true when the window's Closing handler runs in response to CloseRequested.
            IsBusy = false;

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to save the launcher options.");

            await _dialogs.ShowErrorAsync(SaveFailedTitle, InstallationDialogMessages.UnexpectedFailure);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Closes the dialogue without saving. Unavailable while a save is in flight.</summary>
    [ RelayCommand(CanExecute = nameof(CanCancel)) ]
    private void Cancel()
    {
        // Execute() does not consult CanExecute, so the guard is repeated here.
        if (IsBusy)
        {
            return;
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanCancel() => !IsBusy;

    private bool CanExecuteOk() => IsLoaded && !IsBusy;

    private void Choose(bool isChecked, LauncherStartupPreference preference)
    {
        if (isChecked)
        {
            StartupPreference = preference;
        }
    }

    /// <summary>Raised when the dialogue should close — after a successful save, or on Cancel.</summary>
    public event EventHandler? CloseRequested;
}
