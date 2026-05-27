namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher;

/// <summary>
/// EF Core context for <c>Launcher.db</c> — launcher-global settings and the installation registry.
/// </summary>
[UsedImplicitly]
public sealed class LauncherDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    /// <summary>Initialises a new instance with the supplied options.</summary>
    /// <param name="options">The context options (connection string supplied via DI).</param>
    public LauncherDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<LauncherDbContext> options) : base(options) { }

    /// <summary>The single-row settings table.</summary>
    public Microsoft.EntityFrameworkCore.DbSet<LauncherSettings> LauncherSettings => Set<LauncherSettings>();

    /// <summary>The installation registry.</summary>
    public Microsoft.EntityFrameworkCore.DbSet<GameInstallation> GameInstallations => Set<GameInstallation>();

    /// <inheritdoc />
    protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LauncherDbContext).Assembly);
    }

    /// <summary>
    /// Adds the <see cref="LauncherDbContext" /> to the service collection wired to a file-backed SQLite database.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="databasePath">The fully qualified database path; usually <see cref="IAppStorageLocations.LauncherDatabasePath" />.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddLauncherDbContext(IServiceCollection services, string databasePath)
    {
        services.AddDbContext<LauncherDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));
        return services;
    }
}
