using Microsoft.Extensions.Logging;

using Serilog.Extensions.Logging;

using ILogger = Serilog.ILogger;

// Local usings — the two MEL namespaces above collide with Serilog's unqualified ILogger in other Infrastructure files
// (e.g. NullIniSnapshotService), so they must NOT be global. CLAUDE.md "local using only for namespace conflicts" applies.

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Common.Extensions;

/// <summary>Composition-root extensions that register every Infrastructure service into a service collection.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registers Infrastructure services — file system, storage locations, Serilog, EF Core, repositories, locator/verifier/registry, INI snapshot placeholder.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IAppStorageLocations, AppStorageLocations>();

        // Build the underlying Serilog logger once and share it between Serilog.ILogger consumers (e.g. NullIniSnapshotService)
        // and the Microsoft.Extensions.Logging bridge (e.g. LaunchGameHandler taking ILogger<TCategory>).
        services.AddSingleton<ILogger>(provider =>
        {
            IAppStorageLocations locations = provider.GetRequiredService<IAppStorageLocations>();

            return new LoggerConfiguration().ApplyDefaults(locations)
                                            .CreateLogger();
        });

        // services.AddSerilog((sp, config) => …) from Serilog.Extensions.Hosting 9.0.0 registers Serilog.ILogger but does NOT
        // wire up MEL — verified by AddInfrastructureLoggingTests. The explicit pattern below registers ILoggerFactory and
        // open-generic ILogger<T> via AddLogging, then plugs a SerilogLoggerProvider into the factory so MEL log calls flow
        // to the shared Serilog logger above.
        services.AddLogging(builder => builder.ClearProviders());

        services.AddSingleton<ILoggerProvider>(provider
                                                   => new SerilogLoggerProvider(provider.GetRequiredService<ILogger>(), dispose: false));

        services.AddDbContext<LauncherDbContext>((provider, options) =>
        {
            IAppStorageLocations locations = provider.GetRequiredService<IAppStorageLocations>();

            options.UseSqlite($"Data Source={locations.LauncherDatabasePath}");
        });

        services.AddScoped<ILauncherSettingsRepository, LauncherSettingsRepository>();
        services.AddScoped<IInstallationRepository, InstallationRepository>();

        services.AddSingleton<IRegistryReader, WindowsRegistryReader>();
        services.AddSingleton<IInstallationVerifier, InstallationVerifier>();
        services.AddSingleton<IInstallationLocator, InstallationLocator>();
        services.AddSingleton<IProcessLauncher, WindowsProcessLauncher>();

        services.AddSingleton<IInstallationDbContextFactory, InstallationDbContextFactory>();
        services.AddScoped<IIniSnapshotService, NullIniSnapshotService>();

        return services;
    }

    /// <summary>Runs EF Core migrations against <c>Launcher.db</c>. Call once the application starts.</summary>
    /// <param name="serviceProvider">The composed service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task MigrateLauncherDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        LauncherDbContext context = scope.ServiceProvider.GetRequiredService<LauncherDbContext>();

        await context.Database.MigrateAsync(cancellationToken);
    }
}
