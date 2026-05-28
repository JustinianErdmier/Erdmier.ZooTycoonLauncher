namespace Erdmier.ZooTycoonLauncher.Application.Installations.Add;

/// <summary>Outcome of a successful <see cref="AddInstallationCommand" />.</summary>
/// <param name="InstallationId">The newly created installation's identifier.</param>
/// <param name="Validity">The validity computed from the verifier's result.</param>
/// <param name="BecameDefault">
///     <see langword="true" /> when the new installation was promoted to default (either explicitly via <see cref="AddInstallationCommand.MakeDefault" /> or
///     implicitly because it is the first registered installation).
/// </param>
public sealed record AddInstallationResult(Guid InstallationId, InstallationValidity Validity, bool BecameDefault);
