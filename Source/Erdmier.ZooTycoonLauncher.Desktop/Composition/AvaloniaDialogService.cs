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
            DataContext = new LaunchErrorViewModel(message)
        };

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is not null)
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

        await vm.InitialiseAsync();

        AddInstallationDialogView view = new()
        {
            DataContext = vm
        };

        return await view.ShowDialog<AddInstallationResult?>(owner);
    }

    /// <inheritdoc />
    public async Task<bool> ShowInstallationManagerAsync()
    {
        Window? owner = ResolveOwner();

        if (owner is null)
        {
            return false;
        }

        // New-ed up directly against root services (mirroring MainWindowViewModel's construction of the picker's grid), rather than resolved through a scope, so the
        // manager's view model and grid share the same root-level services as everything else — including the nested Add dialogue — and so its reads see writes the
        // Add dialogue makes on the same DbContext. Constructing directly here also keeps the container from tracking this disposable view model, which this method
        // already disposes itself in the finally block below.
        InstallationGridViewModel grid = new(_services.GetRequiredService<IMediator>(),
                                             _services.GetRequiredService<IMessenger>(),
                                             _services.GetRequiredService<ILogger<InstallationGridViewModel>>());
        InstallationManagerDialogViewModel vm = new(grid, this);

        try
        {
            try
            {
                await vm.InitialiseAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _services.GetRequiredService<ILogger<AvaloniaDialogService>>()
                         .LogError(ex, "Failed to load the installation list for the Installation Manager.");
            }

            InstallationManagerDialogView view = new()
            {
                DataContext = vm
            };

            await view.ShowDialog(owner);

            return vm.HasChanges;
        }
        finally
        {
            vm.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ShowEditInstallationAsync(Guid installationId)
    {
        Window? owner = ResolveOwner();

        if (owner is null)
        {
            return false;
        }

        EditInstallationDialogViewModel vm = _services.GetRequiredService<EditInstallationDialogViewModel>();

        await vm.InitialiseAsync(installationId);

        EditInstallationDialogView view = new()
        {
            DataContext = vm
        };

        return await view.ShowDialog<bool>(owner);
    }

    /// <inheritdoc />
    public async Task ShowInstallationInfoAsync(Guid installationId)
    {
        Window? owner = ResolveOwner();

        if (owner is null)
        {
            return;
        }

        InstallationInfoDialogViewModel vm = _services.GetRequiredService<InstallationInfoDialogViewModel>();

        await vm.InitialiseAsync(installationId);

        InstallationInfoDialogView view = new()
        {
            DataContext = vm
        };

        await view.ShowDialog(owner);
    }

    /// <inheritdoc />
    public async Task<bool> ShowDeleteInstallationAsync(Guid installationId)
    {
        Window? owner = ResolveOwner();

        if (owner is null)
        {
            return false;
        }

        DeleteInstallationDialogViewModel vm = _services.GetRequiredService<DeleteInstallationDialogViewModel>();

        await vm.InitialiseAsync(installationId);

        DeleteInstallationDialogView view = new()
        {
            DataContext = vm
        };

        return await view.ShowDialog<bool>(owner);
    }

    /// <inheritdoc />
    public async Task<bool> ShowFixInstallationAsync(Guid installationId)
    {
        Window? owner = ResolveOwner();

        if (owner is null)
        {
            return false;
        }

        FixInstallationDialogViewModel vm = _services.GetRequiredService<FixInstallationDialogViewModel>();

        await vm.InitialiseAsync(installationId);

        FixInstallationDialogView view = new()
        {
            DataContext = vm
        };

        await view.ShowDialog(owner);

        return vm.HasChanges;
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
            Title         = "Select Zoo Tycoon installation folder"
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

        return chosen.Count == 0
                   ? null
                   : chosen[index: 0]
                       .TryGetLocalPath();
    }

    // The currently active window, falling back to MainWindow when none is active. Nested modals (e.g. the Add dialogue opened from the Installation Manager) must be owned by
    // their parent window so ShowDialog disables the parent while the child is open.
    private static Window? ResolveOwner()
        => Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
               ? desktop.Windows.FirstOrDefault(window => window.IsActive) ?? desktop.MainWindow
               : null;
}
