namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Reads and writes the single <see cref="LauncherSettings" /> row.</summary>
public interface ILauncherSettingsRepository
{
    /// <summary>Returns the current settings, creating the single row with defaults if it does not yet exist.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current <see cref="LauncherSettings" /> row.</returns>
    Task<LauncherSettings> GetAsync(CancellationToken cancellationToken);

    /// <summary>Persists changes to the settings row.</summary>
    /// <param name="settings">The settings to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(LauncherSettings settings, CancellationToken cancellationToken);
}
