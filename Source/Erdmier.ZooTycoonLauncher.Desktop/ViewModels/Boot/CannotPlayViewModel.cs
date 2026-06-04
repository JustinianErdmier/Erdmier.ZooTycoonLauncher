namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>The view model for the CannotPlay state — the active installation is invalid or synchronisation failed.</summary>
public sealed class CannotPlayViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The resolved active installation (invalid).</param>
    /// <param name="mediator">The Mediator dispatcher forwarded to <see cref="GeneralTabViewModel" />.</param>
    public CannotPlayViewModel(InstallationSummary installation, IMediator mediator)
    {
        InstallationName = installation.Name;
        InstallationPath = installation.Path;
        IsDefault        = installation.IsDefault;
        GeneralTab       = new GeneralTabViewModel(installation, mediator);
        IniConfigTab     = new IniConfigTabViewModel();
        ScenariosTab     = new ScenariosTabViewModel();
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public CannotPlayViewModel()
        : this(new InstallationSummary(Guid.Empty,
                                       Name: "Designer Installation",
                                       Path: @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
                                       InstallationValidity.InvalidNoExe,
                                       IsDefault: false,
                                       DateTime.UtcNow,
                                       ModifiedUtc: null,
                                       LastPlayedUtc: null,
                                       LastOpenedUtc: null),
               mediator: null!)
    { }

    /// <summary>General tab view model.</summary>
    public GeneralTabViewModel GeneralTab { get; }

    /// <summary>INI Config tab view model.</summary>
    public IniConfigTabViewModel IniConfigTab { get; }

    /// <summary>The installation's user-visible name.</summary>
    public string InstallationName { get; }

    /// <summary>The installation's directory path.</summary>
    public string InstallationPath { get; }

    /// <summary><see langword="true" /> when this is the default installation.</summary>
    public bool IsDefault { get; }

    /// <summary>Scenarios tab view model.</summary>
    public ScenariosTabViewModel ScenariosTab { get; }
}
