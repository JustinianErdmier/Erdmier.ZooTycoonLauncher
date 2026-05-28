namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation;

/// <summary>
///     File-system-backed implementation of <see cref="IInstallationDbContextFactory" />. Creates (when absent) or opens the per-installation database under
///     <c>{DataRoot}\{installationId}.db</c>, runs migrations, and wraps the context in a disposable handle.
/// </summary>
public sealed class InstallationDbContextFactory : IInstallationDbContextFactory
{
    private readonly IFileSystem _fileSystem;

    private readonly IAppStorageLocations _locations;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="locations">Path locations.</param>
    /// <param name="fileSystem">File-system abstraction (only used for delete; EF Core owns its own IO for read/write).</param>
    public InstallationDbContextFactory(IAppStorageLocations locations, IFileSystem fileSystem)
    {
        _locations  = locations;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public async Task<IInstallationDbContextHandle> CreateAsync(Guid installationId, CancellationToken cancellationToken)
    {
        string databasePath = _locations.InstallationDatabasePath(installationId);

        DbContextOptions<InstallationDbContext> options = new DbContextOptionsBuilder<InstallationDbContext>()
                                                          .UseSqlite($"Data Source={databasePath}")
                                                          .Options;

        InstallationDbContext context = new(options);

        await context.Database.MigrateAsync(cancellationToken);

        return new Handle(context);
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid installationId, CancellationToken cancellationToken)
    {
        // cancellationToken is not observed — SqliteConnection.ClearAllPools() and IFileSystem.File.Delete are synchronous.
        SqliteConnection.ClearAllPools();

        string databasePath = _locations.InstallationDatabasePath(installationId);

        if (_fileSystem.File.Exists(databasePath))
        {
            _fileSystem.File.Delete(databasePath);
        }

        foreach (string suffix in new[] { "-wal", "-shm", "-journal" })
        {
            string sidecar = databasePath + suffix;

            if (_fileSystem.File.Exists(sidecar))
            {
                _fileSystem.File.Delete(sidecar);
            }
        }

        return Task.CompletedTask;
    }

    private sealed class Handle : IInstallationDbContextHandle
    {
        private readonly InstallationDbContext _context;

        public Handle(InstallationDbContext context) => _context = context;

        public ValueTask DisposeAsync() => _context.DisposeAsync();
    }
}
