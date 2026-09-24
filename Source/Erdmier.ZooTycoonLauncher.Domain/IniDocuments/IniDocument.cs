namespace Erdmier.ZooTycoonLauncher.Domain.IniDocuments;

/// <summary>
///     A line-preserving model of <c>zoo.ini</c>. Parsing never loses information — every line keeps its raw text and its own terminator, and a leading UTF-8 byte-order mark is
///     kept as a preamble — so <see cref="Render" /> of an unedited document returns the original text exactly (SDD §8.1). Section and key matching is case-insensitive.
/// </summary>
public sealed class IniDocument
{
    private const string Utf8ByteOrderMarkAsLatin1 = "ï»¿";

    private readonly List<IniLine> _lines;

    private readonly string _preamble;

    private IniDocument(string preamble, List<IniLine> lines)
    {
        _preamble = preamble;
        _lines    = lines;
    }

    /// <summary>The document's lines in file order.</summary>
    public IReadOnlyList<IniLine> Lines => _lines;

    /// <summary>Parses INI text. Never fails: unclassifiable lines are kept verbatim as comments.</summary>
    /// <param name="text">The file's text (decoded as Latin-1 by the file store).</param>
    /// <returns>The parsed document.</returns>
    public static IniDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        string preamble = string.Empty;

        if (text.StartsWith(Utf8ByteOrderMarkAsLatin1, StringComparison.Ordinal))
        {
            preamble = Utf8ByteOrderMarkAsLatin1;
            text     = text[Utf8ByteOrderMarkAsLatin1.Length..];
        }

        List<IniLine> lines    = [];
        string        section  = string.Empty;
        int           position = 0;

        while (position < text.Length)
        {
            int    end = text.IndexOfAny(['\r', '\n'], position);
            string raw;
            string ending;

            if (end < 0)
            {
                raw      = text[position..];
                ending   = string.Empty;
                position = text.Length;
            }
            else
            {
                raw      = text[position..end];
                ending   = text[end] == '\r' && end + 1 < text.Length && text[end + 1] == '\n' ? "\r\n" : text[end].ToString();
                position = end + ending.Length;
            }

            IniLine line = Classify(raw, ending, section);

            if (line is IniSectionHeader header)
            {
                section = header.Name;
            }

            lines.Add(line);
        }

        return new IniDocument(preamble, lines);
    }

    /// <summary>Renders the document back to text: the preamble, then every line's raw text followed by its own terminator.</summary>
    /// <returns>The INI text.</returns>
    public string Render()
    {
        StringBuilder builder = new(_preamble);

        foreach (IniLine line in _lines)
        {
            builder.Append(line.RawText)
                   .Append(line.LineEnding);
        }

        return builder.ToString();
    }

    /// <summary>Returns the trimmed value of the first line matching <paramref name="id" /> (case-insensitive).</summary>
    /// <param name="id">The key to find.</param>
    /// <param name="value">The value when found.</param>
    /// <returns><see langword="true" /> when the key is present.</returns>
    public bool TryGetValue(IniKeyId id, [ NotNullWhen(true) ] out string? value)
    {
        foreach (IniLine line in _lines)
        {
            if (line is IniKeyValue keyValue
                && keyValue.Id == id)
            {
                value = keyValue.Value;

                return true;
            }
        }

        value = null;

        return false;
    }

    /// <summary>
    ///     Sets a key's value. An existing key has only its value span rewritten; a missing key is inserted after the last key of the section's first occurrence; a missing section
    ///     is appended (blank line, header, key). Inserted lines use the document's dominant line ending.
    /// </summary>
    /// <param name="id">The key to set; its casing is used when the key or section has to be inserted.</param>
    /// <param name="value">The new value; empty writes <c>key=</c>.</param>
    public void SetValue(IniKeyId id, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        int existing = _lines.FindIndex(line => line is IniKeyValue keyValue && keyValue.Id == id);

        if (existing >= 0)
        {
            _lines[existing] = ((IniKeyValue)_lines[existing]).WithValue(value);

            return;
        }

        string ending = DominantLineEnding();
        int    header = _lines.FindIndex(line => line is IniSectionHeader sectionHeader && string.Equals(sectionHeader.Name, id.Section, StringComparison.OrdinalIgnoreCase));

        if (header >= 0)
        {
            int insertAfter = header;

            for (int index = header + 1; index < _lines.Count && _lines[index] is not IniSectionHeader; index++)
            {
                if (_lines[index] is IniKeyValue)
                {
                    insertAfter = index;
                }
            }

            string sectionName = ((IniSectionHeader)_lines[header]).Name;

            InsertAfter(insertAfter, new IniKeyValue(sectionName, id.Key, value, $"{id.Key}={value}", ending), ending);

            return;
        }

        bool endsWithoutTerminator = _lines.Count > 0 && _lines[^1].LineEnding.Length == 0;

        if (endsWithoutTerminator)
        {
            _lines[^1] = _lines[^1] with
            {
                LineEnding = ending
            };
        }

        if (_lines.Count > 0)
        {
            _lines.Add(new IniBlank(string.Empty, ending));
        }

        _lines.Add(new IniSectionHeader(id.Section, $"[{id.Section}]", ending));
        _lines.Add(new IniKeyValue(id.Section, id.Key, value, $"{id.Key}={value}", endsWithoutTerminator ? string.Empty : ending));
    }

    private static IniLine Classify(string raw, string ending, string section)
    {
        string trimmed = raw.Trim();

        if (trimmed.Length == 0)
        {
            return new IniBlank(raw, ending);
        }

        if (trimmed[0] == '[')
        {
            int close = trimmed.IndexOf(value: ']');

            if (close > 0)
            {
                return new IniSectionHeader(trimmed[1..close].Trim(), raw, ending);
            }
        }

        if (trimmed[0] is ';' or '#')
        {
            return new IniComment(raw, ending);
        }

        int equals = raw.IndexOf(value: '=');

        if (equals >= 0)
        {
            string key = raw[..equals].Trim();

            if (key.Length > 0)
            {
                return new IniKeyValue(section, key, raw[(equals + 1)..].Trim(), raw, ending);
            }
        }

        return new IniComment(raw, ending);
    }

    private string DominantLineEnding()
    {
        int crlf = _lines.Count(line => line.LineEnding == "\r\n");
        int lf   = _lines.Count(line => line.LineEnding == "\n");
        int cr   = _lines.Count(line => line.LineEnding == "\r");

        if (lf > crlf
            && lf >= cr)
        {
            return "\n";
        }

        return cr > crlf && cr > lf ? "\r" : "\r\n";
    }

    private void InsertAfter(int index, IniKeyValue line, string ending)
    {
        // Only the last line can lack a terminator. Give it one, and let the inserted line become the new unterminated last line so the file keeps its shape.
        if (_lines[index].LineEnding.Length == 0)
        {
            _lines[index] = _lines[index] with
            {
                LineEnding = ending
            };

            line = line with
            {
                LineEnding = string.Empty
            };
        }

        _lines.Insert(index + 1, line);
    }
}
