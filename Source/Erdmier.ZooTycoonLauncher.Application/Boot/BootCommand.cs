namespace Erdmier.ZooTycoonLauncher.Application.Boot;

/// <summary>Dispatched at application startup to resolve which installation to open and transition the main window to the correct state (SDD §7.1).</summary>
public sealed record BootCommand : ICommand<ErrorOr<BootResult>>;
