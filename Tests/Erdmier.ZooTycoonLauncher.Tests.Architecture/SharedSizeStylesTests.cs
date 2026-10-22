using System.Text.RegularExpressions;

namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

/// <summary>Milestone 11.1 spec C3: the repeated button width, icon sizes and faded text live in <c>Styles/Controls.axaml</c> and views opt in through <c>Classes</c>.</summary>
public sealed class SharedSizeStylesTests
{
    private static readonly Regex ImageElement = new(@"<Image\b[^>]*>", RegexOptions.Singleline);

    private static readonly Regex TextElement = new(@"<(?:TextBlock|Label|AccessText|SelectableTextBlock|Run|Span|Italic|Bold|Underline|StackPanel)\b[^>]*>", RegexOptions.Singleline);

    private static readonly Regex StyleBlock = new(@"<Style\s+Selector\s*=\s*""(?<selector>[^""]*)""[^>]*>(?<body>.*?)</Style>", RegexOptions.Singleline);

    private static readonly Regex ImageSelector = new(@"\bImage\b");

    private static readonly Regex ButtonElement = new(@"<Button\b[^>]*>", RegexOptions.Singleline);

    private static readonly Regex ButtonSelector = new(@"\bButton\b");

    private static readonly Regex TextSelector = new(@"\b(?:TextBlock|Label|AccessText|SelectableTextBlock|Run|Span|Italic|Bold|Underline|StackPanel)\b");

    private static readonly Regex QualifiedImageSize = new(@"\bImage\.(?:Width|Height)\s*=\s*""(?:32|16)""");

    /// <summary>No view may give a <c>Button</c> a literal <c>MinWidth</c>, use <c>MinWidth="75"</c> anywhere, or size an image at 32 or 16 pixels; the shared styles carry those values.</summary>
    [ Fact ]
    public void Views_UseSharedSizeStyles()
    {
        string desktop = ProjectPaths.Desktop;

        List<string> violations = [];

        foreach (string file in EnumerateViews(desktop))
        {
            string text = XamlAttributeScanner.BlankComments(File.ReadAllText(file));

            HashSet<int> widthLines = [];

            foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(text, "MinWidth"))
            {
                if (match.Value == "75" && widthLines.Add(match.Line))
                {
                    violations.Add($"{Path.GetRelativePath(desktop, file)}:{match.Line}: {match.Text} (use Classes=\"DialogButton\")");
                }
            }

            foreach ((int offset, string scope) in Scopes(text, ButtonElement, ButtonSelector))
            {
                foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(scope, "MinWidth"))
                {
                    if (!match.Value.StartsWith('{') && widthLines.Add(offset + match.Line))
                    {
                        violations.Add($"{Path.GetRelativePath(desktop, file)}:{offset + match.Line}: {match.Text} (use Classes=\"DialogButton\")");
                    }
                }
            }

            foreach ((int offset, string scope) in Scopes(text, ImageElement, ImageSelector))
            {
                foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(scope, "Width", "Height"))
                {
                    if (match.Value is "32" or "16")
                    {
                        violations.Add($"{Path.GetRelativePath(desktop, file)}:{offset + match.Line}: {match.Text} (use Classes=\"IconLarge\" or \"IconSmall\")");
                    }
                }
            }

            foreach (Match match in QualifiedImageSize.Matches(text))
            {
                int line = text.AsSpan(start: 0, match.Index).Count(value: '\n') + 1;

                violations.Add($"{Path.GetRelativePath(desktop, file)}:{line}: {match.Value} (use Classes=\"IconLarge\" or \"IconSmall\")");
            }
        }

        violations.ShouldBeEmpty("Repeated sizes found; use the shared styles in Styles/Controls.axaml:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    /// <summary>No text element may carry a literal <c>Opacity</c> other than <c>1</c>; muted text takes the <c>MutedText</c> class or a named foreground.</summary>
    [ Fact ]
    public void Views_DoNotFadeTextWithOpacity()
    {
        string desktop = ProjectPaths.Desktop;

        List<string> violations = [];

        foreach (string file in EnumerateViews(desktop))
        {
            string text = XamlAttributeScanner.BlankComments(File.ReadAllText(file));

            foreach ((int offset, string scope) in Scopes(text, TextElement, TextSelector))
            {
                foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(scope, "Opacity"))
                {
                    if (match.Value != "1")
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
        string desktop = ProjectPaths.Desktop;

        List<string> violations = [];

        foreach (string file in EnumerateViews(desktop))
        {
            string text = XamlAttributeScanner.BlankComments(File.ReadAllText(file));

            foreach (Match image in ImageElement.Matches(text))
            {
                if (!Regex.IsMatch(image.Value, @"Classes\s*=\s*""[^""]*\bIcon(?:Large|Small)\b"))
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

    /// <summary>Yields the start tags matching <paramref name="element" /> and the bodies of styles whose selector matches <paramref name="selector" />, each with its 0-based line offset.</summary>
    private static IEnumerable<(int Offset, string Scope)> Scopes(string text, Regex element, Regex selector)
    {
        foreach (Match match in element.Matches(text))
        {
            yield return (text.AsSpan(start: 0, match.Index).Count(value: '\n'), match.Value);
        }

        foreach (Match match in StyleBlock.Matches(text))
        {
            if (selector.IsMatch(match.Groups[ "selector" ].Value))
            {
                yield return (text.AsSpan(start: 0, match.Groups[ "body" ].Index).Count(value: '\n'), match.Groups[ "body" ].Value);
            }
        }
    }

    private static IEnumerable<string> EnumerateViews(string desktop)
        => SourceTree.EnumerateSources(desktop, pattern: "*.axaml")
                     .Where(f => !f.EndsWith(Path.Combine("Styles", "Controls.axaml"), StringComparison.OrdinalIgnoreCase));
}
