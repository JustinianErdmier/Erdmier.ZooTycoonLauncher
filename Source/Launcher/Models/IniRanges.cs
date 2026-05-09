namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

// ReSharper disable once GrammarMistakeInComment
/// <summary>
///     Single source of truth for every numeric range applied to <see cref="ZooIniModel" /> integer keys. Consumed by <see cref="ZooIniDefaults" /> for parser-side validation
///     and by the XAML <c> NumericUpDown </c> controls on the INI tab via <c> {x:Static models:IniRanges.&lt;Name&gt;} </c> for UI-side clamping. Some keys (e.g. screen dimensions)
///     are intentionally unbounded by <see cref="ZooIniDefaults" /> but still expose XAML-only defensive caps here.
/// </summary>
/// <remarks>
///     Public because Avalonia's compiled-binding pipeline needs reflective access to types referenced from XAML via <c> {x:Static}</c>. Constants are declared as
///     <see cref="decimal" /> because <c> NumericUpDown.Minimum </c> / <c> NumericUpDown.Maximum </c> are typed <see cref="decimal" /> and the compiled-XAML pipeline does not
///     implicitly convert <see cref="int" />. Parser-side <see cref="ZooIniDefaults" /> casts back to <see cref="int" /> at the call site — every value is well within
///     <see cref="int" /> range by construction, so the cast is lossless.
/// </remarks>
public static class IniRanges
{
    public const decimal CashIncrementMax = 1_000_000;

    public const decimal CashIncrementMin = 100;

    public const decimal DrawFpsPositionMax = 16384;

    /// <summary>Defensive XAML-only cap on FPS-overlay X position; the parser allows any non-negative.</summary>
    public const decimal DrawFpsPositionMin = 0;

    public const decimal DrawRateMax = 120;

    public const decimal DrawRateMin = 15;

    public const decimal HelpTypeMax = 2;

    public const decimal HelpTypeMin = 0;

    public const decimal KeyScrollSpeedMax = 200;

    public const decimal KeyScrollSpeedMin = 1;

    // ReSharper disable once UnusedMember.Global
    // The drop-down (Task 12) is the primary UI; these caps are defensive against future raw-field exposures.
    public const decimal LanguageIdMax = 65535;


    // ReSharper disable once UnusedMember.Global
    // The drop-down (Task 12) is the primary UI; these caps are defensive against future raw-field exposures.
    public const decimal LanguageIdMin = 0;

    public const decimal LevelMax = 4;

    /// <summary>Quality preset enum. 0 = Total Quality, 4 = Paused.</summary>
    public const decimal LevelMin = 0;

    public const decimal LogCutoffMax = 5;

    public const decimal LogCutoffMin = 0;

    public const decimal MapDimensionMax = 128;

    public const decimal MapDimensionMin = 1;

    public const decimal MaxCashMax = 10_000_000;

    public const decimal MaxCashMin = 0;

    public const decimal MaxGuestsMax = 10000;

    public const decimal MaxGuestsMin = 1;

    public const decimal MenuMusicAttenuationMax = 10000;

    public const decimal MenuMusicAttenuationMin = 0;

    public const decimal MinCashMax = 10_000_000;

    public const decimal MinCashMin = 0;

    public const decimal MinimumMessageIntervalMax = 3600;

    public const decimal MinimumMessageIntervalMin = 0;

    public const decimal MouseScrollDelayMax = 10;

    public const decimal MouseScrollDelayMin = 0;

    public const decimal MouseScrollSpeedMax = 200;

    public const decimal MouseScrollSpeedMin = 1;

    public const decimal MouseScrollThresholdMax = 50;

    public const decimal MouseScrollThresholdMin = 0;

    public const decimal MovieVolumeMax = 0;

    /// <summary>Movie volume in dB-style attenuation. <c> 0 </c> = full; <c> -10000 </c> = silent.</summary>
    public const decimal MovieVolumeMin = -10000;

    public const decimal ScreenHeightMax = 16384;

    public const decimal ScreenHeightMin = 1;

    /// <summary>Defensive XAML-only cap; the parser does not validate this.</summary>
    public const decimal ScreenWidthMax = 16384;

    /// <summary>Minimum horizontal resolution. <see cref="ZooIniDefaults" /> uses this as the parser-side floor.</summary>
    public const decimal ScreenWidthMin = 1;

    public const decimal StartingCashMax = 10_000_000;

    public const decimal StartingCashMin = 0;

    public const decimal TooltipDelayMax = 60;

    public const decimal TooltipDelayMin = 0;

    public const decimal TooltipDurationMax = 30000;

    public const decimal TooltipDurationMin = 0;

    public const decimal UpdateRateMax = 60;

    public const decimal UpdateRateMin = 1;

    public const decimal UserAttenuationMax = 10000;

    public const decimal UserAttenuationMin = 0;
}
