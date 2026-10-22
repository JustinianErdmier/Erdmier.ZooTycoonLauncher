using Avalonia.Headless.XUnit;
using Avalonia.Media;

using Erdmier.ZooTycoonLauncher.Desktop.Converters;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Converters;

// Brushes are Avalonia objects, so these run on the headless UI thread: built on another thread once the session is up, they throw and poison the type initialiser.
public sealed class ValidityToBrushConverterTests
{
    private readonly ValidityToBrushConverter _converter = new();

    [ AvaloniaFact ]
    public void Convert_Valid_IsAccentGreen()
    {
        object? result = _converter.Convert(InstallationValidity.Valid, typeof(IBrush), parameter: null, CultureInfo.InvariantCulture);

        result.ShouldBeOfType<SolidColorBrush>().Color.ShouldBe(Color.Parse(s: "#008000"));
    }

    [ AvaloniaTheory ]
    [ MemberData(nameof(InvalidVariants)) ]
    public void Convert_Invalid_IsAccentRed(string validityName)
    {
        object? result = _converter.Convert(InstallationValidity.FromName(validityName), typeof(IBrush), parameter: null, CultureInfo.InvariantCulture);

        result.ShouldBeOfType<SolidColorBrush>().Color.ShouldBe(Color.Parse(s: "#C00000"));
    }

    // Names, not the smart enum: [ AvaloniaTheory ] needs serialisable data so xUnit can enumerate the cases.
    public static TheoryData<string> InvalidVariants()
        => new() { InstallationValidity.InvalidNoExe.Name, InstallationValidity.InvalidNoIni.Name, InstallationValidity.InvalidNoExeOrIni.Name };
}
