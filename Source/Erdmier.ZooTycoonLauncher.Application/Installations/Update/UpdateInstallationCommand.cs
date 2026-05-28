namespace Erdmier.ZooTycoonLauncher.Application.Installations.Update;

/// <summary>
///     Updates the mutable fields on an existing installation: <c>Name</c> and (optionally) the default flag. Path changes go through <see cref="RelocateInstallationCommand" />
///     instead.
/// </summary>
/// <param name="InstallationId">The installation's identifier.</param>
/// <param name="Name">New user-visible name.</param>
/// <param name="MakeDefault">
///     <see langword="true" /> to promote this installation to default. Set to <see langword="false" /> to leave the default untouched (the dialogue cannot
///     un-tick default — see SDD §7.2.3 step 3).
/// </param>
public sealed record UpdateInstallationCommand(Guid InstallationId, string Name, bool MakeDefault) : ICommand<ErrorOr<Success>>;
