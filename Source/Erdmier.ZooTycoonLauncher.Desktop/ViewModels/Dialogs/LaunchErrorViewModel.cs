namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>The view model for the modeless launch-error window. Holds the message displayed verbatim.</summary>
public sealed class LaunchErrorViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">The error message to display.</param>
    public LaunchErrorViewModel(string message) => Message = message;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public LaunchErrorViewModel()
        : this(message: "Zoo Tycoon could not be launched: example message.")
    { }

    /// <summary>The error message to display verbatim.</summary>
    public string Message { get; }
}
