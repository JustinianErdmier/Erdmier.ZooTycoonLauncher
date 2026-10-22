using Avalonia;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Styles;

public sealed class SpaceExtensionTests
{
    private static readonly Dictionary<string, object> Tokens = new()
    {
        [ "GapLabel" ] = 5.0,
        [ "GapRelated" ] = 6.0,
        [ "GroupBoxInset" ] = 14.0,
        [ "GroupBoxFirstControl" ] = 18.0,
        [ "AccentRed" ] = "#C00000"
    };

    private static SpaceExtension Create(string? uniform = null)
        => new(uniform) { Resolver = name => Tokens.TryGetValue(name, out object? value) ? value : null };

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
    public void Side_OverridesUniform()
    {
        SpaceExtension extension = Create(uniform: "GapRelated");

        extension.Top = "GroupBoxFirstControl";

        Provide(extension).ShouldBe(new Thickness(left: 6, top: 18, right: 6, bottom: 6));
    }

    [ Fact ]
    public void Left_OverridesHorizontal()
    {
        SpaceExtension extension = Create();

        extension.Horizontal = "GapRelated";
        extension.Left = "GroupBoxInset";

        Provide(extension).ShouldBe(new Thickness(left: 14, top: 0, right: 6, bottom: 0));
    }

    [ Fact ]
    public void NonSpacingResource_ThrowsSayingSo()
    {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => Provide(Create(uniform: "AccentRed")));

        exception.Message.ShouldContain("AccentRed");
        exception.Message.ShouldContain("not a spacing value");
    }

    [ Fact ]
    public void UnknownToken_Throws()
    {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => Provide(Create(uniform: "GapNonsense")));

        exception.Message.ShouldContain("GapNonsense");
    }
}
