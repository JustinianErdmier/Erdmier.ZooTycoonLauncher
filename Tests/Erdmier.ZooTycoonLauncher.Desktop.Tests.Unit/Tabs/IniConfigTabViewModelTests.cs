namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Tabs;

public sealed class IniConfigTabViewModelTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [ Fact ]
    public async Task NoIni_ShowsThePlaceholderAndNeverLoads()
    {
        IniConfigTabViewModel tab = new(IniEditorTestData.Installation(hasIni: false), iniErrorMessage: null, _mediator, _dialogs);

        await tab.ActivateAsync(CancellationToken.None);

        tab.Content.ShouldBeOfType<IniPlaceholderViewModel>().Headline.ShouldBe(expected: "No INI present");

        await _mediator.DidNotReceive().Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task IniErrorMessage_ShowsUnreadableThenRetriesOnActivation()
    {
        IniConfigTabViewModel tab = new(IniEditorTestData.Installation(), iniErrorMessage: "zoo.ini could not be read: locked", _mediator, _dialogs);

        tab.Content.ShouldBeOfType<IniPlaceholderViewModel>().Body.ShouldBe(expected: "zoo.ini could not be read: locked");

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await tab.ActivateAsync(CancellationToken.None);

        tab.Content.ShouldBeOfType<IniEditorViewModel>();
    }

    [ Fact ]
    public async Task Fresh_ShowsLoadingThenTheEditor()
    {
        IniConfigTabViewModel tab = new(IniEditorTestData.Installation(), iniErrorMessage: null, _mediator, _dialogs);

        tab.Content.ShouldBeOfType<IniPlaceholderViewModel>().Headline.ShouldBe(expected: "Loading zoo.ini…");

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await tab.ActivateAsync(CancellationToken.None);

        tab.Content.ShouldBeOfType<IniEditorViewModel>();
    }

    [ Fact ]
    public async Task FailedLoad_ShowsUnreadableWithTheDescription()
    {
        IniConfigTabViewModel tab = new(IniEditorTestData.Installation(), iniErrorMessage: null, _mediator, _dialogs);

        IniEditorTestData.ReturnsForGet(_mediator, Error.Unexpected(code: "Ini.StoreFailed", description: "The installation's settings history could not be opened: x"));

        await tab.ActivateAsync(CancellationToken.None);

        tab.Content.ShouldBeOfType<IniPlaceholderViewModel>().Body.ShouldBe(expected: "The installation's settings history could not be opened: x");
    }

    [ Fact ]
    public async Task ActivationWhileDirty_DoesNotReload()
    {
        IniConfigTabViewModel tab = new(IniEditorTestData.Installation(), iniErrorMessage: null, _mediator, _dialogs);

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await tab.ActivateAsync(CancellationToken.None);

        IniEditorTestData.Field<IniNumberFieldViewModel>((IniEditorViewModel)tab.Content, section: "user", key: "screenwidth").Value = 1024m;

        await tab.ActivateAsync(CancellationToken.None);

        await _mediator.Received(requiredNumberOfCalls: 1).Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>());
        tab.HasPendingChanges.ShouldBeTrue();
    }

    [ Fact ]
    public async Task ConcurrentActivation_LoadsOnceAndCreatesOneEditor()
    {
        IniConfigTabViewModel                        tab     = new(IniEditorTestData.Installation(), iniErrorMessage: null, _mediator, _dialogs);
        TaskCompletionSource<ErrorOr<IniConfigResult>> pending = new();

        _mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(pending.Task));

        Task first  = tab.ActivateAsync(CancellationToken.None);
        Task second = tab.ActivateAsync(CancellationToken.None);

        second.ShouldBeSameAs(first);

        pending.SetResult(IniEditorTestData.Result(("user", "screenwidth", "800")));

        await first;

        await _mediator.Received(requiredNumberOfCalls: 1).Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>());
        tab.Content.ShouldBeOfType<IniEditorViewModel>();
    }

    [ Fact ]
    public async Task HasPendingChanges_IsForwardedWithANotification()
    {
        IniConfigTabViewModel tab     = new(IniEditorTestData.Installation(), iniErrorMessage: null, _mediator, _dialogs);
        List<string?>         changes = [];

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await tab.ActivateAsync(CancellationToken.None);

        tab.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        IniEditorTestData.Field<IniNumberFieldViewModel>((IniEditorViewModel)tab.Content, section: "user", key: "screenwidth").Value = 1024m;

        changes.ShouldContain(nameof(IniConfigTabViewModel.HasPendingChanges));

        tab.DiscardChanges();

        tab.HasPendingChanges.ShouldBeFalse();
    }

    [ Fact ]
    public async Task SaveAsync_WithoutAnEditor_HasNothingToSave()
        => (await new IniConfigTabViewModel(IniEditorTestData.Installation(hasIni: false), iniErrorMessage: null, _mediator, _dialogs).SaveAsync(CancellationToken.None))
           .ShouldBeTrue();

    [ Fact ]
    public async Task ReloadFailsWhileDirty_KeepsTheEditorAndReportsTheError()
    {
        IniConfigTabViewModel tab = new(IniEditorTestData.Installation(), iniErrorMessage: null, _mediator, _dialogs);

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await tab.ActivateAsync(CancellationToken.None);

        TaskCompletionSource<ErrorOr<IniConfigResult>> pending = new();

        _mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(pending.Task));

        Task reload = tab.ActivateAsync(CancellationToken.None);

        IniEditorTestData.Field<IniNumberFieldViewModel>((IniEditorViewModel)tab.Content, section: "user", key: "screenwidth").Value = 1024m;

        pending.SetResult(Error.Unexpected(code: "Ini.StoreFailed", description: "The installation's settings history could not be opened: x"));

        await reload;

        tab.Content.ShouldBeOfType<IniEditorViewModel>();
        tab.HasPendingChanges.ShouldBeTrue();

        await _dialogs.Received(requiredNumberOfCalls: 1)
                      .ShowErrorAsync(title: "Cannot Reload zoo.ini", message: "The installation's settings history could not be opened: x");
    }

    [ Fact ]
    public async Task ReloadThrowsWhileDirty_KeepsTheEditorAndReportsTheError()
    {
        IniConfigTabViewModel tab = new(IniEditorTestData.Installation(), iniErrorMessage: null, _mediator, _dialogs);

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await tab.ActivateAsync(CancellationToken.None);

        TaskCompletionSource<ErrorOr<IniConfigResult>> pending = new();

        _mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(pending.Task));

        Task reload = tab.ActivateAsync(CancellationToken.None);

        IniEditorTestData.Field<IniNumberFieldViewModel>((IniEditorViewModel)tab.Content, section: "user", key: "screenwidth").Value = 1024m;

        pending.SetException(new InvalidOperationException(message: "database is locked"));

        await reload;

        tab.Content.ShouldBeOfType<IniEditorViewModel>();
        tab.HasPendingChanges.ShouldBeTrue();

        await _dialogs.Received(requiredNumberOfCalls: 1)
                      .ShowErrorAsync(title: "Cannot Reload zoo.ini", message: "zoo.ini could not be reloaded: database is locked");
    }
}
