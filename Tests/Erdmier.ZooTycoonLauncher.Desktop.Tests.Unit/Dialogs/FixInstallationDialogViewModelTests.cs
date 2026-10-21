namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Dialogs;

public sealed class FixInstallationDialogViewModelTests
{
    private static readonly Guid InstallationId = Guid.CreateVersion7();

    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private static InstallationSummary Summary(InstallationValidity validity)
        => new(InstallationId,
               Name: "Main",
               Path: @"C:\ZT",
               validity,
               IsDefault: true,
               new DateTime(year: 2026, month: 10, day: 20, hour: 9, minute: 0, second: 0, DateTimeKind.Utc),
               ModifiedUtc: null,
               LastPlayedUtc: null,
               LastOpenedUtc: null);

    private void ReturnsVerification(bool hasExe, bool hasIni)
        => _mediator.Send(Arg.Any<VerifyInstallationQuery>(), Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<ErrorOr<VerificationResult>>(new VerificationResult(DirectoryExists: true, hasExe, hasIni)));

    private async Task<FixInstallationDialogViewModel> LoadedAsync(InstallationValidity validity, ILogger<FixInstallationDialogViewModel>? logger = null)
    {
        _mediator.Send(Arg.Any<GetInstallationByIdQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<InstallationSummary>>(Summary(validity)));
        ReturnsVerification(validity.HasExe, validity.HasIni);

        FixInstallationDialogViewModel viewModel = new(_mediator, _dialogs, logger ?? NullLogger<FixInstallationDialogViewModel>.Instance);

        await viewModel.InitialiseAsync(InstallationId);

        return viewModel;
    }

    private void ReturnsForCreate(ErrorOr<IniRecoveryResult> result)
        => _mediator.Send(Arg.Any<CreateDefaultIniCommand>(), Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<ErrorOr<IniRecoveryResult>>(result));

    [ Fact ]
    public async Task CreateIni_Success_FlipsToIniPresentAndMarksChanged()
    {
        FixInstallationDialogViewModel viewModel = await LoadedAsync(InstallationValidity.InvalidNoIni);

        ReturnsForCreate(new IniRecoveryResult(InstallationValidity.Valid));

        await viewModel.CreateIniCommand.ExecuteAsync(parameter: null);

        viewModel.HasIni.ShouldBeTrue();
        viewModel.HasChanges.ShouldBeTrue();
        viewModel.IniErrorMessage.ShouldBeNull();
        viewModel.IsBusy.ShouldBeFalse();
    }

