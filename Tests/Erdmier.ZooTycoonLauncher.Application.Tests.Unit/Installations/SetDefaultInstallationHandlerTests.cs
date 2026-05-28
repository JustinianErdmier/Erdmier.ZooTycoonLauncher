namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class SetDefaultInstallationHandlerTests
{
    [ Fact ]
    public async Task Handle_PromotesRowToDefault()
    {
        Guid id = Guid.CreateVersion7();

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Promoted",
            Path     = @"C:\Games\Promoted",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        LauncherSettings settings = new()
        {
            DefaultInstallationId = Guid.CreateVersion7()
        };

        ILauncherSettingsRepository settingsRepo = Substitute.For<ILauncherSettingsRepository>();

        settingsRepo.GetAsync(Arg.Any<CancellationToken>())
                    .Returns(settings);

        SetDefaultInstallationHandler handler = new(installations, settingsRepo);

        ErrorOr<Success> result = await handler.Handle(new SetDefaultInstallationCommand(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        settings.DefaultInstallationId.ShouldBe(id);

        await settingsRepo.Received(requiredNumberOfCalls: 1)
                          .UpdateAsync(settings, Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_NoOp_WhenAlreadyDefault()
    {
        Guid id = Guid.CreateVersion7();

        GameInstallation row = new()
        {
            Id       = id,
            Name     = "Already",
            Path     = @"C:\Games\Already",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        LauncherSettings settings = new()
        {
            DefaultInstallationId = id
        };

        ILauncherSettingsRepository settingsRepo = Substitute.For<ILauncherSettingsRepository>();

        settingsRepo.GetAsync(Arg.Any<CancellationToken>())
                    .Returns(settings);

        SetDefaultInstallationHandler handler = new(installations, settingsRepo);

        await handler.Handle(new SetDefaultInstallationCommand(id), CancellationToken.None);

        await settingsRepo.DidNotReceive()
                          .UpdateAsync(Arg.Any<LauncherSettings>(), Arg.Any<CancellationToken>());
    }
}
