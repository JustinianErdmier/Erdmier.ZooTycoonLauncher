using Avalonia.Platform;

namespace Erdmier.ZooTycoonLauncher.Desktop.Views;

/// <summary>The application's main window — chrome only. Hosts the active state view via a <see cref="ContentControl" /> resolved by the <see cref="Composition.ViewLocator" />.</summary>
public sealed partial class MainWindow : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public MainWindow() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs eventArgs)
    {
        base.OnLoaded(eventArgs);

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.BootCommand.Execute(parameter: null);
        }
    }

    /// <inheritdoc />
    protected override void OnOpened(EventArgs eventArgs)
    {
        base.OnOpened(eventArgs);

        CalculateAndSetWindowPositionAtHorizontalCentreAndQuarterHeight();
    }

    /// <summary>
    ///     Positions the window horizontally centred and with its top edge a quarter of the way down the active screen's working area. <see cref="ClassicWindow.Position" /> is an
    ///     absolute <see cref="PixelPoint" /> in physical pixels, so the placement is computed at runtime from the screen's working area and scaling rather than hard-coded in XAML.
    /// </summary>
    private void CalculateAndSetWindowPositionAtHorizontalCentreAndQuarterHeight()
    {
        Screen? screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;

        if (screen is null)
        {
            return;
        }

        PixelRect workingArea = screen.WorkingArea;
        int       windowWidth = (int)(Width * screen.Scaling);

        int x = workingArea.X + (workingArea.Width - windowWidth) / 2;
        int y = workingArea.Y + (int)(workingArea.Height * 0.15);

        Position = new PixelPoint(x, y);
    }
}
