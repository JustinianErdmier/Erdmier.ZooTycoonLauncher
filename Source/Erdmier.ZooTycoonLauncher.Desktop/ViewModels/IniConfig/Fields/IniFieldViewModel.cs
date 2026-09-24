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
    }

    /// <summary>The key the row edits, or <see langword="null" /> for a derived row (the language picker).</summary>
    public IniKeyId? Id { get; }

    /// <summary>The row label — the literal INI key.</summary>
    public string Label { get; }

    /// <summary>The muted sub-line, or <see langword="null" />.</summary>
    public string? Hint { get; }

    /// <summary>Whether <see cref="Hint" /> has text.</summary>
    public bool HasHint => !string.IsNullOrEmpty(Hint);

    /// <summary>The help text: the row tooltip and the footer line while the row is hovered or focused.</summary>
    public string Help { get; }

    /// <summary>Whether the row is hovered or holds focus; set by <c>IniSectionView</c>, observed by the editor to drive the footer help line.</summary>
    [ ObservableProperty ]
    public partial bool IsHelpActive { get; set; }

    /// <summary>Whether the row's value differs from what was loaded.</summary>
    public virtual bool IsDirty => false;

    /// <summary>Sets the row's baseline from the loaded raw value and resets the control to it.</summary>
    /// <param name="raw">The raw value from <c>Current</c>, or <see langword="null" /> when the key is absent.</param>
    public virtual void Load(string? raw)
    { }

    /// <summary>Discards the row's edit, returning the control to the baseline.</summary>
    public virtual void Reset()
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
