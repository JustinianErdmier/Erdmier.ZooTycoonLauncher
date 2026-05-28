namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class VerifyInstallationHandlerTests
{
    [ Fact ]
    public async Task Handle_PersistsFlagChange_AndStampsModifiedUtc()
    {
        DateTime         fakeNow = new(year: 2026, month: 5, day: 27, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);
        FakeTimeProvider clock   = new(fakeNow);

        GameInstallation row = new()
        {
            Id       = Guid.CreateVersion7(),
            Name     = "Main",
            Path     = @"C:\Games\Main",
            HasExe   = false,
            HasIni   = false,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(row.Id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        VerifyInstallationHandler handler = new(installations, verifier, clock);

        ErrorOr<VerificationResult> result = await handler.Handle(new VerifyInstallationQuery(row.Id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        row.HasExe.ShouldBeTrue();
        row.HasIni.ShouldBeTrue();
        row.ModifiedUtc.ShouldBe(fakeNow);

        await installations.Received(requiredNumberOfCalls: 1)
                           .UpdateAsync(row, Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_NoPersist_WhenFlagsUnchanged()
    {
        GameInstallation row = new()
        {
            Id       = Guid.CreateVersion7(),
            Name     = "Main",
            Path     = @"C:\Games\Main",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(row.Id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        VerifyInstallationHandler handler = new(installations, verifier, TimeProvider.System);

        await handler.Handle(new VerifyInstallationQuery(row.Id), CancellationToken.None);

        await installations.DidNotReceive()
                           .UpdateAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>());
    }
}

internal sealed class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FakeTimeProvider(DateTime utcNow) => _now = new DateTimeOffset(utcNow, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;
}
