namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Boot;

public sealed class BootHandlerTests
{
    [ Fact ]
    public async Task Handle_ReturnsReadyToPlay_WhenDefaultInstallationValid()
    {
        Guid id = Guid.CreateVersion7();

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { DefaultInstallationId = id });

        GameInstallation row = new()
        {
            Id = id, Name = "Main", Path = @"C:\ZT",
            HasExe = true, HasIni = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(row, Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(), snapshots, TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.ReadyToPlay);
        result.Value.ActiveInstallation!.Id.ShouldBe(id);
    }

    [ Fact ]
    public async Task Handle_ReturnsCannotPlay_WhenVerificationFails()
    {
        Guid id = Guid.CreateVersion7();

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { DefaultInstallationId = id });

        GameInstallation row = new()
        {
            Id = id, Name = "Main", Path = @"C:\ZT",
            HasExe = true, HasIni = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: false, HasIni: true));

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(),
                                  Substitute.For<IIniSnapshotService>(), TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.CannotPlay);
        result.Value.ActiveInstallation!.Id.ShouldBe(id);
    }

    [ Fact ]
    public async Task Handle_ReturnsCannotPlay_WhenSynchroniseFails()
    {
        Guid id = Guid.CreateVersion7();

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { DefaultInstallationId = id });

        GameInstallation row = new()
        {
            Id = id, Name = "Main", Path = @"C:\ZT",
            HasExe = true, HasIni = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(row, Arg.Any<CancellationToken>())
                 .Returns(Error.Failure(code: "Ini.SyncFailed", description: "boom"));

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(), snapshots, TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.CannotPlay);
    }

    [ Fact ]
    public async Task Handle_PersistsVerificationDrift()
    {
        Guid     id      = Guid.CreateVersion7();
        DateTime fakeNow = new(year: 2026, month: 5, day: 28, hour: 10, minute: 0, second: 0, DateTimeKind.Utc);

        FakeTimeProvider clock = new(fakeNow);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { DefaultInstallationId = id });

        GameInstallation row = new()
        {
            Id = id, Name = "Main", Path = @"C:\ZT",
            HasExe = true, HasIni = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: false, HasIni: true));

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(),
                                  Substitute.For<IIniSnapshotService>(), clock);

        await handler.Handle(new BootCommand(), CancellationToken.None);

        await installations.Received(requiredNumberOfCalls: 1)
                           .UpdateAsync(Arg.Is<GameInstallation>(i => i.Id == id
                                                                      && !i.HasExe
                                                                      && i.HasIni
                                                                      && i.ModifiedUtc == fakeNow),
                                        Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_PersistsDriftAndStampsLastOpened_WhenDriftButStillPlayable()
    {
        Guid     id      = Guid.CreateVersion7();
        DateTime fakeNow = new(year: 2026, month: 5, day: 28, hour: 10, minute: 0, second: 0, DateTimeKind.Utc);

        FakeTimeProvider clock = new(fakeNow);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { DefaultInstallationId = id });

        GameInstallation row = new()
        {
            Id = id, Name = "Main", Path = @"C:\ZT",
            HasExe = true, HasIni = false,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(row, Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(), snapshots, clock);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.ReadyToPlay);

        // Exactly two UpdateAsync calls: one for drift correction and one to stamp LastOpenedUtc.
        // NSubstitute captures argument references — both calls pass the same mutable row, so predicates
        // are evaluated against the final object state. We verify total count and final state instead.
        await installations.Received(requiredNumberOfCalls: 2)
                           .UpdateAsync(Arg.Any<GameInstallation>(), Arg.Any<CancellationToken>());

        row.HasIni.ShouldBeTrue();
        row.ModifiedUtc.ShouldBe(fakeNow);
        row.LastOpenedUtc.ShouldBe(fakeNow);
    }

    [ Fact ]
    public async Task Handle_StampsLastOpenedUtc_OnReadyToPlay()
    {
        Guid     id      = Guid.CreateVersion7();
        DateTime fakeNow = new(year: 2026, month: 5, day: 28, hour: 10, minute: 0, second: 0, DateTimeKind.Utc);

        FakeTimeProvider clock = new(fakeNow);

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { DefaultInstallationId = id });

        GameInstallation row = new()
        {
            Id = id, Name = "Main", Path = @"C:\ZT",
            HasExe = true, HasIni = true,
            AddedUtc = DateTime.UtcNow
        };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(row);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(row.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(row, Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(), snapshots, clock);

        await handler.Handle(new BootCommand(), CancellationToken.None);

        await installations.Received(requiredNumberOfCalls: 1)
                           .UpdateAsync(Arg.Is<GameInstallation>(i => i.Id == id && i.LastOpenedUtc == fakeNow),
                                        Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_PromotesDefault_WhenDefaultIdNullAndRowsExist()
    {
        Guid             promotedId = Guid.CreateVersion7();
        GameInstallation promoted   = new() { Id = promotedId, Name = "Alpha", Path = @"C:\ZT", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        LauncherSettings            settingsRow   = new() { DefaultInstallationId = null };
        ILauncherSettingsRepository settings      = Substitute.For<ILauncherSettingsRepository>();
        IInstallationRepository     installations = Substitute.For<IInstallationRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(settingsRow);

        installations.FindDefaultPromotionCandidateAsync(Arg.Any<CancellationToken>())
                     .Returns(promoted);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(promoted.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(promoted, Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(), snapshots, TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.Value.Outcome.ShouldBe(BootOutcome.ReadyToPlay);

        await settings.Received(requiredNumberOfCalls: 1)
                      .UpdateAsync(Arg.Is<LauncherSettings>(s => s.DefaultInstallationId == promotedId),
                                   Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Handle_AutoLocates_CandidateFound_ReturnsNoGameInstallationFoundWithPath()
    {
        ILauncherSettingsRepository settings      = Substitute.For<ILauncherSettingsRepository>();
        IInstallationRepository     installations = Substitute.For<IInstallationRepository>();
        IInstallationLocator        locator       = Substitute.For<IInstallationLocator>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings());

        installations.FindDefaultPromotionCandidateAsync(Arg.Any<CancellationToken>())
                     .Returns((GameInstallation?)null);

        locator.LocateAsync(persistedLastKnownPath: null, Arg.Any<CancellationToken>())
               .Returns(new LocatedDirectory(Path: @"C:\Games\ZT", Trail: Array.Empty<LocationProbeAttempt>()));

        BootHandler handler = new(settings, installations,
                                  Substitute.For<IInstallationVerifier>(), locator,
                                  Substitute.For<IIniSnapshotService>(), TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.NoGameInstallationFound);
        result.Value.LocatedCandidatePath.ShouldBe(@"C:\Games\ZT");
    }

    [ Fact ]
    public async Task Handle_AutoLocates_NothingFound_ReturnsNoGameInstallationFound()
    {
        ILauncherSettingsRepository settings      = Substitute.For<ILauncherSettingsRepository>();
        IInstallationRepository     installations = Substitute.For<IInstallationRepository>();
        IInstallationLocator        locator       = Substitute.For<IInstallationLocator>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings());

        installations.FindDefaultPromotionCandidateAsync(Arg.Any<CancellationToken>())
                     .Returns((GameInstallation?)null);

        locator.LocateAsync(persistedLastKnownPath: null, Arg.Any<CancellationToken>())
               .Returns(new LocatedDirectory(Path: null, Trail: Array.Empty<LocationProbeAttempt>()));

        BootHandler handler = new(settings, installations,
                                  Substitute.For<IInstallationVerifier>(), locator,
                                  Substitute.For<IIniSnapshotService>(), TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.NoGameInstallationFound);
        result.Value.LocatedCandidatePath.ShouldBeNull();
    }

    [ Fact ]
    public async Task Handle_ReturnsOpenGameInstallation_WhenPreferenceIsNoInstallation()
    {
        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings { LauncherStartupPreference = LauncherStartupPreference.NoInstallation });

        BootHandler handler = new(settings,
                                  Substitute.For<IInstallationRepository>(),
                                  Substitute.For<IInstallationVerifier>(),
                                  Substitute.For<IInstallationLocator>(),
                                  Substitute.For<IIniSnapshotService>(),
                                  TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.OpenGameInstallation);
    }

    [ Fact ]
    public async Task Handle_FallsBackToDefault_WhenLastPlayedHasNoCandidate()
    {
        Guid id = Guid.CreateVersion7();

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    LauncherStartupPreference = LauncherStartupPreference.LastPlayedInstallation,
                    DefaultInstallationId     = id
                });

        GameInstallation noLastPlayed = new() { Id = Guid.CreateVersion7(), Name = "A", Path = @"C:\A", AddedUtc = DateTime.UtcNow, LastPlayedUtc = null };
        GameInstallation defaultRow   = new() { Id = id,                   Name = "B", Path = @"C:\B", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns(new[] { noLastPlayed });

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(defaultRow);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(defaultRow.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(defaultRow, Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(), snapshots, TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.ReadyToPlay);
        result.Value.ActiveInstallation!.Id.ShouldBe(id);
    }

    [ Fact ]
    public async Task Handle_FallsBackToDefault_WhenLastOpenedHasNoCandidate()
    {
        Guid id = Guid.CreateVersion7();

        ILauncherSettingsRepository settings = Substitute.For<ILauncherSettingsRepository>();

        settings.GetAsync(Arg.Any<CancellationToken>())
                .Returns(new LauncherSettings
                {
                    LauncherStartupPreference = LauncherStartupPreference.LastOpenedInstallation,
                    DefaultInstallationId     = id
                });

        GameInstallation noLastOpened = new() { Id = Guid.CreateVersion7(), Name = "A", Path = @"C:\A", AddedUtc = DateTime.UtcNow, LastOpenedUtc = null };
        GameInstallation defaultRow   = new() { Id = id,                   Name = "B", Path = @"C:\B", HasExe = true, HasIni = true, AddedUtc = DateTime.UtcNow };

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.GetAllAsync(Arg.Any<CancellationToken>())
                     .Returns(new[] { noLastOpened });

        installations.GetByIdAsync(id, Arg.Any<CancellationToken>())
                     .Returns(defaultRow);

        IInstallationVerifier verifier = Substitute.For<IInstallationVerifier>();

        verifier.VerifyAsync(defaultRow.Path, Arg.Any<CancellationToken>())
                .Returns(new VerificationResult(DirectoryExists: true, HasExe: true, HasIni: true));

        IIniSnapshotService snapshots = Substitute.For<IIniSnapshotService>();

        snapshots.SynchroniseAsync(defaultRow, Arg.Any<CancellationToken>())
                 .Returns(Result.Success);

        BootHandler handler = new(settings, installations, verifier,
                                  Substitute.For<IInstallationLocator>(), snapshots, TimeProvider.System);

        ErrorOr<BootResult> result = await handler.Handle(new BootCommand(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(BootOutcome.ReadyToPlay);
        result.Value.ActiveInstallation!.Id.ShouldBe(id);
    }
}
