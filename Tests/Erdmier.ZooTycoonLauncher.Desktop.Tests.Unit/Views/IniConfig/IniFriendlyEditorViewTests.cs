using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Views.IniConfig;

/// <summary>Drives the real INI editor in the Friendly style: the pages render, every row keeps the focus wiring, and a live style switch re-renders the open view.</summary>
public sealed class IniFriendlyEditorViewTests
{
    /// <summary>A Friendly drop-down reached with Tab is focused, so the footer shows its help.</summary>
    [ AvaloniaFact ]
    public void FriendlyDropDown_ReachedWithTab_IsFocused()
    {
        IniEditorHarness harness = IniEditorHarness.Show(IniEditorStyle.Friendly);

        IniChoiceFieldViewModel fullscreen = IniEditorTestData.Field<IniChoiceFieldViewModel>(harness.Editor, section: "user", key: "fullscreen");

        harness.TabTo(harness.Find<ComboBox>(fullscreen));

        fullscreen.IsFocused.ShouldBeTrue();
        harness.Editor.IsFooterHelp.ShouldBeTrue();

        harness.Close();
    }

    /// <summary>A Friendly check box is its own label: clicking it toggles the value and releases pointer focus, and its row has no label column.</summary>
    [ AvaloniaFact ]
    public void FriendlyCheckBox_Click_TogglesAndReleasesFocus()
    {
        IniEditorHarness harness = IniEditorHarness.Show(IniEditorStyle.Friendly);

        IniToggleFieldViewModel music = IniEditorTestData.Field<IniToggleFieldViewModel>(harness.Editor, section: "UI", key: "noMenuMusic");

        harness.ShowSectionOf(music);

        music.ShowLabelColumn.ShouldBeFalse();

        CheckBox checkBox = harness.Find<CheckBox>(music);

        checkBox.Content.ShouldBe("Enable menu music");

        bool before = music.IsChecked;

        harness.Click(checkBox);

        music.IsChecked.ShouldBe(!before);
        music.IsDirty.ShouldBeTrue();
        music.IsFocused.ShouldBeFalse();

        harness.Close();
    }

    /// <summary>A Friendly volume slider reached with Tab is focused and moves with the arrow keys, marking the row dirty.</summary>
    [ AvaloniaFact ]
    public void FriendlySlider_ReachedWithTab_IsFocusedAndMoves()
    {
        IniEditorHarness harness = IniEditorHarness.Show(IniEditorStyle.Friendly);

        IniSliderFieldViewModel volume = IniEditorTestData.Field<IniSliderFieldViewModel>(harness.Editor, section: "UI", key: "userAttenuation");

        harness.ShowSectionOf(volume);

        Slider slider = harness.Find<Slider>(volume);

        harness.TabTo(slider);

        volume.IsFocused.ShouldBeTrue();

        harness.Press(Avalonia.Input.Key.Left);

        volume.IsDirty.ShouldBeTrue();

        harness.Close();
    }

    /// <summary>The Friendly language page renders the picker and its codes line, and nothing for the two hidden code rows.</summary>
    [ AvaloniaFact ]
    public void FriendlyLanguagePage_RendersOnlyThePicker()
    {
        IniEditorHarness harness = IniEditorHarness.Show(IniEditorStyle.Friendly);

        IniLanguageFieldViewModel language = harness.Editor.Sections.SelectMany(section => section.Fields).OfType<IniLanguageFieldViewModel>().Single();

        harness.ShowSectionOf(language);

        harness.View.GetVisualDescendants()
               .OfType<Control>()
               .Where(control => control.DataContext is IniNumberFieldViewModel { IsHidden: true })
               .ShouldBeEmpty();

        harness.View.GetVisualDescendants()
               .OfType<TextBlock>()
               .ShouldContain(text => text.Text == language.CodesText && text.IsVisible);

        harness.Close();
    }

    /// <summary>Switching style while the editor is on screen re-renders it in place, with the pending edit carried across.</summary>
    [ AvaloniaFact ]
    public void ApplyStyle_WhileShown_ReRendersAndKeepsTheEdit()
    {
        IniEditorHarness harness = IniEditorHarness.Show(IniEditorStyle.Ini);

        IniEditorTestData.Field<IniNumberFieldViewModel>(harness.Editor, section: "user", key: "screenwidth").Value = 1024m;

        harness.Editor.ApplyStyle(IniEditorStyle.Friendly);

        Dispatcher.UIThread.RunJobs();

        harness.Editor.HasPendingChanges.ShouldBeTrue();
        harness.Editor.SelectedSection.Title.ShouldBe("Display");

        IniNumberFieldViewModel width = IniEditorTestData.Field<IniNumberFieldViewModel>(harness.Editor, section: "user", key: "screenwidth");

        harness.Find<NumericUpDown>(width).Value.ShouldBe(1024m);

        harness.Close();
    }
}
