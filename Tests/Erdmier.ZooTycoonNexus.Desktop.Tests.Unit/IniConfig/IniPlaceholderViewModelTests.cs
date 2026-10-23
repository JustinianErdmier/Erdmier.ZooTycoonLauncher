namespace Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.IniConfig;

public sealed class IniPlaceholderViewModelTests
{
    private static readonly Guid InstallationId = Guid.CreateVersion7();

    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private int _reopened;

    private IniPlaceholderViewModel Create(ILogger<IniPlaceholderViewModel>? logger = null, bool hasExe = true)
        => IniPlaceholderViewModel.NoIni(InstallationId,
                                         installationPath: @"C:\ZT",
                                         hasExe,
                                         _mediator,
                                         _dialogs,
                                         _ =>
                                         {
                                             _reopened++;

                                             return Task.CompletedTask;
                                         },
                                         logger ?? NullLogger<IniPlaceholderViewModel>.Instance);

    private void ReturnsForCreate(ErrorOr<IniRecoveryResult> result)
        => _mediator.Send(Arg.Any<CreateDefaultIniCommand>(), Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<ErrorOr<IniRecoveryResult>>(result));

    private void ReturnsForLocate(ErrorOr<IniRecoveryResult> result)
        => _mediator.Send(Arg.Any<LocateIniCommand>(), Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<ErrorOr<IniRecoveryResult>>(result));

