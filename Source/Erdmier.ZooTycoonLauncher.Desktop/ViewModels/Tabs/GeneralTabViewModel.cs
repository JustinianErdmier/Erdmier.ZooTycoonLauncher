namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;

/// <summary>The view model for the General tab inside the ReadyToPlay and CannotPlay states. Skeleton — content lands in the Launch Game and Screen Modes slices.</summary>
public sealed class GeneralTabViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The installation whose general information is displayed.</param>
    public GeneralTabViewModel(InstallationSummary installation)
    {
        InstallationName = installation.Name;
        InstallationPath = installation.Path;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public GeneralTabViewModel()
        : this(new InstallationSummary(Guid.Empty,
                                       Name: "Designer Installation",
                                       Path: @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
                                       InstallationValidity.Valid,
                                       IsDefault: true,
                                       DateTime.UtcNow,
                                       ModifiedUtc: null,
                                       LastPlayedUtc: null,
                                       LastOpenedUtc: null))
    { }

    /// <summary>The installation's user-visible name.</summary>
    public string InstallationName { get; }

    /// <summary>The installation's directory path.</summary>
    public string InstallationPath { get; }
}
