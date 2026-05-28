namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Boot;

/// <summary>Transient view shown while <c>BootCommand</c> is in flight.</summary>
public sealed partial class LookingForZooTycoonView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public LookingForZooTycoonView() => AvaloniaXamlLoader.Load(this);
}
