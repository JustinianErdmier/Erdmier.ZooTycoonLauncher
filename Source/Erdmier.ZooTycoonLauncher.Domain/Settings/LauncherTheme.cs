namespace Erdmier.ZooTycoonLauncher.Domain.Settings;

/// <summary>
/// The launcher's theme choice (SDD §5.2, §9.11). Persisted on <see cref="LauncherSettings.Theme" />.
/// </summary>
public sealed class LauncherTheme : SmartEnum<LauncherTheme>
{
    /// <summary>Follow the operating system's light/dark preference at runtime.</summary>
    public static readonly LauncherTheme System = new("System", 1);

    /// <summary>Force the classic silver-on-navy Win95 palette regardless of OS preference.</summary>
    public static readonly LauncherTheme Light  = new("Light",  2);

    /// <summary>Force the dark-grey-on-navy palette regardless of OS preference.</summary>
    public static readonly LauncherTheme Dark   = new("Dark",   3);

    private LauncherTheme(string name, int id) : base(name, id) { }
}
