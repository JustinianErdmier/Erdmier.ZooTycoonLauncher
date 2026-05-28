namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Walks the registry, hard-coded Program Files paths, and any persisted last-known directory looking for a folder that contains <c>zoo.exe</c>.</summary>
/// <remarks>
///     SDD §8.5 catalogues the search order; this interface stays unaware of <c>LauncherSettings</c> — the application layer threads any persisted last-known directory through
///     <see cref="LocateAsync" />.
/// </remarks>
public interface IInstallationLocator
{
    /// <summary>Walks the search order and returns the first directory containing <c>zoo.exe</c>.</summary>
    /// <param name="persistedLastKnownPath">A previously persisted path to probe first, or <see langword="null" /> on first run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The discovered directory and the trail of probed locations.</returns>
    Task<LocatedDirectory> LocateAsync(string? persistedLastKnownPath, CancellationToken cancellationToken);
}
