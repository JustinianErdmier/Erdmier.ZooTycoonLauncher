using Avalonia.Input;

namespace Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig;

/// <summary>
///     View for <see cref="IniSectionViewModel" />. Owns the standard INI row; the row template's handlers mark the row's field as hovered or focused, which
///     drives the editor footer's help line (SDD §9.3.2). Only keyboard focus counts as focused, and a pointer-focused field lets go once its value is set
///     (<see cref="IniFieldFocusPolicy" />). Wired once in the template, so no row hand-rolls it.
/// </summary>
public sealed partial class IniSectionView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public IniSectionView() => AvaloniaXamlLoader.Load(this);

    private void OnRowGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is Control { DataContext: IniFieldViewModel field })
        {
            field.IsFocused = IniFieldFocusPolicy.DrivesFooter(e.NavigationMethod);
        }
    }

    // A toggle's click and a number box's spinner buttons: the value is set, so a pointer-focused field lets go.
    private void OnRowButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control row)
        {
            IniFieldFocusRelease.AfterCommit(row);
        }
    }

    // Enter in a text or number box: the box has committed the typed value on the way up, so a pointer-focused field lets go.
    private void OnRowKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is Control row && IniFieldFocusPolicy.CommitsOnKey(e.Key) && e.Source is TextBox)
        {
            IniFieldFocusRelease.AfterCommit(row);
        }
    }

    private void OnRowLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: IniFieldViewModel field })
        {
            field.IsFocused = false;
        }
    }

    private void OnRowPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Control { DataContext: IniFieldViewModel field })
        {
            field.IsHovered = true;
        }
    }

    private void OnRowPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Control { DataContext: IniFieldViewModel field })
        {
            field.IsHovered = false;
        }
    }
}
