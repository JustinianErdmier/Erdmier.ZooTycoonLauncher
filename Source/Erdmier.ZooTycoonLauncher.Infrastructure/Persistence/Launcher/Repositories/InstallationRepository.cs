namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Repositories;

/// <summary>EF Core implementation of <see cref="IInstallationRepository" /> targeting <c>Launcher.db</c>.</summary>
[ UsedImplicitly ]
public sealed class InstallationRepository : IInstallationRepository
{
    private readonly LauncherDbContext _context;

    /// <summary>Initialises a new instance with the supplied context.</summary>
    /// <param name="context">The launcher database context.</param>
    public InstallationRepository(LauncherDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameInstallation>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<GameInstallation> installations = await _context.GameInstallations
                                                             .OrderBy(i => i.Name.ToLower())
                                                             .ToListAsync(cancellationToken);

        return installations;
    }
}
