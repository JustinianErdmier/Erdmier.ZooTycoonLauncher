namespace Erdmier.ZooTycoonLauncher.Application.Game.Launch;

/// <summary>The outcome of dispatching <see cref="LaunchGameCommand" />.</summary>
/// <param name="Outcome">Which terminal branch the handler took.</param>
/// <param name="CloseAfterGameLaunch">Snapshot of <c>LauncherSettings.CloseAfterGameLaunch</c> at the moment of launch; meaningful only when <paramref name="Outcome" /> is <see cref="LaunchGameOutcome.Started" />.</param>
/// <param name="FailureMessage">Non-<see langword="null" /> only when <paramref name="Outcome" /> is <see cref="LaunchGameOutcome.StartFailed" />; the message displayed verbatim to the user.</param>
public sealed record LaunchGameResult(LaunchGameOutcome Outcome, bool CloseAfterGameLaunch, string? FailureMessage);
