namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>The view model for the NoGameInstallationFound state. Optionally surfaces a candidate path the locator found and owns the Add Installation command (SDD §9.1).</summary>
public sealed partial class NoGameInstallationFoundViewModel : ViewModelBase
{
    private readonly IDialogService? _dialogs;

    private readonly Func<CancellationToken, Task>? _rebootAsync;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="locatedCandidatePath">The path discovered by <c>IInstallationLocator</c>, or <see langword="null" /> when nothing was found.</param>
    /// <param name="dialogs">The dialogue service used to open the Add Installation modal.</param>
    /// <param name="rebootAsync">Callback that re-issues <c>BootCommand</c> on the main window after a successful add.</param>
    public NoGameInstallationFoundViewModel(string? locatedCandidatePath, IDialogService dialogs, Func<CancellationToken, Task> rebootAsync)
    {
        LocatedCandidatePath = locatedCandidatePath;
        _dialogs             = dialogs;
        _rebootAsync         = rebootAsync;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public NoGameInstallationFoundViewModel()
        : this(locatedCandidatePath: @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
               null!,
               null!)
    { }

    /// <summary><see langword="true" /> when a candidate path was discovered.</summary>
    public bool HasLocatedPath => LocatedCandidatePath is not null;

    /// <summary>Path discovered by the auto-locate scan, or <see langword="null" />.</summary>
    public string? LocatedCandidatePath { get; }

    [ RelayCommand ]
    private async Task AddInstallationAsync(CancellationToken cancellationToken)
    {
        if (_dialogs is null
            || _rebootAsync is null)
        {
            return;
        }

        AddInstallationResult? result = await _dialogs.ShowAddInstallationAsync(LocatedCandidatePath);

        if (result is not null)
        {
            await _rebootAsync(cancellationToken);
        }
    }
}
