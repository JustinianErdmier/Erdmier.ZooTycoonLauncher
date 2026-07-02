# Installation Grid Control Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a shared `InstallationGridView` / `InstallationGridViewModel` control that renders the three-column installation DataGrid, wire it into the existing `OpenGameInstallationView`, and scaffold the `InstallationManagerDialog` that reuses the same control.

**Architecture:** A sealed `InstallationGridRowModel` record is the immutable row projection. `InstallationGridViewModel` owns data loading via `GetAllInstallationsQuery`, sorting, and selection state; it subscribes to the four installation messenger messages for future live-refresh (publishers are a separate concern — hosts call `LoadAsync` explicitly after their own mutations). Each host VM holds an `InstallationGridViewModel` dependency and adds its own action commands around it.

**Tech Stack:** .NET 10 / C# 13, Avalonia 11.3 + Classic.Avalonia, CommunityToolkit.Mvvm (ObservableProperty, RelayCommand, IMessenger / WeakReferenceMessenger), Mediator (source-generated), ErrorOr.

## Global Constraints

- Windows-only project; no cross-platform considerations needed.
- British English in all identifiers, comments, and copy where the choice is ours (do not alter external API spellings like `Color`, `FontWeight`).
- One type per file, no exceptions. File-scoped namespaces everywhere.
- No files at any project root — every file lives under a subfolder mirroring its namespace.
- XML doc comments (`///`) on every public member and type. Plain `//` for inline implementation notes only.
- `<c>…</c>` tags carry no inside whitespace: `<c>zoo.ini</c>` not `<c> zoo.ini </c>`.
- `[ ObservableProperty ]` on partial properties; `[ RelayCommand ]` on private async methods (spaced bracket style, matching existing code).
- Compiled bindings: every XAML file declares `x:DataType`.
- Designer constructors: parameterless, delegates to `file`-scoped null-object stubs.
- All timestamps UTC; localisation at UI boundary only.
- Build command: `dotnet build Erdmier.ZooTycoonLauncher.slnx` (run from repo root).
- Run command: `dotnet run --project Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj`.

---

## File Map

| Status | Path | Responsibility |
|--------|------|----------------|
| **Create** | `Source/…Desktop/Models/InstallationGridRowModel.cs` | Immutable row projection record |
| **Create** | `Source/…Desktop/Converters/BoolToFontWeightConverter.cs` | `bool` → `FontWeight.Bold / Normal` |
| **Create** | `Source/…Desktop/Converters/ColourTokenToBrushConverter.cs` | `"Green"/"Red"` → `IBrush` |
| **Create** | `Source/…Desktop/ViewModels/Common/InstallationGridViewModel.cs` | Shared data/selection VM |
| **Create** | `Source/…Desktop/Views/Common/InstallationGridView.axaml` | DataGrid UserControl |
| **Create** | `Source/…Desktop/Views/Common/InstallationGridView.axaml.cs` | Code-behind |
| **Create** | `Source/…Desktop/ViewModels/Dialogs/InstallationManagerDialogViewModel.cs` | Manager dialog VM |
| **Create** | `Source/…Desktop/Views/Dialogs/InstallationManagerDialogView.axaml` | Manager dialog view |
| **Create** | `Source/…Desktop/Views/Dialogs/InstallationManagerDialogView.axaml.cs` | Code-behind |
| **Modify** | `Source/…Desktop/GlobalUsings.cs` | Add messaging + new namespace imports |
| **Modify** | `Source/…Desktop/Composition/DesktopServiceCollectionExtensions.cs` | Register `IMessenger`, transient VMs |
| **Modify** | `Source/…Desktop/Composition/IDialogService.cs` | Add `ShowInstallationManagerAsync` |
| **Modify** | `Source/…Desktop/Composition/AvaloniaDialogService.cs` | Implement `ShowInstallationManagerAsync` |
| **Modify** | `Source/…Desktop/ViewModels/MainWindowViewModel.cs` | Inject + pass `InstallationGridViewModel` |
| **Modify** | `Source/…Desktop/ViewModels/Boot/OpenGameInstallationViewModel.cs` | Add grid + commands |
| **Modify** | `Source/…Desktop/Views/Boot/OpenGameInstallationView.axaml` | Replace TODO TextBlock |

---

## Task 1: `InstallationGridRowModel`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Models/InstallationGridRowModel.cs`

**Interfaces:**
- Consumes: `InstallationSummary` (Application.Common.Models), `InstallationValidity` (Domain.Installations)
- Produces: `InstallationGridRowModel(Guid Id, string Name, string Path, string ValidityDisplayName, string ValidityColourToken, bool IsDefault)` — consumed by Tasks 3, 4, 5, 6.

- [ ] **Step 1: Create the record**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/Models/InstallationGridRowModel.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.Models;

