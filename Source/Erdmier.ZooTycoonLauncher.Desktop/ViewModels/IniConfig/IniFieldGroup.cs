namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>A run of rows inside a section, optionally under a sub-header. A plain model rendered inline by <c>IniSectionView</c>; not a view model.</summary>
public sealed class IniFieldGroup
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="subHeader">The sub-header, or <see langword="null" />.</param>
    /// <param name="fields">The rows.</param>
    public IniFieldGroup(string? subHeader, IReadOnlyList<IniFieldViewModel> fields)
    {
        SubHeader = subHeader;
        Fields    = fields;
    }

    /// <summary>The sub-header, or <see langword="null" />.</summary>
    public string? SubHeader { get; }

    /// <summary>Whether <see cref="SubHeader" /> has text.</summary>
    public bool HasSubHeader => !string.IsNullOrEmpty(SubHeader);

    /// <summary>The rows, in display order.</summary>
    public IReadOnlyList<IniFieldViewModel> Fields { get; }
}