    [ Fact ]
    public async Task CreateIni_AlreadyExists_Reprobes()
    {
        FixInstallationDialogViewModel viewModel = await LoadedAsync(InstallationValidity.InvalidNoIni);

        ReturnsForCreate(Error.Conflict(code: IniErrors.AlreadyExistsCode, description: "zoo.ini already exists"));
        ReturnsVerification(hasExe: true, hasIni: true);

        await viewModel.CreateIniCommand.ExecuteAsync(parameter: null);

        viewModel.HasIni.ShouldBeTrue();
        viewModel.HasChanges.ShouldBeTrue();
        viewModel.IniErrorMessage.ShouldBeNull();
        await _mediator.Received(requiredNumberOfCalls: 2).Send(Arg.Any<VerifyInstallationQuery>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task CreateIni_Fails_ShowsInlineError()
    {
        RecordingLogger<FixInstallationDialogViewModel> logger = new();

        FixInstallationDialogViewModel viewModel = await LoadedAsync(InstallationValidity.InvalidNoIni, logger);

        ReturnsForCreate(Error.Failure(code: "Ini.ReadOnly", description: "The folder is read-only."));

        await viewModel.CreateIniCommand.ExecuteAsync(parameter: null);

        logger.Entries.Count(entry => entry.Level == LogLevel.Warning).ShouldBe(expected: 1);

        viewModel.HasIni.ShouldBeFalse();
        viewModel.HasChanges.ShouldBeFalse();
        viewModel.IniErrorMessage.ShouldBe("The folder is read-only.");
    }

    [ Fact ]
    public async Task CreateIni_InstallationVanished_ShowsMissingAndUnloads()
    {
        FixInstallationDialogViewModel viewModel = await LoadedAsync(InstallationValidity.InvalidNoIni);

        ReturnsForCreate(IniErrors.InstallationNotFound(InstallationId));

        await viewModel.CreateIniCommand.ExecuteAsync(parameter: null);

        viewModel.IsLoaded.ShouldBeFalse();
        viewModel.ErrorMessage.ShouldBe(InstallationDialogMessages.InstallationMissing);
        viewModel.IniErrorMessage.ShouldBeNull();
    }

    [ Fact ]
    public async Task CreateIni_AlreadyExistsThenReprobeFails_LogsAndShowsUnexpected()
    {
        RecordingLogger<FixInstallationDialogViewModel> logger = new();

        FixInstallationDialogViewModel viewModel = await LoadedAsync(InstallationValidity.InvalidNoIni, logger);

        ReturnsForCreate(Error.Conflict(code: IniErrors.AlreadyExistsCode, description: "zoo.ini already exists"));
        _mediator.Send(Arg.Any<VerifyInstallationQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<VerificationResult>>(Error.Failure(code: "Verify.Failed", description: "boom")));

        await viewModel.CreateIniCommand.ExecuteAsync(parameter: null);

        viewModel.IniErrorMessage.ShouldBe(InstallationDialogMessages.UnexpectedFailure);
        logger.Entries.Count(entry => entry.Level == LogLevel.Warning).ShouldBe(expected: 1);
    }

    [ Fact ]
    public async Task LocateIni_Success_FlipsToIniPresentAndMarksChanged()
    {
        FixInstallationDialogViewModel viewModel = await LoadedAsync(InstallationValidity.InvalidNoIni);

        _dialogs.PickFileAsync(@"C:\ZT", title: "Locate zoo.ini").Returns(@"D:\Backup\zoo.ini");
        _mediator.Send(Arg.Any<LocateIniCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<IniRecoveryResult>>(new IniRecoveryResult(InstallationValidity.Valid)));

        await viewModel.LocateIniCommand.ExecuteAsync(parameter: null);

        viewModel.HasIni.ShouldBeTrue();
        viewModel.HasChanges.ShouldBeTrue();
        await _mediator.Received(requiredNumberOfCalls: 1)
                       .Send(Arg.Is<LocateIniCommand>(command => command.InstallationId == InstallationId && command.SourcePath == @"D:\Backup\zoo.ini"), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task IniActions_DisabledWithoutExe()
    {
        FixInstallationDialogViewModel viewModel = await LoadedAsync(InstallationValidity.InvalidNoExeOrIni);

        viewModel.CreateIniCommand.CanExecute(parameter: null).ShouldBeFalse();
        viewModel.LocateIniCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task IniActions_DisabledWhenIniPresent()
    {
        FixInstallationDialogViewModel viewModel = await LoadedAsync(InstallationValidity.Valid);

        viewModel.CreateIniCommand.CanExecute(parameter: null).ShouldBeFalse();
        viewModel.LocateIniCommand.CanExecute(parameter: null).ShouldBeFalse();
    }

    [ Fact ]
    public async Task IniActions_EnabledWithExeAndNoIni()
    {
        FixInstallationDialogViewModel viewModel = await LoadedAsync(InstallationValidity.InvalidNoIni);

        viewModel.CreateIniCommand.CanExecute(parameter: null).ShouldBeTrue();
        viewModel.LocateIniCommand.CanExecute(parameter: null).ShouldBeTrue();
    }

    [ Fact ]
    public async Task LocateIni_Cancelled_SendsNothing()
    {
        FixInstallationDialogViewModel viewModel = await LoadedAsync(InstallationValidity.InvalidNoIni);

        _dialogs.PickFileAsync(Arg.Any<string?>(), Arg.Any<string>()).Returns((string?)null);

        await viewModel.LocateIniCommand.ExecuteAsync(parameter: null);

        await _mediator.DidNotReceive().Send(Arg.Any<LocateIniCommand>(), Arg.Any<CancellationToken>());
        viewModel.HasChanges.ShouldBeFalse();
    }
}
