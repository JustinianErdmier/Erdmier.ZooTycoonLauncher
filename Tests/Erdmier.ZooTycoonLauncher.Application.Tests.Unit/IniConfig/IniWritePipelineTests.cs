namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class IniWritePipelineTests
{
    private static readonly DateTime DiskLastWrite = IniTestData.Now.AddHours(-1);

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

    public IniWritePipelineTests()
    {
        _installations.GetByIdAsync(_installation.Id, Arg.Any<CancellationToken>())
                      .Returns(_installation);

        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(IniTestData.Sample, DiskLastWrite));

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(_ => _transaction);
    }

    [ Fact ]
    public async Task WriteAsync_SelectorError_ReturnsItAndWritesNothing()
    {
        Error selectorError = Error.Validation(code: "Test.Selector", description: "The selector refused.");

        ErrorOr<IniConfigResult> result = await CreatePipeline().WriteAsync(_installation.Id,
                                                                            (_, _, _) => Task.FromResult<ErrorOr<IReadOnlyDictionary<IniKeyId, string>>>(selectorError),
                                                                            CancellationToken.None);

        result.FirstError.ShouldBe(selectorError);

        await _files.DidNotReceive().WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().ArchiveCurrentAsync(Arg.Any<IniSnapshotTrigger>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task WriteAsync_SelectorThrows_ReturnsStoreFailed()
    {
        ErrorOr<IniConfigResult> result = await CreatePipeline().WriteAsync(_installation.Id,
                                                                            (_, _, _) => throw new InvalidOperationException(message: "selector blew up"),
                                                                            CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.StoreFailed");
        result.FirstError.Description.ShouldContain(expected: "selector blew up");

        await _files.DidNotReceive().WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task WriteAsync_EmptySelection_CommitsWithoutArchiving()
    {
        ErrorOr<IniConfigResult> result = await CreatePipeline().WriteAsync(_installation.Id,
                                                                            (_, _, _) => Task.FromResult<ErrorOr<IReadOnlyDictionary<IniKeyId, string>>>(new Dictionary<IniKeyId, string>()),
                                                                            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.FileLastWriteUtc.ShouldBe(DiskLastWrite);

        await _transaction.DidNotReceive().ArchiveCurrentAsync(IniSnapshotTrigger.LauncherGui, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _files.DidNotReceive().WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _transaction.Received(requiredNumberOfCalls: 1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Write_CommitFails_LogsNoReconciliation()
    {
        RecordingLogger<IniWritePipeline> logger = new();

        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(Drifted(), DiskLastWrite));

        _files.WriteAsync(_installation.Path, Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(IniTestData.Now);

        _transaction.CommitAsync(Arg.Any<CancellationToken>())
                    .ThrowsAsync(new InvalidOperationException(message: "database is locked"));

        ErrorOr<IniConfigResult> result = await CreatePipeline(logger).WriteAsync(_installation.Id, SelectScreenWidth, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        logger.Entries.ShouldNotContain(entry => entry.Message.Contains("reconciled"));
    }

    [ Fact ]
    public async Task Write_Succeeds_LogsReconciliationAfterCommit()
    {
        RecordingLogger<IniWritePipeline> logger = new();

        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(Drifted(), DiskLastWrite));

        _files.WriteAsync(_installation.Path, Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(IniTestData.Now);

        bool reconciledBeforeCommit = false;

        _transaction.When(t => t.CommitAsync(Arg.Any<CancellationToken>()))
                    .Do(_ => reconciledBeforeCommit = logger.Entries.Any(entry => entry.Message.Contains("INI snapshot reconciled")));

        ErrorOr<IniConfigResult> result = await CreatePipeline(logger).WriteAsync(_installation.Id, SelectScreenWidth, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        reconciledBeforeCommit.ShouldBeFalse();

        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Information
                                              && entry.Message.Contains("INI snapshot reconciled")
                                              && entry.Message.Contains(_installation.Id.ToString()));
    }

    [ Fact ]
    public async Task Write_NoOpSelection_LogsReconciliationAfterCommit()
    {
        RecordingLogger<IniWritePipeline> logger = new();

        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(Drifted(), DiskLastWrite));

        bool reconciledBeforeCommit = false;

        _transaction.When(t => t.CommitAsync(Arg.Any<CancellationToken>()))
                    .Do(_ => reconciledBeforeCommit = logger.Entries.Any(entry => entry.Message.Contains("INI snapshot reconciled")));

        ErrorOr<IniConfigResult> result = await CreatePipeline(logger).WriteAsync(_installation.Id, SelectNothing, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        reconciledBeforeCommit.ShouldBeFalse();
        logger.Entries.ShouldContain(entry => entry.Message.Contains("INI snapshot reconciled"));
    }

    [ Fact ]
    public async Task Write_NoOpSelection_CommitFails_LogsNoReconciliation()
    {
        RecordingLogger<IniWritePipeline> logger = new();

        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(new IniFileContent(Drifted(), DiskLastWrite));

        _transaction.CommitAsync(Arg.Any<CancellationToken>())
                    .ThrowsAsync(new InvalidOperationException(message: "database is locked"));

        ErrorOr<IniConfigResult> result = await CreatePipeline(logger).WriteAsync(_installation.Id, SelectNothing, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.StoreFailed");
        logger.Entries.ShouldNotContain(entry => entry.Message.Contains("reconciled"));
    }

    [ Fact ]
    public async Task Write_OrphanCleanupThrows_ReturnsReadFailed()
    {
        _files.DeleteOrphanedTempFilesAsync(_installation.Path, Arg.Any<CancellationToken>())
              .ThrowsAsync(new IOException(message: "access denied"));

        IInstallationWriteProbe probe = IniTestData.Probe(InstallationWriteAccess.Writable);

        ErrorOr<IniConfigResult> result = await CreatePipeline(probe: probe).WriteAsync(_installation.Id, SelectScreenWidth, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.ReadFailed");

        await probe.DidNotReceive().ProbeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _files.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _files.DidNotReceive().WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Write_AccessDenied_FileReadOnly_ReturnsReadOnly()
    {
        _files.WriteAsync(_installation.Path, Arg.Any<string>(), Arg.Any<CancellationToken>())
              .ThrowsAsync(new UnauthorizedAccessException(message: "denied"));

        ErrorOr<IniConfigResult> result = await CreatePipeline(probe: IniTestData.Probe(InstallationWriteAccess.FileReadOnly))
                                              .WriteAsync(_installation.Id, SelectScreenWidth, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.ReadOnly");
        result.FirstError.Description.ShouldBe(IniReadOnlyText.For(InstallationWriteAccess.FileReadOnly));

        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Write_AccessDenied_FolderReadOnly_ReturnsReadOnly()
    {
        _files.WriteAsync(_installation.Path, Arg.Any<string>(), Arg.Any<CancellationToken>())
              .ThrowsAsync(new UnauthorizedAccessException(message: "denied"));

        ErrorOr<IniConfigResult> result = await CreatePipeline(probe: IniTestData.Probe(InstallationWriteAccess.FolderReadOnly))
                                              .WriteAsync(_installation.Id, SelectScreenWidth, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.ReadOnly");
        result.FirstError.Description.ShouldBe(IniReadOnlyText.For(InstallationWriteAccess.FolderReadOnly));

        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Write_AccessDenied_ProbeWritable_ReturnsWriteFailed()
    {
        _files.WriteAsync(_installation.Path, Arg.Any<string>(), Arg.Any<CancellationToken>())
              .ThrowsAsync(new UnauthorizedAccessException(message: "denied"));

        ErrorOr<IniConfigResult> result = await CreatePipeline(probe: IniTestData.Probe(InstallationWriteAccess.Writable))
                                              .WriteAsync(_installation.Id, SelectScreenWidth, CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Ini.WriteFailed");

        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Write_DeletesOrphanedTempFilesBeforeReading()
    {
        await CreatePipeline().WriteAsync(_installation.Id, SelectScreenWidth, CancellationToken.None);

        Received.InOrder(() =>
        {
            _files.DeleteOrphanedTempFilesAsync(_installation.Path, Arg.Any<CancellationToken>());
            _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>());
        });
    }

    private static string Drifted() => IniTestData.Sample.Replace(oldValue: "tooltipDelay=1", newValue: "tooltipDelay=5");

    private static Task<ErrorOr<IReadOnlyDictionary<IniKeyId, string>>> SelectNothing(IIniSnapshotTransaction        transaction,
                                                                                      IReadOnlyDictionary<IniKeyId, string?> values,
                                                                                      CancellationToken                    cancellationToken)
        => Task.FromResult<ErrorOr<IReadOnlyDictionary<IniKeyId, string>>>(new Dictionary<IniKeyId, string>());

    private static Task<ErrorOr<IReadOnlyDictionary<IniKeyId, string>>> SelectScreenWidth(IIniSnapshotTransaction        transaction,
                                                                                         IReadOnlyDictionary<IniKeyId, string?> values,
                                                                                         CancellationToken                    cancellationToken)
        => Task.FromResult<ErrorOr<IReadOnlyDictionary<IniKeyId, string>>>(new Dictionary<IniKeyId, string> { [new IniKeyId(Section: "user", Key: "screenwidth")] = "1024" });

    private IniWritePipeline CreatePipeline(ILogger<IniWritePipeline>? logger = null, IInstallationWriteProbe? probe = null)
        => new(new FakeTimeProvider(IniTestData.Now),
               _files,
               _installations,
               logger ?? NullLogger<IniWritePipeline>.Instance,
               probe ?? IniTestData.Probe(InstallationWriteAccess.Writable),
               new IniReconciler(),
               _snapshots);
}
