namespace Erdmier.ZooTycoonLauncher.Domain.IniDrift;

/// <summary>How the on-disk <c>zoo.ini</c> differs from the <c>Current</c> snapshot's recognised values (tiered drift, SDD §7.7).</summary>
public sealed class IniDriftKind : SmartEnum<IniDriftKind>
{
    /// <summary>Only game-managed keys changed; the change is adopted without archiving.</summary>
    public static readonly IniDriftKind GameManagedOnly = new(name: "GameManagedOnly", id: 2);

    /// <summary>No recognised value changed.</summary>
    public static readonly IniDriftKind None = new(name: "None", id: 1);

    /// <summary>At least one user setting changed; <c>Current</c> is archived before the change is adopted.</summary>
    public static readonly IniDriftKind UserSettings = new(name: "UserSettings", id: 3);

    private IniDriftKind(string name, int id)
        : base(name, id)
    { }
}
