namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>Point-in-time materialisation of every recognised <c>zoo.ini</c> setting plus the file's raw text (SDD §5.1, §6.3).</summary>
public sealed class IniSnapshot
{
    /// <summary>UTC timestamp of capture.</summary>
    public DateTime CapturedUtc { get; init; }

    /// <summary>The snapshot's identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Which of the three kinds this snapshot is (<see cref="IniSnapshotKind" />).</summary>
    public IniSnapshotKind Kind { get; init; } = IniSnapshotKind.Current;

    /// <summary>The raw INI text at capture time, used to re-emit the file with comments and ordering preserved.</summary>
    public string StructureBlob { get; set; } = string.Empty;

    /// <summary>What triggered the capture (<see cref="IniSnapshotTrigger" />).</summary>
    public IniSnapshotTrigger Trigger { get; init; } = IniSnapshotTrigger.OriginalImport;

    /// <summary>The EAV value rows belonging to this snapshot.</summary>
    public IList<IniValue> Values { get; init; } = [];
}
