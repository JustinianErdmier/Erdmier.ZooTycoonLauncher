# Installation Flow UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps
> use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire the Add Installation flow end-to-end so a fresh user can move from `NoGameInstallationFound` (or `OpenGameInstallation`) through the Add Installation dialogue into
`ReadyToPlay`, unblocking PR [#3](https://github.com/Erdmier/Erdmier.ZooTycoonLauncher/pull/3) manual testing. Deliberately stub the Installation Manager / Edit / Info / Fix /
Delete / picker-grid surfaces — those land in later slices.

**Architecture:** New `AddInstallationDialogView` + `AddInstallationDialogViewModel` pair in `Desktop/{Views,ViewModels}/Dialogs/`. The two Boot states (
`NoGameInstallationFoundViewModel`, `OpenGameInstallationViewModel`) gain a `RelayCommand` that opens the dialogue via `IDialogService` and re-issues `BootCommand` on success.
`IDialogService` grows two members: `ShowAddInstallationAsync(string? prefilledPath)` (returns the dispatched `AddInstallationResult?`, `null` = cancel) and
`PickFolderAsync(string? startPath)` (folder-picker chrome — kept on the service so view models stay Avalonia-agnostic). The dialogue dispatches `AddInstallationCommand` itself;
the FluentValidation pipeline behaviour catches name/path failures and the handler catches `PathMissing`. Successful dispatch closes the window with the result. After `Show`, the
boot VM invokes the existing `RebootAsync` callback (already wired through `MainWindowViewModel`) to re-route via `BootHandler`.

**Tech stack:** Avalonia 11.3 + Classic.Avalonia, CommunityToolkit.Mvvm source generators, `Mediator` (martinothamar), `ErrorOr`, FluentValidation pipeline behaviour. SDD §7.2.1,
§9.1, §9.5 are authoritative.

---

## File structure (touched and created)

**Create:**

- `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/AddInstallationDialogViewModel.cs` — the dialogue's view model, owns Save/Cancel/Browse commands and dispatches
  `AddInstallationCommand`.
- `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/AddInstallationDialogView.axaml` (+ `.axaml.cs`) — Win95-styled modal with Name TextBox, Folder TextBox + Browse button,
  Default checkbox, error TextBlock, Save and Cancel.

**Modify:**

- `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs` — add `ShowAddInstallationAsync` and `PickFolderAsync`.
- `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaDialogService.cs` — implement them.
- `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/NoGameInstallationFoundViewModel.cs` — `partial`, take `IDialogService` + `rebootAsync`, expose
  `AddInstallationCommand`.
- `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/NoGameInstallationFoundView.axaml` — bind the button to the command, drop `IsEnabled="False"` + stub tooltip.
- `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/OpenGameInstallationViewModel.cs` — same treatment.
- `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/OpenGameInstallationView.axaml` — same treatment.
- `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs` — pass `_dialogs` and the existing `BootAsync` callback through to both boot VMs.
- `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml` (+ `.axaml.cs`) — inherit code-behind from `Classic.Avalonia.Theme.ClassicWindow` and add the caption-button-hiding template-part selectors so the title bar renders with only the close button (mirror of [References/Erdmier.ZooTycoonLauncherRef/Views/MainWindow.axaml:21-32](../../../References/Erdmier.ZooTycoonLauncherRef/Views/MainWindow.axaml)).
- `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs` — add `global using Classic.Avalonia.Theme;` so the code-behind's base-type reference resolves without a local using.

**Out of scope (deliberate stubs will remain "coming soon"):**

- Installation Manager dialogue (§7.2.2, §9.4)
- Edit / Info / Fix dialogues (§7.2.3, §9.5)
- Delete Installation dialogue (§7.2.4, §9.5)
- Picker-grid form of `OpenGameInstallation` (§9.6) — the state shows just an "Add Installation" button in this slice
- Auto-locate trail group box on `NoGameInstallationFound` (§9.1) — already absent; stays absent

---

## Task 1: Extend `IDialogService` with Add Installation and folder picker

**Files:**

- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs`

- [ ] **Step 1: Update the interface**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>Surface for opening Desktop-layer dialogues (modeless or modal). Grows with each new dialogue slice.</summary>
public interface IDialogService
{
    /// <summary>Opens the modeless launch-error window with the supplied message. Owned by <c>MainWindow</c> when available.</summary>
    /// <param name="message">The error message to display verbatim.</param>
    void ShowLaunchError(string message);

    /// <summary>Opens the modal Add Installation dialogue (SDD §7.2.1, §9.5). Returns the dispatched <see cref="AddInstallationResult" /> on Save, or <see langword="null" /> on Cancel.</summary>
    /// <param name="prefilledPath">A candidate path to pre-fill into the Folder input — typically <c>BootResult.LocatedCandidatePath</c>.</param>
    Task<AddInstallationResult?> ShowAddInstallationAsync(string? prefilledPath);

    /// <summary>Opens a native folder picker rooted at the supplied path (or a sensible default when <see langword="null" />) and returns the chosen folder, or <see langword="null" /> when the user cancels.</summary>
    /// <param name="startPath">A directory to start the picker in, when present.</param>
    Task<string?> PickFolderAsync(string? startPath);
}
```

- [ ] **Step 2: Register `AddInstallationResult` namespace in `GlobalUsings.cs`**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs` and add (alphabetical order preserved):

```csharp
global using Erdmier.ZooTycoonLauncher.Application.Installations.Add;
```

- [ ] **Step 3: Build to confirm signatures compile**

Run: `dotnet build Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj`
Expected: FAIL — `AvaloniaDialogService` does not implement the new members. (We fix it in Task 2.)

---

## Task 2: Implement folder picker on `AvaloniaDialogService`

**Files:**

- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaDialogService.cs`

- [ ] **Step 1: Add the folder-picker implementation**

Replace the file's contents with:

```csharp
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;
using Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>The Avalonia-bound implementation of <see cref="IDialogService" />.</summary>
internal sealed class AvaloniaDialogService : IDialogService
{
    private readonly IServiceProvider _services;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="services">The composed service provider, used to resolve dialogue view models with their dependencies.</param>
    public AvaloniaDialogService(IServiceProvider services) => _services = services;

    /// <inheritdoc />
    public void ShowLaunchError(string message)
    {
        LaunchErrorView view = new()
        {
            DataContext = new LaunchErrorViewModel(message),
        };

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            view.Show(desktop.MainWindow);
        }
        else
        {
            view.Show();
        }
    }

    /// <inheritdoc />
    public Task<AddInstallationResult?> ShowAddInstallationAsync(string? prefilledPath)
        => throw new NotImplementedException();   // Wired in Task 5.

    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(string? startPath)
    {
        Window? owner = ResolveOwner();

        if (owner is null)
        {
            return null;
        }

        FolderPickerOpenOptions options = new()
        {
            AllowMultiple = false,
            Title         = "Select Zoo Tycoon installation folder",
        };

        if (!string.IsNullOrWhiteSpace(startPath))
        {
            try
            {
                options.SuggestedStartLocation = await owner.StorageProvider.TryGetFolderFromPathAsync(startPath);
            }
            catch (Exception)
            {
                // Suggested start is best-effort; fall back to the picker's default.
            }
        }

        IReadOnlyList<IStorageFolder> chosen = await owner.StorageProvider.OpenFolderPickerAsync(options);

        return chosen.Count == 0 ? null : chosen[0].TryGetLocalPath();
    }

    private static Window? ResolveOwner()
        => Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}
```

- [ ] **Step 2: Update `DesktopServiceCollectionExtensions` to inject the provider**

Open `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs` and replace the contents with:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>Composition-root extensions that register every Desktop view model into a service collection.</summary>
public static class DesktopServiceCollectionExtensions
{
    /// <summary>Registers Desktop view models.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddDesktop(this IServiceCollection services)
    {
        services.AddSingleton<IApplicationLifecycle, AvaloniaApplicationLifecycle>();
        services.AddSingleton<IDialogService>(sp => new AvaloniaDialogService(sp));
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<AddInstallationDialogViewModel>();

        return services;
    }
}
```

- [ ] **Step 3: Build — expect a missing-type error**

Run: `dotnet build Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj`
Expected: FAIL with `The type or namespace name 'AddInstallationDialogViewModel' could not be found`. We fix that in Task 3.

---

## Task 3: Create `AddInstallationDialogViewModel`

**Files:**

- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/AddInstallationDialogViewModel.cs`

- [ ] **Step 1: Create the file**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>The view model for the Add Installation modal (SDD §7.2.1, §9.5). Owns Name / Folder / Default inputs and dispatches <see cref="AddInstallationCommand" /> on Save. Raises <see cref="CloseRequested" /> with the dispatched result on success, or <see langword="null" /> when the user cancels.</summary>
public sealed partial class AddInstallationDialogViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;

    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">The dialogue service — used here only for the folder picker.</param>
    public AddInstallationDialogViewModel(IMediator mediator, IDialogService dialogs)
    {
        _mediator = mediator;
        _dialogs  = dialogs;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public AddInstallationDialogViewModel()
        : this(mediator: null!, dialogs: null!)
    { }

    /// <summary>Raised when the dialogue should close. Argument is the dispatched <see cref="AddInstallationResult" /> on Save, or <see langword="null" /> on Cancel.</summary>
    public event EventHandler<AddInstallationResult?>? CloseRequested;

    /// <summary>The trimmed user-visible installation name. Bound to the Name TextBox.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>The folder path containing <c>zoo.exe</c>. Bound to the Folder TextBox.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    public partial string Path { get; set; } = string.Empty;

    /// <summary>Marks the new installation as the launcher default. Bound to the Default checkbox.</summary>
    [ ObservableProperty ]
    public partial bool MakeDefault { get; set; }

    /// <summary>The most recent validation or dispatch error description, or <see langword="null" /> when none. Bound to the error TextBlock.</summary>
    [ ObservableProperty ]
    public partial string? ErrorMessage { get; set; }

    /// <summary><see langword="true" /> while a dispatch is in flight.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary>Sets the initial folder when the dialogue is being opened with a discovered candidate.</summary>
    /// <param name="prefilledPath">The candidate path to pre-fill, or <see langword="null" />.</param>
    public void PrefillPath(string? prefilledPath)
    {
        if (!string.IsNullOrWhiteSpace(prefilledPath))
        {
            Path = prefilledPath;
        }
    }

    [ RelayCommand ]
    private async Task BrowseAsync()
    {
        string? chosen = await _dialogs.PickFolderAsync(string.IsNullOrWhiteSpace(Path) ? null : Path);

        if (!string.IsNullOrWhiteSpace(chosen))
        {
            Path = chosen;
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteSave)) ]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (_mediator is null)
        {
            return;
        }

        IsBusy       = true;
        ErrorMessage = null;

        try
        {
            ErrorOr<AddInstallationResult> result =
                await _mediator.Send(new AddInstallationCommand(Name.Trim(), Path.Trim(), MakeDefault), cancellationToken);

            if (result.IsError)
            {
                ErrorMessage = result.FirstError.Description;
                return;
            }

            CloseRequested?.Invoke(this, result.Value);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ RelayCommand ]
    private void Cancel() => CloseRequested?.Invoke(this, null);

    private bool CanExecuteSave()
        => !IsBusy
        && !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(Path)
        && _mediator is not null;
}
```

- [ ] **Step 2: Build — expect a missing-namespace error**

Run: `dotnet build Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj`
Expected: FAIL — `AvaloniaDialogService.ShowAddInstallationAsync` still throws; XAML view missing. We fix in Tasks 4 and 5.

---

## Task 4: Create `AddInstallationDialogView`

**Files:**

- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/AddInstallationDialogView.axaml`
- Create: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/AddInstallationDialogView.axaml.cs`

- [ ] **Step 1: Create the XAML**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs"
        x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs.AddInstallationDialogView"
        x:DataType="vm:AddInstallationDialogViewModel"
        Title="Add Installation"
        Width="420"
        SizeToContent="Height"
        CanResize="False"
        WindowStartupLocation="CenterOwner"
        ShowInTaskbar="False">
    <StackPanel Margin="12"
                Spacing="10">
        <TextBlock Text="Register a Zoo Tycoon installation with the launcher."
                   TextWrapping="Wrap" />

        <StackPanel Spacing="4">
            <TextBlock Text="Name" />
            <TextBox Text="{Binding Name, Mode=TwoWay}" />
        </StackPanel>

        <StackPanel Spacing="4">
            <TextBlock Text="Folder" />
            <DockPanel LastChildFill="True">
                <Button DockPanel.Dock="Right"
                        Content="Browse…"
                        Command="{Binding BrowseCommand}"
                        Margin="6,0,0,0"
                        MinWidth="80" />
                <TextBox Text="{Binding Path, Mode=TwoWay}" />
            </DockPanel>
        </StackPanel>

        <CheckBox Content="Mark as default installation"
                  IsChecked="{Binding MakeDefault, Mode=TwoWay}" />

        <TextBlock Text="{Binding ErrorMessage}"
                   Foreground="Red"
                   TextWrapping="Wrap"
                   IsVisible="{Binding ErrorMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />

        <StackPanel Orientation="Horizontal"
                    HorizontalAlignment="Right"
                    Spacing="8"
                    Margin="0,4,0,0">
            <Button Content="Save"
                    Command="{Binding SaveCommand}"
                    IsDefault="True"
                    MinWidth="80" />
            <Button Content="Cancel"
                    Command="{Binding CancelCommand}"
                    IsCancel="True"
                    MinWidth="80" />
        </StackPanel>
    </StackPanel>
</Window>
```

