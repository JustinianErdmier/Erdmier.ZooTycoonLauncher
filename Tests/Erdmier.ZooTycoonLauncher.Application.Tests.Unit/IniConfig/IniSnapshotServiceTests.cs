namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class IniSnapshotServiceTests
{
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

    private readonly IIniSnapshotRepository _snapshots = Substitute.For<IIniSnapshotRepository>();

    private IniSnapshotService CreateService(ILogger<IniSnapshotService>? logger = null)
        => new(new FakeTimeProvider(IniTestData.Now), _files, logger ?? NullLogger<IniSnapshotService>.Instance, new IniReconciler(), _snapshots);

    [ Fact ]
    public async Task Synchronise_NoIni_IsANoOp()
    {
        _installation.HasIni = false;

        ErrorOr<Success> result = await CreateService().SynchroniseAsync(_installation, CancellationToken.None);

        result.IsError.ShouldBeFalse();

        await _files.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Load_NoIni_ReturnsMissing()
    {
        _installation.HasIni = false;

        ErrorOr<IniConfigResult> result = await CreateService().LoadAsync(_installation, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.Missing");
    }

    [ Fact ]
    public async Task Load_FileAbsent_ReturnsMissing()
    {
        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns((IniFileContent?)null);

        ErrorOr<IniConfigResult> result = await CreateService().LoadAsync(_installation, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.Missing");
    }

    [ Fact ]
    public async Task Load_ReadThrowsIoException_ReturnsReadFailed()
    {
        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .ThrowsAsync(new IOException(message: "locked"));

        ErrorOr<IniConfigResult> result = await CreateService().LoadAsync(_installation, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.ReadFailed");
        result.FirstError.Description.ShouldContain(expected: "locked");
    }

    [ Fact ]
    public async Task Load_RepositoryThrows_ReturnsStoreFailed()
    {
        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(IniTestData.Sample, IniTestData.Now));

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .ThrowsAsync(new InvalidOperationException(message: "database is corrupt"));

        ErrorOr<IniConfigResult> result = await CreateService().LoadAsync(_installation, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.StoreFailed");
    }

    [ Fact ]
    public async Task Load_CountThrows_ReturnsStoreFailed()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(current: null);

        transaction.CountHistoricalAsync(Arg.Any<CancellationToken>())
                   .ThrowsAsync(new InvalidOperationException(message: "no such table"));

        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(IniTestData.Sample, IniTestData.Now));

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(transaction);

        ErrorOr<IniConfigResult> result = await CreateService().LoadAsync(_installation, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.StoreFailed");
        result.FirstError.Description.ShouldContain(expected: "no such table");

        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Load_Success_CleansOrphansFirstThenCommitsAndReturnsTheValues()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(current: null);

        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(IniTestData.Sample, IniTestData.Now.AddMinutes(-5)));

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(transaction);

        ErrorOr<IniConfigResult> result = await CreateService().LoadAsync(_installation, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.FileLastWriteUtc.ShouldBe(IniTestData.Now.AddMinutes(-5));
        result.Value.Values[new IniKeyId(Section: "user", Key: "screenwidth")].ShouldBe(expected: "800");

        Received.InOrder(() =>
        {
            _files.DeleteOrphanedTempFilesAsync(_installation.Path, Arg.Any<CancellationToken>());
            _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>());
            transaction.AddAsync(Arg.Any<IniSnapshot>(), Arg.Any<CancellationToken>());
            transaction.AddAsync(Arg.Any<IniSnapshot>(), Arg.Any<CancellationToken>());
            transaction.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [ Fact ]
    public async Task Load_StoreFailsAtCommit_LogsNoReconciliation()
    {
        RecordingLogger<IniSnapshotService> logger      = new();
        IIniSnapshotTransaction             transaction = IniTestData.Transaction(current: null);

        transaction.CommitAsync(Arg.Any<CancellationToken>())
                   .ThrowsAsync(new InvalidOperationException(message: "database is locked"));

        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(IniTestData.Sample, IniTestData.Now));

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(transaction);

        ErrorOr<IniConfigResult> result = await CreateService(logger).LoadAsync(_installation, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.StoreFailed");
        logger.Entries.ShouldNotContain(entry => entry.Message.Contains("reconciled"));
    }

    [ Fact ]
    public async Task Load_Success_LogsReconciliationAfterCommit()
    {
        RecordingLogger<IniSnapshotService> logger      = new();
        IIniSnapshotTransaction             transaction = IniTestData.Transaction(current: null);

        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(IniTestData.Sample, IniTestData.Now));

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(transaction);

        ErrorOr<IniConfigResult> result = await CreateService(logger).LoadAsync(_installation, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Information && entry.Message.Contains("INI snapshot reconciled"));
    }

    [ Fact ]
    public async Task Load_ReturnsTheHistoricalCount()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(current: null);

        transaction.CountHistoricalAsync(Arg.Any<CancellationToken>())
                   .Returns(3);

        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(IniTestData.Sample, IniTestData.Now));

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(transaction);

        ErrorOr<IniConfigResult> result = await CreateService().LoadAsync(_installation, CancellationToken.None);

        result.Value.HistoricalCount.ShouldBe(3);

        Received.InOrder(() =>
        {
            transaction.CountHistoricalAsync(Arg.Any<CancellationToken>());
            transaction.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [ Fact ]
    public async Task CaptureOriginal_ImportsAndCommits()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(current: null);

        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(IniTestData.Sample, IniTestData.Now));

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(transaction);

        ErrorOr<Success> result = await CreateService().CaptureOriginalAsync(_installation, CancellationToken.None);

        result.IsError.ShouldBeFalse();

        await transaction.Received(requiredNumberOfCalls: 1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
