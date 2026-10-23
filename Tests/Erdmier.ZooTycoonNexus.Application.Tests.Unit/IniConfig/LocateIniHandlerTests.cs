namespace Erdmier.ZooTycoonNexus.Application.Tests.Unit.IniConfig;

public sealed class LocateIniHandlerTests
{
    private const string SourcePath = @"D:\Backup\zoo.ini";

    private readonly IApplicationEventPublisher _events = Substitute.For<IApplicationEventPublisher>();

    private readonly IIniFileStore _files = Substitute.For<IIniFileStore>();

    private readonly GameInstallation _installation = new()
    {
        Id     = Guid.CreateVersion7(),
        Name   = "Zoo",
        Path   = @"C:\Games\Zoo",
        HasExe = true,
        HasIni = false
    };

    private readonly IInstallationRepository _installations = Substitute.For<IInstallationRepository>();

    private readonly IInstallationWriteProbe _probe = Substitute.For<IInstallationWriteProbe>();

    private readonly IIniSnapshotService _snapshots = Substitute.For<IIniSnapshotService>();

    public LocateIniHandlerTests()
    {
        _installations.GetByIdAsync(_installation.Id, Arg.Any<CancellationToken>())
                      .Returns(_installation);

        _files.TryCreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(true);

        _snapshots.SynchroniseAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>())
                  .Returns(Result.Success);
    }

    [ Fact ]
    public async Task Locate_CopiesTheSourceTextUnchanged()
    {
        const string text = "; mine\r\n[user]\r\nfullscreen=0\r\n";

        ReturnsSource(text);

        ErrorOr<IniRecoveryResult> result = await Handle();

        result.IsError.ShouldBeFalse();
        result.Value.Validity.ShouldBe(InstallationValidity.Valid);
        _installation.HasIni.ShouldBeTrue();

        await _files.Received(requiredNumberOfCalls: 1)
                    .TryCreateAsync(_installation.Path, text, Arg.Any<CancellationToken>());

        await _files.Received(requiredNumberOfCalls: 1)
                    .ReadExternalAsync(SourcePath, maxBytes: 1_048_576, Arg.Any<CancellationToken>());

        await _snapshots.Received(requiredNumberOfCalls: 1)
                        .SynchroniseAsync(_installation, Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Locate_SectionNameIsMatchedCaseInsensitively()
    {
        ReturnsSource("[USER]\r\nfullscreen=0\r\n");

        ErrorOr<IniRecoveryResult> result = await Handle();

        result.IsError.ShouldBeFalse();
    }

    [ Fact ]
    public async Task Locate_NoRecognisedSection_ReturnsNotAnIni()
    {
        ReturnsSource("[settings]\r\nfoo=1\r\n");

        await AssertNotAnIni();
    }

    [ Fact ]
    public async Task Locate_EmptyFile_ReturnsNotAnIni()
    {
        ReturnsSource(string.Empty);

        await AssertNotAnIni();
    }

    [ Fact ]
    public async Task Locate_OverLimit_ReturnsNotAnIni()
    {
        ReturnsSource(text: null);

        await AssertNotAnIni();
    }

    [ Fact ]
    public async Task Locate_SourceUnreadable_ReturnsReadFailed()
    {
        _files.ReadExternalAsync(SourcePath, Arg.Any<long>(), Arg.Any<CancellationToken>())
              .Throws(new IOException("in use"));

        ErrorOr<IniRecoveryResult> result = await Handle();

        result.FirstError.Code.ShouldBe(expected: "Ini.ReadFailed");
        result.FirstError.Description.ShouldContain("in use");

        await _files.DidNotReceiveWithAnyArgs()
                    .TryCreateAsync(default!, default!, default);
    }

    [ Fact ]
    public async Task Locate_UnknownInstallation_ReturnsNotFound()
    {
        ErrorOr<IniRecoveryResult> result = await CreateHandler().Handle(new LocateIniCommand(Guid.CreateVersion7(), SourcePath), CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Installation.NotFound");

        await _files.DidNotReceiveWithAnyArgs()
                    .ReadExternalAsync(default!, default, default);
    }

    [ Fact ]
    public async Task Locate_FileAlreadyExists_ReturnsAlreadyExists()
    {
        ReturnsSource("[user]\r\n");

        _files.TryCreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(false);

        ErrorOr<IniRecoveryResult> result = await Handle();

        result.FirstError.Code.ShouldBe(expected: "Ini.AlreadyExists");
    }

    private async Task AssertNotAnIni()
    {
        ErrorOr<IniRecoveryResult> result = await Handle();

        result.FirstError.Code.ShouldBe(expected: "Ini.NotAnIni");
        result.FirstError.Description.ShouldContain("\"zoo.ini\"");

        await _files.DidNotReceiveWithAnyArgs()
                    .TryCreateAsync(default!, default!, default);
    }

    private LocateIniHandler CreateHandler()
        => new(_files,
               _installations,
               new IniRecoverySteps(_events, _files, _installations, new RecordingLogger<IniRecoverySteps>(), _probe, _snapshots, new FakeTimeProvider(IniTestData.Now)));

    private async Task<ErrorOr<IniRecoveryResult>> Handle() => await CreateHandler().Handle(new LocateIniCommand(_installation.Id, SourcePath), CancellationToken.None);

    private void ReturnsSource(string? text)
        => _files.ReadExternalAsync(SourcePath, Arg.Any<long>(), Arg.Any<CancellationToken>())
                 .Returns(text);
}
