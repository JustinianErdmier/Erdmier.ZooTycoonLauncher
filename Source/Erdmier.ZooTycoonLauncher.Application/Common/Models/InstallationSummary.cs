namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>
/// Read-only projection of a <see cref="GameInstallation" /> row enriched with derived state — used by the Installation Manager
/// grid and the picker.
/// </summary>
/// <param name="Id">The installation's identifier.</param>
/// <param name="Name">The user-visible name.</param>
/// <param name="Path">The fully qualified directory path.</param>
/// <param name="Validity">The validity computed from <see cref="GameInstallation.HasExe" /> and <see cref="GameInstallation.HasIni" />.</param>
/// <param name="IsDefault"><see langword="true" /> when this row's <see cref="Id" /> equals <c>LauncherSettings.DefaultInstallationId</c>.</param>
/// <param name="AddedUtc">UTC timestamp the row was created.</param>
/// <param name="ModifiedUtc">UTC timestamp of the most recent mutable-field change, or <see langword="null" /> when the row has never been modified.</param>
/// <param name="LastPlayedUtc">UTC timestamp of the most recent successful <c>zoo.exe</c> launch, or <see langword="null" />.</param>
/// <param name="LastOpenedUtc">UTC timestamp the installation last became the active installation, or <see langword="null" />.</param>
public sealed record InstallationSummary(
    Guid Id,
    string Name,
    string Path,
    InstallationValidity Validity,
    bool IsDefault,
    DateTime AddedUtc,
    DateTime? ModifiedUtc,
    DateTime? LastPlayedUtc,
    DateTime? LastOpenedUtc);
