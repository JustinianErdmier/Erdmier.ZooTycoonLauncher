using Avalonia.Controls;
using Avalonia.Interactivity;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

namespace Erdmier.ZooTycoonLauncher.Launcher.Views;

/// <summary>Modal picker dialog that lets the user select one registered installation.</summary>
public partial class InstallationPickerView : Window
{
    /// <summary>Initialises a new instance of <see cref="InstallationPickerView" />.</summary>
    public InstallationPickerView()
    {
        InitializeComponent();
    }

    /// <summary>Closes the dialog returning the currently selected <see cref="Installation" />.</summary>
    private void OnOkClick(object? sender, RoutedEventArgs e)
        => Close((DataContext as InstallationPickerViewModel)?.SelectedInstallation);

    /// <summary>Closes the dialog returning <see langword="null" /> to indicate cancellation.</summary>
    private void OnCancelClick(object? sender, RoutedEventArgs e)
        => Close((Installation?)null);
}
