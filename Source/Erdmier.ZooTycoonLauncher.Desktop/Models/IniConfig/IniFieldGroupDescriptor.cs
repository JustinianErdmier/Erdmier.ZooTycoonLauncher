namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>A run of rows inside a section, optionally under a sub-header.</summary>
/// <param name="SubHeader">The sub-header (only <c>[UI]</c> uses them), or <see langword="null" />.</param>
/// <param name="Fields">The rows, in display order.</param>
public sealed record IniFieldGroupDescriptor(string? SubHeader, IReadOnlyList<IniFieldDescriptor> Fields);
