using Avalonia.Controls;
using Avalonia.Interactivity;

using Classic.Avalonia.Theme;

namespace Erdmier.ZooTycoonLauncher.Launcher.Views;

/// <summary>Combined startup alert dialog listing all invalid installations with per-row Fix / Remove / Ignore actions.</summary>
public partial class InvalidInstallationsView : ClassicWindow
{
    /// <summary>Initialises a new instance of <see cref="InvalidInstallationsView" />.</summary>
    public InvalidInstallationsView()
    {
        InitializeComponent();
    }

    /// <summary>Closes the dialog. The Invalid-Installations flow has no return value.</summary>
    private void OnContinueClick(object? sender, RoutedEventArgs e) => Close();
}
