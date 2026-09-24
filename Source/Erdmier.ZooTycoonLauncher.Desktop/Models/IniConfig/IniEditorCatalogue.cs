namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>
///     The INI Config editor's presentation catalogue: for every recognised user setting, its control, hint, and help text, grouped by section (SDD §9.3). Game-managed keys
///     have no entry, so they never render.
/// </summary>
public static class IniEditorCatalogue
{
    /// <summary>The <c>[language]/lang</c> key driven by the language picker.</summary>
    public static readonly IniKeyId LanguageId = new(Section: "language", Key: "lang");

    /// <summary>The <c>[language]/sublang</c> key driven by the language picker.</summary>
    public static readonly IniKeyId SubLanguageId = new(Section: "language", Key: "sublang");

    /// <summary>The curated languages offered by the language picker.</summary>
    public static IReadOnlyList<IniLanguageOption> Languages { get; } =
    [
        new(Lang: 9, SubLang: 1, Label: "English (United States)"),
        new(Lang: 9, SubLang: 2, Label: "English (United Kingdom)"),
        new(Lang: 7, SubLang: 1, Label: "German (Germany)"),
        new(Lang: 12, SubLang: 1, Label: "French (France)"),
        new(Lang: 10, SubLang: 3, Label: "Spanish (Modern)"),
        new(Lang: 16, SubLang: 1, Label: "Italian (Italy)"),
        new(Lang: 17, SubLang: 1, Label: "Japanese"),
        new(Lang: 22, SubLang: 1, Label: "Portuguese (Brazil)"),
        new(Lang: 19, SubLang: 1, Label: "Dutch (Netherlands)"),
        new(Lang: 29, SubLang: 1, Label: "Swedish (Sweden)")
    ];

