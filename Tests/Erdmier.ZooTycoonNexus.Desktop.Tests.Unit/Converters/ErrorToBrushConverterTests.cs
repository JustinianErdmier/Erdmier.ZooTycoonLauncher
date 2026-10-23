using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;

using Erdmier.ZooTycoonNexus.Desktop.Converters;

namespace Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.Converters;

// Brushes are Avalonia objects, so these run on the headless UI thread: built on another thread once the session is up, they throw and poison the type initialiser.
public sealed class ErrorToBrushConverterTests
{
    private readonly ErrorToBrushConverter _converter = new();

    [ AvaloniaFact ]
    public void Convert_True_IsAccentRed()
    {
        object? result = _converter.Convert(value: true, typeof(IBrush), parameter: null, CultureInfo.InvariantCulture);

        result.ShouldBeOfType<SolidColorBrush>().Color.ShouldBe(Color.Parse(s: "#C00000"));
    }

    [ AvaloniaFact ]
    public void Convert_False_IsUnset()
    {
        object? result = _converter.Convert(value: false, typeof(IBrush), parameter: null, CultureInfo.InvariantCulture);

        result.ShouldBe(AvaloniaProperty.UnsetValue);
    }
}
