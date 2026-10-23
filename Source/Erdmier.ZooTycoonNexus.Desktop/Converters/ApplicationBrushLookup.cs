using Avalonia.Media;

namespace Erdmier.ZooTycoonNexus.Desktop.Converters;

/// <summary>Resolves the launcher's named brushes (defined once in <c>Styles/Colours.axaml</c>) from the running application's resources.</summary>
internal static class ApplicationBrushLookup
{
    private const string AccentGreenKey = "AccentGreen";

    private const string AccentRedKey = "AccentRed";

    // Used when there is no running application (unit tests); the values mirror Colours.axaml.
    private static readonly IBrush FallbackGreen = new SolidColorBrush(Color.Parse(s: "#008000"));

    private static readonly IBrush FallbackRed = new SolidColorBrush(Color.Parse(s: "#C00000"));

    /// <summary>The green accent brush: <c>AccentGreen</c> from the application resources, or a same-valued fallback when none is available.</summary>
    public static IBrush AccentGreen => Resolve(AccentGreenKey, FallbackGreen);

    /// <summary>The red accent brush: <c>AccentRed</c> from the application resources, or a same-valued fallback when none is available.</summary>
    public static IBrush AccentRed => Resolve(AccentRedKey, FallbackRed);

    private static IBrush Resolve(string key, IBrush fallback)
        => Avalonia.Application.Current is { } application && application.TryGetResource(key, theme: null, out object? value) && value is IBrush brush ? brush : fallback;
}
