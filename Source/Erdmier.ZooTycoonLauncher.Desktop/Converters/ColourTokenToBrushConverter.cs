using Avalonia.Media;

namespace Erdmier.ZooTycoonLauncher.Desktop.Converters;

/// <summary>
///     Converts a colour token string to an <see cref="IBrush" />. Recognised tokens: <c>Green</c> → <see cref="Brushes.Green" />;
///     any other value → <see cref="Brushes.Red" />.
/// </summary>
public sealed class ColourTokenToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is "Green" ? Brushes.Green : Brushes.Red;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always — this converter is one-way.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
