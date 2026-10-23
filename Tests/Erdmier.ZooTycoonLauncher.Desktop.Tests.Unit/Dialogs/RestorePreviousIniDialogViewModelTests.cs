namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Dialogs;

public sealed class RestorePreviousIniDialogViewModelTests
{
    private static readonly Guid InstallationId = Guid.CreateVersion7();

    private static readonly DateTime NewestUtc = new(year: 2026, month: 10, day: 20, hour: 14, minute: 30, second: 0, DateTimeKind.Utc);

    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private static IniHistoryEntry Entry(DateTime capturedUtc, IniSnapshotTrigger trigger, IReadOnlyList<IniDiffRow> rows, params IniKeyId[] keptKeys)
        => new(Guid.CreateVersion7(), IniSnapshotKind.Historical, trigger, capturedUtc, new IniSnapshotComparison(rows, keptKeys));

    private static IniHistoryResult History()
        => new([
            Entry(NewestUtc,
                  IniSnapshotTrigger.LauncherGui,
                  [
                      new IniDiffRow(new IniKeyId(Section: "user", Key: "screenwidth"), Current: "800", Snapshot: "640"),
                      new IniDiffRow(new IniKeyId(Section: "user", Key: "fullscreen"), Current: null, Snapshot: "1")
                  ],
                  new IniKeyId(Section: "ai", Key: "foo")),
            Entry(NewestUtc.AddDays(-1),
                  IniSnapshotTrigger.Manual,
                  [ new IniDiffRow(new IniKeyId(Section: "UI", Key: "scrollrate"), Current: "5", Snapshot: "3") ]),
            Entry(NewestUtc.AddDays(-2), IniSnapshotTrigger.OriginalImport, [ ])
        ]);

    private RestorePreviousIniDialogViewModel CreateViewModel(ILogger<RestorePreviousIniDialogViewModel>? logger = null, IniEditorStyle? style = null)
        => new(_mediator, _dialogs, new IniEditorStyleState { Style = style ?? IniEditorStyle.Ini }, logger ?? NullLogger<RestorePreviousIniDialogViewModel>.Instance);

    private async Task<RestorePreviousIniDialogViewModel> LoadedAsync()
    {
        _mediator.Send(Arg.Any<GetIniHistoryQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniHistoryResult>>(History()));

        RestorePreviousIniDialogViewModel viewModel = CreateViewModel();

        await viewModel.InitialiseAsync(InstallationId);

        return viewModel;
    }

    private static string Captured(DateTime utc) => utc.ToLocalTime().ToString(format: "d MMM yyyy HH:mm", CultureInfo.GetCultureInfo(name: "en-GB"));

    [ Fact ]
    public async Task Initialise_SelectsTheNewestRowAndShowsItsDiff()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedAsync();