/// <summary>Immutable row projection used by <c>InstallationGridViewModel</c> to populate the installation <c>DataGrid</c>.</summary>
/// <param name="Id">The installation's identifier.</param>
/// <param name="Name">The user-visible installation name.</param>
/// <param name="Path">The fully qualified directory path.</param>
/// <param name="ValidityDisplayName">Human-readable validity label (e.g. <c>Valid</c>, <c>Invalid — No EXE</c>).</param>
/// <param name="ValidityColourToken">Colour token consumed by <c>ColourTokenToBrushConverter</c> — <c>Green</c> for valid, <c>Red</c> for all invalid states.</param>
/// <param name="IsDefault"><see langword="true" /> when this installation is the launcher default; drives bold font weight on the Name cell.</param>
public sealed record InstallationGridRowModel(Guid   Id,
                                              string Name,
                                              string Path,
                                              string ValidityDisplayName,
                                              string ValidityColourToken,
                                              bool   IsDefault);
```

- [ ] **Step 2: Build**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Models/InstallationGridRowModel.cs
git commit -m "feat(✨): add InstallationGridRowModel row projection record"
```

---

## Task 2: Value Converters

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Converters/BoolToFontWeightConverter.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Converters/ColourTokenToBrushConverter.cs`

**Interfaces:**
- Produces: `BoolToFontWeightConverter` (key `BoolToFontWeight`), `ColourTokenToBrushConverter` (key `ColourTokenToBrush`) — consumed by Task 4 (XAML resources).

- [ ] **Step 1: Create `BoolToFontWeightConverter`**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/Converters/BoolToFontWeightConverter.cs
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Erdmier.ZooTycoonLauncher.Desktop.Converters;

/// <summary>Converts a <see cref="bool" /> to a <see cref="FontWeight" />: <see langword="true" /> → <see cref="FontWeight.Bold" />, <see langword="false" /> → <see cref="FontWeight.Normal" />.</summary>
public sealed class BoolToFontWeightConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? FontWeight.Bold : FontWeight.Normal;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

- [ ] **Step 2: Create `ColourTokenToBrushConverter`**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/Converters/ColourTokenToBrushConverter.cs
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Erdmier.ZooTycoonLauncher.Desktop.Converters;

/// <summary>
///     Converts a colour token string to an <see cref="IBrush" />. Recognised tokens: <c>Green</c> → <see cref="Brushes.Green" />;
///     any other value → <see cref="Brushes.Red" />.
/// </summary>
public sealed class ColourTokenToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is "Green" ? Brushes.Green : Brushes.Red;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

- [ ] **Step 3: Build**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Converters/BoolToFontWeightConverter.cs
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Converters/ColourTokenToBrushConverter.cs
git commit -m "feat(✨): add BoolToFontWeightConverter and ColourTokenToBrushConverter"
```

---

## Task 3: `InstallationGridViewModel`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Common/InstallationGridViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: `GetAllInstallationsQuery` (Application.Installations.GetAll), `InstallationSummary`, `InstallationGridRowModel` (Task 1), `IMessenger` (CommunityToolkit.Mvvm.Messaging), `IMediator`, four messaging records (`InstallationAddedMessage`, `InstallationChangedMessage`, `InstallationDeletedMessage`, `DefaultInstallationChangedMessage`).
- Produces:
  - `InstallationGridViewModel(IMediator, IMessenger)` constructor
  - `ObservableCollection<InstallationGridRowModel> Rows { get; }`
  - `InstallationGridRowModel? SelectedRow { get; set; }` (`[ObservableProperty]`)
  - `bool HasSelection { get; }` (notified when `SelectedRow` changes)
  - `bool IsSelectionInvalid { get; }` (notified when `SelectedRow` changes)
  - `Task LoadAsync(CancellationToken cancellationToken = default)`
  - `IDisposable.Dispose()`

- [ ] **Step 1: Update `GlobalUsings.cs`**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`. Add these lines after the existing `CommunityToolkit.Mvvm.Input` line and after the Application usings block:

```csharp
global using CommunityToolkit.Mvvm.Messaging;

global using Erdmier.ZooTycoonLauncher.Application.Common.Messaging;
global using Erdmier.ZooTycoonLauncher.Application.Installations.GetAll;
global using Erdmier.ZooTycoonLauncher.Application.Installations.SetDefault;
global using Erdmier.ZooTycoonLauncher.Desktop.Models;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common;
```

The complete file after edits:

```csharp
global using Avalonia;
global using Avalonia.Controls;
global using Avalonia.Interactivity;
global using Avalonia.Markup.Xaml;
global using Avalonia.Threading;

global using Classic.Avalonia.Theme;

global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using CommunityToolkit.Mvvm.Messaging;

global using ErrorOr;

global using Mediator;

