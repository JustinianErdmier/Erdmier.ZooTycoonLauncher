using System.Text.RegularExpressions;

namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

/// <summary>Milestone 11.1: a misspelt token or resource name fails at view load, so these tests catch it at build time, naming the file and line.</summary>
public sealed class TokenReferencesTests
{
    private static readonly Regex ResourceReference = new(@"\{(?<kind>Static|Dynamic)Resource\s+(?<name>[A-Za-z_]\w*)\s*\}");

    private static readonly Regex SpaceExtension = new(@"\{tokens:Space\s+(?<arguments>[^}]*)\}");

    private static readonly string[] SpacingProperties = [ "Spacing", "RowSpacing", "ColumnSpacing", "HorizontalSpacing", "VerticalSpacing" ];

    /// <summary>Every <c>{StaticResource Name}</c> and <c>{DynamicResource Name}</c> in a view names a resource declared in <c>Styles/</c>, <c>App.axaml</c> or the same file.</summary>
    [ Fact ]
    public void ResourceReferences_Resolve()
    {
        string desktop = ProjectPaths.Desktop;

        IReadOnlyDictionary<string, string> shared = XamlResourceKeys.CollectShared(desktop);

        List<string> violations = [];

        foreach (string file in SourceTree.EnumerateSources(desktop, pattern: "*.axaml"))
        {
            string text = XamlAttributeScanner.BlankComments(File.ReadAllText(file));

            IReadOnlyDictionary<string, string> local = XamlResourceKeys.Collect(text);

            foreach (Match match in ResourceReference.Matches(text))
            {
                string name = match.Groups[ "name" ].Value;

                if (!shared.ContainsKey(name) && !local.ContainsKey(name))
                {
                    violations.Add($"{Path.GetRelativePath(desktop, file)}:{LineOf(text, match.Index)}: {match.Value} names no resource");
                }
            }
        }

        violations.ShouldBeEmpty("Unresolved resource references found:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    /// <summary>Every token named inside <c>{tokens:Space …}</c>, positional or as <c>Side=Name</c>, is an <c>x:Double</c> resource.</summary>
    [ Fact ]
    public void SpaceTokens_NameDoubleResources()
    {
        string desktop = ProjectPaths.Desktop;

        IReadOnlyDictionary<string, string> shared = XamlResourceKeys.CollectShared(desktop);

        List<string> violations = [];

        foreach (string file in SourceTree.EnumerateSources(desktop, pattern: "*.axaml"))
        {
            string text = XamlAttributeScanner.BlankComments(File.ReadAllText(file));

            IReadOnlyDictionary<string, string> local = XamlResourceKeys.Collect(text);

            foreach (Match match in SpaceExtension.Matches(text))
            {
                foreach (string argument in match.Groups[ "arguments" ].Value.Split(','))
                {
                    string name = argument.Contains('=') ? argument[ (argument.IndexOf('=') + 1).. ].Trim() : argument.Trim();

                    string? element = shared.TryGetValue(name, out string? sharedElement) ? sharedElement : local.GetValueOrDefault(name);

                    if (element != "x:Double")
                    {
                        string found = element is null ? "no such resource" : $"a {element}";

                        violations.Add($"{Path.GetRelativePath(desktop, file)}:{LineOf(text, match.Index)}: '{name}' in {match.Value} is {found}, not an x:Double");
                    }
                }
            }
        }

        violations.ShouldBeEmpty("Spacing tokens that are not x:Double resources:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    /// <summary>A spacing property that references a resource references an <c>x:Double</c>, and a <c>Margin</c> or <c>Padding</c> that does references a <c>Thickness</c>.</summary>
    [ Fact ]
    public void SpacingResources_HaveTheRightType()
    {
        string desktop = ProjectPaths.Desktop;

        IReadOnlyDictionary<string, string> shared = XamlResourceKeys.CollectShared(desktop);

        List<string> violations = [];

        foreach (string file in SourceTree.EnumerateSources(desktop, pattern: "*.axaml"))
        {
            string text = File.ReadAllText(file);

            IReadOnlyDictionary<string, string> local = XamlResourceKeys.Collect(text);

            foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(text, [ ..SpacingProperties, "Margin", "Padding" ]))
            {
                Match reference = ResourceReference.Match(match.Value);

                if (!reference.Success)
                {
                    continue;
                }

                string expected = SpacingProperties.Contains(match.Name) ? "x:Double" : "Thickness";

                string name = reference.Groups[ "name" ].Value;

                string? element = shared.TryGetValue(name, out string? sharedElement) ? sharedElement : local.GetValueOrDefault(name);

                if (element is not null && element != expected)
                {
                    violations.Add($"{Path.GetRelativePath(desktop, file)}:{match.Line}: {match.Text} references a {element}, expected {expected}");
                }
            }
        }

        violations.ShouldBeEmpty("Spacing resources of the wrong type:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    private static int LineOf(string text, int index) => text.AsSpan(start: 0, index).Count(value: '\n') + 1;
}
