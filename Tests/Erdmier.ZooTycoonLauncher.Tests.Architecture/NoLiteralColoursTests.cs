using System.Text.RegularExpressions;
using Erdmier.ZooTycoonLauncher.Tests.Architecture.Common;

namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

/// <summary>Milestone 11 spec C7: colours are defined once in <c>Styles/Colours.axaml</c> and referenced by name everywhere else.</summary>
public sealed partial class NoLiteralColoursTests
{
    [ GeneratedRegex("""\b(Brushes\.|Colors\.|Color\.Parse\(|Color\.TryParse\(|Brush\.Parse\(|SolidColorBrush\.Parse\(|new\s+(?:[\w.]+\.)?Color\(|Color\.FromRgb\(|Color\.FromArgb\(|Color\.FromUInt32\()""") ]
    private static partial Regex BrushLiteral();

    [ GeneratedRegex("""Fallback(?<name>Green|Red)\s*=\s*new SolidColorBrush\(Color\.Parse\(s:\s*"(?<hex>#[0-9A-Fa-f]+)"\)\)""") ]
    private static partial Regex FallbackDeclaration();

    [ GeneratedRegex("""const string \w+Key\s*=\s*"(?<key>[^"]+)";""") ]
    private static partial Regex LookupKey();

    [ GeneratedRegex(@"x:Key=""(?<key>Accent(?:Green|Red))""\s+Color=""(?<hex>#[0-9A-Fa-f]+)""") ]
    private static partial Regex NamedAccent();

    /// <summary>No view may hold a literal colour; every colour attribute or style setter references a named resource (or is <c>Transparent</c>).</summary>
    [ Fact ]
    public void Views_UseNamedColours()
    {
        string desktop = SourceTree.ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Desktop");

        List<string> violations = [];

        foreach (string file in SourceTree.EnumerateSources(desktop, pattern: "*.axaml"))
        {
            if (file.EndsWith(Path.Combine("Styles", "Colours.axaml"), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string text = File.ReadAllText(file);

            foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(text, "Foreground", "Background", "BorderBrush", "Fill", "Stroke", "Color"))
            {
                if (IsAllowed(match.Name, match.Value))
                {
                    continue;
                }

                violations.Add($"{Path.GetRelativePath(desktop, file)}:{match.Line}: {match.Text}");
            }
        }

        violations.ShouldBeEmpty("Literal colours found; define them in Styles/Colours.axaml and reference them by name:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    /// <summary>No Desktop or Domain code may build colours or brushes from literals (the lookup's pinned fallback aside).</summary>
    [ Fact ]
    public void Code_UsesNoBrushLiterals()
    {
        string desktop = SourceTree.ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Desktop");
        string domain  = SourceTree.ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Domain");

        List<string> violations = [];

        foreach (string root in new[] { desktop, domain })
        {
            foreach (string file in SourceTree.EnumerateSources(root, pattern: "*.cs"))
            {
                if (file.EndsWith(Path.Combine("Converters", "ApplicationBrushLookup.cs"), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string[] lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    Match match = BrushLiteral().Match(lines[i]);

                    if (match.Success)
                    {
                        violations.Add($"{Path.GetRelativePath(root, file)}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }
        }

        violations.ShouldBeEmpty("Brush/colour literals found in code; use the named resources instead:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    /// <summary>The lookup's fallback values (used when no <c>Application</c> exists) must equal the named accents in <c>Colours.axaml</c>.</summary>
    [ Fact ]
    public void BrushLookupFallbacks_MatchColoursAxaml()
    {
        string desktop = SourceTree.ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Desktop");

        string colours = File.ReadAllText(Path.Combine(desktop, path2: "Styles", path3: "Colours.axaml"));
        string lookup  = File.ReadAllText(Path.Combine(desktop, path2: "Converters", path3: "ApplicationBrushLookup.cs"));

        Dictionary<string, string> named = NamedAccent()
                                           .Matches(colours)
                                           .ToDictionary(m => m.Groups[ "key" ].Value, m => m.Groups[ "hex" ].Value, StringComparer.OrdinalIgnoreCase);

        Dictionary<string, string> fallbacks = FallbackDeclaration()
                                               .Matches(lookup)
                                               .ToDictionary(m => "Accent" + m.Groups[ "name" ].Value, m => m.Groups[ "hex" ].Value, StringComparer.OrdinalIgnoreCase);

        named.Count.ShouldBe(expected: 2, "Expected AccentGreen and AccentRed in Colours.axaml.");
        fallbacks.Count.ShouldBe(expected: 2, "Expected FallbackGreen and FallbackRed in ApplicationBrushLookup.cs.");

        string[] lookupKeys = LookupKey().Matches(lookup).Select(m => m.Groups[ "key" ].Value).ToArray();

        lookupKeys.Length.ShouldBe(expected: 2, "Expected the AccentGreen and AccentRed key constants in ApplicationBrushLookup.cs.");

        foreach (string lookupKey in lookupKeys)
        {
            named.ShouldContainKey(lookupKey, $"The lookup key {lookupKey} in ApplicationBrushLookup.cs is not an x:Key in Colours.axaml.");
        }

        foreach ((string key, string hex) in named)
        {
            fallbacks[ key ].ToUpperInvariant().ShouldBe(hex.ToUpperInvariant(), $"The fallback for {key} in ApplicationBrushLookup.cs differs from Colours.axaml.");
        }
    }

    private static bool IsAllowed(string name, string value)
        => value.StartsWith('{')
           || value.Equals("Transparent", StringComparison.Ordinal)
           || (name == "Fill" && (value == "True" || value == "False"));
}
