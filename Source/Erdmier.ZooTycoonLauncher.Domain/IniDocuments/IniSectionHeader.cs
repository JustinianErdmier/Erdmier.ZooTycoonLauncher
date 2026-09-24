namespace Erdmier.ZooTycoonLauncher.Domain.IniDocuments;

/// <summary>A <c>[section]</c> header line.</summary>
/// <param name="Name">The section name without brackets, trimmed.</param>
/// <param name="RawText">The line's text exactly as read.</param>
/// <param name="LineEnding">The line's own terminator.</param>
public sealed record IniSectionHeader(string Name, string RawText, string LineEnding) : IniLine(RawText, LineEnding);
