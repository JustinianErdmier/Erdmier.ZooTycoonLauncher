namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Boot;

/// <summary>Transient view shown while <c>BootCommand</c> is in flight.</summary>
public sealed partial class LookingForZooTycoonView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public LookingForZooTycoonView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is LookingForZooTycoonViewModel vm)
        {
            vm.StartCycling();
        }
    }

    /// <inheritdoc />
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (DataContext is LookingForZooTycoonViewModel vm)
        {
            vm.StopCycling();
        }
    }
}
