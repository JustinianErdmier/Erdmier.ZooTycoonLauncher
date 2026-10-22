using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.VisualTree;

using Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Views.IniConfig;

/// <summary>
///     Drives the real INI editor with keyboard and pointer input to check that only keyboard focus is lasting (milestone 11.1 §6a.1): a field the pointer focused lets go
///     once its value is set, and a click on empty space or Esc clears focus.
/// </summary>
public sealed class IniFieldFocusTests
{
    /// <summary>A drop-down reached with Tab, opened from the keyboard and confirmed with Enter keeps focus, so the footer keeps showing its help.</summary>
    /// <param name="key">The key that opens the drop-down.</param>
    /// <param name="modifiers">The modifiers held with it.</param>
    [ AvaloniaTheory ]
    [ InlineData(Key.F4, RawInputModifiers.None) ]
    [ InlineData(Key.Down, RawInputModifiers.Alt) ]
    public void KeyboardFocusedDropDown_ConfirmedWithEnter_KeepsFocus(Key key, RawInputModifiers modifiers)
    {
        IniEditorHarness harness = IniEditorHarness.Show();

        IniChoiceFieldViewModel fullscreen = IniEditorTestData.Field<IniChoiceFieldViewModel>(harness.Editor, section: "user", key: "fullscreen");
        ComboBox                comboBox   = harness.Find<ComboBox>(fullscreen);

        harness.TabTo(comboBox);

        fullscreen.IsFocused.ShouldBeTrue();

        harness.Press(key, modifiers);

        comboBox.IsDropDownOpen.ShouldBeTrue();
        fullscreen.IsFocused.ShouldBeTrue("Focus moving into the open drop-down's items must not cost the keyboard user their place.");

        harness.Press(Key.Down);
        harness.Press(Key.Enter);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        fullscreen.SelectedOption.ShouldNotBeNull().Label.ShouldBe("Windowed");
        IniFieldFocusRelease.IsWithin(harness.RowOf(fullscreen), harness.FocusedElement).ShouldBeTrue();
        fullscreen.IsFocused.ShouldBeTrue();
        harness.Editor.IsFooterHelp.ShouldBeTrue();

        harness.Close();
    }

    /// <summary>Picking a drop-down item with the pointer releases focus.</summary>
    [ AvaloniaFact ]
    public void PointerPickedDropDownItem_ReleasesFocus()
    {
        IniEditorHarness harness = IniEditorHarness.Show();

        IniChoiceFieldViewModel fullscreen = IniEditorTestData.Field<IniChoiceFieldViewModel>(harness.Editor, section: "user", key: "fullscreen");
        ComboBox                comboBox   = harness.Find<ComboBox>(fullscreen);

        harness.Click(comboBox);

        comboBox.IsDropDownOpen.ShouldBeTrue();

        harness.Click(comboBox.ContainerFromIndex(index: 1).ShouldNotBeNull());

        comboBox.IsDropDownOpen.ShouldBeFalse();
        fullscreen.SelectedOption.ShouldNotBeNull().Label.ShouldBe("Windowed");
        harness.FocusedElement.ShouldBeNull();
        fullscreen.IsFocused.ShouldBeFalse();

        harness.Close();
    }

    /// <summary>A pointer click that opens a drop-down is not a commit, so the drop-down keeps focus.</summary>
    [ AvaloniaFact ]
    public void PointerOpeningDropDown_KeepsFocus()
    {
        IniEditorHarness harness = IniEditorHarness.Show();

        IniChoiceFieldViewModel fullscreen = IniEditorTestData.Field<IniChoiceFieldViewModel>(harness.Editor, section: "user", key: "fullscreen");
        ComboBox                comboBox   = harness.Find<ComboBox>(fullscreen);

        harness.Click(comboBox);

        comboBox.IsDropDownOpen.ShouldBeTrue();
        IniFieldFocusRelease.IsWithin(harness.RowOf(fullscreen), harness.FocusedElement).ShouldBeTrue();

        // The Classic theme's drop-down does not raise a Click the row hears, so raise the one its internal toggle would: the row's filter must still ignore it.
        harness.Find<ComboBox>(fullscreen).GetVisualDescendants().OfType<ToggleButton>().First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        IniFieldFocusRelease.IsWithin(harness.RowOf(fullscreen), harness.FocusedElement).ShouldBeTrue("A click from inside a drop-down is not a commit.");

        harness.Close();
    }

    /// <summary>A pointer click on a toggle sets its value and releases focus.</summary>
    [ AvaloniaFact ]
    public void PointerClickedToggle_ReleasesFocus()
    {
        IniEditorHarness harness = IniEditorHarness.Show();

        IniToggleFieldViewModel toggle = harness.Editor.Sections.SelectMany(section => section.Fields).OfType<IniToggleFieldViewModel>().First();

        harness.ShowSectionOf(toggle);

        CheckBox checkBox = harness.Find<CheckBox>(toggle);
        bool?    before   = checkBox.IsChecked;

        harness.Click(checkBox);

        checkBox.IsChecked.ShouldNotBe(before);
        harness.FocusedElement.ShouldBeNull();
        toggle.IsFocused.ShouldBeFalse();

        harness.Close();
    }

