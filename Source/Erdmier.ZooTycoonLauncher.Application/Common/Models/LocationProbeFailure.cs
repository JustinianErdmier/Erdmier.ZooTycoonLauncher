namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>Structured failure reason for a single <see cref="LocationProbeAttempt" />.</summary>
public enum LocationProbeFailure
{
    /// <summary>The source produced no candidate path (e.g. registry key absent, persisted last-known empty).</summary>
    NoValue = 0,

    /// <summary>The candidate directory does not exist.</summary>
    DirectoryMissing = 1,

    /// <summary>The candidate directory exists but does not contain <c>zoo.exe</c>.</summary>
    NoExe = 2
}
