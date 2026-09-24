namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;

/// <summary>A <c>TextBox</c> over a string key.</summary>
public sealed partial class IniTextFieldViewModel : IniKeyedFieldViewModel
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The row's presentation.</param>
    public IniTextFieldViewModel(IniFieldDescriptor descriptor)
        : base(descriptor)
    { }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniTextFieldViewModel()
        : this(new IniFieldDescriptor(new IniKeyId(Section: "UI", Key: "menuMusic"), Label: "menuMusic", IniControlKind.Text, Hint: "Relative to the game folder",
                                      Help: "Path to the main-menu music file."))
        => Load(raw: null);

    /// <summary>The text shown.</summary>
    [ ObservableProperty ]
    public partial string Value { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override string CurrentRaw => Value;

    /// <inheritdoc />
    protected override void ApplyValue(string? effectiveValue) => Value = effectiveValue ?? string.Empty;

    partial void OnValueChanged(string value) => NotifyValueChanged();
}
