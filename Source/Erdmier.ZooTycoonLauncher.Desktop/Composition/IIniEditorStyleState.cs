namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>
///     The INI editor style in force for the running launcher, shared by every INI Config tab. Loaded from the launcher options before an editor is first built, and set again
///     when the Options dialogue saves a different style, so open editors re-render live.
/// </summary>
public interface IIniEditorStyleState
{
    /// <summary>The style the INI editor renders in. Setting it raises <see cref="StyleChanged" /> only when the value differs.</summary>
    IniEditorStyle Style { get; set; }

    /// <summary>Raised after <see cref="Style" /> changes to a different value.</summary>
    event EventHandler? StyleChanged;
}
