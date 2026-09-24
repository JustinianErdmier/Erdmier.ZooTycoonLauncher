namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>A modal Win95-style error message box.</summary>
public sealed partial class ErrorMessageView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public ErrorMessageView() => AvaloniaXamlLoader.Load(this);

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close();
}
