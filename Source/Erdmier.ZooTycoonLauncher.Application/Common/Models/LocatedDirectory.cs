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
