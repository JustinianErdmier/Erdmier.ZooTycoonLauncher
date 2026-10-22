namespace Erdmier.ZooTycoonLauncher.Desktop.Converters;

/// <summary>
///     Converts an error flag to a foreground brush: <see langword="true" /> gives <c>AccentRed</c> from the application resources; <see langword="false" /> leaves the
///     property unset so it keeps its default foreground.
/// </summary>
public sealed class ErrorToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? ApplicationBrushLookup.AccentRed : AvaloniaProperty.UnsetValue;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always — this converter is one-way.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
