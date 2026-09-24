namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>One open transaction over an installation's snapshot database. Disposing without <see cref="CommitAsync" /> rolls every change back.</summary>
public interface IIniSnapshotTransaction : IAsyncDisposable
{
    /// <summary>Returns the <c>Current</c> snapshot with its values, or <see langword="null" /> when the database has never been imported.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IniSnapshot?> GetCurrentAsync(CancellationToken cancellationToken);

    /// <summary>Inserts a snapshot with its values (used by the first import: <c>Original</c>, then <c>Current</c>).</summary>
    /// <param name="snapshot">The snapshot to insert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(IniSnapshot snapshot, CancellationToken cancellationToken);

    /// <summary>Copies the <c>Current</c> snapshot's structure blob and every value row into a new <c>Historical</c> snapshot.</summary>
    /// <param name="trigger">Why the archive happened.</param>
    /// <param name="capturedUtc">The archive time (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ArchiveCurrentAsync(IniSnapshotTrigger trigger, DateTime capturedUtc, CancellationToken cancellationToken);

    /// <summary>Updates the <c>Current</c> snapshot in place: its structure blob, its <c>CapturedUtc</c>, and only the value rows named in <paramref name="changes" />.</summary>
    /// <param name="structureBlob">The new raw file text.</param>
    /// <param name="changes">The rows to insert, update, or (when <see cref="IniValueChange.Value" /> is <see langword="null" />) delete.</param>
    /// <param name="capturedUtc">The update time (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateCurrentAsync(string structureBlob, IReadOnlyList<IniValueChange> changes, DateTime capturedUtc, CancellationToken cancellationToken);

    /// <summary>Commits the transaction.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CommitAsync(CancellationToken cancellationToken);
}
