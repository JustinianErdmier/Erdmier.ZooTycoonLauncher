# Style Tokens and Milestone 11 Follow-ups — Design Spec

> **Milestone:** 11.1 — follow-up to milestone 11 (Epic 1 — MVP)
> **Date:** 2026-10-22
> **Author:** Justinian
> **Status:** Approved for implementation (maintainer decisions 1–9 incl. 1a–1b; calls C1–C6; §6a added after the first review)
> **Authoritative spec:** [1-zoo-tycoon-launcher-master-design.md](../1-zoo-tycoon-launcher-master-design.md) §9, §11.5, §13.5
> **Source of items:** [milestone-11 ledger](../../Ledgers/Epic-1-MVP/milestone-11-ledger.md) *Outstanding items → Polish*
> **Deferred work taken:** DW-24 (consistent spacing, padding and margins)
> **Design references:** `Docs/UserInterfaceDesign/conventions.md` §1–§2; Microsoft's Windows design guidelines, *Layout* (spacing in dialog units) and *Layout
> Specifications: Win32* (control sizes)
> **Branch:** `feat/milestone-11.1-style-tokens-and-follow-ups`

---

## 1. Goal

Finish what milestone 11 started for colours: every typography value, every spacing value and every repeated control size comes from one named place, and an
architecture test per group keeps hand-written values out. Spacing moves onto the Windows 9x guideline values, so the launcher's layout follows the same
rhythm everywhere. Faded text uses one method. The four polish items milestone 11 left open are closed.

## 2. Decisions (maintainer)

| # | Question                         | Decision                                                                                                                                         |
|---|----------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | Faded text                       | Every `Opacity="0.6"` fade becomes the named grey `ForegroundMuted` (`#808080`), except the Installation Info dialogue's row labels, which take `ForegroundDim` (`#404040`) as conventions §1.1 reserves for them. |
| 1a | Icons                           | Pixel-crisp: the shared icon styles draw without smoothing, so 16 px art shown at 32 px becomes pixel-doubled (authentic Windows 95).               |
| 1b | Per-side spacing                | A recurring per-side pattern with a clear purpose gets its own purpose-based token; a one-off is snapped to the scale. Icons beside text get `GapIconBody` (property-sheet layouts) or `GapIconMessage` (message prompts), measured from Windows screenshots; the Auto-locate trail's `11 15 15 12` is snapped. |
| 2 | Scope of tokens                  | Everything the conventions can name: typography, spacing and repeated control sizes, not only colours.                                         |
| 3 | Spacing values                   | **Windows 9x purpose-based tokens** (§4), not an even 2 → 14 scale. Existing values are snapped onto them, accepting small layout shifts.        |
| 4 | Record of the old spacing        | The 11.1 ledger records every original spacing value, its file and line, and the token it became — enough to remap to an even 2 → 14 scale later. |
| 5 | One test per token group         | Colours, typography, spacing and shared sizes each have their own architecture test.                                                            |
| 6 | Milestone 11 leftovers           | All four "Polish" items from the milestone 11 ledger are in scope (§6).                                                                         |
| 7 | INI field focus (added after the first review) | Only keyboard focus keeps an INI field focused; a field the pointer focused gives focus back once its value is set, and a click elsewhere or Esc clears it (§6a.1). |
| 8 | Muted grey (added after the first review)      | `ForegroundMuted` darkens from `#808080` to `#666666` (§6a.2).                                                                     |
| 9 | Grid headers (added after the first review)    | The installation grid's column headers stop reacting to clicks (§6a.3).                                                            |

## 3. Non-goals and anti-goals

- No new UI, no new behaviour, no theme switching.
- Not a redesign: fonts, colours and sizes keep their current look, apart from the changes this spec names (muted grey, monospace face, spacing).

## 4. Spacing tokens (Windows 9x guideline values)

Microsoft's guidelines measure layout in dialog units (a quarter of the dialogue font's average character width across, an eighth of its height down). The
values below convert them for 8 pt MS Sans Serif at 96 DPI (about 1.5 px per DLU across, 1.6 px down); a 50 × 14 DLU command button converts to 75 × 23 px,
which is the `MinWidth="75"` the launcher's buttons already use.

