namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>The view model for the Win95-style Yes / No confirmation prompt.</summary>
public sealed class ConfirmationPromptViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="title">The window title.</param>
    /// <param name="message">The question asked, shown verbatim.</param>
    public ConfirmationPromptViewModel(string title, string message)
    {
        Title   = title;
        Message = message;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public ConfirmationPromptViewModel()
        : this(title: "Restore Previous INI", message: "Restore zoo.ini to the version captured 20 Oct 2026 14:30? The current version is archived to history first, so this can be undone.")
    { }

    /// <summary>The window title.</summary>
    public string Title { get; }

    /// <summary>The question asked, shown verbatim.</summary>
    public string Message { get; }
}