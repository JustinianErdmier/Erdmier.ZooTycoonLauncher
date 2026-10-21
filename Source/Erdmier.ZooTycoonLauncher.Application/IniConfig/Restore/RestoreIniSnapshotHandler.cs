namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Restore;

/// <summary>
///     Handler for <see cref="RestoreIniSnapshotCommand" />. Runs the shared <see cref="IniWritePipeline" /> (SDD §8.2) with a selector that keeps every user setting whose
///     effective value in the chosen snapshot differs from the reconciled on-disk value.
/// </summary>
public sealed class RestoreIniSnapshotHandler : ICommandHandler<RestoreIniSnapshotCommand, ErrorOr<IniConfigResult>>
{
    private readonly IniWritePipeline _pipeline;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="pipeline">The shared INI write pipeline.</param>
    public RestoreIniSnapshotHandler(IniWritePipeline pipeline) => _pipeline = pipeline;

    /// <inheritdoc />
    public async ValueTask<ErrorOr<IniConfigResult>> Handle(RestoreIniSnapshotCommand command, CancellationToken cancellationToken)
        => await _pipeline.WriteAsync(command.InstallationId,
                                      async (transaction, reconciledValues, token) =>
                                      {
                                          IniSnapshot? snapshot = await transaction.GetByIdAsync(command.SnapshotId, token);

                                          if (snapshot is null)
                                          {
                                              return IniErrors.SnapshotNotFound(command.SnapshotId);
                                          }

                                          // The reconciled values, never the stored Current, are the baseline: an external edit since the editor opened is overwritten (spec C8).
                                          Dictionary<IniKeyId, string> edits = [ ];

                                          foreach (IniDiffRow row in IniSnapshotComparer.Compare(reconciledValues, IniSnapshotComparer.ValuesOf(snapshot)).Rows)
                                          {
                                              edits[row.Id] = row.Snapshot;
                                          }

                                          return (ErrorOr<IReadOnlyDictionary<IniKeyId, string>>)edits;
                                      },
                                      cancellationToken);
}
