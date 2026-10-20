/* global React, W95Window, Btn, GroupBox, Checkbox, MenuBar, StatusBar, Tabs, ProgressBar, Field, Icons, DataGrid, Combo */
/* eslint-disable no-undef */

const { useState, useEffect } = React;

// ============================================================================
// The launcher's main window. Wraps the title bar + menu + tabs + status bar
// and switches inside the General tab between five top-level states.
// ============================================================================

function MainWindow({
  state, // "looking" | "ready" | "cannot" | "noinstall" | "open"
  activeTab,
  setActiveTab,
  onMenuCommand,
  titleIcon,
  installation,
  pickerInstallations,
  pickerSelectedId,
  setPickerSelectedId,
  onLaunch,
  onAddInstallation,
  onOpenInstallation,
  onClose,
  exeCompatOk,
}) {
  const menu = [
  {
    id: "file", label: "File", accel: "F",
    items: [
    { label: "Open Installation…", cmd: "open-install", accel: "O" },
    { label: "Installation Manager…", cmd: "manage", accel: "M" },
    { label: "Close Installation", cmd: "close-install", accel: "C" },
    { kind: "sep" },
    { label: "Settings", cmd: "settings", accel: "S" },
    { kind: "sep" },
    { label: "Exit", cmd: "exit", accel: "x", accelText: "Alt+F4" }]

  },
  {
    id: "help", label: "Help", accel: "H",
    items: [
    { label: "Contents", cmd: "help", accel: "C", accelText: "F1" },
    { kind: "sep" },
    { label: "About", cmd: "about", accel: "A" }]

  }];


  const profileLabel = installation?.name ?? "—";

  return (
    <W95Window
      title="Zoo Tycoon Launcher V1.0.0"
      icon={titleIcon
        ? <img src={titleIcon} width={16} height={16} alt="" draggable={false} style={{ imageRendering: "pixelated", display: "block" }} />
        : <Icons.AppMark size={16} />}
      width={720}
      height={660}
      onClose={onClose}
      className="main-window">
      
      <MenuBar items={menu} onCommand={onMenuCommand} />

      <div style={{ padding: "4px 6px 0 6px" }}>
        <Tabs
          value={activeTab}
          onChange={setActiveTab}
          items={[
          { id: "general", label: "General", accel: "G" },
          { id: "ini", label: "INI Config", accel: "I" }]
          }>
          
          {activeTab === "general" ?
          <GeneralTab
            state={state}
            installation={installation}
            pickerInstallations={pickerInstallations}
            pickerSelectedId={pickerSelectedId}
            setPickerSelectedId={setPickerSelectedId}
            onLaunch={onLaunch}
            onAddInstallation={onAddInstallation}
            onOpenInstallation={onOpenInstallation}
            exeCompatOk={exeCompatOk} /> :


          <IniConfigTab state={state} />
          }
        </Tabs>
      </div>
      
      <div style={{ padding: "6px 6px 4px 6px" }}>
        <StatusBar cells={statusCellsFor(state, installation, activeTab)} />
      </div>

      <div style={{ padding: "1px 8px 3px 8px" }}>
        <span className="muted" style={{ fontSize: 11 }}>For Help, press F1</span>
      </div>
    </W95Window>);

}

function statusCellsFor(state, installation, activeTab) {
  switch (state) {
    case "looking":
      return [
      { text: "Discovering installations…" },
      { text: "Reading registry…", fixed: true, style: { minWidth: 160 } },
      { text: "v1.0.0", fixed: true }];

    case "ready":
      return [
      { text: `Ready — ${installation?.name ?? ""}` },
      { text: activeTab === "ini" ? "INI loaded" : "Display: 1920×1080", fixed: true, style: { minWidth: 160 } },
      { text: "v1.0.0", fixed: true }];

    case "cannot":
      return [
      { text: "Cannot launch — installation invalid" },
      { text: "Fix required", fixed: true, style: { minWidth: 160, fontWeight: "bold" }, className: "dk-c-maroon" },
      { text: "v1.0.0", fixed: true }];

    case "noinstall":
      return [
      { text: "No installation registered" },
      { text: "", fixed: true, style: { minWidth: 160 } },
      { text: "v1.0.0", fixed: true }];

    case "open":
      return [
      { text: "Choose an installation to open" },
      { text: "", fixed: true, style: { minWidth: 160 } },
      { text: "v1.0.0", fixed: true }];

    default:
      return [{ text: "" }, { text: "", fixed: true }, { text: "v1.0.0", fixed: true }];
  }
}

// ============================================================================
// The General tab routes between the five wireframe states.
// ============================================================================

