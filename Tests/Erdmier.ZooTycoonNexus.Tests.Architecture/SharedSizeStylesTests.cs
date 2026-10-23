using System.Globalization;
using System.Text.RegularExpressions;

namespace Erdmier.ZooTycoonNexus.Tests.Architecture;

/// <summary>Milestone 11.1 spec C3: the repeated button width, icon sizes and faded text live in <c>Styles/Controls.axaml</c> and views opt in through <c>Classes</c>.</summary>
public sealed partial class SharedSizeStylesTests
{
    [ GeneratedRegex(@"<Image\b[^>]*>", RegexOptions.Singleline) ]
    private static partial Regex ImageElement();

    [ GeneratedRegex(@"<(?:TextBlock|Label|AccessText|SelectableTextBlock|Run|Span|Italic|Bold|Underline|StackPanel)\b[^>]*>", RegexOptions.Singleline) ]
    private static partial Regex TextElement();

    [ GeneratedRegex(@"<Style\s+Selector\s*=\s*""(?<selector>[^""]*)""[^>]*>(?<body>.*?)</Style>", RegexOptions.Singleline) ]
    private static partial Regex StyleBlock();

    [ GeneratedRegex(@"\bImage\b") ]
    private static partial Regex ImageSelector();

    [ GeneratedRegex(@"<Button\b[^>]*>", RegexOptions.Singleline) ]
    private static partial Regex ButtonElement();

    [ GeneratedRegex(@"\bButton\b") ]
    private static partial Regex ButtonSelector();

    [ GeneratedRegex(@"\b(?:TextBlock|Label|AccessText|SelectableTextBlock|Run|Span|Italic|Bold|Underline|StackPanel)\b") ]
    private static partial Regex TextSelector();

    [ GeneratedRegex(@"Classes\s*=\s*""[^""]*\bIcon(?:Large|Small)\b") ]
    private static partial Regex IconClasses();

    [ GeneratedRegex(@"\bImage\.(?:Width|Height)\s*=\s*""(?:32|16)""") ]
    private static partial Regex QualifiedImageSize();

    /// <summary>No view may give a <c>Button</c> a literal <c>MinWidth</c>, use <c>MinWidth="75"</c> anywhere, or size an image at 32 or 16 pixels; the shared styles carry those values.</summary>
    [ Fact ]
    public void Views_UseSharedSizeStyles()
    {
        string desktop = ProjectPaths.Desktop;

        List<string> violations = [];

        foreach (string file in EnumerateViews(desktop))
        {
            string text = XamlAttributeScanner.BlankComments(File.ReadAllText(file));

            HashSet<int> widthIndexes = [];

            foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(text, "MinWidth"))
            {
                if (match.Value == "75" && widthIndexes.Add(match.Index))
                {
                    violations.Add($"{Path.GetRelativePath(desktop, file)}:{match.Line}: {match.Text} (use Classes=\"DialogButton\")");
                }
            }

            foreach ((int start, string scope) in Scopes(text, ButtonElement(), ButtonSelector()))
            {
                foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(scope, "MinWidth"))
                {
                    if (!match.Value.StartsWith('{') && widthIndexes.Add(start + match.Index))
                    {
                        violations.Add($"{Path.GetRelativePath(desktop, file)}:{LineOf(text, start) + match.Line - 1}: {match.Text} (use Classes=\"DialogButton\")");
                    }
                }
            }

            foreach ((int start, string scope) in Scopes(text, ImageElement(), ImageSelector()))
            {
                foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(scope, "Width", "Height"))
                {
                    if (match.Value is "32" or "16")
                    {
                        violations.Add($"{Path.GetRelativePath(desktop, file)}:{LineOf(text, start) + match.Line - 1}: {match.Text} (use Classes=\"IconLarge\" or \"IconSmall\")");
                    }
                }
            }

            foreach (Match match in QualifiedImageSize().Matches(text))
            {
                violations.Add($"{Path.GetRelativePath(desktop, file)}:{LineOf(text, match.Index)}: {match.Value} (use Classes=\"IconLarge\" or \"IconSmall\")");
            }
        }

        violations.ShouldBeEmpty("Repeated sizes found; use the shared styles in Styles/Controls.axaml:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    /// <summary>No text element may carry a literal <c>Opacity</c> other than <c>1</c> (<c>1.0</c> counts as <c>1</c>; bindings and resources are skipped); muted text takes the <c>MutedText</c> class or a named foreground.</summary>
    [ Fact ]
    public void Views_DoNotFadeTextWithOpacity()
    {
        string desktop = ProjectPaths.Desktop;

        List<string> violations = [];

        foreach (string file in EnumerateViews(desktop))
        {
            string text = XamlAttributeScanner.BlankComments(File.ReadAllText(file));

            foreach ((int start, string scope) in Scopes(text, TextElement(), TextSelector()))
            {
                foreach (XamlAttributeMatch match in XamlAttributeScanner.Scan(scope, "Opacity"))
                {
                    if (!IsBindingOrResource(match.Value) && !IsOpaque(match.Value))
                    {
                        violations.Add($"{Path.GetRelativePath(desktop, file)}:{LineOf(text, start) + match.Line - 1}: {match.Text} (use Classes=\"MutedText\" or a named foreground)");
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

        int icons = 0;

        foreach (string file in EnumerateViews(desktop))
        {
            string text = XamlAttributeScanner.BlankComments(File.ReadAllText(file));

            foreach (Match image in ImageElement().Matches(text))
            {
                if (!IconClasses().IsMatch(image.Value))
                {
                    continue;
                }

                icons++;

                bool crisp = XamlAttributeScanner.Scan(image.Value, "BitmapInterpolationMode").Any(m => m.Value == "None");

                if (!crisp)
                {
                    violations.Add($"{Path.GetRelativePath(desktop, file)}:{LineOf(text, image.Index)}: icon without RenderOptions.BitmapInterpolationMode=\"None\"");
                }
            }
        }

        icons.ShouldBeGreaterThan(expected: 0, customMessage: "No IconLarge or IconSmall images were inspected; the icon-class pattern no longer matches.");

        violations.ShouldBeEmpty("Smoothed icons found:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    /// <summary>Yields the start tags matching <paramref name="element" /> and the bodies of styles whose selector matches <paramref name="selector" />, each with its 0-based start index in <paramref name="text" />.</summary>
    private static IEnumerable<(int Start, string Scope)> Scopes(string text, Regex element, Regex selector)
    {
        foreach (Match match in element.Matches(text))
        {
            yield return (match.Index, match.Value);
        }

        foreach (Match match in StyleBlock().Matches(text))
        {
            if (selector.IsMatch(match.Groups[ "selector" ].Value))
            {
                yield return (match.Groups[ "body" ].Index, match.Groups[ "body" ].Value);
            }
        }
    }

    private static int LineOf(string text, int index) => text.AsSpan(start: 0, index).Count(value: '\n') + 1;

    private static bool IsBindingOrResource(string value) => value.StartsWith('{');

    private static bool IsOpaque(string value) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double opacity) && opacity == 1d;

    private static IEnumerable<string> EnumerateViews(string desktop)
        => SourceTree.EnumerateSources(desktop, pattern: "*.axaml")
                     .Where(f => !f.EndsWith(Path.Combine("Styles", "Controls.axaml"), StringComparison.OrdinalIgnoreCase));
}
