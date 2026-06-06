# Boot + Basic Installation Management

The following execution paths are being manually tested end-to-end, and the necessary UI is being developed to support them. The UI should resemble the hi-fi mockup made in Claude
Design as closely as possible.

> For Claude: Using the same structure as the following sections, update this file with a stubbed section for every possible execution path in the startup process. Only stub out
> the sections — I will go through and modify them as needed. Each execution path should be unique and represent a point in the process that is either terminal, transitional, or
> progressive. Leave the Notes & Future Tasks/Goals section alone, it's there for me and not needed for this task.

## Initial Render: `LookingForZooTycoon` shown while `BootCommand` is in flight

### Testing Strategy

> Set up any scenario that triggers a real boot (the happy path is fine), then artificially extend how long `BootCommand` takes to return so the transitional view stays on screen
> long enough to inspect. The slowdown can come from anywhere in the pipeline — a pause inside the handler, a probe that takes its time, or a debugger break between
> `MainWindowViewModel.BootAsync` assigning the `LookingForZooTycoonViewModel` to `ActiveContent` and the mediator dispatch returning. Confirm the looking view renders immediately
> on launch and remains visible until the terminal state replaces it.

### Expected Outcome

> Explain what the expected outcome for this stage in the process was.

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

## Open Game Installation: `pref = NoInstallation`

### Testing Strategy

> Set the launcher's stored startup preference to `NoInstallation`, then launch the app. The installation registry can be either empty or populated — neither should matter — but
> it's worth running the test in both states to confirm the branch short-circuits regardless.

### Expected Outcome

> Explain what the expected outcome for this stage in the process was.

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

## Auto Locate: `pref = DefaultInstallation`, and `DefaultId = null`, and no rows exist

### Testing Strategy

> Reset the installation registry so that no rows exist and the stored `DefaultInstallationId` is null. Ensure the startup preference is `DefaultInstallation`. Provide a real Zoo
> Tycoon installation on disk in a location one of the locator's probes can discover (registry entry, Steam library, GOG library, or wherever the probes are pointed). Launch the
> app and confirm the located path is surfaced on the resulting screen.

### Expected Outcome

> Explain what the expected outcome for this stage in the process was.

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
> of the locator's probe locations. Either remove them from disk or temporarily point the probes at empty directories. Launch the app and confirm no candidate path is shown on the
> resulting screen.

### Expected Outcome

> Explain what the expected outcome for this stage in the process was.

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

> Explain what the expected outcome for this stage in the process was.

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

> Explain what the expected outcome for this stage in the process was.

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

> Explain what the expected outcome for this stage in the process was.

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

> Explain what the expected outcome for this stage in the process was.

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

> Set up at least one installation row pointing to a valid on-disk installation (zoo.exe present, zoo.ini present and parseable), and set the stored `DefaultInstallationId` to
> that row's id. Ensure the startup preference is `DefaultInstallation`. Launch the app and confirm the right installation surfaces on `ReadyToPlay`.

### Expected Outcome

> Explain what the expected outcome for this stage in the process was.

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

> Explain what the expected outcome for this stage in the process was.

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

> Explain what the expected outcome for this stage in the process was.

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

## Last Played → Ready: `pref = LastPlayedInstallation`, candidate exists, verifies and synchronises

### Testing Strategy

> Set up at least two installation rows with distinct `LastPlayedUtc` values, both pointing at valid on-disk installations. Set the startup preference to `LastPlayedInstallation`.
> Launch the app and confirm the row with the newer `LastPlayedUtc` is the one that surfaces — not the other valid row.

### Expected Outcome

> Explain what the expected outcome for this stage in the process was.

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

## Last Played → Cannot Play (`HasExe = false`): `pref = LastPlayedInstallation`, candidate exists, fails verification

### Testing Strategy

> Same setup as the previous section — multiple rows with distinct `LastPlayedUtc` values, `LastPlayedInstallation` preference — but the row with the newer `LastPlayedUtc` has no
> zoo.exe at its path. Launch the app and confirm `CannotPlay` surfaces for that row; the handler should not silently fall back to another row.

