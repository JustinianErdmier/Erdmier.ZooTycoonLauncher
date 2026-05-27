namespace Erdmier.ZooTycoonLauncher.Desktop.Views;

/// <summary>The application's main window — chrome only. State views are hosted by subsequent milestones via a ContentControl per SDD §9.2.</summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Initialises a new instance.</summary>
    public MainWindow() => AvaloniaXamlLoader.Load(this);
}
