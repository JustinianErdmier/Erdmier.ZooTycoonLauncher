namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Opens a transaction over an installation's snapshot database (<c>{installationId}.db</c>) so the SDD §8.2 ordering can be expressed in the Application layer.</summary>
public interface IIniSnapshotRepository
{
    /// <summary>Opens (migrating if needed) the installation's database and begins a transaction.</summary>
    /// <param name="installationId">The installation's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The open transaction. Dispose it; disposing without <see cref="IIniSnapshotTransaction.CommitAsync" /> rolls back.</returns>
    Task<IIniSnapshotTransaction> BeginAsync(Guid installationId, CancellationToken cancellationToken);
}
