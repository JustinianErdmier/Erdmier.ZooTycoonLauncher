using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Starts <c> zoo.exe </c> as a detached child process. Specced in SDD §6.4.</summary>
public interface ILauncherService
{
    /// <summary>Launches the game executable located at <paramref name="exePath" />. The launcher process itself remains open after launch (SDD §6.4).</summary>
    /// <param name="exePath"> Absolute path to <c> zoo.exe </c>. </param>
    /// <returns> <see cref="LaunchResult.Success" /> = <see langword="true" /> on a successful <c> Process.Start </c>; otherwise the failure is captured in <see cref="LaunchResult.ErrorMessage" /> rather than thrown. </returns>
    Task<LaunchResult> LaunchAsync(string exePath);
}
