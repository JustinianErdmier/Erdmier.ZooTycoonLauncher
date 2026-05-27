namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Reads and writes <see cref="GameInstallation" /> rows in <c>Launcher.db</c>.</summary>
public interface IInstallationRepository
{
    /// <summary>Returns every registered installation, ordered alphabetically (case-insensitive) by name.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The installations, or an empty list when none exist.</returns>
    Task<IReadOnlyList<GameInstallation>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Returns the installation with the supplied identifier, or <see langword="null" /> when no such row exists.</summary>
    /// <param name="installationId">The installation's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<GameInstallation?> GetByIdAsync(Guid installationId, CancellationToken cancellationToken);

    /// <summary>Inserts a new installation row and persists immediately.</summary>
    /// <param name="installation">The installation to persist; <see cref="GameInstallation.Id" /> and <see cref="GameInstallation.AddedUtc" /> must be set by the caller.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(GameInstallation installation, CancellationToken cancellationToken);

    /// <summary>Persists mutable changes (<c>Name</c>, <c>HasExe</c>, <c>HasIni</c>, the timestamps) on the supplied row.</summary>
    /// <param name="installation">The tracked installation with pending changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(GameInstallation installation, CancellationToken cancellationToken);

    /// <summary>Removes the installation with the supplied identifier. Safe to call when no such row exists (no-op).</summary>
    /// <param name="installationId">The installation's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid installationId, CancellationToken cancellationToken);

    /// <summary>Case-insensitive existence check on <see cref="GameInstallation.Name" />, optionally excluding a row by id.</summary>
    /// <param name="name">The name to test.</param>
    /// <param name="excludeId">When supplied, the row with this id is excluded from the comparison (used by Edit).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when another row already uses this name.</returns>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken);

    /// <summary>Case-insensitive existence check on <see cref="GameInstallation.Path" />, optionally excluding a row by id.</summary>
    /// <param name="path">The path to test.</param>
    /// <param name="excludeId">When supplied, the row with this id is excluded from the comparison (used by Relocate).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when another row already uses this path.</returns>
    Task<bool> ExistsByPathAsync(string path, Guid? excludeId, CancellationToken cancellationToken);

    /// <summary>
    /// Picks the row that should be promoted to default — the alphabetically-first remaining row (case-insensitive on <see cref="GameInstallation.Name" />) — or <see langword="null" /> when no rows remain.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<GameInstallation?> FindDefaultPromotionCandidateAsync(CancellationToken cancellationToken);
}
