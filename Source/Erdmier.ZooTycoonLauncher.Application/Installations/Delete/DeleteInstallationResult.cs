namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>Outcome of a <see cref="DeleteInstallationCommand" />.</summary>
/// <param name="RemovedWasDefault"><see langword="true" /> when the removed row was the launcher default before removal.</param>
/// <param name="NewDefaultInstallationId">When <see cref="RemovedWasDefault" /> is <see langword="true" />, the id of the promoted replacement, or <see langword="null" /> when no installations remain.</param>
public sealed record DeleteInstallationResult(bool RemovedWasDefault, Guid? NewDefaultInstallationId);
