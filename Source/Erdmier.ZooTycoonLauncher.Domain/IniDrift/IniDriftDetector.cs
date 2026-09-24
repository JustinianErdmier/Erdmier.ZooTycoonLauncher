namespace Erdmier.ZooTycoonLauncher.Domain.IniDrift;

/// <summary>Classifies the difference between two sets of recognised INI values (SDD §7.7, tiered drift).</summary>
public static class IniDriftDetector
{
    /// <summary>
    ///     Compares every registry key with <see cref="IniKeySpec.AreEquivalent" /> (a missing key counts as <see langword="null" />). Any changed user setting makes the drift
    ///     <see cref="IniDriftKind.UserSettings" />; only changed game-managed keys make it <see cref="IniDriftKind.GameManagedOnly" />. Unrecognised keys are never compared.
    /// </summary>
    /// <param name="currentValues">The <c>Current</c> snapshot's values.</param>
    /// <param name="diskValues">The values extracted from the file on disk.</param>
    /// <returns>The drift tier and every changed key.</returns>
    public static IniDriftResult Detect(IReadOnlyDictionary<IniKeyId, string?> currentValues, IReadOnlyDictionary<IniKeyId, string?> diskValues)
    {
        List<IniKeyId> changed            = [];
        bool           userSettingChanged = false;

        foreach (IniKeySpec spec in ZooIniDefaults.Keys)
        {
            string? current = Find(currentValues, spec.Id);
            string? disk    = Find(diskValues, spec.Id);

            if (spec.AreEquivalent(current, disk))
            {
                continue;
            }

            changed.Add(spec.Id);

            userSettingChanged |= spec.Role == IniKeyRole.UserSetting;
        }

        IniDriftKind kind = changed.Count == 0
                                ? IniDriftKind.None
                                : userSettingChanged
                                    ? IniDriftKind.UserSettings
                                    : IniDriftKind.GameManagedOnly;

        return new IniDriftResult(kind, changed);
    }

    // IniKeyId equality is case-insensitive, so a plain lookup works whatever casing the caller's dictionary holds.
    private static string? Find(IReadOnlyDictionary<IniKeyId, string?> values, IniKeyId id) => values.TryGetValue(id, out string? value) ? value : null;
}
