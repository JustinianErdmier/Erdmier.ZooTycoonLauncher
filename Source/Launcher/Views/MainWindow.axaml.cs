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
    /// <remarks>
    ///     <c>async void</c> is required here — <see cref="OnLoaded" /> is a framework override whose signature is fixed as <c>void</c>, so <c>async Task</c> is not an option.
    ///     The <c>try</c>/<c>catch</c> inside guards against the main risk of <c>async void</c> (unhandled exceptions propagating to the process).
    /// </remarks>
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (App.Services?.GetService<IFolderPicker>() is AvaloniaFolderPicker picker)
        {
            picker.SetTopLevel(this);
        }

        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        try
        {
            await viewModel.InitializeAsync();
        }
        catch (Exception)
        {
            // The startup service catches all expected exceptions and translates them to StartupStatus values; if anything still leaks through, swallow it here, so a single
            // bad
            // disk read can't crash the launcher on a startup.
        }
    }
}
