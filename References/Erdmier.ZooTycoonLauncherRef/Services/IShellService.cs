namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>
///     Abstraction over OS shell operations (currently "show this path in the file manager"), kept off <see cref="ViewModels.MainWindowViewModel" /> so the VM is testable
///     without spawning real processes.
/// </summary>
public interface IShellService
{
    /// <summary>
    ///     Opens the OS file manager focused on <paramref name="path" />. If <paramref name="path" /> points to a file, the file is highlighted in its containing directory; if it
    ///     points to a directory, that directory is opened. No-op when <paramref name="path" /> is <see langword="null" /> or whitespace.
    /// </summary>
    /// <param name="path"> Absolute path to a file or directory. </param>
    void RevealInExplorer(string? path);
}