function GeneralTab(props) {
  switch (props.state) {
    case "looking":return <StateLooking />;
    case "ready":return <StateReady {...props} />;
    case "cannot":return <StateCannot {...props} />;
    case "noinstall":return <StateNoInstall {...props} />;
    case "open":return <StateOpen {...props} />;
    default:return null;
  }
}

// ---- Looking for Zoo Tycoon ------------------------------------------------
function StateLooking() {
  const [phase, setPhase] = useState(0);
  useEffect(() => {
    const id = setInterval(() => setPhase((p) => p + 1), 200);
    return () => clearInterval(id);
  }, []);
  const phases = [
  "Loading Launcher.db…",
  "Reading launcher settings…",
  "Querying registry: HKLM\\…\\Zoo Tycoon\\1.0",
  "Probing C:\\Program Files (x86)\\Microsoft Games…",
  "Verifying zoo.exe…",
  "Verifying zoo.ini…",
  "Parsing INI structure…"];

  const message = phases[phase % phases.length];

  return (
    <div style={{ minHeight: 280 }}>
      <GroupBox label="Status">
        <div style={{ display: "flex", alignItems: "center", gap: 12, padding: "8px 4px" }}>
          <Icons.Magnifier size={32} />
          <div style={{ flex: 1 }}>
            <div style={{ marginBottom: 10, fontWeight: "bold" }}>Looking for Zoo Tycoon…</div>
            <ProgressBar marquee width={300} />
            <div className="muted" style={{ marginTop: 8 }}>{message}</div>
          </div>
        </div>
      </GroupBox>

      <div style={{ height: 160 }} />
    </div>);

}

// ---- Ready to Play ---------------------------------------------------------
function StateReady({ installation, onLaunch, exeCompatOk }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <span>Installation Profile:</span>
        <span style={{ fontWeight: "bold" }}>{installation?.name ?? "—"}</span>
      </div>

      <GroupBox label="Status">
        <div style={{ display: "flex", gap: 14, alignItems: "flex-start" }}>
          <Icons.OkCheck size={36} />
          <div style={{ flex: 1 }}>
            <div style={{ fontSize: 14, fontWeight: "bold", marginBottom: 6 }}>Ready to Play</div>
            <table style={{ borderCollapse: "collapse" }}>
              <tbody>
                <tr><td style={{ paddingRight: 12 }}>EXE:</td><td><span className="badge--ok">Found</span></td></tr>
                <tr><td style={{ paddingRight: 12 }}>INI:</td><td><span className="badge--ok">Found</span></td></tr>
                <tr><td style={{ paddingRight: 12 }}>Path:</td><td className="mono" style={{ fontSize: 11 }}>{installation?.path}</td></tr>
              </tbody>
            </table>
          </div>
          <div>
            <Btn isDefault wide style={{ height: 30, fontWeight: "bold" }} onClick={onLaunch}>Launch Game</Btn>
          </div>
        </div>
      </GroupBox>

      <GroupBox label="Display">
        <div style={{ display: "flex", gap: 14, alignItems: "flex-start" }}>
          <Icons.Monitor size={32} />
          <div style={{ flex: 1 }}>
            <table style={{ borderCollapse: "collapse", width: "100%" }}>
              <tbody>
                <tr>
                  <td style={{ paddingRight: 14 }}>Current resolution:</td>
                  <td className="mono">1920 × 1080 · 32-bit · 60 Hz</td>
                </tr>
                <tr>
                  <td>Adapter:</td>
                  <td className="mono" style={{ fontSize: 11 }}>\\.\DISPLAY1 — Primary</td>
                </tr>
                <tr>
                  <td>Available screen modes:</td>
                  <td><span className="mono">756</span> reported by Windows</td>
                </tr>
                <tr>
                  <td>Possible ZT1 modes:</td>
                  <td><span className="mono bold dk-c-navy">14</span> compatible (16-bit ≤ 1600×1200)</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </GroupBox>

      <YourSystemSection exeCompatOk={exeCompatOk} />

      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <div style={{ flex: 1 }} />
        <span className="muted">Last played: {installation?.lastPlayedUtc ?? "Never"}</span>
      </div>
    </div>);

}

