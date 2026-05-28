namespace Erdmier.ZooTycoonLauncher.Application.Installations.SetDefault;

/// <summary>Sets <c>LauncherSettings.DefaultInstallationId</c> to the supplied installation's id.</summary>
/// <param name="InstallationId">The installation to promote to default.</param>
public sealed record SetDefaultInstallationCommand(Guid InstallationId) : ICommand<ErrorOr<Success>>;
