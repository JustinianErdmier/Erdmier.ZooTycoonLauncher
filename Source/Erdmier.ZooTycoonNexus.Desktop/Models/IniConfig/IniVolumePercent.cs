namespace Erdmier.ZooTycoonNexus.Desktop.Models.IniConfig;

/// <summary>The one place a raw volume value becomes the whole percentage the Friendly style shows, so the slider and the read-only displays always agree (spec §6.3 and §6.4).</summary>
public static class IniVolumePercent
{
    /// <summary>The number of converted volume units that make up one percent.</summary>
    public const int UnitsPerPercent = 100;

    /// <summary>Converts a raw volume value to a percentage.</summary>
    /// <param name="conversion">The volume conversion of the key.</param>
    /// <param name="raw">The raw value as stored in the INI file.</param>
    /// <returns>The converted volume divided by <see cref="UnitsPerPercent" />, rounded away from zero and clamped to 0–100.</returns>
    public static int FromRaw(IniValueConversion conversion, int raw)
        => Math.Clamp((int)Math.Round(conversion.ToDisplay(raw) / (double)UnitsPerPercent, MidpointRounding.AwayFromZero), min: 0, max: 100);
}