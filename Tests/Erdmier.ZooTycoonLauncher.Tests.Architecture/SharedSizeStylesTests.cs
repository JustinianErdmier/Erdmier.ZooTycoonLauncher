using System.Text.RegularExpressions;

namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

/// <summary>Milestone 11.1 spec C3: the repeated button width, icon sizes and faded text live in <c>Styles/Controls.axaml</c> and views opt in through <c>Classes</c>.</summary>
public sealed class SharedSizeStylesTests
{
    private static readonly Regex ImageElement = new(@"<Image\b[^>]*>", RegexOptions.Singleline);

    private static readonly Regex TextElement = new(@"<(?:TextBlock|Run|StackPanel)\b[^>]*>", RegexOptions.Singleline);

    /// <summary>No view may size a button with <c>MinWidth="75"</c> or an image at 32 or 16 pixels; the shared styles carry those values.</summary>
    [ Fact ]
    public void Views_UseSharedSizeStyles()
    {
        string desktop = SourceTree.ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Desktop");

        List<string> violations = [];

        foreach (string file in EnumerateViews(desktop))
        {
            string text = File.ReadAllText(file);

            foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(text, "MinWidth"))
            {
                if (match.Value == "75")
                {
                    violations.Add($"{Path.GetRelativePath(desktop, file)}:{match.Line}: {match.Text} (use Classes=\"DialogButton\")");
                }
            }

            foreach (Match image in ImageElement.Matches(text))
            {
                int offset = text.AsSpan(start: 0, image.Index).Count(value: '\n');

                foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(image.Value, "Width", "Height"))
                {
                    if (match.Value is "32" or "16")
                    {
                        violations.Add($"{Path.GetRelativePath(desktop, file)}:{offset + match.Line}: {match.Text} (use Classes=\"IconLarge\" or \"IconSmall\")");
                    }
                }
            }
        }

        violations.ShouldBeEmpty("Repeated sizes found; use the shared styles in Styles/Controls.axaml:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    /// <summary>No text may be faded with <c>Opacity="0.6"</c>; muted text takes the <c>MutedText</c> class or a named foreground.</summary>
    [ Fact ]
    public void Views_DoNotFadeTextWithOpacity()
    {
        string desktop = SourceTree.ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Desktop");

        List<string> violations = [];

        foreach (string file in EnumerateViews(desktop))
        {
            string text = File.ReadAllText(file);

            foreach (Match element in TextElement.Matches(text))
            {
                int offset = text.AsSpan(start: 0, element.Index).Count(value: '\n');

                foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(element.Value, "Opacity"))
                {
                    if (match.Value == "0.6")
                    {
                        violations.Add($"{Path.GetRelativePath(desktop, file)}:{offset + match.Line}: {match.Text} (use Classes=\"MutedText\" or a named foreground)");
                    }
                }
            }
        }

        violations.ShouldBeEmpty("Faded text found; use a named foreground instead of Opacity:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    ///     Every icon draws pixel-crisp (spec decision 1a). Avalonia 11.3 cannot style <c>RenderOptions.BitmapInterpolationMode</c>, so each <c>IconLarge</c> or <c>IconSmall</c> image must set it
    ///     to <c>None</c> itself.
    /// </summary>
    [ Fact ]
    public void Icons_DrawWithoutSmoothing()
    {
        string desktop = SourceTree.ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Desktop");

        List<string> violations = [];

        foreach (string file in EnumerateViews(desktop))
        {
            string text = File.ReadAllText(file);

            foreach (Match image in ImageElement.Matches(text))
            {
                if (!Regex.IsMatch(image.Value, @"Classes\s*=\s*""Icon(?:Large|Small)"""))
                {
                    continue;
                }

                bool crisp = XamlAttributeScanner.Scan(image.Value, "BitmapInterpolationMode").Any(m => m.Value == "None");

                if (!crisp)
                {
                    int line = text.AsSpan(start: 0, image.Index).Count(value: '\n') + 1;

                    violations.Add($"{Path.GetRelativePath(desktop, file)}:{line}: icon without RenderOptions.BitmapInterpolationMode=\"None\"");
                }
            }
        }

        violations.ShouldBeEmpty("Smoothed icons found:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> EnumerateViews(string desktop)
        => SourceTree.EnumerateSources(desktop, pattern: "*.axaml")
                     .Where(f => !f.EndsWith(Path.Combine("Styles", "Controls.axaml"), StringComparison.OrdinalIgnoreCase));
}
