namespace Erdmier.ZooTycoonLauncher.Application.Common.Messaging;

/// <summary>Published after a <see cref="GameInstallation" /> row and its per-installation database have been removed.</summary>
/// <param name="InstallationId">The removed installation's identifier.</param>
public sealed record InstallationDeletedMessage(Guid InstallationId);
