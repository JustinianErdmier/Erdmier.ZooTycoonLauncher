namespace Erdmier.ZooTycoonNexus.Domain.IniKeys;

/// <summary>
///     Builds a complete default <c>zoo.ini</c> from the INI registry (SDD §7.4; milestone 10 spec, C1). Used to recreate a missing or unreadable file.
/// </summary>
/// <remarks>Keys with no default (the nullable game-managed keys) are omitted, and <c>[scenario]</c> is never written.</remarks>
public static class ZooIniDefaultDocument
{
    /// <summary>Renders the default <c>zoo.ini</c> text: registry sections and key order, <c>key=value</c> lines, blank-line separated sections, CRLF line endings throughout.</summary>
    /// <returns>The default file text, ending in a CRLF.</returns>
    public static string Render()
    {
        StringBuilder builder = new();

        foreach (string section in ZooIniDefaults.Sections)
        {
            if (builder.Length > 0)
            {
                builder.Append("\r\n");
            }

            builder.Append('[').Append(section).Append("]\r\n");

            foreach (IniKeySpec spec in ZooIniDefaults.Keys.Where(spec => string.Equals(spec.Id.Section, section, StringComparison.OrdinalIgnoreCase)))
            {
                if (spec.DefaultValue is not null)
                {
                    builder.Append(spec.Id.Key).Append('=').Append(spec.DefaultValue).Append("\r\n");
                }
            }
        }

        return builder.ToString();
    }

    /// <summary>Parses <see cref="Render" /> into an <see cref="IniDocument" />.</summary>
    /// <returns>The default document.</returns>
    public static IniDocument Create() => IniDocument.Parse(Render());
}
