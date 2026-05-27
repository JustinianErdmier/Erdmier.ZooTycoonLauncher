namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.Installations;

public sealed class InstallationValidityTests
{
    [Theory]
    [InlineData(true,  true,  "Valid")]
    [InlineData(false, true,  "InvalidNoExe")]
    [InlineData(true,  false, "InvalidNoIni")]
    [InlineData(false, false, "InvalidNoExeOrIni")]
    public void From_MapsHasExeAndHasIniToCorrectValidity(bool hasExe, bool hasIni, string expectedName)
    {
        InstallationValidity result = InstallationValidity.From(hasExe, hasIni);

        result.Name.ShouldBe(expectedName);
    }

    [Fact]
    public void Valid_DisplayNameIsValid()
    {
        InstallationValidity.Valid.DisplayName.ShouldBe("Valid");
        InstallationValidity.Valid.ColourToken.ShouldBe("Green");
    }

    [Fact]
    public void InvalidVariants_AllUseRedColourToken()
    {
        InstallationValidity.InvalidNoExe.ColourToken.ShouldBe("Red");
        InstallationValidity.InvalidNoIni.ColourToken.ShouldBe("Red");
        InstallationValidity.InvalidNoExeOrIni.ColourToken.ShouldBe("Red");
    }
}
