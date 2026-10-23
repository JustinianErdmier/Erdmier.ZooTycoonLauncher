namespace Erdmier.ZooTycoonNexus.Desktop.Models.IniConfig;

/// <summary>A run of rows inside a section or page, optionally under a sub-header.</summary>
/// <param name="SubHeader">The sub-header, or <see langword="null" />.</param>
/// <param name="Fields">The rows, in display order.</param>
public sealed record IniFieldGroupDescriptor(string? SubHeader, IReadOnlyList<IniFieldDescriptor> Fields)
{
    /// <summary>Muted prose under the group's rows, or <see langword="null" />.</summary>
    public string? Footnote { get; init; }
}
