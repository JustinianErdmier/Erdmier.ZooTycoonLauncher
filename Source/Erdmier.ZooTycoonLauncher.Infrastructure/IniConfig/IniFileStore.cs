namespace Erdmier.ZooTycoonLauncher.Infrastructure.IniConfig;

/// <summary>
///     <see cref="IIniFileStore" /> over <see cref="IFileSystem" />. Text is decoded and encoded as Latin-1, which maps every byte to exactly one character and back, so a round trip
///     is byte-identical whatever code page the file was written in. Writes go to a temp file in the same folder and are moved over <c>zoo.ini</c> (SDD §7.7, §8.2).
/// </summary>
public sealed class IniFileStore : IIniFileStore
{
    private const string FileName = "zoo.ini";

    private const string TempFilePrefix = "zoo.ini.tmp.";

    private readonly IFileSystem _fileSystem;

    private readonly ILogger _logger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="fileSystem">File-system abstraction.</param>
    /// <param name="logger">The Serilog logger.</param>
    public IniFileStore(IFileSystem fileSystem, ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger     = logger;
    }

    /// <inheritdoc />
    public async Task<IniFileContent?> ReadAsync(string installationPath, CancellationToken cancellationToken)
    {
        string path = _fileSystem.Path.Combine(installationPath, FileName);

        if (!_fileSystem.File.Exists(path))
        {
            return null;
        }

        byte[] bytes = await _fileSystem.File.ReadAllBytesAsync(path, cancellationToken);

        return new IniFileContent(Encoding.Latin1.GetString(bytes), _fileSystem.File.GetLastWriteTimeUtc(path));
    }

    /// <inheritdoc />
    public async Task<DateTime> WriteAsync(string installationPath, string text, CancellationToken cancellationToken)
    {
        string path     = _fileSystem.Path.Combine(installationPath, FileName);
        string tempPath = _fileSystem.Path.Combine(installationPath, TempFilePrefix + Guid.NewGuid().ToString(format: "N"));

        try
        {
            await _fileSystem.File.WriteAllBytesAsync(tempPath, Encoding.Latin1.GetBytes(text), cancellationToken);

            // The temp file sits in the same folder, so this is a same-volume rename — atomic on NTFS.
            _fileSystem.File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            TryDelete(tempPath);

            throw;
        }

        return _fileSystem.File.GetLastWriteTimeUtc(path);
    }

    /// <inheritdoc />
    public Task DeleteOrphanedTempFilesAsync(string installationPath, CancellationToken cancellationToken)
    {
        if (!_fileSystem.Directory.Exists(installationPath))
        {
            return Task.CompletedTask;
        }

        foreach (string orphan in _fileSystem.Directory.EnumerateFiles(installationPath, TempFilePrefix + "*"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            TryDelete(orphan);
        }

        return Task.CompletedTask;
    }

    private void TryDelete(string path)
    {
        try
        {
            if (_fileSystem.File.Exists(path))
            {
                _fileSystem.File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Warning(ex, messageTemplate: "Could not delete the INI temp file {Path}", path);
        }
    }
}
