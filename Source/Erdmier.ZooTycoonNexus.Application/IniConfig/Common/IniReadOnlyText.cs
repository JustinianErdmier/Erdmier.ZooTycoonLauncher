namespace Erdmier.ZooTycoonNexus.Application.IniConfig.Common;

/// <summary>The read-only explanations shown on the Game Config tab's read-only strip and returned by <c>Ini.ReadOnly</c> (milestone 10 spec, C9).</summary>
public static class IniReadOnlyText
{
    /// <summary>Gets the explanation for a write access, if it is one of the read-only cases.</summary>
    /// <param name="access">The probed write access.</param>
    /// <returns>The explanation for <see cref="InstallationWriteAccess.FolderReadOnly" /> or <see cref="InstallationWriteAccess.FileReadOnly" />; otherwise <see langword="null" />.</returns>
    public static string? For(InstallationWriteAccess access)
        => access == InstallationWriteAccess.FolderReadOnly
            ? "This installation's folder is read-only for your account, so changes to zoo.ini cannot be saved. Uninstalling and reinstalling Zoo Tycoon restores normal write access."
            : access == InstallationWriteAccess.FileReadOnly
                ? "zoo.ini is marked read-only, so changes cannot be saved. Clear Read-only in the file's Properties to fix it."
                : null;
}
