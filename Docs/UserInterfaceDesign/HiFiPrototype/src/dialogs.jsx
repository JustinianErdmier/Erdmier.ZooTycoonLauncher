/* global React, W95Window, Btn, GroupBox, Checkbox, TextField, Field, DataGrid, Icons */
/* eslint-disable no-undef */

const { useState } = React;

// ============================================================================
// Modal overlay wrapper — drops a window into a centered overlay above the
// main window. (Not a system-modal — visually, the win95 chrome and
// dimming-of-titlebar carries the affordance.)
// ============================================================================

function Modal({ children, onBackdrop }) {
  return (
    <div className="overlay" onMouseDown={(e) => {if (e.target.classList.contains("overlay")) onBackdrop && onBackdrop();}}>
      {children}
    </div>);

}

// ============================================================================
// Installation Manager
// ============================================================================
function ManagementDialog({
  installations, defaultId, onClose, onAdd, onEdit, onDelete, onFix, onInfo
}) {
  const [selectedId, setSelectedId] = useState(
    installations.length ? defaultId || installations[0].id : null
  );

  // Sort: default first, then alphabetically (case-insensitive)
  const sorted = [...installations].sort((a, b) => {
    if (a.id === defaultId) return -1;
    if (b.id === defaultId) return 1;
    return a.name.localeCompare(b.name);
  });

  const selected = sorted.find((r) => r.id === selectedId);
  const selectedIsInvalid = selected && !selected.valid;

  return (
    <Modal onBackdrop={onClose}>
      <W95Window
        title="Installation Manager"
        icon={<Icons.AppMark size={16} />}
        width={580}
        onClose={onClose}>
        
        <div style={{ padding: 12, display: "flex", gap: 10 }}>
          <div style={{ flex: 1 }}>
            <DataGrid
              height={240}
              selectedId={selectedId}
              onSelect={setSelectedId}
              columns={[
              {
                key: "name", label: "Name", width: "35%",
                render: (r) =>
                <span style={{ fontWeight: r.id === defaultId ? "bold" : "normal" }}>
                      {r.name}{r.id === defaultId && <span className="muted" style={{ fontWeight: "normal" }}>  · default</span>}
                    </span>

              },
              {
                key: "path", label: "Path", width: "45%",
                render: (r) => <span className="mono" style={{ fontSize: 11 }}>{r.path}</span>
              },
              {
                key: "status", label: "Status", width: "20%",
                render: (r) => r.valid ?
                <span className="badge--ok">Valid</span> :
                <span className="badge--err">{r.invalidReason}</span>
              }]
              }
              rows={sorted} />
            
            <div className="muted" style={{ marginTop: 6, fontSize: 11 }}>
              {sorted.length} installation{sorted.length === 1 ? "" : "s"} registered. Double-click a row to open.
            </div>
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: 4, width: 90 }}>
            <Btn sm onClick={onAdd} accel="A">Add</Btn>
            <Btn sm onClick={() => onInfo(selectedId)} disabled={!selectedId} accel="I">Info</Btn>
            <Btn sm onClick={() => onEdit(selectedId)} disabled={!selectedId} accel="E">Edit</Btn>
            <Btn sm onClick={() => onDelete(selectedId)} disabled={!selectedId} accel="D">Delete</Btn>
            <div style={{ height: 8 }} />
            <Btn sm onClick={() => onFix(selectedId)} disabled={!selectedIsInvalid} accel="F">Fix</Btn>
          </div>
        </div>

        <div style={{ padding: "0 12px 12px 12px", display: "flex", justifyContent: "flex-end" }}>
          <Btn isDefault onClick={onClose} accel="C">Close</Btn>
        </div>
      </W95Window>
    </Modal>);

}

// ============================================================================
// Add / Edit Installation
// ============================================================================
function AddEditDialog({ mode, initial, existingNames, hasInstallations, onSave, onCancel }) {
  const [name, setName] = useState(initial?.name ?? "");
  const [path, setPath] = useState(initial?.path ?? "");
  const [markDefault, setMarkDefault] = useState(initial?.isDefault ?? !hasInstallations);

  const trimmed = name.trim();
  const nameTaken = trimmed && existingNames.some((n) => n.toLowerCase() === trimmed.toLowerCase() && n !== initial?.name);
  const pathEmpty = !path.trim();
  const canSave = trimmed.length > 0 && !nameTaken && !pathEmpty;

  return (
    <Modal onBackdrop={onCancel}>
      <W95Window
        title={mode === "edit" ? "Edit Installation" : "Add Installation"}
        icon={<Icons.AppMark size={16} />}
        width={420}
        onClose={onCancel}>
        
        <div style={{ padding: 14 }}>
          <div style={{ display: "flex", alignItems: "flex-start", gap: 12, marginBottom: 12 }}>
            <Icons.FolderLarge size={32} />
            <div style={{ flex: 1 }}>
              <div style={{ fontWeight: "bold", marginBottom: 2 }}>
                {mode === "edit" ? "Edit installation" : "New installation"}
              </div>
              <div className="muted">
                {mode === "edit" ?
                "Rename this installation or change which one is the launcher's default." :
                "Give this installation a name. The folder you selected is shown below."}
              </div>
            </div>
          </div>

          <Field label="Name:" labelWidth={70}>
            <TextField
              value={name}
              onChange={setName}
              placeholder={hasInstallations ? `Installation ${existingNames.length + 1}` : "Main"}
              invalid={nameTaken}
              width="100%" />
            
          </Field>

          <Field label="Folder:" labelWidth={70} align="flex-start">
            <div style={{ display: "flex", gap: 6 }}>
              <TextField value={path} onChange={setPath} placeholder="C:\Program Files (x86)\Microsoft Games\Zoo Tycoon" width="100%" mono />
              <Btn sm xs onClick={() => setPath("C:\\Program Files (x86)\\Microsoft Games\\Zoo Tycoon")}>Browse</Btn>
            </div>
          </Field>

          <div style={{ marginLeft: 78, marginTop: 6 }}>
            <Checkbox
              label="Mark as default installation"
              checked={markDefault}
              onChange={setMarkDefault}
              disabled={!hasInstallations && mode === "add"} />
            
            {!hasInstallations && mode === "add" &&
            <div className="muted" style={{ fontSize: 11, marginTop: 4 }}>
                The first installation is automatically the launcher's default.
              </div>
            }
            {nameTaken &&
            <div className="dk-c-red" style={{ fontSize: 11, marginTop: 4 }}>
                An installation named “{trimmed}” already exists.
              </div>
            }
          </div>

          <div style={{ display: "flex", justifyContent: "flex-end", gap: 6, marginTop: 16 }}>
            <Btn isDefault disabled={!canSave} onClick={() => onSave({ name: trimmed, path, isDefault: markDefault })} accel="S">
              {mode === "edit" ? "Save" : "Save"}
            </Btn>
            <Btn onClick={onCancel} accel="C">Cancel</Btn>
          </div>
        </div>
      </W95Window>
    </Modal>);

}

