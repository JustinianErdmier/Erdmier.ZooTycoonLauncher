namespace Erdmier.ZooTycoonLauncher.Application.Boot;

/// <summary>Handler for <see cref="BootCommand" />. Implements the SDD §7.1.1 startup state machine.</summary>
public sealed class BootHandler : ICommandHandler<BootCommand, ErrorOr<BootResult>>
{
    private readonly TimeProvider _clock;

    private readonly IIniSnapshotService _iniSnapshots;

    private readonly IInstallationRepository _installations;

    private readonly IInstallationLocator _locator;

    private readonly ILauncherSettingsRepository _settings;

    private readonly IInstallationVerifier _verifier;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="settings">Launcher settings repository.</param>
    /// <param name="installations">Installation repository.</param>
    /// <param name="verifier">File-system verifier.</param>
    /// <param name="locator">Registry and path locator.</param>
    /// <param name="iniSnapshots">INI snapshot service (stub until the INI Config slice).</param>
    /// <param name="clock">Time provider for UTC timestamps.</param>
    public BootHandler(ILauncherSettingsRepository settings,
                       IInstallationRepository     installations,
                       IInstallationVerifier       verifier,
                       IInstallationLocator        locator,
                       IIniSnapshotService         iniSnapshots,
                       TimeProvider                clock)
    {
        _settings      = settings;
        _installations = installations;
        _verifier      = verifier;
        _locator       = locator;
        _iniSnapshots  = iniSnapshots;
        _clock         = clock;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<BootResult>> Handle(BootCommand command, CancellationToken cancellationToken)
    {
        LauncherSettings settings = await _settings.GetAsync(cancellationToken);

        if (settings.LauncherStartupPreference == LauncherStartupPreference.NoInstallation)
        {
            return new BootResult(BootOutcome.OpenGameInstallation, ActiveInstallation: null, LocatedCandidatePath: null);
        }

        if (settings.LauncherStartupPreference    == LauncherStartupPreference.LastPlayedInstallation
            || settings.LauncherStartupPreference == LauncherStartupPreference.LastOpenedInstallation)
        {
            bool useLastPlayed = settings.LauncherStartupPreference == LauncherStartupPreference.LastPlayedInstallation;

            IReadOnlyList<GameInstallation> all = await _installations.GetAllAsync(cancellationToken);

            GameInstallation? candidate = useLastPlayed
                                              ? all.OrderByDescending(r => r.LastPlayedUtc)
                                                   .FirstOrDefault(r => r.LastPlayedUtc is not null)
                                              : all.OrderByDescending(r => r.LastOpenedUtc)
                                                   .FirstOrDefault(r => r.LastOpenedUtc is not null);

            if (candidate is not null)
            {
                return await VerifyAsync(candidate, settings, cancellationToken);
            }
        }

        return await ResolveDefaultAsync(settings, cancellationToken);
    }

    private async ValueTask<ErrorOr<BootResult>> ResolveDefaultAsync(LauncherSettings settings, CancellationToken cancellationToken)
    {
        if (settings.DefaultInstallationId is not null)
        {
            GameInstallation? row = await _installations.GetByIdAsync(settings.DefaultInstallationId.Value, cancellationToken);

            if (row is not null)
            {
                return await VerifyAsync(row, settings, cancellationToken);
            }
        }

        // Either no default was ever set, or the persisted default no longer resolves to a stored installation
        // (e.g. the row was deleted out from under a stale settings pointer). In both cases attempt to promote
        // another installation as default, falling back to auto-location when none exists.
        GameInstallation? promoted = await _installations.FindDefaultPromotionCandidateAsync(cancellationToken);

        if (promoted is null)
        {
            // A stale default cannot be promoted away, so clear the dangling pointer rather than leave it
            // referencing a row that no longer exists. When no default was ever set this value is already null,
            // so the write is skipped. The stale value is still present here because it is only ever read above.
            if (settings.DefaultInstallationId is not null)
            {
                settings.DefaultInstallationId = null;

                await _settings.UpdateAsync(settings, cancellationToken);
            }

            return await AutoLocateAsync(cancellationToken);
        }

        settings.DefaultInstallationId = promoted.Id;

        await _settings.UpdateAsync(settings, cancellationToken);

        return await VerifyAsync(promoted, settings, cancellationToken);
    }

    private async ValueTask<BootResult> AutoLocateAsync(CancellationToken cancellationToken)
    {
        LocatedDirectory located = await _locator.LocateAsync(persistedLastKnownPath: null, cancellationToken);

        return new BootResult(BootOutcome.NoGameInstallationFound, ActiveInstallation: null, located.Path);
    }

    private async ValueTask<ErrorOr<BootResult>> VerifyAsync(GameInstallation row, LauncherSettings settings, CancellationToken cancellationToken)
    {
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
            return new BootResult(BootOutcome.CannotPlay, Project(row, settings), LocatedCandidatePath: null);
        }

        ErrorOr<Success> syncResult = await _iniSnapshots.SynchroniseAsync(row, cancellationToken);

        if (syncResult.IsError)
        {
            return new BootResult(BootOutcome.CannotPlay, Project(row, settings), LocatedCandidatePath: null);
        }

        row.LastOpenedUtc = _clock.GetUtcNow()
                                  .UtcDateTime;

        await _installations.UpdateAsync(row, cancellationToken);

        return new BootResult(BootOutcome.ReadyToPlay, Project(row, settings), LocatedCandidatePath: null);
    }

    private static InstallationSummary Project(GameInstallation row, LauncherSettings settings)
        => new(row.Id,
               row.Name,
               row.Path,
               row.Validity,
               settings.DefaultInstallationId == row.Id,
               row.AddedUtc,
               row.ModifiedUtc,
               row.LastPlayedUtc,
               row.LastOpenedUtc);
}
