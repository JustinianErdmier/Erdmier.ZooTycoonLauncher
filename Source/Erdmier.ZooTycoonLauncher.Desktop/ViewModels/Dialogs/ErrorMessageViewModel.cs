namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>The view model for the modal error message box.</summary>
public sealed class ErrorMessageViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="title">The window title.</param>
    /// <param name="message">The message, shown verbatim.</param>
    public ErrorMessageViewModel(string title, string message)
    {
        Title   = title;
        Message = message;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public ErrorMessageViewModel()
        : this(title: "Cannot Save zoo.ini", message: "zoo.ini could not be saved: Access to the path is denied.")
    { }

    /// <summary>The window title.</summary>
    public string Title { get; }

    /// <summary>The message, shown verbatim.</summary>
    public string Message { get; }
}
