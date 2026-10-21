namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>An installation's INI history for the Restore Previous INI dialogue.</summary>
/// <param name="Entries">The <c>Historical</c> snapshots newest first, followed by the <c>Original</c> when one exists.</param>
public sealed record IniHistoryResult(IReadOnlyList<IniHistoryEntry> Entries);
