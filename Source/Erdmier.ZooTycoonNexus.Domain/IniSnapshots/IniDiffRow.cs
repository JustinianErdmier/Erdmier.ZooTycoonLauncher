namespace Erdmier.ZooTycoonNexus.Domain.IniSnapshots;

/// <summary>One user setting whose effective value differs between the current INI and an older snapshot.</summary>
/// <param name="Id">The setting's section and key.</param>
/// <param name="Current">The effective current value, or <see langword="null" /> when the current INI lacks the key.</param>
/// <param name="Snapshot">The snapshot's effective value; never <see langword="null" /> because every user setting has a default.</param>
public sealed record IniDiffRow(IniKeyId Id, string? Current, string Snapshot);
