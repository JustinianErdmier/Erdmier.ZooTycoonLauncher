namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>
/// Composition-root helper that wires all layers into a <see cref="ServiceProvider" /> and
/// runs any startup side-effects (e.g. EF Core migrations) that must complete before the
/// application presents its first window.
/// </summary>
internal static class AppStartup
{
    /// <summary>
    /// Builds the fully-composed <see cref="ServiceProvider" /> and runs startup tasks.
    /// </summary>
    /// <returns>The ready-to-use <see cref="ServiceProvider" />.</returns>
    internal static ServiceProvider BuildAndInitialise()
    {
        ServiceCollection services = new();

        services.AddInfrastructure();
        services.AddDesktop();

        ServiceProvider provider = services.BuildServiceProvider();

        // Run migrations synchronously — the application cannot usefully start without the DB.
        provider.MigrateLauncherDatabaseAsync(CancellationToken.None).GetAwaiter().GetResult();

        return provider;
    }
}
