namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>
///     Capture and synchronisation surface for per-installation INI snapshots (SDD §7.3 / §8.2). The Installation Lifecycle slice only invokes
///     <see cref="CaptureOriginalAsync" />; the INI Config slice expands the surface.
/// </summary>
/// <remarks>
///     The Infrastructure layer ships a <c>NullIniSnapshotService</c> that returns <see cref="ErrorOr.Result.Success" /> and logs a warning for both methods. The real
///     implementation lands in the INI Config slice with no signature change.
/// </remarks>
public interface IIniSnapshotService
{
    /// <summary>
    ///     Reads <c>zoo.ini</c> for the supplied installation and writes the <c>Original</c> + <c>Current</c> snapshots in one transaction. No-op when
    ///     <see cref="GameInstallation.HasIni" /> is <see langword="false" />.
    /// </summary>
    /// <param name="installation">The newly created installation row.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success on capture or no-op; a typed error on parse or persist failure.</returns>
    Task<ErrorOr<Success>> CaptureOriginalAsync(GameInstallation installation, CancellationToken cancellationToken);

    /// <summary>
    ///     Checks whether <c>zoo.ini</c> has drifted on disk since the <c>Current</c> snapshot was written. When drift is detected, archives <c>Current</c> to <c>Historical</c> and
    ///     writes a new <c>Current</c> from the on-disk values. No-op when <see cref="GameInstallation.HasIni" /> is <see langword="false" />.
    /// </summary>
    /// <param name="installation">The installation to synchronise.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="ErrorOr.Result.Success" /> on synchronisation or no-op; a typed error on parse or persist failure.</returns>
    Task<ErrorOr<Success>> SynchroniseAsync(GameInstallation installation, CancellationToken cancellationToken);
}
