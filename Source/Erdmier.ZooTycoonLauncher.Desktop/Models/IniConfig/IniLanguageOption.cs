namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>One curated Windows language (for inspiration: the Ref build's <c>IniSettingsViewModel.LanguageOptions</c>).</summary>
/// <param name="Lang">The Windows LANGID written to <c>[language]/lang</c>.</param>
/// <param name="SubLang">The Windows SUBLANGID written to <c>[language]/sublang</c>.</param>
/// <param name="Label">The text shown in the combo.</param>
public sealed record IniLanguageOption(int Lang, int SubLang, string Label);
