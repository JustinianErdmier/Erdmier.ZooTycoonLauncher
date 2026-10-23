namespace Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.IniConfig;

public sealed class IniEditorLayoutsTests
{
    public static TheoryData<IniEditorStyle> Styles => new() { IniEditorStyle.Friendly, IniEditorStyle.Ini };

    public static TheoryData<IniValueConversion> Conversions => new() { IniValueConversion.Invert, IniValueConversion.VolumeFromAttenuation, IniValueConversion.VolumeFromMovieVolume };

    private static IEnumerable<IniFieldDescriptor> FieldsOf(IniEditorStyle style) => IniEditorLayouts.Sections(style).SelectMany(section => section.Groups).SelectMany(group => group.Fields);

    [ Theory ]
    [ MemberData(nameof(Styles)) ]
    public void EachLayout_ShowsEveryUserSettingKeyOnce(IniEditorStyle style)
    {
        IniFieldDescriptor[] fields = FieldsOf(style).ToArray();

        foreach (IniKeySpec spec in ZooIniDefaults.Keys.Where(spec => spec.Role == IniKeyRole.UserSetting))
        {
            fields.Count(field => field.Id == spec.Id).ShouldBe(expected: 1, customMessage: $"{style.Name}: {spec.Id}");

            IniEditorLayouts.TryLocate(style, spec.Id, out IniFieldLocation? location).ShouldBeTrue(spec.Id.ToString());
            location!.Field.Id.ShouldBe(spec.Id);
        }

        fields.Where(field => field.Id is not null).Select(field => field.Id!.Value).ShouldAllBe(id => ZooIniDefaults.Keys.Any(spec => spec.Id == id));
    }

    [ Fact ]
    public void FriendlyLayout_MatchesTheSpecPageOrder()
        => IniEditorLayouts.Sections(IniEditorStyle.Friendly)
                           .Select(section => section.Title)
                           .ShouldBe([ "Display", "Graphics", "Sound", "New Zoos", "Controls", "Help & Messages", "Language", "Troubleshooting" ]);

    [ Fact ]
    public void FriendlyLayout_LanguagePageIsThePickerPlusHiddenCodes()
    {
        IniFieldDescriptor[] fields = IniEditorLayouts.Sections(IniEditorStyle.Friendly).Single(section => section.Title == "Language").Groups.SelectMany(group => group.Fields).ToArray();

        fields.Where(field => !field.IsHidden).Select(field => field.Control).ShouldBe([ IniControlKind.LanguagePicker ]);
        fields.Where(field => field.IsHidden).Select(field => field.Id).ShouldBe([ IniEditorCatalogue.LanguageId, IniEditorCatalogue.SubLanguageId ], ignoreOrder: true);
    }

    [ Theory ]
    [ MemberData(nameof(Conversions)) ]
    public void Conversion_RoundTrips(IniValueConversion conversion)
    {
        (int min, int max) = conversion == IniValueConversion.Invert ? (0, 1) : conversion == IniValueConversion.VolumeFromAttenuation ? (0, 10000) : (-10000, 0);

        for (int raw = min; raw <= max; raw++)
        {
            int display = conversion.ToDisplay(raw);

            conversion.ToRaw(display).ShouldBe(raw);

            if (conversion != IniValueConversion.Invert)
            {
                display.ShouldBeInRange(0, 10000);
            }
        }
    }

    [ Fact ]
    public void Conversion_None_IsTheIdentity() => IniValueConversion.None.ToDisplay(raw: 1234).ShouldBe(expected: 1234);

    [ Fact ]
    public void Conversions_FollowTheSpecFormulae()
    {
        IniValueConversion.VolumeFromAttenuation.ToDisplay(raw: 1500).ShouldBe(expected: 8500);
        IniValueConversion.VolumeFromMovieVolume.ToDisplay(raw: -1000).ShouldBe(expected: 9000);
        IniValueConversion.Invert.ToDisplay(raw: 1).ShouldBe(expected: 0);
    }

    [ Fact ]
    public void FriendlyLayout_ConversionsAreOnTheFourVolumesAndNoMenuMusic()
        => FieldsOf(IniEditorStyle.Friendly)
           .Where(field => field.Conversion != IniValueConversion.None)
           .ToDictionary(field => field.Id!.Value.Key, field => field.Conversion)
           .ShouldBe(new Dictionary<string, IniValueConversion>
                     {
                         ["noMenuMusic"]          = IniValueConversion.Invert,
                         ["menuMusicAttenuation"] = IniValueConversion.VolumeFromAttenuation,
                         ["userAttenuation"]      = IniValueConversion.VolumeFromAttenuation,
                         ["movievolume1"]         = IniValueConversion.VolumeFromMovieVolume,
                         ["movievolume2"]         = IniValueConversion.VolumeFromMovieVolume
                     },
                     ignoreOrder: true);

