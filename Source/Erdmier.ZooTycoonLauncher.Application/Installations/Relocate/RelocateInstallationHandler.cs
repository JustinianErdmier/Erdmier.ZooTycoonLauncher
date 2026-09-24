namespace Erdmier.ZooTycoonLauncher.Application.Installations.Relocate;

/// <summary>Handler for <see cref="RelocateInstallationCommand" />.</summary>
public sealed class RelocateInstallationHandler : ICommandHandler<RelocateInstallationCommand, ErrorOr<RelocateInstallationResult>>
{
    private readonly TimeProvider _clock;

    private readonly IApplicationEventPublisher _events;

    private readonly IInstallationRepository _installations;

    private readonly IInstallationVerifier _verifier;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="verifier">File-system verifier used to probe the new folder.</param>
    /// <param name="clock">Time provider for the <c>ModifiedUtc</c> stamp.</param>
    /// <param name="events">Publishes installation-change messages after changes are persisted (SDD §7.2).</param>
    public RelocateInstallationHandler(IInstallationRepository installations, IInstallationVerifier verifier, TimeProvider clock, IApplicationEventPublisher events)
    {
        _installations = installations;
        _verifier      = verifier;
        _clock         = clock;
        _events        = events;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<RelocateInstallationResult>> Handle(RelocateInstallationCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", $"No installation with id {command.InstallationId}.");
        }

        VerificationResult verification = await _verifier.VerifyAsync(command.NewPath, cancellationToken);

        if (!verification.DirectoryExists)
        {
            return Error.Validation(code: "Installation.PathMissing", $"The folder \"{command.NewPath}\" does not exist.");
        }

        if (!verification.HasExe)
        {
            // SDD §7.2.5: relocation exists to recover a missing zoo.exe — never move an installation to a folder that still lacks it.
            return Error.Validation(code: "Installation.ExeMissing", $"The folder \"{command.NewPath}\" does not contain zoo.exe.");
        }

        // GameInstallation.Path is init-only — model the relocation as remove + add with the same Id and AddedUtc.
        GameInstallation relocated = new()
        {
            Id       = row.Id,
            Name     = row.Name,
            Path     = command.NewPath,
            HasExe   = verification.HasExe,
            HasIni   = verification.HasIni,
            AddedUtc = row.AddedUtc,
            ModifiedUtc = _clock.GetUtcNow()
                                .UtcDateTime,
            LastPlayedUtc = row.LastPlayedUtc,
            LastOpenedUtc = row.LastOpenedUtc
        };

        await _installations.DeleteAsync(row.Id, cancellationToken);
        await _installations.AddAsync(relocated, cancellationToken);

        _events.Publish(new InstallationChangedMessage(row.Id));

        return new RelocateInstallationResult(verification.Validity);
    }
}
