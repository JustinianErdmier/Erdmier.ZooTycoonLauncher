namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>
///     A run of rows inside a section or page, optionally under a sub-header. A plain model rendered inline by <c>IniSectionView</c>; not a view model. The INI style renders
///     the sub-header as a monospace line over the rows; the Friendly style renders the group as a group box titled with it, the footnote at the bottom.
/// </summary>
public sealed class IniFieldGroup
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="subHeader">The sub-header, or <see langword="null" />.</param>
    /// <param name="footnote">Muted prose under the rows, or <see langword="null" />.</param>
    /// <param name="fields">The rows, including any hidden ones.</param>
    /// <param name="isIniStyle"><see langword="true" /> for the INI style (a sub-header line); <see langword="false" /> for the Friendly style (a group box).</param>
    public IniFieldGroup(string? subHeader, string? footnote, IReadOnlyList<IniFieldViewModel> fields, bool isIniStyle)
    {
        SubHeader  = subHeader;
        Footnote   = footnote;
        Fields     = fields;
        IsIniStyle = isIniStyle;

        VisibleFields = fields.Where(field => !field.IsHidden)
                              .ToList();
    }

    /// <summary>The sub-header, or <see langword="null" />.</summary>
    public string? SubHeader { get; }

    /// <summary>Whether <see cref="SubHeader" /> has text.</summary>
    public bool HasSubHeader => !string.IsNullOrEmpty(SubHeader);

    /// <summary>Muted prose under the rows, or <see langword="null" />.</summary>
    public string? Footnote { get; }

    /// <summary>Whether <see cref="Footnote" /> has text.</summary>
    public bool HasFootnote => !string.IsNullOrEmpty(Footnote);

    /// <summary>Whether the group renders in the INI style.</summary>
    public bool IsIniStyle { get; }

    /// <summary>Whether the group renders as a titled group box (the Friendly style).</summary>
    public bool IsGroupBox => !IsIniStyle;

    /// <summary>Whether the sub-header renders as a monospace line over the rows (the INI style, when there is one).</summary>
    public bool ShowSubHeaderLine => IsIniStyle && HasSubHeader;

    /// <summary>Every row of the group, in display order, including the hidden ones that only carry a key value.</summary>
    public IReadOnlyList<IniFieldViewModel> Fields { get; }

    /// <summary>The rows that are rendered, in display order.</summary>
    public IReadOnlyList<IniFieldViewModel> VisibleFields { get; }
}
