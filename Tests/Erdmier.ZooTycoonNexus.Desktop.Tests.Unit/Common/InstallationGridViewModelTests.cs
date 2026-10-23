using Erdmier.ZooTycoonNexus.Application.Installations.GetAll;

namespace Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.Common;

public sealed class InstallationGridViewModelTests
{
    [ Theory ]
    [ InlineData(true, true, false) ]
    [ InlineData(false, true, true) ]
    [ InlineData(true, false, true) ]
    [ InlineData(false, false, true) ]
    public async Task IsSelectionInvalid_FollowsValidity(bool hasExe, bool hasIni, bool expectedInvalid)
    {
        IMediator mediator = Substitute.For<IMediator>();
        var summary = new InstallationSummary(Guid.NewGuid(),
                                              Name: "Zoo",
                                              Path: @"C:\Zoo",
                                              InstallationValidity.From(hasExe, hasIni),
                                              IsDefault: true,
                                              AddedUtc: DateTime.UtcNow,
                                              ModifiedUtc: null,
                                              LastPlayedUtc: null,
                                              LastOpenedUtc: null);

        ErrorOr<IReadOnlyList<InstallationSummary>> all = ErrorOrFactory.From<IReadOnlyList<InstallationSummary>>([ summary ]);

        mediator.Send(Arg.Any<GetAllInstallationsQuery>(), Arg.Any<CancellationToken>()).Returns(new ValueTask<ErrorOr<IReadOnlyList<InstallationSummary>>>(all));

        using var grid = new InstallationGridViewModel(mediator, new WeakReferenceMessenger(), NullLogger<InstallationGridViewModel>.Instance);

        await grid.LoadAsync();

        grid.IsSelectionInvalid.ShouldBeFalse();

        grid.SelectedRow = grid.Rows.Single();

        grid.IsSelectionInvalid.ShouldBe(expectedInvalid);
    }
}
