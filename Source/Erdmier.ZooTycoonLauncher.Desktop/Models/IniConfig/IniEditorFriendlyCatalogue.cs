namespace Erdmier.ZooTycoonLauncher.Desktop.Models.IniConfig;

/// <summary>
///     The Friendly layout of the INI Config editor (spec §6): eight pages grouped by purpose, with plain labels, hints and help worded for the value the control shows. It
///     reuses the control kinds, bounds and choice lists of <see cref="IniEditorCatalogue" /> and adds the value conversions of spec §6.3.
/// </summary>
public static class IniEditorFriendlyCatalogue
{
    private const string VolumeHint = "Silent – Full";

    /// <summary>The Friendly layout's pages, in display order.</summary>
    public static IReadOnlyList<IniSectionDescriptor> Sections { get; } =
    [
        new(Section: "display",
            Title: "Display",
            Descriptor: "How the game draws its window and how fast it runs.",
            Footnote: null,
            [
                new(SubHeader: "Screen",
                    [
                        Choice(section: "user", key: "fullscreen", label: "Screen mode", hint: "Fullscreen or windowed", help: "Run the game full screen or in a window.",
                               IniEditorCatalogue.FullscreenOptions),
                        Number(section: "user", key: "screenwidth", label: "Width (pixels)", hint: "1–16384, default 800",
                               help: "How wide the game's picture is, in pixels. Must be a size your display adapter supports."),
                        Number(section: "user", key: "screenheight", label: "Height (pixels)", hint: "1–16384, default 600",
                               help: "How tall the game's picture is, in pixels. Must be a size your display adapter supports.")
                    ]),
                new(SubHeader: "Performance",
                    [
                        Number(section: "user", key: "UpdateRate", label: "Game speed", hint: "1–60 updates per second",
                               help: "How many times a second the game works out what happens next. Higher values make play smoother but use more processor time."),
                        Number(section: "user", key: "DrawRate", label: "Frame rate limit", hint: "15–120 frames per second",
                               help: "The most frames the game draws each second. Lower values ease the load on your graphics card.")
                    ])
            ]),
        new(Section: "graphics",
            Title: "Graphics",
            Descriptor: "Picture quality, and when the game trades it for speed.",
            Footnote: null,
            [
                new(SubHeader: "Quality",
                    [
                        Choice(section: "advanced", key: "level", label: "Quality preset", hint: "Quality or speed",
                               help: "The overall balance between picture quality and speed. Lower presets look better; higher ones run faster. Paused stops the game drawing.",
                               IniEditorCatalogue.QualityPresetOptions),
                        Toggle(section: "advanced", key: "loadHalfAnims", label: "Use reduced-detail animations",
                               help: "Use simpler animations to help older computers keep up.")
                    ]),
                new(SubHeader: "Lower quality while…",
                    [
                        Toggle(section: "advanced", key: "drag", label: "Dragging objects", help: "Lower the picture quality while you drag objects, to keep dragging smooth."),
                        Toggle(section: "advanced", key: "click", label: "Clicking", help: "Lower the picture quality while the game handles a click."),
                        Toggle(section: "advanced", key: "normal", label: "Playing normally", help: "Lower the picture quality all the time, not only while dragging or clicking.")
                    ])
            ]),
        new(Section: "sound",
            Title: "Sound",
            Descriptor: "Music, sound effects and the intro movies.",
            Footnote: null,
            [
                new(SubHeader: "Menu music",
                    [
                        Toggle(section: "UI", key: "noMenuMusic", label: "Enable menu music", help: "Play music on the main menu. Untick it for a silent menu.",
                               conversion: IniValueConversion.Invert),
                        Text(section: "UI", key: "menuMusic", label: "Music file", hint: "Relative to the game folder",
                             help: "The sound file played on the main menu, given relative to the game's folder."),
                        Slider(section: "UI", key: "menuMusicAttenuation", label: "Menu music volume", help: "How loud the main-menu music is.",
                               IniValueConversion.VolumeFromAttenuation)
                    ]),
                new(SubHeader: "Game sound",
                    [
                        Slider(section: "UI", key: "userAttenuation", label: "Game sound volume", help: "How loud the game's sound effects are.",
                               IniValueConversion.VolumeFromAttenuation),
                        Toggle(section: "advanced", key: "use8BitSound", label: "Force 8-bit audio", help: "Play sound at 8-bit quality. Can help on older sound hardware.")
                    ]),
                new(SubHeader: "Intro movies",
                    [
                        Toggle(section: "UI", key: "playMovie", label: "Play the first movie", help: "Play the first intro movie when the game starts."),
                        Slider(section: "UI", key: "movievolume1", label: "First movie volume", help: "How loud the first intro movie is.",
                               IniValueConversion.VolumeFromMovieVolume),
                        Toggle(section: "UI", key: "playSecondMovie", label: "Play the second movie", help: "Play the second intro movie when the game starts."),
                        Slider(section: "UI", key: "movievolume2", label: "Second movie volume", help: "How loud the second intro movie is.",
                               IniValueConversion.VolumeFromMovieVolume)
                    ])
            ]),
        new(Section: "newzoos",
            Title: "New Zoos",
            Descriptor: "The money, size and guest limit a new zoo starts with.",
            Footnote: null,
            [
                new(SubHeader: "Money",
                    [
                        Cash(section: "UI", key: "MSStartingCash", label: "Starting cash", hint: "$0–$10,000,000", help: "The cash a new zoo starts with, unless you change it before you start."),
                        Cash(section: "UI", key: "MSCashIncrement", label: "Cash step", hint: "$100–$1,000,000", help: "How much the starting cash goes up or down with each step when you change it."),
                        Cash(section: "UI", key: "MSMinCash", label: "Minimum cash", hint: "$0–$10,000,000", help: "The least starting cash you can choose for a new zoo."),
                        Cash(section: "UI", key: "MSMaxCash", label: "Maximum cash", hint: "$0–$10,000,000", help: "The most starting cash you can choose for a new zoo.")
                    ]),
                new(SubHeader: "Map size",
                    [
                        Number(section: "Map", key: "mapX", label: "Width (tiles)", hint: "1–128 tiles", help: "How many tiles wide a new zoo is."),
                        Number(section: "Map", key: "mapY", label: "Height (tiles)", hint: "1–128 tiles", help: "How many tiles tall a new zoo is.")
                    ])
                {
                    Footnote = "Default dimensions for new zoos. Doubling both values quadruples world memory; large maps may stutter on the original engine."
                },
                new(SubHeader: "Guests",
                    [
                        Number(section: "ai", key: "maxGuests", label: "Maximum guests", hint: "1–10000 guests", help: "The most guests the zoo can hold at once. Higher values use more processor time.")
                    ])
                {
                    Footnote = "Raising this above the stock 1,000 limit can make guest pathing thrash; values above 2,500 need a community AI patch."
                }
            ]),
        new(Section: "controls",
            Title: "Controls",
            Descriptor: "Scrolling the view and the mouse cursor.",
            Footnote: null,
            [
                new(SubHeader: "Mouse-edge scrolling",
                    [
                        Number(section: "UI", key: "mouseScrollThreshold", label: "Start distance from edge", hint: "0–50 pixels",
                               help: "How close to the edge of the screen the mouse must be, in pixels, before the view starts to scroll."),
                        Number(section: "UI", key: "mouseScrollDelay", label: "Delay before scrolling", hint: "0–10",
                               help: "How long the mouse must stay at the edge of the screen before the view starts to scroll."),
                        Number(section: "UI", key: "mouseScrollX", label: "Horizontal speed", hint: "1–200", help: "How fast the view scrolls sideways when the mouse is at the edge."),
                        Number(section: "UI", key: "mouseScrollY", label: "Vertical speed", hint: "1–200", help: "How fast the view scrolls up and down when the mouse is at the edge.")
                    ]),
                new(SubHeader: "Keyboard scrolling",
                    [
                        Number(section: "UI", key: "keyScrollX", label: "Horizontal speed", hint: "1–200", help: "How fast the view scrolls sideways when you use the arrow keys."),
                        Number(section: "UI", key: "keyScrollY", label: "Vertical speed", hint: "1–200", help: "How fast the view scrolls up and down when you use the arrow keys.")
                    ]),
                new(SubHeader: "Cursor",
                    [
                        Toggle(section: "UI", key: "useAlternateCursors", label: "Use monochrome cursors",
                               help: "Use plain black-and-white cursors. Recommended if the cursor flickers or disappears.")
                    ])
            ]),
        new(Section: "help",
            Title: "Help & Messages",
            Descriptor: "In-game tooltips, help text and notifications.",
            Footnote: null,
            [
                new(SubHeader: "Tooltips and help",
                    [
                        Choice(section: "UI", key: "helpType", label: "Help level", hint: "How much help to show",
                               help: "How much in-game help the game shows. Verbose adds extra explanatory text to most controls.", IniEditorCatalogue.HelpLevelOptions),
                        Number(section: "UI", key: "tooltipDelay", label: "Tooltip delay", hint: "0–60 seconds", help: "How many seconds you hover before a tooltip appears."),
                        Number(section: "UI", key: "tooltipDuration", label: "Tooltip duration", hint: "0–30000 milliseconds", help: "How many milliseconds a tooltip stays on screen.")
                    ]),
                new(SubHeader: "Messages",
                    [
                        Toggle(section: "UI", key: "MessageDisplay", label: "Show in-game messages", help: "Show the game's notification messages while you play."),
                        Number(section: "UI", key: "minimumMessageInterval", label: "Time between repeats", hint: "0–3600 seconds",
                               help: "The least time, in seconds, between two notifications of the same kind.")
                    ])
            ]),
        new(Section: "language",
            Title: "Language",
            Descriptor: "The language the game's text appears in.",
            Footnote: null,
            [
                new(SubHeader: "Language",
                    [
                        new IniFieldDescriptor(Id: null, Label: "Game language", IniControlKind.LanguagePicker, Hint: "The language of the game's text",
                                               Help: "The language the game's text appears in."),
                        Hidden(IniEditorCatalogue.LanguageId, label: "Language code", help: "Windows primary language identifier (LANGID). Set by the language picker."),
                        Hidden(IniEditorCatalogue.SubLanguageId, label: "Region code", help: "Windows sub-language identifier (SUBLANGID). Set by the language picker.")
                    ])
            ]),
        new(Section: "troubleshooting",
            Title: "Troubleshooting",
            Descriptor: "Diagnostics for when the game misbehaves.",
            Footnote: null,
            [
                new(SubHeader: "Frame-rate counter",
                    [
                        Toggle(section: "debug", key: "drawfps", label: "Show the frame-rate counter", help: "Show a frame-rate counter on screen while you play."),
                        Number(section: "debug", key: "drawfpsx", label: "Horizontal position", hint: "0–16384 pixels from the left",
                               help: "How far from the left edge of the screen the frame-rate counter sits, in pixels."),
                        Number(section: "debug", key: "drawfpsy", label: "Vertical position", hint: "0–16384 pixels from the top",
                               help: "How far from the top edge of the screen the frame-rate counter sits, in pixels.")
                    ]),
                new(SubHeader: "Logging",
                    [
                        Number(section: "debug", key: "logCutoff", label: "Log detail", hint: "0 most detail → 5 least",
                               help: "How much the game writes to its log. Lower values record more detail; higher values record less."),
                        Toggle(section: "debug", key: "sendLogfile", label: "Write zoo.log", help: "Save the game's log to zoo.log in the game's folder."),
                        Toggle(section: "debug", key: "sendDebugger", label: "Send the log to a debugger", help: "Also send the game's log to a debugging tool, if one is attached.")
                    ])
            ])
    ];

