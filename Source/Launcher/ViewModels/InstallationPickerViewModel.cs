using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>
///     ViewModel for the installation picker dialog. Wraps each registered installation in an
///     <see cref="InstallationListItem" /> so the list can render <c>(default)</c> / <c>(invalid)</c>
///     indicators alongside the name.
/// </summary>
public sealed partial class InstallationPickerViewModel : ViewModelBase
{
    /// <summary>The installations available for selection, wrapped with default/invalid indicators.</summary>
    [ ObservableProperty ]
    public partial ObservableCollection<InstallationListItem> Items { get; set; } = [];

    /// <summary>The currently selected row item, or <see langword="null" /> when nothing is selected.</summary>
    [ ObservableProperty ]
    public partial InstallationListItem? SelectedItem { get; set; }

    /// <summary>
    ///     <see langword="true" /> when the selected installation is valid and the OK button should be enabled.
    ///     Updated whenever <see cref="SelectedItem" /> changes.
    /// </summary>
    public bool CanConfirm => SelectedItem?.IsValid ?? false;

    /// <summary>
    ///     The underlying <see cref="Installation" /> of the current selection, exposed for the View's OK click
    ///     so the dialog returns the model rather than the row wrapper.
    /// </summary>
    public Installation? SelectedInstallation => SelectedItem?.Installation;

    /// <summary>Populates the picker with the given installations and tags the matching default entry.</summary>
    /// <param name="installations">Installations to display.</param>
    /// <param name="defaultId">
    ///     <see cref="LauncherConfig.LastOpenedInstallationId" /> from <c>launcher.config</c>; the matching entry
    ///     is rendered with a <c>(default)</c> suffix.
    /// </param>
    public void Load(IEnumerable<Installation> installations, Guid? defaultId = null)
    {
        Items = new ObservableCollection<InstallationListItem>(installations.Select(i => new InstallationListItem(i, defaultId.HasValue && i.Id == defaultId.Value)));
    }

    /// <summary>Source-generated change handler for <see cref="SelectedItem" />; re-raises <see cref="CanConfirm" />.</summary>
    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedItemChanged(InstallationListItem? value) => OnPropertyChanged(nameof(CanConfirm));
}
