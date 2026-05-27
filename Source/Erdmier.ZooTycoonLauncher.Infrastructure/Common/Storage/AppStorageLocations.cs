namespace Erdmier.ZooTycoonLauncher.Infrastructure.Common.Storage;

/// <summary>Resolves on-disk paths under <c>%LOCALAPPDATA%\ZooTycoonLauncher\</c> (SDD §4.7).</summary>
public sealed class AppStorageLocations : IAppStorageLocations
{
    private const string AppFolderName = "ZooTycoonLauncher";

    private readonly IFileSystem _fileSystem;

    /// <summary>Initialises a new instance using the supplied file system abstraction so paths can be redirected in tests.</summary>
    /// <param name="fileSystem">The file system abstraction used to ensure directories exist.</param>
    public AppStorageLocations(IFileSystem fileSystem)
    {
        _fileSystem          = fileSystem;
        AppDataRoot          = _fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);
        DataRoot             = _fileSystem.Path.Combine(AppDataRoot, path2: "Data");
        LogsRoot             = _fileSystem.Path.Combine(AppDataRoot, path2: "Logs");
        LauncherDatabasePath = _fileSystem.Path.Combine(DataRoot, path2: "Launcher.db");
        LauncherLogPath      = _fileSystem.Path.Combine(LogsRoot, path2: "Launcher.log");

        _fileSystem.Directory.CreateDirectory(DataRoot);
        _fileSystem.Directory.CreateDirectory(LogsRoot);
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.Combine(LogsRoot, path2: "Installations"));
    }

    /// <inheritdoc />
    public string AppDataRoot { get; }

    /// <inheritdoc />
    public string DataRoot { get; }

    /// <inheritdoc />
    public string LogsRoot { get; }

    /// <inheritdoc />
    public string LauncherDatabasePath { get; }

    /// <inheritdoc />
    public string LauncherLogPath { get; }

    /// <inheritdoc />
    public string InstallationDatabasePath(Guid installationId) => _fileSystem.Path.Combine(DataRoot, $"{installationId}.db");

    /// <inheritdoc />
    public string InstallationLogPath(Guid installationId) => _fileSystem.Path.Combine(LogsRoot, path2: "Installations", $"{installationId}.log");
}
