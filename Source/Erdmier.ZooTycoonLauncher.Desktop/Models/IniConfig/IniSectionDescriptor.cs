namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>How one INI section is presented in the editor.</summary>
/// <param name="Section">The section name, in registry casing.</param>
/// <param name="Descriptor">The muted caption above the form pane.</param>
/// <param name="Footnote">Muted prose under the rows, or <see langword="null" />.</param>
/// <param name="Groups">The row groups, in display order.</param>
public sealed record IniSectionDescriptor(string Section, string Descriptor, string? Footnote, IReadOnlyList<IniFieldGroupDescriptor> Groups);
