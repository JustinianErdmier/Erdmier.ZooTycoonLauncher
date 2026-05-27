using System;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using Classic.Avalonia.Theme;

using Erdmier.ZooTycoonLauncher.Launcher.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Erdmier.ZooTycoonLauncher.Launcher.Views;

public partial class MainWindow : ClassicWindow
{
    // Tracks the last control whose Tag was shown, so PointerMoved only updates the VM when the hovered control actually changes.
    private Control? _lastHoveredIniControl;

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    /// <remarks>
    ///     <c>async void</c> is required here — <see cref="OnLoaded" /> is a framework override whose signature is fixed as <c>void</c>, so <c>async Task</c> is not an option. The
    ///     <c>try</c>/<c>catch</c> inside guards against the main risk of <c>async void</c> (unhandled exceptions propagating to the process).
    /// </remarks>
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (App.Services?.GetService<IFolderPicker>() is AvaloniaFolderPicker picker)
        {
            picker.SetTopLevel(this);
        }

        if (App.Services?.GetService<IDialogService>() is AvaloniaDialogService dialogs)
        {
            dialogs.SetOwner(this);
        }

        // PointerMoved bubbles — register on the ScrollViewer to catch movement over any INI control.
        // PointerEntered/Exited are direct events and do not bubble, so PointerMoved is used here instead.
        IniScrollViewer.PointerMoved  += OnIniScrollViewerPointerMoved;
        IniScrollViewer.PointerExited += OnIniScrollViewerPointerExited;

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
            // bad disk read can't crash the launcher on startup.
        }
    }

    /// <summary>
    ///     Walks from the pointer's source element up to <see cref="IniGroupContainer" />, finds the nearest ancestor (or self) with a non-empty string <c>Tag</c>, and updates
    ///     <see cref="IniSettingsViewModel.HoverDescription" />. The last-hovered control is cached so the VM is only updated when the hovered control changes.
    /// </summary>
    private void OnIniScrollViewerPointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        Control? element = e.Source as Control;
        Control? tagged  = null;

        while (element is not null
               && !ReferenceEquals(element, IniGroupContainer))
        {
            if (element.Tag is string { Length: > 0 })
            {
                tagged = element;

                break;
            }

            element = element.Parent as Control;
        }

        if (ReferenceEquals(tagged, _lastHoveredIniControl))
        {
            return;
        }

        _lastHoveredIniControl  = tagged;
        vm.Ini.HoverDescription = tagged?.Tag as string;
    }

    /// <summary>Clears <see cref="IniSettingsViewModel.HoverDescription" /> when the pointer leaves the INI scroll area, restoring save/discard feedback in the status area.</summary>
    private void OnIniScrollViewerPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        _lastHoveredIniControl  = null;
        vm.Ini.HoverDescription = null;
    }
}
