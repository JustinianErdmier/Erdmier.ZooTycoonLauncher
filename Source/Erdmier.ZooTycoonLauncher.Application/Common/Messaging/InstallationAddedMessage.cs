namespace Erdmier.ZooTycoonLauncher.Application.Common.Messaging;

/// <summary>Published after a <see cref="GameInstallation" /> has been persisted to <c>Launcher.db</c>.</summary>
/// <param name="InstallationId">The newly created installation's identifier.</param>
public sealed record InstallationAddedMessage(Guid InstallationId);
