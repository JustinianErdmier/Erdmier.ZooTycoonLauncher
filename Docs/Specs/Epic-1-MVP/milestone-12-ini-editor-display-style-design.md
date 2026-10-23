# INI Editor Display Style — Design Spec

> - **Milestone:** 12 (Epic 1 — MVP)
> - **Date:** 2026-10-22
> - **Author:** Justinian
> - **Status:** Approved for implementation (decisions 1–12; calls C1–C16)
> - **Authoritative spec:** [1-zoo-tycoon-launcher-master-design.md](../1-zoo-tycoon-launcher-master-design.md) §5.1, §5.2, §6.2, §9.3, §9.7, §9.8, §13.2
> - **Design reference:** the brainstorming mock-ups (2026-10-22): the side-by-side `[user]` / Display comparison and the eight-page draft in §6.1 below;
>   `Docs/UserInterfaceDesign/conventions.md` §2–§5
> - **Branch:** `feat/milestone-12-ini-editor-display-style`

---

## 1. Goal

Let a player choose, in the Options dialogue, how the INI Config editor presents `zoo.ini`:

- **Friendly style** (the default) — a settings screen for someone who has never heard of `zoo.ini`. Settings are grouped into pages by what they are for, not by the INI section
  they live in; every row has a plain label that says what the control does; values read the way a person expects (ticking *Enable menu music* turns the music on; a higher volume
  is louder).
- **INI style** — close to today's editor, just without the inverted check boxes: the file's seven sections and key names, for someone who knows `zoo.ini` and wants to see it as it
  is. Its controls follow the file literally.

The choice changes only how settings are **shown**. Reading, saving, snapshots, Restore and Reset always work on the file's real sections and keys.

### 1.1 Why

The editor today is a near faithful view of `zoo.ini`: section names like `[user]` and `[UI]`, key names like `noMenuMusic` and `menuMusicAttenuation`, a "Display and performance"
section that holds screen size and game speed side by side, and related settings scattered across sections (the 8-bit-audio switch sits with graphics quality; the guest limit has a
section of its own). That serves someone who already knows the file and puzzles everyone else.

## 2. Decisions (maintainer)

| #  | Question                        | Decision                                                                                                                                                                                                                         |
|----|---------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | Where the choice lives          | The **Options** dialogue: *Friendly style* or *INI style*.                                                                                                                                                                       |
| 2  | Default                         | **Friendly style**.                                                                                                                                                                                                              |
| 3  | Friendly style grouping         | Pages by **purpose**, gathering keys from any INI section; group boxes inside a page.                                                                                                                                            |
| 4  | Friendly style page header      | A title-cased title (no brackets, no "section of zoo.ini") and, under it, a one-line description of what the page is for.                                                                                                        |
| 5  | Friendly style labels           | Plain labels that say what the control does, not key names.                                                                                                                                                                      |
| 6  | Inverted keys                   | **Friendly style:** shown by meaning (*Enable menu music*, ticked = music plays). **INI style:** shown literally (`noMenuMusic`, ticked = `1` = no music).                                                                       |
| 7  | Attenuation keys                | **Friendly style:** inverted like the other inverted keys and labelled as a volume (*Menu music volume*, *Game sound volume*). **INI style:** raw.                                                                               |
| 8  | Hints and help                  | Both styles keep a hint line under every label (range, unit or default) and the help as a tooltip and in the editor footer on hover or keyboard focus. Each style's hint and help describe the value **as that style shows it**. |
| 9  | Language codes (friendly style) | The LANGID / SUBLANGID boxes are hidden; the codes the file holds are shown as an info line under the language drop-down.                                                                                                        |
| 10 | Restore Previous INI dialogue   | **Follows the chosen style**: friendly key names and friendly values in Friendly style; today's `[section]/key` and raw-faithful values in INI style.                                                                            |
| 11 | Names                           | The page, group and label names in §6.1 are a draft to fine-tune as the work goes.                                                                                                                                               |
| 12 | Volume controls                 | **Friendly style:** sliders (0–100 %), not number boxes. **INI style:** raw number boxes, as today.                                                                                                                              |

## 3. Non-goals and anti-goals

- **No change to what is read or written.** File I/O, the snapshot store, the diff that decides what Restore writes, Reset and Restore defaults are untouched;
  only presentation changes. A switch of style must never write `zoo.ini` or change a pending edit's value.
- **No user-arranged layout.** The two layouts are fixed in code; the player picks one, not a custom arrangement.
- **No per-installation choice.** One setting for the launcher (C1).
- Not the `[scenario]` section (still waiting on the Phase 0 spike, DW-04) and not the screen-modes resolution drop-down (§7.9). When either lands it is added to
  both layouts.
