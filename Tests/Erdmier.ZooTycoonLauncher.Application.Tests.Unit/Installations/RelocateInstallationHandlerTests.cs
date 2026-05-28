namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class RelocateInstallationHandlerTests
{
    [Fact]
    public async Task Handle_PointsRowAtNewPath_AndRecomputesFlags()
    {
        Guid id = Guid.CreateVersion7();
        GameInstallation row = new() { Id = id, Name = "Main", Path = @"C:\Games\Old", HasExe = false, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(@"C:\Games\New", Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        DateTime fakeNow = new(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc);

        RelocateInstallationHandler handler = new(installations, verifier, new FakeTimeProvider(fakeNow));

        ErrorOr<RelocateInstallationResult> result = await handler.Handle(new RelocateInstallationCommand(id, @"C:\Games\New"), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.NewValidity.ShouldBe(InstallationValidity.Valid);
        await installations.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
        await installations.Received(1).AddAsync(Arg.Is<GameInstallation>(i => i.Id == id && i.Path == @"C:\Games\New" && i.ModifiedUtc == fakeNow && i.HasExe && i.HasIni), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenInstallationMissing()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((GameInstallation?)null);

        RelocateInstallationHandler handler = new(installations, Substitute.For<IInstallationVerifier>(), TimeProvider.System);

        ErrorOr<RelocateInstallationResult> result = await handler.Handle(new RelocateInstallationCommand(Guid.CreateVersion7(), @"C:\Games\New"), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ReturnsValidationError_WhenNewDirectoryMissing()
    {
        Guid id = Guid.CreateVersion7();
        GameInstallation row = new() { Id = id, Name = "Main", Path = @"C:\Games\Old", AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();
        verifier.VerifyAsync(@"C:\Missing", Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: false, HasExe: false, HasIni: false));

        RelocateInstallationHandler handler = new(installations, verifier, TimeProvider.System);

        ErrorOr<RelocateInstallationResult> result = await handler.Handle(new RelocateInstallationCommand(id, @"C:\Missing"), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Installation.PathMissing");
        await installations.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
