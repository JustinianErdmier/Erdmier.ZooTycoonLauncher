using System;

using Avalonia;

using Classic.CommonControls;

using JetBrains.Annotations;

namespace Erdmier.ZooTycoonLauncher.Launcher;

/// <summary>The entry point for the Zoo Tycoon Launcher application. This class is responsible for initialising the Avalonia framework and starting the application lifecycle.</summary>
/// <remarks>
///     This class contains the main method and a method for configuring the Avalonia application. It should not include calls to Avalonia, third-party APIs, or any
///     SynchronizationContext-dependent code before the application primary methods are invoked.
/// </remarks>
[ UsedImplicitly ]
internal class Program
{
    /// <summary>
    ///     Initialisation Code: Don't use any Avalonia, third-party APIs, or any SynchronizationContext-reliant code before AppMain is called. Things aren't initialised yet, and
    ///     stuff might break.
    /// </summary>
    /// <param name="args"> Command-line arguments passed to the application. </param>
    [ STAThread ]
    public static void Main(string[] args)
        => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>Avalonia configuration, don't remove; also used by the visual designer.</summary>
    /// <returns> <see cref="Avalonia.AppBuilder" /> instance configured for Avalonia application. </returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
                     .UsePlatformDetect()
                     .UseMessageBoxSounds()
#if DEBUG
                     .WithDeveloperTools()
#endif
                     .WithInterFont()
                     .LogToTrace();
}
