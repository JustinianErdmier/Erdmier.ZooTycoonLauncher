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

        // Registers Serilog's ILogger singleton AND the Microsoft.Extensions.Logging bridge (ILoggerFactory + open-generic ILogger<T>),
        // so handlers that take ILogger<TCategory> (e.g. LaunchGameHandler) and infrastructure types that take Serilog.ILogger
        // (e.g. NullIniSnapshotService) both resolve from the same underlying file sink.
        services.AddSerilog((provider, loggerConfiguration) =>
        {
            IAppStorageLocations locations = provider.GetRequiredService<IAppStorageLocations>();

            loggerConfiguration.ApplyDefaults(locations);
        });

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
