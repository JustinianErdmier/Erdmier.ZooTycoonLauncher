namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;

/// <summary>What <see cref="IniReconciler" /> did to bring <c>Current</c> in line with the file on disk.</summary>
public enum IniReconciliationOutcome
{
    /// <summary>Nothing differed; nothing was written.</summary>
    Unchanged,

    /// <summary>The database had no <c>Current</c> snapshot; <c>Original</c> and <c>Current</c> were imported.</summary>
    FirstImport,

    /// <summary>Only game-managed or unrecognised content changed; <c>Current</c> was updated without archiving.</summary>
    AdoptedSilently,

    /// <summary>A user setting changed outside the launcher; <c>Current</c> was archived (<c>Manual</c>) and then updated.</summary>
    ArchivedAndAdopted
}
