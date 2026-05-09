using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="ILauncherService" />
/// <remarks>
///     Uses <see cref="Process.Start(ProcessStartInfo)" /> with <see cref="ProcessStartInfo.UseShellExecute" /> = <see langword="true" /> so the OS resolves the same launch
///     semantics a user double-clicking the file would get (PATH lookup, manifests, side-by-side assemblies). <c> WorkingDirectory </c> is set to the directory of <c> zoo.exe </c> so
///     the game finds <c> zoo.ini </c> and its asset folders via its expected relative-path lookups.
/// </remarks>
public sealed class LauncherService : ILauncherService
{
    private readonly IFileSystem _fileSystem;

    public LauncherService(IFileSystem fileSystem) => _fileSystem = fileSystem;

    /// <inheritdoc />
    public Task<LaunchResult> LaunchAsync(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return Task.FromResult(new LaunchResult(Success: false, ErrorMessage: "Game executable path is unknown."));
        }

        // Pre-check existence so the user gets a clear error before Win32 returns its own (less actionable) text.
        if (!_fileSystem.File.Exists(exePath))
        {
            return Task.FromResult(new LaunchResult(Success: false, $"Game executable not found at \"{exePath}\"."));
        }

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName         = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty,
                UseShellExecute  = true
            };

            // Process.Start() returns null only when UseShellExecute opens the file with the registered handler and no new process is created — not applicable for an .exe, but
            // defend anyway.
            Process? process = Process.Start(startInfo);

            return Task.FromResult(process is null
                                       ? new LaunchResult(Success: false, ErrorMessage: "The system did not start a process for the game executable.")
                                       : new LaunchResult(Success: true, ErrorMessage: null));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new LaunchResult(Success: false, ex.Message));
        }
    }
}
