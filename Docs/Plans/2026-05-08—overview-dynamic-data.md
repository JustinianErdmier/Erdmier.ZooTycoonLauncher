# Home → Overview: Wire Up Dynamic Data — Implementation Plan

> **For Claude:** This is a stub plan capturing follow-up notes from the Home-tab scaffolding work on 2026-05-08. The Overview group box was populated with a real `StatusMessage`
> binding plus three dummy fields (desktop resolution, available screen modes, detected Zoo Tycoon resolutions). This plan flags the work needed to replace those dummies with
> live data and is **not yet fleshed out into per-step tasks** — promote sections to tasks (and add a sibling `-design.md`) using `superpowers:writing-plans` once the team is
> ready to execute.

**Goal:** Replace the placeholder values in the Home tab's Overview group box with live system + game data. Maintain the project's testability conventions (services behind
interfaces, no direct Win32 calls in view models, designer-friendly null implementations).

**Reference:** [`Source/Launcher/Views/MainWindow.axaml`](../../Source/Launcher/Views/MainWindow.axaml) — current dummy bindings live in the `Overview` group box.

---

## Field 1: Status

**Current binding:** `MainWindowViewModel.StatusMessage` (shared with the bottom status bar).

**Note:** Once we have richer dynamic state to display, consider splitting Overview's status from the bottom-bar status. The bottom bar is a good fit for *transient* progress
(`"Locating Zoo Tycoon…"`, `"Verifying selected directory…"`); the Overview row is a good fit for *steady-state* posture (`"Ready."`, `"Game not found."`,
`"INI parse failed."`). Sharing the property is fine for now — flag this when adding the next dynamic field, and consider an `OverviewStatus` (or projection over
`StartupResult.Status`) on `MainWindowViewModel` so the two surfaces can diverge cleanly.

---

## Field 2: Desktop resolution

**Current dummy:** `1920 × 1080 @ 60 Hz`

**Data sources:**

- **Width / height / scaling:** `TopLevel.GetTopLevel(this)?.Screens.Primary` exposes `Bounds`, `WorkingArea`, and `Scaling`. Reachable from the view model only via an
  abstraction — Avalonia's `Screens` API is `TopLevel`-bound, so wrap it.
- **Refresh rate:** Avalonia does *not* expose this. Need Win32 `EnumDisplaySettings` with `iModeNum = ENUM_CURRENT_SETTINGS` (-1) on the active display device. Returns a
  `DEVMODE` whose `dmDisplayFrequency` is the current refresh rate in Hz.

**Suggested abstraction:** `IDisplayInfoService` exposing `DisplayMode GetCurrent()` and `IReadOnlyList<DisplayMode> EnumerateAll()` (covers Field 3 too — see below). Mirror the
`IRegistryReader` pattern: a Win32 implementation registered on Windows, plus a `NullDisplayInfoService` for the designer ctor.

---

## Field 3: Available screen modes

**Current dummy:** count placeholder.

**Data source:** `EnumDisplaySettings` in a loop — increment `iModeNum` from 0 until it returns `FALSE`. Each `DEVMODE` yields width × height × bit-depth × refresh combinations.
Typically yields a few hundred raw modes; deduplicate by (width, height, refresh) for display unless the user is explicitly inspecting bit-depth.

**Decisions to make before implementing:**

- Show the *count* (current dummy) or the full list? Count is compact for Overview; the full list belongs on a future "Display" detail tab or a popover.
- Filter to "useful" modes (≥640×480, ≥60 Hz) before counting, or expose the raw count? Recommend filtered, with the raw set still available via the service for future UI.

Reuse the `IDisplayInfoService` from Field 2 — both fields are the same Win32 call, just one mode vs. all.

---

## Field 4: Detected Zoo Tycoon resolutions

**Current dummy:** count placeholder.

**Definition:** The intersection of (a) the OS's enumerated display modes and (b) the set of resolutions the game's renderer will actually accept in fullscreen. Zoo Tycoon's
DirectX 8 path historically rejects exotic depth/refresh combinations and refuses fullscreen modes outside a fixed list (see SDD §X — TODO: cite once written).

**Suggested abstraction:** `IZooDisplayResolver` taking `IDisplayInfoService` as input and returning `IReadOnlyList<DisplayMode>`. Two reasons to keep it separate from
`IDisplayInfoService`:

1. The intersection rules are game-specific knowledge that doesn't belong in a generic display-info service.
2. It's pure logic over an injected dependency, so it's trivially unit-testable against a canned `IDisplayInfoService` fake — no Win32 in the test path.

**Open questions:**

- Where does the "fixed list" of accepted modes come from? Hard-coded constant, embedded resource, or scraped from `zoo.ini` / registry? Likely hard-coded, sourced from
  community documentation. File a research task before implementation.
- Does the resolver need to consider the current `[Screen]` section of `zoo.ini` (e.g. windowed vs. fullscreen toggles change which modes apply)? Probably yes for fullscreen
  modes only.

---

## Cross-cutting

- **DI registration:** Add `IDisplayInfoService` + `IZooDisplayResolver` registrations alongside the existing services in `App.OnFrameworkInitializationCompleted`. Mirror the
  `IRegistryReader` pattern: real impl on Windows, null fallback elsewhere (the launcher is Windows-only per the SDD, but the null impl keeps the designer ctor working).
- **ViewModel surface:** Add `DesktopResolutionDisplay`, `ScreenModeCount` (or list), and `ZooResolutionCount` (or list) to `MainWindowViewModel`. Populate them from
  `InitializeAsync` after the existing startup flow completes — failures should degrade gracefully (`"Unknown"` / `0`) rather than throwing.
- **Tests:** Out of scope for the current milestone (per the existing `2026-04-29—ini-parser-and-startup.md` plan), but the abstractions above are designed so the real
  implementations stay thin and the resolver logic is fully testable once the test project lands.
- **Threading:** `EnumDisplaySettings` is fast (microseconds) and safe to call on the UI thread, but if we add bit-depth/refresh filtering or scrape the full mode list, push it
  to a background `Task.Run` to keep `InitializeAsync` snappy.
