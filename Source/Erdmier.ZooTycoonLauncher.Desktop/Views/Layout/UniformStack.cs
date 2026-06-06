namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Layout;

/// <summary>Represents a panel layout that arranges its children uniformly in a single horizontal direction, optionally with customisable spacing between the child elements.</summary>
/// <remarks>
///     This file was copied from the `Classic.Avalonia\Samples\AvaloniaExplorer` sample project. It has been refactored to improve readability and maintainability, but the
///     original implementation logic is the same and may need to be optimised for performance. The current implementation uses a simple layout algorithm that may not be suitable for
///     very large numbers of children or complex child layouts. Nor does it support vertical stacking or other advanced layout features.
/// </remarks>
public class UniformStack : Panel
{
    private static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<UniformStack, double>(nameof(Spacing));

    static UniformStack()
    {
        AffectsArrange<UniformStack>(SpacingProperty);
        AffectsMeasure<UniformStack>(SpacingProperty);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);

        set => SetValue(SpacingProperty, value);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int visibleChildren = Children.Count(x => x.IsVisible);

        if (visibleChildren == 0)
        {
            // ReSharper disable once ArrangeDefaultValueWhenTypeNotEvident
            return default;
        }

        // TODO: Investigate potential performance improvements for layout calculations
        // TODO: Investigate why `y` is not updated and determine if it has been overlooked.
        double       x = 0.0;
        const double y = 0.0;

        double width  = (finalSize.Width - Math.Max(val1: 0, Spacing * (visibleChildren - 1))) / visibleChildren;
        double height = finalSize.Height;

        foreach (Control child in Children.Where(child => child.IsVisible))
        {
            child.Arrange(new Rect(x, y, width, height));

            x += width;
            x += Spacing;
        }

        return finalSize;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        int visibleChildren = Children.Count(x => x.IsVisible);

        if (visibleChildren == 0)
        {
            // ReSharper disable once ArrangeDefaultValueWhenTypeNotEvident
            return default;
        }

        double maxWidth  = 0d;
        double maxHeight = 0d;

        Size childAvailableSize = new(availableSize.Width / visibleChildren, availableSize.Height);

        foreach (Control? child in Children)
        {
            child.Measure(childAvailableSize);

            if (child.DesiredSize.Width > maxWidth)
            {
                maxWidth = child.DesiredSize.Width;
            }

            if (child.DesiredSize.Height > maxHeight)
            {
                maxHeight = child.DesiredSize.Height;
            }
        }

        return new Size(maxWidth * visibleChildren + Math.Max(val1: 0, Spacing * (visibleChildren - 1)), maxHeight);
    }
}
