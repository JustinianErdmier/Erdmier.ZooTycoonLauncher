namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>View model for the NoGameInstallationFound state. Optionally surfaces a candidate path the locator found but could not add because the dialogue is deferred.</summary>
public sealed partial class NoGameInstallationFoundViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="locatedCandidatePath">
    ///     The path discovered by <c>IInstallationLocator</c>, or <see langword="null" /> when nothing was found or the state was reached without running the locator.
    /// </param>
    public NoGameInstallationFoundViewModel(string? locatedCandidatePath)
        => LocatedCandidatePath = locatedCandidatePath;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public NoGameInstallationFoundViewModel() : this(locatedCandidatePath: @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon") { }

    /// <summary>Path discovered by the auto-locate scan, or <see langword="null" />.</summary>
    public string? LocatedCandidatePath { get; }

    /// <summary><see langword="true" /> when a candidate path was discovered and can be surfaced to the user.</summary>
    public bool HasLocatedPath => LocatedCandidatePath is not null;
}
