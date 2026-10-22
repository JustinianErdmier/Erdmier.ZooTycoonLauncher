# Milestone 11.1 — Style Tokens and Follow-ups: Ledger

**Date:** 2026-10-22

**Plan:** [`milestone-11.1-style-tokens-and-follow-ups.md`](../../Plans/Epic-1-MVP/milestone-11.1-style-tokens-and-follow-ups.md) ·
**Spec:** [`milestone-11.1-style-tokens-and-follow-ups-design.md`](../../Specs/Epic-1-MVP/milestone-11.1-style-tokens-and-follow-ups-design.md) ·
**Branch:** `feat/milestone-11.1-style-tokens-and-follow-ups`, cut from `main` at `838ba0b`

*Run in progress — the summary, rulings and outstanding items are written when the run finishes.*

---

## Spacing before and after

Every spacing value in the Desktop views before the snap (recorded at `d6445b8`, before any spacing change), with the token it became. Line numbers are
the original file's. To move to an even 2 → 14 scale later, remap each token here instead of searching the views.

| File | Line | Attribute | Original | New token |
|------|------|-----------|----------|-----------|
| `Views/Boot/LookingForZooTycoonView.axaml` | 6 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Boot/LookingForZooTycoonView.axaml` | 12 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Boot/LookingForZooTycoonView.axaml` | 14 | `Spacing` | `4` | `GapRelated` |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 7 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 8 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 22 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 24 | `Spacing` | `4` | `GapRelated` |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 34 | `Spacing` | `2` | `GapLabel` |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 43 | `Padding` | `11 15 15 12` | `GroupBoxPadding` |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 46 | `RowSpacing` | `4` | `GapRelated` |
| `Views/Boot/OpenGameInstallationView.axaml` | 9 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Boot/OpenGameInstallationView.axaml` | 10 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Boot/OpenGameInstallationView.axaml` | 17 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Boot/OpenGameInstallationView.axaml` | 19 | `Spacing` | `4` | `GapRelated` |
| `Views/Boot/OpenGameInstallationView.axaml` | 32 | `HorizontalSpacing` | `8` | `GapRelated` |
| `Views/Boot/OpenGameInstallationView.axaml` | 38 | `Padding` | `25 0` | `{tokens:Space Horizontal=GroupBoxInset}` |
| `Views/Boot/OpenGameInstallationView.axaml` | 41 | `Spacing` | `8` | `GapRelated` |
| `Views/Boot/OpenGameInstallationView.axaml` | 44 | `Padding` | `10 0` | `{tokens:Space Horizontal=GapUnrelated}` |
| `Views/Boot/OpenGameInstallationView.axaml` | 48 | `Padding` | `10 0` | `{tokens:Space Horizontal=GapUnrelated}` |
| `Views/Boot/OpenGameInstallationView.axaml` | 52 | `Padding` | `10 0` | `{tokens:Space Horizontal=GapUnrelated}` |
| `Views/Common/InstallationFormView.axaml` | 7 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Common/InstallationFormView.axaml` | 10 | `RowSpacing` | `5` | `GapRelated` |
| `Views/Common/InstallationFormView.axaml` | 26 | `HorizontalSpacing` | `5` | `GapRelated` |
| `Views/Common/InstallationFormView.axaml` | 45 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Common/InstallationFormView.axaml` | 54 | `Margin` | `2 0 0 0` | removed (icon-to-text gap is now `GapRelated`) |
| `Views/Common/InstallationGridView.axaml` | 28 | `Margin` | `4 0` | `{tokens:Space Horizontal=GapLabel}` |
| `Views/Common/InstallationGridView.axaml` | 51 | `Margin` | `4 0` | `{tokens:Space Horizontal=GapLabel}` |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 15 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 17 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 18 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 25 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 27 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 43 | `Margin` | `0 5` | `{tokens:Space Vertical=GapLabel}` |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 44 | `Spacing` | `8` | `GapRelated` |
| `Views/Dialogs/ConfirmationPromptView.axaml` | 23 | `Margin` | `14` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/ConfirmationPromptView.axaml` | 24 | `Spacing` | `14` | `GapUnrelated` |
| `Views/Dialogs/ConfirmationPromptView.axaml` | 25 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/ConfirmationPromptView.axaml` | 40 | `Spacing` | `6` | `GapRelated` |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 14 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 16 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 17 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 24 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 26 | `Spacing` | `8` | `GapUnrelated` |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 54 | `Margin` | `0 5` | `{tokens:Space Vertical=GapLabel}` |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 55 | `Spacing` | `8` | `GapRelated` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 15 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 17 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 18 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 25 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 27 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 36 | `Margin` | `0 6 0 0` | `{tokens:Space Top=GapRelated}` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 39 | `Margin` | `0 5` | `{tokens:Space Vertical=GapLabel}` |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 40 | `Spacing` | `8` | `GapRelated` |
| `Views/Dialogs/ErrorMessageView.axaml` | 24 | `Margin` | `14` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/ErrorMessageView.axaml` | 25 | `Spacing` | `14` | `GapUnrelated` |
| `Views/Dialogs/ErrorMessageView.axaml` | 26 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 14 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 16 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 30 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 31 | `Margin` | `4` | removed (the group box now carries `GroupBoxPadding`) |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 53 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 56 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 68 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 89 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 90 | `Margin` | `4` | removed (the group box now carries `GroupBoxPadding`) |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 107 | `Spacing` | `6` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 119 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 122 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 134 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 18 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 20 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 21 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 28 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 30 | `Spacing` | `6` | `GapRelated` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 37 | `ColumnSpacing` | `16` | `GapUnrelated` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 40 | `RowSpacing` | `6` | `GapRelated` |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 116 | `Margin` | `0 5` | `{tokens:Space Vertical=GapLabel}` |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 14 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 17 | `Margin` | `0 8 0 0` | `{tokens:Space Top=GapUnrelated}` |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 18 | `Spacing` | `8` | `GapRelated` |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 32 | `Margin` | `8 0 0 0` | `{tokens:Space Left=GapRelated}` |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 33 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 54 | `Margin` | `0 12 0 0` | `{tokens:Space Top=GapUnrelated}` |
| `Views/Dialogs/LaunchErrorView.axaml` | 25 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/LaunchErrorView.axaml` | 27 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/LaunchErrorView.axaml` | 28 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/OptionsDialogView.axaml` | 15 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/OptionsDialogView.axaml` | 17 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Dialogs/OptionsDialogView.axaml` | 24 | `Padding` | `10 15` | `GroupBoxPadding` |
| `Views/Dialogs/OptionsDialogView.axaml` | 26 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/OptionsDialogView.axaml` | 49 | `Padding` | `10 15` | `GroupBoxPadding` |
| `Views/Dialogs/OptionsDialogView.axaml` | 51 | `Spacing` | `4` | `GapRelated` |
| `Views/Dialogs/OptionsDialogView.axaml` | 55 | `Margin` | `20 0 0 0` | `{tokens:Space Left=GroupBoxInset}` |
| `Views/Dialogs/OptionsDialogView.axaml` | 65 | `Margin` | `0 5` | `{tokens:Space Vertical=GapLabel}` |
| `Views/Dialogs/OptionsDialogView.axaml` | 66 | `Spacing` | `8` | `GapRelated` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 14 | `Margin` | `12` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 16 | `Spacing` | `8` | `GapUnrelated` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 54 | `Margin` | `4 0` | `{tokens:Space Horizontal=GapLabel}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 68 | `Margin` | `4 0` | `{tokens:Space Horizontal=GapLabel}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 76 | `Margin` | `4 0` | `{tokens:Space Horizontal=GapLabel}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 114 | `Margin` | `6 3` | `{tokens:Space Horizontal=GapRelated, Vertical=GapSmallest}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 136 | `Margin` | `6 2` | `{tokens:Space Horizontal=GapRelated, Vertical=GapSmallest}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 171 | `Margin` | `14` | `{tokens:Space GapUnrelated}` |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 187 | `Spacing` | `6` | `GapRelated` |
| `Views/Dialogs/SaveChangesPromptView.axaml` | 23 | `Margin` | `14` | `{tokens:Space MarginDialog}` |
| `Views/Dialogs/SaveChangesPromptView.axaml` | 24 | `Spacing` | `14` | `GapUnrelated` |
| `Views/Dialogs/SaveChangesPromptView.axaml` | 25 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Dialogs/SaveChangesPromptView.axaml` | 40 | `Spacing` | `6` | `GapRelated` |
| `Views/IniConfig/IniEditorView.axaml` | 7 | `ColumnSpacing` | `8` | `GapRelated` |
| `Views/IniConfig/IniEditorView.axaml` | 8 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/IniConfig/IniEditorView.axaml` | 10 | `RowSpacing` | `4` | `GapRelated` |
| `Views/IniConfig/IniEditorView.axaml` | 43 | `Margin` | `10 8` | `{tokens:Space Horizontal=GapUnrelated, Vertical=GapRelated}` |
| `Views/IniConfig/IniEditorView.axaml` | 52 | `Spacing` | `6` | `GapRelated` |
| `Views/IniConfig/IniPlaceholderView.axaml` | 8 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/IniConfig/IniPlaceholderView.axaml` | 9 | `Padding` | `10 15` | `GroupBoxPadding` |
| `Views/IniConfig/IniPlaceholderView.axaml` | 11 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/IniConfig/IniPlaceholderView.axaml` | 19 | `Spacing` | `4` | `GapRelated` |
| `Views/IniConfig/IniPlaceholderView.axaml` | 37 | `Margin` | `0 8 0 0` | `{tokens:Space Top=GapUnrelated}` |
| `Views/IniConfig/IniPlaceholderView.axaml` | 39 | `Spacing` | `6` | `GapRelated` |
| `Views/IniConfig/IniSectionView.axaml` | 7 | `Spacing` | `6` | `GapRelated` |
| `Views/IniConfig/IniSectionView.axaml` | 9 | `Spacing` | `6` | `GapRelated` |
| `Views/IniConfig/IniSectionView.axaml` | 24 | `Margin` | `0 4 0 0` | `{tokens:Space Top=GapRelated}` |
| `Views/IniConfig/IniSectionView.axaml` | 25 | `Spacing` | `6` | `GapRelated` |
| `Views/IniConfig/IniSectionView.axaml` | 39 | `Margin` | `0 2` | `{tokens:Space Vertical=GapSmallest}` |
| `Views/IniConfig/IniSectionView.axaml` | 69 | `Margin` | `0 6 0 0` | `{tokens:Space Top=GapRelated}` |
| `Views/MainWindow.axaml` | 37 | `Margin` | `5 1 5 2` | `{tokens:Space Left=GapLabel, Top=GapSmallest, Right=GapLabel, Bottom=GapSmallest}` |
| `Views/MainWindow.axaml` | 66 | `Margin` | `5 6 5 8` | `{tokens:Space Horizontal=GapLabel, Vertical=GapRelated}` |
| `Views/MainWindow.axaml` | 70 | `Margin` | `0 2 0 0` | `{tokens:Space Top=GapSmallest}` |
| `Views/MainWindow.axaml` | 83 | `Margin` | `0 2 0 0` | `{tokens:Space Top=GapSmallest}` |
| `Views/Tabs/GeneralTabView.axaml` | 7 | `Margin` | `8` | `{tokens:Space MarginDialog}` |
| `Views/Tabs/GeneralTabView.axaml` | 8 | `Spacing` | `10` | `GapUnrelated` |
| `Views/Tabs/GeneralTabView.axaml` | 10 | `Spacing` | `8` | `GapLabel` |
| `Views/Tabs/GeneralTabView.axaml` | 18 | `Padding` | `10 15` | `GroupBoxPadding` |
| `Views/Tabs/GeneralTabView.axaml` | 19 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 50 | `Margin` | `5 0 0 0` | removed (icon gap moved to `IconBesideBody` on the icon) |
| `Views/Tabs/GeneralTabView.axaml` | 52 | `Spacing` | `4` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 63 | `ColumnSpacing` | `8` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 64 | `Margin` | `0 2 0 0` | `{tokens:Space Top=GapSmallest}` |
| `Views/Tabs/GeneralTabView.axaml` | 66 | `RowSpacing` | `2` | `GapSmallest` |
| `Views/Tabs/GeneralTabView.axaml` | 141 | `Padding` | `10 15` | `GroupBoxPadding` |
| `Views/Tabs/GeneralTabView.axaml` | 142 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 150 | `Spacing` | `6` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 152 | `ColumnSpacing` | `20` | `GapUnrelated` |
| `Views/Tabs/GeneralTabView.axaml` | 154 | `RowSpacing` | `2` | `GapSmallest` |
| `Views/Tabs/GeneralTabView.axaml` | 207 | `Padding` | `10 15` | `GroupBoxPadding` |
| `Views/Tabs/GeneralTabView.axaml` | 208 | `HorizontalSpacing` | `10` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 216 | `Spacing` | `6` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 218 | `ColumnSpacing` | `20` | `GapUnrelated` |
| `Views/Tabs/GeneralTabView.axaml` | 220 | `RowSpacing` | `2` | `GapSmallest` |
| `Views/Tabs/GeneralTabView.axaml` | 258 | `Spacing` | `6` | `GapRelated` |
| `Views/Tabs/GeneralTabView.axaml` | 275 | `Spacing` | `6` | `GapRelated` |
| `Views/Tabs/IniConfigTabView.axaml` | 9 | `Margin` | `0,0,0,6` | `{tokens:Space Bottom=GapRelated}` |
| `Views/Tabs/IniConfigTabView.axaml` | 12 | `Margin` | `0,0,6,0` | `{tokens:Space Right=GapRelated}` |

### How the values were classified (Task 3)

Tokens in the table are shown as written in the views: a bare name is a `{StaticResource …}`, and `{tokens:Space …}` is the `SpaceExtension`. Rulings behind the less obvious rows:

- **Icon beside text.** Every `HorizontalSpacing` of `10` beside an icon became `GapRelated`; the text's `5 0 0 0` margin was removed, and the icon took `IconBesideBody` (Boot
  views, Add / Edit / Delete / Info dialogues, General tab, Fix, INI status) or `IconBesideMessage` (confirmation, error, launch-error and save prompts). The derived margins
  (`IconBesideBodyMargin` 5, `IconBesideMessageMargin` 11) live in `Spacing.axaml` with the arithmetic beside them.
- **Group boxes.** Every `Padding` of `10 15`, and the Auto-locate trail's `11 15 15 12`, became `GroupBoxPadding` (left and right `GroupBoxInset` 14, bottom
  `GroupBoxBottom` 11, top `GroupBoxFirstControl` 18 minus an estimated 14 px header = 4). The Fix dialogue's two group boxes had no padding and a `Margin="4"` on the dock
  inside; the margin went and they now take `GroupBoxPadding` like every other group box.
- **Dialogue and tab-page body margins** (`8`, `12`, `14`) became `MarginDialog`; gaps between sections and paragraphs (`8`, `10`, `14`) `GapUnrelated`; button rows, form rows and
  stacks of related controls (`4`, `5`, `6`, `8`) `GapRelated`; a label stacked over its control (`2` in the Auto-locate candidate, `8` in the Installation Profile row) `GapLabel`.
- **Judgement calls.** Button `Padding` `25 0` (Open) and `10 0` (Add / Info / Manage) have no guideline role: they took `GroupBoxInset` and `GapUnrelated` as the nearest
  sensible tokens. The Options explanatory text's `20 0 0 0` indent under a checkbox took `GroupBoxInset`. The `0 5` margin around each dialogue's button row became
  `Vertical=GapLabel`. Data-grid cell margins `4 0` became `Horizontal=GapLabel`;
  the Restore diff rows' `6 3` and `6 2` became `Horizontal=GapRelated, Vertical=GapSmallest`.
- **Main window.** The menu margin `5 1 5 2` became `Left`/`Right=GapLabel`, `Top`/`Bottom=GapSmallest`; the content margin `5 6 5 8` became `Horizontal=GapLabel, Vertical=GapRelated`
  (the hosted tab views carry their own `MarginDialog`); the status bars' `0 2 0 0` became `Top=GapSmallest`. The window size and the INI tab's row-label width are untouched.
