namespace Erdmier.ZooTycoonLauncher.Application.Installations.Update;

/// <summary>Handler for <see cref="UpdateInstallationCommand" />.</summary>
public sealed class UpdateInstallationHandler : ICommandHandler<UpdateInstallationCommand, ErrorOr<Success>>
{
    private readonly TimeProvider _clock;

    private readonly IInstallationRepository _installations;

    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    public UpdateInstallationHandler(IInstallationRepository installations, ILauncherSettingsRepository settings, TimeProvider clock)
    {
        _installations = installations;
        _settings      = settings;
        _clock         = clock;
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
            }
        }

        return Result.Success;
    }
}
