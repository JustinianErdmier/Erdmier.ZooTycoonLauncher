namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Boot;

/// <summary>View for the playable states — hosts the tab strip for both the ReadyToPlay and CannotPlay outcomes. SDD §9.1, §9.2.</summary>
public sealed partial class PlayView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public PlayView() => AvaloniaXamlLoader.Load(this);
}
