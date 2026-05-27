using System;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>
///     Read-only display wrapper for an <see cref="Installation" /> row, augmenting the persisted model
///     with UI-only flags (default / invalid) so the Manage and Picker dialogs can render
///     <c>(default)</c> / <c>(invalid)</c> indicators without polluting the model.
/// </summary>
public sealed class InstallationListItem
{
    /// <summary>Initialises a new wrapper around the given installation.</summary>
    /// <param name="installation">The underlying registered installation.</param>
    /// <param name="isDefault">
    ///     <see langword="true" /> when this entry's <see cref="Installation.Id" /> matches
    ///     <see cref="LauncherConfig.LastOpenedInstallationId" /> at load time.
    /// </param>
    public InstallationListItem(Installation installation, bool isDefault)
    {
        Installation = installation;
        IsDefault    = isDefault;
    }

    /// <summary>The underlying installation. Used by the Manage commands and the Picker's OK click.</summary>
    public Installation Installation { get; }

    /// <summary><see langword="true" /> when this entry is the launcher's default installation.</summary>
    public bool IsDefault { get; }

    /// <summary>Pass-through of <see cref="Installation.IsValid" />, exposed for binding and CanExecute checks.</summary>
    public bool IsValid => Installation.IsValid;

    /// <summary>Pass-through of <see cref="Installation.Id" />.</summary>
    public Guid Id => Installation.Id;

    /// <summary>Pass-through of <see cref="Installation.DisplayName" />.</summary>
    public string DisplayName => Installation.DisplayName;

    /// <summary>
    ///     Display label rendered in list views: the installation's display name with optional
    ///     <c>(default)</c>, <c>(invalid)</c>, or combined <c>(default, invalid)</c> suffix.
    /// </summary>
    public string DisplayLabel
    {
        get
        {
            string? suffix = (IsDefault, Installation.IsValid) switch
            {
                (true,  true)  => " (default)",
                (true,  false) => " (default, invalid)",
                (false, false) => " (invalid)",
                var _          => null
            };

            return suffix is null ? DisplayName : DisplayName + suffix;
        }
    }
}
