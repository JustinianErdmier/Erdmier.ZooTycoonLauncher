namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.History;

/// <summary>Counts an installation's <c>Historical</c> snapshots without reading <c>zoo.ini</c>, reconciling, or creating a database.</summary>
/// <param name="InstallationId">The installation's identifier.</param>
public sealed record GetIniHistoryCountQuery(Guid InstallationId) : IQuery<ErrorOr<int>>;
