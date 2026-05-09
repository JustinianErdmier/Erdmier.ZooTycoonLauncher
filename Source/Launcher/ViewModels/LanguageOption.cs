namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>
///     A single entry in the language drop-down on the INI tab. Wraps a Windows LANGID + SUBLANGID pair with a human-readable display name. Used by
///     <see cref="IniSettingsViewModel.LanguageOptions" /> and <see cref="IniSettingsViewModel.SelectedLanguage" />.
/// </summary>
public sealed class LanguageOption
{
    public LanguageOption(int lang, int subLang, string displayName)
    {
        Lang        = lang;
        SubLang     = subLang;
        DisplayName = displayName;
    }

    /// <summary>Windows primary language identifier. Maps to the <c> [language]/lang </c> INI key.</summary>
    public int Lang { get; }

    /// <summary>Windows sub-language identifier. Maps to the <c> [language]/sublang </c> INI key.</summary>
    public int SubLang { get; }

    /// <summary>Display name shown in the drop-down.</summary>
    public string DisplayName { get; }

    /// <inheritdoc />
    public override string ToString() => DisplayName;
}
