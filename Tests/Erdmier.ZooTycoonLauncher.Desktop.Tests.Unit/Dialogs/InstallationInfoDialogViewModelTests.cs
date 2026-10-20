namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Dialogs;

public sealed class InstallationInfoDialogViewModelTests
{
    private static readonly Guid InstallationId = Guid.CreateVersion7();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private static InstallationSummary Summary()
        => new(InstallationId,
               Name: "Main",
               Path: @"C:\ZT",
               InstallationValidity.Valid,
               IsDefault: true,
               new DateTime(year: 2026, month: 10, day: 20, hour: 9, minute: 0, second: 0, DateTimeKind.Utc),
               ModifiedUtc: null,
               LastPlayedUtc: null,
               LastOpenedUtc: null);

    private async Task<InstallationInfoDialogViewModel> LoadedAsync(ErrorOr<int> count)
    {
        _mediator.Send(Arg.Any<GetInstallationByIdQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<InstallationSummary>>(Summary()));
        _mediator.Send(Arg.Any<GetIniHistoryCountQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<int>>(count));

        InstallationInfoDialogViewModel viewModel = new(_mediator, NullLogger<InstallationInfoDialogViewModel>.Instance);

        await viewModel.InitialiseAsync(InstallationId);

        return viewModel;
    }

    [ Fact ]
    public async Task HistoryEntries_Zero_ReadsZeroSnapshots()
    {
        InstallationInfoDialogViewModel viewModel = await LoadedAsync(0);

        viewModel.HistoryEntriesText.ShouldBe(expected: "0 snapshots");
    }

    [ Fact ]
    public async Task HistoryEntries_One_ReadsOneSnapshot()
    {
        InstallationInfoDialogViewModel viewModel = await LoadedAsync(1);

        viewModel.HistoryEntriesText.ShouldBe(expected: "1 snapshot");
    }

    [ Fact ]
    public async Task HistoryEntries_Many_ReadsTheCount()
    {
        InstallationInfoDialogViewModel viewModel = await LoadedAsync(7);

        viewModel.HistoryEntriesText.ShouldBe(expected: "7 snapshots");
    }

    [ Fact ]
    public async Task HistoryEntries_QueryFails_ReadsDash()
    {
        InstallationInfoDialogViewModel viewModel = await LoadedAsync(Error.Failure(code: "Ini.StoreFailed"));

        viewModel.HistoryEntriesText.ShouldBe(expected: "—");
        viewModel.ErrorMessage.ShouldBeNull();
        viewModel.Name.ShouldBe(expected: "Main");
    }

    [ Fact ]
    public async Task HistoryEntries_QueryThrows_ReadsDash()
    {
        _mediator.Send(Arg.Any<GetInstallationByIdQuery>(), Arg.Any<CancellationToken>())
                 .Returns(new ValueTask<ErrorOr<InstallationSummary>>(Summary()));
        _mediator.Send(Arg.Any<GetIniHistoryCountQuery>(), Arg.Any<CancellationToken>())
                 .Returns<ValueTask<ErrorOr<int>>>(_ => throw new InvalidOperationException("boom"));

        InstallationInfoDialogViewModel viewModel = new(_mediator, NullLogger<InstallationInfoDialogViewModel>.Instance);

        await viewModel.InitialiseAsync(InstallationId);

        viewModel.HistoryEntriesText.ShouldBe(expected: "—");
        viewModel.ErrorMessage.ShouldBeNull();
    }
}