// ============================================================================
// Fix Installation
// ============================================================================
function FixDialog({ installation, onClose, onLocateExe, onCreateIni }) {
  const hasExeProblem = !installation?.hasExe;
  const hasIniProblem = !installation?.hasIni;

  return (
    <Modal onBackdrop={onClose}>
      <W95Window
        title="Fix Installation"
        icon={<Icons.AppMark size={16} />}
        width={460}
        onClose={onClose}>
        
        <div style={{ padding: 14, display: "flex", flexDirection: "column", gap: 12 }}>
          <div className="muted">
            Recover an installation by locating missing files or restoring a fresh INI. The launcher
            never patches <span className="mono">zoo.exe</span>.
          </div>

          <GroupBox label="Fix EXE">
            <div style={{ display: "flex", alignItems: "flex-start", gap: 12 }}>
              {hasExeProblem ?
              <Icons.MailCross size={32} /> :
              <Icons.MailCheck size={32} />}
              <div style={{ flex: 1, lineHeight: 1.45 }}>
                {hasExeProblem ?
                <>
                    <div className="dk-c-red" style={{ fontWeight: "bold" }}>No EXE found!</div>
                    <div className="muted">
                      <span className="mono">zoo.exe</span> is missing from this installation's directory.
                      Browse to a folder that contains <span className="mono">zoo.exe</span>.
                    </div>
                  </> :

                <>
                    <div className="dk-c-green" style={{ fontWeight: "bold" }}>EXE present</div>
                    <div className="muted"><span className="mono">zoo.exe</span> was located at the expected path.</div>
                  </>
                }
              </div>
              <Btn onClick={onLocateExe} disabled={!hasExeProblem} accel="L">Locate</Btn>
            </div>
          </GroupBox>

          <GroupBox label="Fix INI">
            <div style={{ display: "flex", alignItems: "flex-start", gap: 12 }}>
              {hasIniProblem ?
              <Icons.MailCross size={32} /> :
              <Icons.MailCheck size={32} />}
              <div style={{ flex: 1, lineHeight: 1.45 }}>
                {hasIniProblem ?
                <>
                    <div className="dk-c-amber" style={{ fontWeight: "bold" }}>No INI found</div>
                    <div className="muted">
                      A fresh <span className="mono">zoo.ini</span> will be created using the launcher's
                      built-in defaults. Any prior values can be restored later from history.
                    </div>
                  </> :

                <>
                    <div className="dk-c-green" style={{ fontWeight: "bold" }}>INI present</div>
                    <div className="muted"><span className="mono">zoo.ini</span> parsed cleanly.</div>
                  </>
                }
              </div>
              <Btn onClick={onCreateIni} disabled={!hasIniProblem} accel="C">Create</Btn>
            </div>
          </GroupBox>

          <div style={{ display: "flex", justifyContent: "flex-end", gap: 6 }}>
            <Btn isDefault onClick={onClose} accel="O">OK</Btn>
          </div>
        </div>
      </W95Window>
    </Modal>);

}

// ============================================================================
// Installation Info
// ============================================================================
function InfoDialog({ installation, onClose }) {
  if (!installation) return null;
  return (
    <Modal onBackdrop={onClose}>
      <W95Window
        title="Installation Info"
        icon={<Icons.AppMark size={16} />}
        width={420}
        onClose={onClose}>
        
        <div style={{ padding: 14, display: "flex", gap: 12 }}>
          <Icons.Info size={32} />
          <div style={{ flex: 1 }}>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
              <tbody>
                <InfoRow label="Name" value={installation.name} bold />
                <InfoRow label="Path" value={<span className="mono" style={{ fontSize: 11 }}>{installation.path}</span>} />
                <InfoRow label="Status" value={installation.valid ?
                <span className="badge--ok">Valid</span> :
                <span className="badge--err">{installation.invalidReason}</span>} />
                <InfoRow label="Default" value={installation.isDefault ? "Yes" : "No"} />
                <InfoRow label="Added" value={installation.addedUtc} />
                <InfoRow label="Last opened" value={installation.lastOpenedUtc || "—"} />
                <InfoRow label="Last played" value={installation.lastPlayedUtc || "—"} />
                <InfoRow label="History entries" value={`${installation.historyCount ?? 0} snapshots`} />
              </tbody>
            </table>
          </div>
        </div>

        <div style={{ padding: "0 14px 14px 14px", display: "flex", justifyContent: "flex-end" }}>
          <Btn isDefault onClick={onClose} accel="C">Close</Btn>
        </div>
      </W95Window>
    </Modal>);

}

function InfoRow({ label, value, bold }) {
  return (
    <tr>
      <td className="dk-c-dim" style={{ paddingRight: 12, paddingBottom: 4, whiteSpace: "nowrap", verticalAlign: "top" }}>{label}:</td>
      <td style={{ paddingBottom: 4, fontWeight: bold ? "bold" : "normal" }}>{value}</td>
    </tr>);

}

// ============================================================================
// Delete confirmation
// ============================================================================
function ConfirmDeleteDialog({ installation, willPromoteName, onConfirm, onCancel }) {
  if (!installation) return null;
  return (
    <Modal onBackdrop={onCancel}>
      <W95Window
        title="Delete Installation"
        icon={<Icons.AppMark size={16} />}
        width={400}
        onClose={onCancel}>
        
        <div style={{ padding: 16, display: "flex", gap: 12 }}>
          <Icons.Warning size={32} />
          <div style={{ flex: 1 }}>
            <div style={{ marginBottom: 6 }}>
              Remove <b>{installation.name}</b> from the launcher's registry?
            </div>
            <div className="muted" style={{ lineHeight: 1.45 }}>
              The launcher's database for this installation, including INI history, will be deleted. The
              game folder on disk is left untouched.
            </div>
            {willPromoteName &&
            <div style={{ marginTop: 8 }}>
                <b>{willPromoteName}</b> will be promoted to default.
              </div>
            }
          </div>
        </div>

        <div style={{ padding: "0 14px 14px 14px", display: "flex", justifyContent: "flex-end", gap: 6 }}>
          <Btn onClick={onConfirm} accel="D">Delete</Btn>
          <Btn isDefault onClick={onCancel} accel="C">Cancel</Btn>
        </div>
      </W95Window>
    </Modal>);

}

