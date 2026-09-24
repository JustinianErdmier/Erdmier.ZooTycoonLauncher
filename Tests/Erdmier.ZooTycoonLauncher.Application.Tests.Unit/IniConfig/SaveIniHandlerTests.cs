using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute.ExceptionExtensions;

namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class SaveIniHandlerTests
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

    private IIniSnapshotTransaction _transaction = IniTestData.Transaction(IniTestData.CurrentSnapshot(IniTestData.Sample));

    private string? _written;

    public SaveIniHandlerTests()
    {
        _installations.GetByIdAsync(_installation.Id, Arg.Any<CancellationToken>())
                      .Returns(_installation);

        _files.WriteAsync(_installation.Path, Arg.Do<string>(text => _written = text), Arg.Any<CancellationToken>())
              .Returns(NewLastWrite);

        _snapshots.BeginAsync(_installation.Id, Arg.Any<CancellationToken>())
                  .Returns(_ => _transaction);
    }

    [ Fact ]
    public async Task Save_FollowsTheSdd82Order()
    {
        DiskHolds(IniTestData.Sample);

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "1024"));

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
    public async Task Save_ChangesOnlyTheEditedLineAndKeepsNonAsciiBytes()
    {
        DiskHolds(IniTestData.Sample);

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "1024"));

        _written.ShouldBe(IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024"));
        _written!.ShouldContain(expected: "Zoo M\u00FCller.zoo");
        result.Value.Values[ScreenWidth].ShouldBe(expected: "1024");
        result.Value.FileLastWriteUtc.ShouldBe(NewLastWrite);
    }

    [ Fact ]
    public async Task Save_MergesOntoTheOnDiskTextAndArchivesExternalDriftFirst()
    {
        string disk = IniTestData.Sample.Replace(oldValue: "tooltipDelay=1", newValue: "tooltipDelay=5").Replace(oldValue: "lastWindowX=10", newValue: "lastWindowX=300");

        DiskHolds(disk);

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "1024"));

        _written.ShouldBe(disk.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024"));
        result.Value.Values[new IniKeyId(Section: "UI", Key: "tooltipDelay")].ShouldBe(expected: "5");

        Received.InOrder(() =>
        {
            _transaction.ArchiveCurrentAsync(IniSnapshotTrigger.Manual, IniTestData.Now, Arg.Any<CancellationToken>());
            _transaction.ArchiveCurrentAsync(IniSnapshotTrigger.LauncherGui, IniTestData.Now, Arg.Any<CancellationToken>());
        });
    }

    [ Fact ]
    public async Task Save_WriteFails_RollsBackAndReturnsWriteFailed()
    {
        DiskHolds(IniTestData.Sample);

        _files.WriteAsync(_installation.Path, Arg.Any<string>(), Arg.Any<CancellationToken>())
              .ThrowsAsync(new UnauthorizedAccessException(message: "Access to the path is denied."));

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "1024"));

        result.FirstError.Code.ShouldBe(expected: "Ini.WriteFailed");
        result.FirstError.Description.ShouldContain(expected: "denied");

        await _transaction.DidNotReceive().UpdateCurrentAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<IniValueChange>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(requiredNumberOfCalls: 1).DisposeAsync();
    }

    [ Fact ]
    public async Task Save_NoOpEdits_CommitWithoutWriting()
    {
        DiskHolds(IniTestData.Sample);

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "0800"));

        result.Value.FileLastWriteUtc.ShouldBe(DiskLastWrite);

        await _files.DidNotReceive().WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _transaction.Received(requiredNumberOfCalls: 1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Save_EmptiedFile_ImportsThenAppendsTheSectionAndKey()
    {
        _transaction = IniTestData.Transaction(current: null);

        DiskHolds(text: "");

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "1024"));

        result.IsError.ShouldBeFalse();
        _written.ShouldBe(expected: "[user]\r\nscreenwidth=1024\r\n");

        await _transaction.Received(requiredNumberOfCalls: 2).AddAsync(Arg.Any<IniSnapshot>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Save_EditKeyInAnyCasing_InsertsWithRegistryCasing()
    {
        DiskHolds(IniTestData.Sample);

        ErrorOr<IniConfigResult> result = await SaveAsync((new IniKeyId(Section: "USER", Key: "drawrate"), "30"));

        // DrawRate is missing from [user], so it is inserted after that section's last key (lastfile=…).
        _written.ShouldBe(IniTestData.Sample.Replace(oldValue: "Zoo M\u00FCller.zoo\r\n", newValue: "Zoo M\u00FCller.zoo\r\nDrawRate=30\r\n"));
        result.Value.Values[new IniKeyId(Section: "user", Key: "DrawRate")].ShouldBe(expected: "30");
    }

    [ Fact ]
    public async Task Save_FileMissing_ReturnsMissing()
    {
        _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns((IniFileContent?)null);

        ErrorOr<IniConfigResult> result = await SaveAsync((ScreenWidth, "1024"));

        result.FirstError.Code.ShouldBe(expected: "Ini.Missing");
    }

    [ Fact ]
    public async Task Save_UnknownInstallation_ReturnsNotFound()
    {
        SaveIniHandler handler = CreateHandler();

        ErrorOr<IniConfigResult> result = await handler.Handle(new SaveIniCommand(Guid.CreateVersion7(), new Dictionary<IniKeyId, string> { [ScreenWidth] = "1024" }),
                                                               CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Installation.NotFound");
    }

    private void DiskHolds(string text)
        => _files.ReadAsync(_installation.Path, Arg.Any<CancellationToken>())
                 .Returns(new IniFileContent(text, DiskLastWrite));

    private async Task<ErrorOr<IniConfigResult>> SaveAsync(params (IniKeyId Id, string Value)[] edits)
        => await CreateHandler().Handle(new SaveIniCommand(_installation.Id, edits.ToDictionary(edit => edit.Id, edit => edit.Value)), CancellationToken.None);

    private SaveIniHandler CreateHandler()
        => new(new FakeTimeProvider(IniTestData.Now), _files, _installations, NullLogger<SaveIniHandler>.Instance, new IniReconciler(), _snapshots);
}
