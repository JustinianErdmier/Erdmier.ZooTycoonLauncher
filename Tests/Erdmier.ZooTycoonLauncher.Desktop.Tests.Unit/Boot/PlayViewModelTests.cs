namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Boot;

public sealed class PlayViewModelTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [ Fact ]
    public async Task ConfirmLeave_WithoutChanges_ProceedsWithoutPrompting()
    {
        PlayViewModel play = Create();

        (await play.ConfirmLeaveAsync(CancellationToken.None)).ShouldBeTrue();

        await _dialogs.DidNotReceive().ShowSaveChangesPromptAsync();
    }

    [ Fact ]
    public async Task PendingChanges_LockLaunch()
    {
        PlayViewModel play = await CreateWithPendingEditAsync();

        play.HasPendingChanges.ShouldBeTrue();
        play.GeneralTab.HasPendingIniChanges.ShouldBeTrue();
        play.GeneralTab.LaunchCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task Yes_SaveSucceeds_Proceeds()
    {
        PlayViewModel play = await CreateWithPendingEditAsync();

        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.Yes);

        IniEditorTestData.ReturnsForSave(_mediator, IniEditorTestData.Result(("user", "screenwidth", "1024")));

        (await play.ConfirmLeaveAsync(CancellationToken.None)).ShouldBeTrue();

        play.HasPendingChanges.ShouldBeFalse();
        play.GeneralTab.LaunchCommand.CanExecute(parameter: null).ShouldBeTrue();
    }

    [ Fact ]
    public async Task Yes_SaveFails_Stays()
    {
        PlayViewModel play = await CreateWithPendingEditAsync();

        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.Yes);

        IniEditorTestData.ReturnsForSave(_mediator, Error.Failure(code: "Ini.WriteFailed", description: "zoo.ini could not be saved: denied"));

        (await play.ConfirmLeaveAsync(CancellationToken.None)).ShouldBeFalse();

        play.HasPendingChanges.ShouldBeTrue();
    }

    [ Fact ]
    public async Task No_DiscardsAndProceeds()
    {
        PlayViewModel play = await CreateWithPendingEditAsync();

        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.No);

        (await play.ConfirmLeaveAsync(CancellationToken.None)).ShouldBeTrue();

        play.HasPendingChanges.ShouldBeFalse();
    }

    [ Fact ]
    public async Task Cancel_Stays()
    {
        PlayViewModel play = await CreateWithPendingEditAsync();

        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.Cancel);

        (await play.ConfirmLeaveAsync(CancellationToken.None)).ShouldBeFalse();

        play.HasPendingChanges.ShouldBeTrue();
    }

    [ Fact ]
    public void IniErrorMessage_ReachesBothTabs()
    {
        PlayViewModel play = Create(canPlay: false, iniErrorMessage: "zoo.ini could not be read: locked");

        play.GeneralTab.IsIniUnreadable.ShouldBeTrue();
        play.GeneralTab.IniErrorMessage.ShouldBe(expected: "zoo.ini could not be read: locked");
        play.IniConfigTab.Content.ShouldBeOfType<IniPlaceholderViewModel>().Body.ShouldBe(expected: "zoo.ini could not be read: locked");
    }

    [ Fact ]
    public async Task Started_CloseAfterLaunch_ButEditedWhileTheLaunchWasInFlight_DoesNotShutdown()
    {
        IApplicationLifecycle lifecycle = Substitute.For<IApplicationLifecycle>();
        PlayViewModel         play      = Create(lifecycle);

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await play.IniConfigTab.ActivateAsync(CancellationToken.None);

        TaskCompletionSource<ErrorOr<LaunchGameResult>> pending = new();

        _mediator.Send(Arg.Any<LaunchGameCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<LaunchGameResult>>(pending.Task));

        Task launch = play.GeneralTab.LaunchCommand.ExecuteAsync(parameter: null);

        IniEditorTestData.Field<IniNumberFieldViewModel>((IniEditorViewModel)play.IniConfigTab.Content, section: "user", key: "screenwidth").Value = 1024m;

        pending.SetResult(new LaunchGameResult(LaunchGameOutcome.Started, CloseAfterGameLaunch: true, FailureMessage: null));

        await launch;

        lifecycle.DidNotReceive().RequestShutdown();
    }

    [ Fact ]
    public async Task Started_CloseAfterLaunch_WithoutPendingEdits_Shutsdown()
    {
        IApplicationLifecycle lifecycle = Substitute.For<IApplicationLifecycle>();
        PlayViewModel         play      = Create(lifecycle);

        _mediator.Send(Arg.Any<LaunchGameCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<LaunchGameResult>>(new LaunchGameResult(LaunchGameOutcome.Started, CloseAfterGameLaunch: true, FailureMessage: null)));

        await play.GeneralTab.LaunchCommand.ExecuteAsync(parameter: null);

        lifecycle.Received(requiredNumberOfCalls: 1).RequestShutdown();
    }

    [ Fact ]
    public async Task Drifted_EditedWhileTheLaunchWasInFlightAndThePromptIsCancelled_DoesNotReboot()
    {
        bool rebootCalled = false;

        PlayViewModel play = PlayTestData.Create(_mediator,
                                                 _dialogs,
                                                 Substitute.For<IApplicationLifecycle>(),
                                                 rebootAsync: _ =>
                                                 {
                                                     rebootCalled = true;

                                                     return Task.CompletedTask;
                                                 });

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await play.IniConfigTab.ActivateAsync(CancellationToken.None);

        TaskCompletionSource<ErrorOr<LaunchGameResult>> pending = new();

        _mediator.Send(Arg.Any<LaunchGameCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<LaunchGameResult>>(pending.Task));

        Task launch = play.GeneralTab.LaunchCommand.ExecuteAsync(parameter: null);

        IniEditorTestData.Field<IniNumberFieldViewModel>((IniEditorViewModel)play.IniConfigTab.Content, section: "user", key: "screenwidth").Value = 1024m;

        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.Cancel);

        pending.SetResult(new LaunchGameResult(LaunchGameOutcome.Drifted, CloseAfterGameLaunch: false, FailureMessage: null));

        await launch;

        rebootCalled.ShouldBeFalse();
    }

    private PlayViewModel Create(bool canPlay = true, string? iniErrorMessage = null)
        => PlayTestData.Create(_mediator, _dialogs, Substitute.For<IApplicationLifecycle>(), canPlay, iniErrorMessage);

    private PlayViewModel Create(IApplicationLifecycle lifecycle) => PlayTestData.Create(_mediator, _dialogs, lifecycle);

    private Task<PlayViewModel> CreateWithPendingEditAsync() => PlayTestData.CreateWithPendingEditAsync(_mediator, _dialogs, Substitute.For<IApplicationLifecycle>());
}
