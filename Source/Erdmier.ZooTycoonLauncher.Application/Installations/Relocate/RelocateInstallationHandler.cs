namespace Erdmier.ZooTycoonLauncher.Application.Installations.Relocate;

/// <summary>Handler for <see cref="RelocateInstallationCommand" />.</summary>
public sealed class RelocateInstallationHandler : ICommandHandler<RelocateInstallationCommand, ErrorOr<RelocateInstallationResult>>
{
    private readonly TimeProvider _clock;

    private readonly IInstallationRepository _installations;

    private readonly IInstallationVerifier _verifier;

    /// <summary>Initialises a new instance.</summary>
    public RelocateInstallationHandler(IInstallationRepository installations, IInstallationVerifier verifier, TimeProvider clock)
    {
        _installations = installations;
        _verifier      = verifier;
        _clock         = clock;
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

        return new RelocateInstallationResult(verification.Validity);
    }
}
