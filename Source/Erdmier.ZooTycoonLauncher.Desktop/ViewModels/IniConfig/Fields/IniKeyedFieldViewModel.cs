namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;

/// <summary>
///     Base of every row bound to one registry key. Keeps the loaded raw value as the baseline, shows <see cref="IniKeySpec.EffectiveValue" /> (the default when the stored value
///     is invalid), and is dirty only when the control's value is not <see cref="IniKeySpec.AreEquivalent" /> to that effective baseline. Abstract, so it has no view.
/// </summary>
public abstract class IniKeyedFieldViewModel : IniFieldViewModel
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="descriptor">The row's presentation; its id must be a recognised key.</param>
    /// <exception cref="ArgumentException">The descriptor does not name a recognised key.</exception>
    protected IniKeyedFieldViewModel(IniFieldDescriptor descriptor)
        : base(descriptor)
    {
        if (descriptor.Id is not { } id
            || !ZooIniDefaults.TryGet(id, out IniKeySpec? spec))
        {
            throw new ArgumentException($"The descriptor '{descriptor.Label}' does not name a recognised INI key.", nameof(descriptor));
        }

        Spec = spec;
    }

    /// <summary>The key's registry spec.</summary>
    public IniKeySpec Spec { get; }

    /// <summary>The raw value last loaded, or <see langword="null" /> when the key is absent from the file.</summary>
    public string? Baseline { get; private set; }

    /// <inheritdoc />
    public override bool IsDirty => !Spec.AreEquivalent(CurrentRaw, Spec.EffectiveValue(Baseline));

    /// <summary>The control's current value as INI text, or <see langword="null" /> when the control is empty.</summary>
    protected abstract string? CurrentRaw { get; }

    /// <inheritdoc />
    public override void Load(string? raw)
    {
        Baseline = raw;

        Reset();
    }

    /// <inheritdoc />
    public override void Reset()
    {
        ApplyValue(Spec.EffectiveValue(Baseline));

        NotifyValueChanged();
    }

    /// <inheritdoc />
    public override bool TryGetEdit([ NotNullWhen(true) ] out string? raw)
    {
        if (!IsDirty)
        {
            raw = null;

            return false;
        }

        raw = CurrentRaw ?? string.Empty;

        return true;
    }

    /// <summary>Sets the control from an effective raw value.</summary>
    /// <param name="effectiveValue">The value to show.</param>
    protected abstract void ApplyValue(string? effectiveValue);

    /// <summary>Raises <see cref="IniFieldViewModel.IsDirty" />; derived rows call it whenever their control value changes.</summary>
    protected void NotifyValueChanged() => OnPropertyChanged(nameof(IsDirty));
}
