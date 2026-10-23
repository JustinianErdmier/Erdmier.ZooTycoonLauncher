using Avalonia.Controls.ApplicationLifetimes;

using Erdmier.ZooTycoonNexus.Desktop.Views;

namespace Erdmier.ZooTycoonNexus.Desktop;

/// <summary>The Avalonia application class. Delegates DI wiring to <see cref="Composition.AppStartup" />.</summary>
public sealed class App : Avalonia.Application
{
    /// <summary>The composed service provider; <see langword="null" /> until framework initialisation completes.</summary>
    public static IServiceProvider? Services { get; private set; }

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        ServiceProvider provider = AppStartup.BuildAndInitialise();

        Services = provider;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindowViewModel viewModel = provider.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
