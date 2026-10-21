/* global React, Btn, TextField, NumberField, Checkbox, Combo, Icons */
/* eslint-disable no-undef */

const { useState, useContext, createContext } = React;

// ============================================================================
// Help text for every editable INI option. `short` is the one-liner surfaced
// in the editor's status bar on hover/focus. `desc` is the full description
// shown as an OS-level tooltip on hover.
//
// Sourced from References/Erdmier.ZooTycoonLauncherRef/Resources/IniTooltips.axaml
// (the Ref project's canonical tooltip resources); shorts crafted from the
// same prose. Defaults are kept out of these strings — they live on the
// inputs themselves.
// ============================================================================

const HELP = {
  // Display & Performance (SDD §9.1)
  ScreenMode:           { short: "Fullscreen or windowed.",
                          desc:  "Run the game in fullscreen or in a window. Maps to the [user]/fullscreen INI key." },
  ScreenWidth:          { short: "Horizontal resolution in pixels.",
                          desc:  "Horizontal resolution in pixels. Must match a mode supported by the display adapter." },
  ScreenHeight:         { short: "Vertical resolution in pixels.",
                          desc:  "Vertical resolution in pixels. Must match a mode supported by the display adapter." },
  UpdateRate:           { short: "Game logic ticks per second.",
                          desc:  "Game logic ticks per second. Higher values give smoother gameplay but cost CPU." },
  DrawRate:             { short: "Target frame-rate cap (FPS).",
                          desc:  "Target frame-rate cap (FPS). Lower values reduce GPU load." },

  // Graphics Quality (SDD §9.2)
  Level:                { short: "Overall quality preset.",
                          desc:  "Overall quality preset. Lower values prefer quality; higher values prefer speed. Paused disables the renderer entirely." },
  LoadHalfAnims:        { short: "Reduced-detail animations for performance.",
                          desc:  "Load reduced-detail animation sets to improve performance on older hardware." },
  Drag:                 { short: "Drop quality while objects are dragged.",
                          desc:  "Drop rendering quality while objects are being dragged." },
  Click:                { short: "Drop quality during clicks.",
                          desc:  "Drop rendering quality during click operations." },
  Normal:               { short: "Drop quality during normal play.",
                          desc:  "Drop rendering quality during normal play." },

  // Audio (SDD §9.3)
  PlayMenuMusic:        { short: "Play main-menu background music.",
                          desc:  "Play the main-menu background music. (The underlying [UI]/noMenuMusic INI key uses inverted logic.)" },
  MenuMusic:            { short: "Path to the menu-music file.",
                          desc:  "Path to the menu-music file, relative to the game's installation directory." },
  MenuMusicAttenuation: { short: "Menu-music attenuation (higher = quieter).",
                          desc:  "Menu-music attenuation. Higher values are quieter." },
  UserAttenuation:      { short: "Global audio attenuation (higher = quieter).",
                          desc:  "Global audio attenuation applied to all sounds. Higher values are quieter." },
  PlayMovie:            { short: "Play first intro movie on startup.",
                          desc:  "Play the first intro movie on startup." },
  MovieVolume1:         { short: "First intro movie volume (0 full, -10000 silent).",
                          desc:  "Volume for the first intro movie. 0 = full volume; -10000 = silent." },
  PlaySecondMovie:      { short: "Play second intro movie on startup.",
                          desc:  "Play the second intro movie on startup." },
  MovieVolume2:         { short: "Second intro movie volume (0 full, -10000 silent).",
                          desc:  "Volume for the second intro movie. 0 = full volume; -10000 = silent." },
  Use8BitSound:         { short: "Force 8-bit audio output.",
                          desc:  "Force 8-bit audio output. May improve compatibility on older sound hardware." },

  // Gameplay (SDD §9.4)
  StartingCash:         { short: "Cash awarded at the start of a new game.",
                          desc:  "Cash awarded at the start of a new game." },
  CashIncrement:        { short: "Denomination cash awards are rounded to.",
                          desc:  "Denomination by which cash awards are rounded." },
  MinCash:              { short: "Minimum cash the player can hold.",
                          desc:  "Minimum cash the player is allowed to hold." },
  MaxCash:              { short: "Maximum cash the player can hold.",
                          desc:  "Maximum cash the player is allowed to hold." },
  MaxGuests:            { short: "Maximum guests in the zoo simultaneously.",
                          desc:  "Maximum number of guests permitted in the zoo simultaneously. Higher values are more demanding on the CPU." },

  // Interface (SDD §9.5)
  UseAlternateCursors:  { short: "Use monochrome cursors.",
                          desc:  "Use monochrome cursors. Recommended if the cursor flickers or disappears on your display." },
  TooltipDelay:         { short: "Seconds to hover before tooltips appear.",
                          desc:  "Seconds to hover before tooltips appear." },
  TooltipDuration:      { short: "Milliseconds tooltips remain visible.",
                          desc:  "Milliseconds tooltips remain visible after appearing." },
  MessageDisplay:       { short: "Show in-game notification messages.",
                          desc:  "Show in-game notification messages." },
  MouseScrollThreshold: { short: "Edge distance to start mouse scrolling.",
                          desc:  "Pixel distance from the screen edge at which mouse-edge scrolling activates." },
  MouseScrollDelay:     { short: "Delay before mouse-edge scrolling begins.",
                          desc:  "Delay before mouse-edge scrolling begins after the cursor enters the threshold." },
  MouseScrollX:         { short: "Horizontal mouse-edge scroll speed.",
                          desc:  "Horizontal mouse-edge scroll speed." },
  MouseScrollY:         { short: "Vertical mouse-edge scroll speed.",
                          desc:  "Vertical mouse-edge scroll speed." },
  KeyScrollX:           { short: "Horizontal keyboard scroll speed.",
                          desc:  "Horizontal keyboard scroll speed." },
  KeyScrollY:           { short: "Vertical keyboard scroll speed.",
                          desc:  "Vertical keyboard scroll speed." },
  MinimumMessageInterval: { short: "Minimum interval between repeat notifications.",
                            desc:  "Minimum interval (seconds) between repeated notifications of the same kind." },
  HelpType:             { short: "Help-system verbosity.",
                          desc:  "Help-system verbosity. Verbose surfaces extra explanatory text on most controls." },

  // Map (SDD §9.6)
  MapX:                 { short: "Default new-zoo width in tiles.",
                          desc:  "Default new-zoo width in tiles." },
  MapY:                 { short: "Default new-zoo height in tiles.",
                          desc:  "Default new-zoo height in tiles." },

  // Language (SDD §9.7)
  Language:             { short: "Game language (LANGID / SUBLANGID).",
                          desc:  "Game language. Sets the Windows LANGID and SUBLANGID fields. The list shows common combinations bundled with the game; custom IDs require direct INI editing for now." },

  // Debug (SDD §9.8)
  DrawFps:              { short: "Show FPS counter overlay.",
                          desc:  "Show an FPS counter overlay during gameplay." },
  DrawFpsX:             { short: "Horizontal position of FPS overlay.",
                          desc:  "Horizontal pixel position of the FPS counter overlay." },
  DrawFpsY:             { short: "Vertical position of FPS overlay.",
                          desc:  "Vertical pixel position of the FPS counter overlay." },
  LogCutoff:            { short: "Logging verbosity cutoff.",
                          desc:  "Logging verbosity cutoff. Lower values produce more detail." },
  SendLogfile:          { short: "Write log output to a file on disk.",
                          desc:  "Write log output to a file on disk." },
  SendDebugger:         { short: "Send log output to attached debugger.",
                          desc:  "Send log output to an attached debugger via OutputDebugString." },
};

