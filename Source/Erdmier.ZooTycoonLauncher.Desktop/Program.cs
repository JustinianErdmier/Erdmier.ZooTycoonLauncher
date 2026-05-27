namespace Erdmier.ZooTycoonLauncher.Desktop;

/// <summary>
/// Application entry point. Standard Avalonia bootstrap.
/// </summary>
public static class Program
{
    /// <summary>The Windows STA entry point.</summary>
    [ STAThread ]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>Builds the Avalonia <see cref="AppBuilder" />.</summary>
    /// <returns>The configured app builder.</returns>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
                  .UsePlatformDetect()
                  .WithInterFont()
                  .LogToTrace();
}
