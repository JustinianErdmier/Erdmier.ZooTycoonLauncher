namespace Erdmier.ZooTycoonLauncher.Infrastructure.IniSnapshots;

/// <summary>
///     Placeholder <see cref="IIniSnapshotService" /> shipped during the Installation Lifecycle slice. The INI Config slice replaces it with a real implementation that parses
///     <c>zoo.ini</c> and writes the <c>Original</c> + <c>Current</c> snapshots.
/// </summary>
public sealed class NullIniSnapshotService : IIniSnapshotService
{
    private readonly ILogger _logger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="logger">The Serilog logger.</param>
    public NullIniSnapshotService(ILogger logger) => _logger = logger;

    /// <inheritdoc />
    public Task<ErrorOr<Success>> CaptureOriginalAsync(GameInstallation installation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (installation.HasIni)
        {
            _logger.Warning(messageTemplate: "INI snapshot capture for {InstallationId} ({Name}) is deferred — replace NullIniSnapshotService when the INI Config slice lands.",
                            installation.Id,
                            installation.Name);
        }

        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }
}
