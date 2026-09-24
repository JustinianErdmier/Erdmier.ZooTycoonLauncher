namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.IniConfig.Fields;

public sealed class IniFieldViewModelTests
{
    private static IniFieldDescriptor Descriptor(string section, string key)
        => IniEditorCatalogue.Sections.SelectMany(s => s.Groups).SelectMany(g => g.Fields).Single(field => field.Id == new IniKeyId(section, key));

    [ Fact ]
    public void Toggle_TracksDirtyAgainstTheBaseline()
    {
        IniToggleFieldViewModel field = new(Descriptor(section: "advanced", key: "drag"));

        field.Load(raw: "1");

        field.IsChecked.ShouldBeTrue();
        field.IsDirty.ShouldBeFalse();

        field.IsChecked = false;

        field.IsDirty.ShouldBeTrue();
        field.TryGetEdit(out string? edit).ShouldBeTrue();
        edit.ShouldBe(expected: "0");

        field.IsChecked = true;

        field.IsDirty.ShouldBeFalse();
        field.TryGetEdit(out string? _).ShouldBeFalse();
    }

    [ Fact ]
    public void Toggle_Inverted_ShowsTheOppositeAndWritesTheFaithfulValue()
    {
        IniToggleFieldViewModel field = new(Descriptor(section: "UI", key: "noMenuMusic"));

        field.Load(raw: "0");

        field.Caption.ShouldBe(expected: "Play menu music");
        field.IsChecked.ShouldBeTrue();

        field.IsChecked = false;

        field.TryGetEdit(out string? edit).ShouldBeTrue();
        edit.ShouldBe(expected: "1");
    }

    [ Fact ]
    public void Toggle_TrueTextIsEquivalentToOne()
    {
        IniToggleFieldViewModel field = new(Descriptor(section: "advanced", key: "drag"));

        field.Load(raw: "true");

        field.IsChecked.ShouldBeTrue();
        field.IsDirty.ShouldBeFalse();
    }

    [ Fact ]
    public void Number_InvalidBaseline_ShowsTheDefaultAndStaysClean()
    {
        IniNumberFieldViewModel field = new(Descriptor(section: "user", key: "screenwidth"));

        field.Load(raw: "abc");

        field.Value.ShouldBe(expected: 800m);
        field.IsDirty.ShouldBeFalse();
        field.TryGetEdit(out string? _).ShouldBeFalse();
    }

    [ Fact ]
    public void Number_EditsAndBoundsComeFromTheSpec()
    {
        IniNumberFieldViewModel field = new(Descriptor(section: "user", key: "UpdateRate"));

        field.Load(raw: "15");

        field.Minimum.ShouldBe(expected: 1m);
        field.Maximum.ShouldBe(expected: 60m);

        field.Value = 30m;

        field.TryGetEdit(out string? edit).ShouldBeTrue();
        edit.ShouldBe(expected: "30");

        field.Value = 15m;

        field.IsDirty.ShouldBeFalse();
    }

    [ Fact ]
    public void Number_ClearedBox_IsAnEmptyEdit()
    {
        IniNumberFieldViewModel field = new(Descriptor(section: "user", key: "screenwidth"));

        field.Load(raw: "800");

        field.Value = null;

        field.TryGetEdit(out string? edit).ShouldBeTrue();
        edit.ShouldBe(expected: "");
    }

    [ Fact ]
    public void Text_AbsentKey_ShowsTheDefault()
    {
        IniTextFieldViewModel field = new(Descriptor(section: "UI", key: "menuMusic"));

        field.Load(raw: null);

        field.Value.ShouldBe(expected: "sounds/mainmenu.wav");
        field.IsDirty.ShouldBeFalse();
    }

    [ Fact ]
    public void Choice_SelectsTheEquivalentOptionAndEditsItsRaw()
    {
        IniChoiceFieldViewModel field = new(Descriptor(section: "user", key: "fullscreen"));

        field.Load(raw: "true");

        field.SelectedOption!.Label.ShouldBe(expected: "Fullscreen");
        field.IsDirty.ShouldBeFalse();

        field.SelectedOption = field.Options.Single(option => option.Label == "Windowed");

        field.TryGetEdit(out string? edit).ShouldBeTrue();
        edit.ShouldBe(expected: "0");
    }

    [ Fact ]
    public void Choice_Level_ShowsTheLabelledPreset()
    {
        IniChoiceFieldViewModel field = new(Descriptor(section: "advanced", key: "level"));

        field.Load(raw: "3");

        field.SelectedOption!.Label.ShouldBe(expected: "3 – Speed");
    }

    [ Fact ]
    public void Reset_RestoresTheBaseline()
    {
        IniNumberFieldViewModel field = new(Descriptor(section: "Map", key: "mapX"));

        field.Load(raw: "90");

        field.Value = 100m;
        field.Reset();

        field.Value.ShouldBe(expected: 90m);
        field.IsDirty.ShouldBeFalse();
    }

    [ Fact ]
    public void IsDirtyChanges_RaisePropertyChanged()
    {
        IniToggleFieldViewModel field   = new(Descriptor(section: "advanced", key: "drag"));
        List<string?>           changes = [];

        field.Load(raw: "0");

        field.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        field.IsChecked = true;

        changes.ShouldContain(nameof(IniFieldViewModel.IsDirty));
    }

    [ Fact ]
    public void LanguagePicker_DrivesAndFollowsTheTwoNumberRows()
    {
        IniNumberFieldViewModel language    = new(Descriptor(section: "language", key: "lang"));
        IniNumberFieldViewModel subLanguage = new(Descriptor(section: "language", key: "sublang"));

        IniLanguageFieldViewModel picker = new(IniEditorCatalogue.Sections.Single(s => s.Section == "language").Groups[0].Fields[0],
                                               language,
                                               subLanguage,
                                               IniEditorCatalogue.Languages);

        language.Load(raw: "9");
        subLanguage.Load(raw: "1");

        picker.SelectedOption!.Label.ShouldBe(expected: "English (United States)");

        picker.SelectedOption = IniEditorCatalogue.Languages.Single(option => option.Label == "French (France)");

        language.Value.ShouldBe(expected: 12m);
        subLanguage.Value.ShouldBe(expected: 1m);
        language.IsDirty.ShouldBeTrue();

        language.Value = 99m;

        picker.SelectedOption.ShouldBeNull();
        picker.IsDirty.ShouldBeFalse();
        picker.TryGetEdit(out string? _).ShouldBeFalse();
    }
}
