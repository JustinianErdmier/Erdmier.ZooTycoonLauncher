# Boot + Basic Installation Management

The following execution paths are being manually tested end-to-end, and the necessary UI is being developed to support them. The UI should resemble the hi-fi mockup made in Claude
Design as closely as possible.

> For Claude: Using the same structure as the following sections, update this file with a stubbed section for every possible execution path in the startup process. Only stub out
> the sections — I will go through and modify them as needed. Each execution path should be unique and represent a point in the process that is either terminal, transitional, or
> progressive. Leave the Notes & Future Tasks/Goals section alone, it's there for me and not needed for this task.

## Initial Render: `LookingForZooTycoon` shown while `BootCommand` is in flight

### Testing Strategy

I set up a few different scenarios that triggered a real boot (e.g. the happy path), then artificially extended how long `BootCommand` takes to return so the transitional view
stays on screen long enough to inspect. I placed the slowdown in `MainWindowViewModel.BootAsync` as a `Task.Delay()`. Then I confirmed the looking view renders immediately
on launch and remained visible until the terminal state replaced it.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

The INI Config tab should be disabled. In the General tab, a single group box labelled "Status" should display the "Looking for Zoo Tycoon" message, intermediate progress bar, and
cycle through a list of status messages. Unless this process is artificially extended, the view should almost never actually display — modern computers are so fast that the boot
command completes before the view is even rendered.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

The main window renders immediately on the main desktop, horizontally centred, and further up vertically. Whilst in this state, the window is smaller and simply displays
the "Looking for Zoo Tycoon" message, intermediate progress bar, and cycling through a list of status messages. To ensure this view displayed long enough to test, I artificially
delayed the boot command by 30 seconds. This delay is only used for testing and will not be a part of the final product.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

I made two major changes, one starting with this view and carrying over into the following views. Firstly, I eliminated unnecessary whitespace by dynamically setting the window
size based on the view. For this view, the window is rather small. However, I've explicitly programmed it to open front and centre, so the user does not have to search for it.
Secondly, and to further emphasise the purpose of this view and how it lies outside the normal states, I stripped most of the surrounding markup from this view. There is no title
menu, no tabs, and no group box. The only chrome kept is a slimmed status bar — a single message panel plus the version, rather than the full complement of panels — so the view can
still surface progress without pulling the rest of the application shell back in. This view is purely informational and serves as a placeholder until a deterministic state can be
landed on.

### UI/UX

#### Hi-Fi Mockup

One thing worth noting is that the substantial whitespace at the bottom, under the status bar, is not intentional and is cropped out in the following high-fidelity screenshots.

![](../user-interface-design/HiFiMockupScreenshots/LookingForZooTycoonState.png)

#### Actual Implementation

![](../user-interface-design/ImplementationScreenshots/LookingForZooTycoonState.png)

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

No — and the mismatch is intentional. The mockup renders this transitional state inside the full application chrome: the File / Help menu, the General / INI Config tab strip, and a "Status" group box wrapping the magnifier icon, the "Looking for Zoo Tycoon…" heading, the progress bar, and the registry-probe status line — all sat above a large block of empty space that the mockup's own caption flags as unintentional. The implementation instead treats this state as the pure, short-lived placeholder it is. It drops the menu, the tabs, and the group box; promotes the icon, heading, progress bar, and probe line to sit directly on the window; and dynamically sizes the window to the content so the mockup's dead space disappears entirely. The window is also explicitly opened front and centre so the user is not left hunting for a small, briefly displayed dialogue. The only chrome retained is a slimmed status bar at the foot of the window — a single message panel plus the version, rather than the mockup's two message panels, version panel, and "For Help, press F1" line.

## Open Game Installation: `pref = NoInstallation`

### Testing Strategy

I set the launcher's stored startup preference to `NoInstallation`, then launched the app. I tested both with the installation registry empty and populated — neither should
matter — but it was worth running a test in both states to confirm the branch short-circuited regardless.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

The main window should have the INI Config tab disabled, and the General tab should display a status group box conveying the current state. Underneath the status group box should
be a data grid showing the user's registered installations for them to open as well as an option to add a new one.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

A simplified view similar to the boot state; the tab controls were removed. The title menu was made visible again, just for the sake of making additional methods of doing the same
actions displayed in the view possible as well as making the settings dialogue accessible. There is no data grid because this control has not been implemented yet and cannot be
because it relies on undeveloped infrastructure. However, having a working data grid does not impact the purpose of this test.

When designing the data grid that will eventually be shown here, I decided that each host (e.g. the Open Installation view or the Installation Manager dialogue) should be
responsible for defining the buttons around the grid. Because of this, the actual implementation includes the buttons, but none of them are currently functional.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

