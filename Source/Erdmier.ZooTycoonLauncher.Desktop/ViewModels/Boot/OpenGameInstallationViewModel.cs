namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>
///     The view model for the OpenGameInstallation state — shown when <c>LauncherSettings.StartupPreference</c> is <c>Ask</c>. The picker grid (SDD §9.6) is deferred; this slice
///     surfaces only the Add Installation entry point.
/// </summary>
public sealed partial class OpenGameInstallationViewModel : ViewModelBase
{
    private readonly IDialogService? _dialogs;

    private readonly Func<CancellationToken, Task>? _rebootAsync;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="dialogs">The dialogue service used to open the Add Installation modal.</param>
    /// <param name="rebootAsync">Callback that re-issues <c>BootCommand</c> on the main window after a successful add.</param>
    public OpenGameInstallationViewModel(IDialogService dialogs, Func<CancellationToken, Task> rebootAsync)
    {
        _dialogs     = dialogs;
        _rebootAsync = rebootAsync;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public OpenGameInstallationViewModel()
        : this(null!, null!)
    { }

    [ RelayCommand ]
    private async Task AddInstallationAsync(CancellationToken cancellationToken)
    {
        if (_dialogs is null
            || _rebootAsync is null)
        {
            return;
        }

        AddInstallationResult? result = await _dialogs.ShowAddInstallationAsync(prefilledPath: null);

        if (result is not null)
        {
            await _rebootAsync(cancellationToken);
        }
    }
}
