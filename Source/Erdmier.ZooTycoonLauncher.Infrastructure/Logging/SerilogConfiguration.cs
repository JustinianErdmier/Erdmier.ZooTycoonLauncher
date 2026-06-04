namespace Erdmier.ZooTycoonLauncher.Infrastructure.Logging;

/// <summary>Centralised Serilog setup — file-only sink rolling daily under <see cref="IAppStorageLocations.LauncherLogPath" />.</summary>
public static class SerilogConfiguration
{
    /// <summary>Applies the launcher's standard Serilog configuration (information-level minimum, log-context enrichment, daily-rolling file sink) to the supplied builder.</summary>
    /// <param name="loggerConfiguration">The builder to mutate; typically the one supplied by <c>services.AddSerilog((sp, config) =&gt; …)</c>.</param>
    /// <param name="locations">Resolved path locations.</param>
    /// <returns>The same <paramref name="loggerConfiguration" /> instance, for chaining.</returns>
    public static LoggerConfiguration ApplyDefaults(this LoggerConfiguration loggerConfiguration, IAppStorageLocations locations)
        => loggerConfiguration
           .MinimumLevel.Information()
           .Enrich.FromLogContext()
           .WriteTo.File(locations.LauncherLogPath,
                         rollingInterval: RollingInterval.Day,
                         retainedFileCountLimit: 14,
                         shared: true,
                         outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
}
