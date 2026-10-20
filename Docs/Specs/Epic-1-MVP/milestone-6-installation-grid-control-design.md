# Installation Grid Control — Design Spec

**Date:** 2026-07-02  
**Status:** Approved  
**Related SDD sections:** §7.2.2, §9.4, §9.6  

---

## 1. Overview

A shared `InstallationGridView` / `InstallationGridViewModel` pair provides the three-column installation `DataGrid` used in two distinct host contexts:

- **OpenGameInstallation state** (main window, SDD §9.6) — grid above a button row; buttons are `Add`, `Info`, `Manage` (left) and `Open` (right).
- **Installation Manager dialogue** (SDD §9.4) — grid with a vertical button panel docked to its right; buttons are `Add`, `Info`, `Edit`, `Delete`, `Fix`.

The shared control owns only the grid itself — data loading, row projection, and live messenger refresh. Button commands, layout, and navigation remain in each host.

---

## 2. File layout

```text
Source/Erdmier.ZooTycoonLauncher.Desktop/
├── Models/
│   └── InstallationGridRowModel.cs          sealed record — immutable row projection
├── ViewModels/Common/
│   └── InstallationGridViewModel.cs         shared VM — data, selection, messenger
└── Views/Common/
    ├── InstallationGridView.axaml
    └── InstallationGridView.axaml.cs
```

Additionally, two new converters are required (placed in a `Converters/` folder at the Desktop project level if one does not already exist):

```text
Source/Erdmier.ZooTycoonLauncher.Desktop/
└── Converters/
    ├── BoolToFontWeightConverter.cs
    └── ColourTokenToBrushConverter.cs
```

---

## 3. `InstallationGridRowModel`

**Namespace:** `Erdmier.ZooTycoonLauncher.Desktop.Models`  
**Kind:** `sealed record` (immutable; rows are never mutated — the collection is replaced wholesale on refresh)

| Property | Type | Source |
|---|---|---|
| `Id` | `Guid` | `InstallationSummary.Id` |
| `Name` | `string` | `InstallationSummary.Name` |
| `Path` | `string` | `InstallationSummary.Path` |
| `ValidityDisplayName` | `string` | `InstallationSummary.Validity.DisplayName` |
| `ValidityColourToken` | `string` | `InstallationSummary.Validity.ColourToken` (`"Green"` or `"Red"`) |
| `IsDefault` | `bool` | `InstallationSummary.IsDefault` |

Properties from `InstallationSummary` not included here (`AddedUtc`, `ModifiedUtc`, `LastPlayedUtc`, `LastOpenedUtc`) belong to the Info dialogue only and are intentionally excluded.

---

## 4. `InstallationGridViewModel`

**Namespace:** `Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common`  
**Base:** `ViewModelBase`  
**Implements:** `IDisposable`  
**Dependencies:** `IMediator`, `IMessenger`

### 4.1 Public surface

| Member | Kind | Notes |
|---|---|---|
| `Rows` | `ObservableCollection<InstallationGridRowModel>` | `ItemsSource` for the DataGrid |
| `SelectedRow` | `InstallationGridRowModel?` | `[ObservableProperty]`; two-way bound to `DataGrid.SelectedItem` |
| `HasSelection` | `bool` | Computed: `SelectedRow is not null`; consumed by host VM `CanExecute` guards |
| `IsSelectionInvalid` | `bool` | Computed: `SelectedRow?.ValidityColourToken == "Red"`; guards the `Fix` button |
| `LoadAsync(CancellationToken)` | `Task` | Dispatches `GetAllInstallationsQuery`, projects results, replaces `Rows` |

### 4.2 Sort order

On every load, rows are sorted before populating `Rows`:

1. The default installation (`IsDefault == true`) pins to the top.
2. Remaining rows sort alphabetically by `Name`, case-insensitive.

Implemented as an `IComparer<InstallationGridRowModel>` applied via LINQ, per SDD §9.4.

### 4.3 Messenger subscriptions

Registered in the constructor via `IMessenger`. Each message triggers a `LoadAsync` call to rebuild the list:

- `InstallationAddedMessage`
- `InstallationChangedMessage`
- `InstallationDeletedMessage`
- `DefaultInstallationChangedMessage`

### 4.4 Disposal

