namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>The outcome of comparing the current INI with an older snapshot.</summary>
/// <param name="Rows">The user settings that differ, ordered by section then registry position. These are the restore's edit set.</param>
/// <param name="KeptKeys">The user settings present in the current INI but absent from the snapshot, which a restore leaves untouched.</param>
public sealed record IniSnapshotComparison(IReadOnlyList<IniDiffRow> Rows, IReadOnlyList<IniKeyId> KeptKeys);
