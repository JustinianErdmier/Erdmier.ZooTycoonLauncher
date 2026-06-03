namespace Erdmier.ZooTycoonLauncher.Application.Game.Launch;

/// <summary>The outcome of dispatching <see cref="LaunchGameCommand" />.</summary>
/// <param name="Outcome">Which terminal branch the handler took.</param>
/// <param name="CloseAfterGameLaunch">Snapshot of <c>LauncherSettings.CloseAfterGameLaunch</c> at the moment of launch; meaningful only when <paramref name="Outcome" /> is <see cref="LaunchGameOutcome.Started" />.</param>
/// <param name="FailureMessage">Non-<see langword="null" /> only when <paramref name="Outcome" /> is <see cref="LaunchGameOutcome.StartFailed" />; the message displayed verbatim to the user.</param>
public sealed record LaunchGameResult(LaunchGameOutcome Outcome, bool CloseAfterGameLaunch, string? FailureMessage);

/// <summary>The three terminal branches of <see cref="LaunchGameCommand" />.</summary>
/// <remarks>Co-located with <see cref="LaunchGameResult" /> because the two types are a tightly-coupled pair, matching the precedent set by <c>BootResult</c> / <c>BootOutcome</c>.</remarks>
public enum LaunchGameOutcome
{
    /// <summary><c>zoo.exe</c> started successfully.</summary>
    Started,

    /// <summary>Just-in-time verification detected drift (e.g. <c>zoo.exe</c> missing). No launch attempted.</summary>
    Drifted,

    /// <summary>The OS rejected the start request (AV block, ACL deny, file in use). See <see cref="LaunchGameResult.FailureMessage" />.</summary>
    StartFailed,
}
