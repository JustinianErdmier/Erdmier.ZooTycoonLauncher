using Avalonia.Controls.ApplicationLifetimes;

using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;
using Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>The Avalonia-bound implementation of <see cref="IDialogService" />.</summary>
internal sealed class AvaloniaDialogService : IDialogService
{
    /// <inheritdoc />
    public void ShowLaunchError(string message)
    {
        LaunchErrorView view = new()
        {
            DataContext = new LaunchErrorViewModel(message),
        };

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            view.Show(desktop.MainWindow);
        }
        else
        {
            view.Show();
        }
    }
}