// ============================================================================
// About
// ============================================================================
function AboutDialog({ onClose }) {
  return (
    <Modal onBackdrop={onClose}>
      <W95Window
        title="About Zoo Tycoon Launcher"
        icon={<Icons.AppMark size={16} />}
        width={430}
        onClose={onClose}>
        
        {/* Header — icon + name + version */}
        <div style={{ padding: "16px 16px 12px", display: "flex", gap: 14 }}>
          <Icons.AppLogo size={64} />
          <div style={{ flex: 1, lineHeight: 1.5 }}>
            <div style={{ fontWeight: "bold", fontSize: 13 }}>Zoo Tycoon Launcher</div>
            <div>Version 1.0.0 (build 2026.05.26)</div>
            <div className="muted" style={{ fontSize: 11 }}>A native Windows launcher and configuration tool for Zoo Tycoon (2001).</div>
          </div>
        </div>

        <div style={{ padding: "0 16px" }}><div className="etched" /></div>

        {/* Personal message */}
        <div style={{ padding: "10px 16px 0", fontSize: 11, lineHeight: 1.65 }}>
          <p style={{ margin: "0 0 8px" }}>
            Zoo Tycoon (2001) was the first video game I ever played — it has held a very special place in my heart ever since.
            Building such a thorough, feature-rich tool for a game approaching three decades old has been a genuinely wonderful journey,
            and I hope you enjoy using it as much as I enjoyed making it.
          </p>
          <p style={{ margin: "0 0 10px" }}>
            If you would like to support me on Ko-fi, it would mean the world to me. But knowing that even
            one other person cares enough about Zoo Tycoon to use this launcher is already more than enough.
          </p>
          <div className="muted" style={{ textAlign: "right", fontStyle: "italic", marginBottom: 10 }}>— Justinian Erdmier</div>
        </div>

        {/* Ko-fi button */}
        <div style={{ padding: "0 16px 12px", display: "flex", alignItems: "center", gap: 10 }}>
          <img
            src="assets/kofi-button.png"
            alt="Support me on Ko-fi"
            title="Open Ko-fi in your browser"
            style={{ height: 36, width: "auto", cursor: "pointer", imageRendering: "auto", borderRadius: 6 }}
            onClick={() => window.open("https://ko-fi.com", "_blank")} />
          
        </div>

        <div style={{ padding: "0 16px" }}><div className="etched" /></div>

        {/* Main logo */}
        <div style={{ padding: "12px 16px 8px", display: "flex", justifyContent: "center" }}>
          <Icons.AppMainLogo width={356} />
        </div>

        {/* Credits */}
        <div className="muted" style={{ padding: "0 16px 12px", fontSize: 10, textAlign: "center", lineHeight: 1.5 }}>
          Interface icons: <a href="https://github.com/legacy-icons/famfamfam-silk" target="_blank" rel="noreferrer" style={{ color: "#0000cc" }}>FamFamFam Silk</a> by Mark James · Creative Commons Attribution 2.5
        </div>

        <div style={{ padding: "0 14px 14px 14px", display: "flex", justifyContent: "flex-end" }}>
          <Btn isDefault onClick={onClose} accel="O">OK</Btn>
        </div>
      </W95Window>
    </Modal>);

}

// ============================================================================
// Undo / Restore history — bonus dialog from SDD ("INI tab Undo").
// The grid lists past snapshots; selecting one reveals a diff against the
// current state so the user can see exactly what restoring would change.
// ============================================================================
function HistoryDialog({ snapshots, onClose, onRestore }) {
  const [sel, setSel] = useState(snapshots[0]?.id);
  const selected = snapshots.find((s) => s.id === sel);
  const changes = selected?.changes ?? [];

  return (
    <Modal onBackdrop={onClose}>
      <W95Window
        title="Restore Previous INI"
        icon={<Icons.AppMark size={16} />}
        width={620}
        onClose={onClose}>
        
        <div style={{ padding: 12, display: "flex", flexDirection: "column", gap: 8 }}>
          <div className="muted" style={{ lineHeight: 1.4 }}>
            Pick a previous version of <span className="mono">zoo.ini</span> to restore. The current
            version is archived to history before the restore is applied.
          </div>

          <DataGrid
            height={140}
            selectedId={sel}
            onSelect={setSel}
            columns={[
            { key: "capturedUtc", label: "Captured", width: "45%",
              render: (r) => <span className="mono" style={{ fontSize: 11 }}>{r.capturedUtc}</span> },
            { key: "trigger", label: "Trigger", width: "30%" },
            { key: "diff", label: "Δ keys", width: "25%", align: "right",
              render: (r) => <span className="mono">{r.diff}</span> }]
            }
            rows={snapshots} />
          

          <div style={{ display: "flex", alignItems: "baseline", justifyContent: "space-between" }}>
            <span className="muted" style={{ fontSize: 11 }}>
              Diff: values from the selected snapshot replace the current ones on restore.
            </span>
            <span className="muted" style={{ fontSize: 11 }}>
              {changes.length} {changes.length === 1 ? "change" : "changes"}
            </span>
          </div>

          <div className="sunken" style={{ height: 140, overflow: "auto" }}>
            {changes.length === 0 ?
            <div className="muted" style={{ padding: 14, textAlign: "center", fontStyle: "italic" }}>
                No differences — this snapshot matches the current state.
              </div> :

            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 11 }}>
                <thead>
                  <tr className="dk-row-head" style={{ position: "sticky", top: 0 }}>
                    <th style={{ textAlign: "left", padding: "3px 6px", fontWeight: "normal", borderBottom: "1px solid var(--gray)", width: "45%" }}>Key</th>
                    <th style={{ textAlign: "left", padding: "3px 6px", fontWeight: "normal", borderBottom: "1px solid var(--gray)", width: "27.5%" }}>Current</th>
                    <th style={{ textAlign: "left", padding: "3px 6px", fontWeight: "normal", borderBottom: "1px solid var(--gray)", width: "27.5%" }}>Snapshot</th>
                  </tr>
                </thead>
                <tbody>
                  {changes.map((c, i) =>
                <tr key={i} style={{ borderBottom: "1px dotted #e0e0e0" }}>
                      <td className="mono" style={{ padding: "2px 6px", whiteSpace: "nowrap" }}>
                        <span className="muted">[{c.section}]</span>/{c.key}
                      </td>
                      <td className="mono dk-c-maroon" style={{ padding: "2px 6px" }}>{String(c.current)}</td>
                      <td className="mono dk-c-navy" style={{ padding: "2px 6px" }}>{String(c.snapshot)}</td>
                    </tr>
                )}
                </tbody>
              </table>
            }
          </div>

          <div style={{ display: "flex", justifyContent: "flex-end", gap: 6 }}>
            <Btn isDefault onClick={() => onRestore(sel)} disabled={!sel || changes.length === 0} accel="R">Restore</Btn>
            <Btn onClick={onClose} accel="C">Cancel</Btn>
          </div>
        </div>
      </W95Window>
    </Modal>);

}

// ============================================================================
// Settings — File → Settings. Three sections backed by LauncherSettings
// (LauncherStartupPreference + CloseAfterGameLaunch from the SDD) plus a
// launcher theme choice (system / light / dark).
// ============================================================================
function SettingsDialog({ initial, onSave, onCancel }) {
  const [pref, setPref] = useState(initial?.startupPref ?? "DefaultInstallation");
  const [close, setClose] = useState(initial?.closeAfter ?? false);
  const [theme, setTheme] = useState(initial?.theme ?? "system");

  return (
    <Modal onBackdrop={onCancel}>
      <W95Window
        title="Settings"
        icon={<Icons.AppMark size={16} />}
        width={460}
        onClose={onCancel}>
        
        <div style={{ padding: 14, display: "flex", flexDirection: "column", gap: 10 }}>
          <GroupBox label="Startup">
            <div className="muted" style={{ marginBottom: 6, fontSize: 11 }}>
              When the launcher opens, restore:
            </div>
            <div style={{ display: "flex", flexDirection: "column", gap: 3 }}>
              <Radio name="startup" value="DefaultInstallation" current={pref} onChange={setPref} label="The default installation" />
              <Radio name="startup" value="LastPlayedInstallation" current={pref} onChange={setPref} label="The installation I last played" />
              <Radio name="startup" value="LastOpenedInstallation" current={pref} onChange={setPref} label="The installation I last opened in the launcher" />
              <Radio name="startup" value="NoInstallation" current={pref} onChange={setPref} label="No installation — show the picker" />
            </div>
          </GroupBox>

          <GroupBox label="Game launch">
            <Checkbox
              label="Close launcher after the game starts"
              checked={close}
              onChange={setClose} />
            
            <div className="muted" style={{ marginTop: 4, fontSize: 11, marginLeft: 20 }}>
              When ticked, the launcher exits as soon as <span className="mono">zoo.exe</span> reports started.
            </div>
          </GroupBox>

          <GroupBox label="Theme">
            <div className="muted" style={{ marginBottom: 6, fontSize: 11 }}>
              Affects the launcher only; in-game appearance is unchanged.
            </div>
            <div style={{ display: "flex", gap: 18 }}>
              <Radio name="theme" value="system" current={theme} onChange={setTheme} label="System default" />
              <Radio name="theme" value="light" current={theme} onChange={setTheme} label="Light" />
              <Radio name="theme" value="dark" current={theme} onChange={setTheme} label="Dark" />
            </div>
          </GroupBox>

          <div style={{ display: "flex", justifyContent: "flex-end", gap: 6, marginTop: 4 }}>
            <Btn isDefault onClick={() => onSave({ startupPref: pref, closeAfter: close, theme })} accel="O">OK</Btn>
            <Btn onClick={onCancel} accel="C">Cancel</Btn>
          </div>
        </div>
      </W95Window>
    </Modal>);

}

