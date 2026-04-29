using System.Threading.Tasks;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Manages the <c> zoo.ini.original </c> and <c> zoo.ini.undo </c> backup files.</summary>
/// <remarks>
///     This milestone implements only <see cref="EnsureOriginalBackupAsync" />, <see cref="OriginalBackupExists" />, and <see cref="UndoSnapshotExists" />. The remaining members
///     throw <see cref="System.NotImplementedException" /> and will be filled in when the full versioning task is undertaken.
/// </remarks>
public interface IVersioningService
{
    /// <summary>Called once on first launch. Creates <c> zoo.ini.original </c> alongside <paramref name="iniFilePath" /> if it does not already exist.</summary>
    Task EnsureOriginalBackupAsync(string iniFilePath);

    /// <summary>Copies the current <c> zoo.ini </c> to <c> zoo.ini.undo </c> before a save operation. Not yet implemented.</summary>
    Task CreateUndoSnapshotAsync(string iniFilePath);

    /// <summary>Restores <c> zoo.ini </c> from <c> zoo.ini.undo </c>. Not yet implemented.</summary>
    Task<bool> RestoreUndoAsync(string iniFilePath);

    /// <summary>Restores <c> zoo.ini </c> from <c> zoo.ini.original </c>. Not yet implemented.</summary>
    Task<bool> RestoreOriginalAsync(string iniFilePath);

    bool UndoSnapshotExists(string iniFilePath);

    bool OriginalBackupExists(string iniFilePath);
}
