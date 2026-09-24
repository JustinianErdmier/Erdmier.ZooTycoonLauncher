namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;

/// <summary>The result of reconciling <c>Current</c> with the file on disk.</summary>
/// <param name="Values">The recognised values now held by <c>Current</c> (the on-disk values).</param>
/// <param name="Outcome">What the reconciliation did.</param>
/// <param name="ChangedKeyCount">The number of keys the drift changed, for the adoption outcomes; <c>0</c> for first import and for no change.</param>
public sealed record IniReconciliation(IReadOnlyDictionary<IniKeyId, string?> Values, IniReconciliationOutcome Outcome, int ChangedKeyCount);
