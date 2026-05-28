namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class UpdateInstallationHandlerTests
{
    [ Fact ]
    public async Task Handle_RenamesRow_AndStampsModifiedUtc()
    {
        Guid     id      = Guid.CreateVersion7();
        DateTime fakeNow = new(year: 2026, month: 5, day: 27, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Original",
            Path     = @"C:\Games\Main",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        UpdateInstallationHandler handler = new(installations, settings, new FakeTimeProvider(fakeNow));

        ErrorOr<Success> result = await handler.Handle(new UpdateInstallationCommand(id, Name: "Renamed", MakeDefault: false), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        row.Name.ShouldBe(expected: "Renamed");
        row.ModifiedUtc.ShouldBe(fakeNow);

        await settings.DidNotReceive()
                      .UpdateAsync(Arg.Any<LauncherSettings>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_PromotesToDefault_WhenMakeDefaultTrue()
    {
        Guid id = Guid.CreateVersion7();

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Main",
            Path     = @"C:\Games\Main",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        LauncherSettings stored = new()
        {
            DefaultInstallationId = null
        };

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(stored);

        UpdateInstallationHandler handler = new(installations, settings, TimeProvider.System);

        await handler.Handle(new UpdateInstallationCommand(id, Name: "Main", MakeDefault: true), CancellationToken.None);

        stored.DefaultInstallationId.ShouldBe(id);

        await settings.Received(requiredNumberOfCalls: 1)
                      .UpdateAsync(stored, Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_ReturnsNotFound_WhenInstallationMissing()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                     .Returns((GameInstallation?)null);

        UpdateInstallationHandler handler = new(installations, Substitute.For<ILauncherSettingsRepository>(), TimeProvider.System);

        ErrorOr<Success> result = await handler.Handle(new UpdateInstallationCommand(Guid.CreateVersion7(), Name: "Whatever", MakeDefault: false), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }
}
