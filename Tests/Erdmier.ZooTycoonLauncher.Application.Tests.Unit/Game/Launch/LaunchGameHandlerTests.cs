using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute.ExceptionExtensions;

namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Game.Launch;

public sealed class LaunchGameHandlerTests
{
    [ Fact ]
    public async Task Handle_NoDrift_LaunchesProcessAndStampsLastPlayedUtc()
    {
        Guid             id    = Guid.CreateVersion7();
        DateTimeOffset   now   = new(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero);
        FakeTimeProvider clock = new(now);

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Main",
            Path     = @"C:\Games\Zoo",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IProcessLauncher launcher = Substitute.For<IProcessLauncher>();

        launcher.LaunchAsync(exePath: @"C:\Games\Zoo\zoo.exe", workingDirectory: @"C:\Games\Zoo", Arg.Any<CancellationToken>())
                .Returns(new ProcessLaunchResult(Started: true, ErrorMessage: null));

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    CloseAfterGameLaunch = true
                });

        LaunchGameHandler handler = new(clock, installations, NullLogger<LaunchGameHandler>.Instance, launcher, settings, verifier);

        ErrorOr<LaunchGameResult> result = await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(LaunchGameOutcome.Started);
        result.Value.CloseAfterGameLaunch.ShouldBeTrue();
        result.Value.FailureMessage.ShouldBeNull();
        row.LastPlayedUtc.ShouldBe(now.UtcDateTime);

        await installations.Received(requiredNumberOfCalls: 1)
                           .UpdateAsync(row, Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_DriftDetected_PersistsAndReturnsDrifted()
    {
        Guid             id    = Guid.CreateVersion7();
        FakeTimeProvider clock = new(new DateTimeOffset(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero));

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Main",
            Path     = @"C:\Games\Zoo",
            HasExe   = true, // row claims valid
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: false, HasIni: true)); // drift

        IProcessLauncher launcher = Substitute.For<IProcessLauncher>();

        LaunchGameHandler handler = new(clock,
                                        installations,
                                        NullLogger<LaunchGameHandler>.Instance,
                                        launcher,
                                        Substitute.For<ILauncherSettingsRepository>(),
                                        verifier);

        ErrorOr<LaunchGameResult> result = await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(LaunchGameOutcome.Drifted);
        result.Value.CloseAfterGameLaunch.ShouldBeFalse();
        result.Value.FailureMessage.ShouldBeNull();

        row.HasExe.ShouldBeFalse(); // drift persisted on the row

        await installations.Received(requiredNumberOfCalls: 1)
                           .UpdateAsync(row, Arg.Any<CancellationToken>());

        await launcher.DidNotReceive()
                      .LaunchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_ProcessStartFails_ReturnsStartFailedAndDoesNotStampLastPlayed()
    {
        Guid             id     = Guid.CreateVersion7();
        DateTime         before = new(year: 2026, month: 1, day: 1, hour: 0, minute: 0, second: 0, DateTimeKind.Utc);
        FakeTimeProvider clock  = new(new DateTimeOffset(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero));

        GameInstallation row = new()
        {
            Id            = id,
            Name          = "Main",
            Path          = @"C:\Games\Zoo",
            HasExe        = true,
            HasIni        = true,
            AddedUtc      = DateTime.UtcNow,
            LastPlayedUtc = before
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IProcessLauncher launcher = Substitute.For<IProcessLauncher>();

        launcher.LaunchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ProcessLaunchResult(Started: false, ErrorMessage: "Antivirus blocked execution."));

        LaunchGameHandler handler = new(clock,
                                        installations,
                                        NullLogger<LaunchGameHandler>.Instance,
                                        launcher,
                                        Substitute.For<ILauncherSettingsRepository>(),
                                        verifier);

        ErrorOr<LaunchGameResult> result = await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(LaunchGameOutcome.StartFailed);
        result.Value.FailureMessage.ShouldBe(expected: "Antivirus blocked execution.");
        result.Value.CloseAfterGameLaunch.ShouldBeFalse();
        row.LastPlayedUtc.ShouldBe(before); // unchanged
    }

    [ Fact ]
    public async Task Handle_InstallationNotFound_ReturnsError()
    {
        Guid             id    = Guid.CreateVersion7();
        FakeTimeProvider clock = new(new DateTimeOffset(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero));

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns((GameInstallation?)null);

        LaunchGameHandler handler = new(clock,
                                        installations,
                                        NullLogger<LaunchGameHandler>.Instance,
                                        Substitute.For<IProcessLauncher>(),
                                        Substitute.For<ILauncherSettingsRepository>(),
                                        Substitute.For<IInstallationVerifier>());

        ErrorOr<LaunchGameResult> result = await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [ Fact ]
    public async Task Handle_VerifierThrows_PropagatesException()
    {
        Guid             id    = Guid.CreateVersion7();
        FakeTimeProvider clock = new(new DateTimeOffset(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero));

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Main",
            Path     = @"C:\Games\Zoo",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Throws(new UnauthorizedAccessException(message: "Access denied"));

        LaunchGameHandler handler = new(clock,
                                        installations,
                                        NullLogger<LaunchGameHandler>.Instance,
                                        Substitute.For<IProcessLauncher>(),
                                        Substitute.For<ILauncherSettingsRepository>(),
                                        verifier);

        UnauthorizedAccessException ex = await Should.ThrowAsync<UnauthorizedAccessException>(async () => await handler.Handle(new LaunchGameCommand(id), CancellationToken.None));

        ex.Message.ShouldBe(expected: "Access denied");
    }

    [ Fact ]
    public async Task Handle_LastPlayedUpdateThrows_StillReturnsStarted()
    {
        Guid             id    = Guid.CreateVersion7();
        DateTimeOffset   now   = new(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero);
        FakeTimeProvider clock = new(now);

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Main",
            Path     = @"C:\Games\Zoo",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        installations.When(r => r.UpdateAsync(row, Arg.Any<CancellationToken>()))
                     .Do(_ => throw new InvalidOperationException(message: "DB locked"));

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IProcessLauncher launcher = Substitute.For<IProcessLauncher>();

        launcher.LaunchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ProcessLaunchResult(Started: true, ErrorMessage: null));

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    CloseAfterGameLaunch = false
                });

        LaunchGameHandler handler = new(clock,
                                        installations,
                                        NullLogger<LaunchGameHandler>.Instance,
                                        launcher,
                                        settings,
                                        verifier);

        ErrorOr<LaunchGameResult> result = await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(LaunchGameOutcome.Started);
    }

    [ Fact ]
    public async Task Handle_SettingsReadAfterLaunch()
    {
        Guid             id    = Guid.CreateVersion7();
        FakeTimeProvider clock = new(new DateTimeOffset(year: 2026, month: 6, day: 3, hour: 12, minute: 0, second: 0, TimeSpan.Zero));

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Main",
            Path     = @"C:\Games\Zoo",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IProcessLauncher launcher = Substitute.For<IProcessLauncher>();

        launcher.LaunchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ProcessLaunchResult(Started: true, ErrorMessage: null));

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    CloseAfterGameLaunch = true
                });

        LaunchGameHandler handler = new(clock,
                                        installations,
                                        NullLogger<LaunchGameHandler>.Instance,
                                        launcher,
                                        settings,
                                        verifier);

        await handler.Handle(new LaunchGameCommand(id), CancellationToken.None);

        Received.InOrder(() =>
        {
            launcher.LaunchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            settings.GetAsync(Arg.Any<CancellationToken>());
        });
    }
}
