namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Boot;

internal static class PlayTestData
{
    public static PlayViewModel Create(IMediator                      mediator,
                                       IDialogService                 dialogs,
                                       IApplicationLifecycle          lifecycle,
                                       bool                           canPlay         = true,
                                       string?                        iniErrorMessage = null,
                                       Func<CancellationToken, Task>? rebootAsync     = null,
                                       int                            initialTabIndex = 0,
                                       InstallationWriteAccess        writeAccess     = InstallationWriteAccess.Unknown)
        => new(IniEditorTestData.Installation(),
               canPlay,
               rebootAsync ?? (_ => Task.CompletedTask),
               _ => Task.CompletedTask,
               lifecycle,
               dialogs,
               mediator,
               NullLogger<PlayViewModel>.Instance,
               NullLogger<IniConfigTabViewModel>.Instance,
               NullLogger<IniEditorViewModel>.Instance,
               NullLogger<IniPlaceholderViewModel>.Instance,
               _ => Task.CompletedTask,
               new IniEditorStyleState(),
               iniErrorMessage,
               writeAccess,
               initialTabIndex);

    // Opens the Game Config tab over a one-key file and edits that key, so the Play view holds an unsaved change.
    public static async Task<PlayViewModel> CreateWithPendingEditAsync(IMediator mediator, IDialogService dialogs, IApplicationLifecycle lifecycle)
    {
        PlayViewModel play = Create(mediator, dialogs, lifecycle);

        IniEditorTestData.ReturnsForGet(mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await play.IniConfigTab.ActivateAsync(CancellationToken.None);

        IniEditorTestData.Field<IniNumberFieldViewModel>((IniEditorViewModel)play.IniConfigTab.Content, section: "user", key: "screenwidth").Value = 1024m;

        return play;
    }
}
