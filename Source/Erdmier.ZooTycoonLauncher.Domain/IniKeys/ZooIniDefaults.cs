namespace Erdmier.ZooTycoonLauncher.Domain.IniKeys;

/// <summary>
///     The single source of truth for every <c>zoo.ini</c> key the launcher recognises: 56 keys across seven sections, 46 user settings and 10 game-managed keys (SDD §5.3).
///     <c>[scenario]</c> is deliberately absent until the Phase 0 research (SDD §7.8) lands.
/// </summary>
public static class ZooIniDefaults
{
    /// <summary>The recognised sections, in the SDD §9.3 display order.</summary>
    public static IReadOnlyList<string> Sections { get; } = ["user", "UI", "advanced", "ai", "debug", "language", "Map"];

    /// <summary>Every recognised key, grouped by section in <see cref="Sections" /> order.</summary>
    public static IReadOnlyList<IniKeySpec> Keys { get; } =
    [
        Bool(section: "user", key: "fullscreen", defaultValue: true),
        Int(section: "user", key: "screenwidth", defaultValue: 800, min: 1, max: 16384),
        Int(section: "user", key: "screenheight", defaultValue: 600, min: 1, max: 16384),
        Int(section: "user", key: "UpdateRate", defaultValue: 15, min: 1, max: 60),
        Int(section: "user", key: "DrawRate", defaultValue: 60, min: 15, max: 120),
        GameManagedNullableStr(section: "user", key: "lastfile"),
        Bool(section: "user", key: "showUserEntityWarning", defaultValue: false, IniKeyRole.GameManaged),

        Bool(section: "UI", key: "noMenuMusic", defaultValue: false),
        Str(section: "UI", key: "menuMusic", defaultValue: "sounds/mainmenu.wav"),
        Int(section: "UI", key: "menuMusicAttenuation", defaultValue: 1500, min: 0, max: 10000),
        Int(section: "UI", key: "userAttenuation", defaultValue: 0, min: 0, max: 10000),
        Bool(section: "UI", key: "playMovie", defaultValue: false),
        Int(section: "UI", key: "movievolume1", defaultValue: -1000, min: -10000, max: 0),
        Bool(section: "UI", key: "playSecondMovie", defaultValue: false),
        Int(section: "UI", key: "movievolume2", defaultValue: -1000, min: -10000, max: 0),
        Int(section: "UI", key: "MSStartingCash", defaultValue: 70000, min: 0, max: 10000000),
        Int(section: "UI", key: "MSCashIncrement", defaultValue: 5000, min: 100, max: 1000000),
        Int(section: "UI", key: "MSMinCash", defaultValue: 10000, min: 0, max: 10000000),
        Int(section: "UI", key: "MSMaxCash", defaultValue: 500000, min: 0, max: 10000000),
        Bool(section: "UI", key: "useAlternateCursors", defaultValue: false),
        Int(section: "UI", key: "tooltipDelay", defaultValue: 1, min: 0, max: 60),
        Int(section: "UI", key: "tooltipDuration", defaultValue: 3000, min: 0, max: 30000),
        Bool(section: "UI", key: "MessageDisplay", defaultValue: true),
        Int(section: "UI", key: "mouseScrollThreshold", defaultValue: 1, min: 0, max: 50),
        Int(section: "UI", key: "mouseScrollDelay", defaultValue: 1, min: 0, max: 10),
        Int(section: "UI", key: "mouseScrollX", defaultValue: 27, min: 1, max: 200),
        Int(section: "UI", key: "mouseScrollY", defaultValue: 27, min: 1, max: 200),
        Int(section: "UI", key: "keyScrollX", defaultValue: 64, min: 1, max: 200),
        Int(section: "UI", key: "keyScrollY", defaultValue: 64, min: 1, max: 200),
        Int(section: "UI", key: "minimumMessageInterval", defaultValue: 60, min: 0, max: 3600),
        Int(section: "UI", key: "helpType", defaultValue: 1, min: 0, max: 2),
        GameManagedNullableInt(section: "UI", key: "lastWindowX"),
        GameManagedNullableInt(section: "UI", key: "lastWindowY"),
        Bool(section: "UI", key: "startedFirstTutorial", defaultValue: false, IniKeyRole.GameManaged),
        Bool(section: "UI", key: "startedDinoTutorial", defaultValue: false, IniKeyRole.GameManaged),
        Bool(section: "UI", key: "startedAquaTutorial", defaultValue: false, IniKeyRole.GameManaged),
        GameManagedNullableInt(section: "UI", key: "progresscalls"),
        GameManagedNullableInt(section: "UI", key: "defaultEditCharLimit"),
        GameManagedNullableInt(section: "UI", key: "completedExhibitAttenuation"),

        Int(section: "advanced", key: "level", defaultValue: 2, min: 0, max: 4),
        Bool(section: "advanced", key: "loadHalfAnims", defaultValue: false),
        Bool(section: "advanced", key: "drag", defaultValue: false),
        Bool(section: "advanced", key: "click", defaultValue: false),
        Bool(section: "advanced", key: "normal", defaultValue: false),
        Bool(section: "advanced", key: "use8BitSound", defaultValue: false),

        Int(section: "ai", key: "maxGuests", defaultValue: 1000, min: 1, max: 10000),

        Bool(section: "debug", key: "drawfps", defaultValue: false),
        Int(section: "debug", key: "drawfpsx", defaultValue: 720, min: 0, max: 16384),
        Int(section: "debug", key: "drawfpsy", defaultValue: 20, min: 0, max: 16384),
        Int(section: "debug", key: "logCutoff", defaultValue: 1, min: 0, max: 5),
        Bool(section: "debug", key: "sendLogfile", defaultValue: true),
        Bool(section: "debug", key: "sendDebugger", defaultValue: true),

        Int(section: "language", key: "lang", defaultValue: 9, min: 0, max: 65535),
        Int(section: "language", key: "sublang", defaultValue: 1, min: 0, max: 65535),

        Int(section: "Map", key: "mapX", defaultValue: 75, min: 1, max: 128),
        Int(section: "Map", key: "mapY", defaultValue: 75, min: 1, max: 128)
    ];

