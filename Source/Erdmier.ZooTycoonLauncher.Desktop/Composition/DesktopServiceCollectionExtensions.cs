namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>Composition-root extensions that register Desktop chrome services and view models into a service collection.</summary>
public static class DesktopServiceCollectionExtensions
{
    /// <summary>Registers Desktop chrome services (application lifecycle, dialogue service, messenger) and view models.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddDesktop(this IServiceCollection services)
    {
        services.AddSingleton<IApplicationLifecycle, AvaloniaApplicationLifecycle>();
        services.AddSingleton<IDialogService>(sp => new AvaloniaDialogService(sp));
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddTransient<InstallationGridViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<AddInstallationDialogViewModel>();
        services.AddTransient<InstallationManagerDialogViewModel>();

        return services;
    }
}