### Expected Outcome

> Explain what the expected outcome for this stage in the process was.

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

## Last Played → Cannot Play (sync failure): `pref = LastPlayedInstallation`, candidate exists, fails INI synchronisation

### Testing Strategy

> Same setup as the previous two sections, but the row that would be selected by `LastPlayedUtc` has a valid zoo.exe and a zoo.ini the synchroniser cannot process. Launch the
> app.

### Expected Outcome

> Explain what the expected outcome for this stage in the process was.

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

> Explain what the expected outcome for this stage in the process was.

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

## Last Opened → Ready: `pref = LastOpenedInstallation`, candidate exists, verifies and synchronises

### Testing Strategy

> Set up at least two installation rows with distinct `LastOpenedUtc` values, both pointing at valid on-disk installations. Set the startup preference to `LastOpenedInstallation`.
> Launch the app and confirm the row with the newer `LastOpenedUtc` is the one that surfaces.

### Expected Outcome

> Explain what the expected outcome for this stage in the process was.

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

## Last Opened → Cannot Play (`HasExe = false`): `pref = LastOpenedInstallation`, candidate exists, fails verification

### Testing Strategy

> Same setup as the previous section, but the row with the newer `LastOpenedUtc` has no zoo.exe at its path. Launch the app and confirm `CannotPlay` surfaces for that row; the
> handler should not silently fall back to another row.

### Expected Outcome

> Explain what the expected outcome for this stage in the process was.

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

## Last Opened → Cannot Play (sync failure): `pref = LastOpenedInstallation`, candidate exists, fails INI synchronisation

### Testing Strategy

> Same setup as the previous two sections, but the row that would be selected by `LastOpenedUtc` has a valid zoo.exe and a zoo.ini the synchroniser cannot process. Launch the
> app.

### Expected Outcome

> Explain what the expected outcome for this stage in the process was.

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

> Explain what the expected outcome for this stage in the process was.

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
> diverge: remove zoo.exe so the actual `HasExe` should be false, or restore a previously-missing zoo.ini so the actual `HasIni` should now be true. Note the stored flags before
> boot for comparison. Run any scenario that drives the handler through a `Verify(row)` for this installation (the happy path or a `LastPlayed`/`LastOpened` candidate all work).
> After boot completes, inspect the row again to confirm the stored `HasExe` / `HasIni` (and `ModifiedUtc`) now match disk reality. The downstream UI outcome — `CannotPlay` or
> `ReadyToPlay` — depends on the new flag values; both are acceptable here, the point is that the row was updated.

### Expected Outcome

> Explain what the expected outcome for this stage in the process was.

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

> Explain what the expected outcome for this stage in the process was.

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
> crash — for example, corrupt the launcher database file, hold an exclusive lock on it, point the storage path at a directory that cannot be created, or substitute an
> infrastructure dependency that returns a typed error on its first call. Launch the app and confirm `NoGameInstallationFound` renders with no candidate path and the app stays
> responsive rather than crashing.

### Expected Outcome

> Explain what the expected outcome for this stage in the process was.

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

1. Abstract shared string literals used in the UI into a single constants/helper class so that everything can be easily updated in one single place and also eliminate duplication
   (e.g. the version number in the title bar and the two status bar versions are hardcoded and duplicated when it could be read from a single source of truth. Similar thing for the
   status messages; they're not duplicated, but you as of right now, every view model is responsible for defining the appropriate strings. If all the strings were in one class, the
   view models could still be responsible for setting/passing along the info. However, the source text would be in one, central location, meaning that the devs don't have to hunt
   down string literals just to update or add).
2. The "Auto-locate trail" group box on the "No Installation Found" screen needs to be dynamically populated. It's currently hardcoded.
3. The "Name" field in the "Add Installation" dialogue needs to be set as well as the default checkbox if application.
4. The auto-locate pipeline needs to actually return a potential candidate if found. It's currently being hardcoded in the view model.
5. Ensure spacing, padding, and margins are consistent across all views.
6. Create reusable controls for commonly configured stack and dock panels.
