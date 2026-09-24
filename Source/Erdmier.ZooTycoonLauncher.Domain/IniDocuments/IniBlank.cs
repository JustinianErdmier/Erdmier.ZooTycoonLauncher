namespace Erdmier.ZooTycoonLauncher.Domain.IniDocuments;

/// <summary>A blank (empty or whitespace-only) line.</summary>
/// <param name="RawText">The line's text exactly as read (any whitespace is kept).</param>
/// <param name="LineEnding">The line's own terminator.</param>
public sealed record IniBlank(string RawText, string LineEnding) : IniLine(RawText, LineEnding);
