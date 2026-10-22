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
| `Views/Boot/LookingForZooTycoonView.axaml` | 6 | `HorizontalSpacing` | `10` |  |
| `Views/Boot/LookingForZooTycoonView.axaml` | 12 | `Margin` | `5 0 0 0` |  |
| `Views/Boot/LookingForZooTycoonView.axaml` | 14 | `Spacing` | `4` |  |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 7 | `Spacing` | `10` |  |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 8 | `HorizontalSpacing` | `10` |  |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 22 | `Margin` | `5 0 0 0` |  |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 24 | `Spacing` | `4` |  |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 34 | `Spacing` | `2` |  |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 43 | `Padding` | `11 15 15 12` |  |
| `Views/Boot/NoGameInstallationFoundView.axaml` | 46 | `RowSpacing` | `4` |  |
| `Views/Boot/OpenGameInstallationView.axaml` | 9 | `Spacing` | `10` |  |
| `Views/Boot/OpenGameInstallationView.axaml` | 10 | `HorizontalSpacing` | `10` |  |
| `Views/Boot/OpenGameInstallationView.axaml` | 17 | `Margin` | `5 0 0 0` |  |
| `Views/Boot/OpenGameInstallationView.axaml` | 19 | `Spacing` | `4` |  |
| `Views/Boot/OpenGameInstallationView.axaml` | 32 | `HorizontalSpacing` | `8` |  |
| `Views/Boot/OpenGameInstallationView.axaml` | 38 | `Padding` | `25 0` |  |
| `Views/Boot/OpenGameInstallationView.axaml` | 41 | `Spacing` | `8` |  |
| `Views/Boot/OpenGameInstallationView.axaml` | 44 | `Padding` | `10 0` |  |
| `Views/Boot/OpenGameInstallationView.axaml` | 48 | `Padding` | `10 0` |  |
| `Views/Boot/OpenGameInstallationView.axaml` | 52 | `Padding` | `10 0` |  |
| `Views/Common/InstallationFormView.axaml` | 7 | `Spacing` | `10` |  |
| `Views/Common/InstallationFormView.axaml` | 10 | `RowSpacing` | `5` |  |
| `Views/Common/InstallationFormView.axaml` | 26 | `HorizontalSpacing` | `5` |  |
| `Views/Common/InstallationFormView.axaml` | 45 | `HorizontalSpacing` | `10` |  |
| `Views/Common/InstallationFormView.axaml` | 54 | `Margin` | `2 0 0 0` |  |
| `Views/Common/InstallationGridView.axaml` | 28 | `Margin` | `4 0` |  |
| `Views/Common/InstallationGridView.axaml` | 51 | `Margin` | `4 0` |  |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 15 | `Margin` | `8` |  |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 17 | `Spacing` | `10` |  |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 18 | `HorizontalSpacing` | `10` |  |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 25 | `Margin` | `5 0 0 0` |  |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 27 | `Spacing` | `4` |  |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 43 | `Margin` | `0 5` |  |
| `Views/Dialogs/AddInstallationDialogView.axaml` | 44 | `Spacing` | `8` |  |
| `Views/Dialogs/ConfirmationPromptView.axaml` | 23 | `Margin` | `14` |  |
| `Views/Dialogs/ConfirmationPromptView.axaml` | 24 | `Spacing` | `14` |  |
| `Views/Dialogs/ConfirmationPromptView.axaml` | 25 | `HorizontalSpacing` | `10` |  |
| `Views/Dialogs/ConfirmationPromptView.axaml` | 40 | `Spacing` | `6` |  |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 14 | `Margin` | `8` |  |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 16 | `Spacing` | `10` |  |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 17 | `HorizontalSpacing` | `10` |  |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 24 | `Margin` | `5 0 0 0` |  |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 26 | `Spacing` | `8` |  |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 54 | `Margin` | `0 5` |  |
| `Views/Dialogs/DeleteInstallationDialogView.axaml` | 55 | `Spacing` | `8` |  |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 15 | `Margin` | `8` |  |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 17 | `Spacing` | `10` |  |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 18 | `HorizontalSpacing` | `10` |  |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 25 | `Margin` | `5 0 0 0` |  |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 27 | `Spacing` | `4` |  |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 36 | `Margin` | `0 6 0 0` |  |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 39 | `Margin` | `0 5` |  |
| `Views/Dialogs/EditInstallationDialogView.axaml` | 40 | `Spacing` | `8` |  |
| `Views/Dialogs/ErrorMessageView.axaml` | 24 | `Margin` | `14` |  |
| `Views/Dialogs/ErrorMessageView.axaml` | 25 | `Spacing` | `14` |  |
| `Views/Dialogs/ErrorMessageView.axaml` | 26 | `HorizontalSpacing` | `10` |  |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 14 | `Margin` | `8` |  |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 16 | `Spacing` | `10` |  |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 30 | `HorizontalSpacing` | `10` |  |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 31 | `Margin` | `4` |  |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 53 | `Spacing` | `4` |  |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 56 | `Spacing` | `4` |  |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 68 | `Spacing` | `4` |  |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 89 | `HorizontalSpacing` | `10` |  |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 90 | `Margin` | `4` |  |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 107 | `Spacing` | `6` |  |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 119 | `Spacing` | `4` |  |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 122 | `Spacing` | `4` |  |
| `Views/Dialogs/FixInstallationDialogView.axaml` | 134 | `Spacing` | `4` |  |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 18 | `Margin` | `8` |  |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 20 | `Spacing` | `10` |  |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 21 | `HorizontalSpacing` | `10` |  |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 28 | `Margin` | `5 0 0 0` |  |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 30 | `Spacing` | `6` |  |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 37 | `ColumnSpacing` | `16` |  |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 40 | `RowSpacing` | `6` |  |
| `Views/Dialogs/InstallationInfoDialogView.axaml` | 116 | `Margin` | `0 5` |  |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 14 | `Margin` | `8` |  |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 17 | `Margin` | `0 8 0 0` |  |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 18 | `Spacing` | `8` |  |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 32 | `Margin` | `8 0 0 0` |  |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 33 | `Spacing` | `4` |  |
| `Views/Dialogs/InstallationManagerDialogView.axaml` | 54 | `Margin` | `0 12 0 0` |  |
| `Views/Dialogs/LaunchErrorView.axaml` | 25 | `Margin` | `8` |  |
| `Views/Dialogs/LaunchErrorView.axaml` | 27 | `Spacing` | `10` |  |
| `Views/Dialogs/LaunchErrorView.axaml` | 28 | `HorizontalSpacing` | `10` |  |
| `Views/Dialogs/OptionsDialogView.axaml` | 15 | `Margin` | `8` |  |
| `Views/Dialogs/OptionsDialogView.axaml` | 17 | `Spacing` | `10` |  |
| `Views/Dialogs/OptionsDialogView.axaml` | 24 | `Padding` | `10 15` |  |
| `Views/Dialogs/OptionsDialogView.axaml` | 26 | `Spacing` | `4` |  |
| `Views/Dialogs/OptionsDialogView.axaml` | 49 | `Padding` | `10 15` |  |
| `Views/Dialogs/OptionsDialogView.axaml` | 51 | `Spacing` | `4` |  |
| `Views/Dialogs/OptionsDialogView.axaml` | 55 | `Margin` | `20 0 0 0` |  |
| `Views/Dialogs/OptionsDialogView.axaml` | 65 | `Margin` | `0 5` |  |
| `Views/Dialogs/OptionsDialogView.axaml` | 66 | `Spacing` | `8` |  |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 14 | `Margin` | `12` |  |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 16 | `Spacing` | `8` |  |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 54 | `Margin` | `4 0` |  |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 68 | `Margin` | `4 0` |  |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 76 | `Margin` | `4 0` |  |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 114 | `Margin` | `6 3` |  |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 136 | `Margin` | `6 2` |  |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 171 | `Margin` | `14` |  |
| `Views/Dialogs/RestorePreviousIniDialogView.axaml` | 187 | `Spacing` | `6` |  |
| `Views/Dialogs/SaveChangesPromptView.axaml` | 23 | `Margin` | `14` |  |
| `Views/Dialogs/SaveChangesPromptView.axaml` | 24 | `Spacing` | `14` |  |
| `Views/Dialogs/SaveChangesPromptView.axaml` | 25 | `HorizontalSpacing` | `10` |  |
| `Views/Dialogs/SaveChangesPromptView.axaml` | 40 | `Spacing` | `6` |  |
| `Views/IniConfig/IniEditorView.axaml` | 7 | `ColumnSpacing` | `8` |  |
| `Views/IniConfig/IniEditorView.axaml` | 8 | `Margin` | `8` |  |
| `Views/IniConfig/IniEditorView.axaml` | 10 | `RowSpacing` | `4` |  |
| `Views/IniConfig/IniEditorView.axaml` | 43 | `Margin` | `10 8` |  |
| `Views/IniConfig/IniEditorView.axaml` | 52 | `Spacing` | `6` |  |
| `Views/IniConfig/IniPlaceholderView.axaml` | 8 | `Margin` | `8` |  |
| `Views/IniConfig/IniPlaceholderView.axaml` | 9 | `Padding` | `10 15` |  |
| `Views/IniConfig/IniPlaceholderView.axaml` | 11 | `HorizontalSpacing` | `10` |  |
| `Views/IniConfig/IniPlaceholderView.axaml` | 19 | `Spacing` | `4` |  |
| `Views/IniConfig/IniPlaceholderView.axaml` | 37 | `Margin` | `0 8 0 0` |  |
| `Views/IniConfig/IniPlaceholderView.axaml` | 39 | `Spacing` | `6` |  |
| `Views/IniConfig/IniSectionView.axaml` | 7 | `Spacing` | `6` |  |
| `Views/IniConfig/IniSectionView.axaml` | 9 | `Spacing` | `6` |  |
| `Views/IniConfig/IniSectionView.axaml` | 24 | `Margin` | `0 4 0 0` |  |
| `Views/IniConfig/IniSectionView.axaml` | 25 | `Spacing` | `6` |  |
| `Views/IniConfig/IniSectionView.axaml` | 39 | `Margin` | `0 2` |  |
| `Views/IniConfig/IniSectionView.axaml` | 69 | `Margin` | `0 6 0 0` |  |
| `Views/MainWindow.axaml` | 37 | `Margin` | `5 1 5 2` |  |
| `Views/MainWindow.axaml` | 66 | `Margin` | `5 6 5 8` |  |
| `Views/MainWindow.axaml` | 70 | `Margin` | `0 2 0 0` |  |
| `Views/MainWindow.axaml` | 83 | `Margin` | `0 2 0 0` |  |
| `Views/Tabs/GeneralTabView.axaml` | 7 | `Margin` | `8` |  |
| `Views/Tabs/GeneralTabView.axaml` | 8 | `Spacing` | `10` |  |
| `Views/Tabs/GeneralTabView.axaml` | 10 | `Spacing` | `8` |  |
| `Views/Tabs/GeneralTabView.axaml` | 18 | `Padding` | `10 15` |  |
| `Views/Tabs/GeneralTabView.axaml` | 19 | `HorizontalSpacing` | `10` |  |
| `Views/Tabs/GeneralTabView.axaml` | 50 | `Margin` | `5 0 0 0` |  |
| `Views/Tabs/GeneralTabView.axaml` | 52 | `Spacing` | `4` |  |
| `Views/Tabs/GeneralTabView.axaml` | 63 | `ColumnSpacing` | `8` |  |
| `Views/Tabs/GeneralTabView.axaml` | 64 | `Margin` | `0 2 0 0` |  |
| `Views/Tabs/GeneralTabView.axaml` | 66 | `RowSpacing` | `2` |  |
| `Views/Tabs/GeneralTabView.axaml` | 141 | `Padding` | `10 15` |  |
| `Views/Tabs/GeneralTabView.axaml` | 142 | `HorizontalSpacing` | `10` |  |
| `Views/Tabs/GeneralTabView.axaml` | 150 | `Spacing` | `6` |  |
| `Views/Tabs/GeneralTabView.axaml` | 152 | `ColumnSpacing` | `20` |  |
| `Views/Tabs/GeneralTabView.axaml` | 154 | `RowSpacing` | `2` |  |
| `Views/Tabs/GeneralTabView.axaml` | 207 | `Padding` | `10 15` |  |
| `Views/Tabs/GeneralTabView.axaml` | 208 | `HorizontalSpacing` | `10` |  |
| `Views/Tabs/GeneralTabView.axaml` | 216 | `Spacing` | `6` |  |
| `Views/Tabs/GeneralTabView.axaml` | 218 | `ColumnSpacing` | `20` |  |
| `Views/Tabs/GeneralTabView.axaml` | 220 | `RowSpacing` | `2` |  |
| `Views/Tabs/GeneralTabView.axaml` | 258 | `Spacing` | `6` |  |
| `Views/Tabs/GeneralTabView.axaml` | 275 | `Spacing` | `6` |  |
| `Views/Tabs/IniConfigTabView.axaml` | 9 | `Margin` | `0,0,0,6` |  |
| `Views/Tabs/IniConfigTabView.axaml` | 12 | `Margin` | `0,0,6,0` |  |
