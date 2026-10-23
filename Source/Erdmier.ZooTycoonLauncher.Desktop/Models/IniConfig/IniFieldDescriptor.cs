namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>How one INI row is presented (SDD §9.3, <c>conventions.md</c> §3).</summary>
/// <param name="Id">The key the row edits, or <see langword="null" /> for the derived language picker.</param>
/// <param name="Label">The row label in the layout the descriptor belongs to: the literal INI key in the INI layout, the plain-language label in the Friendly layout.</param>
/// <param name="Control">The input control.</param>
/// <param name="Hint">The muted sub-line (units, range, shape), or <see langword="null" />.</param>
/// <param name="Help">The help text shown as the row tooltip and in the editor footer.</param>
public sealed record IniFieldDescriptor(IniKeyId? Id, string Label, IniControlKind Control, string? Hint, string Help)
{
    /// <summary>The caption beside a toggle's check box.</summary>
    public string? Caption { get; init; }

    /// <summary>How the control's value relates to the raw file value (Friendly layout only; the INI layout shows raw values).</summary>
    public IniValueConversion Conversion { get; init; } = IniValueConversion.None;

    /// <summary>Whether the row stays in the layout for its key but is rendered nowhere (the Friendly layout's language codes, which the picker drives).</summary>
    public bool IsHidden { get; init; }

    /// <summary>Whether a number row is a cash amount, shown as dollars (<c>$1,000,000</c>) rather than a plain number.</summary>
    public bool IsCurrency { get; init; }

    /// <summary>Whether a number row is shown but cannot be edited directly (the language codes, which the picker sets).</summary>
    public bool IsReadOnly { get; init; }

    /// <summary>The entries of a choice row.</summary>
    public IReadOnlyList<IniChoiceOption> Options { get; init; } = [];
}
