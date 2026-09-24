namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>One entry of a choice row.</summary>
/// <param name="Raw">The INI value written when the entry is chosen.</param>
/// <param name="Label">The text shown in the combo.</param>
public sealed record IniChoiceOption(string Raw, string Label);
