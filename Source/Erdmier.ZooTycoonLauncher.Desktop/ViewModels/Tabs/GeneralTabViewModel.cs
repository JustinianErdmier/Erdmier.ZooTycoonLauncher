namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;

/// <summary>View model for the General tab inside the ReadyToPlay and CannotPlay states. Skeleton — content lands in the Launch Game and Screen Modes slices.</summary>
public sealed partial class GeneralTabViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The installation whose general information is displayed.</param>
    public GeneralTabViewModel(InstallationSummary installation)
    {
        InstallationName = installation.Name;
        InstallationPath = installation.Path;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public GeneralTabViewModel() : this(new InstallationSummary(Guid.Empty,
                                                                 "Designer Installation",
                                                                 @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
                                                                 InstallationValidity.Valid,
                                                                 IsDefault: true,
                                                                 DateTime.UtcNow,
                                                                 null, null, null)) { }

    /// <summary>The installation's user-visible name.</summary>
    public string InstallationName { get; }

    /// <summary>The installation's directory path.</summary>
    public string InstallationPath { get; }
}
