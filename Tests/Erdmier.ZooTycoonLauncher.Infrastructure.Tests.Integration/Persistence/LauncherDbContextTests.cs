using System.Data.Common;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Persistence;

public sealed class LauncherDbContextTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"zoolauncher-test-{Guid.NewGuid()}.db");

    public void Dispose()
    {
        // Clear the SQLite connection pool so Windows releases the file lock before deletion.
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    [ Fact ]
    public async Task Migrate_OnFreshDatabase_CreatesLauncherSettingsAndGameInstallationsTables()
    {
        await using LauncherDbContext context = BuildContext();

        await context.Database.MigrateAsync();

        bool launcherSettingsExists  = await TableExistsAsync(context, tableName: "LauncherSettings");
        bool gameInstallationsExists = await TableExistsAsync(context, tableName: "GameInstallations");

        launcherSettingsExists.ShouldBeTrue();
        gameInstallationsExists.ShouldBeTrue();
    }

    [ Fact ]
    public async Task LauncherSettings_RejectsInsertWithIdNotEqualToOne()
    {
        await using LauncherDbContext context = BuildContext();
        await context.Database.MigrateAsync();

        LauncherSettings rogue = new()
        {
            Id = 2
        };

        context.LauncherSettings.Add(rogue);

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [ Fact ]
    public async Task GameInstallations_Name_IsCaseInsensitiveUnique()
    {
        await using LauncherDbContext context = BuildContext();
        await context.Database.MigrateAsync();

        Guid sharedPathRoot = Guid.NewGuid();

        context.GameInstallations.Add(new GameInstallation
        {
            Id       = Guid.CreateVersion7(),
            Name     = "Main",
            Path     = $"C:/Games/A-{sharedPathRoot}",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        context.GameInstallations.Add(new GameInstallation
        {
            Id       = Guid.CreateVersion7(),
            Name     = "main", // same name, different casing
            Path     = $"C:/Games/B-{sharedPathRoot}",
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        });

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    private LauncherDbContext BuildContext()
    {
        DbContextOptions<LauncherDbContext> options = new DbContextOptionsBuilder<LauncherDbContext>()
                                                      .UseSqlite($"Data Source={_databasePath}")
                                                      .Options;

        return new LauncherDbContext(options);
    }

    private static async Task<bool> TableExistsAsync(LauncherDbContext context, string tableName)
    {
        await using DbConnection connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=$name";
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value         = tableName;
        command.Parameters.Add(parameter);
        object? result = await command.ExecuteScalarAsync();

        return result is not null;
    }
}
