namespace Erdmier.ZooTycoonNexus.Infrastructure.Persistence.Installation;

/// <summary>An open EF Core transaction over one installation's snapshot database. Owns the context and the transaction; disposing without committing rolls back.</summary>
internal sealed class IniSnapshotTransaction : IIniSnapshotTransaction
{
    private readonly InstallationDbContext _context;

    private readonly IDbContextTransaction _transaction;

    private bool _disposed;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="context">The open context (owned).</param>
    /// <param name="transaction">The begun transaction (owned).</param>
    public IniSnapshotTransaction(InstallationDbContext context, IDbContextTransaction transaction)
    {
        _context     = context;
        _transaction = transaction;
    }

    /// <inheritdoc />
    public Task<IniSnapshot?> GetCurrentAsync(CancellationToken cancellationToken)
        => _context.Snapshots
                   .Include(snapshot => snapshot.Values)
                   .SingleOrDefaultAsync(snapshot => snapshot.Kind == IniSnapshotKind.Current, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<IniSnapshot>> ListHistoricalAsync(CancellationToken cancellationToken)
        => await _context.Snapshots
                         .AsNoTracking()
                         .Include(snapshot => snapshot.Values)
                         .Where(snapshot => snapshot.Kind == IniSnapshotKind.Historical)
                         .OrderByDescending(snapshot => snapshot.CapturedUtc)
                         // Snapshots archived at the same instant (a Manual and a LauncherGui archive in one Save) fall back to their trigger. Trigger is stored as its
                         // name, so ascending puts LauncherGui before Manual: a Save or Restore archives the Manual one first, so the LauncherGui one is the newer.
                         .ThenBy(snapshot => snapshot.Trigger)
                         // Same trigger at the same instant: the time-ordered version 7 ids decide.
                         .ThenByDescending(snapshot => snapshot.Id)
                         .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IniSnapshot?> GetOriginalAsync(CancellationToken cancellationToken)
        => _context.Snapshots
                   .AsNoTracking()
                   .Include(snapshot => snapshot.Values)
                   .SingleOrDefaultAsync(snapshot => snapshot.Kind == IniSnapshotKind.Original, cancellationToken);

    /// <inheritdoc />
    public Task<IniSnapshot?> GetByIdAsync(Guid snapshotId, CancellationToken cancellationToken)
        => _context.Snapshots
                   .AsNoTracking()
                   .Include(snapshot => snapshot.Values)
                   .SingleOrDefaultAsync(snapshot => snapshot.Id == snapshotId && snapshot.Kind != IniSnapshotKind.Current, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountHistoricalAsync(CancellationToken cancellationToken)
        => _context.Snapshots.CountAsync(snapshot => snapshot.Kind == IniSnapshotKind.Historical, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(IniSnapshot snapshot, CancellationToken cancellationToken)
    {
        _context.Snapshots.Add(snapshot);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ArchiveCurrentAsync(IniSnapshotTrigger trigger, DateTime capturedUtc, CancellationToken cancellationToken)
    {
        IniSnapshot current = await RequireCurrentAsync(cancellationToken);
        Guid        id      = Guid.CreateVersion7();

        IniSnapshot historical = new()
        {
            Id            = id,
            Kind          = IniSnapshotKind.Historical,
            Trigger       = trigger,
            CapturedUtc   = capturedUtc,
            StructureBlob = current.StructureBlob,
            Values = current.Values.Select(value => new IniValue
                            {
                                SnapshotId = id,
                                Section    = value.Section,
                                Key        = value.Key,
                                Value      = value.Value,
                                ValueKind  = value.ValueKind,
                                Source     = value.Source
                            })
                            .ToList()
        };

        _context.Snapshots.Add(historical);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateCurrentAsync(string structureBlob, IReadOnlyList<IniValueChange> changes, DateTime capturedUtc, CancellationToken cancellationToken)
    {
        IniSnapshot current = await RequireCurrentAsync(cancellationToken);

        current.StructureBlob = structureBlob;
        current.CapturedUtc   = capturedUtc;

        foreach (IniValueChange change in changes)
        {
            IniValue? row = current.Values.FirstOrDefault(value => new IniKeyId(value.Section, value.Key) == change.Id);

            if (change.Value is null)
            {
                if (row is not null)
                {
                    current.Values.Remove(row);
                    _context.IniValues.Remove(row);
                }

                continue;
            }

            if (row is null)
            {
                current.Values.Add(new IniValue
                {
                    SnapshotId = current.Id,
                    Section    = change.Id.Section,
                    Key        = change.Id.Key,
                    Value      = change.Value,
                    ValueKind  = ZooIniDefaults.TryGet(change.Id, out IniKeySpec? spec) ? spec.Kind : IniValueKind.Str,
                    Source     = change.Source
                });

                continue;
            }

            row.Value  = change.Value;
            row.Source = change.Source;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken) => _transaction.CommitAsync(cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            // Disposing an uncommitted EF Core transaction rolls it back.
            await _transaction.DisposeAsync();
        }
        finally
        {
            // Always disposed, even when the rollback above throws, so a rollback failure can never leak the context.
            await _context.DisposeAsync();
        }
    }

    private async Task<IniSnapshot> RequireCurrentAsync(CancellationToken cancellationToken)
        => await GetCurrentAsync(cancellationToken) ?? throw new InvalidOperationException(message: "The installation's database has no Current snapshot.");
}
