/* global React, MainWindow, ManagementDialog, AddEditDialog, FixDialog, InfoDialog, ConfirmDeleteDialog, AboutDialog, HistoryDialog, SettingsDialog, HelpDialog, TweaksPanel, TweakSection, TweakRadio, TweakSelect, TweakToggle, useTweaks */
/* eslint-disable no-undef */

const { useState, useMemo, useEffect } = React;

// ============================================================================
// Stable mock data
// ============================================================================
const MOCK_INSTALLATIONS = [
  { id: "i-main",     name: "Main",                  path: "C:\\Program Files (x86)\\Microsoft Games\\Zoo Tycoon",
    isDefault: true,  hasExe: true,  hasIni: true,  addedUtc: "12 Mar 2025 18:02", lastOpenedUtc: "26 May 2026 09:18", lastPlayedUtc: "12 May 2026 22:14", historyCount: 24,
    valid: true,  invalidReason: "" },
  { id: "i-cc",       name: "Complete Collection",   path: "D:\\Games\\Zoo Tycoon CC",
    isDefault: false, hasExe: true,  hasIni: true,  addedUtc: "04 Apr 2025 11:30", lastOpenedUtc: "18 May 2026 20:42", lastPlayedUtc: "18 May 2026 20:55", historyCount: 7,
    valid: true,  invalidReason: "" },
  { id: "i-mods",     name: "Modded · A-Pack",       path: "D:\\Games\\Zoo Tycoon Modded",
    isDefault: false, hasExe: true,  hasIni: false, addedUtc: "21 Apr 2025 09:14", lastOpenedUtc: "02 May 2026 19:12", lastPlayedUtc: null, historyCount: 2,
    valid: false, invalidReason: "Invalid — No INI" },
  { id: "i-arch",     name: "Archive · 2023 build",  path: "E:\\Backups\\ZT2023",
    isDefault: false, hasExe: false, hasIni: true,  addedUtc: "30 Jan 2026 14:01", lastOpenedUtc: "30 Jan 2026 14:01", lastPlayedUtc: null, historyCount: 0,
    valid: false, invalidReason: "Invalid — No EXE" },
];

const MOCK_HISTORY = [
  { id: "h-1", capturedUtc: "24 May 2026 19:02", trigger: "LauncherGui", diff: 3,
    changes: [
      { section: "user", key: "screenwidth",  current: 1920, snapshot: 1024 },
      { section: "user", key: "screenheight", current: 1080, snapshot: 768 },
      { section: "UI",   key: "tooltipDelay", current: 0,    snapshot: 1 },
    ] },
  { id: "h-2", capturedUtc: "22 May 2026 08:41", trigger: "Manual",      diff: 1,
    changes: [
      { section: "advanced", key: "level", current: 2, snapshot: 1 },
    ] },
  { id: "h-3", capturedUtc: "12 May 2026 22:13", trigger: "LauncherGui", diff: 7,
    changes: [
      { section: "UI", key: "tooltipDelay",         current: 0,  snapshot: 2 },
      { section: "UI", key: "tooltipDuration",      current: 3000, snapshot: 5000 },
      { section: "UI", key: "mouseScrollX",         current: 27, snapshot: 40 },
      { section: "UI", key: "mouseScrollY",         current: 27, snapshot: 40 },
      { section: "UI", key: "keyScrollX",           current: 64, snapshot: 100 },
      { section: "UI", key: "keyScrollY",           current: 64, snapshot: 100 },
      { section: "UI", key: "minimumMessageInterval", current: 60, snapshot: 30 },
    ] },
  { id: "h-4", capturedUtc: "01 May 2026 12:00", trigger: "OriginalImport", diff: 0,
    changes: [] },
];

// ============================================================================
// App — top-level state machine
// ============================================================================

