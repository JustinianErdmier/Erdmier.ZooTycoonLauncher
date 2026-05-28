namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class DeleteInstallationHandlerTests
{
    [Fact]
    public async Task Handle_RemovesRow_AndDeletesPerInstallationDb()
    {
        Guid id = Guid.CreateVersion7();
        GameInstallation row = new() { Id = id, Name = "Doomed", Path = @"C:\Games\Doomed", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
        settings.GetAsync(Arg.Any<CancellationToken>()).Returns(new LauncherSettings { DefaultInstallationId = Guid.CreateVersion7() });

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();

        DeleteInstallationHandler handler = new(installations, settings, dbFactory);

        ErrorOr<DeleteInstallationResult> result = await handler.Handle(new DeleteInstallationCommand(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.RemovedWasDefault.ShouldBeFalse();
        await installations.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
        await dbFactory.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PromotesAlphabeticallyFirstRow_WhenDefaultRemoved()
    {
        Guid removedId = Guid.CreateVersion7();
        Guid promotedId = Guid.CreateVersion7();

        GameInstallation row = new() { Id = removedId, Name = "Removed", Path = @"C:\Games\Removed", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };
        GameInstallation promotion = new() { Id = promotedId, Name = "Promoted", Path = @"C:\Games\Promoted", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(removedId, Arg.Any<CancellationToken>()).Returns(row);
        installations.FindDefaultPromotionCandidateAsync(Arg.Any<CancellationToken>()).Returns(promotion);

        LauncherSettings settings = new() { DefaultInstallationId = removedId };
        ILauncherSettingsRepository settingsRepo = Substitute.For<ILauncherSettingsRepository>();
        settingsRepo.GetAsync(Arg.Any<CancellationToken>()).Returns(settings);

        IInstallationDbContextFactory dbFactory = Substitute.For<IInstallationDbContextFactory>();

        DeleteInstallationHandler handler = new(installations, settingsRepo, dbFactory);

        ErrorOr<DeleteInstallationResult> result = await handler.Handle(new DeleteInstallationCommand(removedId), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.RemovedWasDefault.ShouldBeTrue();
        result.Value.NewDefaultInstallationId.ShouldBe(promotedId);
        settings.DefaultInstallationId.ShouldBe(promotedId);
        await settingsRepo.Received(1).UpdateAsync(settings, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetsDefaultToNull_WhenNoInstallationsRemain()
    {
        Guid removedId = Guid.CreateVersion7();
        GameInstallation row = new() { Id = removedId, Name = "Last", Path = @"C:\Games\Last", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(removedId, Arg.Any<CancellationToken>()).Returns(row);
        installations.FindDefaultPromotionCandidateAsync(Arg.Any<CancellationToken>()).Returns((GameInstallation?)null);

        LauncherSettings settings = new() { DefaultInstallationId = removedId };
        ILauncherSettingsRepository settingsRepo = Substitute.For<ILauncherSettingsRepository>();
        settingsRepo.GetAsync(Arg.Any<CancellationToken>()).Returns(settings);

        DeleteInstallationHandler handler = new(installations, settingsRepo, Substitute.For<IInstallationDbContextFactory>());

        ErrorOr<DeleteInstallationResult> result = await handler.Handle(new DeleteInstallationCommand(removedId), CancellationToken.None);

        result.Value.RemovedWasDefault.ShouldBeTrue();
        result.Value.NewDefaultInstallationId.ShouldBeNull();
        settings.DefaultInstallationId.ShouldBeNull();
    }
}
