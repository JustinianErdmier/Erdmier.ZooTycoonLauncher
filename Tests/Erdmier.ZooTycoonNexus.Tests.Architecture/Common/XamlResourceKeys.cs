using System.Text.RegularExpressions;

namespace Erdmier.ZooTycoonNexus.Tests.Architecture.Common;

/// <summary>Collects the <c>x:Key</c> resources declared in XAML, each with the element type that declares it, for the tests that check resource references.</summary>
public static partial class XamlResourceKeys
{
    [ GeneratedRegex(@"<(?<element>[\w:.]+)\b(?:[^>""]|""[^""]*"")*?\sx:Key\s*=\s*""(?<key>[^""]+)""", RegexOptions.Singleline) ]
    private static partial Regex KeyedElement();

    /// <summary>Finds every keyed element in <paramref name="text" />, ignoring comments.</summary>
    /// <param name="text">The XAML source.</param>
    /// <returns>A map from each key to the name of the element that declares it (<c>x:Double</c>, <c>Thickness</c>, <c>SolidColorBrush</c>…).</returns>
    public static IReadOnlyDictionary<string, string> Collect(string text)
    {
        Dictionary<string, string> keys = new(StringComparer.Ordinal);

        foreach (Match match in KeyedElement().Matches(XamlAttributeScanner.BlankComments(text)))
        {
            keys[ match.Groups[ "key" ].Value ] = match.Groups[ "element" ].Value;
        }

        return keys;
    }

    /// <summary>Finds the keys the whole Desktop project shares: those declared in <c>Styles/**/*.axaml</c> and <c>App.axaml</c>.</summary>
    /// <param name="desktop">The Desktop project directory.</param>
    /// <returns>A map from each shared key to its declaring element name.</returns>
    public static IReadOnlyDictionary<string, string> CollectShared(string desktop)
    {
        Dictionary<string, string> keys = new(StringComparer.Ordinal);

        IEnumerable<string> files = Directory.EnumerateFiles(Path.Combine(desktop, path2: "Styles"), searchPattern: "*.axaml", SearchOption.AllDirectories)
                                             .Append(Path.Combine(desktop, path2: "App.axaml"));

        foreach (string file in files)
        {
            foreach ((string key, string element) in Collect(File.ReadAllText(file)))
            {
                keys[ key ] = element;
            }
        }

        return keys;
    }
}
