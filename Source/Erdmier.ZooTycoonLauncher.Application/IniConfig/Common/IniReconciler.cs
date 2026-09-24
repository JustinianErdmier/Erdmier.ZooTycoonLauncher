namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;

/// <summary>
///     Brings the <c>Current</c> snapshot in line with the on-disk <c>zoo.ini</c> inside a caller-owned transaction, applying tiered drift (SDD §7.7): a first import when the
///     database is empty, archive-then-adopt for user-setting drift, and silent adoption for game-managed or unrecognised changes.
/// </summary>
public sealed class IniReconciler
{
    /// <summary>Reconciles <c>Current</c> with <paramref name="diskText" />. Does not commit.</summary>
    /// <param name="transaction">The open snapshot transaction.</param>
    /// <param name="diskText">The file's text as read from disk.</param>
    /// <param name="nowUtc">The timestamp for anything captured (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The on-disk values and what was done.</returns>
    public async Task<IniReconciliation> ReconcileAsync(IIniSnapshotTransaction transaction, string diskText, DateTime nowUtc, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<IniKeyId, string?> diskValues = ZooIniDefaults.ExtractValues(IniDocument.Parse(diskText));

        IniSnapshot? current = await transaction.GetCurrentAsync(cancellationToken);

        if (current is null)
        {
            await transaction.AddAsync(CreateImportSnapshot(IniSnapshotKind.Original, diskText, diskValues, nowUtc), cancellationToken);
            await transaction.AddAsync(CreateImportSnapshot(IniSnapshotKind.Current, diskText, diskValues, nowUtc), cancellationToken);

            return new IniReconciliation(diskValues, IniReconciliationOutcome.FirstImport);
        }

        Dictionary<IniKeyId, string?> currentValues = current.Values.ToDictionary(value => new IniKeyId(value.Section, value.Key), value => value.Value);

        IniDriftResult drift       = IniDriftDetector.Detect(currentValues, diskValues);
        bool           textChanged = !string.Equals(current.StructureBlob, diskText, StringComparison.Ordinal);

        if (drift.Kind == IniDriftKind.None
            && !textChanged)
        {
            return new IniReconciliation(diskValues, IniReconciliationOutcome.Unchanged);
        }

        bool archive = drift.Kind == IniDriftKind.UserSettings;

        if (archive)
        {
            await transaction.ArchiveCurrentAsync(IniSnapshotTrigger.Manual, nowUtc, cancellationToken);
        }

        List<IniValueChange> changes = drift.ChangedKeys
                                            .Select(id => new IniValueChange(id, diskValues.GetValueOrDefault(id), IniValueSource.Manual))
                                            .ToList();

        await transaction.UpdateCurrentAsync(diskText, changes, nowUtc, cancellationToken);

        return new IniReconciliation(diskValues, archive ? IniReconciliationOutcome.ArchivedAndAdopted : IniReconciliationOutcome.AdoptedSilently);
    }

    private static IniSnapshot CreateImportSnapshot(IniSnapshotKind kind, string text, IReadOnlyDictionary<IniKeyId, string?> values, DateTime nowUtc)
    {
        Guid id = Guid.CreateVersion7();

        return new IniSnapshot
        {
            Id            = id,
            Kind          = kind,
            Trigger       = IniSnapshotTrigger.OriginalImport,
            CapturedUtc   = nowUtc,
            StructureBlob = text,
            Values = values.Select(pair => new IniValue
                           {
                               SnapshotId = id,
                               Section    = pair.Key.Section,
                               Key        = pair.Key.Key,
                               Value      = pair.Value,
                               ValueKind  = ZooIniDefaults.TryGet(pair.Key, out IniKeySpec? spec) ? spec.Kind : IniValueKind.Str,
                               Source     = IniValueSource.OriginalImport
                           })
                           .ToList()
        };
    }
}
