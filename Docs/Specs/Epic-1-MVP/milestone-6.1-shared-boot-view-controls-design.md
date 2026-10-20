# Shared Boot-View Controls — Design Spec

**Date:** 2026-07-03  
**Status:** Approved  
**Related SDD sections:** §9.2 (view composition)  

---

## 1. Overview

The initial-loading, no-installation-found, and ready-to-play boot views have converged on two repeated layout patterns. This spec extracts both into reusable Desktop controls and migrates the three views onto them.

- **`HeaderRow`** — the icon + copy + optional action row that headlines a view (and, elsewhere, sits inside a `GroupBox`). A left 32×32 icon, a vertical copy stack (bold title over freeform body), and an optional right-aligned action (usually a `Button`).
- **`EntryPointStack`** — the vertical `StackPanel` (`Spacing="10"`) that hosts the top-level controls of a multi-control view.

Both controls own layout only. They carry no view model, no data loading, and no navigation.

### 1.1 Scope

In scope — migrate these views:

- **`LookingForZooTycoonView`** — single `HeaderRow`; no `EntryPointStack` (a single control needs no stack).
- **`NoGameInstallationFoundView`** — `EntryPointStack` hosting a `HeaderRow` plus the existing candidate-path stack and auto-locate panel.
- **`PlayView`** — **no change**; documented as intentionally exempt (single `TabControl`, same rationale as loading).

Out of scope (deliberate) — `OpenGameInstallationView` and the General-tab Status box use the identical `HeaderRow` pattern and are the obvious next adopters, but are left for a follow-up effort.

---

## 2. Background: `GroupBox` vs `HeaderedContentControl`

Recorded here because it motivates a design boundary. In Classic.Avalonia the `GroupBox` control is *literally* an alias:

```csharp
// Classic.Avalonia.Theme.Controls.GroupBox
public class GroupBox : HeaderedContentControl
{
    protected override Type StyleKeyOverride => typeof(HeaderedContentControl);
}
```

`GroupBox` subclasses `HeaderedContentControl` and overrides `StyleKeyOverride` to reuse the `HeaderedContentControl` `ControlTheme`. The theme defines **one** template (a `ClassicBorderDecorator` with `BorderStyle="Etched"`, `BorderThickness="2"`, a 7px header-notch offset, default `Padding="8"`). Consequences:

- The two controls render **identically** — there is no visual difference to reconcile.
- `Padding` feeds the same `ContentPresenter` in both, so the no-game view's `11 15 15 12` and the General tab's `10 15` differ only by hand-tweaking, not by control type or by the `Grid` in the no-game view. `10 15` would render structurally the same. (The no-game view is left untouched as agreed.)

**Design boundary that follows:** `HeaderRow` is the *inner row only*. It never draws the etched frame. A host that wants the frame wraps `HeaderRow` in a `GroupBox` (as the General tab already does); loading and no-game use `HeaderRow` bare.

---

## 3. File layout

```text
Source/Erdmier.ZooTycoonLauncher.Desktop/
├── Controls/
│   ├── HeaderRow.cs            TemplatedControl subclass (StyledProperties + slots)
│   └── HeaderRow.axaml         ControlTheme ResourceDictionary (no x:Class)
└── Views/Layout/
    └── EntryPointStack.cs      StackPanel subclass, next to the existing UniformStack
```

- `HeaderRow` namespace: `Erdmier.ZooTycoonLauncher.Desktop.Controls`.
- `EntryPointStack` namespace: `Erdmier.ZooTycoonLauncher.Desktop.Views.Layout` (mirrors `UniformStack`).
- `HeaderRow.axaml` is a keyless `ResourceDictionary` holding a single `ControlTheme` keyed `{x:Type controls:HeaderRow}`; it has no code-behind and defines no public type.

### 3.1 Architecture-test compliance

