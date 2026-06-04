namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>Surface for opening Desktop-layer dialogues (modeless or modal). Grows with each new dialogue slice.</summary>
public interface IDialogService
{
    /// <summary>Opens the modeless launch-error window with the supplied message. Owned by <c>MainWindow</c> when available.</summary>
    /// <param name="message">The error message to display verbatim.</param>
    void ShowLaunchError(string message);
}
