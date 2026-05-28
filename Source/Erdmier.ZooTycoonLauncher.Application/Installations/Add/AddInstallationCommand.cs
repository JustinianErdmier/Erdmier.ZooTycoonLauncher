namespace Erdmier.ZooTycoonLauncher.Application.Installations.Add;

/// <summary>
/// Persists a new <see cref="GameInstallation" />, creates the per-installation database, runs migrations, and (deferred to the
/// INI Config slice) captures the <c>Original</c> snapshot. See SDD §7.2.1.
/// </summary>
/// <param name="Name">User-visible name; trimmed by the handler; case-insensitive uniqueness enforced.</param>
/// <param name="Path">Absolute installation directory.</param>
/// <param name="MakeDefault"><see langword="true" /> to set this installation as the launcher default after persisting.</param>
public sealed record AddInstallationCommand(string Name, string Path, bool MakeDefault) : ICommand<ErrorOr<AddInstallationResult>>;
