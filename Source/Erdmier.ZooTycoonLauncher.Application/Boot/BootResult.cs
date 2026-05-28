namespace Erdmier.ZooTycoonLauncher.Application.Boot;

/// <summary>Result returned by <see cref="BootHandler" /> after running the SDD §7.1.1 state machine.</summary>
/// <param name="Outcome">The terminal state.</param>
/// <param name="ActiveInstallation">The installation to display, or <see langword="null" /> when none was resolved.</param>
/// <param name="LocatedCandidatePath">
///     Non-null when <see cref="BootOutcome.NoGameInstallationFound" /> is returned because <see cref="IInstallationLocator" /> found a candidate directory but the Add
///     Installation dialogue is deferred; surfaces the discovery to the user.
/// </param>
public sealed record BootResult(BootOutcome Outcome, InstallationSummary? ActiveInstallation, string? LocatedCandidatePath);
