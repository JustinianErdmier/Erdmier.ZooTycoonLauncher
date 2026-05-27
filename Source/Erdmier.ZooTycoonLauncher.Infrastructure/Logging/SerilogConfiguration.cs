namespace Erdmier.ZooTycoonLauncher.Infrastructure.Logging;

/// <summary>Centralised Serilog setup — file-only sink rolling daily under <see cref="IAppStorageLocations.LauncherLogPath" />.</summary>
public static class SerilogConfiguration
{
    /// <summary>Builds a Serilog <see cref="LoggerConfiguration" /> targeting the launcher's rolling log file.</summary>
    /// <param name="locations">Resolved path locations.</param>
    /// <returns>A configured <see cref="LoggerConfiguration" />; call <c>.CreateLogger()</c> on the result.</returns>
    public static LoggerConfiguration Build(IAppStorageLocations locations)
        => new LoggerConfiguration()
           .MinimumLevel.Information()
           .Enrich.FromLogContext()
           .WriteTo.File(locations.LauncherLogPath,
                         rollingInterval: RollingInterval.Day,
                         retainedFileCountLimit: 14,
                         shared: true,
                         outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
}
