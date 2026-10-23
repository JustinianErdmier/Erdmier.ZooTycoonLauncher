using System.Text.RegularExpressions;

namespace Erdmier.ZooTycoonNexus.Tests.Architecture;

/// <summary>Milestone 11.1 spec C4 and C5: spacing is defined once in <c>Styles/Spacing.axaml</c> and referenced by name everywhere else.</summary>
public sealed class NoLiteralSpacingTests
{
    /// <summary>No view may hold a literal margin, padding or spacing; every such attribute or style setter references a resource, or is <c>0</c>.</summary>
    [ Fact ]
    public void Views_UseSpacingTokens()
    {
        string desktop = ProjectPaths.Desktop;

        List<string> violations = [];

        foreach (string file in SourceTree.EnumerateSources(desktop, pattern: "*.axaml"))
        {
            if (file.EndsWith(Path.Combine("Styles", "Spacing.axaml"), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string text = File.ReadAllText(file);

            foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(text, "Margin", "Padding", "Spacing", "RowSpacing", "ColumnSpacing", "HorizontalSpacing", "VerticalSpacing"))
            {
                if (match.Value.StartsWith('{') || match.Value == "0")
                {
                    continue;
                }

                violations.Add($"{Path.GetRelativePath(desktop, file)}:{match.Line}: {match.Text}");
            }
        }

        violations.ShouldBeEmpty("Literal spacing found; define it in Styles/Spacing.axaml and reference it by name:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    /// <summary>The derived resources in <c>Spacing.axaml</c> match their tokens: each icon margin is its icon gap token minus <c>GapRelated</c>, and the group box padding is built from the group box tokens.</summary>
    [ Fact ]
    public void DerivedResources_MatchTheTokens()
    {
        string desktop = ProjectPaths.Desktop;

        string text = File.ReadAllText(Path.Combine(desktop, path2: "Styles", path3: "Spacing.axaml"));

        string Capture(string pattern, string name)
        {
            Match match = Regex.Match(text, pattern);

            return match.Success ? match.Groups[ "v" ].Value : throw new InvalidOperationException($"Spacing.axaml has no resource '{name}' in the expected form.");
        }

        double Parse(string value) => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        double Token(string name) => Parse(Capture($@"<x:Double\s+x:Key=""{name}"">(?<v>[\d.]+)</x:Double>", name));

        double Margin(string name) => Parse(Capture($@"<Thickness\s+x:Key=""{name}"">0,0,(?<v>[\d.]+),0</Thickness>", name));

        double[] Padding(string name) => Capture($@"<Thickness\s+x:Key=""{name}"">(?<v>[^<]*)</Thickness>", name).Split(',').Select(Parse).ToArray();

        Margin("IconBesideBodyMargin").ShouldBe(Token("GapIconBody") - Token("GapRelated"));
        Margin("IconBesideMessageMargin").ShouldBe(Token("GapIconMessage") - Token("GapRelated"));

        double[] padding = Padding("GroupBoxPadding");

        padding.ShouldBe([ Token("GroupBoxInset"), Token("GroupBoxFirstControl") - Token("GroupBoxHeaderHeight"), Token("GroupBoxInset"), Token("GroupBoxBottom") ]);
    }
}
