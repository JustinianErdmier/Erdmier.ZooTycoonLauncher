namespace Erdmier.ZooTycoonNexus.Application.IniConfig.Get;

/// <summary>Reconciles an installation's <c>Current</c> snapshot with the file on disk and returns its recognised values for the Game Config editor.</summary>
/// <param name="InstallationId">The installation's identifier.</param>
public sealed record GetIniConfigQuery(Guid InstallationId) : IQuery<ErrorOr<IniConfigResult>>;
