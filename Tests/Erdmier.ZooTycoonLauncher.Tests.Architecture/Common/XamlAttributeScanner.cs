using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Erdmier.ZooTycoonLauncher.Tests.Architecture.Common;

/// <summary>
///     Finds XAML attributes and style setters for a set of property names, whether owner-qualified (<c>TextElement.FontSize</c>), split over lines, or written with
///     <c>Property</c> and <c>Value</c> in either order.
/// </summary>
public static partial class XamlAttributeScanner
{
    private const string AttributeRun = """(?:[^>"]|"[^"]*")*""";

    private static readonly ConcurrentDictionary<string, (Regex Attribute, Regex Setter)> PropertyRegexes = new(StringComparer.Ordinal);

    private static readonly ConcurrentDictionary<string, Regex> ElementRegexes = new(StringComparer.Ordinal);

    [ GeneratedRegex(@"<!--[\s\S]*?-->") ]
    private static partial Regex Comment();

    [ GeneratedRegex(@"\s+") ]
    private static partial Regex Whitespace();

    /// <summary>Scans <paramref name="text" /> for attributes and <c>Setter</c> elements whose property is one of <paramref name="propertyNames" />.</summary>
    /// <param name="text">The XAML source.</param>
    /// <param name="propertyNames">The unqualified property names to look for; a leading <c>*</c> matches any name with that suffix (<c>*Brush</c>).</param>
    /// <returns>Every match, attributes first and then setters, each with its value and 1-based line.</returns>
    public static IReadOnlyList<XamlAttributeMatch> Scan(string text, params string[] propertyNames)
    {
        text = BlankComments(text);

        (Regex attribute, Regex setter) = PropertyRegexes.GetOrAdd(string.Join(separator: '\u001F', propertyNames), BuildPropertyRegexes, propertyNames);

        List<XamlAttributeMatch> matches = [];

        foreach (Regex regex in new[] { attribute, setter })
        {
            foreach (Match match in regex.Matches(text))
            {
                matches.Add(ToMatch(text, match));
            }
        }

        return matches;
    }

    /// <summary>Scans <paramref name="text" /> for elements of the given names that carry their value as text content (<c>&lt;Color&gt;Red&lt;/Color&gt;</c>).</summary>
    /// <param name="text">The XAML source.</param>
    /// <param name="elementNames">The element names to look for.</param>
    /// <returns>Every element with non-empty text content, the element name as <see cref="XamlAttributeMatch.Name" /> and the content as the value.</returns>
    public static IReadOnlyList<XamlAttributeMatch> ScanElementContent(string text, params string[] elementNames)
    {
        text = BlankComments(text);

        Regex element = ElementRegexes.GetOrAdd(string.Join(separator: '\u001F', elementNames), BuildElementRegex, elementNames);

        return element.Matches(text).Select(m => ToMatch(text, m)).ToList();
    }

    /// <summary>Replaces every XML comment with its newlines alone, so commented-out XAML is not scanned and line numbers stay correct.</summary>
    /// <param name="text">The XAML source.</param>
    /// <returns>The source with each comment reduced to the newlines it contained.</returns>
    public static string BlankComments(string text)
        => Comment().Replace(text, m => new string(m.Value.Where(c => c == '\n').ToArray()));

    private static (Regex Attribute, Regex Setter) BuildPropertyRegexes(string key, string[] propertyNames)
    {
        string names = string.Join(separator: "|", propertyNames.Select(n => n.StartsWith('*') ? @"\w*" + Regex.Escape(n[ 1.. ]) : Regex.Escape(n)));

        Regex attribute = new($@"(?<![\w.:-])(?:\w+\.)?(?<name>{names})\s*=\s*""(?<value>[^""]*)""");

        Regex setter = new($@"<Setter\b(?={AttributeRun}?\sProperty\s*=\s*""(?:\w+\.)?(?<name>{names})"")(?={AttributeRun}?\sValue\s*=\s*""(?<value>[^""]*)""){AttributeRun}?/?>");

        return (attribute, setter);
    }

    private static Regex BuildElementRegex(string key, string[] elementNames)
    {
        string names = string.Join(separator: "|", elementNames.Select(Regex.Escape));

        return new Regex($@"<(?<name>{names})\b{AttributeRun}>\s*(?<value>[^<\s][^<]*?)\s*</\k<name>>");
    }

    private static XamlAttributeMatch ToMatch(string text, Match match)
    {
        int line = text.AsSpan(start: 0, match.Index).Count(value: '\n') + 1;

        return new XamlAttributeMatch(match.Groups[ "name" ].Value, match.Groups[ "value" ].Value.Trim(), line, Whitespace().Replace(match.Value, replacement: " "), match.Index);
    }
}
