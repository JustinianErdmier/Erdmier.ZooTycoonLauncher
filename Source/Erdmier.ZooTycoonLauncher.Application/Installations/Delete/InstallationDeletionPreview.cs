namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>Outcome of a <see cref="PreviewInstallationDeletionQuery" />.</summary>
/// <param name="Name">The name of the installation that would be deleted.</param>
/// <param name="IsDefault"><see langword="true" /> when that installation is the current launcher default.</param>
/// <param name="PromotedName">
///     When <paramref name="IsDefault" /> is <see langword="true" />, the name of the installation the delete would promote to default, or <see langword="null" />
///     when it is the last installation. Always <see langword="null" /> when <paramref name="IsDefault" /> is <see langword="false" />.
/// </param>
public sealed record InstallationDeletionPreview(string Name, bool IsDefault, string? PromotedName);
