namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.IniKeys;

public sealed class ZooIniDefaultDocumentTests
{
    [ Fact ]
    public void Render_StartsWithTheUserSection()
    {
        IniKeySpec first = ZooIniDefaults.Keys[0];

        ZooIniDefaultDocument.Render().ShouldStartWith($"[user]\r\n{first.Id.Key}={first.DefaultValue}\r\n");
    }

    [ Fact ]
    public void Render_UsesCrlfOnly()
    {
        string text = ZooIniDefaultDocument.Render();

        text.Replace(oldValue: "\r\n", newValue: string.Empty).ShouldNotContain("\n");
        text.ShouldEndWith("\r\n");
        text.ShouldContain("\r\n\r\n[UI]\r\n");
    }

    [ Fact ]
    public void Render_OmitsKeysWithNoDefault()
    {
        string text = ZooIniDefaultDocument.Render();

        text.ShouldNotContain("lastfile=");
        text.ShouldNotContain("lastWindowX=");
        text.ShouldNotContain("progresscalls=");
        text.ShouldContain("showUserEntityWarning=0");
        text.ShouldContain("startedFirstTutorial=0");
    }

    [ Fact ]
    public void Create_ExtractsEveryDefault()
    {
        IReadOnlyDictionary<IniKeyId, string?> values = ZooIniDefaults.ExtractValues(ZooIniDefaultDocument.Create());

        foreach (IniKeySpec spec in ZooIniDefaults.Keys.Where(spec => spec.DefaultValue is not null))
        {
            values[spec.Id].ShouldBe(spec.DefaultValue);
        }
    }

    [ Fact ]
    public void Render_HasNoScenarioSection() => ZooIniDefaultDocument.Render().ShouldNotContain("[scenario]");
}
