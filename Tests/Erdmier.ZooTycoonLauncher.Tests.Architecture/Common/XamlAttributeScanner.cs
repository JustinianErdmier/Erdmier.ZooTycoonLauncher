using System.Text.RegularExpressions;

namespace Erdmier.ZooTycoonLauncher.Tests.Architecture.Common;

/// <summary>
///     Finds XAML attributes and style setters for a set of property names, whether owner-qualified (<c>TextElement.FontSize</c>), split over lines, or written with
///     <c>Property</c> and <c>Value</c> in either order.
/// </summary>
public static class XamlAttributeScanner
{
    /// <summary>Scans <paramref name="text" /> for attributes and <c>Setter</c> elements whose property is one of <paramref name="propertyNames" />.</summary>
    /// <param name="text">The XAML source.</param>
    /// <param name="propertyNames">The unqualified property names to look for.</param>
    /// <returns>Every match, attributes first and then setters, each with its value and 1-based line.</returns>
    public static IReadOnlyList<XamlAttributeMatch> Scan(string text, params string[] propertyNames)
    {
        string names = string.Join(separator: "|", propertyNames.Select(Regex.Escape));

        Regex attribute = new($@"(?<![\w.:-])(?:\w+\.)?(?<name>{names})\s*=\s*""(?<value>[^""]*)""");

        Regex setter = new($@"<Setter\s+(?:Property\s*=\s*""(?:\w+\.)?(?<name>{names})""\s+Value\s*=\s*""(?<value>[^""]*)""|Value\s*=\s*""(?<value>[^""]*)""\s+Property\s*=\s*""(?:\w+\.)?(?<name>{names})"")");

        List<XamlAttributeMatch> matches = [];

        foreach (Regex regex in new[] { attribute, setter })
        {
            foreach (Match match in regex.Matches(text))
            {
                int line = text.AsSpan(start: 0, match.Index).Count(value: '\n') + 1;

                matches.Add(new XamlAttributeMatch(match.Groups[ "name" ].Value, match.Groups[ "value" ].Value.Trim(), line, Regex.Replace(match.Value, @"\s+", " ")));
            }
        }

        return matches;
    }
}
