namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Restore;

/// <summary>Rolls <c>zoo.ini</c> back to a chosen snapshot's user settings through the same write path as Save (SDD §8.2).</summary>
/// <param name="InstallationId">The installation's identifier.</param>
/// <param name="SnapshotId">The identifier of the Historical or Original snapshot to restore.</param>
public sealed record RestoreIniSnapshotCommand(Guid InstallationId, Guid SnapshotId) : ICommand<ErrorOr<IniConfigResult>>;
