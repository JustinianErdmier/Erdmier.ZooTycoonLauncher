namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Discovery;

public sealed class InstallationVerifierTests
{
    [ Fact ]
    public async Task VerifyAsync_ReportsAllPresent_WhenExeAndIniExist()
    {
        MockFileSystem fs = new();
        fs.AddDirectory(@"C:\Games\Zoo");
        fs.AddFile(@"C:\Games\Zoo\zoo.exe", new MockFileData([ 0x4D, 0x5A ]));
        fs.AddFile(@"C:\Games\Zoo\zoo.ini", new MockFileData("[ user ]\n"));

        InstallationVerifier verifier = new(fs);

        VerificationResult result = await verifier.VerifyAsync(@"C:\Games\Zoo", CancellationToken.None);

        result.DirectoryExists.ShouldBeTrue();
        result.HasExe.ShouldBeTrue();
        result.HasIni.ShouldBeTrue();
        result.Validity.ShouldBe(InstallationValidity.Valid);
    }

    [ Fact ]
    public async Task VerifyAsync_ReportsMissingDirectory()
    {
        MockFileSystem fs = new();
        InstallationVerifier verifier = new(fs);

        VerificationResult result = await verifier.VerifyAsync(@"C:\Missing", CancellationToken.None);

        result.DirectoryExists.ShouldBeFalse();
        result.HasExe.ShouldBeFalse();
        result.HasIni.ShouldBeFalse();
        result.Validity.ShouldBe(InstallationValidity.InvalidNoExeOrIni);
    }

    [ Fact ]
    public async Task VerifyAsync_DetectsMissingIni()
    {
        MockFileSystem fs = new();
        fs.AddDirectory(@"C:\Games\Zoo");
        fs.AddFile(@"C:\Games\Zoo\zoo.exe", new MockFileData([ 0x4D, 0x5A ]));

        InstallationVerifier verifier = new(fs);

        VerificationResult result = await verifier.VerifyAsync(@"C:\Games\Zoo", CancellationToken.None);

        result.HasExe.ShouldBeTrue();
        result.HasIni.ShouldBeFalse();
        result.Validity.ShouldBe(InstallationValidity.InvalidNoIni);
    }
}
