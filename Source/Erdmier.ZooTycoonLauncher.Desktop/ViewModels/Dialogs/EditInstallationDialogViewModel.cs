namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     View model for the Edit Installation modal (SDD §7.2.3, §9.5). Hosts the shared <see cref="InstallationFormViewModel" /> in edit mode — Name editable, Folder
///     read-only without Browse, Mark as default locked when the installation is already the default — and dispatches <see cref="UpdateInstallationCommand" /> on Save.
///     Raises <see cref="CloseRequested" /> with <see langword="true" /> after a successful save, or <see langword="false" /> on Cancel.
/// </summary>
public sealed partial class EditInstallationDialogViewModel : ViewModelBase
{
    private readonly ILogger<EditInstallationDialogViewModel> _logger;

    private readonly IMediator _mediator;

    private Guid _installationId;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">The dialogue service — passed to the form (Browse is hidden in edit mode).</param>
    /// <param name="logger">Logger for unexpected load and save failures.</param>
    public EditInstallationDialogViewModel(IMediator mediator, IDialogService dialogs, ILogger<EditInstallationDialogViewModel> logger)
    {
        _mediator = mediator;
        _logger   = logger;

        Form = new InstallationFormViewModel(dialogs)
        {
            IsFolderReadOnly = true,
            IsBrowseVisible  = false
        };

        Form.PropertyChanged += OnFormPropertyChanged;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public EditInstallationDialogViewModel()
        : this(null!, null!, NullLogger<EditInstallationDialogViewModel>.Instance)
    { }

    /// <summary>The shared Name / Folder / Default form, in edit mode. Bound to <c>InstallationFormView.DataContext</c>.</summary>
    public InstallationFormViewModel Form { get; }

    /// <summary><see langword="true" /> while a save is in flight.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary><see langword="true" /> once the installation has loaded; Save stays disabled until then (and for good if it no longer exists).</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    public partial bool IsLoaded { get; set; }

    /// <summary>Loads the installation into the form. Must be awaited by the dialogue service before the window is shown.</summary>
    /// <param name="installationId">The installation to edit.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task InitialiseAsync(Guid installationId, CancellationToken cancellationToken = default)
    {
        _installationId = installationId;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_mediator is null)
        {
            return;
        }

        try
        {
            ErrorOr<InstallationSummary> result = await _mediator.Send(new GetInstallationByIdQuery(installationId), cancellationToken);

            if (result.IsError)
            {
                Form.ErrorMessage = InstallationDialogMessages.InstallationMissing;

                return;
            }

            Form.Name = result.Value.Name;
            Form.Path = result.Value.Path;

            if (result.Value.IsDefault)
            {
                Form.LockDefault();
            }

            IsLoaded = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to load installation {InstallationId} for editing.", installationId);

            Form.ErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteSave)) ]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        IsBusy            = true;
        Form.ErrorMessage = null;

        try
        {
            ErrorOr<Success> result =
                await _mediator.Send(new UpdateInstallationCommand(_installationId, Form.Name.Trim(), Form.MakeDefault), cancellationToken);

            if (result.IsError)
            {
                Form.ErrorMessage = result.FirstError.Type == ErrorType.NotFound
                                        ? InstallationDialogMessages.InstallationMissing
                                        : result.FirstError.Description;

                return;
            }

            CloseRequested?.Invoke(this, e: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to save installation {InstallationId}.", _installationId);

            Form.ErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ RelayCommand ]
    private void Cancel() => CloseRequested?.Invoke(this, e: false);

    private bool CanExecuteSave() => IsLoaded && !IsBusy && !string.IsNullOrWhiteSpace(Form.Name);

    private void OnFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(InstallationFormViewModel.Name))
        {
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Raised when the dialogue should close. Argument is <see langword="true" /> after a successful save, <see langword="false" /> on Cancel.</summary>
    public event EventHandler<bool>? CloseRequested;
}
