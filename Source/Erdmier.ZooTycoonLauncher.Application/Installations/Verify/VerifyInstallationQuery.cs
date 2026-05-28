namespace Erdmier.ZooTycoonLauncher.Application.Installations.Verify;

/// <summary>
///     Re-runs <see cref="IInstallationVerifier" /> for the supplied installation, persists any change to <c>HasExe</c>, <c>HasIni</c>, <c>ModifiedUtc</c>, and returns the
///     updated <see cref="VerificationResult" />.
/// </summary>
/// <param name="InstallationId">The installation's identifier.</param>
public sealed record VerifyInstallationQuery(Guid InstallationId) : IQuery<ErrorOr<VerificationResult>>;