    /// <summary>A pointer click on a number box's spinner button steps the value and releases focus the pointer gave the box.</summary>
    [ AvaloniaFact ]
    public void PointerClickedSpinnerButton_ReleasesFocus()
    {
        IniEditorHarness harness = IniEditorHarness.Show();

        IniNumberFieldViewModel screenWidth = IniEditorTestData.Field<IniNumberFieldViewModel>(harness.Editor, section: "user", key: "screenwidth");
        Control                 row         = harness.RowOf(screenWidth);

        harness.Click(harness.Find<TextBox>(screenWidth));

        IniFieldFocusRelease.IsWithin(row, harness.FocusedElement).ShouldBeTrue();
        screenWidth.IsFocused.ShouldBeFalse();

        harness.Click(harness.Find<ButtonSpinner>(screenWidth).GetVisualDescendants().OfType<RepeatButton>().First());

        screenWidth.Value.ShouldNotBe(800m);
        harness.FocusedElement.ShouldBeNull();
        screenWidth.IsFocused.ShouldBeFalse();

        harness.Close();
    }

    /// <summary>Esc clears the focus of a keyboard-focused field.</summary>
    [ AvaloniaFact ]
    public void Escape_ClearsFocus()
    {
        IniEditorHarness harness = IniEditorHarness.Show();

        IniChoiceFieldViewModel fullscreen = IniEditorTestData.Field<IniChoiceFieldViewModel>(harness.Editor, section: "user", key: "fullscreen");

        harness.TabTo(harness.Find<ComboBox>(fullscreen));

        harness.Press(Key.Escape);

        harness.FocusedElement.ShouldBeNull();
        fullscreen.IsFocused.ShouldBeFalse();
        harness.Editor.IsFooterHelp.ShouldBeFalse();

        harness.Close();
    }

    /// <summary>A pointer press on empty editor space (a row's label) clears the focus of a keyboard-focused field.</summary>
    [ AvaloniaFact ]
    public void PointerPressOnEmptySpace_ClearsFocus()
    {
        IniEditorHarness harness = IniEditorHarness.Show();

        IniChoiceFieldViewModel fullscreen  = IniEditorTestData.Field<IniChoiceFieldViewModel>(harness.Editor, section: "user", key: "fullscreen");
        IniNumberFieldViewModel screenWidth = IniEditorTestData.Field<IniNumberFieldViewModel>(harness.Editor, section: "user", key: "screenwidth");

        harness.TabTo(harness.Find<ComboBox>(fullscreen));

        harness.Click(harness.Find<TextBlock>(screenWidth));

        harness.FocusedElement.ShouldBeNull();
        fullscreen.IsFocused.ShouldBeFalse();

        harness.Close();
    }

    /// <summary>
    ///     <see cref="IniFieldFocusRelease.IsWithin" /> counts an item in an open drop-down's pop-up as inside the drop-down's row, and an element of another row as outside.
    ///     The headless pop-up is an overlay, the case a visual-only walk misses; a pop-up in its own window (the Windows default) has the same logical parent.
    /// </summary>
    [ AvaloniaFact ]
    public void IsWithin_CountsPopupItemsAsInsideTheirRow()
    {
        IniEditorHarness harness = IniEditorHarness.Show();

        IniChoiceFieldViewModel fullscreen  = IniEditorTestData.Field<IniChoiceFieldViewModel>(harness.Editor, section: "user", key: "fullscreen");
        IniNumberFieldViewModel screenWidth = IniEditorTestData.Field<IniNumberFieldViewModel>(harness.Editor, section: "user", key: "screenwidth");
        ComboBox                comboBox    = harness.Find<ComboBox>(fullscreen);
        Control                 row         = harness.RowOf(fullscreen);

        harness.TabTo(comboBox);
        harness.Press(Key.F4);

        Control item = comboBox.ContainerFromIndex(index: 0).ShouldNotBeNull();

        // The headless platform has no pop-up windows, so the items sit in an overlay on the main window, visually outside the row: only the logical walk finds them.
        item.GetVisualAncestors().ShouldNotContain(row);

        IniFieldFocusRelease.IsWithin(row, item).ShouldBeTrue();
        IniFieldFocusRelease.IsWithin(row, harness.Find<NumericUpDown>(screenWidth)).ShouldBeFalse();
        IniFieldFocusRelease.IsWithin(row, element: null).ShouldBeFalse();

        harness.Close();
    }
}
