namespace Erdmier.ZooTycoonLauncher.Domain.IniDocuments;

/// <summary>A <c>key=value</c> line. Everything after the first <c>=</c> is the value; inline comments are not recognised.</summary>
/// <param name="Section">The section the line sits under (empty for lines before any header).</param>
/// <param name="Key">The key, trimmed, in the file's casing.</param>
/// <param name="Value">The value, trimmed.</param>
/// <param name="RawText">The line's text exactly as read.</param>
/// <param name="LineEnding">The line's own terminator.</param>
public sealed record IniKeyValue(string Section, string Key, string Value, string RawText, string LineEnding) : IniLine(RawText, LineEnding)
{
    /// <summary>The line's section and key as an <see cref="IniKeyId" />.</summary>
    public IniKeyId Id => new(Section, Key);

    /// <summary>
    ///     Returns a copy whose value is <paramref name="newValue" />, rewriting only the value span: everything up to and including <c>=</c>, the whitespace after it, and any
    ///     trailing whitespace are kept.
    /// </summary>
    /// <param name="newValue">The new value text.</param>
    /// <returns>The rewritten line.</returns>
    public IniKeyValue WithValue(string newValue)
    {
        int    equals       = RawText.IndexOf(value: '=');
        string afterEquals  = RawText[(equals + 1)..];
        string trimmedStart = afterEquals.TrimStart();
        string leading      = afterEquals[..(afterEquals.Length - trimmedStart.Length)];
        string trailing     = trimmedStart[trimmedStart.TrimEnd().Length..];

        return this with
        {
            Value = newValue,
            RawText = RawText[..(equals + 1)] + leading + newValue + trailing
        };
    }
}
