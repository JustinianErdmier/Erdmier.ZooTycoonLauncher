namespace Erdmier.ZooTycoonLauncher.Domain.IniDocuments;

/// <summary>A comment line (<c>;</c> or <c>#</c>), or any line the parser could not classify, kept verbatim.</summary>
/// <param name="RawText">The line's text exactly as read.</param>
/// <param name="LineEnding">The line's own terminator.</param>
public sealed record IniComment(string RawText, string LineEnding) : IniLine(RawText, LineEnding);