- **Anti-goal:** a friendly page that hides a setting the INI style shows, or shows one twice. Every user-setting key appears exactly once in each layout (§9).
- **Anti-goal:** the two styles disagreeing about a value — a friendly row and an INI row for the same key always round-trip to the same file value.

## 4. The setting

- A new SmartEnum **`IniEditorStyle`** (Domain, beside `LauncherStartupPreference`): `Friendly` and `Ini`, with the display names *Friendly style* and *INI style*;
  the values are not suffixed with the type name, as `LauncherStartupPreference`'s are not.
- `LauncherSettings` gains **`IniEditorStyle`**, stored as a new `LauncherSettings.IniEditorStyle` column (TEXT, SmartEnum name, not null, default `'Friendly'`), added by a
  `Launcher.db` migration. Existing installs therefore move to the friendly style on upgrade (decision 2).
- `SaveLauncherOptionsCommand` carries the style alongside the two existing options, saved in the same transaction.
- **Options dialogue** (SDD §9.8) gains a third group box, **INI editor**, between *Startup* and *Game launch*: a muted line *"Show settings in the INI Config tab:"* then two radio
  buttons, **`Friendly style`** and **`INI style`** (C2). Underneath the radio buttons, a muted description of the **selected** option, which changes as the
  selection changes (C15):
  - *Friendly style* — "Settings are grouped by what they do, with plain names and volumes that go up as they get louder."
  - *INI style* — "Settings appear exactly as zoo.ini stores them: its sections, key names and raw values."
- **It applies when `OK` saves** (C3): an open INI Config tab re-renders in the new style at once. This is the first Options value that applies live; the other two keep their
  next-start / next-launch behaviour.

## 5. INI style

Exactly today's editor (SDD §9.3), with one change (decision 6): a key the catalogue marks *inverted* is shown **literally**.

