namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.IniKeys;

public sealed class ZooIniDefaultsTests
{
    [ Fact ]
    public void Registry_Has56KeysOf46AreUserSettings()
    {
        ZooIniDefaults.Keys.Count.ShouldBe(expected: 56);
        ZooIniDefaults.Keys.Count(spec => spec.Role == IniKeyRole.UserSetting).ShouldBe(expected: 46);
        ZooIniDefaults.Keys.Count(spec => spec.Role == IniKeyRole.GameManaged).ShouldBe(expected: 10);
    }

    [ Fact ]
    public void Sections_AreInSddOrder() => ZooIniDefaults.Sections.ShouldBe(["user", "UI", "advanced", "ai", "debug", "language", "Map"]);

    [ Fact ]
    public void Keys_HaveUniqueIds() => ZooIniDefaults.Keys.Select(spec => spec.Id).Distinct().Count().ShouldBe(ZooIniDefaults.Keys.Count);

    [ Fact ]
    public void Keys_OnlyUseListedSections() => ZooIniDefaults.Keys.ShouldAllBe(spec => ZooIniDefaults.Sections.Contains(spec.Id.Section));

    [ Fact ]
    public void Keys_DefaultsAreValidUnderTheirOwnSpec() => ZooIniDefaults.Keys.Where(spec => spec.DefaultValue is not null).ShouldAllBe(spec => spec.IsValid(spec.DefaultValue));

    [ Fact ]
    public void Keys_MinNeverExceedsMax() => ZooIniDefaults.Keys.Where(spec => spec.Min is not null && spec.Max is not null).ShouldAllBe(spec => spec.Min <= spec.Max);

    [ Theory ]
    [ InlineData("user", "lastfile") ]
    [ InlineData("user", "showUserEntityWarning") ]
    [ InlineData("UI", "lastWindowX") ]
    [ InlineData("UI", "lastWindowY") ]
    [ InlineData("UI", "startedFirstTutorial") ]
    [ InlineData("UI", "startedDinoTutorial") ]
    [ InlineData("UI", "startedAquaTutorial") ]
    [ InlineData("UI", "progresscalls") ]
    [ InlineData("UI", "defaultEditCharLimit") ]
    [ InlineData("UI", "completedExhibitAttenuation") ]
    public void GameManagedKeys_AreClassifiedAsSuch(string section, string key)
    {
        ZooIniDefaults.TryGet(new IniKeyId(section, key), out IniKeySpec? spec).ShouldBeTrue();
        spec.Role.ShouldBe(IniKeyRole.GameManaged);
    }

    [ Fact ]
    public void TryGet_IsCaseInsensitiveAndReturnsRegistryCasing()
    {
        ZooIniDefaults.TryGet(new IniKeyId(Section: "ui", Key: "MSSTARTINGCASH"), out IniKeySpec? spec).ShouldBeTrue();
        spec.Id.Section.ShouldBe(expected: "UI");
        spec.Id.Key.ShouldBe(expected: "MSStartingCash");
        spec.DefaultValue.ShouldBe(expected: "70000");
    }

    [ Fact ]
    public void TryGet_UnknownKey_ReturnsFalse() => ZooIniDefaults.TryGet(new IniKeyId(Section: "scenario", Key: "ag"), out IniKeySpec? _).ShouldBeFalse();

    [ Fact ]
    public void ExtractValues_ReturnsOnlyPresentRecognisedKeysKeyedByRegistryId()
    {
        IniDocument document = IniDocument.Parse("[USER]\r\nFullScreen=0\r\nfullscreen=1\r\n[scenario]\r\nag=0\r\n[UI]\r\nlastWindowX=\r\n");

        IReadOnlyDictionary<IniKeyId, string?> values = ZooIniDefaults.ExtractValues(document);

        values.Count.ShouldBe(expected: 2);
        values[new IniKeyId(Section: "user", Key: "fullscreen")].ShouldBe(expected: "0");
        values.Keys.ShouldContain(id => id.Section == "user" && id.Key == "fullscreen");
        values[new IniKeyId(Section: "UI", Key: "lastWindowX")].ShouldBe(expected: "");
    }
}
