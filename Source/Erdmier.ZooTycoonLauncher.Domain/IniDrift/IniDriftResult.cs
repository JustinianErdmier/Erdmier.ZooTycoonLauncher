namespace Erdmier.ZooTycoonLauncher.Domain.IniDrift;

/// <summary>The outcome of comparing <c>Current</c> with the on-disk values.</summary>
/// <param name="Kind">The drift tier.</param>
/// <param name="ChangedKeys">Every recognised key whose value differs, in registry order and casing.</param>
public sealed record IniDriftResult(IniDriftKind Kind, IReadOnlyList<IniKeyId> ChangedKeys);
