using Microsoft.Extensions.Logging;

namespace Erdmier.ZooTycoonLauncher.Application.Game.Launch;

/// <summary>Handler for <see cref="LaunchGameCommand" />. SDD §7.10.</summary>
public sealed class LaunchGameHandler : ICommandHandler<LaunchGameCommand, ErrorOr<LaunchGameResult>>
{
    private readonly TimeProvider _clock;

    private readonly IInstallationRepository _installations;

    private readonly ILogger<LaunchGameHandler> _logger;

    private readonly IProcessLauncher _processLauncher;

    private readonly ILauncherSettingsRepository _settings;

    private readonly IInstallationVerifier _verifier;

    /// <summary>Initialises a new instance.</summary>
    public LaunchGameHandler(IInstallationRepository     installations,
                             IInstallationVerifier       verifier,
                             IProcessLauncher            processLauncher,
                             ILauncherSettingsRepository settings,
                             TimeProvider                clock,
                             ILogger<LaunchGameHandler>  logger)
    {
        _installations   = installations;
        _verifier        = verifier;
        _processLauncher = processLauncher;
        _settings        = settings;
        _clock           = clock;
        _logger          = logger;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<LaunchGameResult>> Handle(LaunchGameCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", $"No installation with id {command.InstallationId}.");
        }

        VerificationResult result = await _verifier.VerifyAsync(row.Path, cancellationToken);

        if (row.HasExe    != result.HasExe
            || row.HasIni != result.HasIni)
        {
            row.HasExe = result.HasExe;
            row.HasIni = result.HasIni;

            row.ModifiedUtc = _clock.GetUtcNow()
                                    .UtcDateTime;

            await _installations.UpdateAsync(row, cancellationToken);
        }

        if (!result.HasExe)
        {
            return new LaunchGameResult(LaunchGameOutcome.Drifted, CloseAfterGameLaunch: false, FailureMessage: null);
        }

        string exePath = Path.Combine(row.Path, path2: "zoo.exe");

        ProcessLaunchResult launch = await _processLauncher.LaunchAsync(exePath, row.Path, cancellationToken);

        if (!launch.Started)
        {
            return new LaunchGameResult(LaunchGameOutcome.StartFailed, CloseAfterGameLaunch: false, launch.ErrorMessage);
        }

        LauncherSettings launcherSettings = await _settings.GetAsync(cancellationToken);

        try
        {
            row.LastPlayedUtc = _clock.GetUtcNow()
                                      .UtcDateTime;

            await _installations.UpdateAsync(row, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, message: "Process started but LastPlayedUtc update failed for {InstallationId}", row.Id);
        }

        return new LaunchGameResult(LaunchGameOutcome.Started, launcherSettings.CloseAfterGameLaunch, FailureMessage: null);
    }
}
