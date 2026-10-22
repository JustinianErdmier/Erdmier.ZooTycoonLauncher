using Avalonia;
using Avalonia.Headless;
using Avalonia.Logging;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Headless;

/// <summary>Builds the headless Avalonia application that every <c>[ AvaloniaFact ]</c> and <c>[ AvaloniaTheory ]</c> in this assembly runs under.</summary>
public static class HeadlessTestAppBuilder
{
    /// <summary>The sink that receives Avalonia's log output for the whole test session.</summary>
    public static RecordingLogSink LogSink { get; } = new();

    /// <summary>Configures <see cref="HeadlessTestApp" /> on the headless platform with stub drawing, so no window, GPU or font rasteriser is needed.</summary>
    /// <returns>The configured application builder; the headless test session starts it.</returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        Logger.Sink = LogSink;

        return AppBuilder.Configure<HeadlessTestApp>()
                     .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
    }
}