// ---- Cannot Play -----------------------------------------------------------
function StateCannot({ installation, onMenuCommand, exeCompatOk }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <span>Installation Profile:</span>
        <span style={{ fontWeight: "bold" }}>{installation?.name ?? "—"}</span>
      </div>

      <GroupBox label="Status">
        <div style={{ display: "flex", alignItems: "flex-start", gap: 14 }}>
          <Icons.Warning size={36} />
          <div style={{ flex: 1 }}>
            <div className="dk-c-maroon" style={{ fontSize: 14, fontWeight: "bold", marginBottom: 6 }}>Cannot Play</div>
            <table style={{ borderCollapse: "collapse" }}>
              <tbody>
                <tr><td style={{ paddingRight: 12 }}>EXE:</td><td><span className="badge--ok">Found</span></td></tr>
                <tr><td style={{ paddingRight: 12 }}>INI:</td><td><span className="badge--err">Not found</span></td></tr>
              </tbody>
            </table>
            <div className="muted" style={{ marginTop: 8, lineHeight: 1.4 }}>
              <code style={{ background: "transparent" }}>zoo.ini</code> is missing from this installation's directory. The
              game won't start without it. Use <b>Fix</b> to locate or recreate the file.
            </div>
            <div style={{ marginTop: 10, display: "flex", gap: 6 }}>
              <Btn onClick={() => window.app.openDialog("fix")} accel="F">Fix</Btn>
              <Btn onClick={() => window.app.openDialog("manage")}>Manage Installations</Btn>
            </div>
          </div>
          <div>
            <Btn wide disabled style={{ height: 30 }}>Launch Game</Btn>
          </div>
        </div>
      </GroupBox>

      <GroupBox label="Display">
        <div className="muted" style={{ display: "flex", gap: 14, alignItems: "flex-start" }}>
          <Icons.Monitor size={32} />
          <div style={{ flex: 1 }}>
            <table style={{ borderCollapse: "collapse", width: "100%" }}>
              <tbody>
                <tr>
                  <td style={{ paddingRight: 14 }}>Current resolution:</td>
                  <td className="mono">1920 × 1080 · 32-bit · 60 Hz</td>
                </tr>
                <tr>
                  <td>Adapter:</td>
                  <td className="mono" style={{ fontSize: 11 }}>\\.\DISPLAY1 — Primary</td>
                </tr>
                <tr>
                  <td>Available screen modes:</td>
                  <td><span className="mono">756</span> reported by Windows</td>
                </tr>
                <tr>
                  <td>Possible ZT1 modes:</td>
                  <td><span className="mono bold">14</span> compatible (16-bit ≤ 1600×1200)</td>
                </tr>
              </tbody>
            </table>
            <div style={{ marginTop: 6, fontStyle: "italic" }}>
              Display detection succeeded but the game cannot launch until the INI is restored.
            </div>
          </div>
        </div>
      </GroupBox>

      <YourSystemSection exeCompatOk={exeCompatOk} />

      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <div style={{ flex: 1 }} />
        <span className="muted">Last played: {installation?.lastPlayedUtc ?? "Never"}</span>
      </div>
    </div>);

}

// ============================================================================
// Your System
// ============================================================================
function YourSystemSection({ exeCompatOk }) {
  const failing = exeCompatOk ? [] : [
    "Compatibility mode is not set to Windows XP (Service Pack 3).",
    "Reduced colour mode (16-bit / 65,536 colours) is not enabled.",
    "Fullscreen optimisations have not been disabled.",
  ];

  return (
    <GroupBox label="Your System">
      <div style={{ display: "flex", gap: 14, alignItems: "flex-start" }}>
        <Icons.MyComputer size={32} />
        <div style={{ flex: 1 }}>
          <table style={{ borderCollapse: "collapse", width: "100%" }}>
            <tbody>
              <tr>
                <td style={{ paddingRight: 14, whiteSpace: "nowrap", verticalAlign: "top" }}>Operating System:</td>
                <td className="mono" style={{ fontSize: 11 }}>Windows 11 Pro · Version 24H2 · Build 26100</td>
              </tr>
              <tr>
                <td style={{ paddingRight: 14, whiteSpace: "nowrap" }}>Processor:</td>
                <td className="mono" style={{ fontSize: 11 }}>Intel Core i7-12700K · 12C/20T · 3.60 GHz</td>
              </tr>
              <tr>
                <td style={{ paddingRight: 14, whiteSpace: "nowrap" }}>Graphics:</td>
                <td className="mono" style={{ fontSize: 11 }}>NVIDIA GeForce RTX 3080 · 10 GB GDDR6X</td>
              </tr>
              <tr>
                <td style={{ paddingRight: 14, whiteSpace: "nowrap" }}>Memory:</td>
                <td className="mono" style={{ fontSize: 11 }}>32 GB DDR5 · 28.4 GB available</td>
              </tr>
            </tbody>
          </table>

          <div className="etched" style={{ margin: "8px 0" }} />

          {exeCompatOk ? (
            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <Icons.OkCheck size={16} />
              <div>
                <b className="dk-c-green">zoo.exe is configured correctly.</b>
                <span className="muted" style={{ fontSize: 11, marginLeft: 6 }}>Windows XP compatibility, 16-bit colour, and fullscreen optimisations are all set.</span>
              </div>
            </div>
          ) : (
            <div style={{ display: "flex", alignItems: "flex-start", gap: 8 }}>
              <Icons.Warning size={16} />
              <div style={{ flex: 1 }}>
                <div style={{ fontWeight: "bold" }}>zoo.exe is not optimised for this system.</div>
                <div className="muted" style={{ fontSize: 11, margin: "2px 0 5px" }}>
                  The following settings are not configured on <span className="mono">zoo.exe</span>:
                </div>
                <ul style={{ margin: "0 0 7px", paddingLeft: 20, fontSize: 11 }}>
                  {failing.map((msg, i) => <li key={i} className="dk-c-maroon">{msg}</li>)}
                </ul>
                <Btn sm onClick={() => window.app.optimiseExe()} accel="O">Optimise zoo.exe…</Btn>
              </div>
            </div>
          )}
        </div>
      </div>
    </GroupBox>
  );
}

