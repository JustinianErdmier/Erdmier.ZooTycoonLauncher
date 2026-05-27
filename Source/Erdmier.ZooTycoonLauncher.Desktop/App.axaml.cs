namespace Erdmier.ZooTycoonLauncher.Desktop;

/// <summary>
/// The Avalonia application class. Wires DI in <see cref="OnFrameworkInitializationCompleted" />.
/// </summary>
public sealed partial class App : Avalonia.Application
{
    /// <summary>The composed service provider; <see langword="null" /> until framework initialisation completes.</summary>
    public static IServiceProvider? Services { get; private set; }

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        ServiceCollection services = new();
        services.AddInfrastructure();
        services.AddDesktop();

        ServiceProvider provider = services.BuildServiceProvider();
        Services = provider;

        // Run migrations synchronously here — the application can't usefully start without the DB.
        provider.MigrateLauncherDatabaseAsync(CancellationToken.None).GetAwaiter().GetResult();

        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            ViewModels.MainWindowViewModel viewModel = provider.GetRequiredService<ViewModels.MainWindowViewModel>();
            desktop.MainWindow = new Views.MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
