namespace Erdmier.ZooTycoonLauncher.Application.Installations.Relocate;

/// <summary>
/// Points an existing installation at a new folder, recomputes <c>HasExe</c> / <c>HasIni</c>, and stamps
/// <c>ModifiedUtc</c>. Implements SDD §7.2.5 for the EXE-relocation case.
/// </summary>
/// <param name="InstallationId">The installation's identifier.</param>
/// <param name="NewPath">The new fully qualified directory path.</param>
public sealed record RelocateInstallationCommand(Guid InstallationId, string NewPath) : ICommand<ErrorOr<RelocateInstallationResult>>;
