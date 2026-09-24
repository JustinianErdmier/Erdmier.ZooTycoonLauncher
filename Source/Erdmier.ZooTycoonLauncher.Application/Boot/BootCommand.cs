namespace Erdmier.ZooTycoonLauncher.Application.Boot;

/// <summary>Dispatched at application startup to resolve which installation to open and transition the main window to the correct state (SDD §7.1).</summary>
/// <param name="InstallationId">
///     When supplied, points the startup pipeline directly at this installation (SDD §7.2.7 — the picker's <c>Open</c> button), bypassing the startup preference and
///     default resolution entirely — unless the id is unknown, in which case the pipeline falls back to a normal preference-driven boot. <see langword="null" /> (the
///     default) also performs a normal boot, governed by <see cref="LauncherSettings.LauncherStartupPreference" />.
/// </param>
public sealed record BootCommand(Guid? InstallationId = null) : ICommand<ErrorOr<BootResult>>;
