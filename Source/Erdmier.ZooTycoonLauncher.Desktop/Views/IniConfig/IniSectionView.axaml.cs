using Avalonia.Input;

namespace Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig;

/// <summary>
///     View for <see cref="IniSectionViewModel" />. Owns the standard INI row; the row template's handlers mark the row's field as help-active while it is hovered or focused, which
///     drives the editor footer's help line (SDD §9.3.2). Wired once in the template, so no row hand-rolls it.
/// </summary>
public sealed partial class IniSectionView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public IniSectionView() => AvaloniaXamlLoader.Load(this);

    private static void SetHelpActive(object? sender, bool isActive)
    {
        if (sender is Control { DataContext: IniFieldViewModel field })
        {
            field.IsHelpActive = isActive;
        }
    }

    private void OnRowGotFocus(object? sender, GotFocusEventArgs e) => SetHelpActive(sender, isActive: true);

    private void OnRowLostFocus(object? sender, RoutedEventArgs e) => SetHelpActive(sender, isActive: false);

    private void OnRowPointerEntered(object? sender, PointerEventArgs e) => SetHelpActive(sender, isActive: true);

    private void OnRowPointerExited(object? sender, PointerEventArgs e) => SetHelpActive(sender, isActive: false);
}
