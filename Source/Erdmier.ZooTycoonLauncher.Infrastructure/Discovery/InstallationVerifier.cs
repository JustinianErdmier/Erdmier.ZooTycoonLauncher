namespace Erdmier.ZooTycoonLauncher.Infrastructure.Discovery;

/// <summary>
/// File-system-backed implementation of <see cref="IInstallationVerifier" />. Probes the supplied directory for
/// <c>zoo.exe</c> and <c>zoo.ini</c>.
/// </summary>
public sealed class InstallationVerifier : IInstallationVerifier
{
    private const string ExeFileName = "zoo.exe";
    private const string IniFileName = "zoo.ini";

    private readonly IFileSystem _fileSystem;

    /// <summary>Initialises a new instance using the supplied file-system abstraction.</summary>
    /// <param name="fileSystem">The file-system abstraction.</param>
    public InstallationVerifier(IFileSystem fileSystem) => _fileSystem = fileSystem;

    /// <inheritdoc />
    public Task<VerificationResult> VerifyAsync(string path, CancellationToken cancellationToken)
    {
        if (!_fileSystem.Directory.Exists(path))
        {
            return Task.FromResult(new VerificationResult(DirectoryExists: false, HasExe: false, HasIni: false));
        }

        bool hasExe = _fileSystem.File.Exists(_fileSystem.Path.Combine(path, ExeFileName));
        bool hasIni = _fileSystem.File.Exists(_fileSystem.Path.Combine(path, IniFileName));

        return Task.FromResult(new VerificationResult(DirectoryExists: true, HasExe: hasExe, HasIni: hasIni));
    }
}
