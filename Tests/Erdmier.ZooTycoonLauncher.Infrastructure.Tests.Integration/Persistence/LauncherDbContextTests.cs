using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

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

    [ Fact ]
    public async Task Migration_DropsTheThemeColumn_AndKeepsSettings()
    {
        Guid defaultInstallationId = Guid.CreateVersion7();

        await using (LauncherDbContext upgrade = BuildContext())
        {
            await upgrade.GetService<IMigrator>().MigrateAsync(targetMigration: "20260527123543_InitialLauncherSchema");

            await upgrade.Database.ExecuteSqlAsync($"INSERT INTO \"LauncherSettings\" (\"Id\", \"LauncherStartupPreference\", \"CloseAfterGameLaunch\", \"DefaultInstallationId\", \"Theme\") VALUES (1, 'LastPlayedInstallation', 1, {defaultInstallationId}, 'Dark')");
        }

        await using LauncherDbContext context = BuildContext();

        await context.Database.MigrateAsync();

        (await ColumnExistsAsync(context, tableName: "LauncherSettings", columnName: "Theme")).ShouldBeFalse();

        LauncherSettings settings = await new LauncherSettingsRepository(context).GetAsync(CancellationToken.None);

        settings.LauncherStartupPreference.ShouldBe(LauncherStartupPreference.LastPlayedInstallation);
        settings.CloseAfterGameLaunch.ShouldBeTrue();
        settings.DefaultInstallationId.ShouldBe(defaultInstallationId);
    }

    [ Fact ]
    public async Task Migration_Down_RestoresTheThemeColumnWithItsOldDefault()
    {
        await using LauncherDbContext context = BuildContext();

        await context.Database.MigrateAsync();

        await context.GetService<IMigrator>().MigrateAsync(targetMigration: "20260527123543_InitialLauncherSchema");

        (await ColumnExistsAsync(context, tableName: "LauncherSettings", columnName: "Theme")).ShouldBeTrue();

        (await ColumnDefaultAsync(context, tableName: "LauncherSettings", columnName: "Theme")).ShouldBe(expected: "'System'");
    }

    [ Fact ]
    public async Task Migration_AddsIniEditorStyleWithFriendlyDefault()
    {
        await using (LauncherDbContext upgrade = BuildContext())
        {
            await upgrade.GetService<IMigrator>().MigrateAsync(targetMigration: "20261022135110_RemoveLauncherTheme");

            await upgrade.Database.ExecuteSqlAsync($"INSERT INTO \"LauncherSettings\" (\"Id\", \"LauncherStartupPreference\", \"CloseAfterGameLaunch\") VALUES (1, 'NoInstallation', 0)");
        }

        await using LauncherDbContext context = BuildContext();

        await context.Database.MigrateAsync();

        (await ColumnDefaultAsync(context, tableName: "LauncherSettings", columnName: "IniEditorStyle")).ShouldBe(expected: "'Friendly'");

        LauncherSettings settings = await new LauncherSettingsRepository(context).GetAsync(CancellationToken.None);

        settings.IniEditorStyle.ShouldBe(IniEditorStyle.Friendly);
        settings.LauncherStartupPreference.ShouldBe(LauncherStartupPreference.NoInstallation);
    }

    [ Fact ]
    public async Task Migration_Down_RemovesIniEditorStyle()
    {
        await using LauncherDbContext context = BuildContext();

        await context.Database.MigrateAsync();

        (await ColumnExistsAsync(context, tableName: "LauncherSettings", columnName: "IniEditorStyle")).ShouldBeTrue();

        await context.GetService<IMigrator>().MigrateAsync(targetMigration: "20261022135110_RemoveLauncherTheme");

        (await ColumnExistsAsync(context, tableName: "LauncherSettings", columnName: "IniEditorStyle")).ShouldBeFalse();
    }

    private LauncherDbContext BuildContext()
    {
        DbContextOptions<LauncherDbContext> options = new DbContextOptionsBuilder<LauncherDbContext>()
                                                      .UseSqlite($"Data Source={_databasePath}")
                                                      .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                                                      .Options;

        return new LauncherDbContext(options);
    }

    private static async Task<bool> TableExistsAsync(LauncherDbContext context, string tableName)
    {
        // EF owns this connection: open it if needed, never dispose it.
        DbConnection connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=$name";
        command.AddParameter(name: "$name", tableName);
        object? result = await command.ExecuteScalarAsync();

        return result is not null;
    }

    private static async Task<bool> ColumnExistsAsync(LauncherDbContext context, string tableName, string columnName)
    {
        // EF owns this connection: open it if needed, never dispose it.
        DbConnection connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info($table) WHERE name = $column";

        command.AddParameter(name: "$table", tableName);
        command.AddParameter(name: "$column", columnName);

        object? result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }

    private static async Task<string?> ColumnDefaultAsync(LauncherDbContext context, string tableName, string columnName)
    {
        // EF owns this connection: open it if needed, never dispose it.
        DbConnection connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using DbCommand command = connection.CreateCommand();

        command.CommandText = "SELECT dflt_value FROM pragma_table_info($table) WHERE name = $column";

        command.AddParameter(name: "$table", tableName);
        command.AddParameter(name: "$column", columnName);

        return await command.ExecuteScalarAsync() as string;
    }
}
