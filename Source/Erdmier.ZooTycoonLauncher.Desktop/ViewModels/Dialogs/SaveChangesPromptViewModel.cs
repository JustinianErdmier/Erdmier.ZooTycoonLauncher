namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>The view model for the Win95-style save-changes prompt.</summary>
public sealed class SaveChangesPromptViewModel : ViewModelBase
{
    /// <summary>The question asked.</summary>
    public string Message => "Do you want to save the changes to zoo.ini?";
}