global using Erdmier.ZooTycoonLauncher.Application.Common.Extensions;
global using Erdmier.ZooTycoonLauncher.Application.Common.Messaging;
global using Erdmier.ZooTycoonLauncher.Application.Common.Models;
global using Erdmier.ZooTycoonLauncher.Application.Game.Launch;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Add;
global using Erdmier.ZooTycoonLauncher.Application.Installations.GetAll;
global using Erdmier.ZooTycoonLauncher.Application.Installations.SetDefault;
global using Erdmier.ZooTycoonLauncher.Domain.Installations;
global using Erdmier.ZooTycoonLauncher.Desktop.Composition;
global using Erdmier.ZooTycoonLauncher.Desktop.Models;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;
global using Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Common.Extensions;

global using JetBrains.Annotations;

global using Microsoft.Extensions.DependencyInjection;
```

- [ ] **Step 2: Create `InstallationGridViewModel`**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Common/InstallationGridViewModel.cs
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common;

/// <summary>
///     Shared view model for the installation <c>DataGrid</c> (SDD §9.4, §9.6). Owns the row collection, selected row, and data loading via
///     <see cref="GetAllInstallationsQuery" />. Subscribes to installation messenger messages so the grid refreshes automatically once the Application
///     handlers begin publishing them.
/// </summary>
public sealed partial class InstallationGridViewModel : ViewModelBase,
                                                        IDisposable,
                                                        IRecipient<InstallationAddedMessage>,
                                                        IRecipient<InstallationChangedMessage>,
                                                        IRecipient<InstallationDeletedMessage>,
                                                        IRecipient<DefaultInstallationChangedMessage>
{
    private readonly IMediator _mediator;

    private readonly IMessenger _messenger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher — used to issue <see cref="GetAllInstallationsQuery" />.</param>
    /// <param name="messenger">The CommunityToolkit messenger — used to subscribe to installation-change notifications.</param>
    public InstallationGridViewModel(IMediator mediator, IMessenger messenger)
    {
        _mediator  = mediator;
        _messenger = messenger;

        _messenger.RegisterAll(this);
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public InstallationGridViewModel()
        : this(null!, WeakReferenceMessenger.Default)
    { }

    /// <summary>The rows bound to the DataGrid. Rebuilt wholesale on every <see cref="LoadAsync" /> call.</summary>
    public ObservableCollection<InstallationGridRowModel> Rows { get; } = [];

    /// <summary>The currently selected row, or <see langword="null" /> when no row is selected.</summary>
    [ ObservableProperty ]
    [ NotifyPropertyChangedFor(nameof(HasSelection)) ]
    [ NotifyPropertyChangedFor(nameof(IsSelectionInvalid)) ]
    public partial InstallationGridRowModel? SelectedRow { get; set; }

    /// <summary><see langword="true" /> when a row is selected. Consumed by host VM <c>CanExecute</c> guards.</summary>
    public bool HasSelection => SelectedRow is not null;

    /// <summary><see langword="true" /> when the selected row's validity is not <c>Valid</c>. Guards the <c>Fix</c> command in the Installation Manager.</summary>
    public bool IsSelectionInvalid => SelectedRow?.ValidityColourToken == "Red";

    /// <summary>
    ///     Loads all registered installations from the database, projects them to <see cref="InstallationGridRowModel" />, sorts them (default first, then
    ///     alphabetical), and replaces <see cref="Rows" />.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_mediator is null)
        {
            return;
        }

        ErrorOr<IReadOnlyList<InstallationSummary>> result = await _mediator.Send(new GetAllInstallationsQuery(), cancellationToken);

        if (result.IsError)
        {
            return;
        }

        IEnumerable<InstallationGridRowModel> sorted = result.Value
                                                             .Select(s => new InstallationGridRowModel(s.Id,
                                                                                                       s.Name,
                                                                                                       s.Path,
                                                                                                       s.Validity.DisplayName,
                                                                                                       s.Validity.ColourToken,
                                                                                                       s.IsDefault))
                                                             .Order(new InstallationGridRowComparer());

        Rows.Clear();

        foreach (InstallationGridRowModel row in sorted)
        {
            Rows.Add(row);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _messenger.UnregisterAll(this);

    void IRecipient<InstallationAddedMessage>.Receive(InstallationAddedMessage message)
        => _ = LoadAsync();

    void IRecipient<InstallationChangedMessage>.Receive(InstallationChangedMessage message)
        => _ = LoadAsync();

    void IRecipient<InstallationDeletedMessage>.Receive(InstallationDeletedMessage message)
        => _ = LoadAsync();

    void IRecipient<DefaultInstallationChangedMessage>.Receive(DefaultInstallationChangedMessage message)
        => _ = LoadAsync();
}

// Sort: default row first, then alphabetical case-insensitive by Name.
file sealed class InstallationGridRowComparer : IComparer<InstallationGridRowModel>
{
    public int Compare(InstallationGridRowModel? x, InstallationGridRowModel? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return 1;
        if (y is null) return -1;

        if (x.IsDefault != y.IsDefault)
        {
            return x.IsDefault ? -1 : 1;
        }

        return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 3: Register `IMessenger` and `InstallationGridViewModel` in DI**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs`. Replace the method body:

```csharp
public static IServiceCollection AddDesktop(this IServiceCollection services)
{
    services.AddSingleton<IApplicationLifecycle, AvaloniaApplicationLifecycle>();
    services.AddSingleton<IDialogService>(sp => new AvaloniaDialogService(sp));
    services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
    services.AddTransient<InstallationGridViewModel>();
    services.AddTransient<MainWindowViewModel>();
    services.AddTransient<AddInstallationDialogViewModel>();
    services.AddTransient<InstallationManagerDialogViewModel>();

    return services;
}
```

`InstallationManagerDialogViewModel` does not exist yet — add the registration here now so it compiles once Task 6 creates the class.

Wait — this will cause a compile error until Task 6 creates `InstallationManagerDialogViewModel`. To keep each task independently buildable, add only the registrations whose types exist after this task:

```csharp
public static IServiceCollection AddDesktop(this IServiceCollection services)
{
    services.AddSingleton<IApplicationLifecycle, AvaloniaApplicationLifecycle>();
    services.AddSingleton<IDialogService>(sp => new AvaloniaDialogService(sp));
    services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
    services.AddTransient<InstallationGridViewModel>();
    services.AddTransient<MainWindowViewModel>();
    services.AddTransient<AddInstallationDialogViewModel>();

    return services;
}
```

The `InstallationManagerDialogViewModel` registration is added in Task 6.

- [ ] **Step 4: Build**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Common/InstallationGridViewModel.cs
git add Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs
git commit -m "feat(✨): add InstallationGridViewModel with data loading and messenger subscriptions"
```

---

## Task 4: `InstallationGridView`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Common/InstallationGridView.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Common/InstallationGridView.axaml.cs`

**Interfaces:**
- Consumes: `InstallationGridViewModel` (Task 3), `BoolToFontWeightConverter` (Task 2), `ColourTokenToBrushConverter` (Task 2), `InstallationGridRowModel` (Task 1).
- Produces: `InstallationGridView` UserControl — consumed by Tasks 5 and 6 via the `common:` XML namespace.

- [ ] **Step 1: Create code-behind**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Common/InstallationGridView.axaml.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Common;

/// <summary>Code-behind for <c>InstallationGridView.axaml</c>.</summary>
public partial class InstallationGridView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public InstallationGridView() => InitializeComponent();
}
```

- [ ] **Step 2: Create XAML**

```xml
<!-- Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Common/InstallationGridView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common"
             xmlns:converters="using:Erdmier.ZooTycoonLauncher.Desktop.Converters"
             xmlns:models="using:Erdmier.ZooTycoonLauncher.Desktop.Models"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Common.InstallationGridView"
             x:DataType="vm:InstallationGridViewModel">
    <UserControl.Resources>
        <converters:BoolToFontWeightConverter x:Key="BoolToFontWeight" />
        <converters:ColourTokenToBrushConverter x:Key="ColourTokenToBrush" />
    </UserControl.Resources>

    <DataGrid ItemsSource="{Binding Rows}"
              SelectedItem="{Binding SelectedRow, Mode=TwoWay}"
              SelectionMode="Single"
              IsReadOnly="True"
              CanUserReorderColumns="False"
              CanUserResizeColumns="True"
              CanUserSortColumns="False"
              AutoGenerateColumns="False">
        <DataGrid.Columns>
            <DataGridTemplateColumn Header="Name"
                                    Width="*">
                <DataGridTemplateColumn.CellTemplate>
                    <DataTemplate DataType="models:InstallationGridRowModel">
                        <TextBlock Text="{Binding Name}"
                                   FontWeight="{Binding IsDefault, Converter={StaticResource BoolToFontWeight}}"
                                   VerticalAlignment="Center"
                                   Margin="4 0" />
                    </DataTemplate>
                </DataGridTemplateColumn.CellTemplate>
            </DataGridTemplateColumn>

            <DataGridTextColumn Header="Path"
                                Binding="{Binding Path}"
                                Width="2*" />

            <DataGridTemplateColumn Header="Status"
                                    Width="Auto">
                <DataGridTemplateColumn.CellTemplate>
                    <DataTemplate DataType="models:InstallationGridRowModel">
                        <TextBlock Text="{Binding ValidityDisplayName}"
                                   Foreground="{Binding ValidityColourToken, Converter={StaticResource ColourTokenToBrush}}"
                                   VerticalAlignment="Center"
                                   Margin="4 0" />
                    </DataTemplate>
                </DataGridTemplateColumn.CellTemplate>
            </DataGridTemplateColumn>
        </DataGrid.Columns>
    </DataGrid>
</UserControl>
```

- [ ] **Step 3: Build**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Common/InstallationGridView.axaml
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Common/InstallationGridView.axaml.cs
git commit -m "feat(✨): add InstallationGridView DataGrid UserControl"
```

---

## Task 5: Wire Grid into `OpenGameInstallationViewModel` + `OpenGameInstallationView`

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/OpenGameInstallationViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/OpenGameInstallationView.axaml`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs`

