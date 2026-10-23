namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>One line of the Restore Previous INI difference grid.</summary>
/// <param name="SectionText">The section header, for example <c>[user]</c>; empty when <paramref name="KeyText" /> already names the whole key (Friendly style).</param>
/// <param name="KeyText">The key name, for example <c>screenwidth</c>, or in Friendly style <c>Display › Screen › Width (pixels)</c>.</param>
/// <param name="CurrentText">The current value, or <c>(not set)</c> when the key is absent from the current file.</param>
/// <param name="SnapshotText">The value the snapshot holds.</param>
public sealed record IniHistoryDiffRow(string SectionText, string KeyText, string CurrentText, string SnapshotText)
{
    /// <summary>The separator between <see cref="SectionText" /> and <see cref="KeyText" />: <c>/</c>, or nothing when there is no section header.</summary>
    public string SeparatorText => SectionText.Length == 0 ? string.Empty : "/";
}