- [ ] **Step 2: Create the code-behind**

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>The Win95-styled modal Add Installation dialogue. Closes itself when the view model raises <c>CloseRequested</c>, carrying the dispatched <see cref="AddInstallationResult" /> or <see langword="null" /> on cancel.</summary>
public sealed partial class AddInstallationDialogView : Window
{
    /// <summary>Initialises a new instance.</summary>
    public AddInstallationDialogView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is AddInstallationDialogViewModel vm)
        {
            vm.CloseRequested -= OnCloseRequested;
            vm.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, AddInstallationResult? result) => Close(result);
}
```

- [ ] **Step 3: Add `using` to `GlobalUsings.cs` if needed**

Confirm `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs` already covers `Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs`. If not, add it. (Check first — the Launch
error pair already lives in that namespace.)

- [ ] **Step 4: Build to verify that the XAML compiles**

Run: `dotnet build Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj`
Expected: `AvaloniaDialogService.ShowAddInstallationAsync` still throws `NotImplementedException`, but everything else compiles.

---

## Task 5: Implement `AvaloniaDialogService.ShowAddInstallationAsync`

**Files:**

- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaDialogService.cs`

- [ ] **Step 1: Replace the throwing stub**

Replace the `ShowAddInstallationAsync` body so the method becomes:

```csharp
/// <inheritdoc />
public async Task<AddInstallationResult?> ShowAddInstallationAsync(string? prefilledPath)
{
    Window? owner = ResolveOwner();

    if (owner is null)
    {
        return null;
    }

    AddInstallationDialogViewModel vm = _services.GetRequiredService<AddInstallationDialogViewModel>();

    vm.PrefillPath(prefilledPath);

    AddInstallationDialogView view = new()
    {
        DataContext = vm,
    };

    return await view.ShowDialog<AddInstallationResult?>(owner);
}
```

