using System.Text.RegularExpressions;

namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

/// <summary>Milestone 11.1: the colour tables in <c>conventions.md</c> (§1.1 and §1.2) list exactly the keys, and values, that <c>Colours.axaml</c> defines.</summary>
public sealed class ColourRegistryTests
{
    private static readonly Regex TableRow = new(@"^\|\s*`(?<key>[^`]+)`\s*\|.*?(?<hex>#[0-9A-Fa-f]{6})\b", RegexOptions.Multiline);

    private static readonly Regex ColourBrush = new(@"x:Key\s*=\s*""(?<key>[^""]+)""[^>]*?\sColor\s*=\s*""(?<hex>#[0-9A-Fa-f]{6})""");

    /// <summary>Every key in the §1.1 and §1.2 tables is a key in <c>Colours.axaml</c> with the same value, and the other way round.</summary>
    [ Fact ]
    public void ConventionsTables_MatchColoursAxaml()
    {
        Dictionary<string, string> documented = ReadDocumentedColours();

        Dictionary<string, string> defined = ReadDefinedColours();

        documented.ShouldNotBeEmpty("No colour keys were read from conventions.md §1.1 and §1.2.");

        defined.ShouldNotBeEmpty("No colour keys were read from Colours.axaml.");

        List<string> differences = [];

        foreach ((string key, string value) in documented.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!defined.TryGetValue(key, out string? actual))
            {
                differences.Add($"{key} is listed in conventions.md but Colours.axaml does not define it");
            }
            else if (!string.Equals(value, actual, StringComparison.OrdinalIgnoreCase))
            {
                differences.Add($"{key} is {value} in conventions.md but {actual} in Colours.axaml");
            }
        }

        foreach (string key in defined.Keys.Except(documented.Keys).OrderBy(name => name, StringComparer.Ordinal))
        {
            differences.Add($"{key} is defined in Colours.axaml but conventions.md does not list it");
        }

        differences.ShouldBeEmpty("conventions.md and Colours.axaml disagree:" + Environment.NewLine + string.Join(Environment.NewLine, differences));
    }

    private static Dictionary<string, string> ReadDocumentedColours()
    {
        string text = File.ReadAllText(ProjectPaths.Conventions).Replace("\r\n", "\n");

        int start = text.IndexOf("### 1.1 ", StringComparison.Ordinal);

        int end = text.IndexOf("### 1.3 ", StringComparison.Ordinal);

        start.ShouldBeGreaterThanOrEqualTo(0, "conventions.md has no '### 1.1' heading.");

        end.ShouldBeGreaterThan(start, "conventions.md has no '### 1.3' heading after '### 1.1'.");

        Dictionary<string, string> colours = new(StringComparer.Ordinal);

        foreach (Match row in TableRow.Matches(text[ start..end ]))
        {
            colours[ row.Groups[ "key" ].Value ] = row.Groups[ "hex" ].Value;
        }

        return colours;
    }

    private static Dictionary<string, string> ReadDefinedColours()
    {
        string text = XamlAttributeScanner.BlankComments(File.ReadAllText(Path.Combine(ProjectPaths.Desktop, "Styles", "Colours.axaml")));

        Dictionary<string, string> colours = new(StringComparer.Ordinal);

        foreach (Match brush in ColourBrush.Matches(text))
        {
            colours[ brush.Groups[ "key" ].Value ] = brush.Groups[ "hex" ].Value;
        }

        return colours;
    }
}
