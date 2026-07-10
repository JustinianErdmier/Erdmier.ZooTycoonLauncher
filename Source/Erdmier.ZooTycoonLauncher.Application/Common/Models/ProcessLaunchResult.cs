namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>The outcome of a single <see cref="IProcessLauncher.LaunchAsync(string, string, System.Threading.CancellationToken)" /> call.</summary>
/// <param name="Started"><see langword="true" /> when the OS accepted the start request and produced a process handle.</param>
/// <param name="ErrorMessage">Non-<see langword="null" /> when <paramref name="Started" /> is <see langword="false" />; the message displayed to the user verbatim.</param>
public sealed record ProcessLaunchResult(bool Started, string? ErrorMessage);