    [ Fact ]
    public void FriendlyLayout_VolumeRowsAreSliders()
    {
        string[] volumes = [ "menuMusicAttenuation", "userAttenuation", "movievolume1", "movievolume2" ];

        FieldsOf(IniEditorStyle.Friendly).Where(field => field.Control == IniControlKind.Slider).Select(field => field.Id!.Value.Key).ShouldBe(volumes, ignoreOrder: true);
        FieldsOf(IniEditorStyle.Friendly).Where(field => field.Control == IniControlKind.Slider).ShouldAllBe(field => field.Hint == "Silent – Full");
    }

    [ Fact ]
    public void IniLayout_NoMenuMusicIsLiteral()
    {
        IniEditorLayouts.TryLocate(IniEditorStyle.Ini, new IniKeyId(Section: "UI", Key: "noMenuMusic"), out IniFieldLocation? location).ShouldBeTrue();

        IniFieldDescriptor field = location!.Field;

        field.Conversion.ShouldBe(IniValueConversion.None);
        field.Caption.ShouldBe(expected: "No menu music");
        field.Hint.ShouldBe(expected: "1 = no menu music");
        field.Label.ShouldBe(expected: "noMenuMusic");
    }

    [ Fact ]
    public void IniLayout_HasNoSlidersAndTitlesAreBracketed()
    {
        FieldsOf(IniEditorStyle.Ini).ShouldNotContain(field => field.Control == IniControlKind.Slider);
        IniEditorLayouts.Sections(IniEditorStyle.Ini).ShouldAllBe(section => section.Title == $"[{section.Section}]");
    }

    [ Fact ]
    public void FriendlyLayout_FootnotesSitOnTheirGroups()
    {
        IniFieldGroupDescriptor[] groups = IniEditorLayouts.Sections(IniEditorStyle.Friendly).SelectMany(section => section.Groups).ToArray();

        groups.Single(group => group.SubHeader == "Guests").Footnote.ShouldNotBeNullOrWhiteSpace();
        groups.Single(group => group.SubHeader == "Map size").Footnote.ShouldNotBeNullOrWhiteSpace();
    }

    [ Fact ]
    public void FriendlyLayout_GuestsAndMapSizeFootnotesEqualTheIniSectionFootnotes()
    {
        IniFieldGroupDescriptor[] groups = IniEditorLayouts.Sections(IniEditorStyle.Friendly).SelectMany(section => section.Groups).ToArray();
        IniSectionDescriptor[]    pages  = IniEditorLayouts.Sections(IniEditorStyle.Ini).ToArray();

        string? ai  = pages.Single(section => section.Section == "ai").Footnote;
        string? map = pages.Single(section => section.Section == "Map").Footnote;

        ai.ShouldNotBeNullOrWhiteSpace();
        map.ShouldNotBeNullOrWhiteSpace();
        groups.Single(group => group.SubHeader == "Guests").Footnote.ShouldBe(ai);
        groups.Single(group => group.SubHeader == "Map size").Footnote.ShouldBe(map);
    }

    [ Fact ]
    public void Layouts_AgreeOnEachKeysControlAndCurrencyExceptTheFourVolumes()
    {
        string[] volumes = [ "menuMusicAttenuation", "userAttenuation", "movievolume1", "movievolume2" ];

        foreach (IniFieldDescriptor friendly in FieldsOf(IniEditorStyle.Friendly).Where(field => field.Id is not null))
        {
            IniKeyId id = friendly.Id!.Value;

            IniEditorLayouts.TryLocate(IniEditorStyle.Ini, id, out IniFieldLocation? location).ShouldBeTrue(id.ToString());

            IniFieldDescriptor ini = location!.Field;

            ini.IsCurrency.ShouldBe(friendly.IsCurrency, id.ToString());

            if (volumes.Contains(id.Key))
            {
                friendly.Control.ShouldBe(IniControlKind.Slider, id.ToString());
                ini.Control.ShouldBe(IniControlKind.Number, id.ToString());
            }
            else
            {
                friendly.Control.ShouldBe(ini.Control, id.ToString());
            }
        }
    }

    [ Fact ]
    public void FriendlyLayout_EveryVisibleRowHasLabelAndHelp()
        => FieldsOf(IniEditorStyle.Friendly).Where(field => !field.IsHidden).ShouldAllBe(field => !string.IsNullOrWhiteSpace(field.Label) && !string.IsNullOrWhiteSpace(field.Help));
}
