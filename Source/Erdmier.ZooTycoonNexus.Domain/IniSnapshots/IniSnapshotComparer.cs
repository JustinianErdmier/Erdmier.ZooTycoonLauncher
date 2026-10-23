namespace Erdmier.ZooTycoonNexus.Domain.IniSnapshots;

/// <summary>Decides which user-setting values differ between the current INI and an older snapshot.</summary>
public static class IniSnapshotComparer
{
    /// <summary>
    ///     Compares the effective values of every user setting. Game-managed keys are ignored; a key absent from <paramref name="snapshot" /> is kept when
    ///     <paramref name="current" /> has it; a key absent from <paramref name="current" /> but present in the snapshot is always a row.
    /// </summary>
    /// <param name="current">The current INI's raw values.</param>
    /// <param name="snapshot">The older snapshot's raw values.</param>
    /// <returns>The differing rows and the kept keys, each ordered by section then registry position.</returns>
    public static IniSnapshotComparison Compare(IReadOnlyDictionary<IniKeyId, string?> current, IReadOnlyDictionary<IniKeyId, string?> snapshot)
    {
        List<IniDiffRow> rows = [ ];
        List<IniKeyId> keptKeys = [ ];

        IEnumerable<IniKeySpec> specs = ZooIniDefaults.Keys.Where(spec => spec.Role == IniKeyRole.UserSetting)
                                                      .Select((spec, index) => (Spec: spec, Index: index))
                                                      .OrderBy(entry => SectionIndex(entry.Spec.Id.Section))
                                                      .ThenBy(entry => entry.Index)
                                                      .Select(entry => entry.Spec);

        foreach (IniKeySpec spec in specs)
        {
            bool inCurrent = current.TryGetValue(spec.Id, out string? currentRaw);

            if (!snapshot.TryGetValue(spec.Id, out string? snapshotRaw))
            {
                if (inCurrent)
                {
                    keptKeys.Add(spec.Id);
                }

                continue;
            }

            // User settings always carry a default, so the effective value is never null.
            string snapshotValue = spec.EffectiveValue(snapshotRaw)!;

            if (!inCurrent)
            {
                rows.Add(new IniDiffRow(spec.Id, Current: null, snapshotValue));

                continue;
            }

            string? currentValue = spec.EffectiveValue(currentRaw);

            if (!spec.AreEquivalent(currentValue, snapshotValue))
            {
                rows.Add(new IniDiffRow(spec.Id, currentValue, snapshotValue));
            }
        }

        return new IniSnapshotComparison(rows, keptKeys);
    }

    /// <summary>Maps a snapshot's values to a lookup keyed by section and key, compared case-insensitively.</summary>
    /// <param name="snapshot">The snapshot to read.</param>
    /// <returns>The raw values keyed by <see cref="IniKeyId" />.</returns>
    public static IReadOnlyDictionary<IniKeyId, string?> ValuesOf(IniSnapshot snapshot)
    {
        Dictionary<IniKeyId, string?> values = [ ];

        foreach (IniValue value in snapshot.Values)
        {
            values[new IniKeyId(value.Section, value.Key)] = value.Value;
        }

        return values;
    }

    private static int SectionIndex(string section)
    {
        for (int i = 0; i < ZooIniDefaults.Sections.Count; i++)
        {
            if (string.Equals(ZooIniDefaults.Sections[i], section, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return int.MaxValue;
    }
}
