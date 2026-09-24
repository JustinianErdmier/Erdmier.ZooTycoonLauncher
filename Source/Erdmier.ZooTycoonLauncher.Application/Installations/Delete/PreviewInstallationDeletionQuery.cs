namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>
///     Describes what deleting an installation would do, without deleting it — used by the Delete Installation confirmation (SDD §7.2.4, §9.5) to name the
///     installation that would be promoted to default.
/// </summary>
/// <param name="InstallationId">The installation the user is about to delete.</param>
public sealed record PreviewInstallationDeletionQuery(Guid InstallationId) : IQuery<ErrorOr<InstallationDeletionPreview>>;
