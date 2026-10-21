namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Tabs;

public sealed class IniConfigTabViewModelTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private IniConfigTabViewModel CreateTab(InstallationSummary installation, string? iniErrorMessage, ILogger<IniConfigTabViewModel>? logger = null)
        => new(installation,
               iniErrorMessage,
               _mediator,
               _dialogs,
               logger ?? NullLogger<IniConfigTabViewModel>.Instance,
               NullLogger<IniEditorViewModel>.Instance,
               NullLogger<IniPlaceholderViewModel>.Instance,
               _ => Task.CompletedTask);

    [ Fact ]
    public async Task NoIni_ShowsThePlaceholderAndNeverLoads()
    {
        IniConfigTabViewModel tab = CreateTab(IniEditorTestData.Installation(hasIni: false), iniErrorMessage: null);

        await tab.ActivateAsync(CancellationToken.None);

        IniPlaceholderViewModel placeholder = tab.Content.ShouldBeOfType<IniPlaceholderViewModel>();

        placeholder.Headline.ShouldBe(expected: "No INI present");
        placeholder.HasActions.ShouldBeTrue();

        await _mediator.DidNotReceive().Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task IniErrorMessage_ShowsUnreadableThenRetriesOnActivation()
    {
        IniConfigTabViewModel tab = CreateTab(IniEditorTestData.Installation(), iniErrorMessage: "zoo.ini could not be read: locked");

        tab.Content.ShouldBeOfType<IniPlaceholderViewModel>().Body.ShouldBe(expected: "zoo.ini could not be read: locked");

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await tab.ActivateAsync(CancellationToken.None);

        tab.Content.ShouldBeOfType<IniEditorViewModel>();
    }

    [ Fact ]
    public async Task Fresh_ShowsLoadingThenTheEditor()
    {
        IniConfigTabViewModel tab = CreateTab(IniEditorTestData.Installation(), iniErrorMessage: null);

        tab.Content.ShouldBeOfType<IniPlaceholderViewModel>().Headline.ShouldBe(expected: "Loading zoo.ini…");

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await tab.ActivateAsync(CancellationToken.None);

        tab.Content.ShouldBeOfType<IniEditorViewModel>();
    }

    [ Fact ]
    public async Task FailedLoad_ShowsUnreadableWithTheDescription()
    {
        IniConfigTabViewModel tab = CreateTab(IniEditorTestData.Installation(), iniErrorMessage: null);

        IniEditorTestData.ReturnsForGet(_mediator, Error.Unexpected(code: "Ini.StoreFailed", description: "The installation's settings history could not be read or updated: x"));

        await tab.ActivateAsync(CancellationToken.None);

        tab.Content.ShouldBeOfType<IniPlaceholderViewModel>().Body.ShouldBe(expected: "The installation's settings history could not be read or updated: x");
    }

    [ Fact ]
    public async Task ActivationWhileDirty_DoesNotReload()
    {
        IniConfigTabViewModel tab = CreateTab(IniEditorTestData.Installation(), iniErrorMessage: null);

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
        IniConfigTabViewModel                        tab     = CreateTab(IniEditorTestData.Installation(), iniErrorMessage: null);
        TaskCompletionSource<ErrorOr<IniConfigResult>> pending = new();

        _mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(pending.Task));

        Task first  = tab.ActivateAsync(CancellationToken.None);
        Task second = tab.ActivateAsync(CancellationToken.None);

        pending.SetResult(IniEditorTestData.Result(("user", "screenwidth", "800")));

        await Task.WhenAll(first, second);

        await _mediator.Received(requiredNumberOfCalls: 1).Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>());
        tab.Content.ShouldBeOfType<IniEditorViewModel>();
    }

    [ Fact ]
    public async Task ConcurrentActivation_FirstCallerCancels_SecondStillLoads()
    {
        IniConfigTabViewModel                          tab     = CreateTab(IniEditorTestData.Installation(), iniErrorMessage: null);
        TaskCompletionSource<ErrorOr<IniConfigResult>> pending = new();
        using CancellationTokenSource                  cts     = new();

        _mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                 .Returns(call => new ValueTask<ErrorOr<IniConfigResult>>(pending.Task.WaitAsync(call.Arg<CancellationToken>())));

        Task first  = tab.ActivateAsync(cts.Token);
        Task second = tab.ActivateAsync(CancellationToken.None);

        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => first);

        pending.SetResult(IniEditorTestData.Result(("user", "screenwidth", "800")));

        await second;

        tab.Content.ShouldBeOfType<IniEditorViewModel>();
    }

    [ Fact ]
    public async Task ConcurrentActivation_SecondCallerCancels_LoadContinues()
    {
        IniConfigTabViewModel                          tab     = CreateTab(IniEditorTestData.Installation(), iniErrorMessage: null);
        TaskCompletionSource<ErrorOr<IniConfigResult>> pending = new();
        using CancellationTokenSource                  cts     = new();

        _mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(pending.Task));

        Task first  = tab.ActivateAsync(CancellationToken.None);
        Task second = tab.ActivateAsync(cts.Token);

        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => second);

        pending.SetResult(IniEditorTestData.Result(("user", "screenwidth", "800")));

        await first;

        tab.Content.ShouldBeOfType<IniEditorViewModel>();
    }

    [ Fact ]
    public async Task Activate_LoadThrows_LogsError()
    {
        RecordingLogger<IniConfigTabViewModel> logger = new();
        IniConfigTabViewModel                  tab    = CreateTab(IniEditorTestData.Installation(), iniErrorMessage: null, logger);

        _mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<IniConfigResult>>>(_ => throw new IOException("boom"));

        await tab.ActivateAsync(CancellationToken.None);

        RecordingLogEntry entry = logger.Entries.ShouldHaveSingleItem();

        entry.Level.ShouldBe(LogLevel.Error);
        entry.Exception.ShouldBeOfType<IOException>();
    }

    [ Fact ]
    public async Task Activate_LoadReturnsError_LogsWarning()
    {
        RecordingLogger<IniConfigTabViewModel> logger = new();
        IniConfigTabViewModel                  tab    = CreateTab(IniEditorTestData.Installation(), iniErrorMessage: null, logger);

        IniEditorTestData.ReturnsForGet(_mediator, Error.Unexpected(code: "Ini.StoreFailed", description: "x"));

        await tab.ActivateAsync(CancellationToken.None);

        RecordingLogEntry entry = logger.Entries.ShouldHaveSingleItem();

        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Exception.ShouldBeNull();
    }

    [ Fact ]
    public async Task HasPendingChanges_IsForwardedWithANotification()
    {
        IniConfigTabViewModel tab     = CreateTab(IniEditorTestData.Installation(), iniErrorMessage: null);
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
        => (await CreateTab(IniEditorTestData.Installation(hasIni: false), iniErrorMessage: null).SaveAsync(CancellationToken.None))
           .ShouldBeTrue();

    [ Fact ]
    public async Task ReloadFailsWhileDirty_KeepsTheEditorAndReportsTheError()
    {
        IniConfigTabViewModel tab = CreateTab(IniEditorTestData.Installation(), iniErrorMessage: null);

        IniEditorTestData.ReturnsForGet(_mediator, IniEditorTestData.Result(("user", "screenwidth", "800")));

        await tab.ActivateAsync(CancellationToken.None);

        TaskCompletionSource<ErrorOr<IniConfigResult>> pending = new();

        _mediator.Send(Arg.Any<GetIniConfigQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniConfigResult>>(pending.Task));

        Task reload = tab.ActivateAsync(CancellationToken.None);

        IniEditorTestData.Field<IniNumberFieldViewModel>((IniEditorViewModel)tab.Content, section: "user", key: "screenwidth").Value = 1024m;

        pending.SetResult(Error.Unexpected(code: "Ini.StoreFailed", description: "The installation's settings history could not be read or updated: x"));

        await reload;

        tab.Content.ShouldBeOfType<IniEditorViewModel>();
        tab.HasPendingChanges.ShouldBeTrue();

        await _dialogs.Received(requiredNumberOfCalls: 1)
                      .ShowErrorAsync(title: "Cannot Reload zoo.ini", message: "The installation's settings history could not be read or updated: x");
    }

    [ Fact ]
    public async Task ReloadThrowsWhileDirty_KeepsTheEditorAndReportsTheError()
    {
        IniConfigTabViewModel tab = CreateTab(IniEditorTestData.Installation(), iniErrorMessage: null);

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
