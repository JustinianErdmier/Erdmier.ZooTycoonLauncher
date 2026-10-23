using System.IO.Abstractions;

using Erdmier.ZooTycoonNexus.Domain.IniKeys;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Erdmier.ZooTycoonNexus.Infrastructure.Tests.Integration.Persistence;

public sealed class IniSnapshotRepositoryTests : IDisposable
{
    private static readonly DateTime Captured = new(year: 2026, month: 9, day: 24, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"zoonexus-snapshots-{Guid.NewGuid():N}");

    private readonly Guid _installationId = Guid.CreateVersion7();

    private readonly IniSnapshotRepository _repository;

    public IniSnapshotRepositoryTests()
    {
        Directory.CreateDirectory(_directory);

        IAppStorageLocations locations = Substitute.For<IAppStorageLocations>();

        locations.InstallationDatabasePath(Arg.Any<Guid>())
                 .Returns(call => Path.Combine(_directory, $"{call.Arg<Guid>()}.db"));

        _repository = new IniSnapshotRepository(new InstallationDbContextFactory(locations, new FileSystem()));
    }

    private string DatabasePath => Path.Combine(_directory, $"{_installationId}.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        Directory.Delete(_directory, recursive: true);
    }

    [ Fact ]
    public async Task FirstImport_RoundTripsTheCurrentSnapshot()
    {
        await ImportAsync();

        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        IniSnapshot? current = await transaction.GetCurrentAsync(CancellationToken.None);

        current.ShouldNotBeNull();
        current.StructureBlob.ShouldBe(expected: "[user]\r\nscreenwidth=800\r\n");
        current.Values.Single().Value.ShouldBe(expected: "800");
    }

    [ Fact ]
    public async Task ArchiveCurrent_CopiesBlobRowsAndSourcesIntoANewHistoricalSnapshot()
    {
        await ImportAsync();

        await using (IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None))
        {
            await transaction.ArchiveCurrentAsync(IniSnapshotTrigger.Manual, Captured.AddHours(1), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        await using InstallationDbContext context = OpenContext();

        List<IniSnapshot> snapshots = await context.Snapshots.Include(snapshot => snapshot.Values).ToListAsync();

        IniSnapshot historical = snapshots.Single(snapshot => snapshot.Kind == IniSnapshotKind.Historical);

        historical.Trigger.ShouldBe(IniSnapshotTrigger.Manual);
        historical.CapturedUtc.ShouldBe(Captured.AddHours(1));
        historical.StructureBlob.ShouldBe(expected: "[user]\r\nscreenwidth=800\r\n");
        historical.Values.Single().Source.ShouldBe(IniValueSource.OriginalImport);
        snapshots.Count.ShouldBe(expected: 3);
    }

    [ Fact ]
    public async Task UpdateCurrent_UpdatesInsertsAndDeletesRowsInPlace()
    {
        await ImportAsync(("user", "lastfile", @"C:\a.zoo"));

        await using (IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None))
        {
            IReadOnlyList<IniValueChange> changes =
            [
                new(new IniKeyId(Section: "USER", Key: "SCREENWIDTH"), Value: "1024", IniValueSource.LauncherGui),
                new(new IniKeyId(Section: "user", Key: "DrawRate"), Value: "30", IniValueSource.LauncherGui),
                new(new IniKeyId(Section: "user", Key: "lastfile"), Value: null, IniValueSource.Manual)
            ];

            await transaction.UpdateCurrentAsync(structureBlob: "new text", changes, Captured.AddDays(1), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        await using InstallationDbContext context = OpenContext();

        IniSnapshot current = (await context.Snapshots.Include(snapshot => snapshot.Values).ToListAsync()).Single(snapshot => snapshot.Kind == IniSnapshotKind.Current);

        current.StructureBlob.ShouldBe(expected: "new text");
        current.CapturedUtc.ShouldBe(Captured.AddDays(1));
        current.Values.Select(value => $"{value.Key}={value.Value}:{value.Source}").Order().ShouldBe(["DrawRate=30:LauncherGui", "screenwidth=1024:LauncherGui"]);
        current.Values.Single(value => value.Key == "DrawRate").ValueKind.ShouldBe(IniValueKind.Int);
    }

    [ Fact ]
    public async Task Dispose_WithoutCommit_RollsBack()
    {
        await using (IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None))
        {
            await transaction.AddAsync(Snapshot(IniSnapshotKind.Current), CancellationToken.None);
        }

        await using IIniSnapshotTransaction check = await _repository.BeginAsync(_installationId, CancellationToken.None);

        (await check.GetCurrentAsync(CancellationToken.None)).ShouldBeNull();
    }

    [ Fact ]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        await transaction.DisposeAsync();
        await transaction.DisposeAsync();
    }

