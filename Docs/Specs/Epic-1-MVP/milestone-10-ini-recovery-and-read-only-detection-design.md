# INI Recovery and Read-only Detection — Design Spec

> **Milestone:** 10 (Epic 1 — MVP)
> **Date:** 2026-10-21
> **Author:** Justinian
> **Status:** Approved for implementation (written from the SDD at the maintainer's request; calls C1–C10 accepted)
> **Authoritative spec:** [1-zoo-tycoon-launcher-master-design.md](../1-zoo-tycoon-launcher-master-design.md) §7.1.2, §7.2.5, §7.4, §7.7, §8.2, §9.3.1, §9.5, §14.5 row 9
> **Deferred work taken:** DW-01 (No-INI recovery actions), DW-12 (detect a read-only installation folder)
> **Design reference:** `Docs/UserInterfaceDesign/HiFiPrototype/src/ini-config.jsx` (`IniMissingState`), `src/dialogs.jsx` (Fix Installation, *Fix INI*)
> **Branch:** `feat/milestone-10-ini-recovery-and-read-only-detection`

---

## 1. Goal

Finish the INI Config tab's error states. An installation with no `zoo.ini` can get one from the launcher — a fresh file from the built-in defaults, or a copy of
an existing file the user points at — from the INI tab's No INI box and from the Fix Installation dialogue, and the window then moves from Cannot Play to Ready to
Play without a restart. An installation whose folder (or `zoo.ini`) the user cannot write to is recognised when it opens, and the launcher says why saving will fail
and how to fix it, instead of the first Save failing with a bare "access denied".

## 2. Non-goals

- **Copy From Another Installation** (SDD §7.4's third action). The SDD's MVP note keeps it out; it is recorded as a new deferred item (§8).
- **Fixing** a read-only installation. MVP explains and recommends a reinstall (SDD §14.5 row 9); the VirtualStore fix stays DW-36 (V2.0.0). The launcher does not
  clear a read-only attribute or change permissions itself.
- **The Corrupted INI state** (SDD §7.5) and the `[scenario]` section (DW-04).
- **Watching `zoo.ini` live** (DW-38). Detection runs when an installation opens, as SDD DW-12 asks.

## 3. Decisions

Taken from the SDD; where the SDD is silent, the author's calls follow in §4.

| # | Question                                | Decision                                                                                                                                       |
|---|-----------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | Which recovery actions                  | `Create zoo.ini from defaults` and `Locate existing zoo.ini…` (SDD §7.4 MVP note, §9.3.1). No Copy From Another.                               |
| 2 | Where they appear                       | The INI tab's No INI box (§9.3.1) and the Fix Installation dialogue's *Fix INI* group (§7.2.5, §9.5), which today has a hard-disabled `Create`. |
| 3 | What a successful action records        | The first-ever parse path: `Original` + `Current`, `Trigger`/`Source = OriginalImport` (SDD §7.4), and `HasIni = true` on the row.            |
| 4 | When the read-only check runs           | When an installation opens (SDD DW-12): the boot pipeline's Verify step.                                                                       |
| 5 | What the read-only explanation says     | The cause (the folder grants ordinary users read-only access) and the fix: uninstall and reinstall the game (SDD DW-12, §14.5 row 9).           |

## 4. Calls made by the author

These go in the ledger as rulings when the plan runs; each is cheap to reverse.

- **C1 — The default file is built from `ZooIniDefaults`.** A new Domain factory renders every key with a non-null `DefaultValue`, sections and keys in registry
  order, CRLF line endings, no comments, no `[scenario]` section (the game writes its own keys; DW-04). Nullable game-managed keys (`lastfile`, `lastWindowX`, …) are
  omitted, as the game adds them on exit. No stock file is embedded: the launcher ships no Microsoft content, and the registry is already the authority on defaults.
- **C2 — Create and Locate never overwrite.** Both write through a new create-only store method (temp file, then `File.Move(overwrite: false)`). If `zoo.ini` exists
  by the time the action runs (the user restored it by hand, or the game created it), the action fails with a new `Ini.AlreadyExists` error whose message tells the
  user the file is already there; the window then re-verifies as after success, so it picks the file up.
- **C3 — Locate copies, it does not move.** The chosen file is read (Latin-1, like every INI read) and written as `zoo.ini`; the source is left untouched. It must look
  like a `zoo.ini`: at least one section header that `ZooIniDefaults.Sections` recognises, and at most 1 MiB; otherwise a new `Ini.NotAnIni` error ("… does not look
  like a Zoo Tycoon zoo.ini"). The picker filters to `*.ini` with an *All files* fallback, starting in the installation folder.
- **C4 — Snapshots after the write follow the existing reconcile.** After the file is written, the handler runs the standard synchronise. With no `Current` snapshot
  this is the first import (`Original` + `Current`, `OriginalImport`). If the installation once had a `zoo.ini` and its database still holds `Current`, the new file is
  ordinary drift: `Current` is archived as `Manual` and the new values adopted, and `Original` keeps the very first file — so a user who loses `zoo.ini` can still
  restore their old settings from history (this is what the prototype's "Any prior values can be restored later from history" promises). A snapshot-store failure
  after the file write is logged and swallowed, as SDD §8.2 does for Save: the file stands and the next open imports it.
- **C5 — The window re-boots onto the INI tab.** After a successful action from the INI tab, the launcher re-runs the boot for the open installation (the same pointed
  boot the Installation Manager uses after a Fix) so the General tab, status bar and INI tab all refresh, and it reopens on the INI Config tab with the editor showing.
  From the Fix dialogue the existing `HasChanges` → re-boot path applies unchanged.
- **C6 — The read-only probe writes a throwaway file.** Permissions and ACLs do not reliably predict whether a write succeeds (inheritance, UAC, group membership), so
  the probe does what Save does: create a uniquely named temp file in the installation folder, using the existing `zoo.ini.tmp.<guid>` pattern so the orphan cleanup
  removes any crash residue, then delete it. It also reads `zoo.ini`'s Read-only attribute. The result is one of **Writable**, **FolderReadOnly**, **FileReadOnly**,
  or **Unknown** (any failure other than access denied — the launcher then says nothing).
- **C7 — A read-only attribute gets its own advice.** `FileReadOnly` says the file is marked read-only and tells the user to clear *Read-only* in the file's
  Properties; reinstalling is the advice only for `FolderReadOnly`. The SDD covers only the folder case; the attribute is a cheaper, distinct cause of the same symptom.
- **C8 — The warning is shown, Save is not disabled.** A detection can be wrong, so Save stays enabled. The INI tab shows a one-line warning strip (warning icon,
  maroon text) above the editor or the No INI box. Every INI write that then fails with access denied (Save, Restore, Restore defaults, Create, Locate) maps to a new
  `Ini.ReadOnly` error carrying the same explanation instead of the OS message.
- **C9 — Copy text.** No INI box (prototype copy, adjusted for Locate): headline **No INI present**; body *"This installation has no `zoo.ini` on disk. Create a
  fresh one from the launcher's defaults, or locate an existing file to copy in."*; buttons `Create zoo.ini from defaults` (default) and `Locate existing zoo.ini…`
  (ellipsis: it opens a picker — conventions §5). Fix INI group, when missing: **No INI found**, *"Create a fresh `zoo.ini` from the launcher's defaults, or locate
  an existing file to copy in. Any earlier values stay in history."*, buttons `Create` and `Locate…` stacked on the right. Read-only strips:
  *"This installation's folder is read-only for your account, so changes to `zoo.ini` cannot be saved. Uninstalling and reinstalling Zoo Tycoon restores normal
  write access."* and *"`zoo.ini` is marked read-only, so changes cannot be saved. Clear Read-only in the file's Properties to fix it."*
- **C10 — The General tab points at the fix.** Cannot Play's missing-INI-only message gains a sentence: *"Use the INI Config tab to create or locate one."*

## 5. Behaviour

### 5.1 Create zoo.ini from defaults

`CreateDefaultIniCommand(InstallationId)` → load the row (`Installation.NotFound`) → delete orphaned temp files → render the C1 document → create-only write (C2) →
set `HasIni = true` on the row and publish `InstallationChangedMessage` → synchronise into snapshots (C4) → return the new validity. Access denied → `Ini.ReadOnly`
(C8); other IO failure → `Ini.WriteFailed`.

### 5.2 Locate existing zoo.ini

The view asks `IDialogService` for a file (a new `PickFileAsync`); cancel does nothing. `LocateIniCommand(InstallationId, SourcePath)` → load the row → read and check
the source (C3; unreadable → `Ini.ReadFailed`) → then as §5.1 from the create-only write, writing the source text unchanged (byte-identical, Latin-1).

### 5.3 After success

- **From the INI tab:** the tab is busy while the command runs (both buttons disabled); success re-boots onto the INI tab (C5); failure shows the error in a
  `Cannot Create zoo.ini` / `Cannot Copy zoo.ini` dialogue and leaves the box in place; `Ini.AlreadyExists` also re-boots.
- **From the Fix dialogue:** the *Fix INI* group flips to *INI present*, `HasChanges` becomes true, and closing the dialogue re-boots as today. Buttons are disabled
  while `HasIni` is true or while busy. An `InvalidNoExeOrIni` installation offers the INI actions only once `zoo.exe` is found (SDD §7.2.5): before that the
  buttons are disabled.

### 5.4 Read-only detection

`BootHandler`'s Verify step runs the probe (C6) when `zoo.exe` is present, and the boot result carries the write access to the Play view. The INI tab shows the C9
strip for `FolderReadOnly` / `FileReadOnly`; `Writable` and `Unknown` show nothing. The probe never fails the boot. The write paths (C8) classify an access-denied
failure with the same probe so their message matches the strip.

## 6. Architecture notes

- **Domain:** the default-document factory (C1) beside `ZooIniDefaults`; a write-access enumeration.
- **Application:** `CreateDefaultIniCommand`, `LocateIniCommand` (+ validators: non-empty id; source path rooted), a shared create-then-import step, new
  `IniErrors` (`AlreadyExists`, `NotAnIni`, `ReadOnly`), an `IInstallationWriteProbe` abstraction, the probe call in `BootHandler`, and the access-denied mapping in
  `IniWritePipeline`. `IIniFileStore` gains a create-only write.
- **Infrastructure:** the create-only write and the probe, both through `IFileSystem`.
- **Desktop:** `IniPlaceholderViewModel` gains the two commands for its No INI case (other cases keep none); the Fix dialogue's *Fix INI* group gets its commands;
  `IDialogService.PickFileAsync` (and every stub implementation); a re-boot callback from `PlayViewModel` into the INI tab; the read-only strip; the General tab
  sentence (C10).

## 7. Testing

- **Domain:** the default document parses back to every non-null default, in registry order, with CRLF.
- **Application:** Create and Locate — success on an empty store (first import), success with a surviving `Current` (Manual archive, `Original` untouched),
  `AlreadyExists` (never overwrites), `NotAnIni`, `ReadOnly` on access denied, swallowed post-write store failure; boot carries each probe result; the pipeline maps
  access denied to `Ini.ReadOnly`.
- **Infrastructure:** create-only write refuses an existing file and cleans its temp on failure; the probe against a temp folder (writable; read-only attribute);
  probe leaves no file behind.
- **Desktop:** placeholder commands, busy gating, re-boot callback invoked on success and on `AlreadyExists`, error dialogue on failure; Fix dialogue INI commands and
  gating; read-only strip visibility per result.
- **In the running app** (controller drive): copy a sample installation into a scratch folder outside the repository (reading `SampleInstallations/` only — never
  writing there), delete the copy's `zoo.ini`, register it, and use Create, then repeat with Locate. For the read-only strip, mark the scratch copy's `zoo.ini`
  Read-only. Remove the scratch installation from the launcher afterwards.

## 8. SDD amendments (same milestone)

- §7.4: replace the MVP note and button list with the two actions as built; §7.2.5: the *Fix INI* options; §9.3.1: the No INI case gains its buttons and the
  read-only strip; §9.5: the *Fix INI* group; §7.1.2 Verify: the write probe; §14.5 row 9: MVP mitigation implemented.
- §13.5: delete DW-01 and DW-12; add **DW-40 — Copy From Another Installation** (SDD §7.4's third action, with its installation selector).
- Revision 1.12.
