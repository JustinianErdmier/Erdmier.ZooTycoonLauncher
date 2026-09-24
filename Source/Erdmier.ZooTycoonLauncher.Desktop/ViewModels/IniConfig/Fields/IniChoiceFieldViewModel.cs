namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;

/// <summary>A <c>ComboBox</c> of labelled raw values (<c>fullscreen</c>, <c>level</c>, <c>helpType</c>).</summary>
public sealed partial class IniChoiceFieldViewModel : IniKeyedFieldViewModel
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The row's presentation, including its options.</param>
    public IniChoiceFieldViewModel(IniFieldDescriptor descriptor)
        : base(descriptor)
        => Options = descriptor.Options;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniChoiceFieldViewModel()
        : this(new IniFieldDescriptor(new IniKeyId(Section: "user", Key: "fullscreen"), Label: "fullscreen", IniControlKind.Choice, Hint: "Display mode",
                                      Help: "Run the game full screen or in a window.")
        {
            Options = [new IniChoiceOption(Raw: "1", Label: "Fullscreen"), new IniChoiceOption(Raw: "0", Label: "Windowed")]
        })
        => Load(raw: null);

    /// <summary>The entries offered.</summary>
    public IReadOnlyList<IniChoiceOption> Options { get; }

    /// <summary>The chosen entry; <see langword="null" /> only when no entry matches.</summary>
    [ ObservableProperty ]
    public partial IniChoiceOption? SelectedOption { get; set; }

    /// <inheritdoc />
    protected override string? CurrentRaw => SelectedOption?.Raw;

    /// <inheritdoc />
    protected override void ApplyValue(string? effectiveValue) => SelectedOption = Options.FirstOrDefault(option => Spec.AreEquivalent(option.Raw, effectiveValue));

    partial void OnSelectedOptionChanged(IniChoiceOption? value) => NotifyValueChanged();
}
