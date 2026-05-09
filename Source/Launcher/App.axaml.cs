using System;
using System.IO.Abstractions;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Erdmier.ZooTycoonLauncher.Launcher.Services;
using Erdmier.ZooTycoonLauncher.Launcher.Views;

using Microsoft.Extensions.DependencyInjection;

namespace Erdmier.ZooTycoonLauncher.Launcher;

/// <summary>Represents the entry point of the application. Sets up the DI container, resolves <see cref="MainWindowViewModel" />, and assigns it as the main window's data context.</summary>
public class App : Application
{
    /// <summary>The application-wide service provider. Exposed as a static property so that <see cref="Views.MainWindow" /> can resolve the folder-picker shim after it loads.</summary>
    public static IServiceProvider? Services { get; private set; }

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        Services = BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new();

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IRegistryReader, WindowsRegistryReader>();

        services.AddSingleton<ILauncherConfigService>(sp => new LauncherConfigService(sp.GetRequiredService<IFileSystem>(),
                                                                                      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));

        services.AddSingleton<IFileLocatorService, FileLocatorService>();
        services.AddSingleton<IIniParserService, IniParserService>();
        services.AddSingleton<IVersioningService, VersioningService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<IFolderPicker, AvaloniaFolderPicker>();
        services.AddSingleton<IShellService, WindowsShellService>();
        services.AddSingleton<ILauncherService, LauncherService>();

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<IniSettingsViewModel>();

        return services.BuildServiceProvider();
    }
}