Like the boot state, I removed the tab control. It doesn't make sense to have the tabs for a state where no installation is open. Additionally, I promoted the status group box
contents to be top-level. Even though the buttons are not currently functional, I did hard-wire some basic states. Once the data grid is implemented, several buttons (e.g. `Open`
and `Info`) will require a selection in the grid, so assuming no default selection is made or if there are no installations, the buttons should be disabled by default. Also, I
removed the `Add` button from the original mockup. Whilst displaying the data grid in this state is nice for the user to quickly select and open an installation, I do not want this
view to functionally do the same things as the Installation Manager dialogue. And since the `Info` button is disabled by default, I switched it with the `Manage` button, so the
order is more visually appealing. I didn't like having a disabled, enabled, and then disabled button order.

### UI/UX

#### Hi-Fi Mockup

![](../user-interface-design/HiFiMockupScreenshots/OpenGameInstallationState.png)

#### Actual Implementation

![](../user-interface-design/ImplementationScreenshots/OpenGameInstallationState.png)

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

No — and, as with the boot state, the divergences are deliberate rather than accidental. The mockup wraps the informational content in a "Status" group box and nests it inside the full tabbed shell (General / INI Config). The implementation drops the tab control entirely — tabs make no sense in a state where no installation is open — and promotes the status content to be top-level, so the folder icon, heading, and message sit directly on the window instead of inside a labelled frame. The title menu and status bar are retained, which matches the mockup, keeping the same actions (and the settings dialogue) reachable from more than one place.

The data grid is the largest visible difference. The mockup shows a populated Name / Path / Status grid, whereas the implementation shows an "Installation Management Grid coming soon…" placeholder, because that control depends on infrastructure that has not been built yet. This gap does not undermine the test — the execution path itself is exercised start to finish regardless of whether a real grid is present.

The buttons around the grid also differ by design. I removed the mockup's `Add` button so this view does not duplicate the Installation Manager dialogue's responsibilities, and I swapped `Info` and `Manage` so the enabled/disabled ordering reads better — the mockup's Add / Info / Manage arrangement left me with a disabled, enabled, disabled sequence I found visually jarring. The remaining buttons are hard-wired to the states they will eventually resolve to: `Open` and `Info` require a grid selection, so with no selection made — or with no installations at all — they default to disabled.

One smaller copy difference follows from the same decision: the mockup's message ends with "…or add a new one", whereas the implementation ends with "…or add a new one in the manager". Because the inline `Add` button was dropped, adding an installation is no longer something this view does; the reworded message therefore points the user at the Installation Manager dialogue instead, keeping the instruction honest about where that action now lives.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

The installation data grid and action buttons are not yet implemented/functional. These features require infrastructure not yet implemented. However, for this test plan, these
features are not necessarily needed. The execution path in the startup flow has still been tested from start to finish.

## Auto Locate: `pref = DefaultInstallation`, and `DefaultId = null`, and no rows exist

### Testing Strategy

> Reset the installation registry so that no rows exist and the stored `DefaultInstallationId` is null. Ensure the startup preference is `DefaultInstallation`. Provide a real Zoo
> Tycoon installation on disk in a location one of the locator's probes can discover (registry entry, Steam library, GOG library, or wherever the probes are pointed). Launch the
> app and confirm the located path is surfaced on the resulting screen.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Auto Locate (no candidate): `pref = DefaultInstallation`, and `DefaultId = null`, and no rows exist, and the locator returns nothing

### Testing Strategy

> Same setup as the previous section — no installation rows, null `DefaultInstallationId`, `DefaultInstallation` preference — but ensure no Zoo Tycoon installation exists in any
> of the locator's probe locations. Either remove them from the disk or temporarily point the probes at empty directories. Launch the app and confirm no candidate path is shown on
> the
> resulting screen.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Default Promotion → Ready: `pref = DefaultInstallation`, `DefaultId = null`, rows ≥ 1, promoted row verifies and synchronises

### Testing Strategy

> Set up at least one installation row pointing to a valid on-disk installation (zoo.exe present, zoo.ini present and parseable). Clear the stored `DefaultInstallationId` so the
> handler is forced to promote. Ensure the startup preference is `DefaultInstallation`. Launch the app, then afterwards confirm both that `ReadyToPlay` rendered and that the
> settings row now holds the promoted installation's id.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Default Promotion → Cannot Play (`HasExe = false`): `pref = DefaultInstallation`, `DefaultId = null`, rows ≥ 1, promoted row fails verification

### Testing Strategy

> Set up at least one installation row whose path no longer contains a zoo.exe — the file can be deleted, renamed, or the row can simply point at a directory that has never had
> one. Clear the stored `DefaultInstallationId`. Ensure the startup preference is `DefaultInstallation`. Launch the app.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Default Promotion → Cannot Play (sync failure): `pref = DefaultInstallation`, `DefaultId = null`, rows ≥ 1, promoted row fails INI synchronisation

