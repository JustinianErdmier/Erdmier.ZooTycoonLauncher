namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Strongly typed representation of the <c> [language] </c> section of <c> zoo.ini </c>.</summary>
/// <remarks><see cref="Lang" /> and <see cref="SubLang" /> correspond to Windows LANGID/SUBLANGID constants (e.g. <c> 9, 1 </c> = English (United States)).</remarks>
public class LanguageSettings
{
    /// <summary>Windows primary language identifier (LANGID).</summary>
    public int Lang { get; set; } = 9;

    /// <summary>Windows sub-language identifier (SUBLANGID).</summary>
    public int SubLang { get; set; } = 1;
}
