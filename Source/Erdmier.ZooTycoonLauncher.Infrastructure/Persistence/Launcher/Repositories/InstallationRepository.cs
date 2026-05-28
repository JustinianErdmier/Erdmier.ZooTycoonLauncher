namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Repositories;

/// <summary>EF Core implementation of <see cref="IInstallationRepository" /> targeting <c>Launcher.db</c>.</summary>
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

    /// <inheritdoc />
    public Task<GameInstallation?> GetByIdAsync(Guid installationId, CancellationToken cancellationToken)
        => _context.GameInstallations.FirstOrDefaultAsync(i => i.Id == installationId, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(GameInstallation installation, CancellationToken cancellationToken)
    {
        _context.GameInstallations.Add(installation);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(GameInstallation installation, CancellationToken cancellationToken)
    {
        _context.GameInstallations.Update(installation);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid installationId, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _context.GameInstallations.FirstOrDefaultAsync(i => i.Id == installationId, cancellationToken);

        if (row is null)
        {
            return;
        }

        _context.GameInstallations.Remove(row);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken)
        => _context.GameInstallations
                   .Where(i => excludeId == null || i.Id != excludeId)
                   .AnyAsync(i => i.Name == name, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsByPathAsync(string path, Guid? excludeId, CancellationToken cancellationToken)
        => _context.GameInstallations
                   .Where(i => excludeId == null || i.Id != excludeId)
                   .AnyAsync(i => i.Path == path, cancellationToken);

    /// <inheritdoc />
    public Task<GameInstallation?> FindDefaultPromotionCandidateAsync(CancellationToken cancellationToken)
        => _context.GameInstallations
                   .OrderBy(i => i.Name.ToLower())
                   .FirstOrDefaultAsync(cancellationToken);
}
