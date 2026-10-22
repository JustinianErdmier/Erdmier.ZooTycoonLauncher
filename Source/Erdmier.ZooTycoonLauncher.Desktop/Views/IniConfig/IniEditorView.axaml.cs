namespace Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig;

/// <summary>
///     View for <see cref="IniEditorViewModel" />: the section list, the selected section's form, and the footer's Save, Reset and Restore defaults commands. Clearing focus
///     (a click on the form's empty space, or Esc) lives here, once, rather than in every field (<see cref="IniFieldFocusPolicy" />).
/// </summary>
public sealed partial class IniEditorView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public IniEditorView() => AvaloniaXamlLoader.Load(this);

    // A press that lands on anything but an input (the row label, the gaps, the background) takes focus off the field; presses on inputs and scroll bars are left alone.
    private void OnFormPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control form && e.Source is Visual source && !IsInput(source))
        {
            IniFieldFocusRelease.Clear(form);
        }
    }

    // Esc anywhere in the form (an open drop-down handles its own Esc first and never reaches here).
    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is Control form && IniFieldFocusPolicy.ClearsFocus(e.Key))
        {
            IniFieldFocusRelease.Clear(form);
        }
    }

    private static bool IsInput(Visual source)
        => source.GetSelfAndVisualAncestors().Any(visual => visual is TextBox or ComboBox or NumericUpDown or ToggleButton or ScrollBar);
}
