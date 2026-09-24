namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.IniDrift;

public sealed class IniDriftDetectorTests
{
    private static readonly IniKeyId ScreenWidth = new(Section: "user", Key: "screenwidth");

    private static readonly IniKeyId LastWindowX = new(Section: "UI", Key: "lastWindowX");

    private static readonly IniKeyId LastFile = new(Section: "user", Key: "lastfile");

    [ Fact ]
    public void Detect_IdenticalValues_IsNone()
    {
        Dictionary<IniKeyId, string?> values = new() { [ScreenWidth] = "800", [LastWindowX] = "10" };

        IniDriftResult drift = IniDriftDetector.Detect(values, new Dictionary<IniKeyId, string?>(values));

        drift.Kind.ShouldBe(IniDriftKind.None);
        drift.ChangedKeys.ShouldBeEmpty();
    }

    [ Fact ]
    public void Detect_EquivalentButTextuallyDifferentValues_IsNone()
    {
        Dictionary<IniKeyId, string?> current = new() { [ScreenWidth] = "800" };
        Dictionary<IniKeyId, string?> disk    = new() { [new IniKeyId(Section: "USER", Key: "SCREENWIDTH")] = " 0800 " };

        IniDriftDetector.Detect(current, disk).Kind.ShouldBe(IniDriftKind.None);
    }

    [ Fact ]
    public void Detect_OnlyGameManagedChanged_IsGameManagedOnly()
    {
        Dictionary<IniKeyId, string?> current = new() { [ScreenWidth] = "800", [LastWindowX] = "10" };
        Dictionary<IniKeyId, string?> disk    = new() { [ScreenWidth] = "800", [LastWindowX] = "250", [LastFile] = @"C:\a.zoo" };

        IniDriftResult drift = IniDriftDetector.Detect(current, disk);

        drift.Kind.ShouldBe(IniDriftKind.GameManagedOnly);
        drift.ChangedKeys.ShouldBe([LastFile, LastWindowX]);
    }

    [ Fact ]
    public void Detect_AnyUserSettingChanged_IsUserSettingsAndListsEveryChange()
    {
        Dictionary<IniKeyId, string?> current = new() { [ScreenWidth] = "800", [LastWindowX] = "10" };
        Dictionary<IniKeyId, string?> disk    = new() { [ScreenWidth] = "1024", [LastWindowX] = "20" };

        IniDriftResult drift = IniDriftDetector.Detect(current, disk);

        drift.Kind.ShouldBe(IniDriftKind.UserSettings);
        drift.ChangedKeys.ShouldBe([ScreenWidth, LastWindowX]);
    }

    [ Fact ]
    public void Detect_UserSettingRemovedFromTheFile_IsUserSettings()
    {
        Dictionary<IniKeyId, string?> current = new() { [ScreenWidth] = "800" };

        IniDriftDetector.Detect(current, new Dictionary<IniKeyId, string?>()).Kind.ShouldBe(IniDriftKind.UserSettings);
    }

    [ Fact ]
    public void Detect_UserSettingAddedToTheFile_IsUserSettings()
    {
        Dictionary<IniKeyId, string?> disk = new() { [ScreenWidth] = "800" };

        IniDriftDetector.Detect(new Dictionary<IniKeyId, string?>(), disk).Kind.ShouldBe(IniDriftKind.UserSettings);
    }

    [ Fact ]
    public void Detect_UnrecognisedKeysAreIgnored()
    {
        Dictionary<IniKeyId, string?> disk = new() { [new IniKeyId(Section: "scenario", Key: "ag")] = "0" };

        IniDriftDetector.Detect(new Dictionary<IniKeyId, string?>(), disk).Kind.ShouldBe(IniDriftKind.None);
    }
}
