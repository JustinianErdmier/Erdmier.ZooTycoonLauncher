namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>One entry in the auto-locate trail — a location that was probed and either matched or did not.</summary>
/// <param name="Source">Which sub-locator produced the candidate (registry value name, hard-coded path, persisted setting).</param>
/// <param name="CandidatePath">The path that was probed (after normalisation), or <see langword="null" /> when the source itself had no value.</param>
/// <param name="Failure"><see langword="null" /> when the probe succeeded; otherwise a structured failure reason.</param>
public sealed record LocationProbeAttempt(string Source, string? CandidatePath, LocationProbeFailure? Failure);
