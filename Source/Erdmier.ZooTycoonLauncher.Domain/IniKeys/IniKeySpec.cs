namespace Erdmier.ZooTycoonLauncher.Domain.IniKeys;

/// <summary>
///     Describes one recognised <c>zoo.ini</c> key: its kind, factory default, numeric bounds, and role. Values stay raw strings everywhere; this type supplies the kind-aware
///     rules for validating and comparing them (SDD §5.3).
/// </summary>
/// <param name="Id">The key's section and name, in the casing the launcher writes when it inserts the key.</param>
/// <param name="Kind">The value's kind.</param>
/// <param name="DefaultValue">The factory default in INI text form, or <see langword="null" /> when the key has none.</param>
/// <param name="Min">The inclusive lower bound for integer kinds, or <see langword="null" /> when unbounded.</param>
/// <param name="Max">The inclusive upper bound for integer kinds, or <see langword="null" /> when unbounded.</param>
/// <param name="Role">Whether the key is a user setting or game-managed runtime state.</param>
public sealed record IniKeySpec(IniKeyId Id, IniValueKind Kind, string? DefaultValue, int? Min, int? Max, IniKeyRole Role)
{
    /// <summary>Returns whether <paramref name="raw" /> is a valid value for this key. <see langword="null" /> means the key is absent from the file.</summary>
    /// <param name="raw">The raw value; surrounding whitespace is ignored.</param>
    /// <returns><see langword="true" /> when the value is valid for the key's kind and bounds.</returns>
    public bool IsValid(string? raw)
    {
        string? trimmed = raw?.Trim();

        if (Kind == IniValueKind.Bool)
        {
            return trimmed is not null && TryParseBool(trimmed, out bool _);
        }

        if (Kind == IniValueKind.Int)
        {
            return trimmed is not null && TryParseBoundedInt(trimmed);
        }

        if (Kind == IniValueKind.NullableInt)
        {
            return string.IsNullOrEmpty(trimmed) || TryParseBoundedInt(trimmed);
        }

        if (Kind == IniValueKind.Str)
        {
            return trimmed is not null;
        }

        return true;
    }

    /// <summary>
    ///     Returns whether two raw values denote the same setting: booleans by meaning (<c>1</c> ≡ <c>true</c>), integers numerically (<c>075</c> ≡ <c>75</c>), nullable kinds
    ///     treating empty as absent, everything else by ordinal comparison of the trimmed text.
    /// </summary>
    /// <param name="left">The first raw value, or <see langword="null" /> when absent.</param>
    /// <param name="right">The second raw value, or <see langword="null" /> when absent.</param>
    /// <returns><see langword="true" /> when the values are equivalent.</returns>
    public bool AreEquivalent(string? left, string? right)
    {
        string? a = left?.Trim();
        string? b = right?.Trim();

        if (Kind == IniValueKind.NullableInt
            || Kind == IniValueKind.NullableStr)
        {
            a = string.IsNullOrEmpty(a) ? null : a;
            b = string.IsNullOrEmpty(b) ? null : b;
        }

        if (a is null
            || b is null)
        {
            return a is null && b is null;
        }

        if (Kind == IniValueKind.Bool
            && TryParseBool(a, out bool leftBool)
            && TryParseBool(b, out bool rightBool))
        {
            return leftBool == rightBool;
        }

        if ((Kind == IniValueKind.Int || Kind == IniValueKind.NullableInt)
            && int.TryParse(a, NumberStyles.Integer, CultureInfo.InvariantCulture, out int leftInt)
            && int.TryParse(b, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rightInt))
        {
            return leftInt == rightInt;
        }

        return string.Equals(a, b, StringComparison.Ordinal);
    }

    /// <summary>Returns the value the editor should display: the trimmed raw value when valid, otherwise <see cref="DefaultValue" /> (the silent fallback of SDD §5.3).</summary>
    /// <param name="raw">The raw value, or <see langword="null" /> when absent.</param>
    /// <returns>The effective value.</returns>
    public string? EffectiveValue(string? raw) => IsValid(raw) ? raw?.Trim() : DefaultValue;

    private static bool TryParseBool(string value, out bool result)
    {
        switch (value)
        {
            case "0":
                result = false;

                return true;

            case "1":
                result = true;

                return true;

            default:
                return bool.TryParse(value, out result);
        }
    }

    private bool TryParseBoundedInt(string value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
           && (Min is null || parsed >= Min)
           && (Max is null || parsed <= Max);
}
