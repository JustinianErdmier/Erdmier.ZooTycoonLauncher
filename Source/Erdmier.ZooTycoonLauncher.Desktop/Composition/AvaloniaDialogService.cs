using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>The Avalonia-bound implementation of <see cref="IDialogService" />.</summary>
internal sealed class AvaloniaDialogService : IDialogService
{
    private readonly IServiceProvider _services;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="services">The composed service provider, used to resolve dialogue view models with their dependencies.</param>
    public AvaloniaDialogService(IServiceProvider services) => _services = services;

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

    /// <inheritdoc />
    public async Task<AddInstallationResult?> ShowAddInstallationAsync(string? prefilledPath)
    {
        Window? owner = ResolveOwner();

        if (owner is null)
        {
            return null;
        }

        AddInstallationDialogViewModel vm = _services.GetRequiredService<AddInstallationDialogViewModel>();
        vm.PrefillPath(prefilledPath);

        AddInstallationDialogView view = new()
        {
            DataContext = vm,
        };

        return await view.ShowDialog<AddInstallationResult?>(owner);
    }

    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(string? startPath)
    {
        Window? owner = ResolveOwner();

        if (owner is null)
        {
            return null;
        }

        FolderPickerOpenOptions options = new()
        {
            AllowMultiple = false,
            Title         = "Select Zoo Tycoon installation folder",
        };

        if (!string.IsNullOrWhiteSpace(startPath))
        {
            try
            {
                options.SuggestedStartLocation = await owner.StorageProvider.TryGetFolderFromPathAsync(startPath);
            }
            catch (Exception)
            {
                // Suggested start is best-effort; fall back to the picker's default.
            }
        }

        IReadOnlyList<IStorageFolder> chosen = await owner.StorageProvider.OpenFolderPickerAsync(options);

        return chosen.Count == 0 ? null : chosen[0].TryGetLocalPath();
    }

    private static Window? ResolveOwner()
        => Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}
