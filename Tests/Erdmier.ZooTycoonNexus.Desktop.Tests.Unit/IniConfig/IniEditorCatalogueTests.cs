namespace Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.IniConfig;

public sealed class IniEditorCatalogueTests
{
    private static IEnumerable<IniFieldDescriptor> AllFields => IniEditorCatalogue.Sections.SelectMany(section => section.Groups).SelectMany(group => group.Fields);

    [ Fact ]
    public void Sections_FollowTheRegistryOrder() => IniEditorCatalogue.Sections.Select(section => section.Section).ShouldBe(ZooIniDefaults.Sections);

    [ Fact ]
    public void EveryUserSetting_HasExactlyOneDescriptor()
    {
        foreach (IniKeySpec spec in ZooIniDefaults.Keys.Where(spec => spec.Role == IniKeyRole.UserSetting))
        {
            AllFields.Count(field => field.Id == spec.Id).ShouldBe(expected: 1, customMessage: spec.Id.ToString());
        }
    }

    [ Fact ]
    public void EveryKeyedDescriptor_NamesAUserSettingInItsOwnSection()
    {
        foreach (IniSectionDescriptor section in IniEditorCatalogue.Sections)
        {
            foreach (IniFieldDescriptor field in section.Groups.SelectMany(group => group.Fields).Where(field => field.Id is not null))
            {
                ZooIniDefaults.TryGet(field.Id!.Value, out IniKeySpec? spec).ShouldBeTrue(field.Label);
                spec!.Role.ShouldBe(IniKeyRole.UserSetting, field.Label);
                spec.Id.Section.ShouldBe(section.Section, field.Label);
                field.Label.ShouldBe(spec.Id.Key);
            }
        }
    }

    [ Fact ]
    public void OnlyLanguagePickers_HaveNoId()
        => AllFields.Where(field => field.Id is null).ShouldAllBe(field => field.Control == IniControlKind.LanguagePicker);

    [ Fact ]
    public void OnlyTheLanguageCodes_AreReadOnly()
        => AllFields.Where(field => field.IsReadOnly)
                    .Select(field => field.Id)
                    .ShouldBe([ IniEditorCatalogue.LanguageId, IniEditorCatalogue.SubLanguageId ], ignoreOrder: true);

    [ Fact ]
    public void CashRows_AreCurrency()
    {
        IniFieldDescriptor[] cash = AllFields.Where(field => field.IsCurrency).ToArray();

        cash.Select(field => (field.Id!.Value, field.Hint))
            .ShouldBe([ (new IniKeyId(Section: "UI", Key: "MSStartingCash"), "$0–$10,000,000"), (new IniKeyId(Section: "UI", Key: "MSCashIncrement"), "$100–$1,000,000"),
                        (new IniKeyId(Section: "UI", Key: "MSMinCash"), "$0–$10,000,000"), (new IniKeyId(Section: "UI", Key: "MSMaxCash"), "$0–$10,000,000") ],
                      ignoreOrder: true);
    }

    [ Fact ]
    public void CashHelp_IsTheSameMeaningInBothLayouts()
    {
        foreach (IReadOnlyList<IniSectionDescriptor> layout in new[] { IniEditorCatalogue.Sections, IniEditorFriendlyCatalogue.Sections })
        {
            string Help(string key) => layout.SelectMany(section => section.Groups).SelectMany(group => group.Fields).Single(field => field.Id == new IniKeyId(Section: "UI", Key: key)).Help;

            Help(key: "MSStartingCash").ShouldContain("new zoo");
            Help(key: "MSStartingCash").ShouldContain("change");
            Help(key: "MSCashIncrement").ShouldContain("starting cash");
            Help(key: "MSCashIncrement").ShouldContain("step");
            Help(key: "MSMinCash").ShouldContain("least starting cash");
            Help(key: "MSMaxCash").ShouldContain("most starting cash");
        }
    }

    [ Fact ]
    public void ChoiceOptions_AreValidForTheirKey()
    {
        foreach (IniFieldDescriptor field in AllFields.Where(field => field.Control == IniControlKind.Choice))
        {
            ZooIniDefaults.TryGet(field.Id!.Value, out IniKeySpec? spec).ShouldBeTrue();
            field.Options.ShouldNotBeEmpty(field.Label);

            foreach (IniChoiceOption option in field.Options)
            {
                spec!.IsValid(option.Raw).ShouldBeTrue($"{field.Label}: {option.Raw}");
            }
        }
    }

    [ Fact ]
    public void EveryDescriptor_HasHelpText() => AllFields.ShouldAllBe(field => !string.IsNullOrWhiteSpace(field.Help));

    [ Fact ]
    public void Languages_AreInRange() => IniEditorCatalogue.Languages.ShouldAllBe(language => language.Lang >= 0 && language.Lang <= 65535 && language.SubLang >= 0);

    [ Fact ]
    public void AiSection_FootnoteWarnsAboutRaisingTheGuestLimit()
        => IniEditorCatalogue.Sections.Single(section => section.Section == "ai").Footnote
                             .ShouldBe(expected: "Raising this above the stock 1,000 limit can make guest pathing thrash; values above 2,500 need a community AI patch.");

    [ Fact ]
    public void UiSection_HasTheThreeSubHeaders()
        => IniEditorCatalogue.Sections.Single(section => section.Section == "UI").Groups.Select(group => group.SubHeader).ShouldBe(["Audio", "Gameplay (cash)", "Interface"]);
}
