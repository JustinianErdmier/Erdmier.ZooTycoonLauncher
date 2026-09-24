namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class AddInstallationHandlerTests
{
    [ Fact ]
    public async Task Handle_PersistsRow_AndCreatesPerInstallationDb()
    {
        AddInstallationCommand command = new(Name: "  Main  ", Path: @"C:\Games\Main", MakeDefault: false);
        FakeTimeProvider       clock   = new(new DateTime(year: 2026, month: 5, day: 27, hour: 12, minute: 0, second: 0, DateTimeKind.Utc));

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns(Array.Empty<GameInstallation>());

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings());

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();

        dbFactory.CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns(Substitute.For<IInstallationDbContextHandle>());

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.CaptureOriginalAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        AddInstallationHandler handler = new(installations, settings, verifier, dbFactory, snapshots, clock, Substitute.For<IApplicationEventPublisher>());

        ErrorOr<AddInstallationResult> result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.BecameDefault.ShouldBeTrue(); // First installation auto-promotes
        result.Value.Validity.ShouldBe(InstallationValidity.Valid);

        await installations.Received(requiredNumberOfCalls: 1)
                           .AddAsync(Arg.Is<GameInstallation>(i => i.Name == "Main" && i.Path == command.Path && i.HasExe && i.HasIni), Arg.Any<CancellationToken>());

        await dbFactory.Received(requiredNumberOfCalls: 1)
                       .CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        await snapshots.Received(requiredNumberOfCalls: 1)
                       .CaptureOriginalAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_ReturnsValidationError_WhenDirectoryMissing()
    {
        AddInstallationCommand command = new(Name: "Main", Path: @"C:\Missing", MakeDefault: false);

        IInstallationRepository     installations = Substitute.For<IInstallationRepository>();
        ILauncherSettingsRepository settings      = Substitute.For<ILauncherSettingsRepository>();

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: false, HasExe: false, HasIni: false));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();
        IIniSnapshotService           snapshots = Substitute.For<IIniSnapshotService>();
        IApplicationEventPublisher    events    = Substitute.For<IApplicationEventPublisher>();

        AddInstallationHandler handler = new(installations, settings, verifier, dbFactory, snapshots, TimeProvider.System, events);

        ErrorOr<AddInstallationResult> result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Installation.PathMissing");

        await installations.DidNotReceive()
                           .AddAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>());

        events.ReceivedCalls()
              .ShouldBeEmpty();
    }

    [ Fact ]
    public async Task Handle_DoesNotPromoteToDefault_WhenAlreadyHaveInstallations_AndMakeDefaultFalse()
    {
        AddInstallationCommand command = new(Name: "Second", Path: @"C:\Games\Second", MakeDefault: false);

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns(new[]
                     {
                         new GameInstallation
                         {
                             Id       = Guid.CreateVersion7(),
                             Name     = "First",
                             Path     = @"C:\Games\First",
                             AddedUtc = DateTime.UtcNow
                         }
                     });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings());

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();

        dbFactory.CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns(Substitute.For<IInstallationDbContextHandle>());

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.CaptureOriginalAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        AddInstallationHandler handler = new(installations, settings, verifier, dbFactory, snapshots, TimeProvider.System, Substitute.For<IApplicationEventPublisher>());

        ErrorOr<AddInstallationResult> result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.BecameDefault.ShouldBeFalse();

        await settings.DidNotReceive()
                      .UpdateAsync(Arg.Any<LauncherSettings>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_PublishesAddedAndDefaultChanged_WhenFirstInstallation()
    {
        AddInstallationCommand command = new(Name: "Main", Path: @"C:\Games\Main", MakeDefault: false);

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns(Array.Empty<GameInstallation>());

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings());

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();

        dbFactory.CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns(Substitute.For<IInstallationDbContextHandle>());

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.CaptureOriginalAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        AddInstallationHandler handler = new(installations, settings, verifier, dbFactory, snapshots, TimeProvider.System, events);

        ErrorOr<AddInstallationResult> result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new InstallationAddedMessage(result.Value.InstallationId));

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new DefaultInstallationChangedMessage(result.Value.InstallationId));
    }

    [ Fact ]
    public async Task Handle_PublishesOnlyAdded_WhenNotDefault()
    {
        AddInstallationCommand command = new(Name: "Second", Path: @"C:\Games\Second", MakeDefault: false);

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns([
                         new GameInstallation
                         {
                             Id       = Guid.CreateVersion7(),
                             Name     = "Main",
                             Path     = @"C:\Games\Main",
                             AddedUtc = DateTime.UtcNow
                         }
                     ]);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();

        dbFactory.CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns(Substitute.For<IInstallationDbContextHandle>());

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.CaptureOriginalAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        AddInstallationHandler handler = new(installations,
                                             Substitute.For<ILauncherSettingsRepository>(),
                                             verifier,
                                             dbFactory,
                                             snapshots,
                                             TimeProvider.System,
                                             events);

        ErrorOr<AddInstallationResult> result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeFalse();

        events.Received(requiredNumberOfCalls: 1)
              .Publish(new InstallationAddedMessage(result.Value.InstallationId));

        events.DidNotReceive()
              .Publish(Arg.Any<DefaultInstallationChangedMessage>());

        events.ReceivedCalls()
              .Count()
              .ShouldBe(expected: 1);
    }

    [ Fact ]
    public async Task Handle_StillPublishesAdded_WhenDatabaseProvisioningThrows()
    {
        AddInstallationCommand command = new(Name: "Main", Path: @"C:\Games\Main", MakeDefault: false);

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns(Array.Empty<GameInstallation>());

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings());

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();

        dbFactory.CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Throws(new IOException(message: "Disk full"));

        IApplicationEventPublisher events = Substitute.For<IApplicationEventPublisher>();

        AddInstallationHandler handler = new(installations,
                                             settings,
                                             verifier,
                                             dbFactory,
                                             Substitute.For<IIniSnapshotService>(),
                                             TimeProvider.System,
                                             events);

        await Should.ThrowAsync<IOException>(async () => await handler.Handle(command, CancellationToken.None));

        events.Received(requiredNumberOfCalls: 1)
              .Publish(Arg.Any<InstallationAddedMessage>());

        // Zero existing installations, so this one became the default — publishes even though provisioning threw.
        events.Received(requiredNumberOfCalls: 1)
              .Publish(Arg.Any<DefaultInstallationChangedMessage>());
    }
}
