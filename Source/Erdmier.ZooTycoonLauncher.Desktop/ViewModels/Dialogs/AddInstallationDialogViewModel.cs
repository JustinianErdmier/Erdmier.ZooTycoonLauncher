namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     The view model for the Add Installation modal (SDD §7.2.1, §9.5). Owns Name / Folder / Default inputs and dispatches <see cref="AddInstallationCommand" /> on Save. Raises
///     <see cref="CloseRequested" /> with the dispatched result on success, or <see langword="null" /> when the user cancels.
/// </summary>
public sealed partial class AddInstallationDialogViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;

    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">The dialogue service — used here only for the folder picker.</param>
    public AddInstallationDialogViewModel(IMediator mediator, IDialogService dialogs)
    {
        _mediator = mediator;
        _dialogs  = dialogs;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public AddInstallationDialogViewModel()
        : this(null!, null!)
    { }

    // TODO: Should probably be a list so we can display multiple errors at once.
    /// <summary>The most recent validation or dispatch error description, or <see langword="null" /> when none. Bound to the error TextBlock.</summary>
    [ ObservableProperty ]
    public partial string? ErrorMessage { get; set; }

    // TODO: Test if this can be made private or if doing that will mess up the source generators.
    /// <summary><see langword="true" /> while a dispatch is in flight.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary>Marks the new installation as the launcher default. Bound to the Default checkbox.</summary>
    [ ObservableProperty ]
    public partial bool MakeDefault { get; set; }

    /// <summary>The trimmed user-visible installation name. Bound to the Name TextBox.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>The folder path containing <c>zoo.exe</c>. Bound to the Folder TextBox.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    public partial string Path { get; set; } = string.Empty;

    /// <summary>Sets the initial folder when the dialogue is being opened with a discovered candidate.</summary>
    /// <param name="prefilledPath">The candidate path to pre-fill, or <see langword="null" />.</param>
    public void PrefillPath(string? prefilledPath)
    {
        if (!string.IsNullOrWhiteSpace(prefilledPath))
        {
            Path = prefilledPath;
        }
    }

    [ RelayCommand ]
    private async Task BrowseAsync()
    {
        string? chosen = await _dialogs.PickFolderAsync(string.IsNullOrWhiteSpace(Path) ? null : Path);

        if (!string.IsNullOrWhiteSpace(chosen))
        {
            Path = chosen;
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteSave)) ]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_mediator is null)
        {
            return;
        }

        IsBusy       = true;
        ErrorMessage = null;

        try
        {
            // TODO: Confirm that the handler validates the name being unique and the path being valid.
            ErrorOr<AddInstallationResult> result =
                await _mediator.Send(new AddInstallationCommand(Name.Trim(), Path.Trim(), MakeDefault), cancellationToken);

            if (result.IsError)
            {
                ErrorMessage = result.FirstError.Description;

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
           && !string.IsNullOrWhiteSpace(Name)
           && !string.IsNullOrWhiteSpace(Path)

           // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
           && _mediator is not null;

    /// <summary>Raised when the dialogue should close. Argument is the dispatched <see cref="AddInstallationResult" /> on Save, or <see langword="null" /> on Cancel.</summary>
    public event EventHandler<AddInstallationResult?>? CloseRequested;
}
