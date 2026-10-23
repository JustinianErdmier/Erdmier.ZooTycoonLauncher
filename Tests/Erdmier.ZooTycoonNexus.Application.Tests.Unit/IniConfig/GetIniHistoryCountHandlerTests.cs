namespace Erdmier.ZooTycoonNexus.Application.Tests.Unit.IniConfig;

public sealed class GetIniHistoryCountHandlerTests
{
    private readonly GameInstallation _installation = new()
    {
        Id       = Guid.CreateVersion7(),
        Name     = "Main",
        Path     = @"C:\ZT",
        HasExe   = true,
        HasIni   = true,
        AddedUtc = IniTestData.Now
    };

    private readonly IInstallationRepository _installations = Substitute.For<IInstallationRepository>();

    private readonly IIniSnapshotRepository _snapshots = Substitute.For<IIniSnapshotRepository>();

    public GetIniHistoryCountHandlerTests()
        => _installations.GetByIdAsync(_installation.Id, Arg.Any<CancellationToken>())
                         .Returns(_installation);

    [ Fact ]
    public async Task Count_ReturnsTheRepositoryCount()
    {
        _snapshots.CountHistoricalAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(7);

        ErrorOr<int> result = await CreateHandler().Handle(new GetIniHistoryCountQuery(_installation.Id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(7);
    }

    [ Fact ]
    public async Task Count_NeverOpensATransaction()
    {
        ErrorOr<int> result = await CreateHandler().Handle(new GetIniHistoryCountQuery(_installation.Id), CancellationToken.None);

        result.IsError.ShouldBeFalse();

        await _snapshots.DidNotReceive().BeginAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Count_UnknownInstallation_ReturnsNotFound()
    {
        ErrorOr<int> result = await CreateHandler().Handle(new GetIniHistoryCountQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Installation.NotFound");
    }

    [ Fact ]
    public async Task Count_StoreThrows_ReturnsStoreFailed()
    {
        _snapshots.CountHistoricalAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .ThrowsAsync(new InvalidOperationException(message: "no such table"));

        ErrorOr<int> result = await CreateHandler().Handle(new GetIniHistoryCountQuery(_installation.Id), CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.StoreFailed");
        result.FirstError.Description.ShouldContain(expected: "no such table");
    }

    private GetIniHistoryCountHandler CreateHandler() => new(_installations, NullLogger<GetIniHistoryCountHandler>.Instance, _snapshots);
}
