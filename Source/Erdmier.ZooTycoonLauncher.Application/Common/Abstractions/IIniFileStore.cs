namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Reads and atomically writes an installation's <c>zoo.ini</c> as Latin-1 text, so every byte round-trips unchanged (SDD §7.7, §8.1).</summary>
public interface IIniFileStore
{
    /// <summary>Reads <c>zoo.ini</c> from the installation folder.</summary>
    /// <param name="installationPath">The installation folder.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file's text and last-write time, or <see langword="null" /> when the file is absent.</returns>
    Task<IniFileContent?> ReadAsync(string installationPath, CancellationToken cancellationToken);

    /// <summary>Writes <c>zoo.ini</c> atomically: a temp file in the same folder, then <c>Move(overwrite: true)</c>. Throws on failure after removing the temp file.</summary>
    /// <param name="installationPath">The installation folder.</param>
    /// <param name="text">The full file text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The written file's last-write time (UTC).</returns>
    Task<DateTime> WriteAsync(string installationPath, string text, CancellationToken cancellationToken);

    /// <summary>Deletes <c>zoo.ini.tmp.*</c> files left behind by an interrupted write. Files that cannot be deleted are logged and skipped.</summary>
    /// <param name="installationPath">The installation folder.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteOrphanedTempFilesAsync(string installationPath, CancellationToken cancellationToken);
}