// ============================================================================
// Help — WinHelp-style dialog with a tree TOC and scrollable content pane.
// Three chapters: (1) Using the Launcher, (2) zoo.ini, (3) ZT Internals.
// ============================================================================

const helpS = {
  h1: { fontSize: 15, fontWeight: "bold", margin: "0 0 10px", paddingBottom: 6, borderBottom: "1px solid #c0c0c0", color: "#000080" },
  h2: { fontSize: 12, fontWeight: "bold", margin: "14px 0 4px", color: "#000040" },
  p: { margin: "0 0 8px", lineHeight: 1.6, fontSize: 12 },
  code: { fontFamily: "'Courier New', monospace", fontSize: 11, background: "#f0f0f0", padding: "1px 3px", border: "1px solid #d8d8d8" },
  block: { fontFamily: "'Courier New', monospace", fontSize: 11, background: "#f0f0f0", border: "1px solid #c8c8c8", padding: "8px 10px", margin: "6px 0 10px", lineHeight: 1.6, whiteSpace: "pre" },
  li: { margin: "0 0 5px", lineHeight: 1.55, fontSize: 12 },
  ul: { margin: "0 0 10px", paddingLeft: 20 },
  ol: { margin: "0 0 10px", paddingLeft: 22 },
  note: { background: "#fffce0", border: "1px solid #d8c840", padding: "7px 10px", margin: "10px 0", fontSize: 11, lineHeight: 1.5 }
};

const HELP_CHAPTERS = [
{
  id: "ch1", label: "Chapter 1: Using the Launcher",
  topics: [
  { id: "ch1-intro", label: "Introduction" },
  { id: "ch1-window", label: "The Main Window" },
  { id: "ch1-manage", label: "Managing Installations" },
  { id: "ch1-launch", label: "Launching the Game" },
  { id: "ch1-settings", label: "Settings" }]

},
{
  id: "ch2", label: "Chapter 2: The zoo.ini File",
  topics: [
  { id: "ch2-what", label: "What Is zoo.ini?" },
  { id: "ch2-struct", label: "File Structure" },
  { id: "ch2-keys", label: "Key Settings" },
  { id: "ch2-history", label: "INI History" },
  { id: "ch2-fresh", label: "Creating a Fresh INI" }]

},
{
  id: "ch3", label: "Chapter 3: Zoo Tycoon Internals",
  topics: [
  { id: "ch3-exe", label: "The Executable" },
  { id: "ch3-ztd", label: "Resource Files (.ztd)" },
  { id: "ch3-expack", label: "Expansion Packs" },
  { id: "ch3-perf", label: "Performance Tuning" },
  { id: "ch3-saves", label: "Save Files" },
  { id: "ch3-compat", label: "Compatibility" }]

}];


function HL({ nav, to, children }) {
  return (
    <span
      style={{ color: "#0000cc", cursor: "pointer", textDecoration: "underline" }}
      onClick={() => nav(to)}>
      {children}</span>);

}
function C({ children }) {
  return <span style={helpS.code}>{children}</span>;
}

