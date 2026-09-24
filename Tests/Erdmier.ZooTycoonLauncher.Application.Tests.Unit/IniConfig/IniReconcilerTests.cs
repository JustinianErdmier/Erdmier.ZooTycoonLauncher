namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class IniReconcilerTests
{
    private readonly IniReconciler _reconciler = new();

    [ Fact ]
    public async Task Reconcile_NoCurrent_ImportsOriginalThenCurrent()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(current: null);
        List<IniSnapshot>       added       = [];

        transaction.When(t => t.AddAsync(Arg.Any<IniSnapshot>(), Arg.Any<CancellationToken>()))
                   .Do(call => added.Add(call.Arg<IniSnapshot>()));

        IniReconciliation result = await _reconciler.ReconcileAsync(transaction, IniTestData.Sample, IniTestData.Now, CancellationToken.None);

        result.Outcome.ShouldBe(IniReconciliationOutcome.FirstImport);
        added.Select(snapshot => snapshot.Kind).ShouldBe([IniSnapshotKind.Original, IniSnapshotKind.Current]);
        added.ShouldAllBe(snapshot => snapshot.Trigger == IniSnapshotTrigger.OriginalImport && snapshot.StructureBlob == IniTestData.Sample);
        added[1].Values.Count.ShouldBe(expected: 5);
        added[1].Values.ShouldAllBe(value => value.Source == IniValueSource.OriginalImport && value.SnapshotId == added[1].Id);
        added[0].Id.ShouldNotBe(added[1].Id);
        result.Values[new IniKeyId(Section: "user", Key: "screenwidth")].ShouldBe(expected: "800");

        await transaction.DidNotReceive().ArchiveCurrentAsync(Arg.Any<IniSnapshotTrigger>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Reconcile_NoDriftAndIdenticalText_WritesNothing()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(IniTestData.CurrentSnapshot(IniTestData.Sample));

        IniReconciliation result = await _reconciler.ReconcileAsync(transaction, IniTestData.Sample, IniTestData.Now, CancellationToken.None);

        result.Outcome.ShouldBe(IniReconciliationOutcome.Unchanged);

        await transaction.DidNotReceive().AddAsync(Arg.Any<IniSnapshot>(), Arg.Any<CancellationToken>());
        await transaction.DidNotReceive().ArchiveCurrentAsync(Arg.Any<IniSnapshotTrigger>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await transaction.DidNotReceive().UpdateCurrentAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<IniValueChange>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Reconcile_GameManagedChange_AdoptsWithoutArchiving()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(IniTestData.CurrentSnapshot(IniTestData.Sample));
        string                  disk        = IniTestData.Sample.Replace(oldValue: "lastWindowX=10", newValue: "lastWindowX=250");

        IniReconciliation result = await _reconciler.ReconcileAsync(transaction, disk, IniTestData.Now, CancellationToken.None);

        result.Outcome.ShouldBe(IniReconciliationOutcome.AdoptedSilently);

        await transaction.DidNotReceive().ArchiveCurrentAsync(Arg.Any<IniSnapshotTrigger>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());

        await transaction.Received(requiredNumberOfCalls: 1)
                         .UpdateCurrentAsync(disk,
                                             Arg.Is<IReadOnlyList<IniValueChange>>(changes => changes.Count == 1
                                                                                             && changes[0].Id == new IniKeyId("UI", "lastWindowX")
                                                                                             && changes[0].Value == "250"
                                                                                             && changes[0].Source == IniValueSource.Manual),
                                             IniTestData.Now,
                                             Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Reconcile_UserSettingChange_ArchivesAsManualThenAdopts()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(IniTestData.CurrentSnapshot(IniTestData.Sample));
        string                  disk        = IniTestData.Sample.Replace(oldValue: "screenwidth=800", newValue: "screenwidth=1024");

        IniReconciliation result = await _reconciler.ReconcileAsync(transaction, disk, IniTestData.Now, CancellationToken.None);

        result.Outcome.ShouldBe(IniReconciliationOutcome.ArchivedAndAdopted);
        result.Values[new IniKeyId(Section: "user", Key: "screenwidth")].ShouldBe(expected: "1024");

        Received.InOrder(() =>
        {
            transaction.ArchiveCurrentAsync(IniSnapshotTrigger.Manual, IniTestData.Now, Arg.Any<CancellationToken>());
            transaction.UpdateCurrentAsync(disk, Arg.Any<IReadOnlyList<IniValueChange>>(), IniTestData.Now, Arg.Any<CancellationToken>());
        });
    }

    [ Fact ]
    public async Task Reconcile_TextOnlyChange_UpdatesTheBlobWithNoRowChanges()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(IniTestData.CurrentSnapshot(IniTestData.Sample));
        string                  disk        = IniTestData.Sample.Replace(oldValue: "ag=0", newValue: "ag=1");

        IniReconciliation result = await _reconciler.ReconcileAsync(transaction, disk, IniTestData.Now, CancellationToken.None);

        result.Outcome.ShouldBe(IniReconciliationOutcome.AdoptedSilently);

        await transaction.DidNotReceive().ArchiveCurrentAsync(Arg.Any<IniSnapshotTrigger>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());

        await transaction.Received(requiredNumberOfCalls: 1)
                         .UpdateCurrentAsync(disk, Arg.Is<IReadOnlyList<IniValueChange>>(changes => changes.Count == 0), IniTestData.Now, Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Reconcile_KeyRemovedFromTheFile_ProducesANullChange()
    {
        IIniSnapshotTransaction transaction = IniTestData.Transaction(IniTestData.CurrentSnapshot(IniTestData.Sample));
        string                  disk        = IniTestData.Sample.Replace(oldValue: "lastWindowX=10\r\n", newValue: "");

        await _reconciler.ReconcileAsync(transaction, disk, IniTestData.Now, CancellationToken.None);

        await transaction.Received(requiredNumberOfCalls: 1)
                         .UpdateCurrentAsync(disk,
                                             Arg.Is<IReadOnlyList<IniValueChange>>(changes => changes.Count == 1 && changes[0].Value == null),
                                             IniTestData.Now,
                                             Arg.Any<CancellationToken>());
    }
}
