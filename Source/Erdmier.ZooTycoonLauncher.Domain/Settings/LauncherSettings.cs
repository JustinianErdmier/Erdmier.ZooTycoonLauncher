namespace Erdmier.ZooTycoonLauncher.Domain.Settings;

/// <summary>The single-row launcher settings table (SDD §5.1).</summary>
/// <remarks>Persisted with a <c>CHECK (ID = 1)</c> constraint to enforce singleton storage.</remarks>
public sealed class LauncherSettings
{
    /// <summary>When <see langword="true" />, the launcher closes after a successful Launch Game.</summary>
    public bool CloseAfterGameLaunch { get; set; }

    /// <summary>The id of the default installation (nullable when no installations are registered).</summary>
    public Guid? DefaultInstallationId { get; set; }

    /// <summary>Fixed primary key; always <c>1</c>.</summary>
    public int Id { get; init; } = 1;

    /// <summary>Startup-resolution preference; default is <see cref="LauncherStartupPreference.DefaultInstallation" />.</summary>
    public LauncherStartupPreference LauncherStartupPreference { get; set; } = LauncherStartupPreference.DefaultInstallation;

    /// <summary>The launcher's theme choice (SDD §9.11); default is <see cref="LauncherTheme.System" />.</summary>
    public LauncherTheme Theme { get; set; } = LauncherTheme.System;
}
