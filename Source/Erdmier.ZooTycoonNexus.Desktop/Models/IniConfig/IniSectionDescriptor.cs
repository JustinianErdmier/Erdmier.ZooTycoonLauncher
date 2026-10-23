namespace Erdmier.ZooTycoonNexus.Desktop.Models.IniConfig;

/// <summary>How one INI section (INI layout) or one page (Friendly layout) is presented in the editor.</summary>
/// <param name="Section">The section name in registry casing (INI layout), or a stable page id such as <c>display</c> (Friendly layout).</param>
/// <param name="Title">The bracketed section name (INI layout), or the page title (Friendly layout).</param>
/// <param name="Descriptor">The muted caption above the form pane.</param>
/// <param name="Footnote">Muted prose under the rows, or <see langword="null" />.</param>
/// <param name="Groups">The row groups, in display order.</param>
public sealed record IniSectionDescriptor(string Section, string Title, string Descriptor, string? Footnote, IReadOnlyList<IniFieldGroupDescriptor> Groups);
