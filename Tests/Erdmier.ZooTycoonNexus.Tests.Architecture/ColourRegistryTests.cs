using System.Text.RegularExpressions;

namespace Erdmier.ZooTycoonNexus.Tests.Architecture;

/// <summary>Milestone 11.1: the colour tables in <c>conventions.md</c> (§1.1 and §1.2) list exactly the keys, and values, that <c>Colours.axaml</c> defines.</summary>
public sealed partial class ColourRegistryTests
{
    [ GeneratedRegex(@"^\|\s*`(?<key>[^`]+)`\s*\|(?<rest>.*)$", RegexOptions.Multiline) ]
    private static partial Regex TableRow();

    [ GeneratedRegex(@"#[0-9A-Fa-f]{6}\b") ]
    private static partial Regex HexValue();

    [ GeneratedRegex(@"<(?<tag>[^<>]*?\bx:Key\s*=\s*""(?<key>[^""]*)""[^<>]*)>") ]
    private static partial Regex KeyedTag();

    [ GeneratedRegex(@"\sColor\s*=\s*""(?<hex>#[0-9A-Fa-f]{6})""") ]
    private static partial Regex ColourAttribute();

    /// <summary>
    ///     Every key in the §1.1 and §1.2 tables is a key in <c>Colours.axaml</c> with the same value, and the other way round; unreadable and duplicate entries are reported.
    /// </summary>
    [ Fact ]
    public void ConventionsTables_MatchColoursAxaml()
    {
        List<string> differences = [];

        Dictionary<string, string> documented = ReadDocumentedColours(differences);

        Dictionary<string, string> defined = ReadDefinedColours(differences);

        documented.ShouldNotBeEmpty(customMessage: "No colour keys were read from conventions.md §1.1 and §1.2.");

        defined.ShouldNotBeEmpty(customMessage: "No colour keys were read from Colours.axaml.");

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

        start.ShouldBeGreaterThanOrEqualTo(expected: 0, customMessage: "conventions.md has no '### 1.1' heading.");

        end.ShouldBeGreaterThan(expected: start, customMessage: "conventions.md has no '### 1.3' heading after '### 1.1'.");

        Dictionary<string, string> colours = new(StringComparer.Ordinal);

        string section = text[ start..end ];

        foreach (string line in section.Split('\n'))
        {
            if (line.StartsWith('|') && !TableRow().IsMatch(line) && !IsHeaderOrSeparator(line))
            {
                problems.Add($"conventions.md has a colour row without a backticked key: {line.Trim()}");
            }
        }

        foreach (Match row in TableRow().Matches(section))
        {
            string key = row.Groups[ "key" ].Value;

            Match hex = HexValue().Match(row.Groups[ "rest" ].Value);

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

    private static bool IsHeaderOrSeparator(string line)
    {
        string firstCell = line.Split('|')[ 1 ].Trim();

        return firstCell == "Key" || firstCell.All(character => character is '-' or ':');
    }

    private static Dictionary<string, string> ReadDefinedColours(List<string> problems)
    {
        string text = XamlAttributeScanner.BlankComments(File.ReadAllText(Path.Combine(ProjectPaths.Desktop, "Styles", "Colours.axaml")));

        Dictionary<string, string> colours = new(StringComparer.Ordinal);

        foreach (Match keyed in KeyedTag().Matches(text))
        {
            string key = keyed.Groups[ "key" ].Value;

            Match colour = ColourAttribute().Match(keyed.Groups[ "tag" ].Value);

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