### Testing Strategy

> Set up at least one installation row whose path has a valid zoo.exe but whose zoo.ini cannot be synchronised — for example, a malformed file the parser rejects, a
> permissions-locked file, or a file held open exclusively by another process. Clear the stored `DefaultInstallationId`. Ensure the startup preference is `DefaultInstallation`.
> Launch the app.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Stale `DefaultId`: `pref = DefaultInstallation`, `DefaultId` is set, but `GetByIdAsync` returns null

### Testing Strategy

> Make the stored `DefaultInstallationId` point at a Guid that is not present in the installation registry. Either delete the row that originally owned it (leaving the settings
> id behind) or just write an arbitrary fresh Guid into the settings row. Ensure the startup preference is `DefaultInstallation`. Launch the app.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Happy Path: `pref = DefaultInstallation`, `DefaultId` is set, row verifies and synchronises

### Testing Strategy

> Set up at least one installation row pointing to a valid on-disk installation (zoo.exe present, zoo.ini present, and parseable), and set the stored `DefaultInstallationId` to
> that row's id. Ensure the startup preference is `DefaultInstallation`. Launch the app and confirm the right installation surfaces on `ReadyToPlay`.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Cannot Play (`HasExe = false`): `pref = DefaultInstallation`, `DefaultId` is set, row fails verification

### Testing Strategy

> Set up an installation row and set the stored `DefaultInstallationId` to match. Then remove or rename zoo.exe at the row's path so verification fails. Ensure the startup
> preference is `DefaultInstallation`. Launch the app.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Cannot Play (sync failure): `pref = DefaultInstallation`, `DefaultId` is set, row fails INI synchronisation

### Testing Strategy

> Set up an installation row whose path has a valid zoo.exe but a zoo.ini the synchroniser cannot process (malformed, locked, or otherwise unreadable). Set the stored
> `DefaultInstallationId` to match. Ensure the startup preference is `DefaultInstallation`. Launch the app.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Last Played → Ready: `pref = LastPlayedInstallation`, a candidate exists, verifies, and synchronises

### Testing Strategy

> Set up at least two installation rows with distinct `LastPlayedUtc` values, both pointing at valid on-disk installations. Set the startup preference to `LastPlayedInstallation`.
> Launch the app and confirm the row with the newer `LastPlayedUtc` is the one that surfaces — not the other valid row.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Last Played → Cannot Play (`HasExe = false`): `pref = LastPlayedInstallation`, a candidate exists, fails verification

### Testing Strategy

> Same setup as the previous section — multiple rows with distinct `LastPlayedUtc` values, `LastPlayedInstallation` preference — but the row with the newer `LastPlayedUtc` has no
> zoo.exe at its path. Launch the app and confirm `CannotPlay` surfaces for that row; the handler should not silently fall back to another row.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Last Played → Cannot Play (sync failure): `pref = LastPlayedInstallation`, a candidate exists, fails INI synchronisation

### Testing Strategy

> Same setup as the previous two sections, but the row that would be selected by `LastPlayedUtc` has a valid zoo.exe and a zoo.ini the synchroniser cannot process. Launch the
> app.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Last Played Fallback: `pref = LastPlayedInstallation`, no row has `LastPlayedUtc`, falls back to `DefaultInstallation` resolution

### Testing Strategy

> Set the startup preference to `LastPlayedInstallation` and ensure every installation row has a null `LastPlayedUtc` (i.e., the game has never been launched through the launcher
> for any of these rows). Then arrange the rest of state to exercise whichever `DefaultInstallation` sub-path you want to see kick in after the fallback — for example, no rows
> plus no locator candidate to land on `NoGameInstallationFound`, or one valid row with a null stored `DefaultInstallationId` to drive the promotion-then-`ReadyToPlay` flow.
> Launch the app.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Last Opened → Ready: `pref = LastOpenedInstallation`, a candidate exists, verifies, and synchronises

### Testing Strategy

> Set up at least two installation rows with distinct `LastOpenedUtc` values, both pointing at valid on-disk installations. Set the startup preference to `LastOpenedInstallation`.
> Launch the app and confirm the row with the newer `LastOpenedUtc` is the one that surfaces.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Last Opened → Cannot Play (`HasExe = false`): `pref = LastOpenedInstallation`, a candidate exists, fails verification

### Testing Strategy

> Same setup as the previous section, but the row with the newer `LastOpenedUtc` has no zoo.exe at its path. Launch the app and confirm `CannotPlay` surfaces for that row; the
> handler should not silently fall back to another row.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Last Opened → Cannot Play (sync failure): `pref = LastOpenedInstallation`, a candidate exists, fails INI synchronisation

