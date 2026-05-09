using System.Collections.Generic;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Opens modal dialogs required by the multi-installation feature.</summary>
public interface IDialogService
{
    /// <summary>Shows the installation picker and returns the selected installation, or <see langword="null" /> if the user cancelled.</summary>
    /// <param name="installations">Installations to display in the picker.</param>
    Task<Installation?> ShowPickerAsync(IEnumerable<Installation> installations);

    /// <summary>Opens the Manage Installations dialog.</summary>
    Task ShowManageAsync();

    /// <summary>Shows the combined invalid-installations alert. Fix, Remove, and Ignore actions are applied inside the dialog.</summary>
    /// <param name="invalid">Installations that failed validation.</param>
    Task ShowInvalidInstallationsAlertAsync(IReadOnlyList<Installation> invalid);

    /// <summary>Shows a yes/no confirmation dialog and returns <see langword="true" /> if the user confirmed.</summary>
    /// <param name="message">Body text shown to the user.</param>
    /// <param name="title">Window title; defaults to <c>"Confirm"</c>.</param>
    Task<bool> ConfirmAsync(string message, string title = "Confirm");

    /// <summary>Shows a single-line text-input dialog and returns the entered text, or <see langword="null" /> if the user cancelled.</summary>
    /// <param name="prompt">Prompt label rendered above the input box.</param>
    /// <param name="title">Window title shown in the title bar.</param>
    /// <param name="defaultValue">Pre-filled text in the input box; defaults to empty.</param>
    Task<string?> ShowInputAsync(string prompt, string title, string? defaultValue = null);
}
