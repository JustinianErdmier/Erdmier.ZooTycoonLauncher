namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.IniConfig.Fields;

public sealed class IniSliderFieldViewModelTests
{
    private static IniSliderFieldViewModel Create(string key)
    {
        IniEditorLayouts.TryLocate(IniEditorStyle.Friendly, new IniKeyId(Section: "UI", Key: key), out IniFieldLocation? location).ShouldBeTrue();

        return new IniSliderFieldViewModel(location!.Field);
    }

    [ Fact ]
    public void Load_ShowsThePercentOfTheConvertedValue()
    {
        IniSliderFieldViewModel music = Create(key: "menuMusicAttenuation");
        IniSliderFieldViewModel movie = Create(key: "movievolume1");

        music.Load(raw: "2500");
        movie.Load(raw: "-10000");

        music.Percent.ShouldBe(expected: 75);
        music.PercentText.ShouldBe(expected: "75 %");
        movie.Percent.ShouldBe(expected: 0);
        music.IsDirty.ShouldBeFalse();
    }

    [ Fact ]
    public void MovingTheSlider_WritesRawInStepsOf100()
    {
        IniSliderFieldViewModel music = Create(key: "menuMusicAttenuation");
        IniSliderFieldViewModel movie = Create(key: "movievolume1");

        music.Load(raw: "0");
        movie.Load(raw: "0");

        music.Percent = 75;
        movie.Percent = 30;

        music.TryGetEdit(out string? musicEdit).ShouldBeTrue();
        movie.TryGetEdit(out string? movieEdit).ShouldBeTrue();
        musicEdit.ShouldBe(expected: "2500");
        movieEdit.ShouldBe(expected: "-7000");
    }

    [ Fact ]
    public void OffStepValue_IsKeptUntilMoved()
    {
        IniSliderFieldViewModel field = Create(key: "menuMusicAttenuation");

        field.Load(raw: "1234");

        field.Percent.ShouldBe(expected: 88);
        field.IsDirty.ShouldBeFalse();
        field.TryGetEdit(out string? _).ShouldBeFalse();

        field.Percent = 87;

        field.TryGetEdit(out string? edit).ShouldBeTrue();
        edit.ShouldBe(expected: "1300");
    }

    [ Fact ]
    public void Reset_RestoresTheRawBaseline()
    {
        IniSliderFieldViewModel field = Create(key: "userAttenuation");

        field.Load(raw: "1234");
        field.Percent = 10;
        field.IsDirty.ShouldBeTrue();

        field.Reset();

        field.IsDirty.ShouldBeFalse();
        field.TryGetEdit(out string? _).ShouldBeFalse();
        field.Percent.ShouldBe(expected: 88);
    }
    [ Fact ]
    public void ResetToDefault_GoesThroughTheConversion()
    {
        IniSliderFieldViewModel music = Create(key: "menuMusicAttenuation");
        IniSliderFieldViewModel movie = Create(key: "movievolume1");

        music.Load(raw: "0");
        movie.Load(raw: "0");

        music.ResetToDefault();
        movie.ResetToDefault();

        music.Percent.ShouldBe(expected: 85);
        music.IsAtDefault.ShouldBeTrue();
        music.IsDirty.ShouldBeTrue();
        music.TryGetEdit(out string? musicEdit).ShouldBeTrue();
        musicEdit.ShouldBe(expected: "1500");
        movie.Percent.ShouldBe(expected: 90);
        movie.IsAtDefault.ShouldBeTrue();
    }

    [ Fact ]
    public void MovingBackToTheLoadedPosition_StillWritesTheStepValue()
    {
        IniSliderFieldViewModel field = Create(key: "menuMusicAttenuation");

        field.Load(raw: "1234");

        field.Percent = 87;
        field.Percent = 88;

        field.IsDirty.ShouldBeTrue();
        field.TryGetEdit(out string? edit).ShouldBeTrue();
        edit.ShouldBe(expected: "1200");
    }
}