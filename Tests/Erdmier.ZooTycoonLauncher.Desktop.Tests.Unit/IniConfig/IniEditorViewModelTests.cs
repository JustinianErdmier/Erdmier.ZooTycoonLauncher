namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.IniConfig;

public sealed class IniEditorViewModelTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private IniEditorViewModel CreateEditor(ILogger<IniEditorViewModel>? logger = null)
        => new(Guid.CreateVersion7(), _mediator, _dialogs, IniEditorTestData.Result(("user", "screenwidth", "800"), ("user", "fullscreen", "1")),
               logger ?? NullLogger<IniEditorViewModel>.Instance);

    private IniEditorViewModel CreateEditorWithHistory(int historicalCount = 2)
        => new(Guid.CreateVersion7(), _mediator, _dialogs,
               IniEditorTestData.ResultWithHistory(historicalCount, ("user", "screenwidth", "800"), ("user", "fullscreen", "1")),
               NullLogger<IniEditorViewModel>.Instance);

    [ Fact ]
    public void RestorePrevious_NoHistory_IsDisabled() => CreateEditorWithHistory(historicalCount: 0).RestorePreviousCommand.CanExecute(parameter: null).ShouldBeFalse();

    [ Fact ]
    public void RestorePrevious_WithHistory_IsEnabled() => CreateEditorWithHistory().RestorePreviousCommand.CanExecute(parameter: null).ShouldBeTrue();

    [ Fact ]
    public async Task RestorePrevious_PendingEdits_Yes_SavesThenOpens()
    {
        IniEditorViewModel editor = CreateEditorWithHistory();

        IniEditorTestData.ReturnsForSave(_mediator, IniEditorTestData.ResultWithHistory(historicalCount: 3, ("user", "screenwidth", "1024")));
        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.Yes);
        _dialogs.ShowRestorePreviousIniAsync(Arg.Any<Guid>()).Returns(Task.FromResult<IniConfigResult?>(result: null));
        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "1024")));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        await editor.RestorePreviousCommand.ExecuteAsync(parameter: null);

        await _mediator.Received(requiredNumberOfCalls: 1).Send(Arg.Any<SaveIniCommand>(), Arg.Any<CancellationToken>());
        await _dialogs.Received(requiredNumberOfCalls: 1).ShowRestorePreviousIniAsync(Arg.Any<Guid>());

        Received.InOrder(() =>
        {
            _mediator.Send(Arg.Any<SaveIniCommand>(), Arg.Any<CancellationToken>());
            _dialogs.ShowRestorePreviousIniAsync(Arg.Any<Guid>());
        });
    }

    [ Fact ]
    public async Task RestorePrevious_PendingEdits_Yes_SaveFails_DoesNotOpen()
    {
        IniEditorViewModel editor = CreateEditorWithHistory();

        IniEditorTestData.ReturnsForSave(_mediator, Error.Validation(code: "Edits", description: "bad"));
        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.Yes);

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        await editor.RestorePreviousCommand.ExecuteAsync(parameter: null);

        await _dialogs.DidNotReceive().ShowRestorePreviousIniAsync(Arg.Any<Guid>());
        editor.HasPendingChanges.ShouldBeTrue();
    }

    [ Fact ]
    public async Task RestorePrevious_PendingEdits_No_DiscardsThenOpens()
    {
        IniEditorViewModel editor = CreateEditorWithHistory();
        IniConfigResult    restored = IniEditorTestData.ResultWithHistory(historicalCount: 2, ("user", "screenwidth", "640"));

        bool?    pendingWhenOpened = null;
        decimal? valueWhenOpened   = null;

        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.No);
        _dialogs.ShowRestorePreviousIniAsync(Arg.Any<Guid>())
                .Returns(_ =>
                {
                    // Observed from inside the dialogue call: the discard must already have happened, or the later Load(restored) would mask a missing DiscardChanges.
                    pendingWhenOpened = editor.HasPendingChanges;
                    valueWhenOpened   = IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value;

                    return Task.FromResult<IniConfigResult?>(restored);
                });

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        await editor.RestorePreviousCommand.ExecuteAsync(parameter: null);

        pendingWhenOpened.ShouldBe(expected: false);
        valueWhenOpened.ShouldBe(expected: 800m);

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowRestorePreviousIniAsync(Arg.Any<Guid>());
        await _mediator.DidNotReceive().Send(Arg.Any<SaveIniCommand>(), Arg.Any<CancellationToken>());
        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value.ShouldBe(expected: 640m);
    }

    [ Fact ]
    public async Task RestorePrevious_PendingEdits_Cancel_KeepsEditsAndDoesNotOpen()
    {
        IniEditorViewModel editor = CreateEditorWithHistory();

        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.Cancel);

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        await editor.RestorePreviousCommand.ExecuteAsync(parameter: null);

        await _dialogs.DidNotReceive().ShowRestorePreviousIniAsync(Arg.Any<Guid>());
        editor.HasPendingChanges.ShouldBeTrue();
        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value.ShouldBe(expected: 1024m);
    }

    [ Fact ]
    public async Task RestorePrevious_Restored_LoadsTheReturnedResult()
    {
        IniEditorViewModel editor   = CreateEditorWithHistory();
        IniConfigResult    restored = IniEditorTestData.ResultWithHistory(historicalCount: 5, ("user", "screenwidth", "640"));

        _dialogs.ShowRestorePreviousIniAsync(Arg.Any<Guid>()).Returns(Task.FromResult<IniConfigResult?>(restored));

        await editor.RestorePreviousCommand.ExecuteAsync(parameter: null);

        await _dialogs.DidNotReceive().ShowSaveChangesPromptAsync();
        await _mediator.DidNotReceive().Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>());
        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value.ShouldBe(expected: 640m);
        editor.HistoricalCount.ShouldBe(expected: 5);
    }

    [ Fact ]
    public async Task RestorePrevious_Cancelled_ReloadsFromGetIniConfig()
    {
        IniEditorViewModel editor = CreateEditorWithHistory();

        _dialogs.ShowRestorePreviousIniAsync(Arg.Any<Guid>()).Returns(Task.FromResult<IniConfigResult?>(result: null));
        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.ResultWithHistory(historicalCount: 4, ("user", "screenwidth", "640")));

        await editor.RestorePreviousCommand.ExecuteAsync(parameter: null);

        await _mediator.Received(requiredNumberOfCalls: 1).Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>());
        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value.ShouldBe(expected: 640m);
        editor.HistoricalCount.ShouldBe(expected: 4);
    }

    [ Fact ]
    public async Task RestorePrevious_DialogueThrows_ShowsAnError()
    {
        IniEditorViewModel editor = CreateEditorWithHistory();

        _dialogs.ShowRestorePreviousIniAsync(Arg.Any<Guid>()).Returns<Task<IniConfigResult?>>(_ => throw new IOException("boom"));

        await editor.RestorePreviousCommand.ExecuteAsync(parameter: null);

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Restore zoo.ini", message: "Restore Previous INI could not be opened: boom");
        editor.IsBusy.ShouldBeFalse();
    }

    [ Fact ]
    public async Task RestorePrevious_WhileDialogueOpen_EditorIsBusy()
    {
        IniEditorViewModel editor = CreateEditorWithHistory();

        TaskCompletionSource<IniConfigResult?> open = new();

        _dialogs.ShowRestorePreviousIniAsync(Arg.Any<Guid>()).Returns(open.Task);

        Task running = editor.RestorePreviousCommand.ExecuteAsync(parameter: null);

        editor.IsBusy.ShouldBeTrue();

        // Edit a field while the dialogue is open so Reset and Restore defaults would otherwise be enabled: only IsBusy can be what disables them.
        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        editor.HasPendingChanges.ShouldBeTrue();
        editor.ResetCommand.CanExecute(parameter: null).ShouldBeFalse();
        editor.RestoreDefaultsCommand.CanExecute(parameter: null).ShouldBeFalse();
        editor.RestorePreviousCommand.CanExecute(parameter: null).ShouldBeFalse();

        open.SetResult(IniEditorTestData.ResultWithHistory(historicalCount: 3, ("user", "screenwidth", "640")));

        await running;

        editor.IsBusy.ShouldBeFalse();
        editor.HistoricalCount.ShouldBe(expected: 3);
    }

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

        width.IsHovered = true;

        editor.IsFooterHelp.ShouldBeTrue();
        editor.FooterText.ShouldBe(width.Help);

        width.IsHovered = false;

        editor.FooterText.ShouldBe(expected: "● Unsaved changes");
    }

    [ Fact ]
    public void ChangingSection_ClearsTheHoveredHelp()
    {
        IniEditorViewModel      editor = CreateEditor();
        IniNumberFieldViewModel width  = IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth");

        width.IsHovered = true;

        editor.IsFooterHelp.ShouldBeTrue();

        editor.SelectedSection = editor.Sections[1];

        editor.IsFooterHelp.ShouldBeFalse();
        editor.IsFooterSaved.ShouldBeTrue();
        editor.FooterText.ShouldStartWith(expected: "All changes saved · Last write: ");
    }

    [ Fact ]
    public void Help_PointerLeavesFocusedRow_KeepsItsHelp()
    {
        IniEditorViewModel      editor = CreateEditor();
        IniNumberFieldViewModel width  = IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth");

        width.IsFocused = true;
        width.IsHovered = true;
        width.IsHovered = false;

        editor.IsFooterHelp.ShouldBeTrue();
        editor.FooterText.ShouldBe(width.Help);
    }

    [ Fact ]
    public void Help_HoverAnotherRowWhileOneIsFocused_ShowsTheHoveredHelp()
    {
        IniEditorViewModel      editor = CreateEditor();
        IniNumberFieldViewModel width  = IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth");
        IniToggleFieldViewModel drag   = IniEditorTestData.Field<IniToggleFieldViewModel>(editor, section: "advanced", key: "drag");

        width.IsFocused = true;
        drag.IsHovered  = true;

        editor.FooterText.ShouldBe(drag.Help);

        drag.IsHovered = false;

        editor.FooterText.ShouldBe(width.Help);
    }

    [ Fact ]
    public void Help_NeitherHoveredNorFocused_ShowsTheFooterState()
    {
        IniEditorViewModel      editor = CreateEditor();
        IniNumberFieldViewModel width  = IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth");

        width.IsFocused = true;
        width.IsHovered = true;
        width.IsFocused = false;
        width.IsHovered = false;

        editor.IsFooterHelp.ShouldBeFalse();
        editor.IsFooterSaved.ShouldBeTrue();
    }

    [ Fact ]
    public void Help_SectionChange_ClearsHoverAndFocus()
    {
        IniEditorViewModel      editor = CreateEditor();
        IniNumberFieldViewModel width  = IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth");

        width.IsFocused = true;
        width.IsHovered = true;

        editor.SelectedSection = editor.Sections[1];

        width.IsHovered.ShouldBeFalse();
        width.IsFocused.ShouldBeFalse();
        editor.IsFooterHelp.ShouldBeFalse();

        width.IsFocused = true;

        editor.IsFooterHelp.ShouldBeTrue();
    }

    [ Fact ]
    public void ChangingSectionAwayAndBack_HoveringTheSameRowAgain_ShowsItsHelp()
    {
        IniEditorViewModel      editor = CreateEditor();
        IniNumberFieldViewModel width  = IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth");

        width.IsHovered = true;

        editor.SelectedSection = editor.Sections[1];
        editor.SelectedSection = editor.Sections[0];

        width.IsHovered = true;

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
                 }, IniEditorTestData.LastWrite.AddDays(1), HistoricalCount: 0)));

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
    public async Task Save_Throws_LogsError()
    {
        RecordingLogger<IniEditorViewModel> logger = new();
        IniEditorViewModel                  editor = CreateEditor(logger);

        _mediator.Send(Arg.Any<SaveIniCommand>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<IniConfigResult>>>(_ => throw new InvalidOperationException(message: "database is locked"));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        await editor.TrySaveAsync(CancellationToken.None);

        RecordingLogEntry entry = logger.Entries.ShouldHaveSingleItem();

        entry.Level.ShouldBe(LogLevel.Error);
        entry.Exception.ShouldBeOfType<InvalidOperationException>();
    }

    [ Fact ]
    public async Task Save_ReturnsError_LogsWarning()
    {
        RecordingLogger<IniEditorViewModel> logger = new();
        IniEditorViewModel                  editor = CreateEditor(logger);

        IniEditorTestData.ReturnsForSave(_mediator, Error.Validation(code: "Edits", description: "bad value"));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        await editor.TrySaveAsync(CancellationToken.None);

        RecordingLogEntry entry = logger.Entries.ShouldHaveSingleItem();

        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Exception.ShouldBeNull();
    }

    [ Fact ]
    public async Task Reload_Fails_LogsWarning()
    {
        RecordingLogger<IniEditorViewModel> logger = new();
        IniEditorViewModel                  editor = CreateEditor(logger);

        IniEditorTestData.ReturnsForGet(_mediator, Error.Unexpected(code: "Ini.StoreFailed", description: "x"));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        await editor.ResetCommand.ExecuteAsync(parameter: null);

        RecordingLogEntry entry = logger.Entries.ShouldHaveSingleItem();

        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Exception.ShouldBeNull();
    }

    [ Fact ]
    public async Task Reload_Throws_LogsError()
    {
        RecordingLogger<IniEditorViewModel> logger = new();
        IniEditorViewModel                  editor = CreateEditor(logger);

        _mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<IniConfigResult>>>(_ => throw new IOException("boom"));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        await editor.ResetCommand.ExecuteAsync(parameter: null);

        RecordingLogEntry entry = logger.Entries.ShouldHaveSingleItem();

        entry.Level.ShouldBe(LogLevel.Error);
        entry.Exception.ShouldBeOfType<IOException>();
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
    public async Task Reset_ReloadsFromDisk()
    {
        IniEditorViewModel editor = CreateEditor();

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "640")));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        await editor.ResetCommand.ExecuteAsync(parameter: null);

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value.ShouldBe(expected: 640m);
        editor.HasPendingChanges.ShouldBeFalse();
    }

    [ Fact ]
    public void RestoreDefaults_SetsEveryFieldToItsDefault()
    {
        IniEditorViewModel editor = new(Guid.CreateVersion7(), _mediator, _dialogs,
                                        IniEditorTestData.Result(("user", "screenwidth", "1024"), (IniEditorCatalogue.LanguageId.Section, IniEditorCatalogue.LanguageId.Key, "12"),
                                                            (IniEditorCatalogue.SubLanguageId.Section, IniEditorCatalogue.SubLanguageId.Key, "1")),
                                        NullLogger<IniEditorViewModel>.Instance);

        editor.RestoreDefaultsCommand.CanExecute(parameter: null).ShouldBeTrue();

        editor.RestoreDefaultsCommand.Execute(parameter: null);

        editor.Sections.SelectMany(section => section.Fields).ShouldAllBe(field => field.IsAtDefault);
        editor.HasPendingChanges.ShouldBeTrue();
        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value.ShouldBe(expected: 800m);
        editor.Sections.SelectMany(section => section.Fields).OfType<IniLanguageFieldViewModel>().Single().SelectedOption!.Label.ShouldBe(expected: "English (United States)");
    }

    [ Fact ]
    public void RestoreDefaults_IsDisabledWhenEverythingIsAlreadyDefault()
    {
        IniEditorViewModel editor = CreateEditor();

        editor.RestoreDefaultsCommand.CanExecute(parameter: null).ShouldBeFalse();

        editor.RestoreDefaultsCommand.Execute(parameter: null);

        editor.RestoreDefaultsCommand.CanExecute(parameter: null).ShouldBeFalse();
        editor.HasPendingChanges.ShouldBeFalse();
    }

    [ Fact ]
    public async Task RestoreDefaults_WhileBusy_IsDisabled()
    {
        IniEditorViewModel editor = new(Guid.CreateVersion7(), _mediator, _dialogs, IniEditorTestData.ResultWithHistory(historicalCount: 2, ("user", "screenwidth", "1024")),
                                        NullLogger<IniEditorViewModel>.Instance);

        TaskCompletionSource<IniConfigResult?> open = new();

        _dialogs.ShowRestorePreviousIniAsync(Arg.Any<Guid>()).Returns(open.Task);

        editor.RestoreDefaultsCommand.CanExecute(parameter: null).ShouldBeTrue();

        Task running = editor.RestorePreviousCommand.ExecuteAsync(parameter: null);

        editor.IsBusy.ShouldBeTrue();
        editor.RestoreDefaultsCommand.CanExecute(parameter: null).ShouldBeFalse();

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "1024")));

        open.SetResult(result: null);

        await running;

        await _dialogs.DidNotReceive().ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
        editor.IsBusy.ShouldBeFalse();
        editor.RestoreDefaultsCommand.CanExecute(parameter: null).ShouldBeTrue();
    }

    [ Fact ]
    public void RestoreDefaults_Designer_IsDisabled()
    {
        IniEditorViewModel editor = new();

        // A non-default value would enable the command were a mediator present: only the designer's missing mediator disables it.
        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        editor.RestoreDefaultsCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task RestoreDefaults_ThenReset_ReloadsTheSavedValues()
    {
        IniEditorViewModel editor = CreateEditor();

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "640")));

        editor.RestoreDefaultsCommand.Execute(parameter: null);

        await editor.ResetCommand.ExecuteAsync(parameter: null);

        await _mediator.Received(requiredNumberOfCalls: 1).Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>());
        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value.ShouldBe(expected: 640m);
        editor.HasPendingChanges.ShouldBeFalse();
    }

    [ Fact ]
    public async Task Reset_Throws_ShowsAReadableMessage()
    {
        IniEditorViewModel editor = CreateEditor();

        _mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<IniConfigResult>>>(_ => throw new InvalidOperationException(message: "database is locked"));

        IniEditorTestData.Field<IniNumberFieldViewModel>(editor, section: "user", key: "screenwidth").Value = 1024m;

        await editor.ResetCommand.ExecuteAsync(parameter: null);

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