// ---- No Game Installation Found --------------------------------------------
function StateNoInstall({ onAddInstallation }) {
  return (
    <div style={{ minHeight: 280, display: "flex", flexDirection: "column", gap: 10 }}>
      <GroupBox label="Status">
        <div style={{ display: "flex", alignItems: "flex-start", gap: 14 }}>
          <Icons.Error size={36} />
          <div style={{ flex: 1, lineHeight: 1.45 }}>
            <div style={{ fontSize: 14, fontWeight: "bold", marginBottom: 6 }}>No Game Installation Found</div>
            <div className="muted">
              Auto-locate searched the Windows registry, <span className="mono">Program Files</span>, and the
              previously-known directory but did not find <span className="mono">zoo.exe</span>. Add an installation
              by pointing the launcher at the folder containing the game.
            </div>
          </div>
          <div>
            <Btn isDefault wide onClick={onAddInstallation} accel="A">Add Installation</Btn>
          </div>
        </div>
      </GroupBox>

      <GroupBox label="Auto-locate trail">
        <table className="mono" style={{ fontSize: 11, width: "100%" }}>
          <tbody>
            <tr><td style={{ width: 24 }}>×</td><td>HKLM\SOFTWARE\Microsoft\Microsoft Games\Zoo Tycoon\1.0</td><td className="muted">no value</td></tr>
            <tr><td>×</td><td>C:\Program Files (x86)\Microsoft Games\Zoo Tycoon\</td><td className="muted">directory missing</td></tr>
            <tr><td>×</td><td>C:\Program Files\Microsoft Games\Zoo Tycoon\</td><td className="muted">directory missing</td></tr>
            <tr><td>×</td><td>Last-known path (LauncherSettings)</td><td className="muted">empty</td></tr>
          </tbody>
        </table>
      </GroupBox>

      <div style={{ flex: 1 }} />
    </div>);

}

// ---- Open Game Installation (picker) ---------------------------------------
function StateOpen({ pickerInstallations, pickerSelectedId, setPickerSelectedId, onOpenInstallation }) {
  return (
    <div style={{ minHeight: 280, display: "flex", flexDirection: "column", gap: 10 }}>
      <GroupBox label="Status">
        <div style={{ display: "flex", alignItems: "center", gap: 14 }}>
          <Icons.Folder size={32} />
          <div style={{ flex: 1 }}>
            <div style={{ fontWeight: "bold", marginBottom: 4 }}>Open Game Installation</div>
            <div className="muted">
              Startup preference is set to <span className="mono">NoInstallation</span>. Choose an installation
              below and click <b>Open</b>, or add a new one.
            </div>
          </div>
        </div>
      </GroupBox>

      <DataGrid
        height={140}
        selectedId={pickerSelectedId}
        onSelect={(id, dbl) => {setPickerSelectedId(id);if (dbl) onOpenInstallation(id);}}
        columns={[
        { key: "name", label: "Name", width: "30%",
          render: (r) => <span style={{ fontWeight: r.isDefault ? "bold" : "normal" }}>{r.name}</span> },
        { key: "path", label: "Path", width: "55%",
          render: (r) => <span className="mono" style={{ fontSize: 11 }}>{r.path}</span> },
        { key: "status", label: "Status", width: "15%",
          render: (r) => r.valid ?
          <span className="badge--ok">Valid</span> :
          <span className="badge--err">{r.invalidReason}</span> }]
        }
        rows={pickerInstallations} />
      

      <div style={{ display: "flex", gap: 6, marginTop: 4 }}>
        <Btn onClick={() => window.app.openDialog("add")} accel="A">Add</Btn>
        <Btn onClick={() => window.app.openDialog("info")} disabled={!pickerSelectedId} accel="I">Info</Btn>
        <Btn onClick={() => window.app.openDialog("manage")} accel="M">Manage</Btn>
        <div style={{ flex: 1 }} />
        <Btn isDefault wide onClick={() => onOpenInstallation(pickerSelectedId)} disabled={!pickerSelectedId} accel="O">Open</Btn>
      </div>
    </div>);

}

Object.assign(window, { MainWindow });