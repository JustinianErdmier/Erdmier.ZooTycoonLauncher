namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.IniSnapshots;

public sealed class IniSnapshotComparerTests
{
    private static Dictionary<IniKeyId, string?> Map(params (string Section, string Key, string? Value)[] entries)
        => entries.ToDictionary(entry => new IniKeyId(entry.Section, entry.Key), entry => entry.Value);

    [ Fact ]
    public void Compare_EqualMaps_HasNoRowsOrKeptKeys()
    {
        Dictionary<IniKeyId, string?> values = Map((Section: "user", Key: "screenwidth", Value: "1024"), (Section: "UI", Key: "tooltipDelay", Value: "2"));

        IniSnapshotComparison result = IniSnapshotComparer.Compare(values, new Dictionary<IniKeyId, string?>(values));

        result.Rows.ShouldBeEmpty();
        result.KeptKeys.ShouldBeEmpty();
    }

    [ Fact ]
    public void Compare_GameManagedDifference_IsIgnored()
    {
        Dictionary<IniKeyId, string?> current = Map((Section: "UI", Key: "lastWindowX", Value: "10"));
        Dictionary<IniKeyId, string?> snapshot = Map((Section: "UI", Key: "lastWindowX", Value: "99"));

        IniSnapshotComparer.Compare(current, snapshot).Rows.ShouldBeEmpty();
    }

    [ Fact ]
    public void Compare_ChangedUserSetting_IsARowWithBothEffectiveValues()
    {
        Dictionary<IniKeyId, string?> current = Map((Section: "user", Key: "screenwidth", Value: "1920"));
        Dictionary<IniKeyId, string?> snapshot = Map((Section: "user", Key: "screenwidth", Value: "1024"));

        IniSnapshotComparison result = IniSnapshotComparer.Compare(current, snapshot);

        result.Rows.ShouldBe([ new IniDiffRow(new IniKeyId(Section: "user", Key: "screenwidth"), Current: "1920", Snapshot: "1024") ]);
    }

    [ Fact ]
    public void Compare_KeyMissingFromSnapshot_IsAKeptKeyNotARow()
    {
        Dictionary<IniKeyId, string?> current = Map((Section: "UI", Key: "tooltipDelay", Value: "2"));
        Dictionary<IniKeyId, string?> snapshot = Map();

        IniSnapshotComparison result = IniSnapshotComparer.Compare(current, snapshot);

        result.Rows.ShouldBeEmpty();
        result.KeptKeys.ShouldBe([ new IniKeyId(Section: "UI", Key: "tooltipDelay") ]);
    }

    [ Fact ]
    public void Compare_KeyMissingFromCurrent_IsARowEvenAtTheDefault()
    {
        Dictionary<IniKeyId, string?> current = Map();
        Dictionary<IniKeyId, string?> snapshot = Map((Section: "user", Key: "screenwidth", Value: "800"));

        IniSnapshotComparison result = IniSnapshotComparer.Compare(current, snapshot);

        result.Rows.ShouldBe([ new IniDiffRow(new IniKeyId(Section: "user", Key: "screenwidth"), Current: null, Snapshot: "800") ]);
    }

    [ Fact ]
    public void Compare_InvalidSnapshotValue_ComparesAsTheDefault()
    {
        Dictionary<IniKeyId, string?> snapshot = Map((Section: "user", Key: "screenwidth", Value: "abc"));

        IniSnapshotComparer.Compare(Map((Section: "user", Key: "screenwidth", Value: "800")), snapshot).Rows.ShouldBeEmpty();

        IniSnapshotComparison result = IniSnapshotComparer.Compare(Map((Section: "user", Key: "screenwidth", Value: "1024")), snapshot);

        result.Rows.ShouldHaveSingleItem().Snapshot.ShouldBe(expected: "800");
    }

    [ Fact ]
    public void Compare_BoolSpellings_AreEquivalent()
    {
        Dictionary<IniKeyId, string?> current = Map((Section: "user", Key: "fullscreen", Value: "1"));
        Dictionary<IniKeyId, string?> snapshot = Map((Section: "user", Key: "fullscreen", Value: "true"));

        IniSnapshotComparer.Compare(current, snapshot).Rows.ShouldBeEmpty();
    }

    [ Fact ]
    public void Compare_OrdersRowsBySectionThenRegistryOrder()
    {
        Dictionary<IniKeyId, string?> current = Map((Section: "UI", Key: "tooltipDelay", Value: "2"), (Section: "user", Key: "screenwidth", Value: "1920"));
        Dictionary<IniKeyId, string?> snapshot = Map((Section: "UI", Key: "tooltipDelay", Value: "3"), (Section: "user", Key: "screenwidth", Value: "1024"));

        IniSnapshotComparison result = IniSnapshotComparer.Compare(current, snapshot);

        result.Rows.Select(row => row.Id).ShouldBe([ new IniKeyId(Section: "user", Key: "screenwidth"), new IniKeyId(Section: "UI", Key: "tooltipDelay") ]);
    }

    [ Fact ]
    public void Compare_KeptKeys_FollowSectionThenRegistryOrder()
    {
        Dictionary<IniKeyId, string?> current = Map((Section: "UI", Key: "tooltipDelay", Value: "2"),
                                                    (Section: "user", Key: "screenheight", Value: "600"),
                                                    (Section: "user", Key: "fullscreen", Value: "1"));

        IniSnapshotComparison result = IniSnapshotComparer.Compare(current, Map());

        result.KeptKeys.ShouldBe([
                                     new IniKeyId(Section: "user", Key: "fullscreen"),
                                     new IniKeyId(Section: "user", Key: "screenheight"),
                                     new IniKeyId(Section: "UI", Key: "tooltipDelay")
                                 ]);
    }

    [ Fact ]
    public void Compare_SnapshotValuePresentButNull_ComparesAsTheDefault()
    {
        Dictionary<IniKeyId, string?> snapshot = Map((Section: "user", Key: "screenwidth", Value: null));

        IniSnapshotComparer.Compare(Map((Section: "user", Key: "screenwidth", Value: "800")), snapshot).Rows.ShouldBeEmpty();

        IniSnapshotComparison result = IniSnapshotComparer.Compare(Map((Section: "user", Key: "screenwidth", Value: "1024")), snapshot);

        result.Rows.ShouldHaveSingleItem().Snapshot.ShouldBe(expected: "800");
    }

    [ Fact ]
    public void ValuesOf_MapsSectionAndKeyCaseInsensitively()
    {
        IniSnapshot snapshot = new();
        snapshot.Values.Add(new IniValue { Section = "USER", Key = "ScreenWidth", Value = "1024" });

        IReadOnlyDictionary<IniKeyId, string?> values = IniSnapshotComparer.ValuesOf(snapshot);

        values[new IniKeyId(Section: "user", Key: "screenwidth")].ShouldBe(expected: "1024");
    }
}
