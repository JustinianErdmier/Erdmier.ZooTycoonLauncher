using Avalonia.Controls.ApplicationLifetimes;

namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>The Avalonia-bound implementation of <see cref="IApplicationLifecycle" />.</summary>
internal sealed class AvaloniaApplicationLifecycle : IApplicationLifecycle
{
    /// <inheritdoc />
    public void RequestShutdown()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
