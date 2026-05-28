namespace Erdmier.ZooTycoonLauncher.Application.Installations.Relocate;

/// <summary>Outcome of <see cref="RelocateInstallationCommand" />.</summary>
/// <param name="NewValidity">The validity computed after the move.</param>
public sealed record RelocateInstallationResult(InstallationValidity NewValidity);
