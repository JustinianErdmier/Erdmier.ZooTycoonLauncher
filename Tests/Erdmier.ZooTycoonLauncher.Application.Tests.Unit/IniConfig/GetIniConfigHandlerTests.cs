namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class GetIniConfigHandlerTests
{
    [ Fact ]
    public async Task Handle_UnknownInstallation_ReturnsNotFound()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        GetIniConfigHandler handler = new(installations, Substitute.For<IIniSnapshotService>());

        ErrorOr<IniConfigResult> result = await handler.Handle(new GetIniConfigQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Installation.NotFound");
    }

    [ Fact ]
    public async Task Handle_DelegatesToLoad()
    {
        GameInstallation row = new()
        {
            Id       = Guid.CreateVersion7(),
            Name     = "Main",
            Path     = @"C:\ZT",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = IniTestData.Now
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(row.Id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IniConfigResult     expected  = new(new Dictionary<IniKeyId, string?>(), IniTestData.Now);
        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.LoadAsync(row, Arg.Any<CancellationToken>())
                 .Returns(expected);

        GetIniConfigHandler handler = new(installations, snapshots);

        ErrorOr<IniConfigResult> result = await handler.Handle(new GetIniConfigQuery(row.Id), CancellationToken.None);

        result.Value.ShouldBeSameAs(expected);
    }
}
