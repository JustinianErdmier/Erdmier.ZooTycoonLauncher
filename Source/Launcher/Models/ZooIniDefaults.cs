using System.Collections.Generic;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Single source of truth for every INI key the launcher understands. Maps each section + key to a typed property on <see cref="ZooIniModel" />.</summary>
internal static class ZooIniDefaults
{
    public static IReadOnlyList<IniKeySpec> KnownKeys { get; } =
    [
        // [user] — display, performance, and runtime state (SDD §9.1, §9.9)
        IniKeySpec.Bool(section: "user", key: "fullscreen", m => m.User.Fullscreen, (m,                       v) => m.User.Fullscreen = v),
        IniKeySpec.Int(section: "user", key: "screenwidth", m => m.User.ScreenWidth, (m,                      v) => m.User.ScreenWidth = v, min: 1),
        IniKeySpec.Int(section: "user", key: "screenheight", m => m.User.ScreenHeight, (m,                    v) => m.User.ScreenHeight = v, min: 1),
        IniKeySpec.Int(section: "user", key: "UpdateRate", m => m.User.UpdateRate, (m,                        v) => m.User.UpdateRate = v, min: 1, max: 60),
        IniKeySpec.Int(section: "user", key: "DrawRate", m => m.User.DrawRate, (m,                            v) => m.User.DrawRate = v, min: 15, max: 120),
        IniKeySpec.NullableStr(section: "user", key: "lastfile", m => m.User.LastFile, (m,                    v) => m.User.LastFile = v),
        IniKeySpec.Bool(section: "user", key: "showUserEntityWarning", m => m.User.ShowUserEntityWarning, (m, v) => m.User.ShowUserEntityWarning = v),

        // [UI] — audio (SDD §9.3)
        IniKeySpec.Bool(section: "UI", key: "noMenuMusic", m => m.UI.NoMenuMusic, (m,                  v) => m.UI.NoMenuMusic = v),
        IniKeySpec.Str(section: "UI", key: "menuMusic", m => m.UI.MenuMusic, (m,                       v) => m.UI.MenuMusic = v),
        IniKeySpec.Int(section: "UI", key: "menuMusicAttenuation", m => m.UI.MenuMusicAttenuation, (m, v) => m.UI.MenuMusicAttenuation = v, min: 0, max: 10000),
        IniKeySpec.Int(section: "UI", key: "userAttenuation", m => m.UI.UserAttenuation, (m,           v) => m.UI.UserAttenuation = v, min: 0, max: 10000),
        IniKeySpec.Bool(section: "UI", key: "playMovie", m => m.UI.PlayMovie, (m,                      v) => m.UI.PlayMovie = v),
        IniKeySpec.Int(section: "UI", key: "movievolume1", m => m.UI.MovieVolume1, (m,                 v) => m.UI.MovieVolume1 = v, min: -10000, max: 0),
        IniKeySpec.Bool(section: "UI", key: "playSecondMovie", m => m.UI.PlaySecondMovie, (m,          v) => m.UI.PlaySecondMovie = v),
        IniKeySpec.Int(section: "UI", key: "movievolume2", m => m.UI.MovieVolume2, (m,                 v) => m.UI.MovieVolume2 = v, min: -10000, max: 0),

        // [UI] — gameplay cash (SDD §9.4)
        IniKeySpec.Int(section: "UI", key: "MSStartingCash", m => m.UI.MSStartingCash, (m,   v) => m.UI.MSStartingCash = v, min: 0, max: 10_000_000),
        IniKeySpec.Int(section: "UI", key: "MSCashIncrement", m => m.UI.MSCashIncrement, (m, v) => m.UI.MSCashIncrement = v, min: 100, max: 1_000_000),
        IniKeySpec.Int(section: "UI", key: "MSMinCash", m => m.UI.MSMinCash, (m,             v) => m.UI.MSMinCash = v, min: 0, max: 10_000_000),
        IniKeySpec.Int(section: "UI", key: "MSMaxCash", m => m.UI.MSMaxCash, (m,             v) => m.UI.MSMaxCash = v, min: 0, max: 10_000_000),

        // [UI] — interface (SDD §9.5)
        IniKeySpec.Bool(section: "UI", key: "useAlternateCursors", m => m.UI.UseAlternateCursors, (m,      v) => m.UI.UseAlternateCursors = v),
        IniKeySpec.Int(section: "UI", key: "tooltipDelay", m => m.UI.TooltipDelay, (m,                     v) => m.UI.TooltipDelay = v, min: 0, max: 60),
        IniKeySpec.Int(section: "UI", key: "tooltipDuration", m => m.UI.TooltipDuration, (m,               v) => m.UI.TooltipDuration = v, min: 0, max: 30000),
        IniKeySpec.Bool(section: "UI", key: "MessageDisplay", m => m.UI.MessageDisplay, (m,                v) => m.UI.MessageDisplay = v),
        IniKeySpec.Int(section: "UI", key: "mouseScrollThreshold", m => m.UI.MouseScrollThreshold, (m,     v) => m.UI.MouseScrollThreshold = v, min: 0, max: 50),
        IniKeySpec.Int(section: "UI", key: "mouseScrollDelay", m => m.UI.MouseScrollDelay, (m,             v) => m.UI.MouseScrollDelay = v, min: 0, max: 10),
        IniKeySpec.Int(section: "UI", key: "mouseScrollX", m => m.UI.MouseScrollX, (m,                     v) => m.UI.MouseScrollX = v, min: 1, max: 200),
        IniKeySpec.Int(section: "UI", key: "mouseScrollY", m => m.UI.MouseScrollY, (m,                     v) => m.UI.MouseScrollY = v, min: 1, max: 200),
        IniKeySpec.Int(section: "UI", key: "keyScrollX", m => m.UI.KeyScrollX, (m,                         v) => m.UI.KeyScrollX = v, min: 1, max: 200),
        IniKeySpec.Int(section: "UI", key: "keyScrollY", m => m.UI.KeyScrollY, (m,                         v) => m.UI.KeyScrollY = v, min: 1, max: 200),
        IniKeySpec.Int(section: "UI", key: "minimumMessageInterval", m => m.UI.MinimumMessageInterval, (m, v) => m.UI.MinimumMessageInterval = v, min: 0, max: 3600),
        IniKeySpec.Int(section: "UI", key: "helpType", m => m.UI.HelpType, (m,                             v) => m.UI.HelpType = v, min: 0, max: 2),

        // [UI] — runtime state preserved on round-trip (SDD §9.9)
        IniKeySpec.NullableInt(section: "UI", key: "lastWindowX", m => m.UI.LastWindowX, (m,                                 v) => m.UI.LastWindowX = v),
        IniKeySpec.NullableInt(section: "UI", key: "lastWindowY", m => m.UI.LastWindowY, (m,                                 v) => m.UI.LastWindowY = v),
        IniKeySpec.Bool(section: "UI", key: "startedFirstTutorial", m => m.UI.StartedFirstTutorial, (m,                      v) => m.UI.StartedFirstTutorial = v),
        IniKeySpec.Bool(section: "UI", key: "startedDinoTutorial", m => m.UI.StartedDinoTutorial, (m,                        v) => m.UI.StartedDinoTutorial = v),
        IniKeySpec.Bool(section: "UI", key: "startedAquaTutorial", m => m.UI.StartedAquaTutorial, (m,                        v) => m.UI.StartedAquaTutorial = v),
        IniKeySpec.NullableInt(section: "UI", key: "progresscalls", m => m.UI.ProgressCalls, (m,                             v) => m.UI.ProgressCalls = v),
        IniKeySpec.NullableInt(section: "UI", key: "defaultEditCharLimit", m => m.UI.DefaultEditCharLimit, (m,               v) => m.UI.DefaultEditCharLimit = v),
        IniKeySpec.NullableInt(section: "UI", key: "completedExhibitAttenuation", m => m.UI.CompletedExhibitAttenuation, (m, v) => m.UI.CompletedExhibitAttenuation = v),

        // [advanced] — graphics & 8-bit audio (SDD §9.2, §9.3)
        IniKeySpec.Int(section: "advanced", key: "level", m => m.Advanced.Level, (m,                  v) => m.Advanced.Level = v, min: 0, max: 4),
        IniKeySpec.Bool(section: "advanced", key: "loadHalfAnims", m => m.Advanced.LoadHalfAnims, (m, v) => m.Advanced.LoadHalfAnims = v),
        IniKeySpec.Bool(section: "advanced", key: "drag", m => m.Advanced.Drag, (m,                   v) => m.Advanced.Drag = v),
        IniKeySpec.Bool(section: "advanced", key: "click", m => m.Advanced.Click, (m,                 v) => m.Advanced.Click = v),
        IniKeySpec.Bool(section: "advanced", key: "normal", m => m.Advanced.Normal, (m,               v) => m.Advanced.Normal = v),
        IniKeySpec.Bool(section: "advanced", key: "use8BitSound", m => m.Advanced.Use8BitSound, (m,   v) => m.Advanced.Use8BitSound = v),

        // [ai] (SDD §9.4)
        IniKeySpec.Int(section: "ai", key: "maxGuests", m => m.AI.MaxGuests, (m, v) => m.AI.MaxGuests = v, min: 1, max: 10000),

        // [debug] (SDD §9.8)
        IniKeySpec.Bool(section: "debug", key: "drawfps", m => m.Debug.DrawFps, (m,           v) => m.Debug.DrawFps = v),
        IniKeySpec.Int(section: "debug", key: "drawfpsx", m => m.Debug.DrawFpsX, (m,          v) => m.Debug.DrawFpsX = v, min: 0),
        IniKeySpec.Int(section: "debug", key: "drawfpsy", m => m.Debug.DrawFpsY, (m,          v) => m.Debug.DrawFpsY = v, min: 0),
        IniKeySpec.Int(section: "debug", key: "logCutoff", m => m.Debug.LogCutoff, (m,        v) => m.Debug.LogCutoff = v, min: 0, max: 5),
        IniKeySpec.Bool(section: "debug", key: "sendLogfile", m => m.Debug.SendLogfile, (m,   v) => m.Debug.SendLogfile = v),
        IniKeySpec.Bool(section: "debug", key: "sendDebugger", m => m.Debug.SendDebugger, (m, v) => m.Debug.SendDebugger = v),

        // [language] (SDD §9.7)
        IniKeySpec.Int(section: "language", key: "lang", m => m.Language.Lang, (m,       v) => m.Language.Lang = v),
        IniKeySpec.Int(section: "language", key: "sublang", m => m.Language.SubLang, (m, v) => m.Language.SubLang = v),

        // [Map] (SDD §9.6)
        IniKeySpec.Int(section: "Map", key: "mapX", m => m.Map.MapX, (m, v) => m.Map.MapX = v, min: 1, max: 128),
        IniKeySpec.Int(section: "Map", key: "mapY", m => m.Map.MapY, (m, v) => m.Map.MapY = v, min: 1, max: 128)
    ];
}
