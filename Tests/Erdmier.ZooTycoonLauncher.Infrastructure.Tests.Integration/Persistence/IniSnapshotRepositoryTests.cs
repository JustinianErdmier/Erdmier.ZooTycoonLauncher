using System.Data.Common;
using System.IO.Abstractions;

using Erdmier.ZooTycoonLauncher.Domain.IniKeys;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Persistence;

public sealed class IniSnapshotRepositoryTests : IDisposable
{
    private static readonly DateTime Captured = new(year: 2026, month: 9, day: 24, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"zoolauncher-snapshots-{Guid.NewGuid():N}");

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

    private async Task ImportAsync(params (string Section, string Key, string Value)[] extraValues)
    {
        await using IIniSnapshotTransaction transaction = await _repository.BeginAsync(_installationId, CancellationToken.None);

        await transaction.AddAsync(Snapshot(IniSnapshotKind.Original, extraValues), CancellationToken.None);
        await transaction.AddAsync(Snapshot(IniSnapshotKind.Current, extraValues), CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
    }

    private static IniSnapshot Snapshot(IniSnapshotKind kind, params (string Section, string Key, string Value)[] extraValues)
    {
        Guid id = Guid.CreateVersion7();

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
            Trigger       = IniSnapshotTrigger.OriginalImport,
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
        await using DbConnection connection = context.Database.GetDbConnection();

        await connection.OpenAsync();

        await using DbCommand command = connection.CreateCommand();

        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'index' AND name = $name";

        DbParameter parameter = command.CreateParameter();

        parameter.ParameterName = "$name";
        parameter.Value         = indexName;

        command.Parameters.Add(parameter);

        return await command.ExecuteScalarAsync() is not null;
    }
}