`Dispose()` calls `_messenger.Unregister(this)` so that a closed dialogue's VM does not continue receiving messages.

### 4.5 Designer constructor

A parameterless constructor (delegating to `file`-scoped null-object stubs) provides an empty `Rows` collection and `null` selection for the XAML designer.

---

## 5. `InstallationGridView`

**Namespace:** `Erdmier.ZooTycoonLauncher.Desktop.Views.Common`  
**`x:DataType`:** `vm:InstallationGridViewModel`

A single `DataGrid` with compiled bindings:

```
ItemsSource    = "{Binding Rows}"
SelectedItem   = "{Binding SelectedRow, Mode=TwoWay}"
SelectionMode  = Single
IsReadOnly     = True
CanUserReorderColumns = False
CanUserResizeColumns  = True
CanUserSortColumns    = False
```

### 5.1 Columns

| Column header | Binding | Display notes |
|---|---|---|
| `Name` | `Name` | `FontWeight` driven by `IsDefault` via `BoolToFontWeightConverter` (`Bold` / `Normal`) |
| `Path` | `Path` | Plain text |
| `Status` | `ValidityDisplayName` | `Foreground` driven by `ValidityColourToken` via `ColourTokenToBrushConverter` |

### 5.2 Sizing

The UserControl carries no `Margin`, fixed `Height`, or `Width`. Sizing is fully the host's responsibility so the control composes cleanly into:

- The main-window state (full-width, height fills available space above the button row).
- The dialogue (left portion of a `DockPanel`, with the button panel docked right).

---

## 6. Converters

**`BoolToFontWeightConverter`** — `IValueConverter`; `true` → `FontWeight.Bold`, `false` → `FontWeight.Normal`. General-purpose; lives in `Converters/` for potential reuse.

**`ColourTokenToBrushConverter`** — `IValueConverter`; maps the string token `"Green"` → `Brushes.Green` (or a theme-appropriate green), `"Red"` → `Brushes.Red`. Keeps colour logic in the Desktop layer; no `IBrush` properties leak into the shared record.

---

## 7. Host integration

### 7.1 `OpenGameInstallationViewModel` (existing — modified)

- Gains `InstallationGridViewModel Grid` as a constructor dependency.
- Calls `Grid.LoadAsync()` on activation.
- Exposes `OpenCommand`, `AddCommand`, `InfoCommand`, `ManageCommand` with `CanExecute` delegating to `Grid.HasSelection` where relevant.
- `AddCommand` calls `_dialogs.ShowAddInstallationAsync()` then `Grid.LoadAsync()` (the messenger subscription will also fire, but an explicit reload is simpler).

View layout: `StackPanel` with the header `DockPanel` at top, then `InstallationGridView`, then a button row (`DockPanel`: `Open` docked right; `Add`, `Info`, `Manage` left-aligned in a `UniformStack`).

### 7.2 `InstallationManagerDialogViewModel` (new — scaffolded)

- Takes `InstallationGridViewModel Grid` as a constructor dependency.
- Calls `Grid.LoadAsync()` on activation.
- Exposes `AddCommand`, `InfoCommand`, `EditCommand`, `DeleteCommand`, `FixCommand`. `CanExecute` for `Info`, `Edit`, `Delete` delegates to `Grid.HasSelection`; `Fix` delegates to `Grid.IsSelectionInvalid`.

View layout: `DockPanel` — button panel (`StackPanel`) docked right, `InstallationGridView` fills remaining space. Footer: row-count `TextBlock` + helper text (`Double-click a row to open`) docked left, `Close` button docked right.

---

## 8. Data flow

```
Host VM activation
  └─ Grid.LoadAsync()
       └─ GetAllInstallationsQuery → IReadOnlyList<InstallationSummary>
            └─ Project + sort → ObservableCollection<InstallationGridRowModel>
                 └─ DataGrid binds and renders

Any installation change (Add / Edit / Delete / SetDefault)
  └─ Application layer publishes messenger message
       └─ InstallationGridViewModel handler → Grid.LoadAsync() (rebuild)
```

---

## 9. What is NOT in scope

- In-place editing in the grid.
- Column sorting via column header clicks (sort is fixed: default first, then alpha).
- A "row count" footer inside the shared control — that belongs to the Installation Manager host only.
- Double-click handling inside the shared control — host views wire their own `DoubleTapped` handlers.
