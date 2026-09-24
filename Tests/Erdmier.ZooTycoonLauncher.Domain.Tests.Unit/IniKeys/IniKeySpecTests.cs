namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.IniKeys;

public sealed class IniKeySpecTests
{
    private static readonly IniKeySpec BoolSpec = new(new IniKeyId(Section: "user", Key: "fullscreen"), IniValueKind.Bool, DefaultValue: "1", Min: null, Max: null,
                                                      IniKeyRole.UserSetting);

    private static readonly IniKeySpec IntSpec = new(new IniKeyId(Section: "user", Key: "UpdateRate"), IniValueKind.Int, DefaultValue: "15", Min: 1, Max: 60,
                                                     IniKeyRole.UserSetting);

    private static readonly IniKeySpec NullableIntSpec = new(new IniKeyId(Section: "UI", Key: "lastWindowX"), IniValueKind.NullableInt, DefaultValue: null, Min: null,
                                                             Max: null, IniKeyRole.GameManaged);

    private static readonly IniKeySpec StrSpec = new(new IniKeyId(Section: "UI", Key: "menuMusic"), IniValueKind.Str, DefaultValue: "sounds/mainmenu.wav", Min: null,
                                                     Max: null, IniKeyRole.UserSetting);

    private static readonly IniKeySpec NullableStrSpec = new(new IniKeyId(Section: "user", Key: "lastfile"), IniValueKind.NullableStr, DefaultValue: null, Min: null,
                                                             Max: null, IniKeyRole.GameManaged);

    [ Theory ]
    [ InlineData("0", true) ]
    [ InlineData("1", true) ]
    [ InlineData("TRUE", true) ]
    [ InlineData(" false ", true) ]
    [ InlineData("2", false) ]
    [ InlineData("", false) ]
    [ InlineData(null, false) ]
    public void IsValid_Bool(string? raw, bool expected) => BoolSpec.IsValid(raw).ShouldBe(expected);

    [ Theory ]
    [ InlineData("1", true) ]
    [ InlineData("60", true) ]
    [ InlineData(" 015 ", true) ]
    [ InlineData("0", false) ]
    [ InlineData("61", false) ]
    [ InlineData("abc", false) ]
    [ InlineData("", false) ]
    [ InlineData(null, false) ]
    public void IsValid_BoundedInt(string? raw, bool expected) => IntSpec.IsValid(raw).ShouldBe(expected);

    [ Theory ]
    [ InlineData(null, true) ]
    [ InlineData("", true) ]
    [ InlineData("-40", true) ]
    [ InlineData("x", false) ]
    public void IsValid_NullableInt(string? raw, bool expected) => NullableIntSpec.IsValid(raw).ShouldBe(expected);

    [ Theory ]
    [ InlineData("", true) ]
    [ InlineData("sounds/other.wav", true) ]
    [ InlineData(null, false) ]
    public void IsValid_Str(string? raw, bool expected) => StrSpec.IsValid(raw).ShouldBe(expected);

    [ Theory ]
    [ InlineData(null, true) ]
    [ InlineData("", true) ]
    [ InlineData(@"C:\Saves\zoo1.zoo", true) ]
    public void IsValid_NullableStr(string? raw, bool expected) => NullableStrSpec.IsValid(raw).ShouldBe(expected);

    [ Theory ]
    [ InlineData("1", "true", true) ]
    [ InlineData("0", "FALSE", true) ]
    [ InlineData("1", "0", false) ]
    [ InlineData("1", null, false) ]
    [ InlineData(null, null, true) ]
    public void AreEquivalent_Bool(string? left, string? right, bool expected) => BoolSpec.AreEquivalent(left, right).ShouldBe(expected);

    [ Theory ]
    [ InlineData("75", "075", true) ]
    [ InlineData(" 15", "15 ", true) ]
    [ InlineData("15", "16", false) ]
    [ InlineData("abc", "abc", true) ]
    [ InlineData("abc", "15", false) ]
    [ InlineData("15", null, false) ]
    public void AreEquivalent_Int(string? left, string? right, bool expected) => IntSpec.AreEquivalent(left, right).ShouldBe(expected);

    [ Theory ]
    [ InlineData("", null, true) ]
    [ InlineData("  ", null, true) ]
    [ InlineData("10", "010", true) ]
    [ InlineData("10", null, false) ]
    public void AreEquivalent_NullableInt(string? left, string? right, bool expected) => NullableIntSpec.AreEquivalent(left, right).ShouldBe(expected);

    [ Theory ]
    [ InlineData("a", " a ", true) ]
    [ InlineData("a", "A", false) ]
    [ InlineData("", null, false) ]
    public void AreEquivalent_Str(string? left, string? right, bool expected) => StrSpec.AreEquivalent(left, right).ShouldBe(expected);

    [ Theory ]
    [ InlineData("", null, true) ]
    [ InlineData("x", "x", true) ]
    [ InlineData("x", null, false) ]
    public void AreEquivalent_NullableStr(string? left, string? right, bool expected) => NullableStrSpec.AreEquivalent(left, right).ShouldBe(expected);

    [ Theory ]
    [ InlineData("30", "30") ]
    [ InlineData(" 30 ", "30") ]
    [ InlineData("99", "15") ]
    [ InlineData("abc", "15") ]
    [ InlineData(null, "15") ]
    public void EffectiveValue_Int_FallsBackToDefaultWhenInvalid(string? raw, string expected) => IntSpec.EffectiveValue(raw).ShouldBe(expected);

    [ Fact ]
    public void EffectiveValue_NullableKinds_KeepAbsentAsNull()
    {
        NullableIntSpec.EffectiveValue(raw: null).ShouldBeNull();
        NullableStrSpec.EffectiveValue(raw: null).ShouldBeNull();
    }
}
