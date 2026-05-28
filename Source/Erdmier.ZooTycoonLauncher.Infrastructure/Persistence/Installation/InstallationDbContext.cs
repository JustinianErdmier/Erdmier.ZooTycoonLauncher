namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation;

/// <summary>
/// EF Core context for a per-installation database (<c>{installationId}.db</c>) — owns the <c>Snapshots</c> and
/// <c>IniValues</c> tables.
/// </summary>
public sealed class InstallationDbContext : DbContext
{
    /// <summary>Initialises a new instance with the supplied options.</summary>
    /// <param name="options">The context options (connection string supplied via the factory).</param>
    public InstallationDbContext(DbContextOptions<InstallationDbContext> options) : base(options) { }

    /// <summary>The per-installation snapshot table.</summary>
    public DbSet<IniSnapshot> Snapshots => Set<IniSnapshot>();

    /// <summary>The flattened EAV value table; rows belong to exactly one <see cref="IniSnapshot"/>.</summary>
    public DbSet<IniValue> IniValues => Set<IniValue>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(
               typeof(InstallationDbContext).Assembly,
               static t => t.Namespace?.StartsWith(
                               "Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation",
                               StringComparison.Ordinal)
                           ?? false);
}
