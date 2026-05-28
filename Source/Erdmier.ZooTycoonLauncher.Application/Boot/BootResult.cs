namespace Erdmier.ZooTycoonLauncher.Application.Boot;

/// <summary>Terminal state determined by <see cref="BootHandler" /> after running the SDD §7.1.1 state machine.</summary>
public enum BootOutcome
{
    /// <summary>The active installation is valid; the launcher is ready to play.</summary>
    ReadyToPlay,

    /// <summary>The active installation is invalid or synchronisation failed; the launcher cannot launch the game.</summary>
    CannotPlay,

    /// <summary>No suitable installation was found; the user must add one.</summary>
    NoGameInstallationFound,

    /// <summary>The startup preference is <c>NoInstallation</c>; the user must select an installation manually.</summary>
    OpenGameInstallation,
}

/// <summary>
///     Result returned by <see cref="BootHandler" />. <see cref="BootOutcome" /> and <see cref="BootResult" /> are a tightly-coupled pair and are never used separately; they
///     live in the same file as the documented exception to the one-type-per-file rule.
/// </summary>
/// <param name="Outcome">The terminal state.</param>
/// <param name="ActiveInstallation">The installation to display, or <see langword="null" /> when none was resolved.</param>
/// <param name="LocatedCandidatePath">
///     Non-null when <see cref="BootOutcome.NoGameInstallationFound" /> is returned because <see cref="IInstallationLocator" /> found a candidate directory but the Add
///     Installation dialogue is deferred; surfaces the discovery to the user.
/// </param>
public sealed record BootResult(BootOutcome Outcome, InstallationSummary? ActiveInstallation, string? LocatedCandidatePath);
