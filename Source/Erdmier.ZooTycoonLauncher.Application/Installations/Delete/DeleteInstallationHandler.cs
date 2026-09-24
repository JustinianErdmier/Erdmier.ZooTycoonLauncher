namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>Handler for <see cref="DeleteInstallationCommand" />.</summary>
public sealed class DeleteInstallationHandler : ICommandHandler<DeleteInstallationCommand, ErrorOr<DeleteInstallationResult>>
{
    private readonly IInstallationDbContextFactory _dbFactory;

    private readonly IApplicationEventPublisher _events;

    private readonly IInstallationRepository _installations;

    private readonly ILogger<DeleteInstallationHandler> _logger;

    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="settings">Launcher settings repository, updated when the removed installation was the default.</param>
    /// <param name="dbFactory">Per-installation database factory, used to best-effort delete the installation's database file.</param>
    /// <param name="logger">Logger for the best-effort database file delete failure.</param>
    /// <param name="events">Publishes installation-change messages after changes are persisted (SDD §7.2).</param>
    public DeleteInstallationHandler(IInstallationRepository            installations,
                                     ILauncherSettingsRepository        settings,
                                     IInstallationDbContextFactory      dbFactory,
                                     ILogger<DeleteInstallationHandler> logger,
                                     IApplicationEventPublisher         events)
    {
        _installations = installations;
        _settings      = settings;
        _dbFactory     = dbFactory;
        _logger        = logger;
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

        try
        {
            await _dbFactory.DeleteAsync(row.Id, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex,
                               "Could not delete the database file for installation {InstallationId}; it has been removed from the registry.",
                               row.Id);
        }

        _events.Publish(new InstallationDeletedMessage(row.Id));

        if (removedWasDefault)
        {
            _events.Publish(new DefaultInstallationChangedMessage(newDefaultId));
        }

        return new DeleteInstallationResult(removedWasDefault, newDefaultId);
    }
}
