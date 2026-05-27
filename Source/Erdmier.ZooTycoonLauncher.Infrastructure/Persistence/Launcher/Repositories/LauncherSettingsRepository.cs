namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Repositories;

/// <summary>EF Core implementation of <see cref="ILauncherSettingsRepository" /> targeting <c>Launcher.db</c>.</summary>
public sealed class LauncherSettingsRepository : ILauncherSettingsRepository
{
    private readonly LauncherDbContext _context;

    /// <summary>Initialises a new instance with the supplied context.</summary>
    /// <param name="context">The launcher database context.</param>
    public LauncherSettingsRepository(LauncherDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<LauncherSettings> GetAsync(CancellationToken cancellationToken)
    {
        LauncherSettings? settings = await _context.LauncherSettings.FirstOrDefaultAsync(cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new LauncherSettings();

        _context.LauncherSettings.Add(settings);

        await _context.SaveChangesAsync(cancellationToken);

        return settings;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(LauncherSettings settings, CancellationToken cancellationToken)
    {
        _context.LauncherSettings.Update(settings);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
