using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Erdmier.ZooTycoonLauncher.Launcher.Views;

/// <summary>List-editor dialog for adding, removing, renaming, fixing, and setting the default installation.</summary>
public partial class ManageInstallationsView : Window
{
    /// <summary>Initialises a new instance of <see cref="ManageInstallationsView" />.</summary>
    public ManageInstallationsView()
    {
        InitializeComponent();
    }

    /// <summary>Closes the dialog. The Manage flow has no return value.</summary>
    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
