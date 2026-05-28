namespace Erdmier.ZooTycoonLauncher.Application.Installations.Add;

/// <summary>Handler for <see cref="AddInstallationCommand" />. Implements SDD §7.2.1 steps 1-5 with deferred snapshot capture.</summary>
public sealed class AddInstallationHandler : ICommandHandler<AddInstallationCommand, ErrorOr<AddInstallationResult>>
{
    private readonly IInstallationRepository _installations;
    private readonly ILauncherSettingsRepository _settings;
    private readonly IInstallationVerifier _verifier;
    private readonly IInstallationDbContextFactory _dbFactory;
    private readonly IIniSnapshotService _snapshots;
    private readonly TimeProvider _clock;

    /// <summary>Initialises a new instance.</summary>
    public AddInstallationHandler(
        IInstallationRepository installations,
        ILauncherSettingsRepository settings,
        IInstallationVerifier verifier,
        IInstallationDbContextFactory dbFactory,
        IIniSnapshotService snapshots,
        TimeProvider clock)
    {
        _installations = installations;
        _settings = settings;
        _verifier = verifier;
        _dbFactory = dbFactory;
        _snapshots = snapshots;
        _clock = clock;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<AddInstallationResult>> Handle(AddInstallationCommand command, CancellationToken cancellationToken)
    {
        string trimmedName = command.Name.Trim();

        VerificationResult verification = await _verifier.VerifyAsync(command.Path, cancellationToken);

        if (!verification.DirectoryExists)
        {
            return Error.Validation(code: "Installation.PathMissing", description: $"The folder \"{command.Path}\" does not exist.");
        }

        IReadOnlyList<GameInstallation> existing = await _installations.GetAllAsync(cancellationToken);
        bool isFirst = existing.Count == 0;
        bool becameDefault = command.MakeDefault || isFirst;

        GameInstallation row = new()
        {
            Id = Guid.CreateVersion7(),
            Name = trimmedName,
            Path = command.Path,
            HasExe = verification.HasExe,
            HasIni = verification.HasIni,
            AddedUtc = _clock.GetUtcNow().UtcDateTime,
        };

        await _installations.AddAsync(row, cancellationToken);

        if (becameDefault)
        {
            LauncherSettings settings = await _settings.GetAsync(cancellationToken);
            settings.DefaultInstallationId = row.Id;

            await _settings.UpdateAsync(settings, cancellationToken);
        }

        // Provision the per-installation database — the file is created and migrations applied here so the INI slice can drop
        // straight in without retrofitting.
        await using (IInstallationDbContextHandle handle = await _dbFactory.CreateAsync(row.Id, cancellationToken))
        {
            // Handle disposed immediately — we just need the DB file on disk with schema applied.
        }

        ErrorOr<Success> snapshotResult = await _snapshots.CaptureOriginalAsync(row, cancellationToken);

        if (snapshotResult.IsError)
        {
            // The installation is persisted; snapshot failure is non-fatal here. The INI Config slice's real service will treat
            // snapshot failure as a transition into the CorruptedIni state rather than an outright error. Infrastructure
            // logging happens inside NullIniSnapshotService / the real service, not here.
            _ = snapshotResult; // Discard: failure surfaced to caller via SnapshotFailed flag if needed in future.
        }

        return new AddInstallationResult(row.Id, verification.Validity, becameDefault);
    }
}
