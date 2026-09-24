namespace Erdmier.ZooTycoonLauncher.Application.Installations.Verify;

/// <summary>Handler for <see cref="VerifyInstallationQuery" />.</summary>
public sealed class VerifyInstallationHandler : IQueryHandler<VerifyInstallationQuery, ErrorOr<VerificationResult>>
{
    private readonly TimeProvider _clock;

    private readonly IApplicationEventPublisher _events;

    private readonly IInstallationRepository _installations;

    private readonly IInstallationVerifier _verifier;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="verifier">File-system verifier.</param>
    /// <param name="clock">Time provider for the <c>ModifiedUtc</c> stamp.</param>
    /// <param name="events">Publishes installation-change messages after changes are persisted (SDD §7.2).</param>
    public VerifyInstallationHandler(IInstallationRepository installations, IInstallationVerifier verifier, TimeProvider clock, IApplicationEventPublisher events)
    {
        _installations = installations;
        _verifier      = verifier;
        _clock         = clock;
        _events        = events;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<VerificationResult>> Handle(VerifyInstallationQuery query, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(query.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", $"No installation with id {query.InstallationId}.");
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

            _events.Publish(new InstallationChangedMessage(row.Id));
        }

        return result;
    }
}
