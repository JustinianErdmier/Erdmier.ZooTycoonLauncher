namespace Erdmier.ZooTycoonLauncher.Application.Installations.Update;

/// <summary>Handler for <see cref="UpdateInstallationCommand" />.</summary>
public sealed class UpdateInstallationHandler : ICommandHandler<UpdateInstallationCommand, ErrorOr<Success>>
{
    private readonly TimeProvider _clock;

    private readonly IApplicationEventPublisher _events;

    private readonly IInstallationRepository _installations;

    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="events">Publishes installation-change messages after changes are persisted (SDD §7.2).</param>
    public UpdateInstallationHandler(IInstallationRepository installations, ILauncherSettingsRepository settings, TimeProvider clock, IApplicationEventPublisher events)
    {
        _installations = installations;
        _settings      = settings;
        _clock         = clock;
        _events        = events;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<Success>> Handle(UpdateInstallationCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", $"No installation with id {command.InstallationId}.");
        }

        row.Name = command.Name.Trim();

        row.ModifiedUtc = _clock.GetUtcNow()
                                .UtcDateTime;

        await _installations.UpdateAsync(row, cancellationToken);

        if (command.MakeDefault)
        {
            LauncherSettings settings = await _settings.GetAsync(cancellationToken);

            if (settings.DefaultInstallationId != row.Id)
            {
                settings.DefaultInstallationId = row.Id;

                await _settings.UpdateAsync(settings, cancellationToken);

                _events.Publish(new DefaultInstallationChangedMessage(row.Id));
            }
        }

        _events.Publish(new InstallationChangedMessage(row.Id));

        return Result.Success;
    }
}
