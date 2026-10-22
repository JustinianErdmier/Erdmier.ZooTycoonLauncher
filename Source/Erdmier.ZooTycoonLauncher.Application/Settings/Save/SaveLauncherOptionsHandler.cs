namespace Erdmier.ZooTycoonLauncher.Application.Settings.Save;

/// <summary>Handler for <see cref="SaveLauncherOptionsCommand" />.</summary>
/// <remarks>A repository exception propagates to the caller, which reports it to the user.</remarks>
public sealed class SaveLauncherOptionsHandler : ICommandHandler<SaveLauncherOptionsCommand, ErrorOr<Success>>
{
    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="settings">The launcher settings repository.</param>
    public SaveLauncherOptionsHandler(ILauncherSettingsRepository settings) => _settings = settings;

    /// <inheritdoc />
    public async ValueTask<ErrorOr<Success>> Handle(SaveLauncherOptionsCommand command, CancellationToken cancellationToken)
    {
        LauncherSettings settings = await _settings.GetAsync(cancellationToken);

        LauncherStartupPreference oldPreference = settings.LauncherStartupPreference;
        bool                      oldClose      = settings.CloseAfterGameLaunch;

        settings.LauncherStartupPreference = command.StartupPreference;
        settings.CloseAfterGameLaunch      = command.CloseAfterGameLaunch;

        try
        {
            await _settings.UpdateAsync(settings, cancellationToken);
        }
        catch
        {
            // The tracked entity outlives this call, so a failed save must not leave the rejected values on it for the next read or save.
            settings.LauncherStartupPreference = oldPreference;
            settings.CloseAfterGameLaunch      = oldClose;

            throw;
        }

        return Result.Success;
    }
}
