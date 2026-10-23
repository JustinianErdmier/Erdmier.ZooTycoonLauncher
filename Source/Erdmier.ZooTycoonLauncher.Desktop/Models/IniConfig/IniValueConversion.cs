namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>
///     A pure, two-way conversion between the raw value a key holds in <c>zoo.ini</c> and the value a Friendly-layout control shows. View models keep the raw value as the
///     source of truth; only the control sees the converted one (spec C5).
/// </summary>
public sealed class IniValueConversion : SmartEnum<IniValueConversion>
{
    /// <summary>The control shows the raw value.</summary>
    public static readonly IniValueConversion None = new(name: "None", id: 0, toDisplay: raw => raw, toRaw: display => display);

    /// <summary>A <c>0</c> / <c>1</c> flag shown the other way round (<c>noMenuMusic</c> as <i>Enable menu music</i>).</summary>
    public static readonly IniValueConversion Invert = new(name: "Invert", id: 1, toDisplay: raw => 1 - raw, toRaw: display => 1 - display);

    /// <summary>An attenuation (<c>0</c>–<c>10000</c>, higher is quieter) shown as a volume: <c>volume = 10000 − attenuation</c>.</summary>
    public static readonly IniValueConversion VolumeFromAttenuation = new(name: "VolumeFromAttenuation", id: 2, toDisplay: raw => 10000 - raw, toRaw: display => 10000 - display);

    /// <summary>A movie volume (<c>-10000</c> silent to <c>0</c> full) shown as a volume: <c>volume = raw + 10000</c>.</summary>
    public static readonly IniValueConversion VolumeFromMovieVolume = new(name: "VolumeFromMovieVolume", id: 3, toDisplay: raw => raw + 10000, toRaw: display => display - 10000);

    private readonly Func<int, int> _toDisplay;
    private readonly Func<int, int> _toRaw;

    private IniValueConversion(string name, int id, Func<int, int> toDisplay, Func<int, int> toRaw)
        : base(name, id)
    {
        _toDisplay = toDisplay;
        _toRaw     = toRaw;
    }

    /// <summary>Converts a raw file value to the value the control shows.</summary>
    /// <param name="raw">The raw value.</param>
    /// <returns>The displayed value.</returns>
    public int ToDisplay(int raw) => _toDisplay(raw);

    /// <summary>Converts a value the control shows back to the raw file value.</summary>
    /// <param name="display">The displayed value.</param>
    /// <returns>The raw value.</returns>
    public int ToRaw(int display) => _toRaw(display);
}
