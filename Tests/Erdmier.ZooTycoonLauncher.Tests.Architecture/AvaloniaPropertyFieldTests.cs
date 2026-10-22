using System.Text.RegularExpressions;

namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

/// <summary>Avalonia's XAML compiler assigns a property through its <c>AvaloniaProperty</c> field when the value is a resource or binding, so that field must be accessible.</summary>
public sealed class AvaloniaPropertyFieldTests
{
    private static readonly Regex PropertyField = new(
        @"^\s*(?<modifiers>(?:\w+\s+)*?)static\s+readonly\s+(?:StyledProperty|DirectProperty|AttachedProperty|AvaloniaProperty)\b[^=;]*?\s(?<name>\w+)\s*=",
        RegexOptions.Multiline);

    /// <summary>Every static readonly Avalonia property field in the Desktop project is <c>public</c>.</summary>
    [ Fact ]
    public void AvaloniaProperties_ArePublic()
    {
        string desktop = ProjectPaths.Desktop;

        List<string> violations = [];

        foreach (string file in SourceTree.EnumerateSources(desktop, pattern: "*.cs"))
        {
            string text = File.ReadAllText(file);

            foreach (Match match in PropertyField.Matches(text))
            {
                if (!Regex.IsMatch(match.Groups[ "modifiers" ].Value, @"\bpublic\b"))
                {
                    int line = text.AsSpan(start: 0, match.Groups[ "name" ].Index).Count(value: '\n') + 1;

                    violations.Add($"{Path.GetRelativePath(desktop, file)}:{line}: {match.Groups[ "name" ].Value} must be public");
                }
            }
        }

        violations.ShouldBeEmpty("Avalonia property fields must be public so compiled XAML can set resource and binding values:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }
}
