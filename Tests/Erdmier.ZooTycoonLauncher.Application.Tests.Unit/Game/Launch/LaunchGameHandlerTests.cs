using Microsoft.Extensions.Logging.Abstractions;

namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Game.Launch;

public sealed class LaunchGameHandlerTests
{
    [ Fact ]
    public async Task Handle_NoDrift_LaunchesProcessAndStampsLastPlayedUtc()
    {
        Guid id = Guid.CreateVersion7();
        DateTimeOffset now = new(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero);
        FakeTimeProvider clock = new(now);

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Main",
            Path     = @"C:\Games\Zoo",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow,
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IProcessLauncher launcher = Substitute.For<IProcessLauncher>();
        launcher.LaunchAsync(@"C:\Games\Zoo\zoo.exe", @"C:\Games\Zoo", Arg.Any<CancellationToken>())
                .Returns(new ProcessLaunchResult(Started: true, ErrorMessage: null));

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { CloseAfterGameLaunch = true });

        LaunchGameHandler handler = new(clock, installations, NullLogger<LaunchGameHandler>.Instance, launcher, settings, verifier);

        ErrorOr<LaunchGameResult> result = await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(LaunchGameOutcome.Started);
        result.Value.CloseAfterGameLaunch.ShouldBeTrue();
        result.Value.FailureMessage.ShouldBeNull();
        row.LastPlayedUtc.ShouldBe(now.UtcDateTime);
        await installations.Received(1).UpdateAsync(row, Arg.Any<CancellationToken>());
    }
}
