namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>The strongly typed kind an INI value carries (SDD §5.2).</summary>
/// <remarks>
///     Persisted as a string but parsed by the registry at read time. <see cref="Scenario" /> is reserved for values in the <c>[scenario]</c> section, which always carry an
///     integer that the Scenarios UI maps to <c>Complete</c>/<c>Locked</c>.
/// </remarks>
public sealed class IniValueKind : SmartEnum<IniValueKind>
{
    /// <summary>A boolean (stored as <c>0</c>/<c>1</c>).</summary>
    public static readonly IniValueKind Bool = new(name: "Bool", id: 1);

    /// <summary>A non-nullable signed 32-bit integer.</summary>
    public static readonly IniValueKind Int = new(name: "Int", id: 2);

    /// <summary>A nullable signed 32-bit integer.</summary>
    public static readonly IniValueKind NullableInt = new(name: "NullableInt", id: 3);

    /// <summary>A nullable string.</summary>
    public static readonly IniValueKind NullableStr = new(name: "NullableStr", id: 5);

    /// <summary>A scenario-section integer with <c>Complete</c>/<c>Locked</c> semantics.</summary>
    public static readonly IniValueKind Scenario = new(name: "Scenario", id: 6);

    /// <summary>A non-nullable string.</summary>
    public static readonly IniValueKind Str = new(name: "Str", id: 4);

    private IniValueKind(string name, int id)
        : base(name, id)
    { }
}
