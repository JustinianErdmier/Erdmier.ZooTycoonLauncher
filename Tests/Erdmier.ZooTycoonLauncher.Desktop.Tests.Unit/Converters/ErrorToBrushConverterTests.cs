using Avalonia;
using Avalonia.Media;

using Erdmier.ZooTycoonLauncher.Desktop.Converters;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Converters;

public sealed class ErrorToBrushConverterTests
{
    private readonly ErrorToBrushConverter _converter = new();

    [ Fact ]
    public void Convert_True_IsAccentRed()
    {
        object? result = _converter.Convert(value: true, typeof(IBrush), parameter: null, CultureInfo.InvariantCulture);

        result.ShouldBeOfType<SolidColorBrush>().Color.ShouldBe(Color.Parse(s: "#C00000"));
    }

    [ Fact ]
    public void Convert_False_IsUnset()
    {
        object? result = _converter.Convert(value: false, typeof(IBrush), parameter: null, CultureInfo.InvariantCulture);

        result.ShouldBe(AvaloniaProperty.UnsetValue);
    }
}
