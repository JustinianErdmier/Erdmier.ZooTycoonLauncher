namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Starts external processes (the game executable) so the handler stays free of <see cref="System.Diagnostics.Process" /> calls.</summary>
public interface IProcessLauncher
{
    /// <summary>Starts the executable at <paramref name="exePath" /> with the supplied working directory.</summary>
    /// <param name="exePath">Fully-qualified path to the executable.</param>
    /// <param name="workingDirectory">Directory the process treats as its working directory; ZT1 resolves <c>zoo.ini</c> and asset folders relative to this.</param>
    /// <param name="cancellationToken">Cancellation token; observed only until the OS accepts the start request.</param>
    /// <returns>A <see cref="ProcessLaunchResult" /> describing whether the start succeeded.</returns>
    Task<ProcessLaunchResult> LaunchAsync(string exePath, string workingDirectory, CancellationToken cancellationToken);
}
