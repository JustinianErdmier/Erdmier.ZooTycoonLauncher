namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>
///     The Game Config tab's non-editor sub-state: loading, no INI present (with Create / Locate actions), or unreadable INI (SDD §9.3.1; milestone 10 spec C5, C9, §5.3).
/// </summary>
public sealed partial class IniPlaceholderViewModel : ViewModelBase
{
    private const string NoIniBody = "This installation has no zoo.ini on disk. Create a fresh one from the launcher's defaults, or locate an existing file to copy in.";

    private readonly IDialogService? _dialogs;

    private readonly bool _hasExe;

    private readonly Guid _installationId;

    private readonly string _installationPath = string.Empty;

    private readonly ILogger? _logger;

    private readonly IMediator? _mediator;

    private readonly Func<CancellationToken, Task>? _reopenOnIniTabAsync;

    /// <summary>Initialises a new instance without actions.</summary>
    /// <param name="headline">The bold headline.</param>
    /// <param name="body">The muted explanation (may be empty).</param>
    /// <param name="showWarningIcon">Whether to show the warning icon.</param>
    public IniPlaceholderViewModel(string headline, string body, bool showWarningIcon)
    {
        Headline        = headline;
        Body            = body;
        ShowWarningIcon = showWarningIcon;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniPlaceholderViewModel()
        : this(headline: "No INI present", NoIniBody, showWarningIcon: true)
    {
        HasActions = true;
    }

    private IniPlaceholderViewModel(Guid                          installationId,
                                    string                        installationPath,
                                    bool                          hasExe,
                                    IMediator                     mediator,
                                    IDialogService                dialogs,
                                    Func<CancellationToken, Task> reopenOnIniTabAsync,
                                    ILogger                       logger)
        : this(headline: "No INI present", NoIniBody, showWarningIcon: true)
    {
        _installationId      = installationId;
        _installationPath    = installationPath;
        _hasExe              = hasExe;
        _mediator            = mediator;
        _dialogs             = dialogs;
        _reopenOnIniTabAsync = reopenOnIniTabAsync;
        _logger              = logger;

        HasActions = true;
    }

    /// <summary>The bold headline.</summary>
    public string Headline { get; }

    /// <summary>The muted explanation.</summary>
    public string Body { get; }

    /// <summary>Whether <see cref="Body" /> has text.</summary>
    public bool HasBody => Body.Length > 0;

    /// <summary>Whether to show the warning icon.</summary>
    public bool ShowWarningIcon { get; }

    /// <summary>Whether the Create / Locate buttons are offered — <see langword="true" /> only for the No INI case.</summary>
    public bool HasActions { get; }

    /// <summary>Whether the body is rendered as one plain muted text block (every case except No INI, whose view renders <c>zoo.ini</c> in monospace).</summary>
    public bool ShowPlainBody => HasBody && !HasActions;

    /// <summary>Whether a Create or Locate command is running; both buttons are disabled meanwhile.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(CreateCommand)) ]
    [ NotifyCanExecuteChangedFor(nameof(LocateCommand)) ]
    public partial bool IsBusy { get; private set; }

    /// <summary>Shown while the first load is in flight.</summary>
    /// <returns>The placeholder.</returns>
    public static IniPlaceholderViewModel Loading() => new(headline: "Loading zoo.ini…", body: string.Empty, showWarningIcon: false);

    /// <summary>Shown when the installation has no <c>zoo.ini</c>; offers to create one from defaults or to locate an existing file.</summary>
    /// <param name="installationId">The open installation.</param>
    /// <param name="installationPath">The installation folder, where the file picker starts.</param>
    /// <param name="hasExe">Whether the installation has <c>zoo.exe</c>; both actions are disabled without it, as in the Fix dialogue.</param>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">Chrome service for the file picker and error dialogue.</param>
    /// <param name="reopenOnIniTabAsync">Re-boots the open installation and reopens on the Game Config tab (called after a successful action).</param>
    /// <param name="logger">Logger for failures.</param>
    /// <returns>The placeholder.</returns>
    public static IniPlaceholderViewModel NoIni(Guid                             installationId,
                                                string                           installationPath,
                                                bool                             hasExe,
                                                IMediator                        mediator,
                                                IDialogService                   dialogs,
                                                Func<CancellationToken, Task>    reopenOnIniTabAsync,
                                                ILogger<IniPlaceholderViewModel> logger)
        => new(installationId, installationPath, hasExe, mediator, dialogs, reopenOnIniTabAsync, logger);

    /// <summary>Shown when <c>zoo.ini</c> or its settings history could not be read.</summary>
    /// <param name="message">The error description.</param>
    /// <returns>The placeholder.</returns>
    public static IniPlaceholderViewModel Unreadable(string message) => new(headline: "zoo.ini could not be read", message, showWarningIcon: true);

    private bool CanRecover() => !IsBusy && _hasExe && _mediator is not null;

    [ RelayCommand(CanExecute = nameof(CanRecover)) ]
    private Task CreateAsync(CancellationToken cancellationToken)
        => RunAsync(errorTitle: "Cannot Create zoo.ini", failureVerb: "created", () => _mediator!.Send(new CreateDefaultIniCommand(_installationId), cancellationToken).AsTask(), cancellationToken);

    [ RelayCommand(CanExecute = nameof(CanRecover)) ]
    private async Task LocateAsync(CancellationToken cancellationToken)
    {
        string? source = await _dialogs!.PickFileAsync(_installationPath, title: "Locate zoo.ini");

        if (source is null)
        {
            return;
        }

        await RunAsync(errorTitle: "Cannot Copy zoo.ini", failureVerb: "copied", () => _mediator!.Send(new LocateIniCommand(_installationId, source), cancellationToken).AsTask(), cancellationToken);
    }

    private async Task RunAsync(string errorTitle, string failureVerb, Func<Task<ErrorOr<IniRecoveryResult>>> send, CancellationToken cancellationToken)
    {
        IsBusy = true;

        try
        {
            ErrorOr<IniRecoveryResult> result = await send();

            if (!result.IsError
                || result.FirstError.Code == IniErrors.AlreadyExistsCode
                || result.FirstError.Type == ErrorType.NotFound)
            {
                await _reopenOnIniTabAsync!(cancellationToken);

                return;
            }

            _logger!.LogWarning(message: "{Title} for installation {InstallationId}: {Error}", errorTitle, _installationId, result.FirstError.Description);

            await _dialogs!.ShowErrorAsync(errorTitle, result.FirstError.Description);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger!.LogError(ex, message: "{Title} for installation {InstallationId} failed unexpectedly", errorTitle, _installationId);

            await _dialogs!.ShowErrorAsync(errorTitle, $"zoo.ini could not be {failureVerb}: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
