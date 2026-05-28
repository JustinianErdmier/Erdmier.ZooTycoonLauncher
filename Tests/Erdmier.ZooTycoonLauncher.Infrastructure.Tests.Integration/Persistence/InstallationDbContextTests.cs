namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Persistence;

public sealed class InstallationDbContextTests : IDisposable
{
    private readonly string _databasePath;

    public InstallationDbContextTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"zoolauncher-install-test-{Guid.NewGuid()}.db");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    [ Fact ]
    public async Task Migrate_OnFreshDatabase_CreatesSnapshotsAndIniValuesTables()
    {
        await using InstallationDbContext context = BuildContext();

        await context.Database.MigrateAsync();

        bool snapshotsExists = await TableExistsAsync(context, "Snapshots");
        bool valuesExists = await TableExistsAsync(context, "IniValues");

        snapshotsExists.ShouldBeTrue();
        valuesExists.ShouldBeTrue();
    }

    [ Fact ]
    public async Task IniValues_SectionAndKey_AreCaseInsensitiveUnique()
    {
        await using InstallationDbContext context = BuildContext();
        await context.Database.MigrateAsync();

        Guid snapshotId = Guid.CreateVersion7();

        IniSnapshot snapshot = new()
        {
            Id = snapshotId,
            Kind = IniSnapshotKind.Original,
            Trigger = IniSnapshotTrigger.OriginalImport,
            CapturedUtc = DateTime.UtcNow,
            StructureBlob = "[user]\n",
        };

        context.Snapshots.Add(snapshot);
        context.IniValues.Add(new IniValue
        {
            SnapshotId = snapshotId,
            Section = "user",
            Key = "ShowToolTips",
            Value = "1",
            ValueKind = IniValueKind.Bool,
            Source = IniValueSource.OriginalImport,
        });
        await context.SaveChangesAsync();

        context.IniValues.Add(new IniValue
        {
            SnapshotId = snapshotId,
            Section = "USER",
            Key = "showtooltips",
            Value = "0",
            ValueKind = IniValueKind.Bool,
            Source = IniValueSource.OriginalImport,
        });

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    private InstallationDbContext BuildContext()
    {
        DbContextOptions<InstallationDbContext> options = new DbContextOptionsBuilder<InstallationDbContext>()
                                                         .UseSqlite($"Data Source={_databasePath}")
                                                         .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                                                         .Options;
        return new InstallationDbContext(options);
    }

    private static async Task<bool> TableExistsAsync(InstallationDbContext context, string tableName)
    {
        await using System.Data.Common.DbConnection connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using System.Data.Common.DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=$name";
        System.Data.Common.DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);
        object? result = await command.ExecuteScalarAsync();
        return result is not null;
    }
}
