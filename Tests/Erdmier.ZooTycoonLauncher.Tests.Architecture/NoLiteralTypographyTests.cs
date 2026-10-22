namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

/// <summary>Milestone 11.1 spec C1: font families, sizes and weights are defined once in <c>Styles/Typography.axaml</c> and referenced by name everywhere else.</summary>
public sealed class NoLiteralTypographyTests
{
    /// <summary>No view may hold a literal font family, size or weight; every such attribute or style setter references a resource.</summary>
    [ Fact ]
    public void Views_UseNamedTypography()
    {
        string desktop = SourceTree.ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Desktop");

        List<string> violations = [];

        foreach (string file in SourceTree.EnumerateSources(desktop, pattern: "*.axaml"))
        {
            if (file.EndsWith(Path.Combine("Styles", "Typography.axaml"), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string text = File.ReadAllText(file);

            foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(text, "FontFamily", "FontSize", "FontWeight"))
            {
                if (match.Value.StartsWith('{'))
                {
                    continue;
                }

                violations.Add($"{Path.GetRelativePath(desktop, file)}:{match.Line}: {match.Text}");
            }
        }

        violations.ShouldBeEmpty("Literal typography found; define it in Styles/Typography.axaml and reference it by name:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }
}
