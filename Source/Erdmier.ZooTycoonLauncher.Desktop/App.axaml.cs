namespace Erdmier.ZooTycoonLauncher.Desktop;

/// <summary>
/// The Avalonia application class. Delegates DI wiring to <see cref="Composition.AppStartup" />.
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
        ServiceProvider provider = Composition.AppStartup.BuildAndInitialise();
        Services = provider;

        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            ViewModels.MainWindowViewModel viewModel = provider.GetRequiredService<ViewModels.MainWindowViewModel>();
            desktop.MainWindow = new Views.MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
