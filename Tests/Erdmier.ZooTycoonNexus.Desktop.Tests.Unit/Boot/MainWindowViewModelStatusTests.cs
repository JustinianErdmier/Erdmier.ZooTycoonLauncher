namespace Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.Boot;

public sealed class MainWindowViewModelStatusTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [ Fact ]
    public async Task CannotPlayBoot_SetsIsStatusError()
    {
        MainWindowViewModel window = Create();

        StubBoot(new BootResult(BootOutcome.CannotPlay, InvalidSummary(), LocatedCandidatePath: null));

        await window.BootCommand.ExecuteAsync(parameter: null);

        window.IsStatusError.ShouldBeTrue();
    }

    [ Fact ]
    public async Task BootFailure_ClearsIsStatusError()
    {
        MainWindowViewModel window = Create();

        StubBoot(new BootResult(BootOutcome.CannotPlay, InvalidSummary(), LocatedCandidatePath: null));

        await window.BootCommand.ExecuteAsync(parameter: null);

        _mediator.Send(Arg.Any<BootCommand>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<BootResult>>(Error.Failure(code: "Boot.Failed", description: "The boot failed.")));

        await window.BootCommand.ExecuteAsync(parameter: null);

        window.IsStatusError.ShouldBeFalse();
    }

    [ Fact ]
    public async Task SuccessfulBoot_ClearsIsStatusError()
    {
        MainWindowViewModel window = Create();

        StubBoot(new BootResult(BootOutcome.CannotPlay, InvalidSummary(), LocatedCandidatePath: null));

        await window.BootCommand.ExecuteAsync(parameter: null);

        StubBoot(new BootResult(BootOutcome.NoGameInstallationFound, ActiveInstallation: null, LocatedCandidatePath: null));

        await window.BootCommand.ExecuteAsync(parameter: null);

        window.IsStatusError.ShouldBeFalse();
    }

    private static InstallationSummary InvalidSummary()
        => new(Guid.NewGuid(),
               Name: "Zoo",
               Path: @"C:\Zoo",
               InstallationValidity.InvalidNoExe,
               IsDefault: true,
               AddedUtc: DateTime.UtcNow,
               ModifiedUtc: null,
               LastPlayedUtc: null,
               LastOpenedUtc: null);

    private void StubBoot(BootResult result)
        => _mediator.Send(Arg.Any<BootCommand>(), Arg.Any<CancellationToken>()).Returns(new ValueTask<ErrorOr<BootResult>>(result));

    private MainWindowViewModel Create()
        => new(_mediator,
               Substitute.For<IApplicationLifecycle>(),
               Substitute.For<IDialogService>(),
               new WeakReferenceMessenger(),
               NullLogger<MainWindowViewModel>.Instance,
               NullLogger<InstallationGridViewModel>.Instance,
               NullLogger<PlayViewModel>.Instance,
               NullLogger<IniConfigTabViewModel>.Instance,
               NullLogger<IniEditorViewModel>.Instance,
               NullLogger<IniPlaceholderViewModel>.Instance,
               new IniEditorStyleState());
}
