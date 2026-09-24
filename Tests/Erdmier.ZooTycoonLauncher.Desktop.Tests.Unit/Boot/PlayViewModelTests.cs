using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

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

    private PlayViewModel Create(bool canPlay = true, string? iniErrorMessage = null)
        => new(IniEditorTestData.Installation(), canPlay, _ => Task.CompletedTask, Substitute.For<IApplicationLifecycle>(), _dialogs, _mediator, iniErrorMessage);

    private async Task<PlayViewModel> CreateWithPendingEditAsync()
    {
        PlayViewModel play = Create();

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await play.IniConfigTab.ActivateAsync(CancellationToken.None);

        IniEditorTestData.Field<IniNumberFieldViewModel>((IniEditorViewModel)play.IniConfigTab.Content, section: "user", key: "screenwidth").Value = 1024m;

        return play;
    }
}
