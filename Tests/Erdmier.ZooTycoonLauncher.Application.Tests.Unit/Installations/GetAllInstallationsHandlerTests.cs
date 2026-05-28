namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class GetAllInstallationsHandlerTests
{
    [ Fact ]
    public async Task Handle_ReturnsSummariesWithDefaultFlag()
    {
        Guid defaultId = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns(new List<GameInstallation>
                     {
                         new()
                         {
                             Id       = defaultId,
                             Name     = "Main",
                             Path     = @"C:\Games\Main",
                             HasExe   = true,
                             HasIni   = true,
                             AddedUtc = DateTime.UtcNow
                         },
                         new()
                         {
                             Id       = Guid.CreateVersion7(),
                             Name     = "Other",
                             Path     = @"C:\Games\Other",
                             HasExe   = true,
                             HasIni   = false,
                             AddedUtc = DateTime.UtcNow
                         }
                     });

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    DefaultInstallationId = defaultId
                });

        GetAllInstallationsHandler handler = new(installations, settings);

        ErrorOr<IReadOnlyList<InstallationSummary>> result = await handler.Handle(new GetAllInstallationsQuery(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Count.ShouldBe(expected: 2);

        result.Value.Single(s => s.Name == "Main")
              .IsDefault.ShouldBeTrue();

        result.Value.Single(s => s.Name == "Other")
              .IsDefault.ShouldBeFalse();

        result.Value.Single(s => s.Name == "Other")
              .Validity.ShouldBe(InstallationValidity.InvalidNoIni);
    }
}
