namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Probes an installation directory for <c>zoo.exe</c> and <c>zoo.ini</c> and reports the resulting flags.</summary>
public interface IInstallationVerifier
{
    /// <summary>Probes <paramref name="path" /> for the launcher's required files.</summary>
    /// <param name="path">The fully qualified installation directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="VerificationResult" /> describing the directory and the two flag values.</returns>
    Task<VerificationResult> VerifyAsync(string path, CancellationToken cancellationToken);
}
