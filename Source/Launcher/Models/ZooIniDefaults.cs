using System.Collections.Generic;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Single source of truth for every INI key the launcher understands. Maps each section + key to a typed property on <see cref="ZooIniModel" />.</summary>
internal static class ZooIniDefaults
{
    public static IReadOnlyList<IniKeySpec> KnownKeys { get; } =
    [
        // [user] — display, performance, and runtime state (SDD §9.1, §9.9)
        IniKeySpec.Bool("user", "fullscreen",            m => m.User.Fullscreen,            (m, v) => m.User.Fullscreen = v),
        IniKeySpec.Int ("user", "screenwidth",           m => m.User.ScreenWidth,           (m, v) => m.User.ScreenWidth = v, min: 1),
        IniKeySpec.Int ("user", "screenheight",          m => m.User.ScreenHeight,          (m, v) => m.User.ScreenHeight = v, min: 1),
        IniKeySpec.Int ("user", "UpdateRate",            m => m.User.UpdateRate,            (m, v) => m.User.UpdateRate = v, min: 1, max: 60),
        IniKeySpec.Int ("user", "DrawRate",              m => m.User.DrawRate,              (m, v) => m.User.DrawRate = v, min: 15, max: 120),
        IniKeySpec.NullableStr("user", "lastfile",       m => m.User.LastFile,              (m, v) => m.User.LastFile = v),
        IniKeySpec.Bool("user", "showUserEntityWarning", m => m.User.ShowUserEntityWarning, (m, v) => m.User.ShowUserEntityWarning = v),

        // [UI] — audio (SDD §9.3)
        IniKeySpec.Bool("UI", "noMenuMusic",           m => m.UI.NoMenuMusic,          (m, v) => m.UI.NoMenuMusic = v),
        IniKeySpec.Str ("UI", "menuMusic",             m => m.UI.MenuMusic,            (m, v) => m.UI.MenuMusic = v),
        IniKeySpec.Int ("UI", "menuMusicAttenuation",  m => m.UI.MenuMusicAttenuation, (m, v) => m.UI.MenuMusicAttenuation = v, min: 0, max: 10000),
        IniKeySpec.Int ("UI", "userAttenuation",       m => m.UI.UserAttenuation,      (m, v) => m.UI.UserAttenuation = v, min: 0, max: 10000),
        IniKeySpec.Bool("UI", "playMovie",             m => m.UI.PlayMovie,            (m, v) => m.UI.PlayMovie = v),
        IniKeySpec.Int ("UI", "movievolume1",          m => m.UI.MovieVolume1,         (m, v) => m.UI.MovieVolume1 = v, min: -10000, max: 0),
        IniKeySpec.Bool("UI", "playSecondMovie",       m => m.UI.PlaySecondMovie,      (m, v) => m.UI.PlaySecondMovie = v),
        IniKeySpec.Int ("UI", "movievolume2",          m => m.UI.MovieVolume2,         (m, v) => m.UI.MovieVolume2 = v, min: -10000, max: 0),

        // [UI] — gameplay cash (SDD §9.4)
        IniKeySpec.Int("UI", "MSStartingCash",  m => m.UI.MSStartingCash,  (m, v) => m.UI.MSStartingCash = v, min: 0, max: 10_000_000),
        IniKeySpec.Int("UI", "MSCashIncrement", m => m.UI.MSCashIncrement, (m, v) => m.UI.MSCashIncrement = v, min: 100, max: 1_000_000),
        IniKeySpec.Int("UI", "MSMinCash",       m => m.UI.MSMinCash,       (m, v) => m.UI.MSMinCash = v, min: 0, max: 10_000_000),
        IniKeySpec.Int("UI", "MSMaxCash",       m => m.UI.MSMaxCash,       (m, v) => m.UI.MSMaxCash = v, min: 0, max: 10_000_000),

        // [UI] — interface (SDD §9.5)
        IniKeySpec.Bool("UI", "useAlternateCursors",   m => m.UI.UseAlternateCursors,   (m, v) => m.UI.UseAlternateCursors = v),
        IniKeySpec.Int ("UI", "tooltipDelay",          m => m.UI.TooltipDelay,          (m, v) => m.UI.TooltipDelay = v, min: 0, max: 60),
        IniKeySpec.Int ("UI", "tooltipDuration",       m => m.UI.TooltipDuration,       (m, v) => m.UI.TooltipDuration = v, min: 0, max: 30000),
        IniKeySpec.Bool("UI", "MessageDisplay",        m => m.UI.MessageDisplay,        (m, v) => m.UI.MessageDisplay = v),
        IniKeySpec.Int ("UI", "mouseScrollThreshold",  m => m.UI.MouseScrollThreshold,  (m, v) => m.UI.MouseScrollThreshold = v, min: 0, max: 50),
        IniKeySpec.Int ("UI", "mouseScrollDelay",      m => m.UI.MouseScrollDelay,      (m, v) => m.UI.MouseScrollDelay = v, min: 0, max: 10),
        IniKeySpec.Int ("UI", "mouseScrollX",          m => m.UI.MouseScrollX,          (m, v) => m.UI.MouseScrollX = v, min: 1, max: 200),
        IniKeySpec.Int ("UI", "mouseScrollY",          m => m.UI.MouseScrollY,          (m, v) => m.UI.MouseScrollY = v, min: 1, max: 200),
        IniKeySpec.Int ("UI", "keyScrollX",            m => m.UI.KeyScrollX,            (m, v) => m.UI.KeyScrollX = v, min: 1, max: 200),
        IniKeySpec.Int ("UI", "keyScrollY",            m => m.UI.KeyScrollY,            (m, v) => m.UI.KeyScrollY = v, min: 1, max: 200),
        IniKeySpec.Int ("UI", "minimumMessageInterval",m => m.UI.MinimumMessageInterval,(m, v) => m.UI.MinimumMessageInterval = v, min: 0, max: 3600),
        IniKeySpec.Int ("UI", "helpType",              m => m.UI.HelpType,              (m, v) => m.UI.HelpType = v, min: 0, max: 2),

        // [UI] — runtime state preserved on round-trip (SDD §9.9)
        IniKeySpec.NullableInt("UI", "lastWindowX",                m => m.UI.LastWindowX,                  (m, v) => m.UI.LastWindowX = v),
        IniKeySpec.NullableInt("UI", "lastWindowY",                m => m.UI.LastWindowY,                  (m, v) => m.UI.LastWindowY = v),
        IniKeySpec.Bool       ("UI", "startedFirstTutorial",       m => m.UI.StartedFirstTutorial,         (m, v) => m.UI.StartedFirstTutorial = v),
        IniKeySpec.Bool       ("UI", "startedDinoTutorial",        m => m.UI.StartedDinoTutorial,          (m, v) => m.UI.StartedDinoTutorial = v),
        IniKeySpec.Bool       ("UI", "startedAquaTutorial",        m => m.UI.StartedAquaTutorial,          (m, v) => m.UI.StartedAquaTutorial = v),
        IniKeySpec.NullableInt("UI", "progresscalls",              m => m.UI.ProgressCalls,                (m, v) => m.UI.ProgressCalls = v),
        IniKeySpec.NullableInt("UI", "defaultEditCharLimit",       m => m.UI.DefaultEditCharLimit,         (m, v) => m.UI.DefaultEditCharLimit = v),
        IniKeySpec.NullableInt("UI", "completedExhibitAttenuation",m => m.UI.CompletedExhibitAttenuation,  (m, v) => m.UI.CompletedExhibitAttenuation = v),

        // [advanced] — graphics & 8-bit audio (SDD §9.2, §9.3)
        IniKeySpec.Int ("advanced", "level",         m => m.Advanced.Level,         (m, v) => m.Advanced.Level = v, min: 0, max: 4),
        IniKeySpec.Bool("advanced", "loadHalfAnims", m => m.Advanced.LoadHalfAnims, (m, v) => m.Advanced.LoadHalfAnims = v),
        IniKeySpec.Bool("advanced", "drag",          m => m.Advanced.Drag,          (m, v) => m.Advanced.Drag = v),
        IniKeySpec.Bool("advanced", "click",         m => m.Advanced.Click,         (m, v) => m.Advanced.Click = v),
        IniKeySpec.Bool("advanced", "normal",        m => m.Advanced.Normal,        (m, v) => m.Advanced.Normal = v),
        IniKeySpec.Bool("advanced", "use8BitSound",  m => m.Advanced.Use8BitSound,  (m, v) => m.Advanced.Use8BitSound = v),

        // [ai] (SDD §9.4)
        IniKeySpec.Int("ai", "maxGuests", m => m.AI.MaxGuests, (m, v) => m.AI.MaxGuests = v, min: 1, max: 10000),

        // [debug] (SDD §9.8)
        IniKeySpec.Bool("debug", "drawfps",      m => m.Debug.DrawFps,      (m, v) => m.Debug.DrawFps = v),
        IniKeySpec.Int ("debug", "drawfpsx",     m => m.Debug.DrawFpsX,     (m, v) => m.Debug.DrawFpsX = v, min: 0),
        IniKeySpec.Int ("debug", "drawfpsy",     m => m.Debug.DrawFpsY,     (m, v) => m.Debug.DrawFpsY = v, min: 0),
        IniKeySpec.Int ("debug", "logCutoff",    m => m.Debug.LogCutoff,    (m, v) => m.Debug.LogCutoff = v, min: 0, max: 5),
        IniKeySpec.Bool("debug", "sendLogfile",  m => m.Debug.SendLogfile,  (m, v) => m.Debug.SendLogfile = v),
        IniKeySpec.Bool("debug", "sendDebugger", m => m.Debug.SendDebugger, (m, v) => m.Debug.SendDebugger = v),

        // [language] (SDD §9.7)
        IniKeySpec.Int("language", "lang",    m => m.Language.Lang,    (m, v) => m.Language.Lang = v),
        IniKeySpec.Int("language", "sublang", m => m.Language.SubLang, (m, v) => m.Language.SubLang = v),

        // [Map] (SDD §9.6)
        IniKeySpec.Int("Map", "mapX", m => m.Map.MapX, (m, v) => m.Map.MapX = v, min: 1, max: 128),
        IniKeySpec.Int("Map", "mapY", m => m.Map.MapY, (m, v) => m.Map.MapY = v, min: 1, max: 128)
    ];
}
