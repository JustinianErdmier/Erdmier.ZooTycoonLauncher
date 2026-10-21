namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>One line of the Restore Previous INI difference grid.</summary>
/// <param name="SectionText">The section header, for example <c>[user]</c>.</param>
/// <param name="KeyText">The key name, for example <c>screenwidth</c>.</param>
/// <param name="CurrentText">The current value, or <c>(not set)</c> when the key is absent from the current file.</param>
/// <param name="SnapshotText">The value the snapshot holds.</param>
public sealed record IniHistoryDiffRow(string SectionText, string KeyText, string CurrentText, string SnapshotText);