namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Boot;

public sealed class MainWindowViewModelStyleMenuTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private readonly IniEditorStyleState _styleState = new();

    private MainWindowViewModel CreateWindow()
        => new(_mediator,
               Substitute.For<IApplicationLifecycle>(),
               _dialogs,
               new WeakReferenceMessenger(),
               NullLogger<MainWindowViewModel>.Instance,
               NullLogger<InstallationGridViewModel>.Instance,
               NullLogger<PlayViewModel>.Instance,
               NullLogger<IniConfigTabViewModel>.Instance,
               NullLogger<IniEditorViewModel>.Instance,
               NullLogger<IniPlaceholderViewModel>.Instance,
               _styleState);

    private void ReturnsStored(LauncherOptions options)
        => _mediator.Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<ErrorOr<LauncherOptions>>(options));

    private void ReturnsSave(ErrorOr<Success> result)
        => _mediator.Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<ErrorOr<Success>>(result));

    [ Fact ]
    public async Task ChooseIniStyle_SavesOnlyTheStyleChangedAndApplies()
    {
        ReturnsStored(new LauncherOptions(LauncherStartupPreference.LastPlayedInstallation, CloseAfterGameLaunch: true, IniEditorStyle.Friendly));
        ReturnsSave(Result.Success);

        MainWindowViewModel window = CreateWindow();

        await window.ChooseIniStyleCommand.ExecuteAsync(parameter: null);

        await _mediator.Received(requiredNumberOfCalls: 1)
                       .Send(new SaveLauncherOptionsCommand(LauncherStartupPreference.LastPlayedInstallation, CloseAfterGameLaunch: true, IniEditorStyle.Ini),
                             Arg.Any<CancellationToken>());

        _styleState.Style.ShouldBe(IniEditorStyle.Ini);
        window.IsIniStyle.ShouldBeTrue();
        window.IsFriendlyStyle.ShouldBeFalse();
    }

    [ Fact ]
    public async Task ChooseFriendlyStyle_SavesOnlyTheStyleChangedAndApplies()
    {
        _styleState.Style = IniEditorStyle.Ini;

        ReturnsStored(new LauncherOptions(LauncherStartupPreference.NoInstallation, CloseAfterGameLaunch: false, IniEditorStyle.Ini));
        ReturnsSave(Result.Success);

        MainWindowViewModel window = CreateWindow();

        await window.ChooseFriendlyStyleCommand.ExecuteAsync(parameter: null);

        await _mediator.Received(requiredNumberOfCalls: 1)
                       .Send(new SaveLauncherOptionsCommand(LauncherStartupPreference.NoInstallation, CloseAfterGameLaunch: false, IniEditorStyle.Friendly),
                             Arg.Any<CancellationToken>());

        _styleState.Style.ShouldBe(IniEditorStyle.Friendly);
        window.IsFriendlyStyle.ShouldBeTrue();
        window.IsIniStyle.ShouldBeFalse();
    }

    [ Fact ]
    public async Task ChooseStyle_FailedSave_ShowsErrorChangesNothingAndSnapsTheChecksBack()
    {
        ReturnsStored(new LauncherOptions(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false, IniEditorStyle.Friendly));
        ReturnsSave(Error.Failure(code: "Settings.WriteFailed", description: "disk full"));

        MainWindowViewModel window = CreateWindow();

        // The radio item writes its own check on click, through the two-way binding.
        window.IsIniStyle      = true;
        window.IsFriendlyStyle = false;

        await window.ChooseIniStyleCommand.ExecuteAsync(parameter: null);

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync("Cannot Save Options", "disk full");

        _styleState.Style.ShouldBe(IniEditorStyle.Friendly);
        window.IsFriendlyStyle.ShouldBeTrue();
        window.IsIniStyle.ShouldBeFalse();
    }

    [ Fact ]
    public async Task ChooseStyle_QueryError_ShowsErrorSavesNothingAndSnapsBack()
    {
        _mediator.Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<LauncherOptions>>(Error.Failure(code: "Settings.ReadFailed", description: "unreadable")));

        MainWindowViewModel window = CreateWindow();

        window.IsIniStyle      = true;
        window.IsFriendlyStyle = false;

        await window.ChooseIniStyleCommand.ExecuteAsync(parameter: null);

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync("Cannot Save Options", "unreadable");
        await _mediator.DidNotReceive().Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>());

        _styleState.Style.ShouldBe(IniEditorStyle.Friendly);
        window.IsFriendlyStyle.ShouldBeTrue();
        window.IsIniStyle.ShouldBeFalse();
    }

    [ Fact ]
    public async Task ChooseStyle_UnexpectedException_ShowsGenericErrorAndSnapsBack()
    {
        _mediator.Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<LauncherOptions>>>(_ => throw new InvalidOperationException("boom"));

        MainWindowViewModel window = CreateWindow();

        window.IsIniStyle      = true;
        window.IsFriendlyStyle = false;

        await window.ChooseIniStyleCommand.ExecuteAsync(parameter: null);

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync("Cannot Save Options", InstallationDialogMessages.UnexpectedFailure);

        _styleState.Style.ShouldBe(IniEditorStyle.Friendly);
        window.IsFriendlyStyle.ShouldBeTrue();
        window.IsIniStyle.ShouldBeFalse();
    }

    [ Fact ]
    public async Task ChooseStyle_WhileASaveIsInFlight_DisablesBothCommands()
    {
        ReturnsStored(new LauncherOptions(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false, IniEditorStyle.Friendly));

        TaskCompletionSource<ErrorOr<Success>> pending = new();

        _mediator.Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<Success>>(pending.Task));

        MainWindowViewModel window = CreateWindow();

        Task running = window.ChooseIniStyleCommand.ExecuteAsync(parameter: null);

        window.ChooseIniStyleCommand.CanExecute(parameter: null).ShouldBeFalse();
        window.ChooseFriendlyStyleCommand.CanExecute(parameter: null).ShouldBeFalse();

        pending.SetResult(Result.Success);

        await running;

        window.ChooseIniStyleCommand.CanExecute(parameter: null).ShouldBeTrue();
        window.ChooseFriendlyStyleCommand.CanExecute(parameter: null).ShouldBeTrue();
    }

    [ Fact ]
    public async Task ChooseStyle_AlreadyStored_SavesNothing()
    {
        ReturnsStored(new LauncherOptions(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false, IniEditorStyle.Friendly));

        MainWindowViewModel window = CreateWindow();

        await window.ChooseFriendlyStyleCommand.ExecuteAsync(parameter: null);

        await _mediator.DidNotReceive().Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>());
        await _dialogs.DidNotReceive().ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());

        window.IsFriendlyStyle.ShouldBeTrue();
    }

    [ Fact ]
    public async Task ChoosingTheStoredStyleOverAStaleState_SavesNothingButSyncsTheState()
    {
        ReturnsStored(new LauncherOptions(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false, IniEditorStyle.Friendly));

        _styleState.Style = IniEditorStyle.Ini;

        MainWindowViewModel window = CreateWindow();

        await window.ChooseFriendlyStyleCommand.ExecuteAsync(parameter: null);

        await _mediator.DidNotReceive().Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>());

        _styleState.Style.ShouldBe(IniEditorStyle.Friendly);
        window.IsFriendlyStyle.ShouldBeTrue();
        window.IsIniStyle.ShouldBeFalse();
    }

    [ Fact ]
    public async Task ChoosingFriendlyWhenIniIsStoredButTheStateIsStale_SavesTheChoice()
    {
        ReturnsStored(new LauncherOptions(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false, IniEditorStyle.Ini));
        ReturnsSave(Result.Success);

        MainWindowViewModel window = CreateWindow();

        await window.ChooseFriendlyStyleCommand.ExecuteAsync(parameter: null);

        await _mediator.Received(requiredNumberOfCalls: 1)
                       .Send(new SaveLauncherOptionsCommand(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false, IniEditorStyle.Friendly),
                             Arg.Any<CancellationToken>());

        _styleState.Style.ShouldBe(IniEditorStyle.Friendly);
    }

    [ Fact ]
    public async Task StartupLoadsTheStoredStyleBeforeAnyEditor()
    {
        ReturnsStored(new LauncherOptions(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false, IniEditorStyle.Ini));

        _mediator.Send(Arg.Any<BootCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<BootResult>>(new BootResult(BootOutcome.NoGameInstallationFound, ActiveInstallation: null, LocatedCandidatePath: null)));

        MainWindowViewModel window = CreateWindow();

        await window.BootCommand.ExecuteAsync(parameter: null);

        _styleState.Style.ShouldBe(IniEditorStyle.Ini);
        window.IsIniStyle.ShouldBeTrue();
        window.IsFriendlyStyle.ShouldBeFalse();
    }

    [ Fact ]
    public void StyleMenuChecks_FollowTheState()
    {
        MainWindowViewModel window = CreateWindow();

        List<string?> raised = [];

        window.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _styleState.Style = IniEditorStyle.Ini;

        window.IsIniStyle.ShouldBeTrue();
        window.IsFriendlyStyle.ShouldBeFalse();
        raised.ShouldContain(nameof(MainWindowViewModel.IsFriendlyStyle));
        raised.ShouldContain(nameof(MainWindowViewModel.IsIniStyle));
    }

    private void ReturnsNoGameBoot()
        => _mediator.Send(Arg.Any<BootCommand>(), Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<ErrorOr<BootResult>>(new BootResult(BootOutcome.NoGameInstallationFound, ActiveInstallation: null, LocatedCandidatePath: null)));

    [ Fact ]
    public async Task Startup_AStyleSetWhileTheQueryIsInFlight_IsKept()
    {
        TaskCompletionSource<ErrorOr<LauncherOptions>> pending = new();

        _mediator.Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<LauncherOptions>>(pending.Task));

        ReturnsNoGameBoot();

        MainWindowViewModel window = CreateWindow();

        Task booting = window.BootCommand.ExecuteAsync(parameter: null);

        _styleState.Style = IniEditorStyle.Friendly;

        pending.SetResult(new LauncherOptions(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false, IniEditorStyle.Ini));

        await booting;

        _styleState.Style.ShouldBe(IniEditorStyle.Friendly);
        window.IsFriendlyStyle.ShouldBeTrue();
    }

    [ Fact ]
    public async Task Startup_AFailingQuery_LeavesTheStyleAndTheBootCompletes()
    {
        _mediator.Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<LauncherOptions>>(Error.Failure(code: "Settings.ReadFailed", description: "unreadable")));

        ReturnsNoGameBoot();

        MainWindowViewModel window = CreateWindow();

        await window.BootCommand.ExecuteAsync(parameter: null);

        _styleState.Style.ShouldBe(IniEditorStyle.Friendly);
        window.IsBooting.ShouldBeFalse();
        window.ActiveContent.ShouldBeOfType<NoGameInstallationFoundViewModel>();
    }

    [ Fact ]
    public async Task Startup_AThrowingQuery_LeavesTheStyleAndTheBootCompletes()
    {
        _mediator.Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<LauncherOptions>>>(_ => throw new InvalidOperationException("boom"));

        ReturnsNoGameBoot();

        MainWindowViewModel window = CreateWindow();

        await window.BootCommand.ExecuteAsync(parameter: null);

        _styleState.Style.ShouldBe(IniEditorStyle.Friendly);
        window.IsBooting.ShouldBeFalse();
        window.ActiveContent.ShouldBeOfType<NoGameInstallationFoundViewModel>();
    }

    [ Fact ]
    public async Task Startup_TheStoredStyleIsLoadedOnlyOnceAcrossAReboot()
    {
        ReturnsStored(new LauncherOptions(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false, IniEditorStyle.Ini));
        ReturnsNoGameBoot();

        MainWindowViewModel window = CreateWindow();

        await window.BootCommand.ExecuteAsync(parameter: null);
        await window.BootCommand.ExecuteAsync(parameter: null);

        await _mediator.Received(requiredNumberOfCalls: 1).Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>());
    }
}