- **`NoFilesAtAssemblyRoot`** — both `.cs` files live under subfolders. ✓
- **`OneTypePerFile`** — each public type (`HeaderRow`, `EntryPointStack`) has a same-named `.cs` file. `HeaderRow.axaml` declares no public type, so it needs no match. ✓
- There is currently **no** enforced View/ViewModel pair test; even so, neither control is a `*View`, so the documented pair convention does not apply to them.

---

## 4. `HeaderRow`

**Namespace:** `Erdmier.ZooTycoonLauncher.Desktop.Controls`  
**Base:** `ContentControl`  
**Kind:** `public class` styled by a `ControlTheme`

### 4.1 Public surface

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Source` | `IImage?` | `null` | The 32×32 icon. Typed `IImage` so Avalonia's built-in converter resolves `"/Assets/…"` **and** Rider offers asset-path completion (parity with `Image.Source`). |
| `Title` | `string?` | `null` | Bold heading text. The template applies `FontWeight="Solid"` (the established weight). Collapsed when `null`. |
| `IconVerticalAlignment` | `VerticalAlignment` | `Top` | Aligns the icon within the row. `Top` for loading; `Center` for no-game. |
| `Action` | `object?` | `null` | Optional right-aligned slot, usually a `Button`. Its presenter is collapsed when `null`. |
| *(content)* | `object?` | — | The `[Content]` body slot: a `TextBlock`, a `ProgressBar` + phrase, a `Grid`, etc. |

`Source`, `Title`, `IconVerticalAlignment`, and `Action` are new `StyledProperty` registrations on `HeaderRow`. `Content` (and its `[Content]` XAML slot) is **inherited from `ContentControl`** — no re-declaration needed; the template's body `ContentPresenter` binds to it.

### 4.2 Template (preserves the current spacing exactly)

```xml
<ControlTheme x:Key="{x:Type controls:HeaderRow}" TargetType="controls:HeaderRow">
    <Setter Property="Template">
        <ControlTemplate>
            <DockPanel HorizontalSpacing="10">
                <Image DockPanel.Dock="Left"
                       Width="32" Height="32"
                       Source="{TemplateBinding Source}"
                       VerticalAlignment="{TemplateBinding IconVerticalAlignment}" />

                <ContentPresenter DockPanel.Dock="Right"
                                  Content="{TemplateBinding Action}"
                                  IsVisible="{Binding Action, RelativeSource={RelativeSource TemplatedParent},
                                                      Converter={x:Static ObjectConverters.IsNotNull}}"
                                  VerticalAlignment="Center" />

                <StackPanel Margin="5 0 0 0" Orientation="Vertical" Spacing="4">
                    <TextBlock FontWeight="Solid"
                               Text="{TemplateBinding Title}"
                               IsVisible="{Binding Title, RelativeSource={RelativeSource TemplatedParent},
                                                   Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
                    <ContentPresenter Content="{TemplateBinding Content}" />
                </StackPanel>
            </DockPanel>
        </ControlTemplate>
    </Setter>