### Testing Strategy

> Same setup as the previous two sections, but the row that would be selected by `LastOpenedUtc` has a valid zoo.exe and a zoo.ini the synchroniser cannot process. Launch the
> app.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Last Opened Fallback: `pref = LastOpenedInstallation`, no row has `LastOpenedUtc`, falls back to `DefaultInstallation` resolution

### Testing Strategy

> Set the startup preference to `LastOpenedInstallation` and ensure every installation row has a null `LastOpenedUtc` (i.e., the launcher has never opened any of these rows on
> the handler's behalf). Then arrange the rest of state to exercise whichever `DefaultInstallation` sub-path you want to see kick in after the fallback — same options as the
> `LastPlayedInstallation` fallback case. Launch the app.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Verification Drift Persisted (progressive): `HasExe` and/or `HasIni` change during `Verify`, handler writes the new flags to the row before continuing

### Testing Strategy

> Set up an installation row whose stored `HasExe` and `HasIni` reflect a previous state — for example, both flags true. Then change reality before launching so the values
> diverge: remove zoo.exe so the actual `HasExe` should be false, or restore a previously missing zoo.ini so the actual `HasIni` should now be true. Note the stored flags before
> boot for comparison. Run any scenario that drives the handler through a `Verify(row)` for this installation (the happy path or a `LastPlayed`/`LastOpened` candidate all work).
> After boot completes, inspect the row again to confirm the stored `HasExe` / `HasIni` (and `ModifiedUtc`) now match disk reality. The downstream UI outcome — `CannotPlay` or
> `ReadyToPlay` — depends on the new flag values; both are acceptable here, the point is that the row was updated.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## `LastOpenedUtc` Stamp (progressive): handler stamps `LastOpenedUtc` on the row before returning `ReadyToPlay`

### Testing Strategy

> Note the row's current `LastOpenedUtc` before boot, or null it out beforehand so the change is unmistakable. Run any scenario that lands on `ReadyToPlay` (the happy path is
> easiest). After boot completes, inspect the row and confirm `LastOpenedUtc` has been updated to a value close to "now" (within whatever drift the handler's `TimeProvider`
> allows).

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory

## Unexpected Handler Failure: `BootCommand` returns a non-success `ErrorOr`, `MainWindowViewModel` falls back to `NoGameInstallationFoundViewModel(null)`

### Testing Strategy

> Force `BootCommand` to return a non-success `ErrorOr` so the Desktop-layer fallback engages. Break the handler's environment in a way that surfaces as an error rather than a
> crash. For example, corrupt the launcher database file, hold an exclusive lock on it, point the storage path at a directory that cannot be created, or substitute an
> infrastructure dependency that returns a typed error on its first call. Launch the app and confirm `NoGameInstallationFound` renders with no candidate path and the app stays
> responsive rather than crashing.

### Expected Outcome

> Explain what the expected outcome for this stage in the process should be.

### Actual Outcome

> Explain what the actual outcome was, how it aligned and/or differed from the expected. If any changes were made, briefly highlight them here (simply to avoid writing multiple
> "Actual Outcome" sections) and then go into more detail in the next section.

### Changes

> Walk through any changes made during testing to address any gaps between the expected and actual outcomes or improvements you made.

### UI/UX

#### Hi-Fi Mockup

> Add a screenshot of the particular view in question from the hi-fi mockup in Claude Design.

#### Actual Implementation

> Add a screenshot of the actual implementation.

#### Alignment

> Does the implemented UI/UX align with that of the mockup? If not, explain why.

### Shortcomings

> Explain any shortcomings not yet implemented, what information/steps are needed to implement them, etc.

### Notes/Thoughts

> Self-explanatory


---

# Notes & Future Tasks/Goals

1. Abstract shared string literals used in the UI into a single constants/helper class so that everything can be easily updated in one single place and also eliminate duplication.
   For example, the version number in the title bar and the two status bar versions are hardcoded and duplicated when it could be read from a single source of truth. Similar thing
   for the status messages; they're not duplicated, but you as of right now, every view model is responsible for defining the appropriate strings. If all the strings were in one
   class, the view models could still be responsible for setting/passing along the info. However, the source text would be in one, central location, meaning that the devs don't
   have to hunt down string literals just to update or add.
2. The "Auto-locate trail" group box on the "No Installation Found" screen needs to be dynamically populated. It's currently hardcoded.
3. The "Name" field in the "Add Installation" dialogue needs to be set as well as the default checkbox if application.
4. The auto-locate pipeline needs to actually return a potential candidate if found. It's currently being hardcoded in the view model.
5. Ensure spacing, padding, and margins are consistent across all views.
6. Create reusable controls for commonly configured stack and dock panels.
