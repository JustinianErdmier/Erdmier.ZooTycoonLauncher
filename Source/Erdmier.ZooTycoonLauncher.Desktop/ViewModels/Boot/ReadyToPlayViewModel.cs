namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>View model for the ReadyToPlay state — the active installation is valid and the game can be launched.</summary>
public sealed partial class ReadyToPlayViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The resolved active installation.</param>
    public ReadyToPlayViewModel(InstallationSummary installation)
    {
        InstallationName = installation.Name;
        InstallationPath = installation.Path;
        IsDefault        = installation.IsDefault;
        GeneralTab       = new GeneralTabViewModel(installation);
        IniConfigTab     = new IniConfigTabViewModel();
        ScenariosTab     = new ScenariosTabViewModel();
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public ReadyToPlayViewModel() : this(new InstallationSummary(Guid.Empty,
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

    /// <summary><see langword="true" /> when this is the default installation.</summary>
    public bool IsDefault { get; }

    /// <summary>General tab view model.</summary>
    public GeneralTabViewModel GeneralTab { get; }

    /// <summary>INI Config tab view model.</summary>
    public IniConfigTabViewModel IniConfigTab { get; }

    /// <summary>Scenarios tab view model.</summary>
    public ScenariosTabViewModel ScenariosTab { get; }
}
