using Avalonia;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Styles;

public sealed class SpaceExtensionTests
{
    private static readonly Dictionary<string, double> Tokens = new()
    {
        [ "GapLabel" ] = 5,
        [ "GapRelated" ] = 6,
        [ "GroupBoxInset" ] = 14,
        [ "GroupBoxFirstControl" ] = 18
    };

    private static SpaceExtension Create(string? uniform = null)
        => new(uniform) { Resolver = name => Tokens.TryGetValue(name, out double value) ? value : null };

    private static Thickness Provide(SpaceExtension extension)
        => (Thickness)extension.ProvideValue(serviceProvider: null!);

    [ Fact ]
    public void Uniform_ResolvesTheToken()
    {
        Provide(Create(uniform: "GapRelated")).ShouldBe(new Thickness(uniformLength: 6));
    }

    [ Fact ]
    public void PerSide_BuildsTheThickness()
    {
        SpaceExtension extension = Create();

        extension.Left = "GroupBoxInset";
        extension.Top = "GroupBoxFirstControl";
        extension.Right = "GapLabel";

        Provide(extension).ShouldBe(new Thickness(left: 14, top: 18, right: 5, bottom: 0));
    }

    [ Fact ]
    public void HorizontalVertical_Pair()
    {
        SpaceExtension extension = Create();

        extension.Horizontal = "GroupBoxInset";
        extension.Vertical = "GapRelated";

        Provide(extension).ShouldBe(new Thickness(horizontal: 14, vertical: 6));
    }

    [ Fact ]
    public void UnknownToken_Throws()
    {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => Provide(Create(uniform: "GapNonsense")));

        exception.Message.ShouldContain("GapNonsense");
    }
}
