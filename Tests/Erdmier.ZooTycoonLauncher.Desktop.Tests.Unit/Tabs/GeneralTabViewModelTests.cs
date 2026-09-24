namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Tabs;

public sealed class GeneralTabViewModelTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [ Fact ]
    public void CannotPlay_WithPendingIniChanges_HidesTheHint()
    {
        GeneralTabViewModel tab = Create(canPlay: false);

        tab.HasPendingIniChanges = true;

        tab.ShowPendingIniChangesHint.ShouldBeFalse();
    }

    [ Fact ]
    public void ReadyToPlay_WithPendingIniChanges_ShowsTheHint()
    {
        GeneralTabViewModel tab = Create(canPlay: true);

        tab.HasPendingIniChanges = true;

        tab.ShowPendingIniChangesHint.ShouldBeTrue();
    }

    [ Fact ]
    public void ReadyToPlay_WithoutPendingIniChanges_HidesTheHint()
    {
        GeneralTabViewModel tab = Create(canPlay: true);

        tab.ShowPendingIniChangesHint.ShouldBeFalse();
    }

    [ Fact ]
    public void HasPendingIniChangesChanging_RaisesPropertyChangedForTheHint()
    {
        GeneralTabViewModel tab     = Create(canPlay: true);
        List<string?>       changes = [];

        tab.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        tab.HasPendingIniChanges = true;

        changes.ShouldContain(nameof(GeneralTabViewModel.ShowPendingIniChangesHint));
    }

    private GeneralTabViewModel Create(bool canPlay) => new(IniEditorTestData.Installation(), canPlay, _mediator, _ => Task.CompletedTask);
}
