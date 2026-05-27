namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>
/// Outcome of <see cref="IInstallationLocator.LocateAsync" /> — either a discovered directory containing <c>zoo.exe</c> or a
/// trail of probed locations explaining why nothing was found.
/// </summary>
/// <remarks>
/// The trail entries are surfaced by the No-Game-Installation-Found state (SDD §9.1) so the user can see what was checked and
/// why. Each entry's <see cref="LocationProbeAttempt.Failure" /> is one of the structured reasons defined on the type.
/// </remarks>
public sealed record LocatedDirectory(string? Path, IReadOnlyList<LocationProbeAttempt> Trail)
{
    /// <summary><see langword="true" /> when a directory containing <c>zoo.exe</c> was found.</summary>
    public bool Found => Path is not null;
}

/// <summary>One entry in the auto-locate trail — a location that was probed and either matched or did not.</summary>
/// <param name="Source">Which sub-locator produced the candidate (registry value name, hard-coded path, persisted setting).</param>
/// <param name="CandidatePath">The path that was probed (after normalisation), or <see langword="null" /> when the source itself had no value.</param>
/// <param name="Failure"><see langword="null" /> when the probe succeeded; otherwise a structured failure reason.</param>
public sealed record LocationProbeAttempt(string Source, string? CandidatePath, LocationProbeFailure? Failure);

/// <summary>Structured failure reason for a single <see cref="LocationProbeAttempt" />.</summary>
public enum LocationProbeFailure
{
    /// <summary>The source produced no candidate path (e.g. registry key absent, persisted last-known empty).</summary>
    NoValue = 0,

    /// <summary>The candidate directory does not exist.</summary>
    DirectoryMissing = 1,

    /// <summary>The candidate directory exists but does not contain <c>zoo.exe</c>.</summary>
    NoExe = 2,
}
