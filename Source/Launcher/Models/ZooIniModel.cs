using System.Collections.Generic;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>In-memory representation of <c> zoo.ini </c>, composed of one strongly typed submodel per INI section.</summary>
/// <remarks>
///     This is a plain class rather than a record so that property change tracking can be added if needed. Unknown keys (those present in the file but not understood by the
///     launcher) are preserved verbatim in <see cref="UnknownKeys" /> so that round-trip writes do not lose data.
/// </remarks>
public class ZooIniModel
{
    /// <summary>Graphics quality and audio compatibility settings (INI section <c> [advanced] </c>).</summary>
    public AdvancedSettings Advanced { get; set; } = new();

    /// <summary>AI-related gameplay settings (INI section <c> [ai] </c>).</summary>
    public AiSettings AI { get; set; } = new();

    /// <summary>Developer and diagnostic settings (INI section <c> [debug] </c>).</summary>
    public DebugSettings Debug { get; set; } = new();

    /// <summary>Language identifier settings (INI section <c> [language] </c>).</summary>
    public LanguageSettings Language { get; set; } = new();

    /// <summary>Default map dimension settings (INI section <c> [Map] </c>).</summary>
    public MapSettings Map { get; set; } = new();

    /// <summary>Audio, gameplay (cash), and interface settings (INI section <c> [UI] </c>).</summary>
    public UISettings UI { get; set; } = new();

    /// <summary>
    ///     Keys present in <c> zoo.ini </c> that are not known to the launcher, including the contents of unmanaged sections such as <c> [mgr] </c>, <c> [lib] </c>,
    ///     <c> [resource] </c>, and <c> [scenario] </c>. Stored as <c> "Section.Key" </c> => <c> "Value" </c> and written back verbatim on save to ensure no data loss.
    /// </summary>
    public Dictionary<string, string> UnknownKeys { get; set; } = new();

    /// <summary>Display and performance settings (INI section <c> [user] </c>).</summary>
    public UserSettings User { get; set; } = new();
}
