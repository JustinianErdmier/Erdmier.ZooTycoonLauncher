namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Boot;

internal static class PlayTestData
{
    public static PlayViewModel Create(IMediator             mediator,
                                       IDialogService        dialogs,
                                       IApplicationLifecycle lifecycle,
                                       bool                  canPlay         = true,
                                       string?               iniErrorMessage = null)
        => new(IniEditorTestData.Installation(), canPlay, _ => Task.CompletedTask, _ => Task.CompletedTask, lifecycle, dialogs, mediator, iniErrorMessage);

    // Opens the INI Config tab over a one-key file and edits that key, so the Play view holds an unsaved change.
    public static async Task<PlayViewModel> CreateWithPendingEditAsync(IMediator mediator, IDialogService dialogs, IApplicationLifecycle lifecycle)
    {
        PlayViewModel play = Create(mediator, dialogs, lifecycle);

        IniEditorTestData.ReturnsForGet(mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await play.IniConfigTab.ActivateAsync(CancellationToken.None);

        IniEditorTestData.Field<IniNumberFieldViewModel>((IniEditorViewModel)play.IniConfigTab.Content, section: "user", key: "screenwidth").Value = 1024m;

        return play;
    }
}
