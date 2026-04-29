using System.Threading.Tasks;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Abstraction over Avalonia's folder-picker dialogue, kept off <see cref="ViewModels.MainWindowViewModel" /> so the VM is testable without a UI host.</summary>
public interface IFolderPicker
{
    /// <summary>Opens a folder picker. Returns the selected directory path, or <see langword="null" /> if the user cancelled.</summary>
    /// <param name="title"> Window title for the picker dialogue. </param>
    Task<string?> PickFolderAsync(string title);
}
