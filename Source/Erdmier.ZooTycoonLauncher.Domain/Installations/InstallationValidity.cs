namespace Erdmier.ZooTycoonLauncher.Domain.Installations;

/// <summary>
/// Classifies a <see cref="GameInstallation" /> by whether its required files are present on disk.
/// </summary>
/// <remarks>
/// Derived from <c>HasExe</c> and <c>HasIni</c> via <see cref="From" /> rather than stored, so the value
/// stays in sync with the row's current flags.
/// </remarks>
public sealed class InstallationValidity : SmartEnum<InstallationValidity>
{
    /// <summary>Both <c>zoo.exe</c> and <c>zoo.ini</c> are present; the installation is launchable.</summary>
    public static readonly InstallationValidity Valid             = new("Valid",             1, "Valid",                   "Green");

    /// <summary>The <c>zoo.exe</c> file is missing; the game cannot be launched.</summary>
    public static readonly InstallationValidity InvalidNoExe      = new("InvalidNoExe",      2, "Invalid — No EXE",         "Red");

    /// <summary>The <c>zoo.ini</c> file is missing; the launcher cannot configure or display INI state.</summary>
    public static readonly InstallationValidity InvalidNoIni      = new("InvalidNoIni",      3, "Invalid — No INI",         "Red");

    /// <summary>Both <c>zoo.exe</c> and <c>zoo.ini</c> are missing; the installation is wholly invalid.</summary>
    public static readonly InstallationValidity InvalidNoExeOrIni = new("InvalidNoExeOrIni", 4, "Invalid — No EXE or INI",  "Red");

    /// <summary>Display string shown in the Installation Manager grid.</summary>
    public string DisplayName { get; }

    /// <summary>Colour token consumed by the XAML row template (Green for Valid, Red for the others).</summary>
    public string ColourToken { get; }

    private InstallationValidity(string name, int id, string displayName, string colourToken)
        : base(name, id)
    {
        DisplayName = displayName;
        ColourToken = colourToken;
    }

    /// <summary>
    /// Maps the <c>(hasExe, hasIni)</c> flag pair to the corresponding validity.
    /// </summary>
    /// <param name="hasExe"><see langword="true" /> when <c>zoo.exe</c> is present on disk.</param>
    /// <param name="hasIni"><see langword="true" /> when <c>zoo.ini</c> is present on disk.</param>
    /// <returns>The validity for the supplied flag pair.</returns>
    public static InstallationValidity From(bool hasExe, bool hasIni) =>
        (hasExe, hasIni) switch
        {
            (true,  true)  => Valid,
            (false, true)  => InvalidNoExe,
            (true,  false) => InvalidNoIni,
            (false, false) => InvalidNoExeOrIni,
        };
}
