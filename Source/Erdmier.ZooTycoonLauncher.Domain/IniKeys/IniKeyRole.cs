namespace Erdmier.ZooTycoonLauncher.Domain.IniKeys;

/// <summary>Whether an INI key is a setting the user edits or runtime state the game maintains itself (tiered drift, SDD §7.7).</summary>
public sealed class IniKeyRole : SmartEnum<IniKeyRole>
{
    /// <summary>Written by the game on exit (window position, last file, tutorial flags). Hidden from the editor; a change is adopted without archiving.</summary>
    public static readonly IniKeyRole GameManaged = new(name: "GameManaged", id: 2);

    /// <summary>A setting the user edits in the INI Config tab. A change made outside the launcher archives <c>Current</c> to <c>Historical</c>.</summary>
    public static readonly IniKeyRole UserSetting = new(name: "UserSetting", id: 1);

    private IniKeyRole(string name, int id)
        : base(name, id)
    { }
}
