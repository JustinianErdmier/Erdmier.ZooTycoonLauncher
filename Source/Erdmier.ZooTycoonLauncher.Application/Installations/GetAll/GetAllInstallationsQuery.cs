namespace Erdmier.ZooTycoonLauncher.Application.Installations.GetAll;

/// <summary>Returns every registered installation as <see cref="InstallationSummary" /> projections with default-installation flag resolved.</summary>
public sealed record GetAllInstallationsQuery : IQuery<ErrorOr<IReadOnlyList<InstallationSummary>>>;
