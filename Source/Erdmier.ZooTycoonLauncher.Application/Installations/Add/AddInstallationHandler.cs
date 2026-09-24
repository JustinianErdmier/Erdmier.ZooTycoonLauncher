namespace Erdmier.ZooTycoonLauncher.Application.Installations.Add;

/// <summary>Handler for <see cref="AddInstallationCommand" />. Implements SDD §7.2.1 steps 1-5 with deferred snapshot capture.</summary>
public sealed class AddInstallationHandler : ICommandHandler<AddInstallationCommand, ErrorOr<AddInstallationResult>>
{
    private readonly TimeProvider _clock;

    private readonly IInstallationDbContextFactory _dbFactory;

    private readonly IApplicationEventPublisher _events;

    private readonly IInstallationRepository _installations;

    private readonly ILauncherSettingsRepository _settings;

    private readonly IIniSnapshotService _snapshots;

    private readonly IInstallationVerifier _verifier;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="settings">Launcher settings repository, updated when the new installation becomes the default.</param>
    /// <param name="verifier">File-system verifier used to probe the candidate folder.</param>
    /// <param name="dbFactory">Per-installation database factory, used to provision the new installation's database.</param>
    /// <param name="snapshots">INI snapshot service, used to capture the <c>Original</c> snapshot.</param>
    /// <param name="clock">Time provider for the <c>AddedUtc</c> stamp.</param>
    /// <param name="events">Publishes installation-change messages after changes are persisted (SDD §7.2).</param>
    public AddInstallationHandler(IInstallationRepository       installations,
                                  ILauncherSettingsRepository   settings,
                                  IInstallationVerifier         verifier,
                                  IInstallationDbContextFactory dbFactory,
                                  IIniSnapshotService           snapshots,
                                  TimeProvider                  clock,
                                  IApplicationEventPublisher    events)
    {
        _installations = installations;
        _settings      = settings;
        _verifier      = verifier;
        _dbFactory     = dbFactory;
        _snapshots     = snapshots;
        _clock         = clock;
        _events        = events;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<AddInstallationResult>> Handle(AddInstallationCommand command, CancellationToken cancellationToken)
    {
        string trimmedName = command.Name.Trim();

        VerificationResult verification = await _verifier.VerifyAsync(command.Path, cancellationToken);

        if (!verification.DirectoryExists)
        {
            return Error.Validation(code: "Installation.PathMissing", $"The folder \"{command.Path}\" does not exist.");
        }

        IReadOnlyList<GameInstallation> existing      = await _installations.GetAllAsync(cancellationToken);
        bool                            isFirst       = existing.Count == 0;
        bool                            becameDefault = command.MakeDefault || isFirst;

        GameInstallation row = new()
        {
            Id     = Guid.CreateVersion7(),
            Name   = trimmedName,
            Path   = command.Path,
            HasExe = verification.HasExe,
            HasIni = verification.HasIni,
            AddedUtc = _clock.GetUtcNow()
                             .UtcDateTime
        };

        await _installations.AddAsync(row, cancellationToken);

        if (becameDefault)
        {
            LauncherSettings settings = await _settings.GetAsync(cancellationToken);
            settings.DefaultInstallationId = row.Id;

            await _settings.UpdateAsync(settings, cancellationToken);
        }

        try
        {
            // Provision the per-installation database — the file is created and migrations applied here so the INI slice can drop
            // straight in without retrofitting.
            await using (IInstallationDbContextHandle handle = await _dbFactory.CreateAsync(row.Id, cancellationToken))
            {
                // Handle disposed immediately — we just need the DB file on disk with schema applied.
            }

            ErrorOr<Success> snapshotResult = await _snapshots.CaptureOriginalAsync(row, cancellationToken);

            if (snapshotResult.IsError)
            {
                // The installation is persisted; a capture failure is non-fatal. Its database stays empty, and the next synchronise (at boot or when the INI tab opens) retries
                // the first import. IniSnapshotService logs read and store failures itself; a missing zoo.ini produces no log there — it is simply recorded as HasIni = false.
                _ = snapshotResult; // Discard: failure surfaced to caller via SnapshotFailed flag if needed in future.
            }
        }
        finally
        {
            // The row (and default setting) is already persisted, so publish even when provisioning above throws — the
            // exception still propagates once the finally block completes.
            _events.Publish(new InstallationAddedMessage(row.Id));

            if (becameDefault)
            {
                _events.Publish(new DefaultInstallationChangedMessage(row.Id));
            }
        }

        return new AddInstallationResult(row.Id, verification.Validity, becameDefault);
    }
}
