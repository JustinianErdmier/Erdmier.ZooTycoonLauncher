namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Dialogs;

public sealed class OptionsDialogViewModelTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private readonly IniEditorStyleState _styleState = new();

    private void ReturnsStored(LauncherStartupPreference preference, bool closeAfterGameLaunch, IniEditorStyle? editorStyle = null)
        => _mediator.Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<ErrorOr<LauncherOptions>>(new LauncherOptions(preference, closeAfterGameLaunch, editorStyle ?? IniEditorStyle.Friendly)));

    private void ReturnsSave(ErrorOr<Success> result)
        => _mediator.Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<ErrorOr<Success>>(result));

    private async Task<OptionsDialogViewModel> LoadedAsync(RecordingLogger<OptionsDialogViewModel>? logger = null)
    {
        ReturnsStored(LauncherStartupPreference.LastPlayedInstallation, closeAfterGameLaunch: true);

        OptionsDialogViewModel viewModel = new(_mediator, _dialogs, _styleState, logger ?? new RecordingLogger<OptionsDialogViewModel>());

        await viewModel.InitialiseAsync();

        return viewModel;
    }

    [ Fact ]
    public async Task Initialise_LoadsTheStoredValues()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();

        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.LastPlayedInstallation);
        viewModel.CloseAfterGameLaunch.ShouldBeTrue();
        viewModel.ErrorMessage.ShouldBeNull();
    }

    [ Fact ]
    public async Task Ok_SendsOneSaveWithTheChosenValues_AndCloses()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();
        int                    closed    = 0;

        ReturnsSave(Result.Success);

        viewModel.CloseRequested += (_, _) => closed++;
        viewModel.StartupPreference    = LauncherStartupPreference.NoInstallation;
        viewModel.CloseAfterGameLaunch = false;

        await viewModel.OkCommand.ExecuteAsync(parameter: null);

        await _mediator.Received(requiredNumberOfCalls: 1)
                       .Send(Arg.Is<SaveLauncherOptionsCommand>(command => command.StartupPreference == LauncherStartupPreference.NoInstallation
                                                                           && !command.CloseAfterGameLaunch),
                             Arg.Any<CancellationToken>());
        closed.ShouldBe(expected: 1);
    }

    [ Fact ]
    public async Task Ok_PassesBackTheLoadedEditorStyle()
    {
        ReturnsStored(LauncherStartupPreference.DefaultInstallation, closeAfterGameLaunch: false, editorStyle: IniEditorStyle.Ini);
        ReturnsSave(Result.Success);

        OptionsDialogViewModel viewModel = new(_mediator, _dialogs, _styleState, new RecordingLogger<OptionsDialogViewModel>());

        await viewModel.InitialiseAsync();
        await viewModel.OkCommand.ExecuteAsync(parameter: null);

        await _mediator.Received(requiredNumberOfCalls: 1)
                       .Send(Arg.Is<SaveLauncherOptionsCommand>(command => command.EditorStyle == IniEditorStyle.Ini), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Initialise_LoadsTheEditorStyle()
    {
        ReturnsStored(LauncherStartupPreference.DefaultInstallation, closeAfterGameLaunch: false, editorStyle: IniEditorStyle.Ini);

        OptionsDialogViewModel viewModel = new(_mediator, _dialogs, _styleState, new RecordingLogger<OptionsDialogViewModel>());

        await viewModel.InitialiseAsync();

        viewModel.EditorStyle.ShouldBe(IniEditorStyle.Ini);
        viewModel.IsIniStyle.ShouldBeTrue();
        viewModel.IsFriendlyStyle.ShouldBeFalse();
    }

    [ Fact ]
    public async Task Ok_SavesTheEditorStyleAndAppliesItAfterTheSave()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();
        IniEditorStyle?        appliedAtSave = null;

        _mediator.Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>())
                 .Returns(_ =>
                          {
                              appliedAtSave = _styleState.Style;

                              return new ValueTask<ErrorOr<Success>>(Result.Success);
                          });

        viewModel.IsIniStyle = true;

        await viewModel.OkCommand.ExecuteAsync(parameter: null);

        await _mediator.Received(requiredNumberOfCalls: 1)
                       .Send(Arg.Is<SaveLauncherOptionsCommand>(command => command.EditorStyle == IniEditorStyle.Ini), Arg.Any<CancellationToken>());
        appliedAtSave.ShouldBe(IniEditorStyle.Friendly);
        _styleState.Style.ShouldBe(IniEditorStyle.Ini);
    }

    [ Fact ]
    public async Task FailedSave_DoesNotApplyTheStyle()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();

        ReturnsSave(Error.Failure(code: "Settings.WriteFailed", description: "disk full"));

        viewModel.IsIniStyle = true;

        await viewModel.OkCommand.ExecuteAsync(parameter: null);

        _styleState.Style.ShouldBe(IniEditorStyle.Friendly);
    }

    [ Fact ]
    public async Task Cancel_DoesNotApplyTheStyle()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();

        viewModel.IsIniStyle = true;
        viewModel.CancelCommand.Execute(parameter: null);

        _styleState.Style.ShouldBe(IniEditorStyle.Friendly);
    }

    [ Fact ]
    public async Task StyleDescription_FollowsTheSelection()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();

        viewModel.StyleDescription.ShouldBe("Settings are grouped by what they do, with plain names and volumes that go up as they get louder.");

        viewModel.IsIniStyle = true;

        viewModel.StyleDescription.ShouldBe("Settings appear exactly as zoo.ini stores them: its sections, key names and raw values.");

        viewModel.IsFriendlyStyle = true;

        viewModel.StyleDescription.ShouldBe("Settings are grouped by what they do, with plain names and volumes that go up as they get louder.");

        // An unchecked radio (the group clearing the previous choice) must not change the style.
        viewModel.IsIniStyle = false;

        viewModel.EditorStyle.ShouldBe(IniEditorStyle.Friendly);
    }

    [ Fact ]
    public async Task Cancel_SendsNothing_AndCloses()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();
        int                    closed    = 0;

        viewModel.CloseRequested += (_, _) => closed++;

        viewModel.CancelCommand.Execute(parameter: null);

        closed.ShouldBe(expected: 1);
        await _mediator.DidNotReceive()
                       .Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Ok_SaveFails_ShowsErrorAndStaysOpen()
    {
        RecordingLogger<OptionsDialogViewModel> logger    = new();
        OptionsDialogViewModel                  viewModel = await LoadedAsync(logger);
        int                                     closed    = 0;

        ReturnsSave(Error.Failure(code: "Settings.StoreFailed", description: "The database is locked."));

        viewModel.CloseRequested += (_, _) => closed++;
        viewModel.StartupPreference    = LauncherStartupPreference.DefaultInstallation;
        viewModel.CloseAfterGameLaunch = false;

        await viewModel.OkCommand.ExecuteAsync(parameter: null);

        closed.ShouldBe(expected: 0);
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.DefaultInstallation);
        viewModel.CloseAfterGameLaunch.ShouldBeFalse();
        viewModel.IsBusy.ShouldBeFalse();
        logger.Entries.Count(entry => entry.Level == LogLevel.Warning).ShouldBe(expected: 1);
        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Save Options", message: "The database is locked.");
    }

    [ Fact ]
    public async Task Ok_SaveThrows_ShowsErrorAndStaysOpen()
    {
        RecordingLogger<OptionsDialogViewModel> logger    = new();
        OptionsDialogViewModel                  viewModel = await LoadedAsync(logger);
        int                                     closed    = 0;

        _mediator.Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<Success>>>(_ => throw new InvalidOperationException("boom"));

        viewModel.CloseRequested += (_, _) => closed++;

        await viewModel.OkCommand.ExecuteAsync(parameter: null);

        closed.ShouldBe(expected: 0);
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.LastPlayedInstallation);
        viewModel.IsBusy.ShouldBeFalse();
        logger.Entries.Count(entry => entry.Level == LogLevel.Error).ShouldBe(expected: 1);
        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Save Options", message: Arg.Any<string>());
    }

    [ Fact ]
    public async Task OkWhileBusy_CannotExecute()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();

        viewModel.OkCommand.CanExecute(parameter: null).ShouldBeTrue();

        viewModel.IsBusy = true;

        viewModel.OkCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task Cancel_WhileSaving_CannotExecute()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();

        viewModel.CancelCommand.CanExecute(parameter: null).ShouldBeTrue();

        viewModel.IsBusy = true;

        viewModel.CancelCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task CanClose_FalseWhileSaving_TrueAfter()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();

        viewModel.CanClose.ShouldBeTrue();

        viewModel.IsBusy = true;

        viewModel.CanClose.ShouldBeFalse();

        viewModel.IsBusy = false;

        viewModel.CanClose.ShouldBeTrue();
    }

    [ Fact ]
    public async Task EscTwiceDuringSave_ClosesOnceAfterSave()
    {
        OptionsDialogViewModel                 viewModel = await LoadedAsync();
        TaskCompletionSource<ErrorOr<Success>> pending   = new();
        int                                    closed    = 0;

        _mediator.Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<Success>>(pending.Task));

        viewModel.CloseRequested += (_, _) => closed++;

        Task save = viewModel.OkCommand.ExecuteAsync(parameter: null);

        viewModel.IsBusy.ShouldBeTrue();

        viewModel.CancelCommand.Execute(parameter: null);
        viewModel.CancelCommand.Execute(parameter: null);

        closed.ShouldBe(expected: 0);

        pending.SetResult(Result.Success);

        await save;

        closed.ShouldBe(expected: 1);
    }

    [ Fact ]
    public async Task Initialise_LoadFails_ShowsInlineErrorAndDisablesOk()
    {
        _mediator.Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<LauncherOptions>>(Error.Failure(code: "Settings.ReadFailed", description: "unreadable")));

        OptionsDialogViewModel viewModel = new(_mediator, _dialogs, _styleState, new RecordingLogger<OptionsDialogViewModel>());

        await viewModel.InitialiseAsync();

        viewModel.ErrorMessage.ShouldNotBeNull();
        viewModel.OkCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task Initialise_LoadThrows_ShowsInlineErrorDisablesOkAndLogsOnce()
    {
        RecordingLogger<OptionsDialogViewModel> logger = new();

        _mediator.Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<LauncherOptions>>>(_ => throw new InvalidOperationException("boom"));

        OptionsDialogViewModel viewModel = new(_mediator, _dialogs, _styleState, logger);

        await viewModel.InitialiseAsync();

        viewModel.ErrorMessage.ShouldNotBeNull();
        viewModel.OkCommand.CanExecute(parameter: null).ShouldBeFalse();
        logger.Entries.Count(entry => entry.Level == LogLevel.Error).ShouldBe(expected: 1);
    }

    [ Fact ]
    public async Task EachRadioChoice_MapsToItsPreference()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();

        viewModel.IsDefaultInstallation = true;
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.DefaultInstallation);
        viewModel.IsDefaultInstallation.ShouldBeTrue();

        viewModel.IsLastPlayedInstallation = true;
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.LastPlayedInstallation);
        viewModel.IsDefaultInstallation.ShouldBeFalse();

        viewModel.IsLastOpenedInstallation = true;
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.LastOpenedInstallation);
        viewModel.IsLastPlayedInstallation.ShouldBeFalse();

        viewModel.IsNoInstallation = true;
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.NoInstallation);
        viewModel.IsLastOpenedInstallation.ShouldBeFalse();

        // An unchecked radio (the group clearing the previous choice) must not change the preference.
        viewModel.IsDefaultInstallation = false;
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.NoInstallation);
    }
}