**Interfaces:**
- Consumes: `InstallationGridViewModel.HasSelection`, `InstallationGridViewModel.LoadAsync`, `SetDefaultInstallationCommand`, `InstallationGridRowModel.Id`.
- Produces: `OpenGameInstallationViewModel(InstallationGridViewModel, IMediator, IDialogService, Func<CancellationToken,Task>)` constructor — consumed by `MainWindowViewModel.RouteResult`.

- [ ] **Step 1: Rewrite `OpenGameInstallationViewModel`**

Replace the entire file content:

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/OpenGameInstallationViewModel.cs
using System.ComponentModel;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>
///     View model for the <c>OpenGameInstallation</c> state — shown when <c>LauncherSettings.StartupPreference</c> is <c>Ask</c> (SDD §9.6). Hosts an
///     <see cref="InstallationGridViewModel" /> and exposes the picker action commands (<c>Open</c>, <c>Add</c>, <c>Info</c>, <c>Manage</c>).
/// </summary>
public sealed partial class OpenGameInstallationViewModel : ViewModelBase, IDisposable
{
    private readonly IDialogService? _dialogs;

    private readonly IMediator? _mediator;

    private readonly Func<CancellationToken, Task>? _rebootAsync;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="grid">The shared installation grid view model.</param>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">The dialogue service.</param>
    /// <param name="rebootAsync">Callback that re-issues <c>BootCommand</c> on the main window.</param>
    public OpenGameInstallationViewModel(InstallationGridViewModel      grid,
                                         IMediator                      mediator,
                                         IDialogService                 dialogs,
                                         Func<CancellationToken, Task>  rebootAsync)
    {
        Grid        = grid;
        _mediator   = mediator;
        _dialogs    = dialogs;
        _rebootAsync = rebootAsync;

        Grid.PropertyChanged += OnGridPropertyChanged;

        _ = Grid.LoadAsync();
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public OpenGameInstallationViewModel()
        : this(new InstallationGridViewModel(), null!, null!, null!)
    { }

    /// <summary>The shared installation grid view model. Bound to <c>InstallationGridView.DataContext</c>.</summary>
    public InstallationGridViewModel Grid { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Grid.PropertyChanged -= OnGridPropertyChanged;
        Grid.Dispose();
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteSelectionCommand)) ]
    private async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (_mediator is null
            || _rebootAsync is null
            || Grid.SelectedRow is null)
        {
            return;
        }

        // Set the selected installation as default so BootHandler picks it up, then re-boot.
        await _mediator.Send(new SetDefaultInstallationCommand(Grid.SelectedRow.Id), cancellationToken);

        await _rebootAsync(cancellationToken);
    }

    [ RelayCommand ]
    private async Task AddAsync(CancellationToken cancellationToken)
    {
        if (_dialogs is null
            || _rebootAsync is null)
        {
            return;
        }

        AddInstallationResult? result = await _dialogs.ShowAddInstallationAsync(prefilledPath: null);

        if (result is not null)
        {
            await _rebootAsync(cancellationToken);
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteSelectionCommand)) ]
    private Task InfoAsync(CancellationToken cancellationToken)
        => Task.CompletedTask; // TODO: SDD §7.2.4 — Installation Info dialogue not yet implemented.

    [ RelayCommand ]
    private Task ManageAsync(CancellationToken cancellationToken)
        => Task.CompletedTask; // TODO: wired in Task 7 once IDialogService.ShowInstallationManagerAsync exists.

    private bool CanExecuteSelectionCommand() => Grid.HasSelection;

    private void OnGridPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(InstallationGridViewModel.HasSelection))
        {
            OpenCommand.NotifyCanExecuteChanged();
            InfoCommand.NotifyCanExecuteChanged();
        }
    }
}
```

- [ ] **Step 2: Update `MainWindowViewModel` to inject `InstallationGridViewModel`**

In `MainWindowViewModel.cs`, add a field and update the constructor and `RouteResult`:

Add field after `_mediator`:
```csharp
private readonly InstallationGridViewModel _openInstallationGrid;
```

Update constructor signature and body:
```csharp
public MainWindowViewModel(IMediator                mediator,
                           IApplicationLifecycle    lifecycle,
                           IDialogService           dialogs,
                           InstallationGridViewModel openInstallationGrid)
{
    _mediator             = mediator;
    _lifecycle            = lifecycle;
    _dialogs              = dialogs;
    _openInstallationGrid = openInstallationGrid;
}
```

Update the `OpenGameInstallation` case in `RouteResult`:
```csharp
AppBoot.BootOutcome.OpenGameInstallation => new OpenGameInstallationViewModel(_openInstallationGrid,
                                                                              _mediator,
                                                                              _dialogs,
                                                                              BootAsync),