| Token                  | px | Guideline (DLUs)                                                                       |
|------------------------|----|----------------------------------------------------------------------------------------|
| `GapSmallest`          | 3  | Smallest space between controls (2)                                                    |
| `GapLabel`             | 5  | Text label to its control (3)                                                          |
| `GapRelated`           | 6  | Related controls; buttons in a row; controls inside a group box (4)                    |
| `MarginDialog`         | 11 | Dialogue margins on all sides (7)                                                      |
| `GapUnrelated`         | 11 | Unrelated controls; paragraphs of text (7)                                             |
| `GroupBoxBottom`       | 11 | Last control to the bottom of a group box (7)                                          |
| `GroupBoxInset`        | 14 | Group box left edge to its controls (9)                                                |
| `GroupBoxFirstControl` | 18 | Group box top edge (including its header) to its first control (11)                   |
| `GapIconBody`          | 11 | Icon to the body text beside it in a property-sheet layout (measured; see below)        |
| `GapIconMessage`       | 17 | Icon to the message text in a message prompt (measured; see below)                     |

The guidelines' spacing table names no icon gap, so the two icon tokens come from measuring real screenshots at actual size: Internet Options' *Content* and
*General* tabs put body text 10–11 px after the 32 px icon slot and buttons about 6 px after the text; Windows 98's *Error Details* message box and
Classic.Avalonia's message box both put the message 17 px after the icon. `GapIconBody` serves the group-box icon headers (General tab, Fix and Info
dialogues and the like); `GapIconMessage` serves the confirmation, error and save prompts.

