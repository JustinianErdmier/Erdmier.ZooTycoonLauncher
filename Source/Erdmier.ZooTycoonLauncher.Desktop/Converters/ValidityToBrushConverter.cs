namespace Erdmier.ZooTycoonLauncher.Desktop.Converters;

/// <summary>
///     Converts an <see cref="InstallationValidity" /> to a status foreground brush: <c>AccentGreen</c> for <see cref="InstallationValidity.Valid" />, <c>AccentRed</c>
///     for every invalid state. The brushes come from the application resources.
/// </summary>
public sealed class ValidityToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ReferenceEquals(value, InstallationValidity.Valid) ? ApplicationBrushLookup.AccentGreen : ApplicationBrushLookup.AccentRed;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always — this converter is one-way.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
