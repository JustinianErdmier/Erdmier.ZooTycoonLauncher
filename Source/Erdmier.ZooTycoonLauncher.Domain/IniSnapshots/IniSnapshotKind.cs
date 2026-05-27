namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>The three kinds of INI snapshot (SDD §5.2).</summary>
public sealed class IniSnapshotKind : SmartEnum<IniSnapshotKind>
{
    /// <summary>The launcher's belief about the on-disk values right now. Exactly one per installation.</summary>
    public static readonly IniSnapshotKind Current = new(name: "Current", id: 2);

    /// <summary>An archived prior <see cref="Current" /> snapshot. Unbounded per installation.</summary>
    public static readonly IniSnapshotKind Historical = new(name: "Historical", id: 3);

    /// <summary>The first-ever parse of <c>zoo.ini</c> when the installation was added. Exactly one per installation.</summary>
    public static readonly IniSnapshotKind Original = new(name: "Original", id: 1);

    private IniSnapshotKind(string name, int id)
        : base(name, id)
    { }
}
