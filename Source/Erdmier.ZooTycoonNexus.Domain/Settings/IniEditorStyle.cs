namespace Erdmier.ZooTycoonNexus.Domain.Settings;

/// <summary>How the INI editor presents its tabs: the friendly controls, or the raw INI keys and values.</summary>
public sealed class IniEditorStyle : SmartEnum<IniEditorStyle>
{
    /// <summary>Friendly labels and controls (the standard style).</summary>
    public static readonly IniEditorStyle Friendly = new(name: "Friendly", id: 1, displayName: "Friendly style");

    /// <summary>The raw INI keys and values.</summary>
    public static readonly IniEditorStyle Ini = new(name: "Ini", id: 2, displayName: "INI style");

    private IniEditorStyle(string name, int id, string displayName)
        : base(name, id)
        => DisplayName = displayName;

    /// <summary>The name shown to the user.</summary>
    public string DisplayName { get; }
}
