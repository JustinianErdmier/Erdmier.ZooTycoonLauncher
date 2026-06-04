namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>Modeless Win95-style error window for launch failures.</summary>
public sealed partial class LaunchErrorView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public LaunchErrorView() => AvaloniaXamlLoader.Load(this);

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close();
}
