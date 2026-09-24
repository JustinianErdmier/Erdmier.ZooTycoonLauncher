namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Common;

/// <summary>Shared read-only <c>DataGrid</c> view for the installation Name / Path / Status columns. Reused by the picker screen and the Installation Manager dialogue.</summary>
public sealed partial class InstallationGridView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public InstallationGridView() => AvaloniaXamlLoader.Load(this);
}
