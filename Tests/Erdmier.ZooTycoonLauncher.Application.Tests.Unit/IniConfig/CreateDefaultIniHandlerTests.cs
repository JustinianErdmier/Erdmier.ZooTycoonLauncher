namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class CreateDefaultIniHandlerTests
{
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

    private readonly RecordingLogger<IniRecoverySteps> _logger = new();

    private readonly IInstallationWriteProbe _probe = Substitute.For<IInstallationWriteProbe>();

    private readonly IIniSnapshotService _snapshots = Substitute.For<IIniSnapshotService>();

    public CreateDefaultIniHandlerTests()
    {
        _installations.GetByIdAsync(_installation.Id, Arg.Any<CancellationToken>())
                      .Returns(_installation);

        _files.TryCreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(true);

        _snapshots.SynchroniseAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>())
                  .Returns(Result.Success);
    }

    [ Fact ]
    public async Task Create_WritesTheDefaultDocument_SetsHasIni_PublishesAndSynchronises()
    {
        ErrorOr<IniRecoveryResult> result = await Handle();

        result.IsError.ShouldBeFalse();
        result.Value.Validity.ShouldBe(InstallationValidity.Valid);
        _installation.HasIni.ShouldBeTrue();

        await _files.Received(requiredNumberOfCalls: 1)
                    .TryCreateAsync(_installation.Path, ZooIniDefaultDocument.Render(), Arg.Any<CancellationToken>());

        _events.Received(requiredNumberOfCalls: 1)
               .Publish(Arg.Is<InstallationChangedMessage>(message => message.InstallationId == _installation.Id));

        Received.InOrder(() =>
        {
            _files.DeleteOrphanedTempFilesAsync(_installation.Path, Arg.Any<CancellationToken>());
            _files.TryCreateAsync(_installation.Path, Arg.Any<string>(), Arg.Any<CancellationToken>());
            _installations.UpdateAsync(Arg.Is<GameInstallation>(row => row.HasIni), Arg.Any<CancellationToken>());
            _snapshots.SynchroniseAsync(_installation, Arg.Any<CancellationToken>());
        });
    }

    [ Fact ]
    public async Task Create_FileAlreadyExists_ReturnsAlreadyExists_AndChangesNothing()
    {
        _files.TryCreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(false);

        ErrorOr<IniRecoveryResult> result = await Handle();

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Ini.AlreadyExists");
        _installation.HasIni.ShouldBeFalse();

        await _installations.DidNotReceiveWithAnyArgs()
                            .UpdateAsync(default!, default);

        _events.DidNotReceiveWithAnyArgs()
               .Publish(default(InstallationChangedMessage)!);

        await _snapshots.DidNotReceiveWithAnyArgs()
                        .SynchroniseAsync(default!, default);
    }

    [ Fact ]
    public async Task Create_AccessDenied_FolderReadOnly_ReturnsReadOnly()
    {
        _files.TryCreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Throws(new UnauthorizedAccessException("denied"));

        _probe.ProbeAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(InstallationWriteAccess.FolderReadOnly);

        ErrorOr<IniRecoveryResult> result = await Handle();

        result.FirstError.Code.ShouldBe(expected: "Ini.ReadOnly");
        result.FirstError.Description.ShouldBe(IniReadOnlyText.For(InstallationWriteAccess.FolderReadOnly));
        _installation.HasIni.ShouldBeFalse();

        await AssertNothingRecordedAsync();
    }

    [ Fact ]
    public async Task Create_AccessDenied_ProbeUnknown_ReturnsWriteFailed()
    {
        _files.TryCreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Throws(new UnauthorizedAccessException("denied"));

        _probe.ProbeAsync(_installation.Path, Arg.Any<CancellationToken>())
              .Returns(InstallationWriteAccess.Unknown);

        ErrorOr<IniRecoveryResult> result = await Handle();

        result.FirstError.Code.ShouldBe(expected: "Ini.WriteFailed");
        result.FirstError.Description.ShouldContain("denied");
    }

    [ Fact ]
    public async Task Create_IoFailure_ReturnsWriteFailed()
    {
        _files.TryCreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Throws(new IOException("disk full"));

        ErrorOr<IniRecoveryResult> result = await Handle();

        result.FirstError.Code.ShouldBe(expected: "Ini.WriteFailed");
        _installation.HasIni.ShouldBeFalse();

        await AssertNothingRecordedAsync();
    }

    [ Fact ]
    public async Task Create_SynchroniseFails_StillSucceeds_AndLogsWarning()
    {
        _snapshots.SynchroniseAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>())
                  .Returns(IniErrors.StoreFailed("locked"));

        ErrorOr<IniRecoveryResult> result = await Handle();

        result.IsError.ShouldBeFalse();
        _installation.HasIni.ShouldBeTrue();
        _logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
    }

    [ Fact ]
    public async Task Create_SynchroniseThrows_StillSucceeds_AndLogsError()
    {
        _snapshots.SynchroniseAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>())
                  .Throws(new InvalidOperationException("boom"));

        ErrorOr<IniRecoveryResult> result = await Handle();

        result.IsError.ShouldBeFalse();
        _logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error);
    }

    [ Fact ]
    public async Task Create_RowUpdateFails_StillSucceeds_AndLogsError()
    {
        _installations.UpdateAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>())
                      .Throws(new InvalidOperationException("locked"));

        ErrorOr<IniRecoveryResult> result = await Handle();

        result.IsError.ShouldBeFalse();
        _logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error);

        await _snapshots.Received(requiredNumberOfCalls: 1)
                        .SynchroniseAsync(_installation, Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Create_UnknownInstallation_ReturnsNotFound()
    {
        ErrorOr<IniRecoveryResult> result = await CreateHandler().Handle(new CreateDefaultIniCommand(Guid.CreateVersion7()), CancellationToken.None);

        result.FirstError.Code.ShouldBe(expected: "Installation.NotFound");

        await _files.DidNotReceiveWithAnyArgs()
                    .TryCreateAsync(default!, default!, default);
    }

    private async Task AssertNothingRecordedAsync()
    {
        await _installations.DidNotReceiveWithAnyArgs()
                            .UpdateAsync(default!, default);

        _events.DidNotReceiveWithAnyArgs()
               .Publish(default(InstallationChangedMessage)!);

        await _snapshots.DidNotReceiveWithAnyArgs()
                        .SynchroniseAsync(default!, default);
    }

    private CreateDefaultIniHandler CreateHandler()
        => new(_installations, new IniRecoverySteps(_events, _files, _installations, _logger, _probe, _snapshots, new FakeTimeProvider(IniTestData.Now)));

    private async Task<ErrorOr<IniRecoveryResult>> Handle() => await CreateHandler().Handle(new CreateDefaultIniCommand(_installation.Id), CancellationToken.None);
}
