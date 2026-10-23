namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>Selects the editor layout for an <see cref="IniEditorStyle" /> and locates a key inside it.</summary>
public static class IniEditorLayouts
{
    private static readonly IReadOnlyDictionary<IniKeyId, IniFieldLocation> IniLocations = Index(IniEditorCatalogue.Sections);

    private static readonly IReadOnlyDictionary<IniKeyId, IniFieldLocation> FriendlyLocations = Index(IniEditorFriendlyCatalogue.Sections);

    /// <summary>Gets the sections (INI) or pages (Friendly) of a layout, in display order.</summary>
    /// <param name="style">The editor style.</param>
    /// <returns>The layout's sections.</returns>
    public static IReadOnlyList<IniSectionDescriptor> Sections(IniEditorStyle style) => style == IniEditorStyle.Ini ? IniEditorCatalogue.Sections : IniEditorFriendlyCatalogue.Sections;

    /// <summary>Finds where a key's row sits in a layout.</summary>
    /// <param name="style">The editor style.</param>
    /// <param name="id">The key.</param>
    /// <param name="location">The row's location when the layout has one for the key.</param>
    /// <returns><see langword="true" /> when the layout holds a row for the key.</returns>
    public static bool TryLocate(IniEditorStyle style, IniKeyId id, [ NotNullWhen(true) ] out IniFieldLocation? location)
        => (style == IniEditorStyle.Ini ? IniLocations : FriendlyLocations).TryGetValue(id, out location);

    private static Dictionary<IniKeyId, IniFieldLocation> Index(IReadOnlyList<IniSectionDescriptor> sections)
    {
        Dictionary<IniKeyId, IniFieldLocation> locations = [];
        int                                    order     = 0;

        foreach (IniSectionDescriptor section in sections)
        {
            foreach (IniFieldGroupDescriptor group in section.Groups)
            {
                foreach (IniFieldDescriptor field in group.Fields)
                {
                    if (field.Id is { } id)
                    {
                        locations[id] = new IniFieldLocation(section, group, field, order);
                    }

                    order++;
                }
            }
        }

        return locations;
    }
}
