namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.Settings;

public sealed class IniEditorStyleTests
{
    [ Fact ]
    public void IniEditorStyle_HasFriendlyAndIni()
    {
        IniEditorStyle.List.Select(style => style.Name).ShouldBe(["Friendly", "Ini"], ignoreOrder: true);

        IniEditorStyle.Friendly.Value.ShouldBe(expected: 1);
        IniEditorStyle.Ini.Value.ShouldBe(expected: 2);

        IniEditorStyle.Friendly.DisplayName.ShouldBe(expected: "Friendly style");
        IniEditorStyle.Ini.DisplayName.ShouldBe(expected: "INI style");
    }

    [ Fact ]
    public void LauncherSettings_DefaultsToFriendly()
        => new LauncherSettings().IniEditorStyle.ShouldBe(IniEditorStyle.Friendly);
}
