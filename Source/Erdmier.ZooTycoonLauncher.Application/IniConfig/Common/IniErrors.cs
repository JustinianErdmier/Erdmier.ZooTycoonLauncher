namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;

/// <summary>The expected failures of the INI Config slice. Descriptions are user-readable because the Desktop layer shows them verbatim.</summary>
public static class IniErrors
{
    /// <summary>The code carried by <see cref="AlreadyExists" />, for callers that treat that failure as a cue to re-probe rather than as an error.</summary>
    public const string AlreadyExistsCode = "Ini.AlreadyExists";

    /// <summary>The installation id does not resolve to a row.</summary>
    /// <param name="installationId">The missing id.</param>
    /// <returns>A not-found error coded <c>Installation.NotFound</c>.</returns>
    public static Error InstallationNotFound(Guid installationId) => Error.NotFound(code: "Installation.NotFound", $"No installation with id {installationId}.");

    /// <summary><c>zoo.ini</c> is absent from the installation folder.</summary>
    /// <param name="installationPath">The installation folder.</param>
    /// <returns>A not-found error coded <c>Ini.Missing</c>.</returns>
    public static Error Missing(string installationPath) => Error.NotFound(code: "Ini.Missing", $"zoo.ini was not found in \"{installationPath}\".");

    /// <summary>Reading <c>zoo.ini</c> failed.</summary>
    /// <param name="message">The operating system's message.</param>
    /// <returns>A failure coded <c>Ini.ReadFailed</c>.</returns>
    public static Error ReadFailed(string message) => Error.Failure(code: "Ini.ReadFailed", $"zoo.ini could not be read: {message}");

    /// <summary>Writing <c>zoo.ini</c> failed.</summary>
    /// <param name="message">The operating system's message.</param>
    /// <returns>A failure coded <c>Ini.WriteFailed</c>.</returns>
    public static Error WriteFailed(string message) => Error.Failure(code: "Ini.WriteFailed", $"zoo.ini could not be saved: {message}");

    /// <summary>The installation's snapshot database failed.</summary>
    /// <param name="message">The underlying exception's message.</param>
    /// <returns>An unexpected error coded <c>Ini.StoreFailed</c>.</returns>
    public static Error StoreFailed(string message) => Error.Unexpected(code: "Ini.StoreFailed", $"The installation's settings history could not be read or updated: {message}");

    /// <summary><c>zoo.ini</c> appeared before Create or Locate could write it, so nothing was written.</summary>
    /// <param name="installationPath">The installation folder.</param>
    /// <returns>A conflict coded <c>Ini.AlreadyExists</c>.</returns>
    public static Error AlreadyExists(string installationPath) => Error.Conflict(code: AlreadyExistsCode, $"zoo.ini already exists in \"{installationPath}\", so nothing was written.");

    /// <summary>The chosen file does not look like a Zoo Tycoon <c>zoo.ini</c>.</summary>
    /// <param name="sourcePath">The chosen file's path; only its file name is shown.</param>
    /// <returns>A validation error coded <c>Ini.NotAnIni</c>.</returns>
    public static Error NotAnIni(string sourcePath) => Error.Validation(code: "Ini.NotAnIni", $"\"{Path.GetFileName(sourcePath)}\" does not look like a Zoo Tycoon zoo.ini.");

    /// <summary>Writing <c>zoo.ini</c> was denied because the folder or the file is read-only.</summary>
    /// <param name="access">The probed write access; must be <see cref="InstallationWriteAccess.FolderReadOnly" /> or <see cref="InstallationWriteAccess.FileReadOnly" />.</param>
    /// <returns>A failure coded <c>Ini.ReadOnly</c> carrying the read-only explanation.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="access" /> is <see cref="InstallationWriteAccess.Writable" /> or <see cref="InstallationWriteAccess.Unknown" />.</exception>
    public static Error ReadOnly(InstallationWriteAccess access)
        => Error.Failure(code: "Ini.ReadOnly",
                         IniReadOnlyText.For(access) ?? throw new ArgumentOutOfRangeException(nameof(access), access, message: "Only a read-only access has an explanation."));

    /// <summary>The snapshot chosen for restoring no longer exists.</summary>
    /// <param name="snapshotId">The missing snapshot id.</param>
    /// <returns>A not-found error coded <c>Ini.SnapshotNotFound</c>.</returns>
    public static Error SnapshotNotFound(Guid snapshotId)
        => Error.NotFound(code: "Ini.SnapshotNotFound",
                          description: "The selected version of zoo.ini no longer exists.",
                          metadata: new Dictionary<string, object> { [ "SnapshotId" ] = snapshotId });
}