function HelpTopic({ id, nav }) {
  switch (id) {

    // -----------------------------------------------------------------------
    // Chapter 1
    // -----------------------------------------------------------------------
    case "ch1-intro":return (
        <>
        <div style={helpS.h1}>Introduction</div>
        <p style={helpS.p}><b>Zoo Tycoon Launcher</b> is a native Windows utility for managing and launching Zoo Tycoon (2001). Instead of navigating to the game folder and double-clicking <C>zoo.exe</C> each session, the launcher maintains a registry of your installations, provides a structured editor for <C>zoo.ini</C>, and records a full history of every configuration change.</p>
        <p style={helpS.p}>The launcher never modifies <C>zoo.exe</C>. Every change it makes goes to <C>zoo.ini</C>, which the game reads once at startup.</p>
        <div style={helpS.h2}>What you can do</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}>Register multiple Zoo Tycoon installations — base game, Complete Collection, modded copies — and switch between them instantly.</li>
          <li style={helpS.li}>View and edit <C>zoo.ini</C> settings through the <b>INI</b> tab without opening a text editor.</li>
          <li style={helpS.li}>Browse a complete timestamped history of INI snapshots and restore any previous version.</li>
          <li style={helpS.li}>Launch the game with a single click once an installation is selected.</li>
        </ul>
        <div style={helpS.note}>
          <b>Getting started:</b> Read <HL nav={nav} to="ch1-window">The Main Window</HL> for a UI overview, then <HL nav={nav} to="ch1-manage">Managing Installations</HL> to register your copy of Zoo Tycoon.
        </div>
      </>);


    case "ch1-window":return (
        <>
        <div style={helpS.h1}>The Main Window</div>
        <p style={helpS.p}>The main window is the launcher's primary interface. It shows the active installation, its status, and provides access to the launch controls and INI editor.</p>
        <div style={helpS.h2}>Menu bar</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}><b>File</b> — open or close an installation, open the Installation Manager, access Settings, or exit.</li>
          <li style={helpS.li}><b>Help</b> — open this document (F1) or the About dialog.</li>
        </ul>
        <div style={helpS.h2}>Tabs</div>
        <p style={helpS.p}>The <b>General</b> tab shows the current installation's status and the Launch button. The <b>INI</b> tab provides a structured key–value editor for the active installation's <C>zoo.ini</C>.</p>
        <div style={helpS.h2}>Status states</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}><b>Looking for Zoo Tycoon</b> — the launcher is scanning the registered path.</li>
          <li style={helpS.li}><b>Ready to Play</b> — both <C>zoo.exe</C> and <C>zoo.ini</C> were found; you can launch.</li>
          <li style={helpS.li}><b>Cannot Play</b> — a required file is missing. Use <HL nav={nav} to="ch1-manage">Fix Installation</HL> to recover.</li>
          <li style={helpS.li}><b>No Installation Found</b> — no installation is active. Use File → Open Installation.</li>
        </ul>
        <div style={helpS.h2}>Status bar</div>
        <p style={helpS.p}>The status bar at the bottom shows the active installation name on the left and the current state on the right.</p>
      </>);


    case "ch1-manage":return (
        <>
        <div style={helpS.h1}>Managing Installations</div>
        <p style={helpS.p}>Open <b>File → Installation Manager</b> to add, edit, remove, or fix game installations. An <i>installation</i> is a named pointer to a folder on disk that contains <C>zoo.exe</C>.</p>
        <div style={helpS.h2}>Valid vs. invalid</div>
        <p style={helpS.p}>The launcher checks each folder for two files:</p>
        <ul style={helpS.ul}>
          <li style={helpS.li}><C>zoo.exe</C> — the game executable. Required to launch.</li>
          <li style={helpS.li}><C>zoo.ini</C> — the configuration file. Required to edit settings and view history.</li>
        </ul>
        <p style={helpS.p}>If either file is absent the installation is shown as <b>invalid</b>. Click <b>Fix</b> to locate a missing EXE or generate a fresh INI.</p>
        <div style={helpS.h2}>The default installation</div>
        <p style={helpS.p}>One installation is marked as the <i>default</i> — shown in bold in the grid. It is opened automatically at startup when the startup preference is set accordingly. The first installation added is always the default.</p>
        <div style={helpS.h2}>Adding an installation</div>
        <ol style={helpS.ol}>
          <li style={helpS.li}>Click <b>Add</b> in the Installation Manager.</li>
          <li style={helpS.li}>Enter a display name (e.g. <i>Main</i>, <i>Complete Collection</i>, <i>Modded</i>).</li>
          <li style={helpS.li}>Enter or browse to the folder that contains <C>zoo.exe</C>.</li>
          <li style={helpS.li}>Optionally tick <b>Mark as default installation</b>.</li>
          <li style={helpS.li}>Click <b>Save</b>.</li>
        </ol>
        <div style={helpS.note}>Deleting an installation removes only the launcher's database entry. The game folder on disk is left completely untouched.</div>
      </>);


    case "ch1-launch":return (
        <>
        <div style={helpS.h1}>Launching the Game</div>
        <p style={helpS.p}>When the main window shows <b>Ready to Play</b>, click <b>Launch</b> to start Zoo Tycoon.</p>
        <div style={helpS.h2}>What happens at launch</div>
        <ol style={helpS.ol}>
          <li style={helpS.li}>The launcher captures a snapshot of the current <C>zoo.ini</C> if it has changed since the last snapshot.</li>
          <li style={helpS.li}>Pending INI edits from the <b>INI</b> tab are written to disk.</li>
          <li style={helpS.li}>The launcher starts <C>zoo.exe</C> as a new process.</li>
          <li style={helpS.li}>If <i>Close launcher after game starts</i> is enabled, the launcher exits as soon as the game process is confirmed running.</li>
        </ol>
        <div style={helpS.h2}>If the game does not start</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}>Confirm the installation is in the <b>Ready to Play</b> state.</li>
          <li style={helpS.li}>Check Windows compatibility settings on <C>zoo.exe</C>. See <HL nav={nav} to="ch3-compat">Compatibility</HL>.</li>
          <li style={helpS.li}>If the screen goes black on startup, reduce resolution in the <b>INI</b> tab — set <C>screenwidth</C> and <C>screenheight</C> to 800 and 600.</li>
        </ul>
      </>);


    case "ch1-settings":return (
        <>
        <div style={helpS.h1}>Settings</div>
        <p style={helpS.p}>Open <b>File → Settings</b> to configure launcher-level behaviour. These settings affect only the launcher; in-game options are controlled via <C>zoo.ini</C>.</p>
        <div style={helpS.h2}>Startup preference</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}><b>Default installation</b> — always opens the installation marked as default.</li>
          <li style={helpS.li}><b>Last played</b> — restores whichever installation you most recently launched a game from.</li>
          <li style={helpS.li}><b>Last opened in launcher</b> — restores the installation that was active when you last closed the launcher.</li>
          <li style={helpS.li}><b>No installation — show the picker</b> — starts with no installation selected.</li>
        </ul>
        <div style={helpS.h2}>Game launch</div>
        <p style={helpS.p}><b>Close launcher after game starts</b> — exits the launcher as soon as <C>zoo.exe</C> is running. Untick to keep the launcher open, which is useful for restoring INI snapshots after a session.</p>
        <div style={helpS.h2}>Theme</div>
        <p style={helpS.p}>Choose <b>Light</b>, <b>Dark</b>, or <b>System default</b> (follows your Windows personalisation setting). The game's appearance is unaffected.</p>
      </>);


    // -----------------------------------------------------------------------
    // Chapter 2
    // -----------------------------------------------------------------------
    case "ch2-what":return (
        <>
        <div style={helpS.h1}>What Is zoo.ini?</div>
        <p style={helpS.p}><C>zoo.ini</C> is a plain-text Windows INI file stored in Zoo Tycoon's root installation folder alongside <C>zoo.exe</C>. The game reads it once at startup to configure display resolution, interface behaviour, audio, and gameplay parameters. It is not written to during a play session — only the launcher and text editors modify it.</p>
        <p style={helpS.p}>Because the game reads the file only at launch, any changes made in the <b>INI</b> tab take effect on the <i>next</i> launch, not immediately.</p>
        <div style={helpS.h2}>Default location</div>
        <div style={helpS.block}>{`C:\\Program Files (x86)\\Microsoft Games\\Zoo Tycoon\\zoo.ini`}</div>
        <div style={helpS.h2}>Why use the launcher to edit it?</div>
        <p style={helpS.p}>The raw file has no descriptions, no validation, and no undo. The launcher's <b>INI</b> tab presents every key with its name, current value, allowed range, and a brief description — and records a full change history so any edit can be rolled back. See <HL nav={nav} to="ch2-history">INI History</HL>.</p>
      </>);


    case "ch2-struct":return (
        <>
        <div style={helpS.h1}>File Structure</div>
        <p style={helpS.p}><C>zoo.ini</C> follows the standard Windows INI format: named <i>sections</i> containing <i>key–value pairs</i>. Zoo Tycoon uses plain ASCII with integer and short string values; there are no quoted strings, multiline values, or Unicode.</p>
        <div style={helpS.h2}>Sections</div>
        <p style={helpS.p}>A section begins with its name in square brackets. Everything below that line, until the next <C>[section]</C> header or the end of file, belongs to that section. Section names are case-sensitive.</p>
        <div style={helpS.block}>{`[user]`}</div>
        <div style={helpS.h2}>Key–value pairs</div>
        <p style={helpS.p}>Each pair is written as <C>key=value</C> with no spaces around the equals sign:</p>
        <div style={helpS.block}>{`[user]\nscreenwidth=1920\nscreenheight=1080\nfullscreen=1\n\n[UI]\ntooltipDelay=0\ntooltipDuration=3000`}</div>
        <div style={helpS.h2}>Comments</div>
        <p style={helpS.p}>Lines beginning with <C>;</C> are comments; Zoo Tycoon ignores them. The launcher preserves comments when rewriting the file.</p>
        <div style={helpS.block}>{`; Resolution — increase for widescreen monitors\n[user]\nscreenwidth=1024`}</div>
        <div style={helpS.note}>Keys the game does not recognise are silently ignored. The launcher shows a warning in the INI tab if unknown keys are detected.</div>
      </>);


    case "ch2-keys":return (
        <>
        <div style={helpS.h1}>Key Settings</div>
        <p style={helpS.p}>The sections and keys below are read by Zoo Tycoon at startup. All values are integers unless noted. The <b>INI</b> tab exposes each key with its allowed range.</p>
        <div style={helpS.h2}>[user] — Display</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}><C>screenwidth</C> / <C>screenheight</C> — rendering resolution in pixels. The game renders at this size regardless of monitor resolution. Lower values improve frame rate significantly on modern hardware.</li>
          <li style={helpS.li}><C>fullscreen</C> — <C>1</C> for exclusive fullscreen, <C>0</C> for windowed mode (windowed requires DxWnd on Win10/11).</li>
        </ul>
        <div style={helpS.h2}>[UI] — Interface</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}><C>tooltipDelay</C> — milliseconds before a tooltip appears. <C>0</C> removes the delay.</li>
          <li style={helpS.li}><C>tooltipDuration</C> — milliseconds a tooltip remains visible.</li>
          <li style={helpS.li}><C>mouseScrollX</C> / <C>mouseScrollY</C> — pixels scrolled per frame when the cursor is at the screen edge.</li>
          <li style={helpS.li}><C>keyScrollX</C> / <C>keyScrollY</C> — pixels scrolled per frame while a scroll key is held.</li>
          <li style={helpS.li}><C>minimumMessageInterval</C> — minimum frames between on-screen advisor messages.</li>
        </ul>
        <div style={helpS.h2}>[advanced] — Gameplay</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}><C>level</C> — difficulty: <C>1</C> Easy, <C>2</C> Medium, <C>3</C> Hard. Affects guest and animal behaviour thresholds.</li>
        </ul>
        <div style={helpS.h2}>[language]</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}><C>locale</C> — short locale string (e.g. <C>en-us</C>, <C>fr-fr</C>). Selects the language <C>.ztd</C> pack loaded at startup.</li>
        </ul>
        <div style={helpS.h2}>[music] / [sound]</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}><C>volume</C> — master volume, 0–100.</li>
          <li style={helpS.li}><C>enabled</C> — <C>1</C> to enable, <C>0</C> to disable the subsystem entirely.</li>
        </ul>
        <div style={helpS.note}>See <HL nav={nav} to="ch3-perf">Performance Tuning</HL> for which settings have the most impact on frame rate.</div>
      </>);


    case "ch2-history":return (
        <>
        <div style={helpS.h1}>INI History</div>
        <p style={helpS.p}>Every time the launcher writes <C>zoo.ini</C> it first captures a timestamped snapshot of the file's current contents. Snapshots are stored in the launcher's SQLite database — never in the game folder — and accumulate indefinitely.</p>
        <div style={helpS.h2}>Viewing history</div>
        <p style={helpS.p}>Open <b>Edit → Restore Previous INI</b> from the menu. The history dialog lists all snapshots with their timestamp, the trigger that caused them, and how many keys differ from the current file.</p>
        <div style={helpS.h2}>The diff view</div>
        <p style={helpS.p}>Selecting a snapshot reveals a diff table: current values on the left, snapshot values on the right, with only changed keys shown.</p>
        <div style={helpS.h2}>Restoring a snapshot</div>
        <p style={helpS.p}>Click <b>Restore</b> to overwrite <C>zoo.ini</C> with the selected snapshot. The launcher captures the current state as a new snapshot before overwriting, so every restore is itself reversible.</p>
        <div style={helpS.note}><b>Tip:</b> A snapshot showing <b>0 changes</b> is identical to the current file. The Restore button is disabled for such entries.</div>
        <div style={helpS.h2}>Snapshot triggers</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}><b>LauncherGui</b> — you edited keys in the INI tab.</li>
          <li style={helpS.li}><b>Manual</b> — <C>zoo.ini</C> was edited outside the launcher and the change was detected on next startup.</li>
          <li style={helpS.li}><b>OriginalImport</b> — the first snapshot, taken when the installation was first registered.</li>
        </ul>
      </>);


    case "ch2-fresh":return (
        <>
        <div style={helpS.h1}>Creating a Fresh INI</div>
        <p style={helpS.p}>If <C>zoo.ini</C> is missing from an installation's folder, the installation is marked <b>invalid</b> with the reason <i>"Invalid — No INI"</i>. The launcher can generate a replacement using its built-in defaults, which match what a clean Microsoft installation produces.</p>
        <div style={helpS.h2}>How to create a fresh INI</div>
        <ol style={helpS.ol}>
          <li style={helpS.li}>Open <b>File → Installation Manager</b>.</li>
          <li style={helpS.li}>Select the invalid installation.</li>
          <li style={helpS.li}>Click <b>Fix</b>.</li>
          <li style={helpS.li}>In the Fix Installation dialog, click <b>Create</b> in the <i>Fix INI</i> section.</li>
          <li style={helpS.li}>The launcher writes a fresh <C>zoo.ini</C> and records it as an <i>OriginalImport</i> snapshot.</li>
        </ol>
        <p style={helpS.p}>If the main window is in the <b>Cannot Play</b> state, a <b>Fix Installation</b> shortcut also appears directly on the General tab.</p>
        <div style={helpS.h2}>Default values</div>
        <p style={helpS.p}>The generated file sets 800 × 600 fullscreen resolution with all UI timings at their original Microsoft defaults. Adjust the resolution in the <b>INI</b> tab before your first launch if desired.</p>
        <div style={helpS.note}>If you previously had a custom <C>zoo.ini</C> that was lost, check <HL nav={nav} to="ch2-history">INI History</HL> — the launcher may already hold a snapshot of it from when the installation was registered.</div>
      </>);


    // -----------------------------------------------------------------------
    // Chapter 3
    // -----------------------------------------------------------------------
    case "ch3-exe":return (
        <>
        <div style={helpS.h1}>The Executable</div>
        <p style={helpS.p}><C>zoo.exe</C> is a 32-bit Windows PE application (~5–6 MB) compiled for Windows 98/ME/2000/XP and targeting DirectX 7. The launcher treats it as a black box — it never patches, injects into, or modifies the binary in any way.</p>
        <div style={helpS.h2}>Startup sequence</div>
        <ol style={helpS.ol}>
          <li style={helpS.li}>Reads <C>zoo.ini</C> from its own directory.</li>
          <li style={helpS.li}>Initialises DirectDraw (display) and DirectSound (audio) using INI settings.</li>
          <li style={helpS.li}>Scans the game directory recursively for <C>.ztd</C> resource archives and loads them in lexicographic order.</li>
          <li style={helpS.li}>Loads the main menu or a saved game.</li>
        </ol>
        <div style={helpS.h2}>Single-threaded execution</div>
        <p style={helpS.p}>The simulation tick, rendering, audio mixing, and AI all execute on one thread serially. This is why the game is CPU-bound rather than GPU-bound, and why single-core clock speed matters more than core count on modern hardware.</p>
        <div style={helpS.h2}>No network code</div>
        <p style={helpS.p}>Zoo Tycoon 2001 has no online or LAN functionality and makes no network calls. Neither does the launcher.</p>
      </>);


    case "ch3-ztd":return (
        <>
        <div style={helpS.h1}>Resource Files (.ztd)</div>
        <p style={helpS.p}>Zoo Tycoon stores its content — sprites, animations, sounds, configuration scripts, and AI definitions — in <C>.ztd</C> archives. A <C>.ztd</C> file is a standard ZIP archive with its extension renamed; any ZIP utility can inspect or extract one.</p>
        <div style={helpS.h2}>Archive contents</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}><C>.bmp</C> — sprite sheets for animals, buildings, terrain, guests, and UI elements.</li>
          <li style={helpS.li}><C>.ani</C> — animation descriptors indexing frames within a sprite sheet and defining frame timings.</li>
          <li style={helpS.li}><C>.wav</C> — uncompressed PCM audio: ambient sounds, animal vocalisations, UI cues.</li>
          <li style={helpS.li}><C>.cfg</C> — plain-text scripts defining animal parameters, building stats, and exhibit requirements.</li>
          <li style={helpS.li}><C>.ai</C> — behaviour-tree scripts for each species' AI.</li>
          <li style={helpS.li}><C>.lyt</C> — layout files for UI panels and placement grids.</li>
        </ul>
        <div style={helpS.h2}>Load order and modding</div>
        <p style={helpS.p}>The game scans the installation directory recursively and loads every <C>.ztd</C> in lexicographic (alphabetical) order. When two archives contain a file with the same internal path, the later-loaded one wins — this is how mods override vanilla content.</p>
        <div style={helpS.note}><b>Mod tip:</b> Prefix mod archives with <C>z_</C> (e.g. <C>z_mymod.ztd</C>) so they sort after all vanilla files and take priority automatically.</div>
      </>);


    case "ch3-expack":return (
        <>
        <div style={helpS.h1}>Expansion Packs</div>
        <p style={helpS.p}>Zoo Tycoon (2001) received two official expansions, each distributed as additional <C>.ztd</C> archives alongside an updated <C>zoo.exe</C>. The Complete Collection bundles the base game with both expansions on a single installer.</p>
        <div style={helpS.h2}>Dinosaur Digs (2002)</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}>Adds the <b>Mesozoic</b> exhibit type with Jurassic and Cretaceous biomes.</li>
          <li style={helpS.li}>Introduces 20 dinosaur species including <i>Tyrannosaurus rex</i>, <i>Triceratops</i>, and <i>Brachiosaurus</i>.</li>
          <li style={helpS.li}>New research paths and exhibit buildings specific to prehistoric fauna.</li>
          <li style={helpS.li}>Dinosaurs cannot share a zoo map with modern animals.</li>
        </ul>
        <div style={helpS.h2}>Marine Mania (2002)</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}>Adds <b>aquatic exhibits</b> with underwater viewing panels.</li>
          <li style={helpS.li}>Introduces 30 marine species including orcas, great white sharks, and giant squid.</li>
          <li style={helpS.li}>New buildings: dolphin shows, wave pools, and marine research stations.</li>
          <li style={helpS.li}>Amphibious species (e.g. sea lions) can occupy hybrid land-and-water exhibits.</li>
        </ul>
        <div style={helpS.h2}>Complete Collection</div>
        <p style={helpS.p}>The Complete Collection <C>zoo.exe</C> handles the DirectDraw initialisation path more gracefully on Windows 7+ and is the recommended binary for modern systems. The launcher works with any version and detects which expansion <C>.ztd</C> packs are present.</p>
      </>);


    case "ch3-perf":return (
        <>
        <div style={helpS.h1}>Performance Tuning</div>
        <p style={helpS.p}>Zoo Tycoon 2001 is a single-threaded DirectDraw application. On modern hardware the DirectDraw surface is typically emulated in software, making rendering the primary bottleneck. The adjustments below can substantially improve frame rate.</p>
        <div style={helpS.h2}>Resolution (highest impact)</div>
        <p style={helpS.p}>The game renders at exactly the resolution set in <C>zoo.ini</C>, not your monitor's native resolution. Setting <C>screenwidth</C> / <C>screenheight</C> to 800 × 600 or 1024 × 768 often doubles frame rate on the software-emulated path. Adjust in <b>INI tab → [user]</b>.</p>
        <div style={helpS.h2}>DxWnd</div>
        <p style={helpS.p}>DxWnd intercepts DirectDraw calls and routes them through Direct3D, enabling GPU acceleration. Set Zoo Tycoon as a DxWnd profile with <i>Windowed</i> ticked; the game runs in a resizable window with hardware rendering. This is usually the most effective single improvement.</p>
        <div style={helpS.h2}>dgVoodoo2</div>
        <p style={helpS.p}>dgVoodoo2 translates DirectX 7 calls to DirectX 11/12. Place <C>DDraw.dll</C> and <C>D3DImm.dll</C> from the dgVoodoo2 package in the same folder as <C>zoo.exe</C>. Useful as an alternative when DxWnd causes instability.</p>
        <div style={helpS.h2}>UI timing settings</div>
        <p style={helpS.p}>Setting <C>tooltipDelay</C> to <C>0</C> and reducing <C>minimumMessageInterval</C> decreases the frequency of UI redraws triggered by advisor messages — a small but measurable benefit on large zoos.</p>
        <div style={helpS.h2}>Zoo complexity</div>
        <p style={helpS.p}>Frame rate degrades as entity count rises because the simulation tick — animal AI, guest pathfinding, financial calculations — scales with the number of active entities. Keeping exhibits below ~30 and guests below ~200 maintains acceptable performance on most modern CPUs.</p>
        <div style={helpS.note}>Set Windows compatibility flags on <C>zoo.exe</C> before tuning performance. See <HL nav={nav} to="ch3-compat">Compatibility</HL>.</div>
      </>);


    case "ch3-saves":return (
        <>
        <div style={helpS.h1}>Save Files</div>
        <p style={helpS.p}>Zoo Tycoon save files use the <C>.zoo</C> extension and are stored in a <C>Saved Games</C> subfolder within the installation directory. The format is a proprietary binary stream; no official specification exists, though community reverse-engineering efforts have documented most of its structure.</p>
        <div style={helpS.h2}>What a save contains</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}><b>Zoo grid</b> — the full tile map (32 × 32 or 64 × 64 tiles) with tile type, elevation, foliage, path, and building data per cell.</li>
          <li style={helpS.li}><b>Animal records</b> — per animal: species, age (in game-months), hunger/thirst/happiness/health, genetic traits, exhibit assignment, and reproductive state.</li>
          <li style={helpS.li}><b>Guest records</b> — position, satisfaction scores, money remaining, and pathfinding state for every guest currently on the map.</li>
          <li style={helpS.li}><b>Finances</b> — monthly income and expenditure breakdown, loan state, and total zoo value.</li>
          <li style={helpS.li}><b>Research progress</b> — which items have been unlocked in each research tree.</li>
          <li style={helpS.li}><b>Camera state</b> — scroll position and zoom level, restored on load.</li>
        </ul>
        <div style={helpS.h2}>File sizes</div>
        <p style={helpS.p}>A minimal early-game save is 80–150 KB. A mature zoo with dozens of exhibits can reach 1–2 MB. Saves consistently above ~3 MB may indicate file corruption.</p>
        <div style={helpS.note}>The launcher does not back up save files automatically. Copy the <C>Saved Games</C> folder manually before installing mods or running Fix Installation operations.</div>
      </>);


    case "ch3-compat":return (
        <>
        <div style={helpS.h1}>Compatibility</div>
        <p style={helpS.p}>Zoo Tycoon 2001 was designed for Windows 98 SE, ME, 2000, and XP. Running it on Windows 10 or 11 requires the following adjustments, applied directly to <C>zoo.exe</C> via right-click → Properties → Compatibility tab.</p>
        <div style={helpS.h2}>Recommended flags</div>
        <ol style={helpS.ol}>
          <li style={helpS.li}><b>Compatibility mode: Windows XP (SP3)</b> — the most important flag. Without it, DirectDraw initialisation may fail entirely.</li>
          <li style={helpS.li}><b>Disable fullscreen optimisations</b> — prevents Windows from applying its DWM fullscreen path, which conflicts with DirectDraw exclusive mode.</li>
          <li style={helpS.li}><b>Run as administrator</b> — required if the installation is under <C>Program Files</C> and the game needs to write to <C>zoo.ini</C> or <C>Saved Games</C>.</li>
          <li style={helpS.li}><b>Reduced colour mode (16-bit)</b> — try this if DirectDraw still fails; some drivers drop 16-bit support unless forced.</li>
        </ol>
        <div style={helpS.h2}>Common failure modes</div>
        <ul style={helpS.ul}>
          <li style={helpS.li}><b>Black screen then crash</b> — DirectDraw initialisation failure. Reduce resolution to 800 × 600 in the INI tab, enable Reduced colour mode, and try DxWnd.</li>
          <li style={helpS.li}><b>Game launches but is unresponsive</b> — disable fullscreen optimisations; prefer the Complete Collection executable if available.</li>
          <li style={helpS.li}><b>No sound</b> — Zoo Tycoon uses DirectSound. Enable the legacy DirectSound shim via the DirectX Diagnostic Tool (<C>dxcpl.exe</C>).</li>
          <li style={helpS.li}><b>Very low frame rate</b> — follow <HL nav={nav} to="ch3-perf">Performance Tuning</HL>.</li>
        </ul>
        <div style={helpS.note}>The <b>Complete Collection</b> <C>zoo.exe</C> handles the DirectDraw path more gracefully on Windows 7+ and is generally the recommended binary for modern systems.</div>
      </>);


    default:return <div className="muted" style={{ fontSize: 12, padding: 8 }}>Select a topic from the contents panel on the left.</div>;
  }
}

