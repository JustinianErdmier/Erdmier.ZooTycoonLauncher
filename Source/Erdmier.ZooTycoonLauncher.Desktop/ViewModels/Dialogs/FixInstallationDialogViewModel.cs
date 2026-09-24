namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     View model for the Fix Installation modal (SDD §7.2.5, §9.5). Re-probes the installation folder on open, and lets the user relocate an installation whose
///     <c>zoo.exe</c> is missing. INI repair (Create) is deferred to the INI Config slice, so the INI box reports status only. <see cref="HasChanges" /> tells the
///     caller whether anything was persisted — a relocation, or drift found by the re-probe.
/// </summary>
public sealed partial class FixInstallationDialogViewModel : ViewModelBase
{
    private readonly IDialogService? _dialogs;

    private readonly ILogger<FixInstallationDialogViewModel> _logger;

    private readonly IMediator _mediator;

    private Guid _installationId;

    private string? _path;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">The dialogue service — used for the folder picker behind Locate.</param>
    /// <param name="logger">Logger for unexpected load and relocation failures.</param>
    public FixInstallationDialogViewModel(IMediator mediator, IDialogService dialogs, ILogger<FixInstallationDialogViewModel> logger)
    {
        _mediator = mediator;
        _dialogs  = dialogs;
        _logger   = logger;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public FixInstallationDialogViewModel()
        : this(null!, null!, NullLogger<FixInstallationDialogViewModel>.Instance)
    { }

    /// <summary>The load error, or <see langword="null" /> when the installation loaded.</summary>
    [ ObservableProperty ]
    public partial string? ErrorMessage { get; set; }

    /// <summary>The most recent Locate error (for example, a folder without <c>zoo.exe</c>), shown inside the Fix EXE box.</summary>
    [ ObservableProperty ]
    public partial string? ExeErrorMessage { get; set; }

    /// <summary><see langword="true" /> when anything was persisted whilst the dialogue was open. Read by the dialogue service once the window closes.</summary>
    public bool HasChanges { get; private set; }

    /// <summary><see langword="true" /> when <c>zoo.exe</c> is present in the installation folder.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(LocateCommand)) ]
    public partial bool HasExe { get; set; }

    /// <summary><see langword="true" /> when <c>zoo.ini</c> is present in the installation folder.</summary>
    [ ObservableProperty ]
    public partial bool HasIni { get; set; }

    /// <summary><see langword="true" /> while a relocation is in flight.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(LocateCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary><see langword="true" /> once the installation has loaded and been re-probed.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(LocateCommand)) ]
    public partial bool IsLoaded { get; set; }

    /// <summary>
    ///     Loads the installation and re-probes its folder — the stored flags are only refreshed at boot, so they can be stale. Must be awaited by the dialogue service
    ///     before the window is shown.
    /// </summary>
    /// <param name="installationId">The installation to fix.</param>
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
            ErrorOr<InstallationSummary> summary = await _mediator.Send(new GetInstallationByIdQuery(installationId), cancellationToken);

            if (summary.IsError)
            {
                ErrorMessage = InstallationDialogMessages.InstallationMissing;

                return;
            }

            ErrorOr<VerificationResult> verification = await _mediator.Send(new VerifyInstallationQuery(installationId), cancellationToken);

            if (verification.IsError)
            {
                ErrorMessage = InstallationDialogMessages.InstallationMissing;

                return;
            }

            _path = summary.Value.Path;

            HasExe = verification.Value.HasExe;
            HasIni = verification.Value.HasIni;

            // VerifyInstallationQuery persists drift, so a difference from the stored flags means the row changed.
            HasChanges = HasExe != summary.Value.Validity.HasExe || HasIni != summary.Value.Validity.HasIni;

            IsLoaded = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to load installation {InstallationId} for fixing.", installationId);

            ErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteLocate)) ]
    private async Task LocateAsync(CancellationToken cancellationToken)
    {
        if (_dialogs is null)
        {
            return;
        }

        string? chosen = await _dialogs.PickFolderAsync(_path);

        if (string.IsNullOrWhiteSpace(chosen))
        {
            return;
        }

        IsBusy          = true;
        ExeErrorMessage = null;

        try
        {
            ErrorOr<RelocateInstallationResult> result = await _mediator.Send(new RelocateInstallationCommand(_installationId, chosen), cancellationToken);

            if (result.IsError)
            {
                if (result.FirstError.Type == ErrorType.NotFound)
                {
                    ExeErrorMessage = InstallationDialogMessages.InstallationMissing;
                    IsLoaded        = false;

                    return;
                }

                ExeErrorMessage = result.FirstError.Description;

                return;
            }

            _path = chosen;

            HasExe     = result.Value.NewValidity.HasExe;
            HasIni     = result.Value.NewValidity.HasIni;
            HasChanges = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to relocate installation {InstallationId}.", _installationId);

            ExeErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ RelayCommand ]
    private void Ok() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private bool CanExecuteLocate() => IsLoaded && !HasExe && !IsBusy;

    /// <summary>Raised when the dialogue should close.</summary>
    public event EventHandler? CloseRequested;
}
