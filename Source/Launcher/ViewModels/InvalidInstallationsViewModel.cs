using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.Services;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>ViewModel for the combined invalid-installations alert. Exposes per-row Fix / Remove / Ignore actions.</summary>
public sealed partial class InvalidInstallationsViewModel : ViewModelBase
{
    private readonly IFolderPicker _folderPicker;

    private readonly IInstallationService _installations;

    /// <summary>Initialises a new instance of <see cref="InvalidInstallationsViewModel" />.</summary>
    /// <param name="installations">Installation service used by per-row Fix and Remove commands.</param>
    /// <param name="folderPicker">Folder-picker shim used by per-row Fix commands.</param>
    public InvalidInstallationsViewModel(IInstallationService installations, IFolderPicker folderPicker)
    {
        _installations = installations;
        _folderPicker  = folderPicker;
    }

    /// <summary>One <see cref="InvalidEntry" /> per invalid installation passed to <see cref="LoadEntries" />.</summary>
    [ ObservableProperty ]
    public partial ObservableCollection<InvalidEntry> Entries { get; set; } = [];

    /// <summary><see langword="true" /> when every entry has been resolved (fixed, removed, or ignored). Enables the Continue button.</summary>
    public bool CanContinue => Entries.All(e => e.IsResolved);

    /// <summary>Populates the entry list from the given invalid installations.</summary>
    /// <param name="invalid">Installations that failed validation.</param>
    public void LoadEntries(IReadOnlyList<Installation> invalid)
    {
        Entries = new ObservableCollection<InvalidEntry>(invalid.Select(i => new InvalidEntry(i, _installations, _folderPicker, this)));
    }

    /// <summary>Re-raises <see cref="CanContinue" /> so the Continue button refreshes when an entry resolves.</summary>
    internal void NotifyCanContinueChanged() => OnPropertyChanged(nameof(CanContinue));
}
