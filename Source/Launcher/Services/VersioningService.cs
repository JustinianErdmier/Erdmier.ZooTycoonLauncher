using System;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IVersioningService" />
public sealed class VersioningService : IVersioningService
{
    private const string OriginalSuffix = ".original";
    private const string UndoSuffix     = ".undo";

    private readonly IFileSystem _fileSystem;

    public VersioningService(IFileSystem fileSystem) => _fileSystem = fileSystem;

    /// <inheritdoc />
    public Task EnsureOriginalBackupAsync(string iniFilePath)
    {
        return Task.Run(() =>
        {
            var originalPath = iniFilePath + OriginalSuffix;
            if (_fileSystem.File.Exists(originalPath)) return;
            if (!_fileSystem.File.Exists(iniFilePath)) return;
            _fileSystem.File.Copy(iniFilePath, originalPath);
        });
    }

    /// <inheritdoc />
    public bool OriginalBackupExists(string iniFilePath) =>
        _fileSystem.File.Exists(iniFilePath + OriginalSuffix);

    /// <inheritdoc />
    public bool UndoSnapshotExists(string iniFilePath) =>
        _fileSystem.File.Exists(iniFilePath + UndoSuffix);

    /// <inheritdoc />
    public Task CreateUndoSnapshotAsync(string iniFilePath) =>
        throw new NotImplementedException("Undo snapshot is implemented in a future milestone.");

    /// <inheritdoc />
    public Task<bool> RestoreUndoAsync(string iniFilePath) =>
        throw new NotImplementedException("Restore undo is implemented in a future milestone.");

    /// <inheritdoc />
    public Task<bool> RestoreOriginalAsync(string iniFilePath) =>
        throw new NotImplementedException("Restore original is implemented in a future milestone.");
}
