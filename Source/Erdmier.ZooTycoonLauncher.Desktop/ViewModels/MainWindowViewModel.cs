namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels;

/// <summary>
/// The main window's view model. In this foundations milestone it only carries a placeholder banner string;
/// subsequent milestones replace the banner with the state-dispatch surface specified by SDD §9.2.
/// </summary>
[UsedImplicitly]
public sealed partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>Placeholder banner string rendered until the state surface lands in P3.</summary>
    [ObservableProperty]
    public partial string Banner { get; set; } = "Zoo Tycoon Launcher — foundations build";
}
