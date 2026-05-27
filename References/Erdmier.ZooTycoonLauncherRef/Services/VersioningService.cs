using System;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <inheritdoc cref="IVersioningService" />
public sealed class VersioningService : IVersioningService
{
    private const string OriginalSuffix = ".original";

    private const string UndoSuffix = ".undo";

    private readonly IFileSystem _fileSystem;

    public VersioningService(IFileSystem fileSystem) => _fileSystem = fileSystem;

    /// <inheritdoc />
    public Task EnsureOriginalBackupAsync(string iniFilePath)
    {
        return Task.Run(() =>
        {
            string originalPath = iniFilePath + OriginalSuffix;

            if (_fileSystem.File.Exists(originalPath))
            {
                return;
            }

            if (!_fileSystem.File.Exists(iniFilePath))
            {
                return;
            }

            _fileSystem.File.Copy(iniFilePath, originalPath);
        });
    }

    /// <inheritdoc />
    public bool OriginalBackupExists(string iniFilePath) => _fileSystem.File.Exists(iniFilePath + OriginalSuffix);

    /// <inheritdoc />
    public bool UndoSnapshotExists(string iniFilePath) => _fileSystem.File.Exists(iniFilePath + UndoSuffix);

    /// <inheritdoc />
    public Task CreateUndoSnapshotAsync(string iniFilePath)
    {
        return Task.Run(() =>
        {
            // No source file to snapshot — nothing to do. (The save path bails out before this in that case, but be defensive.)
            if (!_fileSystem.File.Exists(iniFilePath))
            {
                return;
            }

            // Single-slot rolling snapshot of the *previous* on-disk content. Always overwrite so each save is undoable to its immediate predecessor (SDD §8.2).
            string undoPath = iniFilePath + UndoSuffix;
            _fileSystem.File.Copy(iniFilePath, undoPath, overwrite: true);
        });
    }

    /// <inheritdoc />
    public Task<bool> RestoreUndoAsync(string iniFilePath) => throw new NotImplementedException(message: "Restore undo is implemented in a future milestone.");

    /// <inheritdoc />
    public Task<bool> RestoreOriginalAsync(string iniFilePath) => throw new NotImplementedException(message: "Restore original is implemented in a future milestone.");
}
