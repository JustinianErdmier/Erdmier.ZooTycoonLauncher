namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.IniConfig;

public sealed class IniEditorViewModelTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private IniEditorViewModel CreateEditor()
        => new(Guid.CreateVersion7(), _mediator, _dialogs, IniEditorTestData.Result(("user", "screenwidth", "800"), ("user", "fullscreen", "1")));

    [ Fact ]
    public void Constructor_BuildsTheSevenSectionsSelectsTheFirstAndLoadsValues()
    {
        IniEditorViewModel editor = CreateEditor();

        editor.Sections.Select(section => section.Header).ShouldBe(["[user]", "[UI]", "[advanced]", "[ai]", "[debug]", "[language]", "[Map]"]);
        editor.SelectedSection.ShouldBeSameAs(editor.Sections[0]);
        editor.Sections.SelectMany(section => section.Fields).Count().ShouldBe(expected: 47);
        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value.ShouldBe(expected: 800m);
        editor.HasPendingChanges.ShouldBeFalse();
        editor.SaveCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public void Footer_PrefersHelpThenDirtyThenSaved()
    {
        IniEditorViewModel      editor = CreateEditor();
        IniNumberFieldViewModel width  = IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth");

        editor.IsFooterSaved.ShouldBeTrue();
        editor.FooterText.ShouldStartWith(expected: "All changes saved · Last write: ");

        width.Value = 1024m;

        editor.IsFooterDirty.ShouldBeTrue();
        editor.FooterText.ShouldBe(expected: "● Unsaved changes");
        editor.SaveCommand.CanExecute(parameter: null).ShouldBeTrue();

        width.IsHelpActive = true;

        editor.IsFooterHelp.ShouldBeTrue();
        editor.FooterText.ShouldBe(width.Help);

        width.IsHelpActive = false;

        editor.FooterText.ShouldBe(expected: "● Unsaved changes");
    }

    [ Fact ]
    public void ChangingSection_ClearsTheHoveredHelp()
    {
        IniEditorViewModel      editor = CreateEditor();
        IniNumberFieldViewModel width  = IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth");

        width.IsHelpActive = true;

        editor.IsFooterHelp.ShouldBeTrue();

        editor.SelectedSection = editor.Sections[1];

        editor.IsFooterHelp.ShouldBeFalse();
        editor.IsFooterSaved.ShouldBeTrue();
        editor.FooterText.ShouldStartWith(expected: "All changes saved · Last write: ");
    }

    [ Fact ]
    public void ChangingSectionAwayAndBack_HoveringTheSameRowAgain_ShowsItsHelp()
    {
        IniEditorViewModel      editor = CreateEditor();
        IniNumberFieldViewModel width  = IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth");

        width.IsHelpActive = true;

        editor.SelectedSection = editor.Sections[1];
        editor.SelectedSection = editor.Sections[0];

        width.IsHelpActive = true;

        editor.IsFooterHelp.ShouldBeTrue();
    }

    [ Fact ]
    public async Task Save_Success_SendsOnlyTheEditsAndResetsTheBaseline()
    {
        IniEditorViewModel editor = CreateEditor();
        SaveIniCommand?    sent   = null;

        _mediator.Send(Arg.Do<SaveIniCommand>(command => sent = command), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(new IniConfigResult(new Dictionary<IniKeyId, string?>
                 {
                     [new IniKeyId(Section: "user", Key: "screenwidth")] = "1024"
                 }, IniEditorTestData.LastWrite.AddDays(1))));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        (await editor.TrySaveAsync(CancellationToken.None)).ShouldBeTrue();

        sent!.Edits.ShouldBe(new Dictionary<IniKeyId, string> { [new IniKeyId(Section: "user", Key: "screenwidth")] = "1024" });
        editor.HasPendingChanges.ShouldBeFalse();
        editor.FileLastWriteUtc.ShouldBe(IniEditorTestData.LastWrite.AddDays(1));
    }

    [ Fact ]
    public async Task Save_ValidationFailure_ShowsTheErrorAndKeepsTheEdits()
    {
        IniEditorViewModel editor = CreateEditor();

        IniEditorTestData.ReturnsForSave(_mediator, Error.Validation(code: "Edits", description: "\"\" is not a valid value for [user]/screenwidth."));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = null;

        (await editor.TrySaveAsync(CancellationToken.None)).ShouldBeFalse();

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Save zoo.ini", message: "\"\" is not a valid value for [user]/screenwidth.");
        editor.HasPendingChanges.ShouldBeTrue();
    }

    [ Fact ]
    public async Task Save_Throws_ShowsAReadableMessage()
    {
        IniEditorViewModel editor = CreateEditor();

        _mediator.Send(Arg.Any<SaveIniCommand>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<IniConfigResult>>>(_ => throw new InvalidOperationException(message: "database is locked"));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        (await editor.TrySaveAsync(CancellationToken.None)).ShouldBeFalse();

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Save zoo.ini", message: "zoo.ini could not be saved: database is locked");
        editor.IsBusy.ShouldBeFalse();
    }

    [ Fact ]
    public async Task TrySaveAsync_WhileASaveIsInFlight_AwaitsItWithoutSendingASecondCommand()
    {
        IniEditorViewModel editor = CreateEditor();

        TaskCompletionSource<ErrorOr<IniConfigResult>> pending = new();

        _mediator.Send(Arg.Any<SaveIniCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(pending.Task));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        Task<bool> firstSave  = editor.TrySaveAsync(CancellationToken.None);
        Task<bool> secondSave = editor.TrySaveAsync(CancellationToken.None);

        pending.SetResult(IniEditorTestData.Result(("user", "screenwidth", "1024")));

        (await firstSave).ShouldBeTrue();
        (await secondSave).ShouldBeTrue();

        await _mediator.Received(requiredNumberOfCalls: 1).Send(Arg.Any<SaveIniCommand>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task TrySaveAsync_AfterASaveThatCompletedSynchronously_SendsTheNextSave()
    {
        IniEditorViewModel      editor = CreateEditor();
        IniNumberFieldViewModel width  = IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth");

        IniEditorTestData.ReturnsForSave(_mediator, IniEditorTestData.Result(("user", "screenwidth", "1024")));

        width.Value = 1024m;

        (await editor.TrySaveAsync(CancellationToken.None)).ShouldBeTrue();

        IniEditorTestData.ReturnsForSave(_mediator, IniEditorTestData.Result(("user", "screenwidth", "1280")));

        width.Value = 1280m;

        (await editor.TrySaveAsync(CancellationToken.None)).ShouldBeTrue();

        editor.HasPendingChanges.ShouldBeFalse();

        await _mediator.Received(requiredNumberOfCalls: 2).Send(Arg.Any<SaveIniCommand>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Revert_ReloadsFromDisk()
    {
        IniEditorViewModel editor = CreateEditor();

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "640")));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        await editor.RevertCommand.ExecuteAsync(parameter: null);

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value.ShouldBe(expected: 640m);
        editor.HasPendingChanges.ShouldBeFalse();
    }

    [ Fact ]
    public async Task Revert_Throws_ShowsAReadableMessage()
    {
        IniEditorViewModel editor = CreateEditor();

        _mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<IniConfigResult>>>(_ => throw new InvalidOperationException(message: "database is locked"));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        await editor.RevertCommand.ExecuteAsync(parameter: null);

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Reload zoo.ini", message: "zoo.ini could not be reloaded: database is locked");
        editor.IsBusy.ShouldBeFalse();
    }

    [ Fact ]
    public void DiscardChanges_ResetsEveryField()
    {
        IniEditorViewModel editor = CreateEditor();

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;
        IniEditorTestData.Field<IniToggleFieldViewModel>(editor, section: "advanced", key: "drag").IsChecked = true;

        editor.DiscardChanges();

        editor.HasPendingChanges.ShouldBeFalse();
    }
}