```

- [ ] **Step 3: Replace `OpenGameInstallationView.axaml`**

Replace the entire file:

```xml
<!-- Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/OpenGameInstallationView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot"
             xmlns:common="using:Erdmier.ZooTycoonLauncher.Desktop.Views.Common"
             xmlns:layout="clr-namespace:Erdmier.ZooTycoonLauncher.Desktop.Views.Layout"
             x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Boot.OpenGameInstallationView"
             x:DataType="vm:OpenGameInstallationViewModel">
    <StackPanel Orientation="Vertical"
                Spacing="10">
        <DockPanel HorizontalSpacing="10">
            <Image DockPanel.Dock="Left"
                   Height="32"
                   Source="/Assets/Win95Icons/foldercatalog16.ico"
                   VerticalAlignment="Center"
                   Width="32" />

            <StackPanel Margin="5 0 0 0"
                        Orientation="Vertical"
                        Spacing="4">
                <TextBlock FontWeight="Solid"
                           Text="Open Game Installation" />

                <TextBlock TextWrapping="Wrap">Your startup preference is set to No Installation. Please choose an installation below and click Open, or add a new one.</TextBlock>
            </StackPanel>
        </DockPanel>

        <common:InstallationGridView DataContext="{Binding Grid}" />

        <DockPanel>
            <Button DockPanel.Dock="Right"
                    Command="{Binding OpenCommand}"
                    Content="Open"
                    IsDefault="True"
                    MinWidth="75" />

            <StackPanel HorizontalAlignment="Left"
                        Orientation="Horizontal"
                        Spacing="4">
                <Button Command="{Binding AddCommand}"
                        Content="Add"
                        MinWidth="75" />

                <Button Command="{Binding InfoCommand}"
                        Content="Info"
                        MinWidth="75" />

                <Button Command="{Binding ManageCommand}"
                        Content="Manage"
                        MinWidth="75" />
            </StackPanel>
        </DockPanel>
    </StackPanel>
</UserControl>
```

- [ ] **Step 4: Build**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Run and verify visually**

```powershell
dotnet run --project Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj
```

Trigger the `OpenGameInstallation` state by setting `LauncherStartupPreference` to `Ask` via the settings, or temporarily by placing a breakpoint. Verify:

- The grid renders with Name / Path / Status columns.
- Default installation row (if any) renders **bold** in the Name column.
- Invalid rows show red Status text; Valid rows show green.
- `Open` and `Info` buttons are disabled when no row is selected; enabled when a row is selected.
- Selecting a row and clicking `Open` re-boots the launcher into the selected installation.
- `Add` opens the Add Installation dialogue as before.

- [ ] **Step 6: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/OpenGameInstallationViewModel.cs
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/OpenGameInstallationView.axaml
git add Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs
git commit -m "feat(✨): wire InstallationGridView into OpenGameInstallationView"
```

---

## Task 6: Scaffold `InstallationManagerDialogViewModel` + `InstallationManagerDialogView`

**Files:**
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/InstallationManagerDialogViewModel.cs`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/InstallationManagerDialogView.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/InstallationManagerDialogView.axaml.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: `InstallationGridViewModel` (Task 3), `IDialogService.ShowAddInstallationAsync`.
- Produces:
  - `InstallationManagerDialogViewModel(InstallationGridViewModel, IDialogService)` constructor
  - `Task InitialiseAsync(CancellationToken)` — called by the dialog service before `ShowDialog`
  - `event EventHandler? CloseRequested` — raised by `CloseCommand`
  - `AddCommand`, `InfoCommand`, `EditCommand`, `DeleteCommand`, `FixCommand`, `CloseCommand`

- [ ] **Step 1: Create `InstallationManagerDialogViewModel`**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/InstallationManagerDialogViewModel.cs
using System.ComponentModel;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     View model for the Installation Manager modal (SDD §7.2.2, §9.4). Hosts <see cref="InstallationGridViewModel" /> and exposes the five management
///     commands. <c>Info</c>, <c>Edit</c>, <c>Delete</c>, and <c>Fix</c> are scaffolded stubs — each will be completed when its corresponding dialogue is
///     implemented.
/// </summary>
public sealed partial class InstallationManagerDialogViewModel : ViewModelBase, IDisposable
{
    private readonly IDialogService? _dialogs;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="grid">The shared installation grid view model.</param>
    /// <param name="dialogs">The dialogue service — used here to open the Add Installation modal.</param>
    public InstallationManagerDialogViewModel(InstallationGridViewModel grid, IDialogService dialogs)
    {
        Grid    = grid;
        _dialogs = dialogs;

        Grid.PropertyChanged += OnGridPropertyChanged;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public InstallationManagerDialogViewModel()
        : this(new InstallationGridViewModel(), null!)
    { }

    /// <summary>The shared installation grid view model. Bound to <c>InstallationGridView.DataContext</c>.</summary>
    public InstallationGridViewModel Grid { get; }

    /// <summary>Loads the installation list. Must be awaited by the dialog service before the window is shown.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task InitialiseAsync(CancellationToken cancellationToken = default)
        => Grid.LoadAsync(cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        Grid.PropertyChanged -= OnGridPropertyChanged;
        Grid.Dispose();
    }

    [ RelayCommand ]
    private async Task AddAsync(CancellationToken cancellationToken)
    {
        if (_dialogs is null)
        {
            return;
        }

        AddInstallationResult? result = await _dialogs.ShowAddInstallationAsync(prefilledPath: null);

        if (result is not null)
        {
            await Grid.LoadAsync(cancellationToken);
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteSelectionCommand)) ]
    private Task InfoAsync(CancellationToken cancellationToken)
        => Task.CompletedTask; // TODO: SDD §7.2.4 — Installation Info dialogue not yet implemented.

    [ RelayCommand(CanExecute = nameof(CanExecuteSelectionCommand)) ]
    private Task EditAsync(CancellationToken cancellationToken)
        => Task.CompletedTask; // TODO: SDD §7.2.3 — Edit Installation dialogue not yet implemented.

    [ RelayCommand(CanExecute = nameof(CanExecuteSelectionCommand)) ]
    private Task DeleteAsync(CancellationToken cancellationToken)
        => Task.CompletedTask; // TODO: SDD §7.2.5 — Delete Installation confirmation not yet implemented.

    [ RelayCommand(CanExecute = nameof(CanExecuteFixCommand)) ]
    private Task FixAsync(CancellationToken cancellationToken)
        => Task.CompletedTask; // TODO: SDD §7.2.6 — Fix Installation dialogue not yet implemented.

    [ RelayCommand ]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private bool CanExecuteSelectionCommand() => Grid.HasSelection;

    private bool CanExecuteFixCommand() => Grid.IsSelectionInvalid;

    private void OnGridPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(InstallationGridViewModel.HasSelection)
                           or nameof(InstallationGridViewModel.IsSelectionInvalid))
        {
            InfoCommand.NotifyCanExecuteChanged();
            EditCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
            FixCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Raised when the dialogue should close.</summary>
    public event EventHandler? CloseRequested;
}
```

