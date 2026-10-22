using System.Text.RegularExpressions;

namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

/// <summary>Milestone 11.1: the colour tables in <c>conventions.md</c> (§1.1 and §1.2) list exactly the keys, and values, that <c>Colours.axaml</c> defines.</summary>
public sealed class ColourRegistryTests
{
    private static readonly Regex TableRow = new(@"^\|\s*`(?<key>[^`]+)`\s*\|(?<rest>.*)$", RegexOptions.Multiline);

    private static readonly Regex HexValue = new(@"#[0-9A-Fa-f]{6}\b");

    private static readonly Regex KeyedTag = new(@"<(?<tag>[^<>]*?\bx:Key\s*=\s*""(?<key>[^""]*)""[^<>]*)>");

    private static readonly Regex ColourAttribute = new(@"\sColor\s*=\s*""(?<hex>#[0-9A-Fa-f]{6})""");

    /// <summary>Every key in the §1.1 and §1.2 tables is a key in <c>Colours.axaml</c> with the same value, and the other way round; unreadable and duplicate entries are reported.</summary>
    [ Fact ]
    public void ConventionsTables_MatchColoursAxaml()
    {
        List<string> differences = [];

        Dictionary<string, string> documented = ReadDocumentedColours(differences);

        Dictionary<string, string> defined = ReadDefinedColours(differences);

        documented.ShouldNotBeEmpty("No colour keys were read from conventions.md §1.1 and §1.2.");

        defined.ShouldNotBeEmpty("No colour keys were read from Colours.axaml.");

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

    private static Dictionary<string, string> ReadDocumentedColours(List<string> problems)
    {
        string text = File.ReadAllText(ProjectPaths.Conventions).Replace("\r\n", "\n");

        int start = text.IndexOf("### 1.1 ", StringComparison.Ordinal);

        int end = text.IndexOf("### 1.3 ", StringComparison.Ordinal);

        start.ShouldBeGreaterThanOrEqualTo(0, "conventions.md has no '### 1.1' heading.");

        end.ShouldBeGreaterThan(start, "conventions.md has no '### 1.3' heading after '### 1.1'.");

        Dictionary<string, string> colours = new(StringComparer.Ordinal);

        foreach (Match row in TableRow.Matches(text[ start..end ]))
        {
            string key = row.Groups[ "key" ].Value;

            Match hex = HexValue.Match(row.Groups[ "rest" ].Value);

            if (!hex.Success)
            {
                problems.Add($"{key} in conventions.md has no #RRGGBB value");
            }
            else if (!colours.TryAdd(key, hex.Value))
            {
                problems.Add($"{key} is listed more than once in conventions.md");
            }
        }

        return colours;
    }

    private static Dictionary<string, string> ReadDefinedColours(List<string> problems)
    {
        string text = XamlAttributeScanner.BlankComments(File.ReadAllText(Path.Combine(ProjectPaths.Desktop, "Styles", "Colours.axaml")));

        Dictionary<string, string> colours = new(StringComparer.Ordinal);

        foreach (Match keyed in KeyedTag.Matches(text))
        {
            string key = keyed.Groups[ "key" ].Value;

            Match colour = ColourAttribute.Match(keyed.Groups[ "tag" ].Value);

            if (!colour.Success)
            {
                problems.Add($"{key} in Colours.axaml is an unrecognised colour definition (expected Color=\"#RRGGBB\")");
            }
            else if (!colours.TryAdd(key, colour.Groups[ "hex" ].Value))
            {
                problems.Add($"{key} is defined more than once in Colours.axaml");
            }
        }

        return colours;
    }
}