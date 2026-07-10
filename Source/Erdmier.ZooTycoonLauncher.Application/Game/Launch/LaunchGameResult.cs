namespace Erdmier.ZooTycoonLauncher.Application.Game.Launch;

/// <summary>The outcome of dispatching <see cref="LaunchGameCommand" />.</summary>
/// <param name="Outcome">Which terminal branch the handler took.</param>
/// <param name="CloseAfterGameLaunch">
///     Snapshot of <c>LauncherSettings.CloseAfterGameLaunch</c> at the moment of launch; meaningful only when <paramref name="Outcome" /> is
///     <see cref="LaunchGameOutcome.Started" />.
/// </param>
/// <param name="FailureMessage">
///     Non-<see langword="null" /> only when <paramref name="Outcome" /> is <see cref="LaunchGameOutcome.StartFailed" />; the message displayed verbatim to
///     the user.
/// </param>
/// <param name="LastPlayedUtc">
///     The UTC timestamp stamped onto the row after a successful start; non-<see langword="null" /> only when <paramref name="Outcome" /> is
///     <see cref="LaunchGameOutcome.Started" /> and the persist succeeded. The Desktop layer uses it to refresh the "Last played" display without re-reading the
///     database.
/// </param>
public sealed record LaunchGameResult(LaunchGameOutcome Outcome, bool CloseAfterGameLaunch, string? FailureMessage, DateTime? LastPlayedUtc = null);