- [ ] **Step 2: Build the whole solution**

Run: `dotnet build Erdmier.ZooTycoonLauncher.slnx`
Expected: PASS. No tests run yet.

- [ ] **Step 3: Commit the dialogue + service plumbing**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/IDialogService.cs `
        Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/AvaloniaDialogService.cs `
        Source/Erdmier.ZooTycoonLauncher.Desktop/Composition/DesktopServiceCollectionExtensions.cs `
        Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs `
        Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Dialogs/AddInstallationDialogViewModel.cs `
        Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/AddInstallationDialogView.axaml `
        Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Dialogs/AddInstallationDialogView.axaml.cs

git commit -m "feat(✨): add Add Installation dialogue with folder picker"
```

---

## Task 6: Wire `NoGameInstallationFoundViewModel` to the dialogue

**Files:**

- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/NoGameInstallationFoundViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/NoGameInstallationFoundView.axaml`

- [ ] **Step 1: Rewrite the view model**

Replace the file with:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>The view model for the NoGameInstallationFound state. Optionally surfaces a candidate path the locator found and owns the Add Installation command (SDD §9.1).</summary>
public sealed partial class NoGameInstallationFoundViewModel : ViewModelBase
{
    private readonly IDialogService? _dialogs;

    private readonly Func<CancellationToken, Task>? _rebootAsync;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="locatedCandidatePath">The path discovered by <c>IInstallationLocator</c>, or <see langword="null" /> when nothing was found.</param>
    /// <param name="dialogs">The dialogue service used to open the Add Installation modal.</param>
    /// <param name="rebootAsync">Callback that re-issues <c>BootCommand</c> on the main window after a successful add.</param>
    public NoGameInstallationFoundViewModel(string? locatedCandidatePath, IDialogService dialogs, Func<CancellationToken, Task> rebootAsync)
    {
        LocatedCandidatePath = locatedCandidatePath;
        _dialogs             = dialogs;
        _rebootAsync         = rebootAsync;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public NoGameInstallationFoundViewModel()
        : this(locatedCandidatePath: @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
               dialogs: null!,
               rebootAsync: null!)
    { }

    /// <summary><see langword="true" /> when a candidate path was discovered.</summary>
    public bool HasLocatedPath => LocatedCandidatePath is not null;

    /// <summary>Path discovered by the auto-locate scan, or <see langword="null" />.</summary>
    public string? LocatedCandidatePath { get; }

    [ RelayCommand ]
    private async Task AddInstallationAsync(CancellationToken cancellationToken)
    {
        if (_dialogs is null || _rebootAsync is null)
        {
            return;
        }

        AddInstallationResult? result = await _dialogs.ShowAddInstallationAsync(LocatedCandidatePath);

        if (result is not null)
        {
            await _rebootAsync(cancellationToken);
        }
    }
}
```

- [ ] **Step 2: Enable the button in the XAML**

Replace the `<Button …>` block in `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/NoGameInstallationFoundView.axaml` with:

```xml
        <Button Content="Add Installation"
                Command="{Binding AddInstallationCommand}" />
```

(Drop the `IsEnabled="False"` and the `ToolTip.Tip` line entirely.)

- [ ] **Step 3: Build**

Run: `dotnet build Erdmier.ZooTycoonLauncher.slnx`
Expected: PASS — `MainWindowViewModel.RouteResult` still constructs `NoGameInstallationFoundViewModel(result.LocatedCandidatePath)` with one arg; that call now fails. Fix in Task
8. (Plan continues with `OpenGameInstallation` first so each task commits atomically.)

If you see a compiler error pointing at `MainWindowViewModel.RouteResult`, that is expected. Proceed without committing and let Task 8's edit silence it.

---

## Task 7: Wire `OpenGameInstallationViewModel` to the same dialogue

**Files:**

- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/OpenGameInstallationViewModel.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/OpenGameInstallationView.axaml`

- [ ] **Step 1: Rewrite the view model**

Replace the file with:

```csharp
namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>The view model for the OpenGameInstallation state — shown when <c>LauncherSettings.StartupPreference</c> is <c>Ask</c>. The picker grid (SDD §9.6) is deferred; this slice surfaces only the Add Installation entry point.</summary>
public sealed partial class OpenGameInstallationViewModel : ViewModelBase
{
    private readonly IDialogService? _dialogs;

    private readonly Func<CancellationToken, Task>? _rebootAsync;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="dialogs">The dialogue service used to open the Add Installation modal.</param>
    /// <param name="rebootAsync">Callback that re-issues <c>BootCommand</c> on the main window after a successful add.</param>
    public OpenGameInstallationViewModel(IDialogService dialogs, Func<CancellationToken, Task> rebootAsync)
    {
        _dialogs     = dialogs;
        _rebootAsync = rebootAsync;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public OpenGameInstallationViewModel()
        : this(dialogs: null!, rebootAsync: null!)
    { }

    [ RelayCommand ]
    private async Task AddInstallationAsync(CancellationToken cancellationToken)
    {
        if (_dialogs is null || _rebootAsync is null)
        {
            return;
        }

        AddInstallationResult? result = await _dialogs.ShowAddInstallationAsync(prefilledPath: null);

        if (result is not null)
        {
            await _rebootAsync(cancellationToken);
        }
    }
}
```

- [ ] **Step 2: Enable the button in the XAML**

Replace the `<Button …>` block in `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/OpenGameInstallationView.axaml` with:

```xml
        <Button Content="Add / Pick Installation"
                Command="{Binding AddInstallationCommand}" />
```

(Drop the `IsEnabled="False"` and the `ToolTip.Tip` line entirely.)

- [ ] **Step 3: Don't build yet — proceed to Task 8 to fix the call sites**

The `MainWindowViewModel.RouteResult` switch still constructs both boot VMs with their old signatures. Fix in Task 8.

---

## Task 8: Update `MainWindowViewModel` to inject dialogues + reboot

**Files:**

- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: Update the `RouteResult` switch**

In `Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs`, replace the body of `RouteResult` with:

```csharp
private ViewModelBase RouteResult(AppBoot.BootResult result)
    => result.Outcome switch
    {
        AppBoot.BootOutcome.ReadyToPlay             => new ReadyToPlayViewModel(result.ActiveInstallation!,
                                                                                rebootAsync: BootAsync,
                                                                                _lifecycle,
                                                                                _dialogs,
                                                                                _mediator),
        AppBoot.BootOutcome.CannotPlay              => new CannotPlayViewModel(result.ActiveInstallation!, _mediator),
        AppBoot.BootOutcome.NoGameInstallationFound => new NoGameInstallationFoundViewModel(result.LocatedCandidatePath,
                                                                                            _dialogs,
                                                                                            rebootAsync: BootAsync),
        AppBoot.BootOutcome.OpenGameInstallation    => new OpenGameInstallationViewModel(_dialogs,
                                                                                         rebootAsync: BootAsync),
        var _                                       => new NoGameInstallationFoundViewModel(locatedCandidatePath: null,
                                                                                            _dialogs,
                                                                                            rebootAsync: BootAsync),
    };
```

- [ ] **Step 2: Also update the `BootAsync` error path**

Inside `BootAsync`, the line:

```csharp
ActiveContent = result.IsError ? new NoGameInstallationFoundViewModel(locatedCandidatePath: null) : RouteResult(result.Value);
```

becomes:

```csharp
ActiveContent = result.IsError
    ? new NoGameInstallationFoundViewModel(locatedCandidatePath: null, _dialogs, rebootAsync: BootAsync)
    : RouteResult(result.Value);
```

- [ ] **Step 3: Build the full solution**

Run: `dotnet build Erdmier.ZooTycoonLauncher.slnx`
Expected: PASS.

- [ ] **Step 4: Run all automated tests**

Run: `dotnet test Erdmier.ZooTycoonLauncher.slnx`
Expected: PASS — all 88 tests still pass. The new view model code has no test project; UI smoke is manual.

- [ ] **Step 5: Commit the boot-state wiring**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/NoGameInstallationFoundViewModel.cs `
        Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/Boot/OpenGameInstallationViewModel.cs `
        Source/Erdmier.ZooTycoonLauncher.Desktop/ViewModels/MainWindowViewModel.cs `
        Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/NoGameInstallationFoundView.axaml `
        Source/Erdmier.ZooTycoonLauncher.Desktop/Views/Boot/OpenGameInstallationView.axaml

git commit -m "feat(✨): wire NoGameInstallationFound and OpenGameInstallation to the Add Installation dialogue"
```

---

## Task 9: Switch `MainWindow` to `ClassicWindow` chrome with close-only caption buttons

The Ref launcher inherits its main window from `Classic.Avalonia.Theme.ClassicWindow` and hides the minimise and maximise caption buttons with template-part selectors — the Classic theme's caption-button template ignores `CanMinimize` / `CanMaximize` at the property level on its own (Ref: [References/Erdmier.ZooTycoonLauncherRef/Views/MainWindow.axaml:21-32](../../../References/Erdmier.ZooTycoonLauncherRef/Views/MainWindow.axaml)). Mirror that here so `MainWindow` renders with the proper Win95 title bar carrying only a close button.

**Files:**

- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml.cs`
- Modify: `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml`

- [ ] **Step 1: Add the `Classic.Avalonia.Theme` namespace as a global using**

In `Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs`, add (placing it next to the other `Avalonia.*` global usings, alphabetical order preserved):

```csharp
global using Classic.Avalonia.Theme;
```

- [ ] **Step 2: Switch the code-behind base type to `ClassicWindow`**

In `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml.cs`, change the class declaration so it now reads:

```csharp
public sealed partial class MainWindow : ClassicWindow
```

The rest of the file stays as-is — `ClassicWindow` derives from `Window`, so the existing `OnLoaded(RoutedEventArgs)` override and the `AvaloniaXamlLoader.Load(this)` call continue to compile and work unchanged.

- [ ] **Step 3: Add caption-button hiding to the XAML**

Replace `Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml` with:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Erdmier.ZooTycoonLauncher.Desktop.ViewModels"
        x:Class="Erdmier.ZooTycoonLauncher.Desktop.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="Zoo Tycoon Launcher"
        Width="800"
        Height="600"
        CanMaximize="False"
        CanMinimize="False">

