namespace Erdmier.ZooTycoonNexus.Application.Settings.Get;

/// <summary>Reads the launcher-wide options for the Options dialogue.</summary>
public sealed record GetLauncherOptionsQuery : IQuery<ErrorOr<LauncherOptions>>;
