namespace Erdmier.ZooTycoonLauncher.Application.Game.Launch;

/// <summary>The three terminal branches of <see cref="LaunchGameCommand" />.</summary>
public enum LaunchGameOutcome
{
    /// <summary><c>zoo.exe</c> started successfully.</summary>
    Started,

    /// <summary>Just-in-time verification detected drift (e.g. <c>zoo.exe</c> missing). No launch attempted.</summary>
    Drifted,

    /// <summary>The OS rejected the start request (AV block, ACL deny, file in use). See <see cref="LaunchGameResult.FailureMessage" />.</summary>
    StartFailed,
}
