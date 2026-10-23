namespace Erdmier.ZooTycoonNexus.Application.IniConfig.Common;

/// <summary>
///     Chooses which keys <see cref="IniWritePipeline" /> writes, once the file has been read and reconciled inside the open snapshot transaction.
/// </summary>
/// <param name="transaction">The open snapshot transaction, for selectors that read stored snapshots.</param>
/// <param name="reconciledValues">The effective values after drift reconciliation.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The values to write, keyed by registry id (empty when nothing needs writing), or an error that aborts the write.</returns>
public delegate Task<ErrorOr<IReadOnlyDictionary<IniKeyId, string>>> IniEditSelector(IIniSnapshotTransaction                transaction,
                                                                                    IReadOnlyDictionary<IniKeyId, string?> reconciledValues,
                                                                                    CancellationToken                      cancellationToken);
