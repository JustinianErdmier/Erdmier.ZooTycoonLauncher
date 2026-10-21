namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Probes whether the launcher can write <c>zoo.ini</c> in an installation folder (milestone 10 spec, C6).</summary>
public interface IInstallationWriteProbe
{
    /// <summary>Determines the write access to an installation. Never throws.</summary>
    /// <param name="installationPath">The installation folder.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     <see cref="InstallationWriteAccess.FileReadOnly" /> when <c>zoo.ini</c> carries the read-only attribute, <see cref="InstallationWriteAccess.FolderReadOnly" /> when the
    ///     folder refuses a new file, <see cref="InstallationWriteAccess.Writable" /> when a probe file could be created, otherwise <see cref="InstallationWriteAccess.Unknown" />.
    /// </returns>
    Task<InstallationWriteAccess> ProbeAsync(string installationPath, CancellationToken cancellationToken);
}
