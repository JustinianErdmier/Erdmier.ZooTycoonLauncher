namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig.Fields;

/// <summary>
///     Base of every INI editor row. Rendered by <c>IniSectionView</c>'s standard row (label + hint on the left, the field's own view on the right); the <c>ViewLocator</c> resolves
///     the concrete field's view. Abstract, so it has no view of its own.
/// </summary>
public abstract partial class IniFieldViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance from its catalogue entry.</summary>
    /// <param name="descriptor">The row's presentation.</param>
    protected IniFieldViewModel(IniFieldDescriptor descriptor)
    {
        Id    = descriptor.Id;
        Label = descriptor.Label;
        Hint  = descriptor.Hint;
        Help  = descriptor.Help;

        IsHidden = descriptor.IsHidden;
    }

    /// <summary>The key the row edits, or <see langword="null" /> for a derived row (the language picker).</summary>
    public IniKeyId? Id { get; }

    /// <summary>The row label in the active layout: the literal INI key in the INI style, the plain-language label in the Friendly style.</summary>
    public string Label { get; }

    /// <summary>The muted sub-line, or <see langword="null" />.</summary>
    public string? Hint { get; }

    /// <summary>Whether <see cref="Hint" /> has text.</summary>
    public bool HasHint => !string.IsNullOrEmpty(Hint);

    /// <summary>The help text: the row tooltip and the footer line while the row is hovered or focused.</summary>
    public string Help { get; }

    /// <summary>
    ///     Whether the row exists only to carry its key's value and edits and is rendered nowhere (the Friendly layout's language codes, which the picker drives).
    /// </summary>
    public bool IsHidden { get; }

    /// <summary>Whether the editor is in the INI style: the label and hint use the monospace key face. Set by <see cref="IniFieldFactory" /> when it builds the row.</summary>
    public bool IsIniStyle { get; set; }

    /// <summary>Whether the row's own control carries the label, so the row has no left-hand label column (a Friendly style check box, spec C4).</summary>
    public virtual bool UsesCaptionAsLabel => false;

    /// <summary>Whether the row has a left-hand label column.</summary>
    public bool ShowLabelColumn => !UsesCaptionAsLabel;

    /// <summary>Whether the hint shows under the row's control, because the row has no label column to carry it.</summary>
    public bool ShowHintBelowControl => !ShowLabelColumn && HasHint;

    /// <summary>The grid column the row's control sits in.</summary>
    public int ControlColumn => ShowLabelColumn ? 1 : 0;

    /// <summary>The number of grid columns the row's control spans.</summary>
    public int ControlColumnSpan => ShowLabelColumn ? 1 : 2;

    /// <summary>Whether the pointer is over the row; set by <c>IniSectionView</c>, observed by the editor to drive the footer help line.</summary>
    [ ObservableProperty ]
    public partial bool IsHovered { get; set; }

    /// <summary>Whether focus is within the row; set by <c>IniSectionView</c>, observed by the editor to drive the footer help line.</summary>
    [ ObservableProperty ]
    public partial bool IsFocused { get; set; }

    /// <summary>Whether the row's value differs from what was loaded.</summary>
    public virtual bool IsDirty => false;

    /// <summary>Whether the control already shows the key's factory default; <see langword="true" /> for a derived row, which has nothing of its own to reset.</summary>
    public virtual bool IsAtDefault => true;

    /// <summary>Sets the row's baseline from the loaded raw value and resets the control to it.</summary>
    /// <param name="raw">The raw value from <c>Current</c>, or <see langword="null" /> when the key is absent.</param>
    public virtual void Load(string? raw)
    { }

    /// <summary>Discards the row's edit, returning the control to the baseline.</summary>
    public virtual void Reset()
    { }

    /// <summary>Sets the control to the key's factory default as an unsaved edit; the baseline is untouched.</summary>
    public virtual void ResetToDefault()
    { }

    /// <summary>Returns the row's edit as a raw INI value when the row is dirty.</summary>
    /// <param name="raw">The raw value to save.</param>
    /// <returns><see langword="true" /> when there is an edit.</returns>
    public virtual bool TryGetEdit([ NotNullWhen(true) ] out string? raw)
    {
        raw = null;

        return false;
    }
}
