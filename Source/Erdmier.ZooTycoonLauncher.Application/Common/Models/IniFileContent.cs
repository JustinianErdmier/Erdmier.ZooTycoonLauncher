namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>The text of <c>zoo.ini</c> as read from disk.</summary>
/// <param name="Text">The file's text, decoded as Latin-1.</param>
/// <param name="LastWriteUtc">The file's last-write time (UTC).</param>
public sealed record IniFileContent(string Text, DateTime LastWriteUtc);
