using Avalonia.Media;

using Erdmier.ZooTycoonLauncher.Desktop.Converters;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Converters;

public sealed class ValidityToBrushConverterTests
{
    private readonly ValidityToBrushConverter _converter = new();

    [ Fact ]
    public void Convert_Valid_IsAccentGreen()
    {
        object? result = _converter.Convert(InstallationValidity.Valid, typeof(IBrush), parameter: null, CultureInfo.InvariantCulture);

        result.ShouldBeOfType<SolidColorBrush>().Color.ShouldBe(Color.Parse(s: "#008000"));
    }

    [ Theory ]
    [ MemberData(nameof(InvalidVariants)) ]
    public void Convert_Invalid_IsAccentRed(InstallationValidity validity)
    {
        object? result = _converter.Convert(validity, typeof(IBrush), parameter: null, CultureInfo.InvariantCulture);

        result.ShouldBeOfType<SolidColorBrush>().Color.ShouldBe(Color.Parse(s: "#C00000"));
    }

    public static TheoryData<InstallationValidity> InvalidVariants()
        => new() { InstallationValidity.InvalidNoExe, InstallationValidity.InvalidNoIni, InstallationValidity.InvalidNoExeOrIni };
}