- `[UI]/noMenuMusic` — the only inverted key today — becomes a plain check box labelled `noMenuMusic` (the key, as every INI-style row), ticked when the file holds `1`. Its caption
  changes from *Play menu music* to *No menu music* (C12), its hint becomes *"1 = no menu music"* and its help describes the key as stored ("Silence the main-menu music. 1 turns it
  off, 0 plays it."). Every other INI-style check box keeps today's caption.
- The attenuation and movie-volume keys stay raw, with their hints (`0–10000`, `-10000 silent → 0 full`), as today.
- Everything else — the seven sections and their order, the key-name labels in the monospace face, the `[UI]` sub-headers, the cash formatting, the language picker with its
  read-only code rows — is unchanged.

## 6. Friendly style

### 6.1 Pages and groups

Eight pages, in this order. Each page shows its title, the one-line description, then its group boxes. The key column is for the builder and is not shown.

| Page                | Description                                             | Group                | Label                         | Key                                    |
|---------------------|---------------------------------------------------------|----------------------|-------------------------------|----------------------------------------|
| **Display**         | How the game draws its window and how fast it runs.     | Screen               | Screen mode                   | `[user] fullscreen`                    |
|                     |                                                         |                      | Width (pixels)                | `[user] screenwidth`                   |
|                     |                                                         |                      | Height (pixels)               | `[user] screenheight`                  |
|                     |                                                         | Performance          | Game speed                    | `[user] UpdateRate`                    |
|                     |                                                         |                      | Frame rate limit              | `[user] DrawRate`                      |
| **Graphics**        | Picture quality, and when the game trades it for speed. | Quality              | Quality preset                | `[advanced] level`                     |
|                     |                                                         |                      | Use reduced-detail animations | `[advanced] loadHalfAnims`             |
|                     |                                                         | Lower quality while… | Dragging objects              | `[advanced] drag`                      |
|                     |                                                         |                      | Clicking                      | `[advanced] click`                     |
|                     |                                                         |                      | Playing normally              | `[advanced] normal`                    |
| **Sound**           | Music, sound effects and the intro movies.              | Menu music           | Enable menu music             | `[UI] noMenuMusic` (inverted)          |
|                     |                                                         |                      | Music file                    | `[UI] menuMusic`                       |
|                     |                                                         |                      | Menu music volume             | `[UI] menuMusicAttenuation` (inverted) |
|                     |                                                         | Game sound           | Game sound volume             | `[UI] userAttenuation` (inverted)      |
|                     |                                                         |                      | Force 8-bit audio             | `[advanced] use8BitSound`              |
|                     |                                                         | Intro movies         | Play the first movie          | `[UI] playMovie`                       |
|                     |                                                         |                      | First movie volume            | `[UI] movievolume1` (offset)           |
|                     |                                                         |                      | Play the second movie         | `[UI] playSecondMovie`                 |
|                     |                                                         |                      | Second movie volume           | `[UI] movievolume2` (offset)           |
| **New Zoos**        | The money, size and guest limit a new zoo starts with.  | Money                | Starting cash                 | `[UI] MSStartingCash`                  |
|                     |                                                         |                      | Cash step                     | `[UI] MSCashIncrement`                 |
|                     |                                                         |                      | Minimum cash                  | `[UI] MSMinCash`                       |
|                     |                                                         |                      | Maximum cash                  | `[UI] MSMaxCash`                       |
|                     |                                                         | Map size             | Width (tiles)                 | `[Map] mapX`                           |
|                     |                                                         |                      | Height (tiles)                | `[Map] mapY`                           |
|                     |                                                         | Guests               | Maximum guests                | `[ai] maxGuests`                       |
| **Controls**        | Scrolling the view and the mouse cursor.                | Mouse-edge scrolling | Start distance from edge      | `[UI] mouseScrollThreshold`            |
|                     |                                                         |                      | Delay before scrolling        | `[UI] mouseScrollDelay`                |
|                     |                                                         |                      | Horizontal speed              | `[UI] mouseScrollX`                    |
|                     |                                                         |                      | Vertical speed                | `[UI] mouseScrollY`                    |
|                     |                                                         | Keyboard scrolling   | Horizontal speed              | `[UI] keyScrollX`                      |
|                     |                                                         |                      | Vertical speed                | `[UI] keyScrollY`                      |
|                     |                                                         | Cursor               | Use monochrome cursors        | `[UI] useAlternateCursors`             |
| **Help & Messages** | In-game tooltips, help text and notifications.          | Tooltips and help    | Help level                    | `[UI] helpType`                        |
|                     |                                                         |                      | Tooltip delay                 | `[UI] tooltipDelay`                    |
|                     |                                                         |                      | Tooltip duration              | `[UI] tooltipDuration`                 |
|                     |                                                         | Messages             | Show in-game messages         | `[UI] MessageDisplay`                  |
|                     |                                                         |                      | Time between repeats          | `[UI] minimumMessageInterval`          |
| **Language**        | The language the game's text appears in.                | Language             | Game language                 | `[language] lang` + `sublang`          |
| **Troubleshooting** | Diagnostics for when the game misbehaves.               | Frame-rate counter   | Show the frame-rate counter   | `[debug] drawfps`                      |
|                     |                                                         |                      | Horizontal position           | `[debug] drawfpsx`                     |
|                     |                                                         |                      | Vertical position             | `[debug] drawfpsy`                     |
|                     |                                                         | Logging              | Log detail                    | `[debug] logCutoff`                    |
|                     |                                                         |                      | Write zoo.log                 | `[debug] sendLogfile`                  |
|                     |                                                         |                      | Send the log to a debugger    | `[debug] sendDebugger`                 |

The `[ai]` and `[Map]` section footnotes (SDD §9.3) move with their keys: the guest-limit note under the *Guests* group, the map-memory note under *Map size*.

### 6.2 Rows

- The **label** is plain text in the system face, not the monospace key face. Check boxes carry their label as the check box's own caption (Win9x convention); every other control
  has its label on the left (C4).
- A muted **hint line** under every label (decision 8), worded for the value the friendly control shows.
- The **help** (tooltip and footer) is written for the friendly value: it explains what the setting does, never which key it writes.
- Drop-downs keep their existing labels (*Fullscreen* / *Windowed*, the quality presets, the help levels).

### 6.3 Values shown differently from the file

Four keys show a converted value. The conversion applies to what the control shows and accepts; the file keeps the raw value, and saving writes it back exactly (C5).

| Key                                 | File                           | Friendly control                                            | Conversion                     |
|-------------------------------------|--------------------------------|-------------------------------------------------------------|--------------------------------|
| `[UI] noMenuMusic`                  | `1` = no music                 | *Enable menu music*, ticked = music plays                   | invert the check box           |
| `[UI] menuMusicAttenuation`         | `0`–`10000`, higher is quieter | *Menu music volume* `0`–`10000`, higher is louder           | `volume = 10000 − attenuation` |
| `[UI] userAttenuation`              | `0`–`10000`, higher is quieter | *Game sound volume* `0`–`10000`, higher is louder           | `volume = 10000 − attenuation` |
| `[UI] movievolume1`, `movievolume2` | `-10000` silent → `0` full     | *First / Second movie volume* `0`–`10000`, higher is louder | `volume = raw + 10000`         |

The two movie volumes are the author's call (C6): decision 7 names the attenuation keys, and moving the movie volumes onto the same scale makes all four volume controls behave
alike.

### 6.4 Volume sliders

The four volume rows (§6.3) are **sliders**, not number boxes (decision 12), drawn as Win9x trackbars (Avalonia's `Slider` under Classic.Avalonia):

- **Scale.** The slider runs 0–100 %: *percent = volume ÷ 100*, so each 1 % step is 100 raw units and the file only ever receives multiples of 100 from the
  slider. A file value that is not a multiple of 100 shows at the nearest step and is written back unchanged unless the slider is moved (C16).
- **Readout.** The percentage beside the slider (*75 %*); the hint line reads *"Silent – Full"*.
- **Keyboard.** The arrow keys move 1 %; Page Up / Page Down move 10 %; a tick mark every 10 %.
- **Focus.** A slider follows the milestone 11.1 focus rules: one the pointer dragged gives focus back when the drag ends, like a spinner step; one reached with
  Tab keeps focus.
- **INI style** keeps its raw number boxes: it shows what the file holds.

### 6.5 Language

The *Language* page shows only the **Game language** drop-down. Under it, a muted info line shows the codes — *"Windows language codes: LANGID 9 · SUBLANGID 1"* — so a file with
the wrong codes is visible without switching style. A pair the drop-down does not list leaves it blank, as today, and the info line shows the pair (decision 9). The line shows the
**current** values, as INI style's read-only code rows do: what the file holds when nothing is pending, and the picker's codes once a language is picked (C13).

### 6.6 Maintainer follow-ups (added 2026-10-23, after the first review)

Found by the maintainer using the reviewed build. They extend this milestone.

- **Cash help (both styles).** The four `[UI]` cash keys configure the *new zoo* screen, where the player picks a starting cash before the zoo starts. Their help,
  in both layouts, says so — one meaning in both styles (supersedes ruling R5's wording):
  - `MSStartingCash` — the starting cash a new zoo offers by default; the player can change it before the zoo starts.
  - `MSCashIncrement` — how much the starting cash goes up or down per step while the player chooses it (default `$75,000`, step `$25,000`: one step up gives
    `$100,000`).
  - `MSMinCash` / `MSMaxCash` — the least / most starting cash the player can choose for a new zoo. Neither limits cash during play (it can go negative).
- **Language codes line.** The info line under the language drop-down reads *Windows language codes:* on its first line and *LANGID 9 · SUBLANGID 2* on its
  own second line, so the pair never splits across a wrap.
- **View menu.** A **View** menu between *File* and *Help* holds **Config layout ▸ Friendly style / INI style**. The two items are a mutually exclusive pair with
  the Win9x radio-check bullet (•) on the active style. Choosing one saves the style with the other launcher options (as Options' OK does) and applies it at
  once; the bullet follows the style however it changes (View menu or Options). A failed save shows *Cannot Save Options* with the error and changes nothing.
  Enabled in every state after boot, like *Options…*.

The SDD changes these bring (§9.3.3, §9.7's key-column font, §9.10's menu table, §11.4's test inventory) land in this milestone too: the SDD is never left
behind the code.

## 7. Shared behaviour

Both styles behave identically apart from §5 and §6:

- **Footer and help.** Hover and keyboard-focus help, the footer's dirty / saved indicator, and the focus rules of milestone 11.1 §6a.1 (SDD §9.3.2).
- **Buttons.** `Restore previous…`, `Restore defaults`, `Save`, `Reset` and their enablement are unchanged; they act on the same pending edits.
- **Read-only strip** and the INI Config sub-states (Loading, No INI, could not be read) are unchanged.
- **Switching style.** Pending edits are held by INI key, so they survive a style switch: an edited row shows its edited value in the new style and the editor stays dirty. The
  selected section or page resets to the first one, because the two lists do not correspond (C7).

## 8. Restore Previous INI dialogue

The dialogue follows the chosen style (decision 10). The snapshot grid, the diff computation, the change count, `Restore`'s enablement and what a restore writes are unchanged.

- **INI style:** exactly today's dialogue (SDD §9.7) — `Key` as `[section]/key`, values as the editor row formats them. The inverted `noMenuMusic` now reads its literal value
  (`true` when the file holds `1`), which is already how the dialogue shows it.
- **Friendly style:** the `Key` column reads *Page › Group › Label* (*Sound › Intro movies › Play the first movie*), so labels that repeat across groups — the two *Horizontal
  speed* rows on *Controls* — stay distinct (C11). Values are shown as the friendly row shows them: check boxes as `On` / `Off` by meaning (so `noMenuMusic=1` reads *Enable menu
  music: Off*), converted volumes, choice labels, cash as dollars (C8). `(not set)` stays. The kept-keys note names its keys the same way.
- **One row per diff row, in both styles,** so the change count and `Restore`'s enablement never differ between styles. The language pair therefore stays two rows in Friendly
  style: *Language › Language › Language code* (`lang`) and *… › Region code* (`sublang`), each showing its number (C14).
- Rows keep the diff's order in INI style; in Friendly style they follow the page order of §6.1 (C9).

## 9. Calls made by the author

These go in the ledger as rulings when the plan runs; each is cheap to reverse.

- **C1 — One setting for the launcher**, in `LauncherSettings`, not per installation: it is a preference about the player, not about a game copy.
- **C2 — Option wording.** *Friendly style* and *INI style*, under the line *"Show settings in the INI Config tab:"*.
- **C3 — Applies on `OK`.** The open editor re-renders immediately; there is no restart.
- **C4 — Check-box captions.** A friendly check box's label is its own caption, as Win9x dialogues do; other controls keep a left-hand label column.
- **C5 — Conversions live in presentation.** A conversion is a pure, two-way function on the displayed value, owned by the friendly layout. View models keep raw values as the
  source of truth, so dirty tracking, Reset, Restore defaults and Save never see a converted value.
- **C6 — Movie volumes join the volume scale** (§6.3).
- **C7 — Page resets on a switch.**
- **C8 — Friendly style check-box values read `On` / `Off`** in the Restore dialogue (by meaning); INI style keeps `true` / `false`.
- **C9 — Friendly style diff rows follow page order.**
- **C10 — One catalogue, two layouts.** The existing `IniEditorCatalogue` keeps each key's control kind, bounds and choice options once. Each style is a layout over it: the section
  or page list, the groups, and per key a label, hint, help and (friendly style only) conversion. A Desktop unit test asserts that each layout places every user-setting key the
  editor shows exactly once, and that every conversion round-trips every value in the key's range.
- **C11 — Friendly style diff keys name the group** (*Page › Group › Label*), because a label may repeat in two groups of one page.
- **C12 — INI style `noMenuMusic` caption** is *No menu music*; captions are per layout, so the INI layout keeps today's captions for the other check boxes.
- **C13 — The language info line shows current values**, matching INI style's read-only rows.
- **C14 — The Restore dialogue keeps one row per diff row** in both styles; the language pair is two friendly rows showing their codes.
- **C15 — The option description follows the selection**: one muted line under the radio group, as Win9x dialogues do, not one per option.
- **C16 — Slider steps are 1 %**; an off-step file value is kept until the slider moves, so opening the editor never changes the file.

## 10. SDD amendments (in this milestone)

- §5.1 / §5.2 — `LauncherSettings.IniEditorStyle`; the `IniEditorStyle` SmartEnum.
- §6.2 — the `IniEditorStyle` column.
- §9.3 — the two styles: §9.3 describes the INI style as today plus the literal inverted key; a new §9.3.3 *Display styles* covers the friendly layout (§6 here)
  and the shared behaviour (§7).
- §9.7 — the dialogue follows the style (§8).
- §9.8 — the *INI editor* group box and that it applies on `OK`.
- §13.2 — the editor item names both styles. Revision **1.16** (1.15 went to the headless view-load test).

## 11. Testing

- **Domain:** `IniEditorStyle` values and default; `LauncherSettings` default.
- **Application:** `SaveLauncherOptionsCommand` saves the style; the get-options query returns it.
- **Infrastructure:** the migration adds the column with its default, and `Down` removes it; an existing row reads `Friendly`.
- **Desktop:** both layouts cover every key exactly once (C10); every conversion round-trips; a volume slider maps 0–100 % to the raw scale in steps of 100 and
  leaves an off-step file value unchanged until moved; the editor keeps pending edits across a style switch; the INI-style `noMenuMusic` row is literal; Options
  saves and applies the style; the Restore dialogue formats keys and values per style.
- **Headless** (the view-load suite): the editor renders in both styles without errors; the friendly *Sound* page shows *Enable menu music* ticked when the file holds
  `noMenuMusic=0`.
- **In the running app:** every page in both styles; switch with unsaved edits; Save in the friendly style and confirm `zoo.ini` holds the raw values; the Restore dialogue in both
  styles.

## 12. Left to the builder

- The friendly hint and help wording for every row (§6.2), drafted from today's help.
- How a layout is declared in code, provided C5 and C10 hold.
- The exact layout of the page header and group boxes, following conventions §2–§5 and the spacing tokens.
