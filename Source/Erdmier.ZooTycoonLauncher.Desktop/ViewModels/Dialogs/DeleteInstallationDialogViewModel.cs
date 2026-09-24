namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     View model for the Delete Installation confirmation (SDD §7.2.4, §9.5). Previews the delete — naming the installation that would be promoted to default, or that
///     none would remain — and dispatches <see cref="DeleteInstallationCommand" /> on confirm. Raises <see cref="CloseRequested" /> with <see langword="true" /> after
///     a successful delete, or <see langword="false" /> on Cancel.
/// </summary>
public sealed partial class DeleteInstallationDialogViewModel : ViewModelBase
{
    private readonly ILogger<DeleteInstallationDialogViewModel> _logger;

    private readonly IMediator _mediator;

    private Guid _installationId;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="logger">Logger for unexpected preview and delete failures.</param>
    public DeleteInstallationDialogViewModel(IMediator mediator, ILogger<DeleteInstallationDialogViewModel> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public DeleteInstallationDialogViewModel()
        : this(null!, NullLogger<DeleteInstallationDialogViewModel>.Instance)
    { }

    /// <summary>The preview or delete error, or <see langword="null" /> when none.</summary>
    [ ObservableProperty ]
    public partial string? ErrorMessage { get; set; }

    /// <summary><see langword="true" /> when the installation is the default and another installation would be promoted.</summary>
    [ ObservableProperty ]
    public partial bool HasPromotion { get; set; }

    /// <summary>The name of the installation being deleted.</summary>
    [ ObservableProperty ]
    public partial string InstallationName { get; set; } = string.Empty;

    /// <summary><see langword="true" /> while the delete is in flight.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(DeleteCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary><see langword="true" /> when the installation is the default and no other installation remains.</summary>
    [ ObservableProperty ]
    public partial bool IsLastInstallation { get; set; }

    /// <summary><see langword="true" /> once the preview has loaded; Delete stays disabled until then.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(DeleteCommand)) ]
    public partial bool IsLoaded { get; set; }

    /// <summary>The name of the installation that would be promoted to default, or empty when none.</summary>
    [ ObservableProperty ]
    public partial string PromotedName { get; set; } = string.Empty;

    /// <summary>Loads the deletion preview. Must be awaited by the dialogue service before the window is shown.</summary>
    /// <param name="installationId">The installation the user asked to delete.</param>
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
            ErrorOr<InstallationDeletionPreview> result = await _mediator.Send(new PreviewInstallationDeletionQuery(installationId), cancellationToken);

            if (result.IsError)
            {
                ErrorMessage = InstallationDialogMessages.InstallationMissing;

                return;
            }

            InstallationDeletionPreview preview = result.Value;

            InstallationName   = preview.Name;
            HasPromotion       = preview is { IsDefault: true, PromotedName: not null };
            IsLastInstallation = preview is { IsDefault: true, PromotedName: null };
            PromotedName       = preview.PromotedName ?? string.Empty;
            IsLoaded           = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to preview deleting installation {InstallationId}.", installationId);

            ErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteDelete)) ]
    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        IsBusy       = true;
        ErrorMessage = null;

        try
        {
            ErrorOr<DeleteInstallationResult> result = await _mediator.Send(new DeleteInstallationCommand(_installationId), cancellationToken);

            if (result.IsError)
            {
                if (result.FirstError.Type == ErrorType.NotFound)
                {
                    ErrorMessage = InstallationDialogMessages.InstallationMissing;
                    IsLoaded     = false;

                    return;
                }

                ErrorMessage = result.FirstError.Description;

                return;
            }

            CloseRequested?.Invoke(this, e: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to delete installation {InstallationId}.", _installationId);

            ErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ RelayCommand ]
    private void Cancel() => CloseRequested?.Invoke(this, e: false);

    private bool CanExecuteDelete() => IsLoaded && !IsBusy;

    /// <summary>Raised when the dialogue should close. Argument is <see langword="true" /> after a successful delete, <see langword="false" /> on Cancel.</summary>
    public event EventHandler<bool>? CloseRequested;
}
