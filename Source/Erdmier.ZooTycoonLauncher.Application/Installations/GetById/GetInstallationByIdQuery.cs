namespace Erdmier.ZooTycoonLauncher.Application.Installations.GetById;

/// <summary>Returns a single installation as an <see cref="InstallationSummary" /> projection.</summary>
/// <param name="InstallationId">The installation's identifier.</param>
public sealed record GetInstallationByIdQuery(Guid InstallationId) : IQuery<ErrorOr<InstallationSummary>>;
