namespace Erdmier.ZooTycoonNexus.Desktop.Models.IniConfig;

/// <summary>
///     Renders a key and its raw value the way the Game Config editor shows them in the chosen style, for read-only displays such as the Restore Previous INI differences.
/// </summary>
public static class IniDisplayText
{
    private const string Breadcrumb = " › ";

    /// <summary>Names a key for display.</summary>
    /// <param name="style">The editor style.</param>
    /// <param name="id">The key.</param>
    /// <returns>
    ///     In Friendly style, <c>Page › Group › Label</c> (the group is omitted when the layout gives it no sub-header); otherwise <c>[section]/key</c>, which is also what a key
    ///     the Friendly layout does not place reads as.
    /// </returns>
    public static string KeyText(IniEditorStyle style, IniKeyId id)
    {
        if (style == IniEditorStyle.Friendly && IniEditorLayouts.TryLocate(style, id, out IniFieldLocation? location))
        {
            return location.Group.SubHeader is { } group
                ? string.Join(Breadcrumb, location.Section.Title, group, location.Field.Label)
                : string.Join(Breadcrumb, location.Section.Title, location.Field.Label);
        }

        return id.ToString();
    }

    /// <summary>Formats a raw value for display.</summary>
    /// <param name="style">The editor style.</param>
    /// <param name="id">The key the value belongs to.</param>
    /// <param name="value">The raw value.</param>
    /// <returns>
    ///     The matching choice label, a toggle's truth (<c>true</c> / <c>false</c> in INI style, never inverted; <c>On</c> / <c>Off</c> by meaning in Friendly style), a volume as
    ///     a percentage (Friendly style) or dollars for a cash setting; the raw <paramref name="value" /> when the key has no descriptor or the value does not map.
    /// </returns>
    public static string Format(IniEditorStyle style, IniKeyId id, string value)
    {
        bool friendly = style == IniEditorStyle.Friendly;

        IniFieldDescriptor? field = null;

        if (friendly)
        {
            if (IniEditorLayouts.TryLocate(style, id, out IniFieldLocation? location))
            {
                field = location.Field;
            }
        }
        else if (IniEditorCatalogue.TryGetField(id, out IniFieldDescriptor? iniField))
        {
            field = iniField;
        }

        if (field is null)
        {
            return value;
        }

        switch (field.Control)
        {
            case IniControlKind.Choice when ZooIniDefaults.TryGet(id, out IniKeySpec? spec):
                IniChoiceOption? option = field.Options.FirstOrDefault(candidate => spec.AreEquivalent(candidate.Raw, value));

                return option?.Label ?? value;

            case IniControlKind.Toggle when IniKeySpec.TryParseBool(value, out bool truth):
                if (!friendly)
                {
                    return truth ? "true" : "false";
                }

                return field.Conversion.ToDisplay(truth ? 1 : 0) != 0 ? "On" : "Off";

            case IniControlKind.Slider when friendly && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int raw):
                return $"{IniVolumePercent.FromRaw(field.Conversion, raw)} %";

            case IniControlKind.Number when field.IsCurrency
                                            && int.TryParse(value, IniCurrencyFormat.ParsingNumberStyle, IniCurrencyFormat.NumberFormat, out int amount):
                return IniCurrencyFormat.Format(amount);

            default:
                return value;
        }
    }
}
