namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>
/// The three kinds of INI snapshot (SDD §5.2).
/// </summary>
public sealed class IniSnapshotKind : SmartEnum<IniSnapshotKind>
{
    /// <summary>The first-ever parse of <c>zoo.ini</c> when the installation was added. Exactly one per installation.</summary>
    public static readonly IniSnapshotKind Original   = new("Original",   1);

    /// <summary>The launcher's belief about the on-disk values right now. Exactly one per installation.</summary>
    public static readonly IniSnapshotKind Current    = new("Current",    2);

    /// <summary>An archived prior <see cref="Current" /> snapshot. Unbounded per installation.</summary>
    public static readonly IniSnapshotKind Historical = new("Historical", 3);

    private IniSnapshotKind(string name, int id) : base(name, id) { }
}