</ControlTheme>
```

The `DockPanel HorizontalSpacing="10"` + copy-stack `Margin="5 0 0 0"` composition is retained verbatim, so migrated views render byte-identically to today.

> Implementation note — visibility of the `Action` presenter: binding `IsVisible` to an `IsNotNull` check keeps the right dock slot from reserving space when no action is supplied. Confirm during implementation that a collapsed right-docked presenter yields the same body width as the current single-child `DockPanel` in loading/open-game.

### 4.3 Subclass base — decision

Three candidate bases:

- **`ContentControl`** (recommended) — already provides `Content`, `ContentTemplate`, the `[Content]` XAML slot, and the `ContentPresenter` wiring, with **no** inherited `Header` or frame semantics. `HeaderRow` adds only `Source`, `Title`, `IconVerticalAlignment`, and `Action`.
- `TemplatedControl` — would force hand-rolling a `Content` property and `[ContentProperty]` attribute for no benefit over `ContentControl`.
- `HeaderedContentControl` — reuses `Content` but drags in `Header` and the Classic etched-frame theme association, which we explicitly do *not* want on the inner row.

Recommendation: subclass `ContentControl`; add the four extra StyledProperties.

---

## 5. `EntryPointStack`

**Namespace:** `Erdmier.ZooTycoonLauncher.Desktop.Views.Layout`  
**Base:** `StackPanel`  
**Kind:** `public sealed class`

```csharp
public sealed class EntryPointStack : StackPanel
{
    static EntryPointStack()
    {
        OrientationProperty.OverrideDefaultValue<EntryPointStack>(Orientation.Vertical);
        SpacingProperty.OverrideDefaultValue<EntryPointStack>(10d);
    }
}
```

`OverrideDefaultValue` changes defaults, not values, so an instance may still set `Spacing`/`Orientation` explicitly. Single-control views (loading, play) do not use it — a lone control needs no host stack.

---

## 6. Registration

Merge the control theme into application resources in `App.axaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://Erdmier.ZooTycoonLauncher.Desktop/Controls/HeaderRow.axaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

`Application.Styles` (Classic theme + data-grid/dock includes) is unchanged. The `ControlTheme` is keyed by type, so it applies implicitly to every `HeaderRow`.

---

## 7. Per-view migration

### 7.1 `LookingForZooTycoonView`

```xml
<controls:HeaderRow Source="/Assets/Win95Icons/searchcomputer16.ico"
                    Title="Looking for Zoo Tycoon…"
                    IconVerticalAlignment="Top">
    <StackPanel Orientation="Vertical" Spacing="4">
        <ProgressBar HorizontalAlignment="Stretch" IsIndeterminate="True" />
        <TextBlock Foreground="Gray" Text="{Binding CurrentPhrase}" />
    </StackPanel>
</controls:HeaderRow>
```

The body carries two elements, so it needs its own inner `Spacing="4"` stack (the `HeaderRow` copy stack spaces *title-to-body*, not the body's internal children).

### 7.2 `NoGameInstallationFoundView`

```xml
<layout:EntryPointStack>
    <controls:HeaderRow Source="/Assets/Win95Icons/error32.gif"
                        Title="No Game Installation Found"
                        IconVerticalAlignment="Center">
        <controls:HeaderRow.Action>
            <Button Command="{Binding AddInstallationCommand}" IsDefault="True">Add Installation</Button>
        </controls:HeaderRow.Action>
        <TextBlock TextWrapping="Wrap">The auto-locate searched the Windows registry, Program Files, and the previously known directory (if applicable), but did not find a zoo.exe file. Add an installation by pointing the launcher at a folder containing the game.</TextBlock>
    </controls:HeaderRow>

    <!-- Candidate-path stack: unchanged -->
    <!-- Auto-locate HeaderedContentControl (Padding="11 15 15 12"): unchanged -->
</layout:EntryPointStack>
```

### 7.3 `PlayView`

No change. A comment records that a single `TabControl` needs neither `HeaderRow` nor `EntryPointStack`.

---

## 8. Testing

- Build clean, then build: `dotnet clean …slnx; dotnet build …slnx`.
- `dotnet test …slnx` — architecture tests (`OneTypePerFile`, `NoFilesAtAssemblyRoot`) must stay green.
- Manual visual check via `dotnet run` on the boot flow: loading and no-game states render pixel-identically to pre-migration; confirm the no-action row (loading) leaves no phantom right-side gap, and Rider offers asset-path completion on `HeaderRow.Source`.

---

## 9. Open questions

None. All decisions resolved during brainstorming:

- Icon-to-copy gap kept as the current `10` (dock) `+ 5` (margin) composition.
- API shape: `Title` string + freeform body slot + optional `Action` slot.
- Implementation: templated control + `ControlTheme`.
- Scope: the three named views only.
- Placement: `Controls/` for `HeaderRow`.
