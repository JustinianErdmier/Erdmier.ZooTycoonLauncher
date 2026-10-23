namespace Erdmier.ZooTycoonNexus.Domain.Tests.Unit.IniKeys;

public sealed class ZooIniDefaultDocumentTests
{
    [ Fact ]
    public void Render_StartsWithTheUserSection()
    {
        IniKeySpec first = ZooIniDefaults.Keys[0];

        ZooIniDefaultDocument.Render().ShouldStartWith(expected: $"[user]\r\n{first.Id.Key}={first.DefaultValue}\r\n");
    }

    [ Fact ]
    public void Render_UsesCrlfOnly()
    {
        string text = ZooIniDefaultDocument.Render();

        text.Replace(oldValue: "\r\n", newValue: string.Empty).ShouldNotContain(expected: "\n");
        text.ShouldEndWith(expected: "\r\n");
        text.ShouldContain(expected: "\r\n\r\n[UI]\r\n");
    }

    [ Fact ]
    public void Render_OmitsKeysWithNoDefault()
    {
        string text = ZooIniDefaultDocument.Render();

        text.ShouldNotContain(expected: "lastfile=");
        text.ShouldNotContain(expected: "lastWindowX=");
        text.ShouldNotContain(expected: "progresscalls=");
        text.ShouldContain(expected: "showUserEntityWarning=0");
        text.ShouldContain(expected: "startedFirstTutorial=0");
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
    public void Render_HasNoScenarioSection() => ZooIniDefaultDocument.Render().ShouldNotContain(expected: "[scenario]");
}