- [ ] **Step 2: Create code-behind**

```csharp
// Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/InstallationManagerDialogView.axaml.cs
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>Code-behind for <c>InstallationManagerDialogView.axaml</c>.</summary>
public partial class InstallationManagerDialogView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public InstallationManagerDialogView() => InitializeComponent();
}
```

- [ ] **Step 3: Create XAML**

```xml
<!-- Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/InstallationManagerDialogView.axaml -->
<ClassicWindow xmlns="https://github.com/avaloniaui"
               xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
               xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs"
               xmlns:common="using:Erdmier.ZooTycoonLauncher.Desktop.Views.Common"
               x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs.InstallationManagerDialogView"
               x:DataType="vm:InstallationManagerDialogViewModel"
               Icon="/Assets/Win95Icons/installationoncomputer16.ico"
               Title="Installation Manager"
               Width="560"
               SizeToContent="Height"
               CanResize="False"
               WindowStartupLocation="CenterOwner"
               ShowInTaskbar="False">
    <DockPanel Margin="5">
        <!-- Footer: row count + Close button -->
        <DockPanel DockPanel.Dock="Bottom"
                   Margin="0 5 0 0">
            <Button DockPanel.Dock="Right"
                    Command="{Binding CloseCommand}"
                    Content="Close"
                    IsCancel="True"
                    MinWidth="75" />

            <TextBlock VerticalAlignment="Center"
                       Text="{Binding Grid.Rows.Count, StringFormat='{}{0} installation(s) registered. Double-click a row to open.'}" />
        </DockPanel>

        <!-- Right-side action panel -->
        <StackPanel DockPanel.Dock="Right"
                    Margin="5 0 0 0"
                    Spacing="4"
                    Width="80">
            <Button Command="{Binding AddCommand}"
                    Content="Add"
                    HorizontalAlignment="Stretch" />

            <Button Command="{Binding InfoCommand}"
                    Content="Info"
                    HorizontalAlignment="Stretch" />

            <Button Command="{Binding EditCommand}"
                    Content="Edit"
                    HorizontalAlignment="Stretch" />

            <Button Command="{Binding DeleteCommand}"
                    Content="Delete"
                    HorizontalAlignment="Stretch" />

            <Separator />

            <Button Command="{Binding FixCommand}"
                    Content="Fix"
                    HorizontalAlignment="Stretch" />
        </StackPanel>

        <!-- Grid fills remaining space -->
        <common:InstallationGridView DataContext="{Binding Grid}" />
    </DockPanel>
</ClassicWindow>
```

