namespace Erdmier.ZooTycoonLauncher.Domain.Tests.Unit.IniDocuments;

public sealed class IniDocumentTests
{
    private const string Sample = "; Zoo Tycoon settings\r\n"
                                  + "[user]\r\n"
                                  + "fullscreen = 1\r\n"
                                  + "screenwidth=800\r\n"
                                  + "\r\n"
                                  + "[UI]\r\n"
                                  + "tooltipDelay=1   \r\n"
                                  + "garbage line\r\n"
                                  + "[mgr]\r\n"
                                  + "foo=bar\r\n";

    [ Theory ]
    [ InlineData("") ]
    [ InlineData("\r\n") ]
    [ InlineData(Sample) ]
    [ InlineData("[user]\nfullscreen=1\n") ]
    [ InlineData("[user]\r\nfullscreen=1\nscreenwidth=800\r\n") ]
    [ InlineData("[user]\r\nfullscreen=1") ]
    [ InlineData("\u00EF\u00BB\u00BF[user]\r\nfullscreen=1\r\n") ]
    [ InlineData("orphan=1\r\n[user]\r\n[user]\r\nfullscreen=0\r\n# hash comment\r\n=novalue\r\n[broken\r\n") ]
    [ InlineData("[user]\rfullscreen=1\r") ]
    public void Render_ReturnsTheParsedTextByteForByte(string text) => IniDocument.Parse(text).Render().ShouldBe(text);

    [ Fact ]
    public void Parse_ClassifiesEveryLine()
    {
        IReadOnlyList<IniLine> lines = IniDocument.Parse(Sample).Lines;

        lines[0].ShouldBeOfType<IniComment>();
        lines[1].ShouldBeOfType<IniSectionHeader>().Name.ShouldBe(expected: "user");
        lines[2].ShouldBeOfType<IniKeyValue>().Value.ShouldBe(expected: "1");
        lines[4].ShouldBeOfType<IniBlank>();
        lines[7].ShouldBeOfType<IniComment>().RawText.ShouldBe(expected: "garbage line");
    }

    [ Fact ]
    public void TryGetValue_IsCaseInsensitiveAndTrims()
    {
        IniDocument document = IniDocument.Parse(Sample);

        document.TryGetValue(new IniKeyId(Section: "USER", Key: "FullScreen"), out string? value).ShouldBeTrue();
        value.ShouldBe(expected: "1");
        document.TryGetValue(new IniKeyId(Section: "ui", Key: "tooltipdelay"), out string? delay).ShouldBeTrue();
        delay.ShouldBe(expected: "1");
    }

    [ Fact ]
    public void TryGetValue_FirstOccurrenceWins()
    {
        IniDocument document = IniDocument.Parse("[user]\r\nfullscreen=0\r\nfullscreen=1\r\n");

        document.TryGetValue(new IniKeyId(Section: "user", Key: "fullscreen"), out string? value).ShouldBeTrue();
        value.ShouldBe(expected: "0");
    }

    [ Fact ]
    public void TryGetValue_KeysBeforeAnySectionAreNotFoundUnderANamedSection()
    {
        IniDocument document = IniDocument.Parse("fullscreen=1\r\n[user]\r\n");

        document.TryGetValue(new IniKeyId(Section: "user", Key: "fullscreen"), out string? _).ShouldBeFalse();
    }

    [ Fact ]
    public void SetValue_ExistingKey_RewritesOnlyTheValueSpan()
    {
        IniDocument document = IniDocument.Parse(Sample);

        document.SetValue(new IniKeyId(Section: "user", Key: "FULLSCREEN"), value: "0");
        document.SetValue(new IniKeyId(Section: "UI", Key: "tooltipDelay"), value: "5");

        document.Render().ShouldBe(Sample.Replace(oldValue: "fullscreen = 1", newValue: "fullscreen = 0").Replace(oldValue: "tooltipDelay=1   ", newValue: "tooltipDelay=5   "));
    }

    [ Fact ]
    public void SetValue_EmptyValue_WritesKeyEquals()
    {
        IniDocument document = IniDocument.Parse("[user]\r\nlastfile=C:\\a.zoo\r\n");

        document.SetValue(new IniKeyId(Section: "user", Key: "lastfile"), value: "");

        document.Render().ShouldBe(expected: "[user]\r\nlastfile=\r\n");
    }

    [ Fact ]
    public void SetValue_MissingKey_InsertsAfterTheSectionsLastKey()
    {
        IniDocument document = IniDocument.Parse(Sample);

        document.SetValue(new IniKeyId(Section: "user", Key: "DrawRate"), value: "30");

        document.Render().ShouldBe(Sample.Replace(oldValue: "screenwidth=800\r\n", newValue: "screenwidth=800\r\nDrawRate=30\r\n"));
    }

    [ Fact ]
    public void SetValue_MissingKey_UsesTheExistingSectionEvenWhenItsCasingDiffers()
    {
        IniDocument document = IniDocument.Parse("[ui]\r\ntooltipDelay=1\r\n");

        document.SetValue(new IniKeyId(Section: "UI", Key: "keyScrollX"), value: "64");

        document.Render().ShouldBe(expected: "[ui]\r\ntooltipDelay=1\r\nkeyScrollX=64\r\n");
    }

    [ Fact ]
    public void SetValue_MissingSection_AppendsBlankLineHeaderAndKey()
    {
        IniDocument document = IniDocument.Parse("[user]\r\nfullscreen=1\r\n");

        document.SetValue(new IniKeyId(Section: "Map", Key: "mapX"), value: "100");

        document.Render().ShouldBe(expected: "[user]\r\nfullscreen=1\r\n\r\n[Map]\r\nmapX=100\r\n");
    }

    [ Fact ]
    public void SetValue_OnEmptyDocument_WritesSectionAndKeyWithCrlf()
    {
        IniDocument document = IniDocument.Parse(text: "");

        document.SetValue(new IniKeyId(Section: "user", Key: "fullscreen"), value: "0");

        document.Render().ShouldBe(expected: "[user]\r\nfullscreen=0\r\n");
    }

    [ Fact ]
    public void SetValue_DocumentWithoutTrailingNewline_TerminatesThePreviousLastLineAndKeepsTheShape()
    {
        IniDocument document = IniDocument.Parse("[user]\nfullscreen=1");

        document.SetValue(new IniKeyId(Section: "user", Key: "screenwidth"), value: "1024");

        document.Render().ShouldBe(expected: "[user]\nfullscreen=1\nscreenwidth=1024");
    }

    [ Fact ]
    public void SetValue_UsesTheDominantLineEnding()
    {
        IniDocument document = IniDocument.Parse("[user]\nfullscreen=1\nscreenwidth=800\n");

        document.SetValue(new IniKeyId(Section: "Map", Key: "mapY"), value: "90");

        document.Render().ShouldBe(expected: "[user]\nfullscreen=1\nscreenwidth=800\n\n[Map]\nmapY=90\n");
    }

    [ Fact ]
    public void SetValue_PreservesABomPreamble()
    {
        IniDocument document = IniDocument.Parse("\u00EF\u00BB\u00BF[user]\r\nfullscreen=1\r\n");

        document.SetValue(new IniKeyId(Section: "user", Key: "fullscreen"), value: "0");

        document.Render().ShouldBe(expected: "\u00EF\u00BB\u00BF[user]\r\nfullscreen=0\r\n");
    }
}
