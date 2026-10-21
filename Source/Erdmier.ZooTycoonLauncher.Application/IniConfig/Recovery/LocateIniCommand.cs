namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Recovery;

/// <summary>Copies an existing INI file into the installation as <c>zoo.ini</c> without ever overwriting an existing file (milestone 10 spec, §5.2).</summary>
/// <param name="InstallationId">The installation's identifier.</param>
/// <param name="SourcePath">The full path of the file to copy; it is left untouched.</param>
public sealed record LocateIniCommand(Guid InstallationId, string SourcePath) : ICommand<ErrorOr<IniRecoveryResult>>;
