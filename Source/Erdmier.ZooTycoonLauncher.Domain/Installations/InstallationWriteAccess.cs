namespace Erdmier.ZooTycoonLauncher.Domain.Installations;

/// <summary>Whether the launcher can write <c>zoo.ini</c> in an installation (SDD §7.4; milestone 10 spec).</summary>
public enum InstallationWriteAccess
{
    /// <summary>Not yet probed, or the probe could not decide.</summary>
    Unknown,

    /// <summary>The installation folder and <c>zoo.ini</c> can both be written.</summary>
    Writable,

    /// <summary>The installation folder cannot be written, so <c>zoo.ini</c> cannot be replaced.</summary>
    FolderReadOnly,

    /// <summary>The <c>zoo.ini</c> file itself is read-only.</summary>
    FileReadOnly
}
