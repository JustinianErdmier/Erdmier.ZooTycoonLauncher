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

    private readonly IIniSnapshotTransaction _transaction = IniTestData.Transaction(IniTestData.CurrentSnapshot(IniTestData.Sample));

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

    private IniWritePipeline CreatePipeline()
        => new(new FakeTimeProvider(IniTestData.Now), _files, _installations, NullLogger<IniWritePipeline>.Instance, new IniReconciler(), _snapshots);
}
