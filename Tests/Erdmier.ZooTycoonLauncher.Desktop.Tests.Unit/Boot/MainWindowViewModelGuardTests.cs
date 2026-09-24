namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Boot;

public sealed class MainWindowViewModelGuardTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IApplicationLifecycle _lifecycle = Substitute.For<IApplicationLifecycle>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [ Fact ]
    public async Task ManageInstallations_ChangeWithPendingEdits_GuardDeclines_KeepsTheEditsWithoutRebooting()
    {
        MainWindowViewModel window = Create();
        PlayViewModel       play   = await PlayTestData.CreateWithPendingEditAsync(_mediator, _dialogs, _lifecycle);

        window.IsBooting     = false;
        window.ActiveContent = play;

        _dialogs.ShowInstallationManagerAsync().Returns(true);
        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.Cancel);

        await window.ManageInstallationsCommand.ExecuteAsync(parameter: null);

        window.ActiveContent.ShouldBeSameAs(play);
        play.HasPendingChanges.ShouldBeTrue();

        await _mediator.DidNotReceive().Send(Arg.Any<BootCommand>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task ManageInstallations_ChangeWithPendingEdits_GuardAllows_RebootsTheOpenInstallation()
    {
        MainWindowViewModel window = Create();
        PlayViewModel       play   = await PlayTestData.CreateWithPendingEditAsync(_mediator, _dialogs, _lifecycle);

        window.IsBooting     = false;
        window.ActiveContent = play;

        _dialogs.ShowInstallationManagerAsync().Returns(true);
        _dialogs.ShowSaveChangesPromptAsync().Returns(SaveChangesChoice.No);

        _mediator.Send(Arg.Any<BootCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<BootResult>>(Error.Failure(code: "Boot.Failed", description: "The boot failed.")));

        await window.ManageInstallationsCommand.ExecuteAsync(parameter: null);

        await _mediator.Received(requiredNumberOfCalls: 1).Send(Arg.Is<BootCommand>(command => command.InstallationId == play.InstallationId), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task ManageInstallations_NoChangeWithPendingEdits_NeitherPromptsNorReboots()
    {
        MainWindowViewModel window = Create();
        PlayViewModel       play   = await PlayTestData.CreateWithPendingEditAsync(_mediator, _dialogs, _lifecycle);

        window.IsBooting     = false;
        window.ActiveContent = play;

        _dialogs.ShowInstallationManagerAsync().Returns(false);

        await window.ManageInstallationsCommand.ExecuteAsync(parameter: null);

        window.ActiveContent.ShouldBeSameAs(play);

        await _dialogs.DidNotReceive().ShowSaveChangesPromptAsync();
        await _mediator.DidNotReceive().Send(Arg.Any<BootCommand>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Exit_GuardDeclines_StaysOpen()
    {
        MainWindowViewModel window = Create();

        window.ActiveContent = new FakePendingChangesGuard(hasPendingChanges: true, allowLeave: false);

        await window.ExitCommand.ExecuteAsync(parameter: null);

        _lifecycle.DidNotReceive().RequestShutdown();
        window.IsCloseConfirmed.ShouldBeFalse();
    }

    [ Fact ]
    public async Task Exit_GuardAllows_ConfirmsTheCloseAndShutsDown()
    {
        MainWindowViewModel window = Create();

        window.ActiveContent = new FakePendingChangesGuard(hasPendingChanges: true, allowLeave: true);

        await window.ExitCommand.ExecuteAsync(parameter: null);

        _lifecycle.Received(requiredNumberOfCalls: 1).RequestShutdown();
        window.IsCloseConfirmed.ShouldBeTrue();
    }

    [ Fact ]
    public async Task Exit_WithoutAGuard_ShutsDown()
    {
        MainWindowViewModel window = Create();

        await window.ExitCommand.ExecuteAsync(parameter: null);

        _lifecycle.Received(requiredNumberOfCalls: 1).RequestShutdown();
    }

    [ Fact ]
    public void HasPendingChanges_ReflectsTheActiveGuard()
    {
        MainWindowViewModel window = Create();

        window.HasPendingChanges.ShouldBeFalse();

        window.ActiveContent = new FakePendingChangesGuard(hasPendingChanges: true, allowLeave: true);

        window.HasPendingChanges.ShouldBeTrue();
    }

    [ Fact ]
    public async Task OpenInstallation_GuardDeclines_KeepsTheCurrentContent()
    {
        MainWindowViewModel     window = Create();
        FakePendingChangesGuard guard  = new(hasPendingChanges: true, allowLeave: false);

        window.IsBooting     = false;
        window.ActiveContent = guard;

        await window.OpenInstallationPickerCommand.ExecuteAsync(parameter: null);

        window.ActiveContent.ShouldBeSameAs(guard);
        guard.Prompts.ShouldBe(expected: 1);
    }

    [ Fact ]
    public async Task ConfirmClose_DelegatesToTheGuard()
    {
        MainWindowViewModel window = Create();

        window.ActiveContent = new FakePendingChangesGuard(hasPendingChanges: true, allowLeave: false);

        (await window.ConfirmCloseAsync()).ShouldBeFalse();
    }

    [ Fact ]
    public async Task ConfirmClose_GuardThrows_ReturnsFalseWithoutThrowing()
    {
        MainWindowViewModel window = Create();

        window.ActiveContent = new FakePendingChangesGuard(hasPendingChanges: true, allowLeave: false, throwOnConfirm: new InvalidOperationException());

        (await window.ConfirmCloseAsync()).ShouldBeFalse();
    }

    private MainWindowViewModel Create()
        => new(_mediator,
               _lifecycle,
               _dialogs,
               new WeakReferenceMessenger(),
               NullLogger<MainWindowViewModel>.Instance,
               NullLogger<InstallationGridViewModel>.Instance);
}
