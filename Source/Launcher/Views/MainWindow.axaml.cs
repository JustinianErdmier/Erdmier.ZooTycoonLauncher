using System;

using Avalonia.Interactivity;

using Classic.Avalonia.Theme;

using Erdmier.ZooTycoonLauncher.Launcher.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Erdmier.ZooTycoonLauncher.Launcher.Views;

public partial class MainWindow : ClassicWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (App.Services?.GetService<IFolderPicker>() is AvaloniaFolderPicker picker)
        {
            picker.SetTopLevel(this);
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            try
            {
                await viewModel.InitializeAsync();
            }
            catch (Exception)
            {
                // The startup service catches all expected exceptions and translates them to StartupStatus values; if anything still leaks through, swallow it here so a single bad
                // disk read can't crash the launcher on startup.
            }
        }
    }
}
