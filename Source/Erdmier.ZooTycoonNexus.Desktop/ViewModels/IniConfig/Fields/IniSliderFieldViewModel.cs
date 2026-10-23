namespace Erdmier.ZooTycoonNexus.Desktop.ViewModels.IniConfig.Fields;

/// <summary>
///     A volume slider over an integer key (spec §6.4). The slider shows <see cref="Percent" /> of the converted value (<c>percent = volume ÷ 100</c>) and writes raw values in
///     steps of 100. The raw value stays the source of truth (spec C5), so an off-step file value is kept, and the row stays clean, until the slider moves (spec C16).
/// </summary>
public sealed partial class IniSliderFieldViewModel : IniKeyedFieldViewModel
{
    private readonly IniValueConversion _conversion;

    private bool _isApplying;

    private string? _raw;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The row's presentation.</param>
    public IniSliderFieldViewModel(IniFieldDescriptor descriptor)
        : base(descriptor)
        => _conversion = descriptor.Conversion;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniSliderFieldViewModel()
        : this(new IniFieldDescriptor(new IniKeyId(Section: "UI", Key: "menuMusicAttenuation"), Label: "Menu music volume", IniControlKind.Slider, Hint: "Silent – Full",
                                      Help: "How loud the main-menu music is.")
        {
            Conversion = IniValueConversion.VolumeFromAttenuation
        })
        => Load(raw: null);

    /// <summary>The slider position, 0–100.</summary>
    [ ObservableProperty ]
    public partial int Percent { get; set; }

    /// <summary>The readout beside the slider, for example <c>75 %</c>.</summary>
    public string PercentText => $"{Percent} %";

    /// <inheritdoc />
    protected override string? CurrentRaw => _raw;

    /// <inheritdoc />
    protected override void ApplyValue(string? effectiveValue)
    {
        _raw = effectiveValue;

        _isApplying = true;
        Percent     = int.TryParse(effectiveValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? IniVolumePercent.FromRaw(_conversion, parsed) : 0;
        _isApplying = false;
    }

    partial void OnPercentChanged(int value)
    {
        OnPropertyChanged(nameof(PercentText));

        if (_isApplying)
        {
            return;
        }

        _raw = _conversion.ToRaw(value * IniVolumePercent.UnitsPerPercent)
                          .ToString(CultureInfo.InvariantCulture);

        NotifyValueChanged();
    }
}
