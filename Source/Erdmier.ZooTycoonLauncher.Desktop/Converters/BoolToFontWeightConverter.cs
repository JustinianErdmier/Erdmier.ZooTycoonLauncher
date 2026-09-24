using Avalonia.Media;

namespace Erdmier.ZooTycoonLauncher.Desktop.Converters;

/// <summary>
///     Converts a <see cref="bool" /> to a <see cref="FontWeight" />: <see langword="true" /> → <see cref="FontWeight.Bold" />,
///     <see langword="false" /> → <see cref="FontWeight.Normal" />.
/// </summary>
public sealed class BoolToFontWeightConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? FontWeight.Bold : FontWeight.Normal;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always — this converter is one-way.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
