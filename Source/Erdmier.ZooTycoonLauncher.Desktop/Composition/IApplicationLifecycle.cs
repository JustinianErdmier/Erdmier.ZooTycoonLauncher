namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>Wraps the Avalonia application lifetime so view models can request shutdown without referencing Avalonia directly.</summary>
public interface IApplicationLifecycle
{
    /// <summary>Requests that the application shut down. Equivalent to pressing the main window's close button.</summary>
    void RequestShutdown();
}