    <!-- The Classic theme's caption-button template doesn't honour CanMinimize / CanMaximize at the property level — only :fullscreen and :dialog:cantresize. -->
    <!-- The template-part selectors below collapse the minimise and restore buttons explicitly so the title bar renders with only a close button. -->
    <Window.Styles>
        <Style Selector="CaptionButtons /template/ Button#PART_MinimizeButton">
            <Setter Property="IsVisible"
                    Value="False" />
        </Style>
        <Style Selector="CaptionButtons /template/ Button#PART_RestoreButton">
            <Setter Property="IsVisible"
                    Value="False" />
        </Style>
    </Window.Styles>

    <ContentControl Content="{Binding ActiveContent}" />
</Window>
```

Note: the XAML root stays `<Window>` even though the code-behind now inherits from `ClassicWindow`. Avalonia's XAML loader accepts a `<Window>` root for partial classes deriving from any `Window` subclass — the same pattern the Ref uses ([References/Erdmier.ZooTycoonLauncherRef/Views/MainWindow.axaml:1](../../../References/Erdmier.ZooTycoonLauncherRef/Views/MainWindow.axaml)).

Note: `CanResize` is left at its default (`True`); only the caption buttons are touched in this task. If a fully fixed-size dialogue feel is wanted later, add `CanResize="False"` in a follow-up — it is a deliberate scope decision, not an oversight.

- [ ] **Step 4: Build the full solution**

Run: `dotnet build Erdmier.ZooTycoonLauncher.slnx`
Expected: PASS.

- [ ] **Step 5: Run all automated tests**

Run: `dotnet test Erdmier.ZooTycoonLauncher.slnx`
Expected: PASS — 88 tests still pass. The architecture cap on `MainWindow.axaml` (≤ 100 lines per CLAUDE.md §Conventions) is well clear at roughly 25 lines.

- [ ] **Step 6: Commit**

```powershell
git add Source/Erdmier.ZooTycoonLauncher.Desktop/GlobalUsings.cs `
        Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml `
        Source/Erdmier.ZooTycoonLauncher.Desktop/Views/MainWindow.axaml.cs

git commit -m "style(🎨): inherit MainWindow from ClassicWindow and hide min/max caption buttons"
```

---

## Task 10: Manual smoke

This slice has no automated tests beyond the existing 88; verify by running the desktop project.

- [ ] **Step 1: Clean local state (optional)**

Either start fresh by removing `%LOCALAPPDATA%\ZooTycoonLauncher\Data\Launcher.db`, or proceed with whatever state is there.

```powershell
Remove-Item "$env:LOCALAPPDATA\ZooTycoonLauncher\Data\Launcher.db" -ErrorAction SilentlyContinue
```

- [ ] **Step 2: Run the launcher**

Run: `dotnet run --project Source/Erdmier.ZooTycoonLauncher.Desktop/Erdmier.ZooTycoonLauncher.Desktop.csproj`
Expected — fresh profile: `NoGameInstallationFound` state appears. If the locator found a candidate path, it's surfaced in the read-only TextBox; the `Add Installation` button is
enabled. The title bar carries Win95-style chrome with **only a close button** (no minimise, no maximise).

- [ ] **Step 3: Click `Add Installation`**

Expected: Modal opens. If a candidate path was surfaced on the boot state, it pre-fills the Folder TextBox.

- [ ] **Step 4: Click `Browse…`**

Expected: The native folder picker appears. Pick the real ZT install folder, click OK. The Folder TextBox now shows that path.

- [ ] **Step 5: Type a Name and click `Save`**

Expected: Dialogue closes. The launcher is re-booted and either:

- Lands on `ReadyToPlay` (the EXE and INI were found) — proceed with the PR #3 smoke (click Launch Game).
- Lands on `CannotPlay` (one of the files is missing) — that's correct routing.

- [ ] **Step 6: Try the unhappy paths**

Quick rotations to confirm the error display:

1. Open the dialogue again. Click `Save` with an empty Name → error TextBlock shows `"Name is required."` (or similar) and dialogue stays open.
2. Type a Name that already exists → `"Another installation already uses this name."`
3. Type a real Name but a Folder that doesn't exist → `"The folder "…" does not exist."`
4. Click `Cancel` at any time → dialogue closes, boot state unchanged.

- [ ] **Step 7: Decide on additional commits**

If anything in the smoke surfaced a necessary tweak, commit it as a follow-up with `fix(🐛):` or `style(🎨):` per CLAUDE.md.

---

## Task 11: Push and update PR [#3](https://github.com/Erdmier/Erdmier.ZooTycoonLauncher/pull/3)

Per the user's standing preference, finishing the branch means pushing and updating the PR — no permission needed (memory: `feedback_branch_completion.md`).

- [ ] **Step 1: Push the branch**

Run: `git push`
Expected: Four new commits land on `feat/launch-game` (dialogue plumbing → boot wiring → MainWindow chrome → any follow-up tweak from the smoke).

- [ ] **Step 2: Update the PR body**

Run:

```powershell
gh pr edit 3 --body "$(cat <<'EOF'
## Summary
- Implements SDD §7.10 (Launch Game). Clicking **Launch Game** on the General tab spawns `zoo.exe` with `UseShellExecute=true` and working-directory set to the install root, matching the Ref launcher's pattern.
- Re-verifies the installation at click time. On drift, persists the new `HasExe`/`HasIni` state and re-issues `BootCommand` so the state machine routes to `CannotPlay` cleanly.
- Stamps `LastPlayedUtc` on success, honours `LauncherSettings.CloseAfterGameLaunch`, and surfaces process-start failures via a modeless Win95-style `LaunchErrorView` window owned by `MainWindow`.
- Implements SDD §7.2.1 / §9.5 (Add Installation dialogue) so the launcher is usable from a clean profile end-to-end. `NoGameInstallationFound` and `OpenGameInstallation` boot states now both open the modal; on Save the launcher re-issues `BootCommand`. Installation Manager, Edit / Info / Fix / Delete dialogues, and the picker grid remain deliberate stubs for later slices.
- `MainWindow` now inherits from `Classic.Avalonia.Theme.ClassicWindow` with the minimise and maximise caption buttons collapsed via template-part selectors, mirroring the Ref launcher's chrome (only the close button shows in the title bar).

## What landed besides the launch slice
- Pre-task: read-only `References/Erdmier.ZooTycoonLauncherRef` excluded from active build (CPM/AXAML errors had it failing silently in headless `dotnet build`).
- Mid-slice fix: `LaunchGameOutcome` split into its own file to satisfy `OneTypePerFileTests`.
- `Microsoft.Extensions.Logging.Abstractions` added as direct dependency.
- `IDialogService` gained `ShowAddInstallationAsync` and `PickFolderAsync`; `AvaloniaDialogService` now resolves dialogue VMs from the composed `IServiceProvider`.
- `Classic.Avalonia.Theme` added as a global using in `Desktop/GlobalUsings.cs` to keep the `ClassicWindow` base reference clean in code-behind.

## Test plan
- [x] All 88 automated tests pass on cold clean build (6 Domain + 47 Application + 22 Infrastructure + 13 Architecture).
- [ ] Manual smoke — title bar chrome: on launch the window's title bar shows only a close button (no minimise, no maximise), and the system menu's `Minimize` / `Maximize` entries are disabled.
- [ ] Manual smoke — fresh profile: delete `%LOCALAPPDATA%\ZooTycoonLauncher\Data\Launcher.db`, `dotnet run` the desktop project, click **Add Installation**, browse to the ZT folder, name it, Save; confirm the state transitions to `ReadyToPlay`, click **Launch Game**, confirm `zoo.exe` starts.
- [ ] Manual smoke — `CloseAfterGameLaunch`: `UPDATE LauncherSettings SET CloseAfterGameLaunch = 1 WHERE Id = 1;` then click Launch; confirm launcher closes after ZT1 starts. Reset flag to 0 afterwards.
- [ ] Manual smoke — drift: rename `zoo.exe` → `zoo.bak` between launches; click Launch; confirm UI transitions to `CannotPlay`.
- [ ] Manual smoke — start failure: apply `icacls "<zoo.exe>" /deny "$env:USERNAME:(RX)"`; click Launch; confirm modeless `LaunchErrorView` appears with the Win32 message. Reset ACL.
- [ ] Manual smoke — dialogue validation: empty Name → `Name is required.`; duplicate Name → uniqueness error; non-existent Folder → `The folder "…" does not exist.`; Cancel → no state change.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

Expected: `gh` prints the updated PR URL.
