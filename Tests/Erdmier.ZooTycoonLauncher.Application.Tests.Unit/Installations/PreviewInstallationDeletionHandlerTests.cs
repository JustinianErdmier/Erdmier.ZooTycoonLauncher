namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class PreviewInstallationDeletionHandlerTests
{
    [ Fact ]
    public async Task Handle_ReturnsPromotedName_WhenDefaultWithSuccessor()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Main",
                         Path     = @"C:\Games\Main",
                         AddedUtc = DateTime.UtcNow
                     });

        installations.FindDefaultPromotionCandidateAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = Guid.CreateVersion7(),
                         Name     = "Archive",
                         Path     = @"C:\Games\Archive",
                         AddedUtc = DateTime.UtcNow
                     });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = id
                });

        PreviewInstallationDeletionHandler handler = new(installations, settings);

        ErrorOr<InstallationDeletionPreview> result = await handler.Handle(new PreviewInstallationDeletionQuery(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(new InstallationDeletionPreview(Name: "Main", IsDefault: true, PromotedName: "Archive"));
    }

    [ Fact ]
    public async Task Handle_ReturnsNoPromotion_WhenLastInstallation()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Main",
                         Path     = @"C:\Games\Main",
                         AddedUtc = DateTime.UtcNow
                     });

        installations.FindDefaultPromotionCandidateAsync(id, Arg.Any<CancellationToken>())
                     .Returns((GameInstallation?)null);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = id
                });

        PreviewInstallationDeletionHandler handler = new(installations, settings);

        ErrorOr<InstallationDeletionPreview> result = await handler.Handle(new PreviewInstallationDeletionQuery(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(new InstallationDeletionPreview(Name: "Main", IsDefault: true, PromotedName: null));
    }

    [ Fact ]
    public async Task Handle_ReturnsNotDefault_WithoutQueryingPromotion_WhenNotDefault()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(new GameInstallation
                     {
                         Id       = id,
                         Name     = "Spare",
                         Path     = @"C:\Games\Spare",
                         AddedUtc = DateTime.UtcNow
                     });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = Guid.CreateVersion7()
                });

        PreviewInstallationDeletionHandler handler = new(installations, settings);

        ErrorOr<InstallationDeletionPreview> result = await handler.Handle(new PreviewInstallationDeletionQuery(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(new InstallationDeletionPreview(Name: "Spare", IsDefault: false, PromotedName: null));

        await installations.DidNotReceive()
                           .FindDefaultPromotionCandidateAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_ReturnsNotFound_WhenInstallationMissing()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                     .Returns((GameInstallation?)null);

        PreviewInstallationDeletionHandler handler = new(installations, Substitute.For<ILauncherSettingsRepository>());

        ErrorOr<InstallationDeletionPreview> result = await handler.Handle(new PreviewInstallationDeletionQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }
}
