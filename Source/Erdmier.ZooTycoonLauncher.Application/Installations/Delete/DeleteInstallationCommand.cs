namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>Removes the installation, deletes its per-installation database, and (when needed) promotes a new default.</summary>
/// <param name="InstallationId">The installation to remove.</param>
public sealed record DeleteInstallationCommand(Guid InstallationId) : ICommand<ErrorOr<DeleteInstallationResult>>;

/// <summary>Outcome of a <see cref="DeleteInstallationCommand" />.</summary>
/// <param name="RemovedWasDefault"><see langword="true" /> when the removed row was the launcher default before removal.</param>
/// <param name="NewDefaultInstallationId">When <see cref="RemovedWasDefault" /> is <see langword="true" />, the id of the promoted replacement, or <see langword="null" /> when no installations remain.</param>
public sealed record DeleteInstallationResult(bool RemovedWasDefault, Guid? NewDefaultInstallationId);
