namespace Erdmier.ZooTycoonLauncher.Desktop.Styles;

/// <summary>
///     Builds a <see cref="Thickness" /> from the spacing token names defined in <c>Styles/Spacing.axaml</c>, so views write
///     <c>{tokens:Space GapRelated}</c> or <c>{tokens:Space Left=GroupBoxInset, Top=GroupBoxFirstControl}</c> rather than a literal margin or padding.
/// </summary>
/// <remarks>
///     An unset side is 0. The sides combine in order of precedence: <see cref="Uniform" /> first, then <see cref="Horizontal" /> and <see cref="Vertical" />, then the
///     individual <see cref="Left" />, <see cref="Top" />, <see cref="Right" /> and <see cref="Bottom" />.
/// </remarks>
public sealed class SpaceExtension : MarkupExtension
{
    /// <summary>Creates an extension with no sides set, for use with named properties such as <c>Left=GapLabel</c>.</summary>
    public SpaceExtension()
    {
    }

    /// <summary>Creates an extension whose four sides all take one token.</summary>
    /// <param name="uniform">The token name for every side.</param>
    public SpaceExtension(string? uniform)
    {
        Uniform = uniform;
    }

    /// <summary>The token name for every side, unless a more specific property overrides it.</summary>
    public string? Uniform { get; set; }

    /// <summary>The token name for the left and right sides.</summary>
    public string? Horizontal { get; set; }

    /// <summary>The token name for the top and bottom sides.</summary>
    public string? Vertical { get; set; }

    /// <summary>The token name for the left side.</summary>
    public string? Left { get; set; }

    /// <summary>The token name for the top side.</summary>
    public string? Top { get; set; }

    /// <summary>The token name for the right side.</summary>
    public string? Right { get; set; }

    /// <summary>The token name for the bottom side.</summary>
    public string? Bottom { get; set; }

    /// <summary>Resolves a token name to its value, or <see langword="null" /> when it is unknown. Defaults to the running application's resources; tests replace it.</summary>
    public Func<string, double?>? Resolver { get; set; }

    /// <summary>Builds the thickness.</summary>
    /// <param name="serviceProvider">Unused.</param>
    /// <returns>The <see cref="Thickness" /> whose sides hold the named tokens' values.</returns>
    /// <exception cref="InvalidOperationException">A side names a token that does not exist.</exception>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        double left = Resolve(Left ?? Horizontal ?? Uniform);
        double top = Resolve(Top ?? Vertical ?? Uniform);
        double right = Resolve(Right ?? Horizontal ?? Uniform);
        double bottom = Resolve(Bottom ?? Vertical ?? Uniform);

        return new Thickness(left, top, right, bottom);
    }

    private double Resolve(string? name)
    {
        if (name is null)
        {
            return 0;
        }

        double? value = Resolver is not null ? Resolver(name) : FromApplication(name);

        return value ?? throw new InvalidOperationException($"Unknown spacing token '{name}'. Spacing tokens are defined in Styles/Spacing.axaml.");
    }

    private static double? FromApplication(string name)
        => Avalonia.Application.Current is { } application && application.TryGetResource(name, theme: null, out object? value) && value is double number ? number : null;
}
