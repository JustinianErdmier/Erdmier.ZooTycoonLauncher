namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.History;

/// <summary>Reconciles an installation's <c>Current</c> snapshot with the file on disk, then lists every stored snapshot with its differences against <c>Current</c>.</summary>
/// <param name="InstallationId">The installation's identifier.</param>
public sealed record GetIniHistoryQuery(Guid InstallationId) : IQuery<ErrorOr<IniHistoryResult>>;
