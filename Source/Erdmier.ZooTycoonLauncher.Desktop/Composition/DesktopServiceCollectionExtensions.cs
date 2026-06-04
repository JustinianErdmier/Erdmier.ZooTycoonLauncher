namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>Composition-root extensions that register every Desktop view model into a service collection.</summary>
public static class DesktopServiceCollectionExtensions
{
    /// <summary>Registers Desktop view models.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddDesktop(this IServiceCollection services)
    {
        services.AddSingleton<IApplicationLifecycle, AvaloniaApplicationLifecycle>();
        services.AddSingleton<IDialogService>(sp => new AvaloniaDialogService(sp));
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<AddInstallationDialogViewModel>();

        return services;
    }
}
