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
    }

    [ Fact ]
    public async Task ChooseStyle_FailedSave_ShowsErrorChangesNothingAndSnapsTheChecksBack()
    {
        ReturnsStored(new LauncherOptions(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false, IniEditorStyle.Friendly));
        ReturnsSave(Error.Failure(code: "Settings.WriteFailed", description: "disk full"));

        MainWindowViewModel window = CreateWindow();

        List<string?> raised = [];

        window.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        await window.ChooseIniStyleCommand.ExecuteAsync(parameter: null);

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync("Cannot Save Options", "disk full");

        _styleState.Style.ShouldBe(IniEditorStyle.Friendly);
        window.IsFriendlyStyle.ShouldBeTrue();
        raised.ShouldContain(nameof(MainWindowViewModel.IsFriendlyStyle));
        raised.ShouldContain(nameof(MainWindowViewModel.IsIniStyle));
    }

    [ Fact ]
    public async Task ChooseStyle_AlreadyActive_DoesNothing()
    {
        MainWindowViewModel window = CreateWindow();

        await window.ChooseFriendlyStyleCommand.ExecuteAsync(parameter: null);

        await _mediator.DidNotReceive().Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>());
        await _dialogs.DidNotReceive().ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
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
}