        viewModel.Rows.Count.ShouldBe(expected: 3);
        viewModel.SelectedRow.ShouldBeSameAs(viewModel.Rows[0]);
        viewModel.Rows[0].CapturedText.ShouldBe(Captured(NewestUtc));
        viewModel.Rows[0].TriggerText.ShouldBe(expected: "LauncherGui");
        viewModel.Rows[0].DeltaText.ShouldBe(expected: "2");
        viewModel.DiffRows.Count.ShouldBe(expected: 2);
        viewModel.DiffRows[0].ShouldBe(new IniHistoryDiffRow(SectionText: "[user]", KeyText: "screenwidth", CurrentText: "800", SnapshotText: "640"));
        viewModel.DiffRows[1].CurrentText.ShouldBe(expected: "(not set)");
        viewModel.ChangeCountText.ShouldBe(expected: "2 changes");
        viewModel.HasNoDifferences.ShouldBeFalse();
        viewModel.RestoreCommand.CanExecute(parameter: null).ShouldBeTrue();
    }

    [ Fact ]
    public async Task Initialise_DiffShowsEditorValues()
    {
        _mediator.Send(Arg.Any<GetIniHistoryQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniHistoryResult>>(new IniHistoryResult([
                     Entry(NewestUtc, IniSnapshotTrigger.Manual, [ new IniDiffRow(new IniKeyId(Section: "user", Key: "fullscreen"), Current: "1", Snapshot: "0") ])
                 ])));

        RestorePreviousIniDialogViewModel viewModel = CreateViewModel();

        await viewModel.InitialiseAsync(InstallationId);

        viewModel.DiffRows.Single().ShouldBe(new IniHistoryDiffRow(SectionText: "[user]", KeyText: "fullscreen", CurrentText: "Fullscreen", SnapshotText: "Windowed"));
    }

    [ Fact ]
    public async Task SelectingARow_UpdatesTheDiffCaptionAndKeptKeys()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedAsync();

        viewModel.HasKeptKeys.ShouldBeTrue();
        viewModel.KeptKeysText.ShouldBe(expected: "Not in this snapshot, so they keep their current values: [ai]/foo");

        viewModel.SelectedRow = viewModel.Rows[1];

        viewModel.DiffRows.Count.ShouldBe(expected: 1);
        viewModel.ChangeCountText.ShouldBe(expected: "1 change");
        viewModel.HasKeptKeys.ShouldBeFalse();
    }

    [ Fact ]
    public async Task EmptyDiff_DisablesRestoreAndShowsNoDifferences()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedAsync();

        viewModel.SelectedRow = viewModel.Rows[2];

        viewModel.DiffRows.ShouldBeEmpty();
        viewModel.HasNoDifferences.ShouldBeTrue();
        viewModel.ChangeCountText.ShouldBe(expected: "0 changes");
        viewModel.RestoreCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task Restore_ConfirmationDeclined_SendsNothing()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedAsync();

        _dialogs.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        await viewModel.RestoreCommand.ExecuteAsync(parameter: null);

        await _mediator.DidNotReceive().Send(Arg.Any<RestoreIniSnapshotCommand>(), Arg.Any<CancellationToken>());
        viewModel.Result.ShouldBeNull();
        viewModel.IsBusy.ShouldBeFalse();
    }

    [ Fact ]
    public async Task Restore_Succeeds_SetsResultAndRequestsClose()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedAsync();

        IniConfigResult         restored = IniEditorTestData.Result(("user", "screenwidth", "640"));
        RestoreIniSnapshotCommand? sent  = null;
        int                     closes   = 0;

        viewModel.CloseRequested += (_, _) => closes++;
        _dialogs.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _mediator.Send(Arg.Do<RestoreIniSnapshotCommand>(command => sent = command), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(restored));

        await viewModel.RestoreCommand.ExecuteAsync(parameter: null);

        sent.ShouldBe(new RestoreIniSnapshotCommand(InstallationId, viewModel.Rows[0].Entry.SnapshotId));
        viewModel.Result.ShouldBeSameAs(restored);
        closes.ShouldBe(expected: 1);
    }

    [ Fact ]
    public async Task Restore_Succeeds_CanCloseIsTrueWhenCloseIsRequested()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedAsync();

        bool? canCloseInHandler = null;

        viewModel.CloseRequested += (_, _) => canCloseInHandler = viewModel.CanClose;
        _dialogs.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _mediator.Send(Arg.Any<RestoreIniSnapshotCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(IniEditorTestData.Result(("user", "screenwidth", "640"))));

        await viewModel.RestoreCommand.ExecuteAsync(parameter: null);

        canCloseInHandler.ShouldBe(expected: true);
    }

    [ Fact ]
    public async Task Restore_Fails_ShowsTheErrorAndStaysOpen()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedAsync();

        int closes = 0;

        viewModel.CloseRequested += (_, _) => closes++;
        _dialogs.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _mediator.Send(Arg.Any<RestoreIniSnapshotCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(Error.Failure(code: "Ini.Locked", description: "zoo.ini is locked.")));

        await viewModel.RestoreCommand.ExecuteAsync(parameter: null);

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Restore zoo.ini", message: "zoo.ini is locked.");
        viewModel.Result.ShouldBeNull();
        closes.ShouldBe(expected: 0);
        viewModel.IsBusy.ShouldBeFalse();
    }

    [ Fact ]
    public async Task Restore_WhileBusy_CannotExecute()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedAsync();

        TaskCompletionSource<ErrorOr<IniConfigResult>> pending = new();

        _dialogs.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _mediator.Send(Arg.Any<RestoreIniSnapshotCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(pending.Task));

        Task first = viewModel.RestoreCommand.ExecuteAsync(parameter: null);

        viewModel.IsBusy.ShouldBeTrue();
        viewModel.RestoreCommand.CanExecute(parameter: null).ShouldBeFalse();
        viewModel.RestoreCommand.Execute(parameter: null);

        pending.SetResult(IniEditorTestData.Result(("user", "screenwidth", "640")));

        await first;

        await _mediator.Received(requiredNumberOfCalls: 1).Send(Arg.Any<RestoreIniSnapshotCommand>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Cancel_WhileRestoring_DoesNotClose()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedAsync();
        int                               closed    = 0;

        TaskCompletionSource<ErrorOr<IniConfigResult>> pending = new();

        _dialogs.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _mediator.Send(Arg.Any<RestoreIniSnapshotCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(pending.Task));

        viewModel.CloseRequested += (_, _) => closed++;

        Task restoring = viewModel.RestoreCommand.ExecuteAsync(parameter: null);

        viewModel.CancelCommand.Execute(parameter: null);

        closed.ShouldBe(expected: 0);

        pending.SetResult(IniEditorTestData.Result(("user", "screenwidth", "640")));

        await restoring;

        closed.ShouldBe(expected: 1);
    }

    [ Fact ]
    public async Task Cancel_WhileRestoring_CannotExecute()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedAsync();

        TaskCompletionSource<ErrorOr<IniConfigResult>> pending = new();

        _dialogs.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _mediator.Send(Arg.Any<RestoreIniSnapshotCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(pending.Task));

        viewModel.CancelCommand.CanExecute(parameter: null).ShouldBeTrue();
        viewModel.CanClose.ShouldBeTrue();

        Task restoring = viewModel.RestoreCommand.ExecuteAsync(parameter: null);

        viewModel.CancelCommand.CanExecute(parameter: null).ShouldBeFalse();
        viewModel.CanClose.ShouldBeFalse();

        pending.SetResult(IniEditorTestData.Result(("user", "screenwidth", "640")));

        await restoring;

        viewModel.CancelCommand.CanExecute(parameter: null).ShouldBeTrue();
        viewModel.CanClose.ShouldBeTrue();
    }

    [ Fact ]
    public async Task Confirmation_MentionsKeptKeysOnlyWhenPresent()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedAsync();

        string? title   = null;
        string? message = null;

        _dialogs.ShowConfirmationAsync(Arg.Do<string>(value => title = value), Arg.Do<string>(value => message = value)).Returns(false);

        await viewModel.RestoreCommand.ExecuteAsync(parameter: null);

        title.ShouldBe(expected: "Restore Previous INI");
        message.ShouldBe($"Restore zoo.ini to the version captured {Captured(NewestUtc)}? The current version is archived to history first, so this can be undone."
                         + " 1 setting is not in this snapshot and will keep its current value.");

        viewModel.SelectedRow = viewModel.Rows[1];

        await viewModel.RestoreCommand.ExecuteAsync(parameter: null);

        message.ShouldBe($"Restore zoo.ini to the version captured {Captured(NewestUtc.AddDays(-1))}? The current version is archived to history first, so this can be undone.");
    }

    [ Fact ]
    public async Task Initialise_QueryFails_SetsErrorMessageAndDisablesRestore()
    {
        _mediator.Send(Arg.Any<GetIniHistoryQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniHistoryResult>>(Error.NotFound(code: "Installation.NotFound", description: "Installation missing.")));

        RestorePreviousIniDialogViewModel viewModel = CreateViewModel();

        await viewModel.InitialiseAsync(InstallationId);

        viewModel.ErrorMessage.ShouldBe(expected: "Installation missing.");
        viewModel.Rows.ShouldBeEmpty();
        viewModel.RestoreCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task Initialise_QueryFails_HasError()
    {
        _mediator.Send(Arg.Any<GetIniHistoryQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniHistoryResult>>(Error.NotFound(code: "Installation.NotFound", description: "Installation missing.")));

        RestorePreviousIniDialogViewModel viewModel = CreateViewModel();

        viewModel.HasError.ShouldBeFalse();

        await viewModel.InitialiseAsync(InstallationId);

        viewModel.HasError.ShouldBeTrue();
        viewModel.HasNoDifferences.ShouldBeFalse();
    }

    [ Fact ]
    public async Task Initialise_QueryThrows_SetsErrorMessage()
    {
        _mediator.Send(Arg.Any<GetIniHistoryQuery>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<IniHistoryResult>>>(_ => throw new IOException("boom"));

        RestorePreviousIniDialogViewModel viewModel = CreateViewModel();

        await viewModel.InitialiseAsync(InstallationId);

        viewModel.ErrorMessage.ShouldBe(expected: "Restore Previous INI could not load the history of zoo.ini: boom");
        viewModel.HasError.ShouldBeTrue();
        viewModel.Rows.ShouldBeEmpty();
        viewModel.RestoreCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task Initialise_QueryThrows_LogsError()
    {
        RecordingLogger<RestorePreviousIniDialogViewModel> logger = new();

        _mediator.Send(Arg.Any<GetIniHistoryQuery>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<IniHistoryResult>>>(_ => throw new IOException("boom"));

        await CreateViewModel(logger).InitialiseAsync(InstallationId);

        RecordingLogEntry entry = logger.Entries.ShouldHaveSingleItem();

        entry.Level.ShouldBe(LogLevel.Error);
        entry.Exception.ShouldBeOfType<IOException>();
    }

    [ Fact ]
    public async Task Initialise_QueryFails_LogsWarning()
    {
        RecordingLogger<RestorePreviousIniDialogViewModel> logger = new();

        _mediator.Send(Arg.Any<GetIniHistoryQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniHistoryResult>>(Error.Unexpected(code: "Ini.StoreFailed", description: "x")));

        await CreateViewModel(logger).InitialiseAsync(InstallationId);

        RecordingLogEntry entry = logger.Entries.ShouldHaveSingleItem();

        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Exception.ShouldBeNull();
    }

    [ Fact ]
    public async Task Restore_Throws_LogsError()
    {
        RecordingLogger<RestorePreviousIniDialogViewModel> logger = new();

        _mediator.Send(Arg.Any<GetIniHistoryQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniHistoryResult>>(History()));

        RestorePreviousIniDialogViewModel viewModel = CreateViewModel(logger);

        await viewModel.InitialiseAsync(InstallationId);

        _dialogs.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _mediator.Send(Arg.Any<RestoreIniSnapshotCommand>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<IniConfigResult>>>(_ => throw new IOException("boom"));

        await viewModel.RestoreCommand.ExecuteAsync(parameter: null);

        RecordingLogEntry entry = logger.Entries.ShouldHaveSingleItem();

        entry.Level.ShouldBe(LogLevel.Error);
        entry.Exception.ShouldBeOfType<IOException>();
    }

    [ Fact ]
    public async Task Restore_ReturnsError_LogsWarning()
    {
        RecordingLogger<RestorePreviousIniDialogViewModel> logger = new();

        _mediator.Send(Arg.Any<GetIniHistoryQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniHistoryResult>>(History()));

        RestorePreviousIniDialogViewModel viewModel = CreateViewModel(logger);

        await viewModel.InitialiseAsync(InstallationId);

        _dialogs.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _mediator.Send(Arg.Any<RestoreIniSnapshotCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(Error.Unexpected(code: "Ini.StoreFailed", description: "x")));

        await viewModel.RestoreCommand.ExecuteAsync(parameter: null);

        RecordingLogEntry entry = logger.Entries.ShouldHaveSingleItem();

        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Exception.ShouldBeNull();
    }

    private static IniHistoryResult MixedHistory()
        => new([
            Entry(NewestUtc,
                  IniSnapshotTrigger.Manual,
                  [
                      new IniDiffRow(new IniKeyId(Section: "language", Key: "sublang"), Current: "1", Snapshot: "2"),
                      new IniDiffRow(new IniKeyId(Section: "UI", Key: "userAttenuation"), Current: "0", Snapshot: "2500"),
                      new IniDiffRow(new IniKeyId(Section: "user", Key: "fullscreen"), Current: null, Snapshot: "1"),
                      new IniDiffRow(new IniKeyId(Section: "language", Key: "lang"), Current: "9", Snapshot: "12"),
                      new IniDiffRow(new IniKeyId(Section: "UI", Key: "noMenuMusic"), Current: "0", Snapshot: "1")
                  ],
                  new IniKeyId(Section: "UI", Key: "playMovie"),
                  new IniKeyId(Section: "ai", Key: "foo"))
        ]);

    private async Task<RestorePreviousIniDialogViewModel> LoadedMixedAsync(IniEditorStyle style)
    {
        _mediator.Send(Arg.Any<GetIniHistoryQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniHistoryResult>>(MixedHistory()));

        RestorePreviousIniDialogViewModel viewModel = CreateViewModel(style: style);

        await viewModel.InitialiseAsync(InstallationId);

        return viewModel;
    }

    [ Fact ]
    public async Task Rows_SameCountInBothStyles()
    {
        RestorePreviousIniDialogViewModel ini      = await LoadedMixedAsync(IniEditorStyle.Ini);
        RestorePreviousIniDialogViewModel friendly = await LoadedMixedAsync(IniEditorStyle.Friendly);

        ini.DiffRows.Count.ShouldBe(expected: 5);
        friendly.DiffRows.Count.ShouldBe(expected: 5);
        friendly.ChangeCountText.ShouldBe(ini.ChangeCountText);
        friendly.RestoreCommand.CanExecute(parameter: null).ShouldBe(ini.RestoreCommand.CanExecute(parameter: null));
    }

    [ Fact ]
    public async Task Rows_Friendly_FollowPageOrder()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedMixedAsync(IniEditorStyle.Friendly);

        viewModel.DiffRows.Select(row => row.KeyText)
                 .ShouldBe([
                     "Display › Screen › Screen mode",
                     "Sound › Menu music › Enable menu music",
                     "Sound › Game sound › Game sound volume",
                     "Language › Language › Language code",
                     "Language › Language › Region code"
                 ]);
        viewModel.DiffRows.Select(row => row.SectionText).ShouldAllBe(text => text.Length == 0);
        viewModel.DiffRows[1].ShouldBe(new IniHistoryDiffRow(SectionText: "", KeyText: "Sound › Menu music › Enable menu music", CurrentText: "On", SnapshotText: "Off"));
        viewModel.DiffRows[2].SnapshotText.ShouldBe(expected: "75 %");
        viewModel.DiffRows[0].CurrentText.ShouldBe(expected: "(not set)");
    }

    [ Fact ]
    public async Task Rows_Ini_KeepTheDiffOrder()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedMixedAsync(IniEditorStyle.Ini);

        viewModel.DiffRows.Select(row => row.KeyText).ShouldBe([ "sublang", "userAttenuation", "fullscreen", "lang", "noMenuMusic" ]);
        viewModel.DiffRows[4].SnapshotText.ShouldBe(expected: "true");
    }

    [ Fact ]
    public async Task KeptKeysNote_NamesKeysInTheStyle()
    {
        RestorePreviousIniDialogViewModel ini      = await LoadedMixedAsync(IniEditorStyle.Ini);
        RestorePreviousIniDialogViewModel friendly = await LoadedMixedAsync(IniEditorStyle.Friendly);

        ini.KeptKeysText.ShouldBe(expected: "Not in this snapshot, so they keep their current values: [UI]/playMovie, [ai]/foo");
        friendly.KeptKeysText.ShouldBe(expected: "Not in this snapshot, so they keep their current values: Sound › Intro movies › Play the first movie, [ai]/foo");
    }

    [ Fact ]
    public async Task Restore_Throws_ShowsTheErrorAndStaysOpen()
    {
        RestorePreviousIniDialogViewModel viewModel = await LoadedAsync();

        int closes = 0;

        viewModel.CloseRequested += (_, _) => closes++;
        _dialogs.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _mediator.Send(Arg.Any<RestoreIniSnapshotCommand>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<IniConfigResult>>>(_ => throw new IOException("boom"));

        await viewModel.RestoreCommand.ExecuteAsync(parameter: null);

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Restore zoo.ini", message: "zoo.ini could not be restored: boom");
        viewModel.Result.ShouldBeNull();
        closes.ShouldBe(expected: 0);
        viewModel.IsBusy.ShouldBeFalse();
    }
}
