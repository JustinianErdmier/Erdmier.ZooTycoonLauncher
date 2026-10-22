namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Dialogs;

public sealed class OptionsDialogViewModelTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private void ReturnsStored(LauncherStartupPreference preference, bool closeAfterGameLaunch)
        => _mediator.Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<ErrorOr<LauncherOptions>>(new LauncherOptions(preference, closeAfterGameLaunch)));

    private void ReturnsSave(ErrorOr<Success> result)
        => _mediator.Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<ErrorOr<Success>>(result));

    private async Task<OptionsDialogViewModel> LoadedAsync(RecordingLogger<OptionsDialogViewModel>? logger = null)
    {
        ReturnsStored(LauncherStartupPreference.LastPlayedInstallation, closeAfterGameLaunch: true);

        OptionsDialogViewModel viewModel = new(_mediator, _dialogs, logger ?? new RecordingLogger<OptionsDialogViewModel>());

        await viewModel.InitialiseAsync();

        return viewModel;
    }

    [ Fact ]
    public async Task Initialise_LoadsTheStoredValues()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();

        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.LastPlayedInstallation);
        viewModel.CloseAfterGameLaunch.ShouldBeTrue();
        viewModel.ErrorMessage.ShouldBeNull();
    }

    [ Fact ]
    public async Task Ok_SendsOneSaveWithTheChosenValues_AndCloses()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();
        int                    closed    = 0;

        ReturnsSave(Result.Success);

        viewModel.CloseRequested += (_, _) => closed++;
        viewModel.StartupPreference    = LauncherStartupPreference.NoInstallation;
        viewModel.CloseAfterGameLaunch = false;

        await viewModel.OkCommand.ExecuteAsync(parameter: null);

        await _mediator.Received(requiredNumberOfCalls: 1)
                       .Send(Arg.Is<SaveLauncherOptionsCommand>(command => command.StartupPreference == LauncherStartupPreference.NoInstallation
                                                                           && !command.CloseAfterGameLaunch),
                             Arg.Any<CancellationToken>());
        closed.ShouldBe(expected: 1);
    }

    [ Fact ]
    public async Task Cancel_SendsNothing_AndCloses()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();
        int                    closed    = 0;

        viewModel.CloseRequested += (_, _) => closed++;

        viewModel.CancelCommand.Execute(parameter: null);

        closed.ShouldBe(expected: 1);
        await _mediator.DidNotReceive()
                       .Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Ok_SaveFails_ShowsErrorAndStaysOpen()
    {
        RecordingLogger<OptionsDialogViewModel> logger    = new();
        OptionsDialogViewModel                  viewModel = await LoadedAsync(logger);
        int                                     closed    = 0;

        ReturnsSave(Error.Failure(code: "Settings.StoreFailed", description: "The database is locked."));

        viewModel.CloseRequested += (_, _) => closed++;
        viewModel.StartupPreference    = LauncherStartupPreference.DefaultInstallation;
        viewModel.CloseAfterGameLaunch = false;

        await viewModel.OkCommand.ExecuteAsync(parameter: null);

        closed.ShouldBe(expected: 0);
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.DefaultInstallation);
        viewModel.CloseAfterGameLaunch.ShouldBeFalse();
        viewModel.IsBusy.ShouldBeFalse();
        logger.Entries.Count(entry => entry.Level == LogLevel.Warning).ShouldBe(expected: 1);
        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Save Options", message: "The database is locked.");
    }

    [ Fact ]
    public async Task Ok_SaveThrows_ShowsErrorAndStaysOpen()
    {
        RecordingLogger<OptionsDialogViewModel> logger    = new();
        OptionsDialogViewModel                  viewModel = await LoadedAsync(logger);
        int                                     closed    = 0;

        _mediator.Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<Success>>>(_ => throw new InvalidOperationException("boom"));

        viewModel.CloseRequested += (_, _) => closed++;

        await viewModel.OkCommand.ExecuteAsync(parameter: null);

        closed.ShouldBe(expected: 0);
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.LastPlayedInstallation);
        viewModel.IsBusy.ShouldBeFalse();
        logger.Entries.Count(entry => entry.Level == LogLevel.Error).ShouldBe(expected: 1);
        await _dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync(title: "Cannot Save Options", message: Arg.Any<string>());
    }

    [ Fact ]
    public async Task OkWhileBusy_CannotExecute()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();

        viewModel.OkCommand.CanExecute(parameter: null).ShouldBeTrue();

        viewModel.IsBusy = true;

        viewModel.OkCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task Initialise_LoadFails_ShowsInlineErrorAndDisablesOk()
    {
        _mediator.Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<LauncherOptions>>(Error.Failure(code: "Settings.ReadFailed", description: "unreadable")));

        OptionsDialogViewModel viewModel = new(_mediator, _dialogs, new RecordingLogger<OptionsDialogViewModel>());

        await viewModel.InitialiseAsync();

        viewModel.ErrorMessage.ShouldNotBeNull();
        viewModel.OkCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task EachRadioChoice_MapsToItsPreference()
    {
        OptionsDialogViewModel viewModel = await LoadedAsync();

        viewModel.IsDefaultInstallation = true;
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.DefaultInstallation);
        viewModel.IsDefaultInstallation.ShouldBeTrue();

        viewModel.IsLastPlayedInstallation = true;
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.LastPlayedInstallation);
        viewModel.IsDefaultInstallation.ShouldBeFalse();

        viewModel.IsLastOpenedInstallation = true;
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.LastOpenedInstallation);
        viewModel.IsLastPlayedInstallation.ShouldBeFalse();

        viewModel.IsNoInstallation = true;
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.NoInstallation);
        viewModel.IsLastOpenedInstallation.ShouldBeFalse();

        // An unchecked radio (the group clearing the previous choice) must not change the preference.
        viewModel.IsDefaultInstallation = false;
        viewModel.StartupPreference.ShouldBe(LauncherStartupPreference.NoInstallation);
    }
}
