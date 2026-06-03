using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Game;

/// <summary>The Windows implementation of <see cref="IProcessLauncher" />. Starts <c>zoo.exe</c> with the shell-execute semantics that match the Ref launcher. SDD §7.10.</summary>
public sealed class WindowsProcessLauncher : IProcessLauncher
{
    /// <inheritdoc />
    public Task<ProcessLaunchResult> LaunchAsync(string exePath, string workingDirectory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName         = exePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute  = true,
            };

            Process? process = Process.Start(startInfo);

            return Task.FromResult(process is null
                                       ? new ProcessLaunchResult(Started: false, ErrorMessage: "The system did not start a process for the game executable.")
                                       : new ProcessLaunchResult(Started: true, ErrorMessage: null));
        }
        catch (FileNotFoundException ex)
        {
            return Task.FromResult(new ProcessLaunchResult(Started: false, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(new ProcessLaunchResult(Started: false, ex.Message));
        }
        catch (IOException ex)
        {
            return Task.FromResult(new ProcessLaunchResult(Started: false, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Task.FromResult(new ProcessLaunchResult(Started: false, ex.Message));
        }
        catch (Win32Exception ex)
        {
            return Task.FromResult(new ProcessLaunchResult(Started: false, ex.Message));
        }
    }
}
