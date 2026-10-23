namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.IniConfig.Fields;

public sealed class IniLanguageFieldViewModelTests
{
    private static IniLanguageFieldViewModel Language(IniEditorViewModel editor) => editor.Sections.SelectMany(section => section.Fields).OfType<IniLanguageFieldViewModel>().Single();

    private static IniEditorViewModel Editor(IniEditorStyle style)
        => new(Guid.CreateVersion7(), Substitute.For<IMediator>(), Substitute.For<IDialogService>(),
               IniEditorTestData.Result(("language", "lang", "9"), ("language", "sublang", "1")), NullLogger<IniEditorViewModel>.Instance, style);

    [ Fact ]
    public void LanguageField_CodesTextShowsCurrentValues()
    {
        IniEditorViewModel editor = Editor(IniEditorStyle.Friendly);

        IniLanguageFieldViewModel language = Language(editor);

        language.CodesText.ShouldBe(expected: "Windows language codes: LANGID 9 · SUBLANGID 1");

        language.SelectedOption = IniEditorCatalogue.Languages.First(option => option is { Lang: 7, SubLang: 1 });

        language.CodesText.ShouldBe(expected: "Windows language codes: LANGID 7 · SUBLANGID 1");
    }

    [ Fact ]
    public void LanguageField_ShowsCodeRowsOnlyInTheIniStyle()
    {
        Language(Editor(IniEditorStyle.Friendly)).ShowCodeRows.ShouldBeFalse();
        Language(Editor(IniEditorStyle.Ini)).ShowCodeRows.ShouldBeTrue();
    }

    [ Fact ]
    public void FriendlyLanguagePage_BuildsButHidesTheCodeRows()
    {
        IniEditorViewModel editor = Editor(IniEditorStyle.Friendly);

        IniSectionViewModel page = editor.Sections.Single(section => section.Title == "Language");

        page.Fields.Count().ShouldBe(expected: 3);
        page.Groups.Single().VisibleFields.ShouldHaveSingleItem().ShouldBeOfType<IniLanguageFieldViewModel>();
    }
}
