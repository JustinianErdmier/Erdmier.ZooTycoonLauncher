namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>One INI section in the editor's form pane (SDD §9.2.1 Layer 3 — one generic section pair for every section).</summary>
public sealed class IniSectionViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The section's catalogue entry.</param>
    /// <param name="groups">The section's row groups.</param>
    public IniSectionViewModel(IniSectionDescriptor descriptor, IReadOnlyList<IniFieldGroup> groups)
    {
        Section    = descriptor.Section;
        Descriptor = descriptor.Descriptor;
        Footnote   = descriptor.Footnote;
        Groups     = groups;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniSectionViewModel()
        : this(IniEditorCatalogue.Sections[0], IniFieldFactory.CreateGroups(IniEditorCatalogue.Sections[0]))
    { }

    /// <summary>The section name, in registry casing.</summary>
    public string Section { get; }

    /// <summary>The section as written in the file and the section list, e.g. <c>[user]</c>.</summary>
    public string Header => $"[{Section}]";

    /// <summary>The muted caption above the form pane.</summary>
    public string Descriptor { get; }

    /// <summary>Muted prose under the rows, or <see langword="null" />.</summary>
    public string? Footnote { get; }

    /// <summary>Whether <see cref="Footnote" /> has text.</summary>
    public bool HasFootnote => !string.IsNullOrEmpty(Footnote);

    /// <summary>The row groups.</summary>
    public IReadOnlyList<IniFieldGroup> Groups { get; }

    /// <summary>Every row in the section.</summary>
    public IEnumerable<IniFieldViewModel> Fields => Groups.SelectMany(group => group.Fields);

    /// <summary>Whether any row has an edit.</summary>
    public bool IsDirty => Fields.Any(row => row.IsDirty);
}
