namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class IniErrorsTests
{
    [ Theory ]
    [ InlineData(InstallationWriteAccess.FolderReadOnly) ]
    [ InlineData(InstallationWriteAccess.FileReadOnly) ]
    public void ReadOnly_ReadOnlyAccess_CarriesTheExplanation(InstallationWriteAccess access)
    {
        Error error = IniErrors.ReadOnly(access);

        error.Code.ShouldBe(expected: "Ini.ReadOnly");
        error.Description.ShouldBe(IniReadOnlyText.For(access));
    }

    [ Theory ]
    [ InlineData(InstallationWriteAccess.Writable) ]
    [ InlineData(InstallationWriteAccess.Unknown) ]
    public void ReadOnly_NonReadOnlyAccess_Throws(InstallationWriteAccess access)
        => Should.Throw<ArgumentOutOfRangeException>(() => IniErrors.ReadOnly(access));
}
