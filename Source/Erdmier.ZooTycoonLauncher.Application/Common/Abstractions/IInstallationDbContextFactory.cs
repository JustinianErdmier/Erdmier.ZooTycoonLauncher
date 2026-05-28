namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Resolves a per-installation EF Core context targeting <c>{installationId}.db</c>, creating the database file and applying migrations on first use.</summary>
/// <remarks>
///     The factory is the Application-layer seam over <c>Microsoft.EntityFrameworkCore.DbContext</c>; consumers receive an opaque <see cref="IAsyncDisposable" />-shaped handle
///     so the EF type does not leak into Application code.
/// </remarks>
public interface IInstallationDbContextFactory
{
    /// <summary>Creates (when absent) or opens the per-installation database for <paramref name="installationId" />, runs migrations, and returns a handle wrapping the open context.</summary>
    /// <param name="installationId">The installation's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An opened handle. Dispose to release the context and its connection.</returns>
    Task<IInstallationDbContextHandle> CreateAsync(Guid installationId, CancellationToken cancellationToken);

    /// <summary>Deletes the per-installation database file (and any sidecar journal) for <paramref name="installationId" />. Safe to call when the file is absent.</summary>
    /// <param name="installationId">The installation's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid installationId, CancellationToken cancellationToken);
}
