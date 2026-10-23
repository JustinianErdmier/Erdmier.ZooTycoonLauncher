namespace Erdmier.ZooTycoonNexus.Application.Common.Models;

/// <summary>The recognised values of an installation's <c>zoo.ini</c>, as reconciled with the file on disk.</summary>
/// <param name="Values">Raw values keyed by registry id; a key absent from the file is absent here.</param>
/// <param name="FileLastWriteUtc">The file's last-write time (UTC), shown in the editor footer.</param>
/// <param name="HistoricalCount">How many historical snapshots the installation holds; the editor enables <c>Restore previous…</c> when it is above zero.</param>
public sealed record IniConfigResult(IReadOnlyDictionary<IniKeyId, string?> Values, DateTime FileLastWriteUtc, int HistoricalCount);
