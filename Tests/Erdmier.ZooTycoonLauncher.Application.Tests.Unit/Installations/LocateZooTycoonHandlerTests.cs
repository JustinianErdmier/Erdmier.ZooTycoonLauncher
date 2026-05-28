namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class LocateZooTycoonHandlerTests
{
    [ Fact ]
    public async Task Handle_ReturnsLocatorResult()
    {
        IInstallationLocator locator = Substitute.For<IInstallationLocator>();

        locator.LocateAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
               .Returns(new LocatedDirectory(Path: @"C:\Games\Found", new[] { new LocationProbeAttempt(Source: "S", CandidatePath: @"C:\Games\Found", Failure: null) }));

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings());

        LocateZooTycoonHandler handler = new(locator, settings);

        ErrorOr<LocatedDirectory> result = await handler.Handle(new LocateZooTycoonQuery(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Found.ShouldBeTrue();
        result.Value.Path.ShouldBe(expected: @"C:\Games\Found");
    }
}