function App() {
  const [t, setTweak] = useTweaks(/*EDITMODE-BEGIN*/{
    "mainState":    "ready",
    "activeTab":    "general",
    "showDialog":   "none",
    "activeInstall": "i-main",
    "theme":         "light",
    "titleIcon":     "assets/ztl-title-icon.png",
    "exeCompatOk":   false
  }/*EDITMODE-END*/);

  // Apply the theme to <body> so the dark-mode CSS overrides take effect.
  // "system" follows the OS preference and updates live if it changes.
  useEffect(() => {
    const mq = window.matchMedia("(prefers-color-scheme: dark)");
    const apply = () => {
      const dark = t.theme === "dark" || (t.theme === "system" && mq.matches);
      document.body.classList.toggle("theme-dark", dark);
    };
    apply();
    if (t.theme === "system") {
      mq.addEventListener("change", apply);
      return () => mq.removeEventListener("change", apply);
    }
  }, [t.theme]);

  // Sync the active installation when the main state is "ready" so the header
  // shows a sensible value.
  const installations = MOCK_INSTALLATIONS;
  const activeInstall = installations.find(i => i.id === t.activeInstall) ?? installations[0];

  // When state is "cannot", show an invalid installation as the active one
  const cannotInstall = installations.find(i => !i.hasIni) ?? installations[2];
  const displayInstall = t.mainState === "cannot" ? cannotInstall : activeInstall;

  // ---- Picker state for "Open Game Installation" ---------------------------
  const [pickerSelectedId, setPickerSelectedId] = useState("i-cc");

  // ---- Dialog routing ------------------------------------------------------
  const openDialog = (name) => setTweak("showDialog", name);
  const closeDialog = () => setTweak("showDialog", "none");

  useEffect(() => {
    // Tiny app facade so child components don't need to pass openDialog through props.
    window.app = { openDialog, closeDialog, optimiseExe: () => setTweak("exeCompatOk", true) };
  });

  const onMenuCommand = (cmd) => {
    switch (cmd) {
      case "manage":       openDialog("manage"); break;
      case "close-install": setTweak("mainState", "noinstall"); break;
      case "open-install": setTweak("mainState", "open"); break;
      case "launch":       handleLaunch();        break;
      case "settings":     openDialog("settings"); break;
      case "exit":         break;
      case "undo-ini":     openDialog("history"); break;
      case "restore-orig": openDialog("history"); break;
      case "about":        openDialog("about");   break;
      case "help":         openDialog("help");    break;
      case "togglestatus": break;
      default: break;
    }
  };

  // F1 key opens help
  React.useEffect(() => {
    const handler = (e) => { if (e.key === "F1") { e.preventDefault(); openDialog("help"); } };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, []);

  const handleLaunch = () => {
    // Tiny visual feedback — flash the title bar
    const win = document.querySelector(".main-window .titlebar");
    if (!win) return;
    win.style.transition = "background 80ms";
    const orig = win.style.background;
    win.style.background = "#1084d0";
    setTimeout(() => { win.style.background = orig; }, 200);
  };

  // ---- Dialog data --------------------------------------------------------
  const [editingId, setEditingId] = useState(null);
  const existingNames = useMemo(() => installations.map(i => i.name), [installations]);
  const editingInstall = installations.find(i => i.id === editingId);

  // Picker for the open state
  const pickerInstall = installations.find(i => i.id === pickerSelectedId);

  return (
    <div className="app-shell">
      <div className="scene-label">Zoo Tycoon Launcher — Hi-fi prototype · Windows 95 chrome</div>

      <div style={{ position: "relative" }}>
        <MainWindow
          state={t.mainState}
          activeTab={t.activeTab}
          setActiveTab={(v) => setTweak("activeTab", v)}
          onMenuCommand={onMenuCommand}
          titleIcon={t.titleIcon}
          exeCompatOk={t.exeCompatOk}
          installation={displayInstall}
          pickerInstallations={installations}
          pickerSelectedId={pickerSelectedId}
          setPickerSelectedId={setPickerSelectedId}
          onLaunch={handleLaunch}
          onAddInstallation={() => openDialog("add")}
          onOpenInstallation={(id) => {
            if (!id) return;
            setTweak({ activeInstall: id, mainState: "ready" });
            closeDialog();
          }}
          onClose={() => {}}
        />

        {/* ---------- Dialogs ---------- */}
        {t.showDialog === "manage" && (
          <ManagementDialog
            installations={installations}
            defaultId={installations.find(i => i.isDefault)?.id}
            onClose={closeDialog}
            onAdd={() => { setEditingId(null); openDialog("add"); }}
            onEdit={(id) => { setEditingId(id); openDialog("edit"); }}
            onDelete={(id) => { setEditingId(id); openDialog("delete"); }}
            onFix={(id) => { setEditingId(id); openDialog("fix"); }}
            onInfo={(id) => { setEditingId(id); openDialog("info"); }}
          />
        )}

        {t.showDialog === "add" && (
          <AddEditDialog
            mode="add"
            existingNames={existingNames}
            hasInstallations={installations.length > 0}
            onSave={() => { openDialog("manage"); }}
            onCancel={() => openDialog(installations.length ? "manage" : "none")}
          />
        )}

        {t.showDialog === "edit" && (
          <AddEditDialog
            mode="edit"
            initial={editingInstall}
            existingNames={existingNames}
            hasInstallations
            onSave={() => openDialog("manage")}
            onCancel={() => openDialog("manage")}
          />
        )}

        {t.showDialog === "fix" && (
          <FixDialog
            installation={editingInstall ?? cannotInstall}
            onClose={() => openDialog(editingId ? "manage" : "none")}
            onLocateExe={() => {}}
            onCreateIni={() => {}}
          />
        )}

        {t.showDialog === "info" && (
          <InfoDialog
            installation={editingInstall ?? activeInstall}
            onClose={() => openDialog(editingId ? "manage" : "none")}
          />
        )}

        {t.showDialog === "delete" && (
          <ConfirmDeleteDialog
            installation={editingInstall}
            willPromoteName={editingInstall?.isDefault && installations.length > 1
              ? installations.filter(i => i.id !== editingInstall.id).sort((a, b) => a.name.localeCompare(b.name))[0].name
              : null}
            onConfirm={() => openDialog("manage")}
            onCancel={() => openDialog("manage")}
          />
        )}

        {t.showDialog === "about"   && <AboutDialog onClose={closeDialog} />}
        {t.showDialog === "help"    && <HelpDialog onClose={closeDialog} />}
        {t.showDialog === "history" && <HistoryDialog snapshots={MOCK_HISTORY} onClose={closeDialog} onRestore={() => closeDialog()} />}
        {t.showDialog === "settings" && (
          <SettingsDialog
            initial={{ startupPref: "DefaultInstallation", closeAfter: false, theme: t.theme }}
            onSave={({ theme }) => { setTweak("theme", theme); closeDialog(); }}
            onCancel={closeDialog}
          />
        )}
      </div>

      <Tweaks t={t} setTweak={setTweak} />
    </div>
  );
}

// ============================================================================
// Tweaks panel — explore states + dialogs without leaving the page
// ============================================================================
function Tweaks({ t, setTweak }) {
  return (
    <TweaksPanel>
      <TweakSection label="Main-window state" />
      <TweakSelect
        label="State"
        value={t.mainState}
        options={[
          { value: "looking",   label: "Looking for Zoo Tycoon" },
          { value: "ready",     label: "Ready to Play" },
          { value: "cannot",    label: "Cannot Play" },
          { value: "noinstall", label: "No Install Found" },
          { value: "open",      label: "Open Picker" },
        ]}
        onChange={(v) => setTweak("mainState", v)}
      />

      <TweakSection label="Active tab" />
      <TweakRadio
        label="Tab"
        value={t.activeTab}
        options={[
          { value: "general", label: "General" },
          { value: "ini",     label: "INI" },
        ]}
        onChange={(v) => setTweak("activeTab", v)}
      />

      <TweakSection label="Open dialog" />
      <TweakSelect
        label="Dialog"
        value={t.showDialog}
        options={[
          { value: "none",    label: "— closed —" },
          { value: "manage",  label: "Installation Manager" },
          { value: "add",     label: "Add Installation" },
          { value: "edit",    label: "Edit Installation" },
          { value: "fix",     label: "Fix Installation" },
          { value: "info",    label: "Installation Info" },
          { value: "delete",  label: "Confirm Delete" },
          { value: "history", label: "Restore INI History" },
          { value: "settings", label: "Settings" },
          { value: "about",   label: "About" },
          { value: "help",    label: "Help" },
        ]}
        onChange={(v) => setTweak("showDialog", v)}
      />

      <TweakSection label="Theme" />
      <TweakRadio
        label="Theme"
        value={t.theme}
        options={[
          { value: "light",  label: "Light" },
          { value: "dark",   label: "Dark" },
          { value: "system", label: "System" },
        ]}
        onChange={(v) => setTweak("theme", v)}
      />

      <TweakSection label="Active installation" />
      <TweakSelect
        label="Profile"
        value={t.activeInstall}
        options={MOCK_INSTALLATIONS.map(i => ({ value: i.id, label: i.name }))}
        onChange={(v) => setTweak("activeInstall", v)}
      />

      <TweakSection label="EXE compatibility" />
      <TweakToggle
        label="zoo.exe optimised"
        value={t.exeCompatOk}
        onChange={(v) => setTweak("exeCompatOk", v)}
      />

      <TweakSection label="Title bar icon" />
      <TweakSelect
        label="Icon"
        value={t.titleIcon}
        options={[
          { value: "assets/ztl-title-icon.png",   label: "Lion (default)" },
          { value: "assets/ztl-title-icon-2.png",  label: "Zoo Gate" },
          { value: "assets/ztl-title-icon-3.png",  label: "Animal + Bracket" },
          { value: "assets/ztl-title-icon-4.png",  label: "Elephant" },
        ]}
        onChange={(v) => setTweak("titleIcon", v)}
      />
    </TweaksPanel>
  );
}

// ---- Mount ----------------------------------------------------------------
ReactDOM.createRoot(document.getElementById("root")).render(<App />);
