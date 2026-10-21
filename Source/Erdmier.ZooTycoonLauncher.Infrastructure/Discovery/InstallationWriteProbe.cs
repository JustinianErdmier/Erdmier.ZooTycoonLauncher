using Microsoft.Extensions.Logging;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Discovery;

/// <summary>
///     File-system-backed <see cref="IInstallationWriteProbe" />. Checks the read-only attribute on <c>zoo.ini</c>, then creates and deletes an empty <c>zoo.ini.tmp.*</c> file in the
///     folder to learn whether the folder accepts new files.
/// </summary>
public sealed class InstallationWriteProbe : IInstallationWriteProbe
{
    private const string IniFileName = "zoo.ini";

    private const string TempFilePrefix = "zoo.ini.tmp.";

    private readonly IFileSystem _fileSystem;

    private readonly ILogger<InstallationWriteProbe> _logger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="fileSystem">The file-system abstraction.</param>
    /// <param name="logger">The logger.</param>
    public InstallationWriteProbe(IFileSystem fileSystem, ILogger<InstallationWriteProbe> logger)
    {
        _fileSystem = fileSystem;
        _logger     = logger;
    }

    /// <inheritdoc />
    public async Task<InstallationWriteAccess> ProbeAsync(string installationPath, CancellationToken cancellationToken)
    {
        string probePath = string.Empty;

        try
        {
            string iniPath = _fileSystem.Path.Combine(installationPath, IniFileName);

            if (_fileSystem.File.Exists(iniPath) && _fileSystem.File.GetAttributes(iniPath).HasFlag(FileAttributes.ReadOnly))
            {
                return InstallationWriteAccess.FileReadOnly;
            }

            probePath = _fileSystem.Path.Combine(installationPath, TempFilePrefix + Guid.NewGuid().ToString(format: "N"));

            await _fileSystem.File.WriteAllBytesAsync(probePath, [], cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogDebug(ex, message: "The installation folder {Path} refused a probe file", installationPath);

            return InstallationWriteAccess.FolderReadOnly;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, message: "Could not probe write access to {Path}", installationPath);

            return InstallationWriteAccess.Unknown;
        }

        try
        {
            _fileSystem.File.Delete(probePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, message: "Could not delete the write-probe file {Path}", probePath);
        }

        return InstallationWriteAccess.Writable;
    }
}
