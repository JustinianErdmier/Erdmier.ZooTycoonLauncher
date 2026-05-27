namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Reads and writes <see cref="GameInstallation" /> rows in <c>Launcher.db</c>.</summary>
/// <remarks>
///     Implementations in subsequent milestones will add membership operations (Add/Edit/Delete/Fix) — this foundations milestone only exposes the read API needed by the
///     placeholder boot pipeline.
/// </remarks>
public interface IInstallationRepository
{
    /// <summary>Returns every registered installation, ordered alphabetically (case-insensitive) by name.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The installations, or an empty list when none exist.</returns>
    Task<IReadOnlyList<GameInstallation>> GetAllAsync(CancellationToken cancellationToken);
}
