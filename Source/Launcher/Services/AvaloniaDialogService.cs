using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia.Controls;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IDialogService" />
public sealed class AvaloniaDialogService : IDialogService
{
    private readonly IServiceProvider _services;

    private Window? _owner;

    /// <summary>Initialises a new instance of <see cref="AvaloniaDialogService" />.</summary>
    /// <param name="services">DI container used to resolve dialog ViewModels on demand.</param>
    public AvaloniaDialogService(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>Sets the owner window used as the parent for all modal dialogs. Call this from <c>MainWindow.OnLoaded</c>.</summary>
    /// <param name="owner">The application's main window, used as the parent for modal dialogs.</param>
    public void SetOwner(Window owner) => _owner = owner;

    /// <inheritdoc />
    public async Task<Installation?> ShowPickerAsync(IEnumerable<Installation> installations)
    {
        var vm = _services.GetRequiredService<InstallationPickerViewModel>();
        vm.Load(installations);

        var dialog = new Views.InstallationPickerView { DataContext = vm };

        return await dialog.ShowDialog<Installation?>(_owner!);
    }

    /// <inheritdoc />
    public Task ShowManageAsync()
    {
        // Wired up in Task 7 once ManageInstallationsViewModel exists.
        throw new NotImplementedException("ManageInstallationsViewModel is introduced in Task 7.");
    }

    /// <inheritdoc />
    public Task ShowInvalidInstallationsAlertAsync(IReadOnlyList<Installation> invalid)
    {
        // Wired up in Task 8 once InvalidInstallationsViewModel exists.
        throw new NotImplementedException("InvalidInstallationsViewModel is introduced in Task 8.");
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string message, string title = "Confirm")
    {
        var dialog = new Views.InputDialogView(message)
                     {
                         Title = title
                     };

        // Re-use InputDialogView as a yes/no prompt: OK = non-null result, Cancel = null.
        string? result = await dialog.ShowDialog<string?>(_owner!);

        return result is not null;
    }

    /// <inheritdoc />
    public async Task<string?> ShowInputAsync(string prompt, string title, string? defaultValue = null)
    {
        var dialog = new Views.InputDialogView(prompt, defaultValue ?? string.Empty)
                     {
                         Title = title
                     };

        return await dialog.ShowDialog<string?>(_owner!);
    }
}
