using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.Services;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>
///     Wraps a single invalid <see cref="Installation" /> entry with per-row Fix / Remove / Ignore commands.
///     Declared <see langword="public" /> (rather than <c>file</c>-scoped) so the Avalonia compiled-binding
///     pipeline can resolve <c>x:DataType="vm:InvalidEntry"</c> in <c>InvalidInstallationsView.axaml</c>.
/// </summary>
public sealed partial class InvalidEntry : ViewModelBase
{
    private readonly IFolderPicker _folderPicker;

    private readonly IInstallationService _installations;

    private readonly InvalidInstallationsViewModel _parent;

    /// <summary>Initialises a new entry wrapping the given invalid installation.</summary>
    /// <param name="installation">The invalid installation this row represents.</param>
    /// <param name="installations">Installation service used by Fix and Remove.</param>
    /// <param name="folderPicker">Folder-picker shim used by Fix.</param>
    /// <param name="parent">Owning ViewModel; notified when this entry resolves so the Continue button can update.</param>
    public InvalidEntry(Installation                  installation,
                        IInstallationService          installations,
                        IFolderPicker                 folderPicker,
                        InvalidInstallationsViewModel parent)
    {
        Installation   = installation;
        _installations = installations;
        _folderPicker  = folderPicker;
        _parent        = parent;
    }

    /// <summary>The underlying invalid <see cref="Installation" />.</summary>
    public Installation Installation { get; }

    /// <summary>Display name shown for this row; falls back to the directory path when no friendly name was set.</summary>
    public string DisplayName => Installation.DisplayName;

    /// <summary><see langword="true" /> once the user has chosen Fix, Remove, or Ignore for this entry.</summary>
    [ ObservableProperty ]
    public partial bool IsResolved { get; set; }

    /// <summary>Inline error text shown when a Fix attempt selected a directory that still failed validation.</summary>
    [ ObservableProperty ]
    public partial string? StatusText { get; set; }

    /// <summary>Replaces the entry's directory with one chosen via the folder picker; marks the row resolved on success.</summary>
    [ RelayCommand ]
    private async Task FixAsync()
    {
        string? picked = await _folderPicker.PickFolderAsync(title: "Locate replacement directory for " + DisplayName);

        if (picked is null)
        {
            return;
        }

        bool valid = await _installations.ValidateAsync(picked);

        if (!valid)
        {
            StatusText = "Invalid: zoo.exe not found in the selected directory.";

            return;
        }

        await _installations.UpdateAsync(Installation.Id, gameDirectory: picked);
        IsResolved = true;
        StatusText = null;
        _parent.NotifyCanContinueChanged();
    }

    /// <summary>Removes the entry from the launcher config and marks the row resolved.</summary>
    [ RelayCommand ]
    private async Task RemoveAsync()
    {
        await _installations.RemoveAsync(Installation.Id);
        IsResolved = true;
        StatusText = null;
        _parent.NotifyCanContinueChanged();
    }

    /// <summary>Marks the row resolved without modifying the launcher config; the entry remains invalid.</summary>
    [ RelayCommand ]
    private void Ignore()
    {
        IsResolved = true;
        StatusText = null;
        _parent.NotifyCanContinueChanged();
    }
}