These replace today's pairing of a `DockPanel` with `HorizontalSpacing="10"` and a `Margin="5 0 0 0"` on the text beside the icon (15 px). The panel's spacing
becomes `GapRelated` (6 px), which is the right text-to-button distance, and the icon carries the rest of its gap as a right margin, so the visible icon-to-text
distance is exactly `GapIconBody` or `GapIconMessage` while buttons stay 6 px from the text. Any value derived from the tokens (such as the icon's extra margin)
is defined in a token file from those tokens, never written in a view.

## 5. Calls made by the author

- **C1 — Token files.** Under `Desktop/Styles/`, next to `Colours.axaml`: `Typography.axaml`, `Spacing.axaml` (resources: `x:Double` for `Spacing`, `Thickness`
  for margins and paddings) and `Controls.axaml` (shared styles), all merged in `App.axaml` and used through `StaticResource`.
- **C2 — Typography tokens.** `FontFamilySystem` and `FontFamilyMono` with conventions §1.3's stacks; `FontSizeBase` 11, `FontSizeStrong` 14,
  `FontSizeAboutTitle` 13; `FontWeightStrong` (`Bold`) and `FontWeightHeading` (the `Solid` weight the state headlines use today). The nine `Consolas` uses become
  `FontFamilyMono` (the prototype's Lucida Console stack), so they change face; `FontStyle="Italic"` stays a literal (a style, not a token). Tokens nothing sets
  today (`FontFamilySystem`, `FontSizeStrong`, `FontSizeAboutTitle`) are defined but not applied, so headlines and the default face do not change.
- **C3 — Shared styles** (`Controls.axaml`), the conventions §2 primitives that the code repeats: `DialogButton` (`MinWidth` 75), `IconLarge` / `IconSmall`
  (32 / 16 px, crisp interpolation) and `MutedText` (`ForegroundMuted`, `FontSizeBase`). Views opt in through `Classes`.
- **C4 — Snapping.** Each spacing value is classified by its role and given the matching token. A value with no clear role takes the nearest token, and a `0`
  stays `0`. A recurring per-side pattern with a clear purpose gets its own purpose-based token (as the icon gaps do) and is listed in the ledger; one-offs are
  snapped — the Auto-locate trail (a `HeaderedContentControl` drawn as a group box) takes the group-box tokens: `GroupBoxInset` left and right,
  `GroupBoxBottom` bottom, a top that puts its first row `GroupBoxFirstControl` below the box's top edge, and `GapRelated` between its rows. Thicknesses are
  written with a small markup extension that builds a `Thickness` from token names — `{tokens:Space GapRelated}`, `{tokens:Space Left=GapLabel}`,
  `{tokens:Space Left=GroupBoxInset, Top=GroupBoxFirstControl}` — so no per-combination resources are needed and the spacing test sees a `{…}` value.
  Group-box tokens are measured from the box edge: the group box's top content padding is set so its first control sits `GroupBoxFirstControl` below the
  box's top edge after Classic.Avalonia's header. The main window's fixed sizes and the INI tab's row-label width stay as they are (layout dimensions, not
  spacing).
- **C5 — One test per group** (`Tests.Architecture`): `NoLiteralColoursTests` (widened, §6 item 2), `NoLiteralTypographyTests` (`FontFamily`, `FontSize`,
  `FontWeight` hold resources), `NoLiteralSpacingTests` (`Margin`, `Padding`, `Spacing`, `RowSpacing`, `ColumnSpacing`, `HorizontalSpacing` hold resources, or `0`)
  and `SharedSizeStylesTests` (no `MinWidth="75"` and no 32 / 16 px `Width`/`Height` pair on an `Image` outside `Controls.axaml`). Each excludes its own token file,
  catches style setters and owner-qualified attributes, and reports `file:line`.
- **C6 — The ledger table.** Before any spacing change, the controller records every `Margin` / `Padding` / `Spacing`-family value in the views (file, line,
  attribute, original value); the implementer adds the token each became. The table lives in the 11.1 ledger.

## 6. Milestone 11 leftovers

1. **Esc or close during an Options save.** While a save is in flight, `Cancel` is disabled and the title-bar close is refused (as the Restore Previous INI
   dialogue already does with `CanClose`).
2. **Colour test coverage.** `NoLiteralColoursTests` also checks `SelectionBrush`, `CaretBrush`, `OpacityMask` and any other `*Brush` attribute, colours written
   as element content (`<SolidColorBrush>Red</SolidColorBrush>`, `<Color>…</Color>`) and setters with an attribute between `<Setter` and `Property` / `Value`.
3. **Test hygiene.** One shared `ProjectPaths` helper replaces the duplicated `ResolveProjectDirectory`; the launcher persistence test helpers stop disposing the
   connection EF owns; the migration Down test asserts the restored column's `'System'` default its name promises.
4. **conventions.md tidy.** Drop the vestigial `dk-c-*` column from §1.2 and hard-wrap every prose line over 180 characters.

## 6a. Maintainer follow-ups (added 2026-10-22, after the first review)

Found by the maintainer using the build from the first review. They extend this milestone rather than open another.

### 6a.1 INI field focus follows the keyboard

Today a mouse click on any INI field (the `fullscreen` drop-down, a number box, a toggle) leaves that field focused after the value is set. The editor footer
shows a focused field's help instead of the save status, so the status disappears. Nothing gives the focus back: clicking elsewhere and Esc do not, and only
switching section and back clears it.

- **Keyboard focus is the only lasting focus.** A field reached with Tab (or the arrow keys) stays focused and shows its help in the footer, as today.
- **Pointer focus is temporary.** A field the pointer focused gives focus back as soon as its value is set:
  - a drop-down, when its list closes;
  - a toggle, after the click;
  - a number box's spinner, after the step;
  - a text or number box being typed into, on Enter.
- **Pointer focus never drives the footer.** The help line shows for pointer hover (unchanged) and keyboard focus only. A field still being typed into after
  a click does not take the footer over.
- **A click elsewhere or Esc clears focus.** A click on any part of the editor that is not an input, or Esc while an INI field is focused, clears the focus,
  and the footer goes back to the save status. A value typed but not yet committed is committed the same way clicking away commits it today; Esc does not
  discard it.
- Switching section keeps clearing focus and hover as today.

### 6a.2 A slightly darker muted grey

`ForegroundMuted` (`#808080`) is hard to read on the `#C0C0C0` surface: a contrast of 2.17:1. It becomes `#666666` (3.16:1, above the 3:1 guideline for
interface text), still clearly lighter than `ForegroundDim` (`#404040`, 5.70:1). Only the token changes; `MidShadow` (`#808080`, the 3D shadow) stays as it
is. conventions §1.1/§7 and SDD §9 are updated where they quote the old value.

### 6a.3 Installation grid headers do not react to clicks

The installation grid (`InstallationGridView`, shared by the Installation Manager and the Open Game Installation view) has sorting off, yet its column
headers still press when clicked, promising an action that never comes. They stop reacting to clicks, as the Restore Previous INI grid's already do. The
columns stay resizable if the theme allows it without a clickable header; if it does not, resizing goes too, and the ledger records that.

## 7. Documentation

- conventions §1.3 and §1.4 become the typography and spacing token tables as built (§4 here), §2 points at `Controls.axaml`, §7 names one fading method.
- SDD §9 (the conventions summary and §9.11's colour note) mentions the four token files and tests; §11.5 lists the three new tests. DW-24 keeps only its
  second half — reusable controls for common stack and dock panels, and `UniformStack`'s open `TODO`s — reworded; its spacing half is done. conventions §4's
  `Opacity="0.55"` rule for a whole muted group box stays as a separate, disabled-state rule (it is not text fading).
- Revision 1.14.

## 8. Testing

- **Architecture:** the four tests (C5), each proven by a planted violation.
- **Desktop / other projects:** unchanged behaviour; the existing suites pass.
- **In the running app:** compare before/after captures of the main window states and every dialogue; the expected differences are the lighter muted text,
  the Info labels' darker grey, the monospace face, the pixel-crisp icons and the spacing shifts — nothing clipped, overlapping or misaligned.
