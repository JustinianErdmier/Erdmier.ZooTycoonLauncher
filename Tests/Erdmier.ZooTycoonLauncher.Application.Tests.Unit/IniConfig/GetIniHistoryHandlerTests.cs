namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class GetIniHistoryHandlerTests
{
    private static readonly IniKeyId ScreenWidth = new(Section: "user", Key: "screenwidth");

    private readonly IIniFileStore _files = Substitute.For<IIniFileStore>();

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

    private readonly IIniSnapshotTransaction _transaction = IniTestData.Transaction(IniTestData.Snapshot(IniTestData.Sample));

    public GetIniHistoryHandlerTests()
    {
        _installations.GetByIdAsync(_installation.Id, Arg.Any<CancellationToken>())
                      .Returns(_installation);

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(_ => _transaction);

        DiskHolds(IniTestData.Sample);
    }

    [ Fact ]
    public async Task History_ListsHistoricalNewestFirstThenOriginal()
    {
        IniSnapshot newest   = Snapshot(IniSnapshotKind.Historical, IniSnapshotTrigger.Manual, IniTestData.Sample, IniTestData.Now.AddHours(value: -1));
        IniSnapshot older    = Snapshot(IniSnapshotKind.Historical, IniSnapshotTrigger.LauncherGui, IniTestData.Sample, IniTestData.Now.AddDays(value: -1));
        IniSnapshot original = Snapshot(IniSnapshotKind.Original, IniSnapshotTrigger.OriginalImport, IniTestData.Sample, IniTestData.Now.AddDays(value: -7));

        _transaction.ListHistoricalAsync(Arg.Any<CancellationToken>())
                    .Returns([ newest, older ]);

        _transaction.GetOriginalAsync(Arg.Any<CancellationToken>())
                    .Returns(original);

        ErrorOr<IniHistoryResult> result = await CreateHandler().Handle(new GetIniHistoryQuery(_installation.Id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Entries.Select(entry => entry.SnapshotId).ShouldBe([ newest.Id, older.Id, original.Id ]);
        result.Value.Entries.Select(entry => entry.Kind).ShouldBe([ IniSnapshotKind.Historical, IniSnapshotKind.Historical, IniSnapshotKind.Original ]);
        result.Value.Entries[0].Trigger.ShouldBe(IniSnapshotTrigger.Manual);
        result.Value.Entries.Select(entry => entry.CapturedUtc).ShouldBe([ newest.CapturedUtc, older.CapturedUtc, original.CapturedUtc ]);
        result.Value.Entries[0].CapturedUtc.ShouldBeGreaterThan(result.Value.Entries[1].CapturedUtc);
        result.Value.Entries[1].CapturedUtc.ShouldBeGreaterThan(result.Value.Entries[2].CapturedUtc);
    }

    [ Fact ]
    public async Task History_ComparesEachSnapshotAgainstTheReconciledValues()
    {
        string disk = IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1280");

        DiskHolds(disk);

        IniSnapshot matchesDisk = Snapshot(IniSnapshotKind.Historical, IniSnapshotTrigger.Manual, disk);
        IniSnapshot matchesOld  = Snapshot(IniSnapshotKind.Historical, IniSnapshotTrigger.Manual, IniTestData.Sample);

        _transaction.ListHistoricalAsync(Arg.Any<CancellationToken>())
                    .Returns([ matchesDisk, matchesOld ]);

        ErrorOr<IniHistoryResult> result = await CreateHandler().Handle(new GetIniHistoryQuery(_installation.Id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Entries[0].Comparison.Rows.ShouldNotContain(row => row.Id == ScreenWidth);
        result.Value.Entries[1].Comparison.Rows.ShouldContain(row => row.Id == ScreenWidth && row.Current == "1280" && row.Snapshot == "800");
    }

    [ Fact ]
    public async Task History_ReconcilesBeforeReading()
    {
        ErrorOr<IniHistoryResult> result = await CreateHandler().Handle(new GetIniHistoryQuery(_installation.Id), CancellationToken.None);

        result.IsError.ShouldBeFalse();

        Received.InOrder(() =>
        {
            _transaction.GetCurrentAsync(Arg.Any<CancellationToken>());
            _transaction.ListHistoricalAsync(Arg.Any<CancellationToken>());
            _transaction.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [ Fact ]
    public async Task History_UnknownInstallation_ReturnsNotFound()
    {
        ErrorOr<IniHistoryResult> result = await CreateHandler().Handle(new GetIniHistoryQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Installation.NotFound");
    }

    [ Fact ]
    public async Task History_MissingFile_ReturnsMissing()
    {
        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns((IniFileContent?)null);

        ErrorOr<IniHistoryResult> result = await CreateHandler().Handle(new GetIniHistoryQuery(_installation.Id), CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.Missing");
    }

    [ Fact ]
    public async Task History_ReadThrows_ReturnsReadFailed()
    {
        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .ThrowsAsync(new IOException(message: "sharing violation"));

        ErrorOr<IniHistoryResult> result = await CreateHandler().Handle(new GetIniHistoryQuery(_installation.Id), CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.ReadFailed");
        result.FirstError.Description.ShouldContain(expected: "sharing violation");
    }

    [ Fact ]
    public async Task History_StoreThrows_ReturnsStoreFailed()
    {
        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .ThrowsAsync(new InvalidOperationException(message: "database is locked"));

        ErrorOr<IniHistoryResult> result = await CreateHandler().Handle(new GetIniHistoryQuery(_installation.Id), CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.StoreFailed");
        result.FirstError.Description.ShouldContain(expected: "database is locked");
    }

    [ Fact ]
    public async Task History_StoreFailsAfterReconcile_LogsNoReconciliation()
    {
        RecordingLogger<GetIniHistoryHandler> logger = new();

        DiskHolds(IniTestData.Sample.Replace(oldValue: "tooltipDelay=1", newValue: "tooltipDelay=5"));

        _transaction.CommitAsync(Arg.Any<CancellationToken>())
                    .ThrowsAsync(new InvalidOperationException(message: "database is locked"));

        ErrorOr<IniHistoryResult> result = await CreateHandler(logger).Handle(new GetIniHistoryQuery(_installation.Id), CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.StoreFailed");
        logger.Entries.ShouldNotContain(entry => entry.Message.Contains("reconciled"));
    }

    private static IniSnapshot Snapshot(IniSnapshotKind kind, IniSnapshotTrigger trigger, string text, DateTime? capturedUtc = null)
        => new()
        {
            Id            = Guid.CreateVersion7(),
            Kind          = kind,
            Trigger       = trigger,
            CapturedUtc   = capturedUtc ?? IniTestData.Now.AddDays(value: -1),
            StructureBlob = text,
            Values        = IniTestData.Snapshot(text).Values
        };

    private void DiskHolds(string text)
        => _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
                 .Returns(new IniFileContent(text, IniTestData.Now.AddHours(value: -1)));

    private GetIniHistoryHandler CreateHandler(ILogger<GetIniHistoryHandler>? logger = null)
        => new(new FakeTimeProvider(IniTestData.Now), _files, _installations, logger ?? NullLogger<GetIniHistoryHandler>.Instance, new IniReconciler(), _snapshots);
}
