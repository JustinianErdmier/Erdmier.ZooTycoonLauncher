namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;

/// <summary>A <c>ComboBox</c> of labelled raw values (<c>fullscreen</c>, <c>level</c>, <c>helpType</c>).</summary>
public sealed class IniChoiceFieldViewModel : IniKeyedFieldViewModel
{
    private IniChoiceOption? _selectedOption;

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

    /// <summary>
    ///     The chosen entry; <see langword="null" /> only when no entry matches. Setting <see langword="null" /> is ignored — a combo box detaching from the visual tree can write
    ///     it back — mirroring <see cref="IniLanguageFieldViewModel.SelectedOption" />; <see cref="ApplyValue" /> still writes <see langword="null" /> through directly.
    /// </summary>
    public IniChoiceOption? SelectedOption
    {
        get => _selectedOption;
        set
        {
            if (value is null)
            {
                return;
            }

            SetSelectedOption(value);
        }
    }

    /// <inheritdoc />
    protected override string? CurrentRaw => SelectedOption?.Raw;

    /// <inheritdoc />
    protected override void ApplyValue(string? effectiveValue) => SetSelectedOption(Options.FirstOrDefault(option => Spec.AreEquivalent(option.Raw, effectiveValue)));

    private void SetSelectedOption(IniChoiceOption? option)
    {
        if (SetProperty(ref _selectedOption, option, nameof(SelectedOption)))
        {
            NotifyValueChanged();
        }
    }
}