function HelpDialog({ onClose }) {
  const contentRef = React.useRef(null);
  const [expanded, setExpanded] = useState(new Set(["ch1"]));
  const [history, setHistory] = useState(["ch1-intro"]);
  const [histIdx, setHistIdx] = useState(0);
  const selectedTopic = history[histIdx];

  const navigate = (topicId) => {
    const next = [...history.slice(0, histIdx + 1), topicId];
    setHistory(next);
    setHistIdx(next.length - 1);
    const ch = HELP_CHAPTERS.find((c) => c.topics.some((t) => t.id === topicId));
    if (ch) setExpanded((prev) => new Set([...prev, ch.id]));
    if (contentRef.current) contentRef.current.scrollTop = 0;
  };

  const goBack = () => {
    if (histIdx > 0) {setHistIdx(histIdx - 1);if (contentRef.current) contentRef.current.scrollTop = 0;}
  };
  const goFwd = () => {
    if (histIdx < history.length - 1) {setHistIdx(histIdx + 1);if (contentRef.current) contentRef.current.scrollTop = 0;}
  };

  const toggleCh = (id) => setExpanded((prev) => {
    const s = new Set(prev);s.has(id) ? s.delete(id) : s.add(id);return s;
  });

  const tbBase = { padding: "2px 10px", fontSize: 12, cursor: "pointer", background: "transparent", border: "none", fontFamily: "inherit" };

  return (
    <Modal onBackdrop={onClose}>
      <W95Window
        title="Zoo Tycoon Launcher Help"
        icon={<Icons.AppMark size={16} />}
        width={740}
        onClose={onClose}
        noPadding>
        
        {/* Toolbar */}
        <div style={{ display: "flex", alignItems: "center", gap: 2, padding: "3px 6px", borderBottom: "1px solid var(--gray, #c0c0c0)", background: "var(--face, #d4d0c8)", userSelect: "none" }}>
          <button style={{ ...tbBase, opacity: histIdx === 0 ? 0.4 : 1 }} disabled={histIdx === 0} onClick={goBack}>← Back</button>
          <button style={{ ...tbBase, opacity: histIdx >= history.length - 1 ? 0.4 : 1 }} disabled={histIdx >= history.length - 1} onClick={goFwd}>Forward →</button>
          <div style={{ width: 1, height: 18, background: "var(--gray, #aaa)", margin: "0 4px", flexShrink: 0 }} />
          <button style={tbBase} onClick={() => navigate("ch1-intro")}>Contents</button>
        </div>

        {/* Body */}
        <div style={{ display: "flex", height: 500 }}>
          {/* Tree */}
          <div className="sunken" style={{ width: 214, overflowY: "auto", flexShrink: 0, background: "#ffffff" }}>
            {HELP_CHAPTERS.map((ch) =>
            <div key={ch.id}>
                <div
                onClick={() => toggleCh(ch.id)}
                style={{ display: "flex", alignItems: "flex-start", gap: 5, padding: "5px 8px", cursor: "default", fontWeight: "bold", fontSize: 11, userSelect: "none", background: "var(--face, #d4d0c8)", borderBottom: "1px solid #b8b8b8", lineHeight: 1.35 }}>
                
                  <span style={{ fontSize: 8, marginTop: 2, minWidth: 9, display: "inline-block" }}>{expanded.has(ch.id) ? "▼" : "▶"}</span>
                  {ch.label}
                </div>
                {expanded.has(ch.id) && ch.topics.map((topic) =>
              <div
                key={topic.id}
                onClick={() => navigate(topic.id)}
                style={{
                  padding: "3px 8px 3px 22px",
                  fontSize: 11,
                  cursor: "default",
                  userSelect: "none",
                  borderBottom: "1px solid #ebebeb",
                  background: selectedTopic === topic.id ? "#000080" : "transparent",
                  color: selectedTopic === topic.id ? "#ffffff" : "inherit"
                }}>
                
                    {topic.label}
                  </div>
              )}
              </div>
            )}
          </div>

          {/* Content pane */}
          <div
            ref={contentRef}
            style={{ flex: 1, overflowY: "auto", padding: "14px 18px", background: "#ffffff", fontSize: 12, minWidth: 0 }}>
            
            <HelpTopic id={selectedTopic} nav={navigate} />
          </div>
        </div>
      </W95Window>
    </Modal>);

}

Object.assign(window, {
  Modal, ManagementDialog, AddEditDialog, FixDialog, InfoDialog,
  ConfirmDeleteDialog, AboutDialog, HistoryDialog, SettingsDialog, HelpDialog
});