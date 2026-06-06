namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Game;

public sealed class WindowsProcessLauncherTests
{
    [ Fact ]
    public async Task LaunchAsync_KnownGoodExe_ReturnsStarted()
    {
        WindowsProcessLauncher launcher = new();
        string                 cmdPath  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), path2: "cmd.exe");

        ProcessLaunchResult result = await launcher.LaunchAsync(cmdPath, Environment.SystemDirectory, CancellationToken.None);

        result.Started.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [ Fact ]
    public async Task LaunchAsync_NonExistentPath_ReturnsStartFailedWithMessage()
    {
        WindowsProcessLauncher launcher = new();

        string missingPath = @"C:\definitely-not-real-"
                             + Guid.NewGuid()
                                   .ToString(format: "N")
                             + ".exe";

        ProcessLaunchResult result = await launcher.LaunchAsync(missingPath, workingDirectory: @"C:\", CancellationToken.None);

        result.Started.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
    }
}
