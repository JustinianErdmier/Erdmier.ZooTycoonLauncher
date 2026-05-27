namespace Erdmier.ZooTycoonLauncher.Application.Common.Messaging;

/// <summary>Published after any mutable field on a <see cref="GameInstallation" /> changes (rename, relocation, verification update).</summary>
/// <param name="InstallationId">The affected installation's identifier.</param>
public sealed record InstallationChangedMessage(Guid InstallationId);
