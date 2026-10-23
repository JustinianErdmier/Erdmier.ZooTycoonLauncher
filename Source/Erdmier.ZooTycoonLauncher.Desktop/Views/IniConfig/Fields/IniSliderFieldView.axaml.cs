namespace Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig.Fields;

/// <summary>View for <see cref="IniSliderFieldViewModel" />: a trackbar with a percentage readout. A pointer drag that ends gives focus back; a Tab-focused slider keeps it.</summary>
public sealed partial class IniSliderFieldView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public IniSliderFieldView()
    {
        AvaloniaXamlLoader.Load(this);

        // The thumb captures the pointer and handles its release, so listen for handled events too.
        this.FindControl<Slider>("VolumeSlider")!
            .AddHandler(PointerReleasedEvent, OnSliderPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void OnSliderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control slider)
        {
            IniFieldFocusRelease.AfterCommit(slider);
        }
    }
}
