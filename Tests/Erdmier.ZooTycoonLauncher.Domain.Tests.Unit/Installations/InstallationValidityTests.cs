namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.Installations;

public sealed class InstallationValidityTests
{
    [ Theory ]
    [ InlineData(true, true, "Valid") ]
    [ InlineData(false, true, "InvalidNoExe") ]
    [ InlineData(true, false, "InvalidNoIni") ]
    [ InlineData(false, false, "InvalidNoExeOrIni") ]
    public void From_MapsHasExeAndHasIniToCorrectValidity(bool hasExe, bool hasIni, string expectedName)
    {
        InstallationValidity result = InstallationValidity.From(hasExe, hasIni);

        result.Name.ShouldBe(expectedName);
    }

    [ Theory ]
    [ InlineData(true, true) ]
    [ InlineData(false, true) ]
    [ InlineData(true, false) ]
    [ InlineData(false, false) ]
    public void HasExeAndHasIni_RoundTripThroughFrom(bool hasExe, bool hasIni)
    {
        InstallationValidity result = InstallationValidity.From(hasExe, hasIni);

        result.HasExe.ShouldBe(hasExe);
        result.HasIni.ShouldBe(hasIni);
    }

    [ Fact ]
    public void Valid_DisplayNameIsValid()
    {
        InstallationValidity.Valid.DisplayName.ShouldBe(expected: "Valid");
        InstallationValidity.Valid.ColourToken.ShouldBe(expected: "Green");
    }

    [ Fact ]
    public void InvalidVariants_AllUseRedColourToken()
    {
        InstallationValidity.InvalidNoExe.ColourToken.ShouldBe(expected: "Red");
        InstallationValidity.InvalidNoIni.ColourToken.ShouldBe(expected: "Red");
        InstallationValidity.InvalidNoExeOrIni.ColourToken.ShouldBe(expected: "Red");
    }
}
