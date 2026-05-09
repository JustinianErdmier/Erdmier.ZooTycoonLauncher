using System.Collections.Generic;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>ViewModel for the installation picker dialog. Exposes the list of registered installations and the user's selection.</summary>
public sealed partial class InstallationPickerViewModel : ViewModelBase
{
    /// <summary>The installations available for selection.</summary>
    [ ObservableProperty ]
    public partial ObservableCollection<Installation> Installations { get; set; } = [];

    /// <summary>The currently selected installation, or <see langword="null" /> when nothing is selected.</summary>
    [ ObservableProperty ]
    public partial Installation? SelectedInstallation { get; set; }

    /// <summary>
    ///     <see langword="true" /> when the selected installation is valid and the OK button should be enabled.
    ///     Updated whenever <see cref="SelectedInstallation" /> changes.
    /// </summary>
    public bool CanConfirm => SelectedInstallation?.IsValid ?? false;

    /// <summary>Populates the picker with the given installations.</summary>
    /// <param name="installations">Installations to display in the picker.</param>
    public void Load(IEnumerable<Installation> installations)
    {
        Installations = new ObservableCollection<Installation>(installations);
    }

    /// <summary>Source-generated change handler for <see cref="SelectedInstallation" />; re-raises <see cref="CanConfirm" />.</summary>
    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedInstallationChanged(Installation? value) => OnPropertyChanged(nameof(CanConfirm));
}
