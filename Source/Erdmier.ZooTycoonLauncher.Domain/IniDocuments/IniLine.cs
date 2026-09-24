namespace Erdmier.ZooTycoonLauncher.Domain.IniDocuments;

/// <summary>One physical line of a <c>zoo.ini</c> file, kept verbatim so the file round-trips byte for byte (SDD §8.1).</summary>
/// <param name="RawText">The line's text exactly as read, without its terminator.</param>
/// <param name="LineEnding">The line's own terminator: <c>"\r\n"</c>, <c>"\n"</c>, <c>"\r"</c>, or empty for a last line without one.</param>
public abstract record IniLine(string RawText, string LineEnding);