    // Declared after Keys so the static initialisers run in the right order.
    private static readonly Dictionary<IniKeyId, IniKeySpec> ById = Keys.ToDictionary(spec => spec.Id);

    /// <summary>Looks a key up case-insensitively.</summary>
    /// <param name="id">The key to find.</param>
    /// <param name="spec">The key's spec, in registry casing, when recognised.</param>
    /// <returns><see langword="true" /> when the key is recognised.</returns>
    public static bool TryGet(IniKeyId id, [ NotNullWhen(true) ] out IniKeySpec? spec) => ById.TryGetValue(id, out spec);

    /// <summary>
    ///     Returns the raw value of every recognised key present in <paramref name="document" />, keyed by the registry's <see cref="IniKeySpec.Id" />. When a key appears more
    ///     than once the first occurrence wins. Unrecognised keys are ignored — they stay in the document's text only.
    /// </summary>
    /// <param name="document">The parsed file.</param>
    /// <returns>The recognised values present in the file.</returns>
    public static IReadOnlyDictionary<IniKeyId, string?> ExtractValues(IniDocument document)
    {
        Dictionary<IniKeyId, string?> values = [];

        foreach (IniKeyValue line in document.Lines.OfType<IniKeyValue>())
        {
            if (ById.TryGetValue(line.Id, out IniKeySpec? spec)
                && !values.ContainsKey(spec.Id))
            {
                values[spec.Id] = line.Value;
            }
        }

        return values;
    }

    private static IniKeySpec Bool(string section, string key, bool defaultValue, IniKeyRole? role = null)
        => new(new IniKeyId(section, key), IniValueKind.Bool, defaultValue ? "1" : "0", Min: null, Max: null, role ?? IniKeyRole.UserSetting);

    private static IniKeySpec Int(string section, string key, int defaultValue, int min, int max)
        => new(new IniKeyId(section, key), IniValueKind.Int, defaultValue.ToString(CultureInfo.InvariantCulture), min, max, IniKeyRole.UserSetting);

    private static IniKeySpec Str(string section, string key, string defaultValue)
        => new(new IniKeyId(section, key), IniValueKind.Str, defaultValue, Min: null, Max: null, IniKeyRole.UserSetting);

    private static IniKeySpec GameManagedNullableInt(string section, string key)
        => new(new IniKeyId(section, key), IniValueKind.NullableInt, DefaultValue: null, Min: null, Max: null, IniKeyRole.GameManaged);

    private static IniKeySpec GameManagedNullableStr(string section, string key)
        => new(new IniKeyId(section, key), IniValueKind.NullableStr, DefaultValue: null, Min: null, Max: null, IniKeyRole.GameManaged);
}
