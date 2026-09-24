namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>Handler for <see cref="DeleteInstallationCommand" />.</summary>
public sealed class DeleteInstallationHandler : ICommandHandler<DeleteInstallationCommand, ErrorOr<DeleteInstallationResult>>
{
    private readonly IInstallationDbContextFactory _dbFactory;

    private readonly IApplicationEventPublisher _events;

    private readonly IInstallationRepository _installations;

    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="events">Publishes installation-change messages after changes are persisted (SDD §7.2).</param>
    public DeleteInstallationHandler(IInstallationRepository       installations,
                                     ILauncherSettingsRepository   settings,
                                     IInstallationDbContextFactory dbFactory,
                                     IApplicationEventPublisher    events)
    {
        _installations = installations;
        _settings      = settings;
        _dbFactory     = dbFactory;
        _events        = events;
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
            GameInstallation? promotion = await _installations.FindDefaultPromotionCandidateAsync(excludeId: null, cancellationToken);
            newDefaultId                   = promotion?.Id;
            settings.DefaultInstallationId = newDefaultId;

            await _settings.UpdateAsync(settings, cancellationToken);
        }

        await _dbFactory.DeleteAsync(row.Id, cancellationToken);

        _events.Publish(new InstallationDeletedMessage(row.Id));

        if (removedWasDefault)
        {
            _events.Publish(new DefaultInstallationChangedMessage(newDefaultId));
        }

        return new DeleteInstallationResult(removedWasDefault, newDefaultId);
    }
}
