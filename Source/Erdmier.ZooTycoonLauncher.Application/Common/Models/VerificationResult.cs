namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>
/// Outcome of <see cref="IInstallationVerifier.VerifyAsync" /> — the <c>HasExe</c> / <c>HasIni</c> flags computed against the supplied path.
/// </summary>
/// <param name="DirectoryExists"><see langword="true" /> when the supplied directory exists on disk.</param>
/// <param name="HasExe"><see langword="true" /> when <c>zoo.exe</c> is present in the directory.</param>
/// <param name="HasIni"><see langword="true" /> when <c>zoo.ini</c> is present in the directory.</param>
public sealed record VerificationResult(bool DirectoryExists, bool HasExe, bool HasIni)
{
    /// <summary>The <see cref="InstallationValidity" /> implied by the flags; falls back to <see cref="InstallationValidity.InvalidNoExeOrIni" /> when the directory is missing.</summary>
    public InstallationValidity Validity => DirectoryExists ? InstallationValidity.From(HasExe, HasIni) : InstallationValidity.InvalidNoExeOrIni;
}
