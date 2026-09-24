namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation;

/// <summary><see cref="IIniSnapshotRepository" /> over the per-installation <see cref="InstallationDbContext" />.</summary>
public sealed class IniSnapshotRepository : IIniSnapshotRepository
{
    private readonly InstallationDbContextFactory _factory;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="factory">The per-installation context factory.</param>
    public IniSnapshotRepository(InstallationDbContextFactory factory) => _factory = factory;

    /// <inheritdoc />
    public async Task<IIniSnapshotTransaction> BeginAsync(Guid installationId, CancellationToken cancellationToken)
    {
        InstallationDbContext context = await _factory.OpenAsync(installationId, cancellationToken);

        try
        {
            IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            return new IniSnapshotTransaction(context, transaction);
        }
        catch
        {
            await context.DisposeAsync();

            throw;
        }
    }
}
