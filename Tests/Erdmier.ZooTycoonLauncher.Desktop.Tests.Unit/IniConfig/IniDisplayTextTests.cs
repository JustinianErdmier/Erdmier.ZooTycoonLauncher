namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.IniConfig;

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
        => IniDisplayText.Format(new IniKeyId(section, key), value).ShouldBe(expected);
}
