namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class AddInstallationHandlerTests
{
    [Fact]
    public async Task Handle_PersistsRow_AndCreatesPerInstallationDb()
    {
        AddInstallationCommand command = new(Name: "  Main  ", Path: @"C:\Games\Main", MakeDefault: false);
        FakeTimeProvider clock = new(new DateTime(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc));

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<GameInstallation>());

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
        settings.GetAsync(Arg.Any<CancellationToken>()).Returns(new LauncherSettings());

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();
        dbFactory.CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns(Substitute.For<IInstallationDbContextHandle>());

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();
        snapshots.CaptureOriginalAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        AddInstallationHandler handler = new(installations, settings, verifier, dbFactory, snapshots, clock);

        ErrorOr<AddInstallationResult> result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.BecameDefault.ShouldBeTrue();             // First installation auto-promotes
        result.Value.Validity.ShouldBe(InstallationValidity.Valid);

        await installations.Received(1).AddAsync(Arg.Is<GameInstallation>(i => i.Name == "Main" && i.Path == command.Path && i.HasExe && i.HasIni), Arg.Any<CancellationToken>());
        await dbFactory.Received(1).CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await snapshots.Received(1).CaptureOriginalAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsValidationError_WhenDirectoryMissing()
    {
        AddInstallationCommand command = new(Name: "Main", Path: @"C:\Missing", MakeDefault: false);

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: false, HasExe: false, HasIni: false));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();
        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        AddInstallationHandler handler = new(installations, settings, verifier, dbFactory, snapshots, TimeProvider.System);

        ErrorOr<AddInstallationResult> result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Installation.PathMissing");
        await installations.DidNotReceive().AddAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotPromoteToDefault_WhenAlreadyHaveInstallations_AndMakeDefaultFalse()
    {
        AddInstallationCommand command = new(Name: "Second", Path: @"C:\Games\Second", MakeDefault: false);

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns(new[] { new GameInstallation { Id = Guid.CreateVersion7(), Name = "First", Path = @"C:\Games\First", AddedUtc = DateTime.UtcNow } });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
        settings.GetAsync(Arg.Any<CancellationToken>()).Returns(new LauncherSettings());

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(command.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();
        dbFactory.CreateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Substitute.For<IInstallationDbContextHandle>());
        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();
        snapshots.CaptureOriginalAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>()).Returns(Result.Success);

        AddInstallationHandler handler = new(installations, settings, verifier, dbFactory, snapshots, TimeProvider.System);

        ErrorOr<AddInstallationResult> result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.BecameDefault.ShouldBeFalse();
        await settings.DidNotReceive().UpdateAsync(Arg.Any<LauncherSettings>(), Arg.Any<CancellationToken>());
    }
}
