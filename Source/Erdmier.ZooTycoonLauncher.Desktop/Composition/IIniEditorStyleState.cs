namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>
///     The INI editor style in force for the running launcher, shared by every Game Config tab. Loaded from the launcher options before an editor is first built, and set again
///     when the Options dialogue saves a different style, so open editors re-render live.
/// </summary>
public interface IIniEditorStyleState
{
    /// <summary>The style the INI editor renders in. Setting it raises <see cref="StyleChanged" /> only when the value differs.</summary>
    IniEditorStyle Style { get; set; }

    /// <summary>
    ///     A counter that increases on every assignment to <see cref="Style" />, including one that leaves the value unchanged. A lazy loader reads it before awaiting its query and
    ///     assigns the loaded style only if it is unchanged afterwards, so a stored value read before an Options save never overwrites the style that save applied.
    /// </summary>
    int Version { get; }

    /// <summary>Raised after <see cref="Style" /> changes to a different value.</summary>
    event EventHandler? StyleChanged;
}
