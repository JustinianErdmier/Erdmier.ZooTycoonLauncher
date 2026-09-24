namespace Erdmier.ZooTycoonLauncher.Domain.IniKeys;

/// <summary>
///     Identifies one <c>zoo.ini</c> key by its section and key name. Equality and hashing ignore case on both parts, matching how the game reads the file (SDD §5.3).
/// </summary>
/// <param name="Section">The section name without brackets, e.g. <c>UI</c>.</param>
/// <param name="Key">The key name, e.g. <c>tooltipDelay</c>.</param>
public readonly record struct IniKeyId(string Section, string Key)
{
    /// <summary>Compares both parts case-insensitively.</summary>
    /// <param name="other">The id to compare with.</param>
    /// <returns><see langword="true" /> when section and key match ignoring case.</returns>
    public bool Equals(IniKeyId other)
        => string.Equals(Section, other.Section, StringComparison.OrdinalIgnoreCase)
           && string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns a hash code consistent with the case-insensitive <see cref="Equals(IniKeyId)" />.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
        => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(Section ?? string.Empty),
                            StringComparer.OrdinalIgnoreCase.GetHashCode(Key ?? string.Empty));

    /// <summary>Formats the id as <c>[Section]/Key</c>.</summary>
    /// <returns>The formatted id.</returns>
    public override string ToString() => $"[{Section}]/{Key}";
}
