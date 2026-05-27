namespace Erdmier.ZooTycoonLauncher.Infrastructure.Common.Extensions;

/// <summary>
/// Composition-root extensions that register every Infrastructure service into a service collection.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers Infrastructure services — file system, storage locations, Serilog, EF Core, repositories.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IAppStorageLocations, AppStorageLocations>();

        services.AddSingleton<ILogger>(provider =>
        {
            IAppStorageLocations locations = provider.GetRequiredService<IAppStorageLocations>();
            return SerilogConfiguration.Build(locations).CreateLogger();
        });

        services.AddDbContext<LauncherDbContext>((provider, options) =>
        {
            IAppStorageLocations locations = provider.GetRequiredService<IAppStorageLocations>();
            options.UseSqlite($"Data Source={locations.LauncherDatabasePath}");
        });

        services.AddScoped<ILauncherSettingsRepository, LauncherSettingsRepository>();
        services.AddScoped<IInstallationRepository, InstallationRepository>();

        return services;
    }

    /// <summary>
    /// Runs EF Core migrations against <c>Launcher.db</c>. Call once at application start.
    /// </summary>
    /// <param name="serviceProvider">The composed service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task MigrateLauncherDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        LauncherDbContext context = scope.ServiceProvider.GetRequiredService<LauncherDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
