namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;

/// <summary>A caption check box over a <c>Bool</c> key. <see cref="Inverted" /> rows show the opposite of the stored value.</summary>
public sealed partial class IniToggleFieldViewModel : IniKeyedFieldViewModel
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The row's presentation.</param>
    public IniToggleFieldViewModel(IniFieldDescriptor descriptor)
        : base(descriptor)
    {
        Caption  = descriptor.Caption ?? string.Empty;
        Inverted = descriptor.Inverted;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniToggleFieldViewModel()
        : this(new IniFieldDescriptor(new IniKeyId(Section: "advanced", Key: "loadHalfAnims"), Label: "loadHalfAnims", IniControlKind.Toggle, Hint: null,
                                      Help: "Load reduced-detail animation sets to help older hardware.")
        {
            Caption = "Reduced-detail animations"
        })
        => Load(raw: null);

    /// <summary>The caption beside the check box.</summary>
    public string Caption { get; }

    /// <summary>Whether the check box shows the opposite of the stored value.</summary>
    public bool Inverted { get; }

    /// <summary>The check box state as the user sees it.</summary>
    [ ObservableProperty ]
    public partial bool IsChecked { get; set; }

    /// <inheritdoc />
    protected override string CurrentRaw => IsChecked != Inverted ? "1" : "0";

    /// <inheritdoc />
    protected override void ApplyValue(string? effectiveValue) => IsChecked = IsTrue(effectiveValue) != Inverted;

    partial void OnIsCheckedChanged(bool value) => NotifyValueChanged();

    private static bool IsTrue(string? value)
    {
        string? trimmed = value?.Trim();

        return trimmed == "1" || (bool.TryParse(trimmed, out bool parsed) && parsed);
    }
}
