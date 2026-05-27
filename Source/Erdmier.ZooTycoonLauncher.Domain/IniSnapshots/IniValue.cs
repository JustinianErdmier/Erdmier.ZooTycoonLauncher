namespace Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;

/// <summary>
/// One INI value row in an <see cref="IniSnapshot" />.
/// </summary>
public sealed class IniValue
{
    /// <summary>Auto-increment surrogate key.</summary>
    public long Id { get; init; }

    /// <summary>The snapshot this row belongs to.</summary>
    public Guid SnapshotId { get; init; }

    /// <summary>The <c>[section]</c> the key is under, e.g. <c>user</c>, <c>ui</c>, <c>scenario</c>.</summary>
    public string Section { get; init; } = string.Empty;

    /// <summary>The key within the section, e.g. <c>showtipsatstartup</c>.</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>The value as a string (parsed by the registry per <see cref="ValueKind" />). Nullable for absent values.</summary>
    public string? Value { get; set; }

    /// <summary>The strongly typed kind of value carried here.</summary>
    public IniValueKind ValueKind { get; init; } = IniValueKind.Str;

    /// <summary>How this value got into its snapshot.</summary>
    public IniValueSource Source { get; set; } = IniValueSource.OriginalImport;
}
