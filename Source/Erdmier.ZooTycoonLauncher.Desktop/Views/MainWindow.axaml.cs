namespace Erdmier.ZooTycoonLauncher.Desktop.Views;

/// <summary>The application's main window — chrome only. Hosts the active state view via a <see cref="ContentControl" /> resolved by the <see cref="Composition.ViewLocator" />.</summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Initialises a new instance.</summary>
    public MainWindow() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is MainWindowViewModel vm)
        {
            vm.BootCommand.Execute(parameter: null);
        }
    }
}
