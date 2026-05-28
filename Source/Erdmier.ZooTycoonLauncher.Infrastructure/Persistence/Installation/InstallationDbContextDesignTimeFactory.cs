namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation;

/// <summary>
/// Design-time factory used by the EF Core CLI to construct <see cref="InstallationDbContext"/> outside the running
/// application (e.g. <c>dotnet ef migrations add</c>). Targets a fixed temp file; never touches a user-owned DB.
/// </summary>
public sealed class InstallationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<InstallationDbContext>
{
    /// <inheritdoc />
    public InstallationDbContext CreateDbContext(string[] args)
    {
        string databasePath = Environment.GetEnvironmentVariable("ZOOLAUNCHER_INSTALLATION_DESIGNTIME_DB")
                              ?? Path.Combine(Path.GetTempPath(), "ZooTycoonLauncher.Installation.DesignTime.db");

        DbContextOptions<InstallationDbContext> options = new DbContextOptionsBuilder<InstallationDbContext>()
                                                         .UseSqlite($"Data Source={databasePath}")
                                                         .Options;

        return new InstallationDbContext(options);
    }
}
