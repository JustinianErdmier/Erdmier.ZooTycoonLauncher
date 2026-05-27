using System;
using System.Diagnostics;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IShellService" />
/// <remarks>
///     Uses <c>explorer.exe /select,&lt;path&gt;</c> Explorer is forgiving here — if the target file no longer exists, it falls back to opening the parent directory; if even the
///     parent is gone, the user's profile directory is shown. We deliberately don't pre-validate the path, so deletions between launch and click don't surface a different code path
///     than the happy case.
/// </remarks>
public sealed class WindowsShellService : IShellService
{
    /// <inheritdoc />
    public void RevealInExplorer(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            // The two-arg overload passes Arguments verbatim; quote the path so spaces in “Program Files (x86)” survive the round-trip into Explorer’s command-line parser.
            Process.Start(fileName: "explorer.exe", $"/select,\"{path}\"");
        }
        catch (Exception)
        {
            // Defensive: an exotic Windows installation without an explorer.exe on the PATH would throw. Swallow rather than crashing the launcher — the worst case is a click that
            // does
            // nothing, which is acceptable degradation.
        }
    }
}
