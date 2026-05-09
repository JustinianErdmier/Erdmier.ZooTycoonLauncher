using System.Collections.Generic;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>
///     Internal raw line-by-line representation of a parsed INI file. Used by <see cref="Services.IniParserService" /> to preserve comments, blank lines, and key ordering on
///     round-trip writes.
/// </summary>
internal sealed class IniDocument
{
    public List<IniLine> Lines { get; } = [];
}

/// <summary>Base type for a single line in an <see cref="IniDocument" />.</summary>
internal abstract record IniLine;

/// <summary>A section header line of the form <c>[SectionName]</c>.</summary>
/// <param name="Name"> The section name without brackets, normalised to its original casing. </param>
/// <param name="RawText"> The original line text, preserved verbatim for a round-trip. </param>
internal sealed record IniSectionHeader(string Name, string RawText) : IniLine;

/// <summary>A key-value line of the form <c> Key=Value </c>.</summary>
/// <param name="Section"> The name of the section this key belongs to (without brackets). </param>
/// <param name="Key"> The key name, normalised to its original casing. </param>
/// <param name="Value"> The current value as a string. Updated by <see cref="Services.IniParserService.WriteAsync" /> when a known key's typed value has changed. </param>
/// <param name="RawText"> The original line text. Used as a template when re-emitting modified values, so surrounding whitespace and casing are preserved. </param>
internal sealed record IniKeyValue(string Section, string Key, string Value, string RawText) : IniLine
{
    public IniKeyValue WithValue(string newValue)
        => this with
        {
            Value = newValue
        };
}

/// <summary>A comment line beginning with <c> ; </c>.</summary>
/// <param name="RawText"> The original line text, preserved verbatim. </param>
internal sealed record IniComment(string RawText) : IniLine;

/// <summary>A blank line.</summary>
internal sealed record IniBlank : IniLine;
