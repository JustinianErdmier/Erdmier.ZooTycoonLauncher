namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>Handler for <see cref="DeleteInstallationCommand" />.</summary>
public sealed class DeleteInstallationHandler : ICommandHandler<DeleteInstallationCommand, ErrorOr<DeleteInstallationResult>>
{
    private readonly IInstallationDbContextFactory _dbFactory;

    private readonly IInstallationRepository _installations;

    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    public DeleteInstallationHandler(IInstallationRepository installations, ILauncherSettingsRepository settings, IInstallationDbContextFactory dbFactory)
    {
        _installations = installations;
        _settings      = settings;
        _dbFactory     = dbFactory;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<DeleteInstallationResult>> Handle(DeleteInstallationCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", $"No installation with id {command.InstallationId}.");
        }

        LauncherSettings settings          = await _settings.GetAsync(cancellationToken);
        bool             removedWasDefault = settings.DefaultInstallationId == row.Id;

        await _installations.DeleteAsync(row.Id, cancellationToken);

        Guid? newDefaultId = null;

        if (removedWasDefault)
        {
            GameInstallation? promotion = await _installations.FindDefaultPromotionCandidateAsync(cancellationToken);
            newDefaultId                   = promotion?.Id;
            settings.DefaultInstallationId = newDefaultId;

            await _settings.UpdateAsync(settings, cancellationToken);
        }

        await _dbFactory.DeleteAsync(row.Id, cancellationToken);

        return new DeleteInstallationResult(removedWasDefault, newDefaultId);
    }
}
