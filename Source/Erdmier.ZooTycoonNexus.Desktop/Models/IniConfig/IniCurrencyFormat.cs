namespace Erdmier.ZooTycoonNexus.Desktop.Models.IniConfig;

/// <summary>How the INI cash settings are shown: whole dollars with grouping, whatever the machine's culture.</summary>
public static class IniCurrencyFormat
{
    /// <summary>The custom format string — a dollar sign, grouped thousands and no decimals.</summary>
    public const string FormatString = "$#,0";

    /// <summary>The number format that renders and parses the dollar sign and the grouping separator the same way on every machine.</summary>
    public static NumberFormatInfo NumberFormat { get; } = CreateNumberFormat();

    /// <summary>The styles accepted when parsing typed cash: an optional dollar sign, grouping separators and surrounding spaces; no decimals or sign.</summary>
    public const NumberStyles ParsingNumberStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowThousands;

    /// <summary>Formats a whole-dollar amount.</summary>
    /// <param name="value">The amount.</param>
    /// <returns>The amount as dollars, e.g. <c>$1,000,000</c>.</returns>
    public static string Format(int value) => value.ToString(FormatString, NumberFormat);

    private static NumberFormatInfo CreateNumberFormat()
    {
        NumberFormatInfo format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();

        format.CurrencySymbol = "$";

        return NumberFormatInfo.ReadOnly(format);
    }
}
