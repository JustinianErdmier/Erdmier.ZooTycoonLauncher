namespace Erdmier.ZooTycoonNexus.Application.Common.Models;

/// <summary>One stored snapshot in an installation's INI history, with its per-key differences against the reconciled <c>Current</c> values.</summary>
/// <param name="SnapshotId">The snapshot's identifier.</param>
/// <param name="Kind">Whether the snapshot is <c>Historical</c> or the <c>Original</c>.</param>
/// <param name="Trigger">What triggered the capture.</param>
/// <param name="CapturedUtc">UTC timestamp of capture.</param>
/// <param name="Comparison">The differing rows and the kept keys, compared against the reconciled <c>Current</c> values.</param>
public sealed record IniHistoryEntry(Guid SnapshotId, IniSnapshotKind Kind, IniSnapshotTrigger Trigger, DateTime CapturedUtc, IniSnapshotComparison Comparison);
