# Restore Previous INI — Design Spec

> **Milestone:** 9 — Restore Previous INI (Epic 1 — MVP)
> **Date:** 2026-10-20
> **Author:** Justinian
> **Status:** Approved for implementation
> **Authoritative spec:** [1-zoo-tycoon-launcher-master-design.md](../1-zoo-tycoon-launcher-master-design.md) §6.3, §7.3.2, §7.3.3, §7.6, §8.2, §9.3, §9.4, §9.5, §9.7
> **Deferred work taken:** DW-02 (Restore Previous INI dialogue), DW-03 (Restore defaults and Undo), DW-08 (Installation Info's History entries)
> **Design reference:** [`HiFiPrototype/src/dialogs.jsx`](../../UserInterfaceDesign/HiFiPrototype/src/dialogs.jsx) `HistoryDialog` (lines 427–506);
> [`HiFiPrototype/src/ini-config.jsx`](../../UserInterfaceDesign/HiFiPrototype/src/ini-config.jsx) `IniEditor` footer

---

## 1. Goal

Every Save already archives the previous `Current` snapshot to `Historical`, but nothing lets the user see or use that history. This milestone makes it usable: a
**Restore Previous INI** dialogue lists every earlier version of `zoo.ini`, shows exactly which settings differ from today's, and rolls the file back to the chosen
version through the same safe write path as Save. Alongside it, the INI editor's footer is finished: **Restore defaults** works, and the footer buttons take their Win9x
names. A small Win9x label sweep across the app rides along, because the no-ellipsis rule it corrects was retired at the start of this milestone.

---

## 2. Decisions taken during brainstorming

| #  | Question                                               | Decision                                                                                                                                                       |
|----|--------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | Footer button names                                    | Win9x standard commands: `Revert` becomes **`Reset`**; `Undo` becomes **`Restore previous…`**. Footer: `Restore previous…` · `Restore defaults` · `Save` · `Reset`. |
| 2  | Ellipses                                               | The SDD's blanket no-ellipsis rule is retired for the Win9x rule (conventions §5): an ellipsis marks a command that needs more input before it completes.       |
| 3  | Label sweep                                            | Fix every existing label the Win9x rule now contradicts, in this milestone (§7.6).                                                                             |
| 4  | `Restore previous…` with unsaved edits                 | The standard *"Do you want to save the changes to `zoo.ini`?"* prompt. `Yes` saves then opens; `No` discards then opens; `Cancel` stays in the editor.        |
| 5  | `Restore previous…` with no history                    | Disabled when the installation has no `Historical` snapshots.                                                                                                  |
| 6  | Initial selection in the dialogue                      | The newest snapshot is selected and its diff shown on open (amends SDD §7.6, which said nothing was selected).                                                 |
| 7  | `Restore defaults`                                     | In scope, as an **editor-only** action: every visible setting's default is loaded into the form as unsaved edits. Nothing is written until `Save`.            |
| 8  | Settings → Options                                     | Not in this milestone; recorded as DW-37.                                                                                                                       |
| 9  | Design reference                                       | The Claude Design prototype source is imported under `Docs/UserInterfaceDesign/HiFiPrototype/` and referenced from CLAUDE.md for this and later milestones.    |

**Calls made by the author on the user's behalf** (each also goes in the ledger as a ruling when the plan runs):

- **C1 — Restore covers user settings only.** The diff and the restore touch only `IniKeyRole.UserSetting` keys — the 46 keys the editor shows. Game-managed keys
  (window position, last file, tutorial flags, counters) are never listed and never rolled back; rolling back the game's own bookkeeping would surprise the user and buy
  nothing. `[scenario]` is not in the key registry, so it is untouched as well.
- **C2 — The `Original` snapshot is listed.** The dialogue lists every `Historical` snapshot newest first, then the `Original` snapshot as the last row (its trigger
  reads `OriginalImport`), so the first-imported state is restorable. The prototype shows the same row.
- **C3 — Comparison and restore use effective values.** A stored raw value that is invalid for its key (out of range, unparsable) is shown and restored as that key's
  default — exactly what the editor would display for it. The launcher therefore never writes an invalid value, and the Save validator's guarantees hold for a restore.
- **C4 — Presence rules.** If the chosen snapshot has no value for a key that `Current` has (the key was inserted by a later Save), the row is omitted and the key is
  left alone: a restore never deletes a key from `zoo.ini`. A key present in the snapshot but absent from `Current` is always a difference — even when the snapshot
  holds the default — because restoring it writes the key into the file; its `Current` cell reads `(not set)`.
  **The user is told about kept keys** (maintainer's addition): when the selected snapshot lacks keys that `Current` has, the dialogue says so under the diff and names
  them, and the restore confirmation repeats it (§7.4), so a restore never silently leaves newer settings in place.
- **C8 — The dialogue's rows are the restore's edit set.** One Domain diff function (§4) decides both what the dialogue shows and what the restore writes, so
  *N changes* on screen always means exactly *N* keys written.
- **C5 — Installation Info's History entries** (DW-08) shows the count of `Historical` snapshots, since it needs the same history read and costs almost nothing.
- **C6 — Captured times are shown in local time**, formatted `d MMM yyyy HH:mm` like the Installation Info dialogue, and stored UTC as always. SDD §9.7's "(UTC)" is
  amended to match (localisation at the UI boundary).
- **C7 — The restore confirmation** is a Win95 message box: *"Restore `zoo.ini` to the version captured `<local time>`? The current version is archived to history
  first, so this can be undone."* When the snapshot has kept keys (C4) it adds: *"`N` settings are not in this snapshot and will keep their current values."*
  Buttons `Yes` (default) and `No`.

---

## 3. Scope

### 3.1 In scope

1. **History read** — an Application query returning the installation's snapshot list (newest `Historical` first, then `Original`), each with its per-key diff
   against `Current`.
2. **Restore command** — `RestoreIniSnapshotCommand`, writing a chosen snapshot's user-setting values through the SDD §8.2 ordering.
3. **Restore Previous INI dialogue** — view and view model pair, opened from the editor footer (SDD §9.7, the prototype's `HistoryDialog`).
4. **Editor footer** — `Restore previous…`, `Restore defaults`, `Save`, `Reset`, with the unsaved-changes prompt in front of `Restore previous…`.
5. **Win9x label sweep** (§7.6).
6. **Installation Info History entries** (DW-08).
7. **SDD amendments** (§10) and the manual test plan stubs (§9.5).

### 3.2 Out of scope

| Item                                                                                  | Where it goes                                                    |
|---------------------------------------------------------------------------------------|------------------------------------------------------------------|
| The `CorruptedSource` restore mode (SDD §7.5, §7.6, §8.2's closing paragraph)         | Stays future work — the app has no corrupted-INI state to open it from. |
| `ResetIniToDefaultsCommand` (delete and rewrite `zoo.ini`, SDD §7.5)                  | Same — it belongs to the corrupted-INI recovery flow, not the editor. |
| Settings → Options rename                                                             | DW-37.                                                           |
| Historical-snapshot retention or deletion                                             | V3 (SDD §6.5).                                                   |
| Restoring `[scenario]` or game-managed keys                                           | Not planned (C1).                                                |

---

## 4. Domain layer

No new entities and no migration: `Snapshots` and `IniValues` already hold everything (SDD §6.3), and the `(Kind, CapturedUtc DESC)` index already serves the list.

The diff rule lives in the Domain so the Application query and its tests share one definition. A pure function compares two value maps
(`IReadOnlyDictionary<IniKeyId, string?>`) over the user-setting keys of `ZooIniDefaults`:

- Skip a key whose snapshot value is absent (C4).
- A key absent from `Current` but present in the snapshot is a row, whatever its value (C4).
- Otherwise compare `spec.EffectiveValue(current)` with `spec.EffectiveValue(snapshot)` using `spec.AreEquivalent` (kind-aware: `Bool` `1` and `true` agree; empty
  nullable values agree) (C3).
- Emit one row per disagreeing key: `IniKeyId`, the current effective value (or `null` when `Current` lacks the key), and the snapshot's effective value. The rows are
  the restore's edit set (C8): the edit for a row is its snapshot effective value.
- Rows are ordered by section in the editor's order, then by key in registry order.
- Separately, return the **kept keys**: user-setting keys that `Current` has but the snapshot lacks (C4), in the same order. They are not rows, are not counted in
  `Δ keys`, and are never edits.

Where the function lives and what it is called are the builder's choice, within the one-type-per-file and folder-mirrors-namespace rules.

---

## 5. Application layer

### 5.1 Snapshot reads

`IIniSnapshotTransaction` exposes only `Current` today. Add the reads the history query needs: list the `Historical` snapshots newest first, read the `Original`
snapshot, read one snapshot by id, each with its values, and count the `Historical` snapshots. Whether these sit on the transaction or on `IIniSnapshotRepository` is
the builder's choice. The history query and the restore run them inside their synchronising transaction; the Installation Info count (§5.5) must not open that
transaction at all.

### 5.2 `IniConfig/History/` — `GetIniHistoryQuery`

`GetIniHistoryQuery(Guid InstallationId)` → `ErrorOr<IniHistoryResult>`.

1. Load the installation (`IniErrors.InstallationNotFound` when missing).
2. **Synchronise first** — reconcile `Current` with the file on disk exactly as `GetIniConfigQuery` does, so the diff compares against the real file, and any external
   user-setting change is archived as `Manual` and so appears in the list.
3. Read `Current`, the `Historical` snapshots (newest first) and the `Original` snapshot.
4. For each snapshot, compute its diff against `Current` (§4). `Δ keys` is the row count.

`IniHistoryResult` carries the ordered entries; each entry has the snapshot id, `Kind`, `Trigger`, `CapturedUtc`, its diff rows and its kept keys. Snapshots are small
(about 100 values, SDD §6.3), so computing every diff up front is cheap and keeps the dialogue free of further round trips.

Store and file failures map onto the existing `IniErrors` exactly as the Get path does.

### 5.3 `IniConfig/Restore/` — `RestoreIniSnapshotCommand`

`RestoreIniSnapshotCommand(Guid InstallationId, Guid SnapshotId)` → `ErrorOr<IniConfigResult>`, validated (non-empty ids) by a FluentValidation validator.

It follows SDD §8.2 step for step, with the edit set derived from the snapshot instead of from the user:

1. Read `zoo.ini`; open the per-installation transaction.
2. Reconcile `Current` with the on-disk text (tiered drift).
3. Read the chosen snapshot (`Historical` or `Original`); a missing id returns a new `IniErrors.SnapshotNotFound`.
4. Build the edits with the §4 diff function, comparing the snapshot with the **reconciled** values (C8). Save's raw-value match filter is not used here.
5. No edits left → commit and return the reconciled values (the same no-op path as Save).
6. Archive `Current` (`Trigger = LauncherGui`), apply the edits to the on-disk document with `IniDocument.SetValue`, write via temp file + move, update `Current`
   (`Source = LauncherGui`), commit. Post-write store failures are logged and swallowed, as in Save (SDD §7.7).

Steps 1, 2 and 5–6 are Save's pipeline; only the edit set differs (Save filters the user's edits with its raw `AreEquivalent` check; Restore takes the diff rows).
**Extract the pipeline** from `SaveIniHandler` into a shared component in `IniConfig/Common/` that takes a way to derive the edits from the reconciled values, rather
than duplicating it; Save's behaviour and tests must not change. The extracted component's name and shape are the builder's choice.

### 5.4 `IniConfigResult` gains the history count

`IniConfigResult` gains `int HistoricalCount` — the number of `Historical` snapshots after the operation. Get, Save and Restore all fill it, so the editor can enable
`Restore previous…` (decision 5) from whichever result it last loaded, with no extra query. Get builds its result in `IniSnapshotService.LoadAsync`, which the boot
sync also uses; fill the count there with one `COUNT` inside the transaction it already opens (*C9 — the extra read on the boot path is accepted*). Every existing
`new IniConfigResult(…)` call — handlers, the editor's designer constructor, tests — is updated.

### 5.5 Installation Info (DW-08)

The Info dialogue shows the **stored** `Historical` count without synchronising (*C10*): a new lightweight query (for example `GetIniHistoryCountQuery`) reads the
count, and when the installation's database file does not exist it answers `0` without creating or migrating one. That needs a repository read which checks for the
file first instead of going through `BeginAsync` (which migrates). The dialogue shows `24 snapshots`, `1 snapshot` or `0 snapshots`, and `—` when the read fails.
`GetIniHistoryQuery` is not reused here, because its synchronise can create the database, import an `Original`, or archive a `Manual` snapshot.

---

## 6. Infrastructure layer

Implement the §5.1 reads in `Persistence/Installation/` over the existing context: `Historical` ordered by `CapturedUtc` descending, values included, no tracking needed
for read-only paths. No schema change, no migration.

---

## 7. Desktop layer

### 7.1 Editor footer (`IniEditorView.axaml`, `IniEditorViewModel`)

Footer, left to right after the status label: **`Restore previous…`** · **`Restore defaults`** · **`Save`** (default) · **`Reset`**.

| Button              | Command                         | Enabled when                                                                                     |
|---------------------|---------------------------------|--------------------------------------------------------------------------------------------------|
| `Restore previous…` | `RestorePreviousCommand` (new)  | Not busy, a mediator is present, and the last loaded result's `HistoricalCount` is above zero.   |
| `Restore defaults`  | `RestoreDefaultsCommand` (new)  | Not busy, a mediator is present, and at least one field is not already at its default.          |
| `Save`              | `SaveCommand` (unchanged)       | Unchanged.                                                                                       |
| `Reset`             | `ResetCommand` (renamed from `RevertCommand`) | Unchanged.                                                                         |

Renaming `Revert` to `Reset` is a full rename — command, private method, comments, tests — not just the label. Its behaviour is unchanged: it reloads through
`GetIniConfigQuery` (a database round trip that re-synchronises), not a purely local reset, so it also undoes `Restore defaults`.

### 7.2 `Restore defaults`

Sets every field to its key's default as an ordinary pending edit, then refreshes the dirty state; the footer shows `● Unsaved changes` and `Save` / `Reset` behave as
for any edit. No prompt — nothing is written, and `Reset` undoes it.

Field view models today expose `Load`, `Reset` and `TryGetEdit`, with `ApplyValue` protected. Add a public way to set a field to its default (for example
`ResetToDefault()`), which must also cover the derived Language field (`IniLanguageFieldViewModel`, which drives both `lang` and `sublang` and has no single `Id`).
Fields already at their default stay clean.

### 7.3 `Restore previous…`

1. If the editor has pending edits, show `IDialogService.ShowSaveChangesPromptAsync()`. `Yes` → `TrySaveAsync`; stop if the save fails. `No` → `DiscardChanges()`.
   `Cancel` → stop. The editor calls the prompt directly; it is not routed through `IPendingChangesGuard`, which guards leaving the installation, not this.
2. Open the dialogue through a new `IDialogService` method (for example `ShowRestorePreviousIniAsync(Guid installationId)`) that reports whether a restore happened and,
   if so, carries the new `IniConfigResult`.
3. On a restore, `Load` the returned result into the editor (the same path as after Save). On cancel, reload from `GetIniConfigQuery` exactly as `Reset` does
   (*C11*): the history query may have synchronised external drift into `Current`, so the editor's baseline and `HistoricalCount` must catch up.

Every `NoOpDialogService` copy (in `IniEditorViewModel`, `IniConfigTabViewModel` and `PlayViewModel`) gains the new method.

### 7.4 Restore Previous INI dialogue

A new `RestorePreviousIniDialogViewModel` + `RestorePreviousIniDialogView` pair under `ViewModels/Dialogs/` and `Views/Dialogs/` (the pair rule, SDD §9.2.3). It is a
`ClassicWindow` modal titled `Restore Previous INI`, with the committed `Assets/Win95Icons/sheetsanddocuments32.ico` as its title icon (conventions §4 calls it
`sheets-docs.ico`; it was committed under this name at 32 px only, like the other dialogues' 32 px icons), laid out as the prototype's `HistoryDialog`:

- **Intro line** (muted): *Pick a previous version of `zoo.ini` to restore. The current version is archived to history before the restore is applied.*
- **Snapshot grid** — `Captured` (local time, mono, C6), `Trigger` (`OriginalImport` / `LauncherGui` / `Manual`), `Δ keys` (right-aligned, mono). Newest first, `Original`
  last (C2). The newest row is selected on open (decision 6).
- **Diff caption row** — muted *Diff: values from the selected snapshot replace the current ones on restore.* on the left, *`N` changes* / *1 change* on the right.
- **Diff table** — `Key` (`[section]/key`, the section part muted, mono), `Current` (maroon), `Snapshot` (navy), scrolling. Column headers carry the meaning; colour is
  supplementary (SDD §9.7). An empty diff shows the centred italic message *No differences — this snapshot matches the current state.*
- **Kept-keys note** — shown under the diff table only when the selected snapshot has kept keys (C4): a warning-styled line, *Not in this snapshot, so they keep
  their current values:* followed by the keys as `[section]/key`, comma-separated and wrapping. It does not change the change count or `Restore`'s enablement.
- **Footer** — `Restore` (default; disabled when no row is selected or the diff is empty) and `Cancel`.

Loading: the dialogue sends `GetIniHistoryQuery` when it opens. The history loads before the window opens. A query error is shown in
the dialogue (as the Info dialogue shows `ErrorMessage`) and `Restore` stays disabled.

`Restore`: show the C7 confirmation; on `Yes`, send `RestoreIniSnapshotCommand` with the dialogue busy (buttons disabled); on success, close the dialogue returning the
result; on error, show it with `IDialogService.ShowErrorAsync` and keep the dialogue open so the user can try another snapshot or cancel.

The confirmation needs a Yes/No message box. Add one through `IDialogService` (the builder may generalise it as a reusable confirmation view model, since Delete and future
dialogues need the same shape) — following the existing `SaveChangesPromptViewModel` pattern.

### 7.5 Footer and dialogue copy are sentence case

Window titles stay Title Case (`Restore Previous INI`); every button and label is sentence case (conventions §8).

### 7.6 Win9x label sweep

| View                                           | Today                        | After               | Why                                    |
|------------------------------------------------|------------------------------|---------------------|----------------------------------------|
| `IniEditorView.axaml`                          | `Undo`, `Revert`             | `Restore previous…`, `Reset` | Decision 1.                   |
| `InstallationManagerDialogView.axaml`          | `Add`, `Edit`, `Fix`         | `Add…`, `Edit…`, `Fix…` | Each opens a dialogue that collects input. |
| `OpenGameInstallationView.axaml`               | `Add`, `Manage`              | `Add…`, `Manage…`   | Same.                                  |
| `FixInstallationDialogView.axaml`              | `Locate`                     | `Locate…`           | Opens a file picker.                   |

Unchanged on purpose: `Info` (shows information only), `Delete` (asks only for a confirmation), `Create` and `Open` (complete on the spot), `Browse…` and the File menu items
(already correct). Update any test or architecture test that asserts on an old label.

---

## 8. Failure model

| Situation                                                  | Behaviour                                                                                                              |
|------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------|
| History query fails (store or file)                        | Dialogue shows the error; `Restore` disabled; `Cancel` closes.                                                         |
| Snapshot vanished between list and restore                 | `IniErrors.SnapshotNotFound`; error dialogue; the dialogue stays open.                                                 |
| `zoo.ini` missing or unreadable at restore time            | `IniErrors.Missing` / `ReadFailed`; nothing written; error dialogue.                                                   |
| Write fails                                                 | `IniErrors.WriteFailed`; the archive rolls back with the transaction; file and database keep their previous state.     |
| Snapshot cannot be recorded after the write                 | Logged and swallowed; the restore succeeds; the next synchronise adopts the file as `Manual` drift (SDD §7.7).          |
| The file changed externally since the dialogue opened       | Step 2 of the restore reconciles it first (archived as `Manual` if user settings changed); the snapshot's values still win. |
| `Yes` on the save prompt but the save fails                 | The save path already shows its error; the dialogue does not open.                                                     |

---

## 9. Testing strategy

Follow SDD §11 per layer; tests are written test-first per task.

### 9.1 `Domain.Tests.Unit`

The diff function: equal maps → no rows; game-managed differences ignored; a key absent from the snapshot is not a row but is returned as a kept key (C4); a key
absent from `Current` reported as `null` current; invalid raw values compared by effective value (C3); `Bool` `1`/`true` equivalence; ordering by section then key.

### 9.2 `Application.Tests.Unit`

- `GetIniHistoryQuery`: ordering (`Historical` newest first, `Original` last); `Δ keys` per entry; synchronises before reading; installation and store errors.
- `RestoreIniSnapshotCommand`: archives `Current` as `LauncherGui` and writes only the differing user settings; leaves game-managed and absent keys alone; restores an
  invalid stored value as the default; no-op when nothing differs (no archive, no write); `SnapshotNotFound`; write failure rolls back; post-write store failure is
  swallowed; external drift is reconciled first.
- The extracted shared pipeline: Save's existing tests still pass unchanged.
- `HistoricalCount` is filled by Get, Save and Restore.

### 9.3 `Infrastructure.Tests.Integration`

The new reads against a real SQLite file: `Historical` order, `Original` read, read by id, values included, a missing id, the `Historical` count, and the Info
count answering `0` for a missing database file without creating it.

### 9.4 `Desktop.Tests.Unit`

- Footer: `Restore previous…` enablement by `HistoricalCount`; the prompt's three answers (and a failed save not opening the dialogue); `Restore defaults` sets every
  field including Language and marks the editor dirty; fields already at default stay clean; `Reset` (renamed) behaves as `Revert` did.
- Dialogue view model: newest row selected on load; diff and caption follow the selection; `Restore` disabled on an empty diff; confirmation `No` sends nothing;
  success closes with the result; an error keeps it open; the kept-keys note and the confirmation's extra sentence appear only when the snapshot has kept keys.
- Info: History entries text for 0, 1 and many, and `—` on failure.
- Cancelling the dialogue reloads the editor (C11).

### 9.5 Manual smoke (added to `Docs/TestPlans/manual-end-to-end-tests.md`)

Stubs for: restoring a snapshot and seeing the editor and `zoo.ini` follow it (comments and game-managed keys intact); restoring then restoring back; the unsaved-changes
prompt's three answers; `Restore previous…` disabled on a fresh installation; `Restore defaults` then `Reset`; `Restore defaults` then `Save`; the label sweep; Info's
History entries. Run against `SampleInstallations/` copies through the app.

---

## 10. SDD amendments (revision 1.9, continued)

- **§7.3.3** retitled *Restore previous button*; it opens the Restore Previous INI dialogue, behind the unsaved-changes prompt, and is disabled with no history.
- **§7.6** retitled *Restore Previous INI dialogue*; superseded detail replaced by a pointer to §9.7; the restore steps restated as §8.2 with the edit set derived from
  the snapshot, user settings only, effective values, absent keys untouched. `CorruptedSource` stays described as future work.
- **§9.3** footer row: `Restore previous…`, `Restore defaults`, `Save` (default), `Reset`, with `Restore defaults` described as loading defaults as unsaved edits.
- **§9.4** action panel labels `Add…`, `Edit…`, `Fix…`; **§9.6** picker `Add…`, `Manage…`; **§9.5** Fix dialogue `Locate…`.
- **§9.7** `Captured` in local time; the `Original` row last; newest selected on open; user settings only.
- **§9.10** unchanged (DW-37 owns the Settings rename).
- **§13.5** deletes DW-02, DW-03 and DW-08, noting it in the revision history.

---

## 11. Conventions checklist

One type per file; no project-root files; file-scoped namespaces; British English; XML docs on every public member; `[ ObservableProperty ]` / `[ RelayCommand ]`; compiled
bindings with `x:DataType`; designer constructors over `file`-scoped null objects; UTC storage with local display only at the UI; the view composition pair rule; no
`[ UsedImplicitly ]` added proactively; Markdown wrapped at 180; the `.slnx` updated for every new document.

---

## 12. Risks and open questions

- **Restoring while the game is running.** As with Save (SDD §14.5), the game may rewrite `zoo.ini` on exit and overwrite a restore. No new mitigation; the risk is
  unchanged.
- **Large histories.** The history query computes every diff up front. At the SDD's ~13 KB per snapshot this stays fast into the hundreds of snapshots; retention is V3.
- **Conventions §4 is stale on icons.** It says no ICO files are committed and names files that do not match the committed ones. Not fixed here; the plan's
  ledger records it as an outstanding item.
- **`LauncherGui` / `Manual` / `OriginalImport` are shown raw.** The SDD and prototype both use the enum names. Friendlier wording is a possible later polish item, not a
  blocker.
