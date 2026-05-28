namespace Erdmier.ZooTycoonLauncher.Application.Installations.SetDefault;

/// <summary>Handler for <see cref="SetDefaultInstallationCommand" />.</summary>
public sealed class SetDefaultInstallationHandler : ICommandHandler<SetDefaultInstallationCommand, ErrorOr<Success>>
{
    private readonly IInstallationRepository _installations;
    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    public SetDefaultInstallationHandler(IInstallationRepository installations, ILauncherSettingsRepository settings)
    {
        _installations = installations;
        _settings = settings;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<Success>> Handle(SetDefaultInstallationCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", description: $"No installation with id {command.InstallationId}.");
        }

        LauncherSettings settings = await _settings.GetAsync(cancellationToken);

        if (settings.DefaultInstallationId == row.Id)
        {
            return Result.Success;
        }

        settings.DefaultInstallationId = row.Id;
        await _settings.UpdateAsync(settings, cancellationToken);

        return Result.Success;
    }
}
