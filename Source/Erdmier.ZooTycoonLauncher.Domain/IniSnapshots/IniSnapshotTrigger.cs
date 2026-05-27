namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>Why a snapshot was captured (SDD §5.2).</summary>
public sealed class IniSnapshotTrigger : SmartEnum<IniSnapshotTrigger>
{
    /// <summary>The user saved INI changes through the launcher's GUI.</summary>
    public static readonly IniSnapshotTrigger LauncherGui = new(name: "LauncherGui", id: 2);

    /// <summary>Manual-edit drift was detected on the next open; the prior <c>Current</c> is archived before adoption.</summary>
    public static readonly IniSnapshotTrigger Manual = new(name: "Manual", id: 3);

    /// <summary>The installation was just added and its <c>zoo.ini</c> was parsed for the first time.</summary>
    public static readonly IniSnapshotTrigger OriginalImport = new(name: "OriginalImport", id: 1);

    private IniSnapshotTrigger(string name, int id)
        : base(name, id)
    { }
}