    /// <summary>The seven sections, in SDD §9.3 order.</summary>
    public static IReadOnlyList<IniSectionDescriptor> Sections { get; } =
    [
        new(Section: "user",
            Descriptor: "Display and performance",
            Footnote: null,
            [
                new(SubHeader: null,
                    [
                        Choice(section: "user", key: "fullscreen", hint: "Display mode", help: "Run the game full screen or in a window.",
                               [new IniChoiceOption(Raw: "1", Label: "Fullscreen"), new IniChoiceOption(Raw: "0", Label: "Windowed")]),
                        Number(section: "user", key: "screenwidth", hint: "px", help: "Horizontal resolution in pixels. Must be a mode your display adapter supports."),
                        Number(section: "user", key: "screenheight", hint: "px", help: "Vertical resolution in pixels. Must be a mode your display adapter supports."),
                        Number(section: "user", key: "UpdateRate", hint: "1–60 ticks/sec",
                               help: "Game-logic ticks per second. Higher values make play smoother at the cost of CPU time."),
                        Number(section: "user", key: "DrawRate", hint: "15–120 FPS", help: "Frame-rate cap. Lower values reduce the load on the graphics card.")
                    ])
            ]),
        new(Section: "UI",
            Descriptor: "Audio, gameplay, and interface",
            Footnote: null,
            [
                new(SubHeader: "Audio",
                    [
                        Toggle(section: "UI", key: "noMenuMusic", caption: "Play menu music",
                               help: "Play the main-menu music. The INI key is inverted: noMenuMusic=1 silences it.", inverted: true, hint: "Inverted in INI"),
                        Text(section: "UI", key: "menuMusic", hint: "Relative to the game folder",
                             help: "Path to the main-menu music file, relative to the installation folder."),
                        Number(section: "UI", key: "menuMusicAttenuation", hint: "0–10000", help: "Attenuation applied to the menu music. Higher values are quieter."),
                        Number(section: "UI", key: "userAttenuation", hint: "0–10000", help: "Attenuation applied to every game sound. Higher values are quieter."),
                        Toggle(section: "UI", key: "playMovie", caption: "Enabled", help: "Play the first intro movie when the game starts."),
                        Number(section: "UI", key: "movievolume1", hint: "-10000 silent → 0 full",
                               help: "Volume of the first intro movie: 0 is full volume, -10000 is silent."),
                        Toggle(section: "UI", key: "playSecondMovie", caption: "Enabled", help: "Play the second intro movie when the game starts."),
                        Number(section: "UI", key: "movievolume2", hint: "-10000 silent → 0 full",
                               help: "Volume of the second intro movie: 0 is full volume, -10000 is silent.")
                    ]),
                new(SubHeader: "Gameplay (cash)",
                    [
                        Number(section: "UI", key: "MSStartingCash", hint: "0–10,000,000", help: "Cash available at the start of a new game."),
                        Number(section: "UI", key: "MSCashIncrement", hint: "100–1,000,000", help: "Denomination in which cash amounts are awarded."),
                        Number(section: "UI", key: "MSMinCash", hint: "0–10,000,000", help: "Minimum cash the player may hold."),
                        Number(section: "UI", key: "MSMaxCash", hint: "0–10,000,000", help: "Maximum cash the player may hold.")
                    ]),
                new(SubHeader: "Interface",
                    [
                        Toggle(section: "UI", key: "useAlternateCursors", caption: "Monochrome cursors",
                               help: "Use monochrome cursors. Recommended when the cursor flickers or disappears."),
                        Number(section: "UI", key: "tooltipDelay", hint: "0–60 s", help: "Seconds to hover before an in-game tooltip appears."),
                        Number(section: "UI", key: "tooltipDuration", hint: "0–30000 ms", help: "Milliseconds an in-game tooltip stays visible."),
                        Toggle(section: "UI", key: "MessageDisplay", caption: "Show in-game messages", help: "Show in-game notification messages."),
                        Number(section: "UI", key: "mouseScrollThreshold", hint: "0–50 px",
                               help: "Distance from the screen edge, in pixels, at which mouse-edge scrolling starts."),
                        Number(section: "UI", key: "mouseScrollDelay", hint: "0–10", help: "Delay before mouse-edge scrolling begins once the cursor reaches the edge."),
                        Number(section: "UI", key: "mouseScrollX", hint: "1–200", help: "Horizontal mouse-edge scroll speed."),
                        Number(section: "UI", key: "mouseScrollY", hint: "1–200", help: "Vertical mouse-edge scroll speed."),
                        Number(section: "UI", key: "keyScrollX", hint: "1–200", help: "Horizontal keyboard scroll speed."),
                        Number(section: "UI", key: "keyScrollY", hint: "1–200", help: "Vertical keyboard scroll speed."),
                        Number(section: "UI", key: "minimumMessageInterval", hint: "0–3600 s", help: "Minimum seconds between repeated notifications of the same kind."),
                        Choice(section: "UI", key: "helpType", hint: "In-game help",
                               help: "How much in-game help the game shows. Verbose adds extra explanatory text to most controls.",
                               [
                                   new IniChoiceOption(Raw: "0", Label: "Off"),
                                   new IniChoiceOption(Raw: "1", Label: "Standard"),
                                   new IniChoiceOption(Raw: "2", Label: "Verbose")
                               ])
                    ])
            ]),
        new(Section: "advanced",
            Descriptor: "Graphics quality and 8-bit audio",
            Footnote: null,
            [
                new(SubHeader: null,
                    [
                        Choice(section: "advanced", key: "level", hint: "Quality preset",
                               help: "Overall quality preset. Lower values favour quality, higher values favour speed; Paused stops the renderer.",
                               [
                                   new IniChoiceOption(Raw: "0", Label: "0 – Total Quality"),
                                   new IniChoiceOption(Raw: "1", Label: "1 – Quality"),
                                   new IniChoiceOption(Raw: "2", Label: "2 – Balance"),
                                   new IniChoiceOption(Raw: "3", Label: "3 – Speed"),
                                   new IniChoiceOption(Raw: "4", Label: "4 – Paused")
                               ]),
                        Toggle(section: "advanced", key: "loadHalfAnims", caption: "Reduced-detail animations",
                               help: "Load reduced-detail animation sets to help older hardware."),
                        Toggle(section: "advanced", key: "drag", caption: "Drop quality on drag", help: "Lower the rendering quality while objects are being dragged."),
                        Toggle(section: "advanced", key: "click", caption: "Drop quality on click", help: "Lower the rendering quality during click operations."),
                        Toggle(section: "advanced", key: "normal", caption: "Drop quality in normal play", help: "Lower the rendering quality during normal play."),
                        Toggle(section: "advanced", key: "use8BitSound", caption: "Force 8-bit audio", help: "Force 8-bit audio output. Can help on older sound hardware.")
                    ])
            ]),
        new(Section: "ai",
            Descriptor: "AI behaviour limits",
            Footnote: "Raising this above the stock 1,000 limit can make guest pathing thrash on the original engine; values above 2,500 need a community AI patch.",
            [
                new(SubHeader: null,
                    [
                        Number(section: "ai", key: "maxGuests", hint: "1–10000",
                               help: "Maximum number of guests allowed in the zoo at once. Higher values cost more CPU time.")
                    ])
            ]),
        new(Section: "debug",
            Descriptor: "Diagnostic logging and FPS overlay",
            Footnote: null,
            [
                new(SubHeader: null,
                    [
                        Toggle(section: "debug", key: "drawfps", caption: "Show FPS counter", help: "Show a frame-rate counter overlay while playing."),
                        Number(section: "debug", key: "drawfpsx", hint: "0–16384 px", help: "Horizontal position of the frame-rate overlay, in pixels."),
                        Number(section: "debug", key: "drawfpsy", hint: "0–16384 px", help: "Vertical position of the frame-rate overlay, in pixels."),
                        Number(section: "debug", key: "logCutoff", hint: "0 verbose → 5 silent", help: "Logging cut-off. Lower values write more detail."),
                        Toggle(section: "debug", key: "sendLogfile", caption: "Write zoo.log", help: "Write the game's log to zoo.log in the installation folder."),
                        Toggle(section: "debug", key: "sendDebugger", caption: "OutputDebugString", help: "Send the game's log to an attached debugger.")
                    ])
            ]),
        new(Section: "language",
            Descriptor: "Windows LANGID / SUBLANGID",
            Footnote: null,
            [
                new(SubHeader: null,
                    [
                        new IniFieldDescriptor(Id: null, Label: "lang / sublang", IniControlKind.LanguagePicker, Hint: "Curated LANGID + SUBLANGID",
                                               Help: "Game language. Choosing an entry sets both LANGID and SUBLANGID below; other combinations can be entered directly."),
                        Number(section: "language", key: "lang", hint: "Windows LANGID 0–65535", help: "Windows primary language identifier (LANGID)."),
                        Number(section: "language", key: "sublang", hint: "Windows SUBLANGID 0–65535", help: "Windows sub-language identifier (SUBLANGID).")
                    ])
            ]),
        new(Section: "Map",
            Descriptor: "Default zoo dimensions",
            Footnote: "Default dimensions for new zoos. Doubling both values quadruples world memory; large maps may stutter on the original engine.",
            [
                new(SubHeader: null,
                    [
                        Number(section: "Map", key: "mapX", hint: "1–128 tiles wide", help: "Width of new zoos, in tiles."),
                        Number(section: "Map", key: "mapY", hint: "1–128 tiles tall", help: "Height of new zoos, in tiles.")
                    ])
            ])
    ];

    private static IniFieldDescriptor Choice(string section, string key, string hint, string help, IReadOnlyList<IniChoiceOption> options)
        => new(new IniKeyId(section, key), key, IniControlKind.Choice, hint, help)
        {
            Options = options
        };

    private static IniFieldDescriptor Number(string section, string key, string hint, string help) => new(new IniKeyId(section, key), key, IniControlKind.Number, hint, help);

    private static IniFieldDescriptor Text(string section, string key, string hint, string help) => new(new IniKeyId(section, key), key, IniControlKind.Text, hint, help);

    private static IniFieldDescriptor Toggle(string section, string key, string caption, string help, bool inverted = false, string? hint = null)
        => new(new IniKeyId(section, key), key, IniControlKind.Toggle, hint, help)
        {
            Caption  = caption,
            Inverted = inverted
        };
}
