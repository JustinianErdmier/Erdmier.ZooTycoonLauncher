namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     The view model for the Add Installation modal (SDD §7.2.1, §9.5). Hosts the shared <see cref="InstallationFormViewModel" /> and dispatches
///     <see cref="AddInstallationCommand" /> on Save. With no installations registered yet, the name is pre-filled with <c>Main</c> and Mark as default is ticked and
///     locked, because the first installation always becomes the default. Raises <see cref="CloseRequested" /> with the dispatched result on success, or
///     <see langword="null" /> when the user cancels.
/// </summary>
public sealed partial class AddInstallationDialogViewModel : ViewModelBase
{
    private const string FirstInstallationName = "Main";

    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">The dialogue service — passed to the form for the folder picker.</param>
    public AddInstallationDialogViewModel(IMediator mediator, IDialogService dialogs)
    {
        _mediator = mediator;

        Form = new InstallationFormViewModel(dialogs);

        Form.PropertyChanged += OnFormPropertyChanged;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public AddInstallationDialogViewModel()
        : this(null!, null!)
    { }

    /// <summary>The shared Name / Folder / Default form. Bound to <c>InstallationFormView.DataContext</c>.</summary>
    public InstallationFormViewModel Form { get; }

    /// <summary><see langword="true" /> while a dispatch is in flight.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary>
    ///     Applies the first-installation defaults (SDD §7.2.1): with no installations registered, pre-fills the name with <c>Main</c> and ticks and locks Mark as
    ///     default. Must be awaited by the dialogue service before the window is shown.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_mediator is null)
        {
            return;
        }

        ErrorOr<IReadOnlyList<InstallationSummary>> existing = await _mediator.Send(new GetAllInstallationsQuery(), cancellationToken);

        if (!existing.IsError
            && existing.Value.Count == 0)
        {
            Form.Name = FirstInstallationName;

            Form.LockDefault();
        }
    }

    /// <summary>Sets the initial folder when the dialogue is being opened with a discovered candidate.</summary>
    /// <param name="prefilledPath">The candidate path to pre-fill, or <see langword="null" />.</param>
    public void PrefillPath(string? prefilledPath)
    {
        if (!string.IsNullOrWhiteSpace(prefilledPath))
        {
            Form.Path = prefilledPath;
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteSave)) ]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        IsBusy            = true;
        Form.ErrorMessage = null;

        try
        {
            ErrorOr<AddInstallationResult> result =
                await _mediator.Send(new AddInstallationCommand(Form.Name.Trim(), Form.Path.Trim(), Form.MakeDefault), cancellationToken);

            if (result.IsError)
            {
                Form.ErrorMessage = result.FirstError.Description;

                return;
            }

            CloseRequested?.Invoke(this, result.Value);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ RelayCommand ]
    private void Cancel() => CloseRequested?.Invoke(this, e: null);

    private bool CanExecuteSave()
        => !IsBusy
           && !string.IsNullOrWhiteSpace(Form.Name)
           && !string.IsNullOrWhiteSpace(Form.Path)

           // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
           && _mediator is not null;

    private void OnFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(InstallationFormViewModel.Name)
                           or nameof(InstallationFormViewModel.Path))
        {
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Raised when the dialogue should close. Argument is the dispatched <see cref="AddInstallationResult" /> on Save, or <see langword="null" /> on Cancel.</summary>
    public event EventHandler<AddInstallationResult?>? CloseRequested;
}
