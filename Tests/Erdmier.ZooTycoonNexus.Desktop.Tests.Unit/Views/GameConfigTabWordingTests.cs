using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;

using Classic.Avalonia.Theme.Controls;

using Erdmier.ZooTycoonNexus.Desktop.Views.Boot;
using Erdmier.ZooTycoonNexus.Desktop.Views.Dialogs;

namespace Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.Views;

/// <summary>
///     Pins the user-visible name of the <c>zoo.ini</c> editor tab: the main window's tab strip and the Options dialogue call it Game Config, never the file-format name.
/// </summary>
public sealed class GameConfigTabWordingTests
{
    [ AvaloniaFact ]
    public void PlayView_TabHeaders_ReadGeneralThenGameConfig()
    {
        PlayView view = new() { DataContext = new PlayViewModel() };

        Window window = new() { Content = view };

        window.Show();

        Dispatcher.UIThread.RunJobs();

        string?[] headers = view.GetLogicalDescendants().OfType<TabItem>().Select(tab => tab.Header as string).ToArray();

        headers.ShouldBe([ "General", "Game Config" ]);
    }

    [ AvaloniaFact ]
    public void OptionsDialog_EditorStyleLine_NamesTheGameConfigTab()
    {
        OptionsDialogView view = new() { DataContext = new OptionsDialogViewModel() };

        view.Show();

        Dispatcher.UIThread.RunJobs();

        string?[] texts = view.GetLogicalDescendants().OfType<TextBlock>().Select(block => block.Text).ToArray();

        texts.ShouldContain("Show settings in the Game Config tab:");
        texts.ShouldNotContain(text => text != null && text.Contains("INI Config"));
    }

    [ AvaloniaFact ]
    public void OptionsDialog_EditorStyleGroup_IsHeadedConfigEditor()
    {
        OptionsDialogView view = new() { DataContext = new OptionsDialogViewModel() };

        view.Show();

        Dispatcher.UIThread.RunJobs();

        object?[] headers = view.GetLogicalDescendants().OfType<GroupBox>().Select(box => box.Header).ToArray();

        headers.ShouldContain("Config editor");
        headers.ShouldNotContain("INI editor");
    }
}
