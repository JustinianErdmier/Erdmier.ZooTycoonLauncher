using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

using Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Headless;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Views.IniConfig;

/// <summary>Drives the real INI editor in both styles: every section and page renders without Avalonia logging a problem, and the Friendly sound page and sliders behave.</summary>
public sealed class IniEditorStyleHeadlessTests
{
    /// <summary>Both editor styles.</summary>
    public static TheoryData<string> Styles => new() { IniEditorStyle.Ini.Name, IniEditorStyle.Friendly.Name };

    /// <summary>Every section (INI) or page (Friendly) can be selected in turn and laid out without Avalonia logging a warning or error, such as a binding that cannot convert.</summary>
    /// <param name="styleName">The name of the editor style under test.</param>
    [ AvaloniaTheory ]
    [ MemberData(nameof(Styles)) ]
    public void Editor_RendersInBothStyles(string styleName)
    {
        HeadlessTestAppBuilder.LogSink.Clear();

        IniEditorStyle style = IniEditorStyle.FromName(styleName);

        IniEditorHarness harness = IniEditorHarness.Show(style);

        harness.Editor.Sections.ShouldNotBeEmpty();

        foreach (IniSectionViewModel section in harness.Editor.Sections)
        {
            harness.Editor.SelectedSection = section;

            Dispatcher.UIThread.RunJobs();

            harness.View.IsMeasureValid.ShouldBeTrue($"The {section.Title} section was not laid out.");
        }

        harness.Close();

        IReadOnlyList<string> logged = HeadlessTestAppBuilder.LogSink.Messages;

        logged.ShouldBeEmpty($"The {style} editor made Avalonia log a warning or error:" + Environment.NewLine + string.Join(Environment.NewLine, logged));
    }

    /// <summary>The inverted <c>noMenuMusic</c> key shows <i>Enable menu music</i> ticked when the file holds <c>noMenuMusic=0</c>, and the rendered check box agrees.</summary>
    [ AvaloniaFact ]
    public void FriendlySoundPage_EnableMenuMusicTicked()
    {
        IniEditorHarness harness = IniEditorHarness.Show(IniEditorStyle.Friendly, [ ("UI", "noMenuMusic", "0") ]);

        IniToggleFieldViewModel music = IniEditorTestData.Field<IniToggleFieldViewModel>(harness.Editor, section: "UI", key: "noMenuMusic");

        harness.ShowSectionOf(music);

        music.IsChecked.ShouldBeTrue();
        music.IsDirty.ShouldBeFalse();
        harness.Find<CheckBox>(music).IsChecked.ShouldBe(expected: true);

        harness.Close();
    }

    /// <summary>A pointer drag on a Friendly slider moves it and, once released, lets go of the focus the pointer gave it.</summary>
    [ AvaloniaFact ]
    public void FriendlySlider_DragReleasesFocus()
    {
        IniEditorHarness harness = IniEditorHarness.Show(IniEditorStyle.Friendly, [ ("UI", "userAttenuation", "-5000") ]);

        IniSliderFieldViewModel volume = IniEditorTestData.Field<IniSliderFieldViewModel>(harness.Editor, section: "UI", key: "userAttenuation");

        harness.ShowSectionOf(volume);

        int before = volume.Percent;

        harness.Drag(harness.Find<Slider>(volume), deltaX: 40);

        volume.Percent.ShouldNotBe(before);
        volume.IsDirty.ShouldBeTrue();
        harness.FocusedElement.ShouldBeNull();
        volume.IsFocused.ShouldBeFalse();

        harness.Close();
    }

    /// <summary>A Friendly slider reached with Tab keeps its focus while the arrow keys move it, so the footer keeps showing its help.</summary>
    [ AvaloniaFact ]
    public void FriendlySlider_TabKeepsFocus()
    {
        IniEditorHarness harness = IniEditorHarness.Show(IniEditorStyle.Friendly);

        IniSliderFieldViewModel volume = IniEditorTestData.Field<IniSliderFieldViewModel>(harness.Editor, section: "UI", key: "userAttenuation");

        harness.ShowSectionOf(volume);

        Slider slider = harness.Find<Slider>(volume);

        int before = volume.Percent;

        harness.TabTo(slider);
        harness.Press(Key.Left);
        harness.Press(Key.Left);

        volume.Percent.ShouldNotBe(before);

        harness.FocusedElement.ShouldBeSameAs(slider);
        volume.IsFocused.ShouldBeTrue();
        harness.Editor.IsFooterHelp.ShouldBeTrue();

        harness.Close();
    }
}
