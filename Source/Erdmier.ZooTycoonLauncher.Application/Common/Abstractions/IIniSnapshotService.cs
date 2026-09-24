namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Capture, synchronisation, and load surface for per-installation INI snapshots (SDD §7.1, §7.3, §7.7). Implemented by <c>IniSnapshotService</c>.</summary>
public interface IIniSnapshotService
{
    /// <summary>
    ///     Imports <c>zoo.ini</c> into a freshly created installation database (<c>Original</c> + <c>Current</c>). No-op when <see cref="GameInstallation.HasIni" /> is
    ///     <see langword="false" />.
    /// </summary>
    /// <param name="installation">The newly created installation row.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success on import or no-op; a typed error on read or store failure.</returns>
    Task<ErrorOr<Success>> CaptureOriginalAsync(GameInstallation installation, CancellationToken cancellationToken);

    /// <summary>
    ///     Reconciles the <c>Current</c> snapshot with the file on disk using tiered drift (first import when empty). No-op when <see cref="GameInstallation.HasIni" /> is
    ///     <see langword="false" />.
    /// </summary>
    /// <param name="installation">The installation to synchronise.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success on synchronisation or no-op; a typed error on read or store failure.</returns>
    Task<ErrorOr<Success>> SynchroniseAsync(GameInstallation installation, CancellationToken cancellationToken);

    /// <summary>Synchronises, then returns the reconciled values and the file's last-write time for the editor.</summary>
    /// <param name="installation">The installation to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The values, or <c>Ini.Missing</c> / <c>Ini.ReadFailed</c> / <c>Ini.StoreFailed</c>.</returns>
    Task<ErrorOr<IniConfigResult>> LoadAsync(GameInstallation installation, CancellationToken cancellationToken);
}
