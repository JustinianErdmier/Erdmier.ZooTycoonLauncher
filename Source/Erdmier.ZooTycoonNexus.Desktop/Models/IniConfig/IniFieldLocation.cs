namespace Erdmier.ZooTycoonNexus.Desktop.Models.IniConfig;

/// <summary>Where a key sits in one editor layout.</summary>
/// <param name="Section">The section (INI) or page (Friendly) holding the row.</param>
/// <param name="Group">The group holding the row.</param>
/// <param name="Field">The row's descriptor.</param>
/// <param name="Order">The row's zero-based position across the whole layout, in display order.</param>
public sealed record IniFieldLocation(IniSectionDescriptor Section, IniFieldGroupDescriptor Group, IniFieldDescriptor Field, int Order);