// ============================================================================
// Contexts.
//   HelpCtx — Rows fire onHover/onLeave; the editor reflects the short
//             string in its footer status area.
// ============================================================================

const HelpCtx = createContext({ onHover: () => {}, onLeave: () => {} });

// ============================================================================
// SECTIONS — one INI section per list entry. `label` is the mono list caption;
// `desc` is a friendly one-liner shown above the panel. [scenario] is appended
// last for editing convenience.
// ============================================================================

const SECTIONS = [
  { id: "user",     label: "[user]",     desc: "Display and performance" },
  { id: "ui",       label: "[UI]",       desc: "Audio, gameplay, and interface" },
  { id: "advanced", label: "[advanced]", desc: "Graphics quality and 8-bit audio" },
  { id: "ai",       label: "[ai]",       desc: "AI behaviour limits" },
  { id: "debug",    label: "[debug]",    desc: "Diagnostic logging and FPS overlay" },
  { id: "language", label: "[language]", desc: "Windows LANGID / SUBLANGID" },
  { id: "map",      label: "[Map]",      desc: "Default zoo dimensions" },
  { id: "scenario", label: "[scenario]", desc: "Campaign completion flags" },
];

// ============================================================================
// IniConfigTab — three-state shell that routes between the live INI editor
// (IniEditor), the recovery prompt (IniMissingState), and the no-install
// surface.
// ============================================================================

