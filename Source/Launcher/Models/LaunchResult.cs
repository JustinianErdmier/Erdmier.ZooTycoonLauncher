namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>The outcome of attempting to start the Zoo Tycoon executable.</summary>
/// <param name="Success"> Whether the process was started successfully. </param>
/// <param name="ErrorMessage"> A human-readable error message describing the failure, or <see langword="null" /> on success. </param>
public record LaunchResult(bool Success, string? ErrorMessage);
