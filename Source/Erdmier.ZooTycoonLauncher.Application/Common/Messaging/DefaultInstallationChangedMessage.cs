namespace Erdmier.ZooTycoonLauncher.Application.Common.Messaging;

/// <summary>Published when <c>LauncherSettings.DefaultInstallationId</c> changes value, including transitions to and from <see langword="null" />.</summary>
/// <param name="NewDefaultInstallationId">The new default installation's identifier, or <see langword="null" /> when no installation is registered.</param>
public sealed record DefaultInstallationChangedMessage(Guid? NewDefaultInstallationId);