    [ Fact ]
    public async Task PartialUniqueIndexes_RejectASecondCurrent()
    {
        await ImportAsync();

        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        await Should.ThrowAsync<DbUpdateException>(() => transaction.AddAsync(Snapshot(IniSnapshotKind.Current), CancellationToken.None));
    }

    [ Fact ]
    public async Task PartialUniqueIndexes_RejectASecondOriginal()
    {
        await ImportAsync();

        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        await Should.ThrowAsync<DbUpdateException>(() => transaction.AddAsync(Snapshot(IniSnapshotKind.Original), CancellationToken.None));
    }

    [ Fact ]
    public async Task Migration_UpgradesADatabaseCreatedByTheInitialSchema()
    {
        await using (InstallationDbContext context = OpenContext())
        {
            await context.GetService<IMigrator>().MigrateAsync(targetMigration: "20260528001444_InitialInstallationSchema");
        }

        await using (IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None))
        {
            await transaction.CommitAsync(CancellationToken.None);
        }

        await using InstallationDbContext verify = OpenContext();

        (await IndexExistsAsync(verify, indexName: "IX_Snapshots_Kind_Original")).ShouldBeTrue();
        (await IndexExistsAsync(verify, indexName: "IX_Snapshots_Kind_Current")).ShouldBeTrue();
        (await IndexExistsAsync(verify, indexName: "IX_Snapshots_Kind_CapturedUtc")).ShouldBeTrue();
    }

    [ Fact ]
    public async Task ListHistorical_ReturnsNewestFirstWithValues()
    {
        await ImportAsync();
        await ArchiveAsync(Captured.AddHours(2), Captured.AddHours(1), Captured.AddHours(3));

        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        IReadOnlyList<IniSnapshot> historical = await transaction.ListHistoricalAsync(CancellationToken.None);

        historical.Select(snapshot => snapshot.CapturedUtc).ShouldBe([Captured.AddHours(3), Captured.AddHours(2), Captured.AddHours(1)]);
        historical.ShouldAllBe(snapshot => snapshot.Kind == IniSnapshotKind.Historical);
        historical.ShouldAllBe(snapshot => snapshot.Values.Count == 1);
    }

    [ Fact ]
    public async Task ListHistorical_EqualTimestamps_NewestArchiveFirst()
    {
        await ImportAsync();

        // Two historical snapshots, inserted directly, share one capture instant (as a Save after an external edit would archive a Manual snapshot and then a
        // LauncherGui one). The ids are explicit, with the Manual one larger, so only the trigger can put the LauncherGui archive first.
        await using (IIniSnapshotTransaction archiving = await _repository.BeginAsync(_installationId, CancellationToken.None))
        {
            await archiving.AddAsync(Snapshot(IniSnapshotKind.Historical, IniSnapshotTrigger.Manual, Guid.Parse(input: "00000000-0000-0000-0000-000000000002")), CancellationToken.None);
            await archiving.AddAsync(Snapshot(IniSnapshotKind.Historical, IniSnapshotTrigger.LauncherGui, Guid.Parse(input: "00000000-0000-0000-0000-000000000001")), CancellationToken.None);
            await archiving.CommitAsync(CancellationToken.None);
        }

        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        IReadOnlyList<IniSnapshot> historical = await transaction.ListHistoricalAsync(CancellationToken.None);

        historical.Select(snapshot => snapshot.Trigger).ShouldBe([IniSnapshotTrigger.LauncherGui, IniSnapshotTrigger.Manual]);
    }

    [ Fact ]
    public async Task GetOriginal_ReturnsTheFirstImportWithValues()
    {
        await ImportAsync();

        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        IniSnapshot? original = await transaction.GetOriginalAsync(CancellationToken.None);

        original.ShouldNotBeNull();
        original.Kind.ShouldBe(IniSnapshotKind.Original);
        original.Values.Single().Value.ShouldBe(expected: "800");
    }

    [ Fact ]
    public async Task GetById_ReturnsAHistoricalSnapshot()
    {
        await ImportAsync();
        await ArchiveAsync(Captured.AddHours(1));

        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        IniSnapshot historical = (await transaction.ListHistoricalAsync(CancellationToken.None)).Single();

        IniSnapshot? found = await transaction.GetByIdAsync(historical.Id, CancellationToken.None);

        found.ShouldNotBeNull();
        found.Id.ShouldBe(historical.Id);
        found.Values.Single().Value.ShouldBe(expected: "800");
    }

    [ Fact ]
    public async Task GetById_ReturnsTheOriginalSnapshot()
    {
        await ImportAsync();

        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        IniSnapshot original = (await transaction.GetOriginalAsync(CancellationToken.None))!;

        IniSnapshot? found = await transaction.GetByIdAsync(original.Id, CancellationToken.None);

        found.ShouldNotBeNull();
        found.Id.ShouldBe(original.Id);
        found.Kind.ShouldBe(IniSnapshotKind.Original);
        found.Values.Single().Value.ShouldBe(expected: "800");
    }

    [ Fact ]
    public async Task GetById_CurrentOrUnknownId_ReturnsNull()
    {
        await ImportAsync();

        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        IniSnapshot current = (await transaction.GetCurrentAsync(CancellationToken.None))!;

        (await transaction.GetByIdAsync(current.Id, CancellationToken.None)).ShouldBeNull();
        (await transaction.GetByIdAsync(Guid.NewGuid(), CancellationToken.None)).ShouldBeNull();
    }

    [ Fact ]
    public async Task CountHistorical_CountsOnlyHistorical()
    {
        await ImportAsync();
        await ArchiveAsync(Captured.AddHours(1), Captured.AddHours(2));

        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        (await transaction.CountHistoricalAsync(CancellationToken.None)).ShouldBe(expected: 2);
    }

    [ Fact ]
    public async Task RepositoryCountHistorical_MissingDatabase_ReturnsZeroAndCreatesNoFile()
    {
        Guid missingId = Guid.CreateVersion7();

        (await _repository.CountHistoricalAsync(missingId, CancellationToken.None)).ShouldBe(expected: 0);

        File.Exists(Path.Combine(_directory, $"{missingId}.db")).ShouldBeFalse();
    }

    [ Fact ]
    public async Task RepositoryCountHistorical_ExistingDatabase_Counts()
    {
        await ImportAsync();
        await ArchiveAsync(Captured.AddHours(1), Captured.AddHours(2), Captured.AddHours(3));

        (await _repository.CountHistoricalAsync(_installationId, CancellationToken.None)).ShouldBe(expected: 3);
    }

    private async Task ArchiveAsync(params DateTime[] capturedTimes)
    {
        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        foreach (DateTime captured in capturedTimes)
        {
            await transaction.ArchiveCurrentAsync(IniSnapshotTrigger.LauncherGui, captured, CancellationToken.None);
        }

        await transaction.CommitAsync(CancellationToken.None);
    }

    private async Task ImportAsync(params (string Section, string Key, string Value)[] extraValues)
    {
        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        await transaction.AddAsync(Snapshot(IniSnapshotKind.Original, extraValues), CancellationToken.None);
        await transaction.AddAsync(Snapshot(IniSnapshotKind.Current, extraValues), CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
    }

    private static IniSnapshot Snapshot(IniSnapshotKind kind, params (string Section, string Key, string Value)[] extraValues)
        => Snapshot(kind, IniSnapshotTrigger.OriginalImport, Guid.CreateVersion7(), extraValues);

    private static IniSnapshot Snapshot(IniSnapshotKind kind, IniSnapshotTrigger trigger, Guid id, params (string Section, string Key, string Value)[] extraValues)
    {
        List<IniValue> values =
        [
            new() { SnapshotId = id, Section = "user", Key = "screenwidth", Value = "800", ValueKind = IniValueKind.Int, Source = IniValueSource.OriginalImport }
        ];

        values.AddRange(extraValues.Select(extra => new IniValue
        {
            SnapshotId = id,
            Section    = extra.Section,
            Key        = extra.Key,
            Value      = extra.Value,
            ValueKind  = IniValueKind.NullableStr,
            Source     = IniValueSource.OriginalImport
        }));

        return new IniSnapshot
        {
            Id            = id,
            Kind          = kind,
            Trigger       = trigger,
            CapturedUtc   = Captured,
            StructureBlob = "[user]\r\nscreenwidth=800\r\n",
            Values        = values
        };
    }

    private InstallationDbContext OpenContext()
        => new(new DbContextOptionsBuilder<InstallationDbContext>().UseSqlite($"Data Source={DatabasePath}")
                                                                    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                                                                    .Options);

    private static async Task<bool> IndexExistsAsync(InstallationDbContext context, string indexName)
    {
        // EF owns this connection: open it if needed, never dispose it.
        DbConnection connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using DbCommand command = connection.CreateCommand();

        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'index' AND name = $name";

        command.AddParameter(name: "$name", indexName);

        return await command.ExecuteScalarAsync() is not null;
    }
}