- [ ] **Step 4: Register `InstallationManagerDialogViewModel` in DI**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs` and add the registration:

```csharp
services.AddTransient<InstallationManagerDialogViewModel>();
```

The complete method:

```csharp
public static IServiceCollection AddDesktop(this IServiceCollection services)
{
    services.AddSingleton<IApplicationLifecycle, AvaloniaApplicationLifecycle>();
    services.AddSingleton<IDialogService>(sp => new AvaloniaDialogService(sp));
    services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
    services.AddTransient<InstallationGridViewModel>();
    services.AddTransient<MainWindowViewModel>();
    services.AddTransient<AddInstallationDialogViewModel>();
    services.AddTransient<InstallationManagerDialogViewModel>();

    return services;
}
```

- [ ] **Step 5: Build**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/InstallationManagerDialogViewModel.cs
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/InstallationManagerDialogView.axaml
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/InstallationManagerDialogView.axaml.cs
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs
git commit -m "feat(✨): scaffold InstallationManagerDialogViewModel and view"
```

---

## Task 7: Wire `IDialogService`, `Manage` Button, and File Menu

**Files:**
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaDialogService.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/OpenGameInstallationViewModel.cs`

**Interfaces:**
- Consumes: `InstallationManagerDialogViewModel.InitialiseAsync`, `InstallationManagerDialogViewModel.CloseRequested`, `InstallationManagerDialogView`.
- Produces: `IDialogService.ShowInstallationManagerAsync()` — consumed by `OpenGameInstallationViewModel.ManageAsync`.

- [ ] **Step 1: Add `ShowInstallationManagerAsync` to `IDialogService`**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs`. Add the method after `ShowAddInstallationAsync`:

```csharp
/// <summary>Opens the modal Installation Manager dialogue (SDD §7.2.2, §9.4).</summary>
Task ShowInstallationManagerAsync();
```

- [ ] **Step 2: Implement in `AvaloniaDialogService`**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaDialogService.cs`. Add the implementation after `ShowAddInstallationAsync`:

```csharp
/// <inheritdoc />
public async Task ShowInstallationManagerAsync()
{
    Window? owner = ResolveOwner();

    if (owner is null)
    {
        return;
    }

    InstallationManagerDialogViewModel vm = _services.GetRequiredService<InstallationManagerDialogViewModel>();

    await vm.InitialiseAsync();

    InstallationManagerDialogView view = new()
    {
        DataContext = vm
    };

    vm.CloseRequested += (_, _) => view.Close();

    await view.ShowDialog(owner);
}
```

- [ ] **Step 3: Wire `ManageAsync` in `OpenGameInstallationViewModel`**

Replace the stub `ManageAsync` method:

```csharp
[ RelayCommand ]
private async Task ManageAsync(CancellationToken cancellationToken)
{
    if (_dialogs is null)
    {
        return;
    }

    await _dialogs.ShowInstallationManagerAsync();

    // Reload the picker grid in case the user added or removed installations from the manager.
    await Grid.LoadAsync(cancellationToken);
}
```

- [ ] **Step 4: Build**

```powershell
dotnet build Erdmier.ZooTycoonLauncher.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Run and verify visually**

```powershell
dotnet run --project Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj
```

From the `OpenGameInstallation` state, click **Manage**. Verify:

- The Installation Manager modal opens titled "Installation Manager".
- The grid shows all registered installations with the same Name / Path / Status columns.
- The default row is **bold**.
- `Info`, `Edit`, `Delete` are disabled when nothing is selected; enabled when a row is selected.
- `Fix` is disabled unless the selected row is invalid.
- `Add` opens the Add Installation dialogue; after saving, the grid refreshes.
- `Close` dismisses the modal.
- The footer shows the correct row count.
- After closing, the picker grid in `OpenGameInstallationView` also reflects any changes.

- [ ] **Step 6: Commit**

```
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaDialogService.cs
git add Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/OpenGameInstallationViewModel.cs
git commit -m "feat(✨): wire IDialogService.ShowInstallationManagerAsync and Manage button"
```

---

## Self-Review Notes

- **Spec coverage:** All nine sections of the design spec are addressed. `InstallationGridRowModel` (§3), converters (§6), `InstallationGridViewModel` (§4), `InstallationGridView` (§5), host integration for both contexts (§7), data flow (§8), and out-of-scope items are documented.
- **Messenger publishing not in scope:** The four messaging messages are defined in the Application layer but the handlers do not yet publish them. `InstallationGridViewModel` subscribes — the subscriptions will activate automatically once publishing is wired in a future plan.
- **`OpenCommand` sets default installation:** `SetDefaultInstallationCommand` is used as a pragmatic proxy for `SwitchInstallationCommand` (SDD §9.6), which is not yet implemented. This is a known gap; no `SwitchInstallationCommand` handler exists in the Application layer.
- **`Info`, `Edit`, `Delete`, `Fix` stubbed:** These commands in `InstallationManagerDialogViewModel` are no-ops pending the corresponding dialogue implementations (SDD §7.2.3–7.2.6).
- **Type consistency:** `InstallationGridRowModel` is referenced by the same name in all tasks. `LoadAsync` has the same signature throughout. `HasSelection` / `IsSelectionInvalid` are read from the exact property names defined in Task 3.
