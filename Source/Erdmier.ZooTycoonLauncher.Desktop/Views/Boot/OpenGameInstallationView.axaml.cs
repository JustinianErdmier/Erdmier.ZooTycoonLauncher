using Avalonia.Input;
using Avalonia.VisualTree;

namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Boot;

/// <summary>View for the OpenGameInstallation state.</summary>
public sealed partial class OpenGameInstallationView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public OpenGameInstallationView() => AvaloniaXamlLoader.Load(this);

    // Double-clicking a row opens it (SDD §9.1). Ignore taps with no DataGridRow ancestor (header row, empty grid area).
    private void OnGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if ((e.Source as Visual)?.FindAncestorOfType<DataGridRow>() is null)
        {
            return;
        }

        if (DataContext is OpenGameInstallationViewModel viewModel
            && viewModel.OpenCommand.CanExecute(null))
        {
            viewModel.OpenCommand.Execute(null);
        }
    }
}
