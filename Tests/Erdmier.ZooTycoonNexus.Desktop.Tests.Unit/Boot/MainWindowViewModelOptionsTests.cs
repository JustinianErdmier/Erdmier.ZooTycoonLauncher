namespace Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.Boot;

public sealed class MainWindowViewModelOptionsTests
{
    [ Fact ]
    public async Task OpenOptionsCommand_ShowsTheOptionsDialogueOnce()
    {
        IDialogService dialogs = Substitute.For<IDialogService>();

        MainWindowViewModel window = new(Substitute.For<IMediator>(),
                                         Substitute.For<IApplicationLifecycle>(),
                                         dialogs,
                                         new WeakReferenceMessenger(),
                                         NullLogger<MainWindowViewModel>.Instance,
                                         NullLogger<InstallationGridViewModel>.Instance,
                                         NullLogger<PlayViewModel>.Instance,
                                         NullLogger<IniConfigTabViewModel>.Instance,
                                         NullLogger<IniEditorViewModel>.Instance,
                                         NullLogger<IniPlaceholderViewModel>.Instance,
                                         new IniEditorStyleState());

        await window.OpenOptionsCommand.ExecuteAsync(parameter: null);

        await dialogs.Received(requiredNumberOfCalls: 1).ShowOptionsAsync();
    }
}
