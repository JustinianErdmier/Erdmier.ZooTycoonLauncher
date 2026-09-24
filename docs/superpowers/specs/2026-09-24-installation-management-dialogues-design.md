# Installation Management Dialogues — Design Spec

**Date:** 2026-09-24  
**Status:** Approved  
**Related SDD sections:** §3.1 (goal 2), §7.2.1, §7.2.3–§7.2.6, §9.4, §9.5, §13.2 (Installation lifecycle)

---

## 1. Overview

The Installation Manager (shipped as a scaffold in PR #4) has four placeholder buttons: **Info**, **Edit**, **Delete** and **Fix**. This slice makes all four real, completing
MVP goal 2 ("Manage installations through a dedicated dialogue: add, edit, delete, fix invalid, view info") and the §13.2 *Installation lifecycle* milestone.

It also switches on the change-notification path the SDD describes (§7.2.1–§7.2.4: handlers publish messages, grids refresh themselves), refreshes the main window after the
manager changes the open installation, wires two entry points that already exist in the UI, and adds two small fixes to the Add Installation dialogue.

Success means: every Manager button works; both grids (picker and Manager) refresh themselves after any change; the main window reflects a renamed, fixed, or deleted open
installation; and the new Application logic is covered by unit tests.

---

## 2. Decisions

| # | Decision | Source | Notes |
|---|---|---|---|
| D1 | Fix repairs the **EXE** only. The INI group box shows status; its **Create** button stays disabled until the INI Config slice. | User, 2026-09-24 | Create needs `ZooIniDefaults` + the INI writer (§5.3, §8), which do not exist yet. Locate Manually / Copy From Another are also deferred. |
| D2 | Edit shows the folder **read-only with no Browse button**. Relocation happens only through Fix. | User | Mock-up shows Browse; SDD §7.2.3 limits Edit to name + default. |
| D3 | **Handlers publish** change messages through an Application abstraction; Desktop implements it with the CommunityToolkit messenger. | User | Matches SDD §7.2.1–§7.2.4 ("Publishes `InstallationChangedMessage`"). |
| D4 | Info's **History entries** row shows "—". | Controller | INI snapshots are not written until the INI Config slice; a count would always read 0. |
| D5 | Add, with **zero** installations registered: Name is **pre-filled** with `Main` (an editable value) and **Mark as default** is ticked and read-only. | User | SDD §7.2.1 calls `Main` a *placeholder*; the user's instruction (pre-filled value) wins. The ≥ 1 case (`Installation N`) is unchanged by this slice. |
| D6 | After the manager closes with changes while an installation is open, the main window **re-runs a pointed boot** of that installation. | Controller | Covers rename, Cannot Play → Ready after a fix, and delete (falls back to normal resolution, per §7.2.4). |
| D7 | Relocate **rejects** a folder with no `zoo.exe`. | SDD §7.2.5 | "If still no `zoo.exe`, surface a non-blocking error" — the path must not change. |

---

## 3. Application layer

### 3.1 `IApplicationEventPublisher`

New abstraction in `Application/Common/Abstractions/`:

```csharp
public interface IApplicationEventPublisher
{
    void Publish<TMessage>(TMessage message) where TMessage : class;
}
```

Fire-and-forget by design: publishing never fails a command. Handlers call it only **after** their changes are persisted.

### 3.2 What each handler publishes

The four message records already exist in `Application/Common/Messaging/`.

| Handler | Always | Additionally |
|---|---|---|
| `AddInstallationHandler` | `InstallationAddedMessage(id)` | `DefaultInstallationChangedMessage(id)` when the new row became the default |
| `UpdateInstallationHandler` | `InstallationChangedMessage(id)` | `DefaultInstallationChangedMessage(id)` when `MakeDefault` moved the default to this row |
| `DeleteInstallationHandler` | `InstallationDeletedMessage(id)` | `DefaultInstallationChangedMessage(newDefaultId)` when the removed row was the default (`null` when none remain) |
| `RelocateInstallationHandler` | `InstallationChangedMessage(id)` | — |
| `SetDefaultInstallationHandler` | `DefaultInstallationChangedMessage(id)` | only when the default actually changed |
| `VerifyInstallationHandler` | — | `InstallationChangedMessage(id)` when the re-probe changed `HasExe` / `HasIni` (drift persisted) |

`BootHandler` does not publish: boot replaces the main window's content, so no grid is listening.

### 3.3 Relocate rejects a folder without `zoo.exe`

`RelocateInstallationHandler` returns `Error.Validation("Installation.ExeMissing", …)` when the chosen folder exists but contains no `zoo.exe`, leaving the row untouched. The
existing `Installation.PathMissing` and path-uniqueness checks are unchanged.

### 3.4 Deletion preview

New query slice `Installations/Delete/PreviewInstallationDeletionQuery(Guid InstallationId)` returning
`ErrorOr<InstallationDeletionPreview>` where `InstallationDeletionPreview(string Name, bool IsDefault, string? PromotedName)`:

- `IsDefault` — whether the row is the current default.
- `PromotedName` — when `IsDefault`, the name of the installation the delete would promote; `null` when it is the last installation (or when not the default).
- `Installation.NotFound` when the id no longer exists.

`IInstallationRepository.FindDefaultPromotionCandidateAsync` gains an optional `Guid? excludeId` parameter. The preview passes the id being deleted; `DeleteInstallationHandler`
keeps calling it after the delete (no exclusion needed). Both therefore use the same ordering (`lower(Name)`), so the preview cannot disagree with the actual promotion.

---

## 4. Desktop layer — change flow

### 4.1 `MessengerEventPublisher`

`Composition/MessengerEventPublisher : IApplicationEventPublisher` sends through the registered `IMessenger`, always posting to the UI thread via `Dispatcher.UIThread.Post`
(even when already called from it), and catches and logs any exception a recipient throws rather than letting it propagate back into the publishing handler. Registered as
a singleton in `AddDesktop`.

### 4.2 Grids refresh themselves

- `InstallationGridViewModel` already subscribes to all four messages and posts reloads to the UI thread. Reloads become **coalesced**: if a load is in flight when another
  message arrives, one follow-up reload runs after it instead of a second concurrent query (Delete of the default publishes two messages).
- Hosts drop their explicit post-dialogue reloads (picker Add, Manager Add, the picker reload after the Manager closes); the messages now do that work.

### 4.3 `IDialogService` additions

| Member | Returns |
|---|---|
| `Task ShowInstallationInfoAsync(Guid installationId)` | — |
| `Task<bool> ShowEditInstallationAsync(Guid installationId)` | `true` when saved |
| `Task<bool> ShowDeleteInstallationAsync(Guid installationId)` | `true` when deleted |
| `Task<bool> ShowFixInstallationAsync(Guid installationId)` | `true` when anything changed |

All modals are owned by the active window (existing `ResolveOwner`). The `file`-scoped `NoOpDialogService` in `PlayViewModel.cs` gains matching no-op members.

### 4.4 Installation Manager wiring

`InstallationManagerDialogViewModel`'s Info / Edit / Delete / Fix commands call the matching `IDialogService` members for `Grid.SelectedRow`. Any `true` result sets the
existing `HasChanges` flag, which `ShowInstallationManagerAsync` already returns to callers. Enablement rules are unchanged (Info / Edit / Delete need a selection; Fix needs an
invalid selection).

### 4.5 Main window after the manager closes

`MainWindowViewModel.ManageInstallationsAsync`, when the manager reports a change:

| Active state | Action |
|---|---|
| Play (Ready to Play / Cannot Play) | Pointed boot of the open installation: `RunBootAsync(openInstallationId)`. A deleted installation falls back to normal resolution. |
| No Game Installation Found | Normal boot (unchanged). |
| Picker | Nothing — its grid refreshed from the messages. |

### 4.6 Newly wired entry points

- **Cannot Play → "Open Installation Manager…"** (General tab): runs the main window's `ManageInstallationsCommand`, so §4.5 applies. Plumbed as a
  `Func<CancellationToken, Task>` callback from `MainWindowViewModel` through `PlayViewModel` into `GeneralTabViewModel`.
- **Picker → Info**: opens the Info dialogue for the selected row.

---

## 5. Dialogues

All dialogues are `ClassicWindow` modals (`WindowStartupLocation="CenterOwner"`, `SizeToContent="Height"`, `CanResize="False"`, `ShowInTaskbar="False"`) mirroring
`AddInstallationDialogView`'s chrome, each a view + view-model pair under `Views/Dialogs/` and `ViewModels/Dialogs/`. Layouts follow the hi-fi mock-ups in
`docs/user-interface-design/HiFiMockupScreenshots/`, adjusted by the decisions in §2.

### 5.1 Shared installation form

"Same view as Add Installation" (§7.2.3) is achieved by extracting the shared inputs into an `InstallationFormView` + `InstallationFormViewModel` pair under
`Views/Common/` and `ViewModels/Common/` (the same composition pattern as `InstallationGridView`):

- **Name** `TextBox`, **Folder** `TextBox` + **Browse** button, **Mark as default installation** `CheckBox`, and the inline error `TextBlock`.
- Flags set by the host: `IsFolderReadOnly`, `IsBrowseVisible`, `IsDefaultLocked` (checkbox ticked and disabled).
- The host dialogue owns its heading, Save / Cancel, and the command it dispatches.

### 5.2 Add Installation (changed)

Hosts the shared form; behaviour otherwise unchanged. **New (D5):** before showing, the dialogue checks how many installations exist. With zero, Name is pre-filled with `Main`
and Mark as default is ticked and locked (`IsDefaultLocked`), mirroring `AddInstallationHandler`, which already makes the first installation the default.

### 5.3 Edit Installation (new)

- Title **Edit Installation**; heading "Edit installation" with the muted line "Rename this installation or change which one is the launcher's default."
- Name editable; Folder read-only with **no Browse** (D2); Mark as default pre-ticked and **locked** when the installation is already the default (§7.2.3).
- Save dispatches `UpdateInstallationCommand(id, name, makeDefault)`; the existing validator enforces name uniqueness excluding this row. Returns `true` on success.

### 5.4 Installation Info (new)

- Information icon; label / value rows: **Name** (bold), **Path** (monospace, wraps), **Status** (coloured via `ColourTokenToBrushConverter`), **Default** (Yes / No),
  **Added**, **Last opened**, **Last played**, **History entries**.
- Timestamps are converted from UTC to local time and formatted en-GB as `d MMM yyyy HH:mm` (e.g. `12 Mar 2025 18:02`); "—" when null. History entries shows "—" (D4).
- Data from the existing `GetInstallationByIdQuery`. **Close** is the default and cancel button.

### 5.5 Delete Installation (new)

- Warning icon; "Remove **{Name}** from the launcher's registry?"; muted: "The launcher's database for this installation, including INI history, will be deleted. The game
  folder on disk is left untouched."
- When the row is the default: "**{PromotedName}** will be promoted to default." — or, for the last installation, "No installations will remain."
- Data from `PreviewInstallationDeletionQuery`. **Delete** dispatches `DeleteInstallationCommand`; **Cancel** is the default and cancel button. Returns `true` when deleted.

### 5.6 Fix Installation (new)

- Warning icon in the title bar; intro: "Recover an installation by locating missing files or restoring a fresh INI. The launcher never patches `zoo.exe`." Then two group boxes
  and **OK** (default).
- **Fix EXE** — present: green tick, "EXE present", "`zoo.exe` was located at the expected path.", **Locate** disabled. Missing: red cross, "No EXE found!", "`zoo.exe` is
  missing from this installation's directory. Browse to a folder that contains `zoo.exe`.", **Locate** enabled.
  - Locate opens the folder picker at the current path and dispatches `RelocateInstallationCommand`. `Installation.ExeMissing` (or any error) shows as inline red text in the
    box and nothing moves. On success the box flips to present and the INI box re-evaluates against the new folder.
- **Fix INI** — present: green tick, "INI present", "`zoo.ini` was found in the installation folder." Missing: red cross, "No INI found", "INI repair arrives with the INI
  Config slice." **Create** is disabled in both states (D1).
- Status comes from `VerifyInstallationQuery`, which re-probes the folder on open (and after a relocation) and persists any drift — the stored flags are only refreshed at
  boot, so they can be stale. The title is the fixed "Fix Installation" (as both mock-ups show), not the installation's name; `GetInstallationByIdQuery` instead supplies
  the folder path for the picker and the stored flags, which are compared against the re-probe's result to detect drift. Returns `true` when a relocation succeeded or the
  re-probe persisted drift.

---

## 6. Error handling

- **Expected failures** (`ErrorOr` validation / conflict, `Installation.ExeMissing`) show as inline red text; the dialogue stays open (the existing Add pattern).
- **Installation no longer exists** (deleted since the grid loaded): the dialogue shows "This installation no longer exists." and only Close / Cancel remains enabled.
- **Unexpected exceptions** are logged (`ILogger<T>`) and shown as a generic inline message.
- Publishing never throws into a handler (fire-and-forget); a failed grid reload leaves the previous rows in place.
- Delete's per-installation database clean-up is best-effort: a locked `{id}.db` file is logged and left behind rather than failing the operation — the registry change
  still completes and publishes.

---

## 7. Testing

- **Application unit tests (TDD)** — the messages each handler publishes, including the default-changed cases (§3.2); Relocate rejecting a folder without `zoo.exe` and
  leaving the row untouched; the deletion preview for a default with a successor, the last installation, a non-default, and an unknown id.
- **Infrastructure integration test** — `FindDefaultPromotionCandidateAsync(excludeId)` skips the excluded row and keeps the `lower(Name)` ordering.
- **Desktop** — solution build (0 warnings) + architecture tests; there is no Desktop test project. A manual checklist goes in the PR description.

---

## 8. File layout

```text
Source/Erdmier.ZooTycoonLauncher.Application/
├── Common/Abstractions/IApplicationEventPublisher.cs                     new
├── Common/Abstractions/IInstallationRepository.cs                         excludeId parameter
└── Installations/
    ├── Add/AddInstallationHandler.cs                                      publish
    ├── Delete/DeleteInstallationHandler.cs                                publish
    ├── Delete/PreviewInstallationDeletionQuery.cs                         new
    ├── Delete/PreviewInstallationDeletionHandler.cs                       new
    ├── Delete/InstallationDeletionPreview.cs                              new
    ├── Relocate/RelocateInstallationHandler.cs                            ExeMissing + publish
    ├── SetDefault/SetDefaultInstallationHandler.cs                        publish
    ├── Update/UpdateInstallationHandler.cs                                publish
    └── Verify/VerifyInstallationHandler.cs                                publish on drift
Source/Erdmier.ZooTycoonLauncher.Infrastructure/…/InstallationRepository.cs   excludeId
Source/Erdmier.ZooTycoonLauncher.Desktop/
├── Composition/MessengerEventPublisher.cs                                 new
├── Composition/IDialogService.cs, AvaloniaDialogService.cs                four members
├── Composition/DesktopServiceCollectionExtensions.cs                      registrations
├── ViewModels/Common/InstallationFormViewModel.cs                         new
├── ViewModels/Common/InstallationGridViewModel.cs                         coalesced reloads
├── ViewModels/Dialogs/{Edit,Delete,Fix}InstallationDialogViewModel.cs     new
├── ViewModels/Dialogs/InstallationInfoDialogViewModel.cs                  new
├── ViewModels/Dialogs/AddInstallationDialogViewModel.cs                   hosts the form; D5
├── ViewModels/Dialogs/InstallationManagerDialogViewModel.cs               commands wired
├── ViewModels/Boot/{OpenGameInstallation,Play}ViewModel.cs                Info; manager callback
├── ViewModels/Tabs/GeneralTabViewModel.cs                                 manager command
├── ViewModels/MainWindowViewModel.cs                                      §4.5 refresh
├── Views/Common/InstallationFormView.axaml(.cs)                           new
├── Views/Dialogs/{Edit,Delete,Fix}InstallationDialogView.axaml(.cs)       new
├── Views/Dialogs/InstallationInfoDialogView.axaml(.cs)                    new
├── Views/Dialogs/AddInstallationDialogView.axaml                          hosts the form
└── Views/Tabs/GeneralTabView.axaml                                        button command
```

---

## 9. Out of scope

- INI repair in Fix — Create Default, Locate Manually, Copy From Another Installation (INI Config slice).
- Opening an installation from the Manager (Open button, double-click) and the SDD version of File → Open Installation… (a later milestone).
- The `Installation N` name placeholder for the ≥ 1 case of Add (§7.2.1).
- Settings dialogue, About dialogue.
