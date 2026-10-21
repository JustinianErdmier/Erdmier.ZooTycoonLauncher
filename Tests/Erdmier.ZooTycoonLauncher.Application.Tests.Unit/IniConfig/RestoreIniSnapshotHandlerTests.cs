namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class RestoreIniSnapshotHandlerTests
{
    private static readonly DateTime DiskLastWrite = IniTestData.Now.AddHours(-1);

    private static readonly DateTime NewLastWrite = IniTestData.Now.AddSeconds(1);

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

    private string? _written;

    public RestoreIniSnapshotHandlerTests()
    {
        _installations.GetByIdAsync(_installation.Id, Arg.Any<CancellationToken>())
                      .Returns(_installation);

        _files.WriteAsync(_installation.Path, Arg.Do<string>(text => _written = text), Arg.Any<CancellationToken>())
              .Returns(NewLastWrite);

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(_ => _transaction);
    }

    [ Fact ]
    public async Task Restore_FollowsTheSdd82Order()
    {
        DiskHolds(IniTestData.Sample);

        Guid snapshotId = SnapshotHolds(IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024"));

        ErrorOr<IniConfigResult> result = await RestoreAsync(snapshotId);

        result.IsError.ShouldBeFalse();

        Received.InOrder(() =>
        {
            _transaction.ArchiveCurrentAsync(IniSnapshotTrigger.LauncherGui, IniTestData.Now, Arg.Any<CancellationToken>());
            _files.WriteAsync(_installation.Path, Arg.Any<string>(), Arg.Any<CancellationToken>());
            _transaction.UpdateCurrentAsync(Arg.Any<string>(),
                                            Arg.Is<IReadOnlyList<IniValueChange>>(changes => changes.Count == 1
                                                                                            && changes[0].Id == ScreenWidth
                                                                                            && changes[0].Value == "1024"
                                                                                            && changes[0].Source == IniValueSource.LauncherGui),
                                            IniTestData.Now,
                                            Arg.Any<CancellationToken>());
            _transaction.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [ Fact ]
    public async Task Restore_WritesOnlyTheDifferingUserSettings()
    {
        DiskHolds(IniTestData.Sample);

        Guid snapshotId = SnapshotHolds(IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024")
                                                          .Replace(oldValue: "lastWindowX=10", newValue: "lastWindowX=99"));

        ErrorOr<IniConfigResult> result = await RestoreAsync(snapshotId);

        result.IsError.ShouldBeFalse();
        _written.ShouldBe(IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024"));
        result.Value.Values[ScreenWidth].ShouldBe(expected: "1024");
    }

    [ Fact ]
    public async Task Restore_KeyMissingFromSnapshot_IsLeftInTheFile()
    {
        DiskHolds(IniTestData.Sample);

        Guid snapshotId = SnapshotHolds(IniTestData.Sample.Replace(oldValue: "screenwidth=800\r\n", newValue: "").Replace(oldValue: "tooltipDelay=1", newValue: "tooltipDelay=7"));

        ErrorOr<IniConfigResult> result = await RestoreAsync(snapshotId);

        result.IsError.ShouldBeFalse();
        _written.ShouldBe(IniTestData.Sample.Replace(oldValue: "tooltipDelay=1", newValue: "tooltipDelay=7"));
        _written!.ShouldContain(expected: "screenwidth=800");
    }

    [ Fact ]
    public async Task Restore_InvalidSnapshotValue_WritesTheDefault()
    {
        DiskHolds(IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024"));

        Guid snapshotId = SnapshotHolds(IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=abc"));

        ErrorOr<IniConfigResult> result = await RestoreAsync(snapshotId);

        result.IsError.ShouldBeFalse();
        _written.ShouldBe(IniTestData.Sample);
        result.Value.Values[ScreenWidth].ShouldBe(expected: "800");
    }

    [ Fact ]
    public async Task Restore_NothingDiffers_CommitsWithoutArchivingOrWriting()
    {
        DiskHolds(IniTestData.Sample);

        Guid snapshotId = SnapshotHolds(IniTestData.Sample);

        ErrorOr<IniConfigResult> result = await RestoreAsync(snapshotId);

        result.IsError.ShouldBeFalse();
        result.Value.FileLastWriteUtc.ShouldBe(DiskLastWrite);

        await _files.DidNotReceive().WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().ArchiveCurrentAsync(Arg.Any<IniSnapshotTrigger>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _transaction.Received(requiredNumberOfCalls: 1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Restore_ExternalEditSinceOpening_IsReconciledThenOverwritten()
    {
        DiskHolds(IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1280"));

        Guid snapshotId = SnapshotHolds(IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024"));

        ErrorOr<IniConfigResult> result = await RestoreAsync(snapshotId);

        result.IsError.ShouldBeFalse();
        _written.ShouldBe(IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024"));

        Received.InOrder(() =>
        {
            _transaction.ArchiveCurrentAsync(IniSnapshotTrigger.Manual, IniTestData.Now, Arg.Any<CancellationToken>());
            _transaction.ArchiveCurrentAsync(IniSnapshotTrigger.LauncherGui, IniTestData.Now, Arg.Any<CancellationToken>());
            _files.WriteAsync(_installation.Path, Arg.Any<string>(), Arg.Any<CancellationToken>());
        });
    }

    [ Fact ]
    public async Task Restore_UnknownSnapshot_ReturnsSnapshotNotFoundAndWritesNothing()
    {
        DiskHolds(IniTestData.Sample);

        ErrorOr<IniConfigResult> result = await RestoreAsync(Guid.CreateVersion7());

        result.FirstError.Code.ShouldBe(expected: "Ini.SnapshotNotFound");

        await _transaction.DidNotReceive().ArchiveCurrentAsync(Arg.Any<IniSnapshotTrigger>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _files.DidNotReceive().WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Restore_ReturnsTheHistoricalCount()
    {
        DiskHolds(IniTestData.Sample);

        Guid snapshotId = SnapshotHolds(IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024"));

        bool archived = false;

        _transaction.ArchiveCurrentAsync(Arg.Any<IniSnapshotTrigger>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                    .Returns(_ =>
                    {
                        archived = true;

                        return Task.CompletedTask;
                    });

        _transaction.CountHistoricalAsync(Arg.Any<CancellationToken>())
                    .Returns(_ => archived ? 6 : 5);

        ErrorOr<IniConfigResult> result = await RestoreAsync(snapshotId);

        result.IsError.ShouldBeFalse();
        result.Value.HistoricalCount.ShouldBe(6);
    }

    [ Fact ]
    public async Task Restore_WriteFails_ReturnsWriteFailed()
    {
        DiskHolds(IniTestData.Sample);

        Guid snapshotId = SnapshotHolds(IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024"));

        _files.WriteAsync(_installation.Path, Arg.Any<string>(), Arg.Any<CancellationToken>())
              .ThrowsAsync(new UnauthorizedAccessException(message: "Access to the path is denied."));

        ErrorOr<IniConfigResult> result = await RestoreAsync(snapshotId);

        result.FirstError.Code.ShouldBe(expected: "Ini.WriteFailed");

        // The archive is rolled back: nothing is committed or recorded, and disposing the transaction uncommitted discards it.
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().UpdateCurrentAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<IniValueChange>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _transaction.Received(requiredNumberOfCalls: 1).DisposeAsync();
    }

    [ Fact ]
    public async Task Restore_RecordFailsAfterWrite_IsSwallowedAndSucceeds()
    {
        DiskHolds(IniTestData.Sample);

        Guid snapshotId = SnapshotHolds(IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024"));

        _transaction.UpdateCurrentAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<IniValueChange>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                    .ThrowsAsync(new InvalidOperationException(message: "database is locked"));

        ErrorOr<IniConfigResult> result = await RestoreAsync(snapshotId);

        result.IsError.ShouldBeFalse();
        result.Value.Values[ScreenWidth].ShouldBe(expected: "1024");
        result.Value.FileLastWriteUtc.ShouldBe(NewLastWrite);
        _written.ShouldNotBeNull();

        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private void DiskHolds(string text)
        => _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
                 .Returns(new IniFileContent(text, DiskLastWrite));

    private Guid SnapshotHolds(string text)
    {
        IniSnapshot snapshot = IniTestData.Snapshot(text, IniSnapshotKind.Historical);

        _transaction.GetByIdAsync(snapshot.Id, Arg.Any<CancellationToken>())
                    .Returns(snapshot);

        return snapshot.Id;
    }

    private async Task<ErrorOr<IniConfigResult>> RestoreAsync(Guid snapshotId)
    {
        IniWritePipeline pipeline = new(new FakeTimeProvider(IniTestData.Now),
                                        _files,
                                        _installations,
                                        NullLogger<IniWritePipeline>.Instance,
                                        IniTestData.Probe(InstallationWriteAccess.Writable),
                                        new IniReconciler(),
                                        _snapshots);

        return await new RestoreIniSnapshotHandler(pipeline).Handle(new RestoreIniSnapshotCommand(_installation.Id, snapshotId), CancellationToken.None);
    }
}
