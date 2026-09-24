namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.IniKeys;

public sealed class IniKeyIdTests
{
    [ Fact ]
    public void Equals_IgnoresCaseOnSectionAndKey()
    {
        IniKeyId registry = new(Section: "UI", Key: "tooltipDelay");
        IniKeyId fromFile = new(Section: "ui", Key: "TOOLTIPDELAY");

        (registry == fromFile).ShouldBeTrue();
        registry.GetHashCode().ShouldBe(fromFile.GetHashCode());
    }

    [ Fact ]
    public void Equals_DistinguishesDifferentKeys()
    {
        IniKeyId left  = new(Section: "UI", Key: "keyScrollX");
        IniKeyId right = new(Section: "UI", Key: "keyScrollY");

        (left == right).ShouldBeFalse();
    }

    [ Fact ]
    public void ToString_FormatsAsBracketedSectionSlashKey()
    {
        IniKeyId id = new(Section: "UI", Key: "tooltipDelay");

        id.ToString().ShouldBe(expected: "[UI]/tooltipDelay");
    }

    [ Fact ]
    public void Default_HasAStableHashCode()
    {
        IniKeyId id = default;

        Should.NotThrow(() => id.GetHashCode());
    }
}
