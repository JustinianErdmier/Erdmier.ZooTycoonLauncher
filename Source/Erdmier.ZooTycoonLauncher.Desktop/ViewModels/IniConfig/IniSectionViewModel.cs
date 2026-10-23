namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>One INI section (INI style) or page (Friendly style) in the editor's form pane (SDD §9.2.1 Layer 3 — one generic section pair for every section).</summary>
public sealed class IniSectionViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The section or page layout entry.</param>
    /// <param name="groups">The row groups.</param>
    /// <param name="isIniStyle"><see langword="true" /> for the INI style; <see langword="false" /> for the Friendly style.</param>
    public IniSectionViewModel(IniSectionDescriptor descriptor, IReadOnlyList<IniFieldGroup> groups, bool isIniStyle)
    {
        Section    = descriptor.Section;
        Title      = descriptor.Title;
        Descriptor = descriptor.Descriptor;
        Footnote   = descriptor.Footnote;
        Groups     = groups;
        IsIniStyle = isIniStyle;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniSectionViewModel()
        : this(IniEditorCatalogue.Sections[0], IniFieldFactory.CreateGroups(IniEditorCatalogue.Sections[0], IniEditorStyle.Ini), isIniStyle: true)
    { }

    /// <summary>The section name in registry casing (INI style), or the page id (Friendly style).</summary>
    public string Section { get; }

    /// <summary>The title shown over the form and in the list: the bracketed section name, e.g. <c>[user]</c> (INI style), or the page title (Friendly style).</summary>
    public string Title { get; }

    /// <summary>The muted one-line description of the section or page.</summary>
    public string Descriptor { get; }

    /// <summary>Muted prose under the rows, or <see langword="null" />.</summary>
    public string? Footnote { get; }

    /// <summary>Whether <see cref="Footnote" /> has text.</summary>
    public bool HasFootnote => !string.IsNullOrEmpty(Footnote);

    /// <summary>Whether the section renders in the INI style: monospace titles, the "section of zoo.ini" suffix and sub-header lines.</summary>
    public bool IsIniStyle { get; }

    /// <summary>Whether the section renders in the Friendly style.</summary>
    public bool IsFriendlyStyle => !IsIniStyle;

    /// <summary>The row groups.</summary>
    public IReadOnlyList<IniFieldGroup> Groups { get; }

    /// <summary>The groups the INI style renders (empty in the Friendly style), so each row is built once, by the list that matches the style.</summary>
    public IReadOnlyList<IniFieldGroup> IniStyleGroups => IsIniStyle ? Groups : [];

    /// <summary>The groups the Friendly style renders as group boxes (empty in the INI style).</summary>
    public IReadOnlyList<IniFieldGroup> FriendlyStyleGroups => IsIniStyle ? [] : Groups;

    /// <summary>Every row in the section, including hidden ones.</summary>
    public IEnumerable<IniFieldViewModel> Fields => Groups.SelectMany(group => group.Fields);

    /// <summary>Whether any row has an edit.</summary>
    public bool IsDirty => Fields.Any(row => row.IsDirty);
}
