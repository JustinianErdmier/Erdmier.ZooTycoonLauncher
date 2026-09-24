namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>How one INI row is presented (SDD §9.3, <c>conventions.md</c> §3).</summary>
/// <param name="Id">The key the row edits, or <see langword="null" /> for the derived language picker.</param>
/// <param name="Label">The row label — the literal INI key.</param>
/// <param name="Control">The input control.</param>
/// <param name="Hint">The muted sub-line (units, range, shape), or <see langword="null" />.</param>
/// <param name="Help">The help text shown as the row tooltip and in the editor footer.</param>
public sealed record IniFieldDescriptor(IniKeyId? Id, string Label, IniControlKind Control, string? Hint, string Help)
{
    /// <summary>The caption beside a toggle's check box.</summary>
    public string? Caption { get; init; }

    /// <summary>Whether a toggle shows the opposite of the stored value (<c>noMenuMusic</c> reads "Play menu music").</summary>
    public bool Inverted { get; init; }

    /// <summary>The entries of a choice row.</summary>
    public IReadOnlyList<IniChoiceOption> Options { get; init; } = [];
}
