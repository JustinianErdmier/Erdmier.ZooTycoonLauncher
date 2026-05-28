namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class GetInstallationByIdHandlerTests
{
    [ Fact ]
    public async Task Handle_ReturnsNotFound_WhenInstallationMissing()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((GameInstallation?)null);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        GetInstallationByIdHandler handler = new(installations, settings);

        ErrorOr<InstallationSummary> result = await handler.Handle(new GetInstallationByIdQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [ Fact ]
    public async Task Handle_ReturnsSummary_WhenFound()
    {
        Guid id = Guid.CreateVersion7();
        GameInstallation row = new() { Id = id, Name = "Main", Path = @"C:\Games\Main", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(row);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();
        settings.GetAsync(Arg.Any<CancellationToken>()).Returns(new LauncherSettings { DefaultInstallationId = id });

        GetInstallationByIdHandler handler = new(installations, settings);

        ErrorOr<InstallationSummary> result = await handler.Handle(new GetInstallationByIdQuery(id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.IsDefault.ShouldBeTrue();
    }
}
