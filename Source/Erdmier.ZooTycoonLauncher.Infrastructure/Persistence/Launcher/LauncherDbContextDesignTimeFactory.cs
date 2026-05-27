namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher;

/// <summary>
/// Design-time factory used by the EF Core CLI to construct <see cref="LauncherDbContext" /> outside the
/// running application — e.g. when running <c>dotnet ef migrations add</c>.
/// </summary>
/// <remarks>
/// Resolves the connection string from the <c>ZOOLAUNCHER_DESIGNTIME_DB</c> environment variable when set,
/// otherwise falls back to a fixed temp path. Never touches the user's real <c>Launcher.db</c>.
/// </remarks>
[UsedImplicitly]
public sealed class LauncherDbContextDesignTimeFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<LauncherDbContext>
{
    /// <inheritdoc />
    public LauncherDbContext CreateDbContext(string[] args)
    {
        string databasePath = Environment.GetEnvironmentVariable("ZOOLAUNCHER_DESIGNTIME_DB")
            ?? Path.Combine(Path.GetTempPath(), "ZooTycoonLauncher.DesignTime.db");

        Microsoft.EntityFrameworkCore.DbContextOptions<LauncherDbContext> options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<LauncherDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new LauncherDbContext(options);
    }
}
