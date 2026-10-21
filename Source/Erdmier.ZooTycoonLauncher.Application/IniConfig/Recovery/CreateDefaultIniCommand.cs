namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Recovery;

/// <summary>Creates a missing <c>zoo.ini</c> from the launcher's defaults without ever overwriting an existing file (milestone 10 spec, §5.1).</summary>
/// <param name="InstallationId">The installation's identifier.</param>
public sealed record CreateDefaultIniCommand(Guid InstallationId) : ICommand<ErrorOr<IniRecoveryResult>>;