function IniConfigTab({ state }) {
  if (state === "looking" || state === "noinstall" || state === "open") {
    return (
      <div className="dk-c-dim" style={{ minHeight: 280, display: "flex", alignItems: "center", justifyContent: "center", textAlign: "center", padding: 30 }}>
        <div style={{ fontSize: 12 }}>
          INI editing becomes available once an installation is open and its <span className="mono">zoo.ini</span> has been parsed.
        </div>
      </div>
    );
  }

  if (state === "cannot") {
    return <IniMissingState />;
  }

  return <IniEditor />;
}

// ---- No INI present (Cannot Play) ------------------------------------------
function IniMissingState() {
  return (
    <div style={{ minHeight: 280, padding: 10, display: "flex", alignItems: "flex-start", gap: 14 }}>
      <Icons.Warning size={32} />
      <div style={{ flex: 1, lineHeight: 1.45 }}>
        <div style={{ fontWeight: "bold" }}>No INI present</div>
        <div className="muted">
          This installation has no <span className="mono">zoo.ini</span> on disk. Generate a fresh
          file from launcher defaults to begin editing — Original and Current snapshots will be
          created in the installation database.
        </div>
        <div style={{ marginTop: 10, display: "flex", gap: 6 }}>
          <Btn isDefault accel="C">Create zoo.ini from defaults</Btn>
          <Btn accel="L">Locate existing zoo.ini</Btn>
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// IniEditor — Win95 two-pane form: a section list-box on the left, a sunken
// content panel on the right, and an action row at the bottom.
// ============================================================================

function IniEditor() {
  const [sectionId, setSectionId] = useState("user");
  const [model, setModel] = useState(DEFAULT_MODEL);
  const [dirty, setDirty] = useState(false);
  const [hoverShort, setHoverShort] = useState(null);

  const set = (section, key, value) => {
    setDirty(true);
    setModel(m => ({ ...m, [section]: { ...m[section], [key]: value } }));
  };

  const helpCtx = {
    onHover: (id) => { if (HELP[id]) setHoverShort(HELP[id].short); },
    onLeave: () => setHoverShort(null),
  };

  const section = SECTIONS.find(s => s.id === sectionId);
  const dirtyLabel = dirty ? "● Unsaved changes" : "All changes saved";
  const showShort  = !!hoverShort;

  return (
    <HelpCtx.Provider value={helpCtx}>
      <div style={{ display: "flex", flexDirection: "column", gap: 8, padding: 2 }}>

        <div style={{ display: "flex", gap: 14, alignItems: "stretch" }}>

          {/* Left — section list-box */}
          <div style={{ flex: "0 0 150px", display: "flex", flexDirection: "column" }}>
            <div className="muted" style={{ fontSize: 11, marginBottom: 3 }}>Section</div>
            <div className="sunken" style={{ flex: 1, padding: 2, overflowY: "auto" }}>
              {SECTIONS.map(s => {
                const sel = s.id === sectionId;
                return (
                  <div
                    key={s.id}
                    className="mono"
                    onClick={() => setSectionId(s.id)}
                    style={{
                      fontSize: 12,
                      padding: "2px 6px",
                      cursor: "default",
                      background: sel ? "#316ac5" : "transparent",
                      color: sel ? "#ffffff" : "#000000",
                    }}
                  >
                    {s.label}
                  </div>
                );
              })}
            </div>
          </div>

          {/* Right — section description + sunken content panel */}
          <div style={{ flex: 1, minWidth: 0, display: "flex", flexDirection: "column" }}>
            <div className="muted" style={{ fontSize: 11, marginBottom: 3 }}>{section.desc}</div>
            <div className="sunken" style={{ padding: 14, height: 340, overflowY: "auto", overflowX: "hidden" }}>
              <div style={{ marginBottom: 12, paddingBottom: 8, borderBottom: "1px solid #d4d0c8" }}>
                <span className="mono bold">{section.label}</span>
                <span className="muted mono" style={{ fontSize: 11 }}> section of zoo.ini</span>
              </div>
              {sectionId === "user"     && <UserSection     m={model.user}     set={(k,v) => set("user",     k, v)} />}
              {sectionId === "ui"       && <UiSection       m={model.ui}       set={(k,v) => set("ui",       k, v)} />}
              {sectionId === "advanced" && <AdvancedSection m={model.advanced} set={(k,v) => set("advanced", k, v)} />}
              {sectionId === "ai"       && <AiSection       m={model.ai}       set={(k,v) => set("ai",       k, v)} />}
              {sectionId === "debug"    && <DebugSection    m={model.debug}    set={(k,v) => set("debug",    k, v)} />}
              {sectionId === "language" && <LanguageSection m={model.language} set={(k,v) => set("language", k, v)} />}
              {sectionId === "map"      && <MapSection      m={model.map}      set={(k,v) => set("map",      k, v)} />}
              {sectionId === "scenario" && <ScenarioSection m={model.scenario} set={(k,v) => set("scenario", k, v)} />}
            </div>
          </div>
        </div>

        {/* Status + action row */}
        <div style={{ display: "flex", alignItems: "center", gap: 6, minHeight: 23 }}>
          <span
            className={`${showShort ? "" : (dirty ? "bold dk-c-maroon" : "muted")}`}
            style={{ fontStyle: showShort ? "italic" : undefined, fontSize: 11 }}
          >
            {showShort ? hoverShort : dirtyLabel}
          </span>
          {!showShort && (
            <>
              <span className="muted">·</span>
              <span className="muted" style={{ fontSize: 11 }}>Last write: 24/05/2026 19:02 UTC</span>
            </>
          )}
          <div style={{ flex: 1 }} />
          <Btn onClick={() => window.app.openDialog("history")} accel="U">Undo</Btn>
          <Btn onClick={() => { setModel(DEFAULT_MODEL); setDirty(false); }}>Restore defaults</Btn>
          <Btn isDefault disabled={!dirty} onClick={() => setDirty(false)} accel="S">Save</Btn>
          <Btn disabled={!dirty} onClick={() => { setModel(DEFAULT_MODEL); setDirty(false); }} accel="R">Revert</Btn>
        </div>

      </div>
    </HelpCtx.Provider>
  );
}

// ---- Layout primitives ------------------------------------------------------

// A sub-header dividing groups of rows inside a section panel.
function SubHead({ children }) {
  return (
    <div className="bold" style={{ fontSize: 11, margin: "12px 0 6px", color: "#000080" }}>
      {children}
    </div>
  );
}

function Row({ id, label, hint, children, labelWidth = 184 }) {
  const help = useContext(HelpCtx);
  const handlers = id ? {
    onMouseEnter: () => help.onHover(id),
    onMouseLeave: () => help.onLeave(),
    title:        HELP[id] ? HELP[id].desc : undefined,
  } : {};
  return (
    <div
      style={{
        display: "grid",
        gridTemplateColumns: `${labelWidth}px minmax(0, 1fr)`,
        alignItems: "center",
        marginBottom: 7,
        gap: 10,
      }}
      {...handlers}
    >
      <div className="mono" style={{ fontSize: 11, lineHeight: 1.25 }}>
        {label}
        {hint && <div className="muted" style={{ fontSize: 11, fontFamily: "var(--ff-sys)", marginTop: 1 }}>{hint}</div>}
      </div>
      <div style={{ display: "flex", alignItems: "center", minWidth: 0 }}>{children}</div>
    </div>
  );
}

// A full-width note/aside.
function Note({ children }) {
  return (
    <div className="muted" style={{ marginTop: 4, lineHeight: 1.5, fontSize: 11 }}>{children}</div>
  );
}

// ============================================================================
// Section bodies — flat rows grouped by sub-headers. Field set matches
// ZooIniDefaults.KnownKeys (Ref repo); defaults match each submodel's
// property initialisers.
// ============================================================================

function UserSection({ m, set }) {
  return (
    <>
      <SubHead>Screen</SubHead>
      <Row id="ScreenMode" label="fullscreen" hint="Display mode">
        <Combo
          width={130}
          value={m.Fullscreen ? "fs" : "win"}
          onChange={v => set("Fullscreen", v === "fs")}
          options={[
            { value: "fs",  label: "Fullscreen" },
            { value: "win", label: "Windowed" },
          ]}
        />
      </Row>
      <Row id="ScreenWidth"  label="screenwidth"  hint="px"><NumberField value={m.ScreenWidth}  min={1} max={16384} onChange={v => set("ScreenWidth", v)} /></Row>
      <Row id="ScreenHeight" label="screenheight" hint="px"><NumberField value={m.ScreenHeight} min={1} max={16384} onChange={v => set("ScreenHeight", v)} /></Row>

      <SubHead>Frame rate</SubHead>
      <Row id="UpdateRate" label="UpdateRate" hint="1–60 ticks/sec"><NumberField value={m.UpdateRate} min={1}  max={60}  onChange={v => set("UpdateRate", v)} /></Row>
      <Row id="DrawRate"   label="DrawRate"   hint="15–120 FPS"><NumberField value={m.DrawRate}   min={15} max={120} onChange={v => set("DrawRate", v)} /></Row>
    </>
  );
}

function UiSection({ m, set }) {
  return (
    <>
      <SubHead>Audio</SubHead>
      <Row id="PlayMenuMusic" label="noMenuMusic" hint="Inverted in INI">
        <Checkbox label="Play menu music" checked={!m.NoMenuMusic} onChange={v => set("NoMenuMusic", !v)} />
      </Row>
      <Row id="MenuMusic" label="menuMusic"><TextField value={m.MenuMusic} onChange={v => set("MenuMusic", v)} width={300} /></Row>
      <Row id="MenuMusicAttenuation" label="menuMusicAttenuation" hint="0–10000"><NumberField value={m.MenuMusicAttenuation} min={0} max={10000} onChange={v => set("MenuMusicAttenuation", v)} width={100} /></Row>
      <Row id="UserAttenuation" label="userAttenuation" hint="0–10000"><NumberField value={m.UserAttenuation} min={0} max={10000} onChange={v => set("UserAttenuation", v)} width={100} /></Row>
      <Row id="PlayMovie" label="playMovie"><Checkbox label="Play first intro movie" checked={m.PlayMovie} onChange={v => set("PlayMovie", v)} /></Row>
      <Row id="MovieVolume1" label="movievolume1" hint="-10000 silent → 0 full"><NumberField value={m.MovieVolume1} min={-10000} max={0} step={100} onChange={v => set("MovieVolume1", v)} width={100} /></Row>
      <Row id="PlaySecondMovie" label="playSecondMovie"><Checkbox label="Play second intro movie" checked={m.PlaySecondMovie} onChange={v => set("PlaySecondMovie", v)} /></Row>
      <Row id="MovieVolume2" label="movievolume2" hint="-10000 silent → 0 full"><NumberField value={m.MovieVolume2} min={-10000} max={0} step={100} onChange={v => set("MovieVolume2", v)} width={100} /></Row>

      <SubHead>Gameplay — cash</SubHead>
      <Row id="StartingCash"  label="MSStartingCash"  hint="0–10,000,000"><NumberField value={m.StartingCash}  min={0} max={10000000} step={1000} onChange={v => set("StartingCash", v)}  width={120} /></Row>
      <Row id="CashIncrement" label="MSCashIncrement" hint="100–1,000,000"><NumberField value={m.CashIncrement} min={100} max={1000000} step={100}  onChange={v => set("CashIncrement", v)} width={120} /></Row>
      <Row id="MinCash"       label="MSMinCash"       hint="0–10,000,000"><NumberField value={m.MinCash}       min={0} max={10000000} step={1000} onChange={v => set("MinCash", v)}       width={120} /></Row>
      <Row id="MaxCash"       label="MSMaxCash"       hint="0–10,000,000"><NumberField value={m.MaxCash}       min={0} max={10000000} step={1000} onChange={v => set("MaxCash", v)}       width={120} /></Row>

      <SubHead>Interface — cursors, tooltips &amp; messages</SubHead>
      <Row id="UseAlternateCursors" label="useAlternateCursors"><Checkbox label="Monochrome cursors" checked={m.UseAlternateCursors} onChange={v => set("UseAlternateCursors", v)} /></Row>
      <Row id="TooltipDelay"    label="tooltipDelay"    hint="0–60 s"><NumberField value={m.TooltipDelay}    min={0} max={60}    onChange={v => set("TooltipDelay", v)} /></Row>
      <Row id="TooltipDuration" label="tooltipDuration" hint="0–30000 ms"><NumberField value={m.TooltipDuration} min={0} max={30000} step={100} onChange={v => set("TooltipDuration", v)} width={100} /></Row>
      <Row id="MessageDisplay"  label="MessageDisplay"><Checkbox label="Show in-game messages" checked={m.MessageDisplay} onChange={v => set("MessageDisplay", v)} /></Row>
      <Row id="MinimumMessageInterval" label="minimumMessageInterval" hint="0–3600 s"><NumberField value={m.MinimumMessageInterval} min={0} max={3600} onChange={v => set("MinimumMessageInterval", v)} /></Row>
      <Row id="HelpType" label="helpType" hint="Help-system verbosity">
        <Combo
          width={160}
          value={m.HelpType}
          onChange={v => set("HelpType", v)}
          options={[
            { value: 0, label: "0 — Off" },
            { value: 1, label: "1 — Standard" },
            { value: 2, label: "2 — Verbose" },
          ]}
        />
      </Row>

      <SubHead>Interface — scrolling</SubHead>
      <Row id="MouseScrollThreshold" label="mouseScrollThreshold" hint="0–50"><NumberField value={m.MouseScrollThreshold} min={0} max={50} onChange={v => set("MouseScrollThreshold", v)} /></Row>
      <Row id="MouseScrollDelay"     label="mouseScrollDelay"     hint="0–10"><NumberField value={m.MouseScrollDelay}     min={0} max={10} onChange={v => set("MouseScrollDelay", v)} /></Row>
      <Row id="MouseScrollX" label="mouseScrollX" hint="1–200"><NumberField value={m.MouseScrollX} min={1} max={200} onChange={v => set("MouseScrollX", v)} /></Row>
      <Row id="MouseScrollY" label="mouseScrollY" hint="1–200"><NumberField value={m.MouseScrollY} min={1} max={200} onChange={v => set("MouseScrollY", v)} /></Row>
      <Row id="KeyScrollX" label="keyScrollX" hint="1–200"><NumberField value={m.KeyScrollX} min={1} max={200} onChange={v => set("KeyScrollX", v)} /></Row>
      <Row id="KeyScrollY" label="keyScrollY" hint="1–200"><NumberField value={m.KeyScrollY} min={1} max={200} onChange={v => set("KeyScrollY", v)} /></Row>
    </>
  );
}

function AdvancedSection({ m, set }) {
  return (
    <>
      <SubHead>Quality</SubHead>
      <Row id="Level" label="level" hint="Quality preset">
        <Combo
          width={150}
          value={m.Level}
          onChange={v => set("Level", v)}
          options={[
            { value: 0, label: "0 — Total Quality" },
            { value: 1, label: "1 — Quality" },
            { value: 2, label: "2 — Balance" },
            { value: 3, label: "3 — Speed" },
            { value: 4, label: "4 — Paused" },
          ]}
        />
      </Row>
      <Row id="LoadHalfAnims" label="loadHalfAnims"><Checkbox label="Reduced-detail animations" checked={m.LoadHalfAnims} onChange={v => set("LoadHalfAnims", v)} /></Row>

      <SubHead>Drop quality during…</SubHead>
      <Row id="Drag"   label="drag"><Checkbox label="Dragging objects" checked={m.Drag}   onChange={v => set("Drag", v)} /></Row>
      <Row id="Click"  label="click"><Checkbox label="Click operations" checked={m.Click}  onChange={v => set("Click", v)} /></Row>
      <Row id="Normal" label="normal"><Checkbox label="Normal play" checked={m.Normal} onChange={v => set("Normal", v)} /></Row>

      <SubHead>Sound</SubHead>
      <Row id="Use8BitSound" label="use8BitSound"><Checkbox label="Force 8-bit audio output" checked={m.Use8BitSound} onChange={v => set("Use8BitSound", v)} /></Row>
    </>
  );
}

function AiSection({ m, set }) {
  return (
    <>
      <SubHead>Guests</SubHead>
      <Row id="MaxGuests" label="maxGuests" hint="1–10000"><NumberField value={m.MaxGuests} min={1} max={10000} step={50} onChange={v => set("MaxGuests", v)} width={100} /></Row>
      <Note>
        Increasing this beyond the stock 1,000 limit can cause AI pathing to thrash on the original engine.
        Values above ~2,500 are not recommended without the community AI patch.
      </Note>
    </>
  );
}

function DebugSection({ m, set }) {
  return (
    <>
      <SubHead>Logging</SubHead>
      <Row id="LogCutoff"    label="logCutoff"    hint="0 verbose ↔ 5 silent"><NumberField value={m.LogCutoff} min={0} max={5} onChange={v => set("LogCutoff", v)} /></Row>
      <Row id="SendLogfile"  label="sendLogfile"><Checkbox label="Write zoo.log" checked={m.SendLogfile}  onChange={v => set("SendLogfile",  v)} /></Row>
      <Row id="SendDebugger" label="sendDebugger"><Checkbox label="OutputDebugString" checked={m.SendDebugger} onChange={v => set("SendDebugger", v)} /></Row>

      <SubHead>FPS overlay</SubHead>
      <Row id="DrawFps"  label="drawfps"><Checkbox label="Show FPS counter" checked={m.DrawFps} onChange={v => set("DrawFps", v)} /></Row>
      <Row id="DrawFpsX" label="drawfpsx" hint="0–16384"><NumberField value={m.DrawFpsX} min={0} max={16384} onChange={v => set("DrawFpsX", v)} /></Row>
      <Row id="DrawFpsY" label="drawfpsy" hint="0–16384"><NumberField value={m.DrawFpsY} min={0} max={16384} onChange={v => set("DrawFpsY", v)} /></Row>
    </>
  );
}

function LanguageSection({ m, set }) {
  const langs = [
    { value: "9-1",  label: "English (US)" },
    { value: "9-2",  label: "English (UK)" },
    { value: "7-1",  label: "German (Germany)" },
    { value: "12-1", label: "French (France)" },
    { value: "10-3", label: "Spanish (Modern)" },
    { value: "16-1", label: "Italian (Italy)" },
    { value: "17-1", label: "Japanese" },
    { value: "22-1", label: "Portuguese (Brazil)" },
    { value: "19-1", label: "Dutch (Netherlands)" },
    { value: "29-1", label: "Swedish (Sweden)" },
  ];
  const key = `${m.Lang}-${m.SubLang}`;
  const matched = langs.some(o => o.value === key);

  return (
    <>
      <SubHead>Language</SubHead>
      <Row id="Language" label="lang / sublang" hint="Curated LANGID + SUBLANGID">
        <Combo
          width={220}
          value={matched ? key : ""}
          onChange={v => { const [l, s] = v.split("-").map(Number); set("Lang", l); set("SubLang", s); }}
          options={[{ value: "", label: "(custom)" }, ...langs]}
        />
      </Row>
      <Row label="lang"    hint="Windows LANGID 0–65535"><NumberField value={m.Lang}    min={0} max={65535} onChange={v => set("Lang", v)} /></Row>
      <Row label="sublang" hint="Windows SUBLANGID 0–65535"><NumberField value={m.SubLang} min={0} max={65535} onChange={v => set("SubLang", v)} /></Row>
    </>
  );
}

function MapSection({ m, set }) {
  return (
    <>
      <SubHead>New zoo dimensions</SubHead>
      <Row id="MapX" label="mapX" hint="1–128 tiles wide"><NumberField value={m.MapX} min={1} max={128} onChange={v => set("MapX", v)} /></Row>
      <Row id="MapY" label="mapY" hint="1–128 tiles tall"><NumberField value={m.MapY} min={1} max={128} onChange={v => set("MapY", v)} /></Row>
      <Note>
        Default dimensions for new zoos. The ZT1 engine renders one tile per ground square; doubling these
        values quadruples world memory at runtime — large maps may stutter on the original engine.
      </Note>
    </>
  );
}

// ---- [scenario] ------------------------------------------------------------
const SCENARIO_TIERS = [
  { tier: "Tutorial", scenarios: [
    { key: "tu", label: "Tutorial" },
  ]},
  { tier: "Beginner", scenarios: [
    { key: "ag", label: "African Adventure" },
    { key: "bz", label: "Breeding Zoo" },
    { key: "sm", label: "Small Zoo" },
    { key: "kz", label: "Kid's Zoo" },
  ]},
  { tier: "Intermediate", scenarios: [
    { key: "ay", label: "Australian Outback" },
    { key: "sa", label: "South American Adventure" },
    { key: "ek", label: "Education Zoo" },
    { key: "fz", label: "Fashion Zoo" },
  ]},
  { tier: "Advanced", scenarios: [
    { key: "ar", label: "Arctic Adventure" },
    { key: "tg", label: "Tropical Garden" },
    { key: "mr", label: "Marine Reserve" },
    { key: "ds", label: "Desert Sanctuary" },
  ]},
  { tier: "Expert", scenarios: [
    { key: "co", label: "Conservation" },
    { key: "rs", label: "Rescue Mission" },
    { key: "es", label: "Endangered Species" },
  ]},
];

function ScenarioSection({ m, set }) {
  const toggle = (key) => {
    const next = { ...m.completed, [key]: !m.completed[key] };
    set("completed", next);
  };
  return (
    <>
      <SubHead>Campaign completion</SubHead>
      <Note>
        Each key is a two-letter scenario code. Toggling a checkbox writes <span className="mono">0</span> (complete) or
        <span className="mono"> 1</span> (locked) on save. Mapping verified via Phase 0 research; unknown keys round-trip untouched.
      </Note>
      <div style={{ marginTop: 10, display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: "10px 22px" }}>
        {SCENARIO_TIERS.map(tier => (
          <div key={tier.tier}>
            <div style={{ fontWeight: "bold", marginBottom: 4, fontSize: 11, color: "#000080" }}>{tier.tier}</div>
            <div style={{ display: "flex", flexDirection: "column", gap: 2, paddingLeft: 2 }}>
              {tier.scenarios.map(s => (
                <Checkbox
                  key={s.key}
                  checked={!!m.completed[s.key]}
                  onChange={() => toggle(s.key)}
                  label={<><span className="mono dk-c-dim" style={{ fontSize: 11 }}>{s.key}</span>  {s.label}</>}
                />
              ))}
            </div>
          </div>
        ))}
      </div>
    </>
  );
}

// ============================================================================
// Default model (matches each Ref submodel's property initialisers).
// ============================================================================

const DEFAULT_MODEL = {
  user: {
    Fullscreen: true,
    ScreenWidth: 800,
    ScreenHeight: 600,
    UpdateRate: 15,
    DrawRate: 60,
  },
  ui: {
    NoMenuMusic: false,
    MenuMusic: "sounds/mainmenu.wav",
    MenuMusicAttenuation: 1500,
    UserAttenuation: 0,
    PlayMovie: false,
    MovieVolume1: -1000,
    PlaySecondMovie: false,
    MovieVolume2: -1000,
    StartingCash: 70000,
    CashIncrement: 5000,
    MinCash: 10000,
    MaxCash: 500000,
    UseAlternateCursors: false,
    TooltipDelay: 1,
    TooltipDuration: 3000,
    MessageDisplay: true,
    MouseScrollThreshold: 1,
    MouseScrollDelay: 1,
    MouseScrollX: 27,
    MouseScrollY: 27,
    KeyScrollX: 64,
    KeyScrollY: 64,
    MinimumMessageInterval: 60,
    HelpType: 1,
  },
  advanced: { Level: 2, LoadHalfAnims: false, Drag: false, Click: false, Normal: false, Use8BitSound: false },
  ai: { MaxGuests: 1000 },
  debug: { DrawFps: false, DrawFpsX: 720, DrawFpsY: 20, LogCutoff: 1, SendLogfile: true, SendDebugger: true },
  language: { Lang: 9, SubLang: 1 },
  map: { MapX: 75, MapY: 75 },
  scenario: { completed: { tu: true, ag: true, bz: false, sm: true, kz: false, ay: false, sa: false, ek: false, fz: false, ar: false, tg: false, mr: false, ds: false, co: false, rs: false, es: false } },
};

Object.assign(window, { IniConfigTab });