    [ Fact ]
    public async Task Create_Success_ReopensOnTheIniTab()
    {
        ReturnsForCreate(new IniRecoveryResult(InstallationValidity.Valid));

        await Create().CreateCommand.ExecuteAsync(parameter: null);

        _reopened.ShouldBe(expected: 1);
        await _mediator.Received(requiredNumberOfCalls: 1).Send(Arg.Is<CreateDefaultIniCommand>(command => command.InstallationId == InstallationId), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Create_AlreadyExists_ReopensOnTheIniTab()
    {
        ReturnsForCreate(Error.Conflict(code: IniErrors.AlreadyExistsCode, description: "zoo.ini already exists"));

        await Create().CreateCommand.ExecuteAsync(parameter: null);

        _reopened.ShouldBe(expected: 1);
        await _dialogs.DidNotReceive().ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [ Fact ]
    public async Task Create_NotFound_ReopensOnTheIniTab()
    {
        ReturnsForCreate(IniErrors.InstallationNotFound(InstallationId));

        await Create().CreateCommand.ExecuteAsync(parameter: null);

        _reopened.ShouldBe(expected: 1);
        await _dialogs.DidNotReceive().ShowErrorAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [ Fact ]
    public async Task Create_Fails_ShowsErrorAndStays()
    {
        RecordingLogger<IniPlaceholderViewModel> logger = new();

        ReturnsForCreate(Error.Failure(code: "Ini.ReadOnly", description: "The folder is read-only."));

        IniPlaceholderViewModel placeholder = Create(logger);

        await placeholder.CreateCommand.ExecuteAsync(parameter: null);

        _reopened.ShouldBe(expected: 0);
        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Create zoo.ini", message: "The folder is read-only.");
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        placeholder.IsBusy.ShouldBeFalse();
    }

    [ Fact ]
    public async Task Create_Throws_ShowsTheMessageAndStays()
    {
        RecordingLogger<IniPlaceholderViewModel> logger = new();

        _mediator.Send(Arg.Any<CreateDefaultIniCommand>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<IniRecoveryResult>>>(_ => throw new InvalidOperationException("boom"));

        await Create(logger).CreateCommand.ExecuteAsync(parameter: null);

        _reopened.ShouldBe(expected: 0);
        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Create zoo.ini", message: "zoo.ini could not be created: boom");
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error);
    }

    [ Fact ]
    public async Task Locate_Cancelled_SendsNothing()
    {
        _dialogs.PickFileAsync(Arg.Any<string?>(), Arg.Any<string>()).Returns(Task.FromResult<string?>(result: null));

        await Create().LocateCommand.ExecuteAsync(parameter: null);

        await _dialogs.Received(requiredNumberOfCalls: 1).PickFileAsync(startPath: @"C:\ZT", title: "Locate zoo.ini");
        await _mediator.DidNotReceive().Send(Arg.Any<LocateIniCommand>(), Arg.Any<CancellationToken>());
        _reopened.ShouldBe(expected: 0);
    }

    [ Fact ]
    public async Task Locate_Success_SendsTheChosenPathAndReopens()
    {
        _dialogs.PickFileAsync(Arg.Any<string?>(), Arg.Any<string>()).Returns(Task.FromResult<string?>(@"D:\Backup\zoo.ini"));
        ReturnsForLocate(new IniRecoveryResult(InstallationValidity.Valid));

        await Create().LocateCommand.ExecuteAsync(parameter: null);

        await _mediator.Received(requiredNumberOfCalls: 1)
                       .Send(Arg.Is<LocateIniCommand>(command => command.InstallationId == InstallationId && command.SourcePath == @"D:\Backup\zoo.ini"), Arg.Any<CancellationToken>());
        _reopened.ShouldBe(expected: 1);
    }

    [ Fact ]
    public async Task Locate_Fails_ShowsTheCopyErrorAndStays()
    {
        _dialogs.PickFileAsync(Arg.Any<string?>(), Arg.Any<string>()).Returns(Task.FromResult<string?>(@"D:\Backup\notes.txt"));
        ReturnsForLocate(Error.Validation(code: "Ini.NotAnIni", description: "That file is not a zoo.ini."));

        await Create().LocateCommand.ExecuteAsync(parameter: null);

        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Copy zoo.ini", message: "That file is not a zoo.ini.");
        _reopened.ShouldBe(expected: 0);
    }

    [ Fact ]
    public async Task Locate_Throws_ShowsTheCopyMessageAndStays()
    {
        _dialogs.PickFileAsync(Arg.Any<string?>(), Arg.Any<string>()).Returns(Task.FromResult<string?>(@"D:\Backup\zoo.ini"));

        _mediator.Send(Arg.Any<LocateIniCommand>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<IniRecoveryResult>>>(_ => throw new InvalidOperationException("boom"));

        await Create().LocateCommand.ExecuteAsync(parameter: null);

        _reopened.ShouldBe(expected: 0);
        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Copy zoo.ini", message: "zoo.ini could not be copied: boom");
    }

    [ Fact ]
    public void Commands_DisabledWithoutExe()
    {
        IniPlaceholderViewModel placeholder = Create(hasExe: false);

        placeholder.CreateCommand.CanExecute(parameter: null).ShouldBeFalse();
        placeholder.LocateCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task Commands_DisabledWhileBusy()
    {
        TaskCompletionSource<ErrorOr<IniRecoveryResult>> pending = new();

        _mediator.Send(Arg.Any<CreateDefaultIniCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniRecoveryResult>>(pending.Task));

        IniPlaceholderViewModel placeholder = Create();

        placeholder.CreateCommand.CanExecute(parameter: null).ShouldBeTrue();

        Task running = placeholder.CreateCommand.ExecuteAsync(parameter: null);

        placeholder.IsBusy.ShouldBeTrue();
        placeholder.CreateCommand.CanExecute(parameter: null).ShouldBeFalse();
        placeholder.LocateCommand.CanExecute(parameter: null).ShouldBeFalse();

        pending.SetResult(new IniRecoveryResult(InstallationValidity.Valid));

        await running;

        placeholder.IsBusy.ShouldBeFalse();
        placeholder.LocateCommand.CanExecute(parameter: null).ShouldBeTrue();
    }

    [ Fact ]
    public void NoIni_HasActionsAndTheC9Body()
    {
        IniPlaceholderViewModel placeholder = Create();

        placeholder.HasActions.ShouldBeTrue();
        placeholder.Headline.ShouldBe(expected: "No INI present");
        placeholder.Body.ShouldBe(expected: "This installation has no zoo.ini on disk. Create a fresh one from the launcher's defaults, or locate an existing file to copy in.");
    }

    [ Fact ]
    public void NonNoIniCases_HaveNoActions()
    {
        IniPlaceholderViewModel.Loading().HasActions.ShouldBeFalse();
        IniPlaceholderViewModel.Unreadable(message: "x").HasActions.ShouldBeFalse();
        IniPlaceholderViewModel.Unreadable(message: "x").ShowPlainBody.ShouldBeTrue();
    }
}
