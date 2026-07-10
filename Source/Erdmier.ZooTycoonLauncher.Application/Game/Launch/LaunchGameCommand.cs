namespace Erdmier.ZooTycoonLauncher.Application.Game.Launch;

/// <summary>Request to launch <c>zoo.exe</c> for a single installation. SDD §7.10.</summary>
/// <param name="InstallationId">The identifier of the installation to launch.</param>
public sealed record LaunchGameCommand(Guid InstallationId) : ICommand<ErrorOr<LaunchGameResult>>;
