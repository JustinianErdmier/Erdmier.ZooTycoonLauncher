namespace Erdmier.ZooTycoonLauncher.Application.Installations.Locate;

/// <summary>
/// Walks <see cref="IInstallationLocator" /> and returns the discovered directory plus the full probe trail. Always succeeds —
/// "no candidate found" is a value, not an error.
/// </summary>
public sealed record LocateZooTycoonQuery : IQuery<ErrorOr<LocatedDirectory>>;
