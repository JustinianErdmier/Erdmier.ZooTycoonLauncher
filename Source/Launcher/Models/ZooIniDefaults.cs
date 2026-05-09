using System.Collections.Generic;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Single source of truth for every INI key the launcher understands. Maps each section and key to a typed property on <see cref="ZooIniModel" />.</summary>
internal static class ZooIniDefaults
{
    public static IReadOnlyList<IniKeySpec> KnownKeys { get; } =
    [
        // [user] — display, performance and runtime state (SDD §9.1, §9.9)
        IniKeySpec.Bool(section: "user", key: "fullscreen", m => m.User.Fullscreen, (m, v) => m.User.Fullscreen = v),
        IniKeySpec.Int(section: "user", key: "screenwidth", m => m.User.ScreenWidth, (m, v) => m.User.ScreenWidth = v, (int)IniRanges.ScreenWidthMin),
        IniKeySpec.Int(section: "user", key: "screenheight", m => m.User.ScreenHeight, (m, v) => m.User.ScreenHeight = v, (int)IniRanges.ScreenHeightMin),
        IniKeySpec.Int(section: "user", key: "UpdateRate", m => m.User.UpdateRate, (m, v) => m.User.UpdateRate = v, (int)IniRanges.UpdateRateMin, (int)IniRanges.UpdateRateMax),
        IniKeySpec.Int(section: "user", key: "DrawRate", m => m.User.DrawRate, (m, v) => m.User.DrawRate = v, (int)IniRanges.DrawRateMin, (int)IniRanges.DrawRateMax),
        IniKeySpec.NullableStr(section: "user", key: "lastfile", m => m.User.LastFile, (m, v) => m.User.LastFile = v),
        IniKeySpec.Bool(section: "user", key: "showUserEntityWarning", m => m.User.ShowUserEntityWarning, (m, v) => m.User.ShowUserEntityWarning = v),

        // [UI] — audio (SDD §9.3)
        IniKeySpec.Bool(section: "UI", key: "noMenuMusic", m => m.Ui.NoMenuMusic, (m, v) => m.Ui.NoMenuMusic = v),
        IniKeySpec.Str(section: "UI", key: "menuMusic", m => m.Ui.MenuMusic, (m,      v) => m.Ui.MenuMusic = v),
        IniKeySpec.Int(section: "UI",
                       key: "menuMusicAttenuation",
                       m => m.Ui.MenuMusicAttenuation,
                       (m, v) => m.Ui.MenuMusicAttenuation = v,
                       (int)IniRanges.MenuMusicAttenuationMin,
                       (int)IniRanges.MenuMusicAttenuationMax),
        IniKeySpec.Int(section: "UI",
                       key: "userAttenuation",
                       m => m.Ui.UserAttenuation,
                       (m, v) => m.Ui.UserAttenuation = v,
                       (int)IniRanges.UserAttenuationMin,
                       (int)IniRanges.UserAttenuationMax),
        IniKeySpec.Bool(section: "UI", key: "playMovie", m => m.Ui.PlayMovie, (m, v) => m.Ui.PlayMovie = v),
        IniKeySpec.Int(section: "UI", key: "movievolume1", m => m.Ui.MovieVolume1, (m, v) => m.Ui.MovieVolume1 = v, (int)IniRanges.MovieVolumeMin, (int)IniRanges.MovieVolumeMax),
        IniKeySpec.Bool(section: "UI", key: "playSecondMovie", m => m.Ui.PlaySecondMovie, (m, v) => m.Ui.PlaySecondMovie = v),
        IniKeySpec.Int(section: "UI", key: "movievolume2", m => m.Ui.MovieVolume2, (m, v) => m.Ui.MovieVolume2 = v, (int)IniRanges.MovieVolumeMin, (int)IniRanges.MovieVolumeMax),

        // [UI] — gameplay cash (SDD §9.4)
        IniKeySpec.Int(section: "UI",
                       key: "MSStartingCash",
                       m => m.Ui.StartingCash,
                       (m, v) => m.Ui.StartingCash = v,
                       (int)IniRanges.StartingCashMin,
                       (int)IniRanges.StartingCashMax),
        IniKeySpec.Int(section: "UI",
                       key: "MSCashIncrement",
                       m => m.Ui.CashIncrement,
                       (m, v) => m.Ui.CashIncrement = v,
                       (int)IniRanges.CashIncrementMin,
                       (int)IniRanges.CashIncrementMax),
        IniKeySpec.Int(section: "UI", key: "MSMinCash", m => m.Ui.MinCash, (m, v) => m.Ui.MinCash = v, (int)IniRanges.MinCashMin, (int)IniRanges.MinCashMax),
        IniKeySpec.Int(section: "UI", key: "MSMaxCash", m => m.Ui.MaxCash, (m, v) => m.Ui.MaxCash = v, (int)IniRanges.MaxCashMin, (int)IniRanges.MaxCashMax),

        // [UI] — interface (SDD §9.5)
        IniKeySpec.Bool(section: "UI", key: "useAlternateCursors", m => m.Ui.UseAlternateCursors, (m, v) => m.Ui.UseAlternateCursors = v),
        IniKeySpec.Int(section: "UI", key: "tooltipDelay", m => m.Ui.TooltipDelay, (m, v) => m.Ui.TooltipDelay = v, (int)IniRanges.TooltipDelayMin, (int)IniRanges.TooltipDelayMax),
        IniKeySpec.Int(section: "UI",
                       key: "tooltipDuration",
                       m => m.Ui.TooltipDuration,
                       (m, v) => m.Ui.TooltipDuration = v,
                       (int)IniRanges.TooltipDurationMin,
                       (int)IniRanges.TooltipDurationMax),
        IniKeySpec.Bool(section: "UI", key: "MessageDisplay", m => m.Ui.MessageDisplay, (m, v) => m.Ui.MessageDisplay = v),
        IniKeySpec.Int(section: "UI",
                       key: "mouseScrollThreshold",
                       m => m.Ui.MouseScrollThreshold,
                       (m, v) => m.Ui.MouseScrollThreshold = v,
                       (int)IniRanges.MouseScrollThresholdMin,
                       (int)IniRanges.MouseScrollThresholdMax),
        IniKeySpec.Int(section: "UI",
                       key: "mouseScrollDelay",
                       m => m.Ui.MouseScrollDelay,
                       (m, v) => m.Ui.MouseScrollDelay = v,
                       (int)IniRanges.MouseScrollDelayMin,
                       (int)IniRanges.MouseScrollDelayMax),
        IniKeySpec.Int(section: "UI",
                       key: "mouseScrollX",
                       m => m.Ui.MouseScrollX,
                       (m, v) => m.Ui.MouseScrollX = v,
                       (int)IniRanges.MouseScrollSpeedMin,
                       (int)IniRanges.MouseScrollSpeedMax),
        IniKeySpec.Int(section: "UI",
                       key: "mouseScrollY",
                       m => m.Ui.MouseScrollY,
                       (m, v) => m.Ui.MouseScrollY = v,
                       (int)IniRanges.MouseScrollSpeedMin,
                       (int)IniRanges.MouseScrollSpeedMax),
        IniKeySpec.Int(section: "UI", key: "keyScrollX", m => m.Ui.KeyScrollX, (m, v) => m.Ui.KeyScrollX = v, (int)IniRanges.KeyScrollSpeedMin, (int)IniRanges.KeyScrollSpeedMax),
        IniKeySpec.Int(section: "UI", key: "keyScrollY", m => m.Ui.KeyScrollY, (m, v) => m.Ui.KeyScrollY = v, (int)IniRanges.KeyScrollSpeedMin, (int)IniRanges.KeyScrollSpeedMax),
        IniKeySpec.Int(section: "UI",
                       key: "minimumMessageInterval",
                       m => m.Ui.MinimumMessageInterval,
                       (m, v) => m.Ui.MinimumMessageInterval = v,
                       (int)IniRanges.MinimumMessageIntervalMin,
                       (int)IniRanges.MinimumMessageIntervalMax),
        IniKeySpec.Int(section: "UI", key: "helpType", m => m.Ui.HelpType, (m, v) => m.Ui.HelpType = v, (int)IniRanges.HelpTypeMin, (int)IniRanges.HelpTypeMax),

        // [UI] — runtime state preserved on round-trip (SDD §9.9)
        IniKeySpec.NullableInt(section: "UI", key: "lastWindowX", m => m.Ui.LastWindowX, (m,                                 v) => m.Ui.LastWindowX = v),
        IniKeySpec.NullableInt(section: "UI", key: "lastWindowY", m => m.Ui.LastWindowY, (m,                                 v) => m.Ui.LastWindowY = v),
        IniKeySpec.Bool(section: "UI", key: "startedFirstTutorial", m => m.Ui.StartedFirstTutorial, (m,                      v) => m.Ui.StartedFirstTutorial = v),
        IniKeySpec.Bool(section: "UI", key: "startedDinoTutorial", m => m.Ui.StartedDinoTutorial, (m,                        v) => m.Ui.StartedDinoTutorial = v),
        IniKeySpec.Bool(section: "UI", key: "startedAquaTutorial", m => m.Ui.StartedAquaTutorial, (m,                        v) => m.Ui.StartedAquaTutorial = v),
        IniKeySpec.NullableInt(section: "UI", key: "progresscalls", m => m.Ui.ProgressCalls, (m,                             v) => m.Ui.ProgressCalls = v),
        IniKeySpec.NullableInt(section: "UI", key: "defaultEditCharLimit", m => m.Ui.DefaultEditCharLimit, (m,               v) => m.Ui.DefaultEditCharLimit = v),
        IniKeySpec.NullableInt(section: "UI", key: "completedExhibitAttenuation", m => m.Ui.CompletedExhibitAttenuation, (m, v) => m.Ui.CompletedExhibitAttenuation = v),

        // [advanced] — graphics & 8-bit audio (SDD §9.2, §9.3)
        IniKeySpec.Int(section: "advanced", key: "level", m => m.Advanced.Level, (m,                  v) => m.Advanced.Level = v, (int)IniRanges.LevelMin, (int)IniRanges.LevelMax),
        IniKeySpec.Bool(section: "advanced", key: "loadHalfAnims", m => m.Advanced.LoadHalfAnims, (m, v) => m.Advanced.LoadHalfAnims = v),
        IniKeySpec.Bool(section: "advanced", key: "drag", m => m.Advanced.Drag, (m,                   v) => m.Advanced.Drag = v),
        IniKeySpec.Bool(section: "advanced", key: "click", m => m.Advanced.Click, (m,                 v) => m.Advanced.Click = v),
        IniKeySpec.Bool(section: "advanced", key: "normal", m => m.Advanced.Normal, (m,               v) => m.Advanced.Normal = v),
        IniKeySpec.Bool(section: "advanced", key: "use8BitSound", m => m.Advanced.Use8BitSound, (m,   v) => m.Advanced.Use8BitSound = v),

        // ReSharper disable once GrammarMistakeInComment
        // [ai] (SDD §9.4)
        IniKeySpec.Int(section: "ai", key: "maxGuests", m => m.Ai.MaxGuests, (m, v) => m.Ai.MaxGuests = v, (int)IniRanges.MaxGuestsMin, (int)IniRanges.MaxGuestsMax),

        // [debug] (SDD §9.8)
        IniKeySpec.Bool(section: "debug", key: "drawfps", m => m.Debug.DrawFps, (m, v) => m.Debug.DrawFps = v),
        IniKeySpec.Int(section: "debug", key: "drawfpsx", m => m.Debug.DrawFpsX, (m, v) => m.Debug.DrawFpsX = v, (int)IniRanges.DrawFpsPositionMin),
        IniKeySpec.Int(section: "debug", key: "drawfpsy", m => m.Debug.DrawFpsY, (m, v) => m.Debug.DrawFpsY = v, (int)IniRanges.DrawFpsPositionMin),
        IniKeySpec.Int(section: "debug", key: "logCutoff", m => m.Debug.LogCutoff, (m, v) => m.Debug.LogCutoff = v, (int)IniRanges.LogCutoffMin, (int)IniRanges.LogCutoffMax),
        IniKeySpec.Bool(section: "debug", key: "sendLogfile", m => m.Debug.SendLogfile, (m, v) => m.Debug.SendLogfile = v),
        IniKeySpec.Bool(section: "debug", key: "sendDebugger", m => m.Debug.SendDebugger, (m, v) => m.Debug.SendDebugger = v),

        // [language] (SDD §9.7)
        // Deliberately no min/max: the SDD treats LANGID values as opaque, and the parser shouldn't reject a valid Windows
        // LANGID just because of a defensive cap. Defensive XAML caps live in IniRanges for any future raw-field re-exposure.
        IniKeySpec.Int(section: "language", key: "lang", m => m.Language.Lang, (m,       v) => m.Language.Lang = v),
        IniKeySpec.Int(section: "language", key: "sublang", m => m.Language.SubLang, (m, v) => m.Language.SubLang = v),

        // [Map] (SDD §9.6)
        IniKeySpec.Int(section: "Map", key: "mapX", m => m.Map.MapX, (m, v) => m.Map.MapX = v, (int)IniRanges.MapDimensionMin, (int)IniRanges.MapDimensionMax),
        IniKeySpec.Int(section: "Map", key: "mapY", m => m.Map.MapY, (m, v) => m.Map.MapY = v, (int)IniRanges.MapDimensionMin, (int)IniRanges.MapDimensionMax)
    ];
}
