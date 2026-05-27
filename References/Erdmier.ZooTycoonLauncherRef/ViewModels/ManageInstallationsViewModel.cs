using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.Services;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>
///     ViewModel for the Manage Installations dialog. Supports add, remove, rename, fix, and set-default operations.
///     Each row is wrapped in an <see cref="InstallationListItem" /> so the list renders
///     <c>(default)</c> / <c>(invalid)</c> indicators alongside the installation name.
/// </summary>
public sealed partial class ManageInstallationsViewModel : ViewModelBase
{
    private readonly IDialogService _dialog;

    private readonly IFolderPicker _folderPicker;

    private readonly IInstallationService _installations;

    /// <summary>Initialises a new instance of <see cref="ManageInstallationsViewModel" />.</summary>
    /// <param name="installations">Installation service used for CRUD operations.</param>
    /// <param name="folderPicker">Folder-picker shim used by Add and Fix.</param>
    /// <param name="dialog">Dialog service used to prompt for friendly names and confirmations.</param>
    public ManageInstallationsViewModel(IInstallationService installations,
                                        IFolderPicker        folderPicker,
                                        IDialogService       dialog)
    {
        _installations = installations;
        _folderPicker  = folderPicker;
        _dialog        = dialog;
    }

    /// <summary>The installations currently registered, wrapped with default/invalid indicators.</summary>
    [ ObservableProperty ]
    public partial ObservableCollection<InstallationListItem> Items { get; set; } = [];

    /// <summary>The row currently selected in the list, or <see langword="null" /> if none.</summary>
    [ ObservableProperty ]
    public partial InstallationListItem? SelectedItem { get; set; }

    /// <summary>Transient status text shown beneath the list. Cleared on the next successful operation.</summary>
    [ ObservableProperty ]
    public partial string? StatusMessage { get; set; }

    /// <summary>Loads the current installations list from config along with the default-id flag.</summary>
    public async Task LoadAsync()
    {
        IReadOnlyList<Installation> all       = await _installations.GetAllAsync();
        Guid?                       defaultId = await _installations.GetDefaultIdAsync();

        Items = new ObservableCollection<InstallationListItem>(all.Select(i => new InstallationListItem(i, defaultId.HasValue && i.Id == defaultId.Value)));
    }

    /// <summary>Prompts for a directory and friendly name, then registers the installation.</summary>
    [ RelayCommand ]
    private async Task AddAsync()
    {
        string? picked = await _folderPicker.PickFolderAsync(title: "Locate Zoo Tycoon installation directory");

        if (picked is null)
        {
            return;
        }

        bool valid = await _installations.ValidateAsync(picked);

        if (!valid)
        {
            StatusMessage = "The selected directory does not contain zoo.exe.";

            return;
        }

        string? name = await _dialog.ShowInputAsync(prompt: "Friendly name (optional):", title: "Name Installation");

        await _installations.AddAsync(picked, string.IsNullOrWhiteSpace(name) ? null : name);
        StatusMessage = null;
        await LoadAsync();
    }

    /// <summary>Removes the selected installation after a yes/no confirmation.</summary>
    [ RelayCommand(CanExecute = nameof(HasSelection)) ]
    private async Task RemoveAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        bool confirmed = await _dialog.ConfirmAsync($"Remove \"{SelectedItem.DisplayName}\" from the launcher? The game files on disk are not affected.",
                                                    title: "Remove Installation");

        if (!confirmed)
        {
            return;
        }

        await _installations.RemoveAsync(SelectedItem.Id);
        await LoadAsync();
    }

    /// <summary>Prompts for a new friendly name for the selected installation.</summary>
    [ RelayCommand(CanExecute = nameof(HasSelection)) ]
    private async Task RenameAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        string? newName = await _dialog.ShowInputAsync(prompt: "New name:", title: "Rename Installation", SelectedItem.Installation.Name);

        if (newName is null)
        {
            return;
        }

        await _installations.UpdateAsync(SelectedItem.Id, string.IsNullOrWhiteSpace(newName) ? null : newName);
        await LoadAsync();
    }

    /// <summary>Replaces the directory of the selected installation with one chosen via the folder picker.</summary>
    [ RelayCommand(CanExecute = nameof(CanFix)) ]
    private async Task FixAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        string? picked = await _folderPicker.PickFolderAsync(title: "Locate replacement directory for " + SelectedItem.DisplayName);

        if (picked is null)
        {
            return;
        }

        bool valid = await _installations.ValidateAsync(picked);

        if (!valid)
        {
            StatusMessage = "The selected directory does not contain zoo.exe.";

            return;
        }

        await _installations.UpdateAsync(SelectedItem.Id, gameDirectory: picked);
        StatusMessage = null;
        await LoadAsync();
    }

    /// <summary>Marks the selected installation as the default by setting <see cref="LauncherConfig.LastOpenedInstallationId" />.</summary>
    [ RelayCommand(CanExecute = nameof(HasSelection)) ]
    private async Task SetAsDefaultAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        await _installations.SetLastOpenedAsync(SelectedItem.Id);
        StatusMessage = $"\"{SelectedItem.DisplayName}\" set as default.";

        // Refresh the list so the (default) indicator moves to the new entry.
        await LoadAsync();
    }

    /// <summary><see langword="true" /> when an installation is selected; gates Remove, Rename, and Set Default.</summary>
    private bool HasSelection() => SelectedItem is not null;

    /// <summary><see langword="true" /> only when an invalid installation is selected; Fix is meaningless for valid entries.</summary>
    private bool CanFix() => SelectedItem is { IsValid: false };

    /// <summary>Source-generated change handler for <see cref="SelectedItem" />; refreshes the per-row command CanExecute states.</summary>
    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedItemChanged(InstallationListItem? value)
    {
        RemoveCommand.NotifyCanExecuteChanged();
        RenameCommand.NotifyCanExecuteChanged();
        FixCommand.NotifyCanExecuteChanged();
        SetAsDefaultCommand.NotifyCanExecuteChanged();
    }
}
