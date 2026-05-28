namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>Removes the installation, deletes its per-installation database, and (when needed) promotes a new default.</summary>
/// <param name="InstallationId">The installation to remove.</param>
public sealed record DeleteInstallationCommand(Guid InstallationId) : ICommand<ErrorOr<DeleteInstallationResult>>;