    private static IniFieldDescriptor Choice(string section, string key, string label, string hint, string help, IReadOnlyList<IniChoiceOption> options)
        => new(new IniKeyId(section, key), label, IniControlKind.Choice, hint, help)
        {
            Options = options
        };

    private static IniFieldDescriptor Number(string section, string key, string label, string hint, string help)
        => new(new IniKeyId(section, key), label, IniControlKind.Number, hint, help);

    private static IniFieldDescriptor Cash(string section, string key, string label, string hint, string help)
        => new(new IniKeyId(section, key), label, IniControlKind.Number, hint, help) { IsCurrency = true };

    private static IniFieldDescriptor Text(string section, string key, string label, string hint, string help)
        => new(new IniKeyId(section, key), label, IniControlKind.Text, hint, help);

    private static IniFieldDescriptor Toggle(string section, string key, string label, string help, IniValueConversion? conversion = null)
        => new(new IniKeyId(section, key), label, IniControlKind.Toggle, Hint: null, help)
        {
            Caption    = label,
            Conversion = conversion ?? IniValueConversion.None
        };

    private static IniFieldDescriptor Slider(string section, string key, string label, string help, IniValueConversion conversion)
        => new(new IniKeyId(section, key), label, IniControlKind.Slider, VolumeHint, help) { Conversion = conversion };

    private static IniFieldDescriptor Hidden(IniKeyId id, string label, string help)
        => new(id, label, IniControlKind.Number, Hint: null, help)
        {
            IsReadOnly = true,
            IsHidden   = true
        };
}
