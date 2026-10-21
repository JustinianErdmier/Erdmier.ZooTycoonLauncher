namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>Renders a raw INI value the way the INI Config editor shows it, for read-only displays such as the Restore Previous INI differences.</summary>
public static class IniDisplayText
{
    /// <summary>Formats a raw value for display.</summary>
    /// <param name="id">The key the value belongs to.</param>
    /// <param name="value">The raw value.</param>
    /// <returns>
    ///     The matching choice label, <c>true</c> / <c>false</c> for a toggle (the key's own truth, never inverted), or dollars for a cash setting; the raw
    ///     <paramref name="value" /> when the key has no descriptor or the value does not map.
    /// </returns>
    public static string Format(IniKeyId id, string value)
    {
        if (!IniEditorCatalogue.TryGetField(id, out IniFieldDescriptor? field))
        {
            return value;
        }

        switch (field.Control)
        {
            case IniControlKind.Choice when ZooIniDefaults.TryGet(id, out IniKeySpec? spec):
                IniChoiceOption? option = field.Options.FirstOrDefault(candidate => spec.AreEquivalent(candidate.Raw, value));

                return option?.Label ?? value;

            case IniControlKind.Toggle when IniKeySpec.TryParseBool(value, out bool truth):
                return truth ? "true" : "false";

            case IniControlKind.Number when field.IsCurrency
                                            && int.TryParse(value, IniCurrencyFormat.ParsingNumberStyle, IniCurrencyFormat.NumberFormat, out int amount):
                return IniCurrencyFormat.Format(amount);

            default:
                return value;
        }
    }
}
