using CommunityToolkit.Mvvm.Messaging;

using Erdmier.ZooTycoonLauncher.Desktop.ViewModels;

using Microsoft.Extensions.Logging.Abstractions;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Boot;

public sealed class MainWindowViewModelGuardTests
{
    private readonly IApplicationLifecycle _lifecycle = Substitute.For<IApplicationLifecycle>();

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
        => new(Substitute.For<IMediator>(), _lifecycle, Substitute.For<IDialogService>(), new WeakReferenceMessenger(), NullLogger<MainWindowViewModel>.Instance);
}
