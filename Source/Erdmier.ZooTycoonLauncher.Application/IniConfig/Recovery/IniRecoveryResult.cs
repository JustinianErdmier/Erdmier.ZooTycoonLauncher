namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Recovery;

/// <summary>The outcome of creating or locating a missing <c>zoo.ini</c>.</summary>
/// <param name="Validity">The installation's validity after the file was written.</param>
public sealed record IniRecoveryResult(InstallationValidity Validity);
