namespace Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.IniConfig;

public sealed class IniDisplayTextTests
{
    [ Theory ]
    [ InlineData("user", "fullscreen", "1", "Fullscreen") ]
    [ InlineData("user", "fullscreen", "0", "Windowed") ]
    [ InlineData("advanced", "level", "2", "2 – Balance") ]
    [ InlineData("UI", "helpType", " 1", "Standard") ]
    [ InlineData("UI", "playMovie", "1", "true") ]
    [ InlineData("UI", "playMovie", "0", "false") ]
    [ InlineData("UI", "playMovie", "True", "true") ]
    [ InlineData("UI", "playMovie", " 1 ", "true") ]
    [ InlineData("UI", "noMenuMusic", "1", "true") ]
    [ InlineData("UI", "noMenuMusic", "0", "false") ]
    [ InlineData("UI", "MSStartingCash", "10000", "$10,000") ]
    [ InlineData("user", "screenwidth", "800", "800") ]
    [ InlineData("UI", "menuMusic", "x.mp3", "x.mp3") ]
    [ InlineData("user", "fullscreen", "7", "7") ]
    [ InlineData("nope", "key", "abc", "abc") ]
    public void Format_ShowsTheValueAsTheEditorDoes(string section, string key, string value, string expected)
        => IniDisplayText.Format(IniEditorStyle.Ini, new IniKeyId(section, key), value).ShouldBe(expected);

    [ Fact ]
    public void KeyText_Ini_IsSectionSlashKey()
        => IniDisplayText.KeyText(IniEditorStyle.Ini, new IniKeyId(Section: "UI", Key: "playMovie")).ShouldBe(expected: "[UI]/playMovie");

    [ Theory ]
    [ InlineData("UI", "playMovie", "Sound › Intro movies › Play the first movie") ]
    [ InlineData("language", "lang", "Language › Language › Language code") ]
    [ InlineData("language", "sublang", "Language › Language › Region code") ]
    public void KeyText_Friendly_IsPageGroupLabel(string section, string key, string expected)
        => IniDisplayText.KeyText(IniEditorStyle.Friendly, new IniKeyId(section, key)).ShouldBe(expected);

    [ Fact ]
    public void KeyText_Friendly_UnknownKey_FallsBackToSectionSlashKey()
        => IniDisplayText.KeyText(IniEditorStyle.Friendly, new IniKeyId(Section: "nope", Key: "key")).ShouldBe(expected: "[nope]/key");

    [ Theory ]
    [ InlineData("UI", "noMenuMusic", "1", "Off") ]
    [ InlineData("UI", "noMenuMusic", "0", "On") ]
    [ InlineData("UI", "playMovie", "1", "On") ]
    [ InlineData("UI", "playMovie", "0", "Off") ]
    public void Format_Friendly_Toggle_IsOnOffByMeaning(string section, string key, string value, string expected)
        => IniDisplayText.Format(IniEditorStyle.Friendly, new IniKeyId(section, key), value).ShouldBe(expected);

    [ Theory ]
    [ InlineData("UI", "userAttenuation", "2500", "75 %") ]
    [ InlineData("UI", "userAttenuation", "0", "100 %") ]
    [ InlineData("UI", "movievolume1", "-10000", "0 %") ]
    [ InlineData("UI", "movievolume1", "-2500", "75 %") ]
    public void Format_Friendly_Volume_IsPercent(string section, string key, string value, string expected)
        => IniDisplayText.Format(IniEditorStyle.Friendly, new IniKeyId(section, key), value).ShouldBe(expected);

    [ Theory ]
    [ InlineData("user", "fullscreen", "1", "Fullscreen") ]
    [ InlineData("UI", "MSStartingCash", "10000", "$10,000") ]
    [ InlineData("user", "screenwidth", "800", "800") ]
    [ InlineData("language", "lang", "9", "9") ]
    [ InlineData("UI", "userAttenuation", "loud", "loud") ]
    [ InlineData("UI", "playMovie", "maybe", "maybe") ]
    [ InlineData("nope", "key", "abc", "abc") ]
    public void Format_Friendly_ChoicesCashAndUnmappableValues(string section, string key, string value, string expected)
        => IniDisplayText.Format(IniEditorStyle.Friendly, new IniKeyId(section, key), value).ShouldBe(expected);

    [ Theory ]
    [ InlineData("UI", "noMenuMusic", "1", "true") ]
    [ InlineData("UI", "userAttenuation", "2500", "2500") ]
    public void Format_Ini_IsUnchanged(string section, string key, string value, string expected)
        => IniDisplayText.Format(IniEditorStyle.Ini, new IniKeyId(section, key), value).ShouldBe(expected);
}
