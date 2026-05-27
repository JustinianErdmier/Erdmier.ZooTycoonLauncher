namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>How a particular value landed in its snapshot (SDD §5.1 — per-row source flag).</summary>
public sealed class IniValueSource : SmartEnum<IniValueSource>
{
    /// <summary>The value was written by the launcher's GUI in the most recent save.</summary>
    public static readonly IniValueSource LauncherGui = new(name: "LauncherGui", id: 2);

    /// <summary>The value was detected as a manual edit during drift detection on the next open.</summary>
    public static readonly IniValueSource Manual = new(name: "Manual", id: 3);

    /// <summary>The value was captured during the first-ever parse of <c>zoo.ini</c>.</summary>
    public static readonly IniValueSource OriginalImport = new(name: "OriginalImport", id: 1);

    private IniValueSource(string name, int id)
        : base(name, id)
    { }
}
