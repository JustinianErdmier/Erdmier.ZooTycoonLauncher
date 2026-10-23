namespace Erdmier.ZooTycoonNexus.Desktop.ViewModels.IniConfig.Fields;

/// <summary>A <c>NumericUpDown</c> over an integer key, bounded by the key's spec.</summary>
public sealed partial class IniNumberFieldViewModel : IniKeyedFieldViewModel
{
    private readonly bool _isCurrency;

    private readonly bool _isReadOnly;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The row's presentation.</param>
    public IniNumberFieldViewModel(IniFieldDescriptor descriptor)
        : base(descriptor)
    {
        _isCurrency = descriptor.IsCurrency;
        _isReadOnly = descriptor.IsReadOnly;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniNumberFieldViewModel()
        : this(new IniFieldDescriptor(new IniKeyId(Section: "user", Key: "UpdateRate"), Label: "UpdateRate", IniControlKind.Number, Hint: "1–60 ticks/sec",
                                      Help: "Game-logic ticks per second."))
        => Load(raw: null);

    /// <summary>The lower bound (the spec's <c>Min</c>, or <see cref="int.MinValue" />).</summary>
    public decimal Minimum => Spec.Min ?? int.MinValue;

    /// <summary>The upper bound (the spec's <c>Max</c>, or <see cref="int.MaxValue" />).</summary>
    public decimal Maximum => Spec.Max ?? int.MaxValue;

    /// <summary>The format string the box displays its number with.</summary>
    public string FormatString => _isCurrency ? IniCurrencyFormat.FormatString : "0";

    /// <summary>The number format the box displays and parses with.</summary>
    public NumberFormatInfo NumberFormat => _isCurrency ? IniCurrencyFormat.NumberFormat : CultureInfo.CurrentCulture.NumberFormat;

    /// <summary>The styles the box accepts when parsing typed text.</summary>
    public NumberStyles ParsingNumberStyle => _isCurrency ? IniCurrencyFormat.ParsingNumberStyle : NumberStyles.Integer;

    /// <summary>Whether the box shows the number without letting the user edit it.</summary>
    public bool IsReadOnly => _isReadOnly;

    /// <summary>The number shown; <see langword="null" /> when the box is empty.</summary>
    [ ObservableProperty ]
    public partial decimal? Value { get; set; }

    /// <inheritdoc />
    protected override string? CurrentRaw
        => Value is { } value
               ? decimal.ToInt32(decimal.Truncate(value))
                        .ToString(CultureInfo.InvariantCulture)
               : null;

    /// <inheritdoc />
    protected override void ApplyValue(string? effectiveValue)
        => Value = int.TryParse(effectiveValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : null;

    partial void OnValueChanged(decimal? value) => NotifyValueChanged();
}
