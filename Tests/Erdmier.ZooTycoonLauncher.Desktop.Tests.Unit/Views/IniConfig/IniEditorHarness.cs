using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

using Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Views.IniConfig;

/// <summary>
///     Hosts the real <see cref="IniEditorView" /> in a headless window and drives it with keyboard and pointer input, so the INI focus handlers in the views run as they do
///     in the launcher (milestone 11.1 §6a.1).
/// </summary>
internal sealed class IniEditorHarness
{
    private IniEditorHarness(Window window, IniEditorView view, IniEditorViewModel editor)
    {
        Window = window;
        View   = view;
        Editor = editor;
    }

    /// <summary>The window hosting the editor.</summary>
    public Window Window { get; }

    /// <summary>The editor view under test.</summary>
    public IniEditorView View { get; }

    /// <summary>The editor view model, loaded with the values passed to <see cref="Show" />.</summary>
    public IniEditorViewModel Editor { get; }

    /// <summary>Presses, moves and releases the left button on <paramref name="target" />, dragging from its centre by <paramref name="deltaX" /> pixels.</summary>
    /// <param name="target">The control to drag.</param>
    /// <param name="deltaX">The horizontal distance to drag.</param>
    public void Drag(Visual target, double deltaX)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(target).ShouldNotBeNull();

        Point start = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), topLevel).ShouldNotBeNull();

        topLevel.MouseMove(start);
        topLevel.MouseDown(start, MouseButton.Left);
        topLevel.MouseMove(new Point(start.X + deltaX, start.Y));
        topLevel.MouseUp(new Point(start.X + deltaX, start.Y), MouseButton.Left);

        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The element that has keyboard focus, or <see langword="null" /> when nothing has.</summary>
    public IInputElement? FocusedElement => Window.FocusManager?.GetFocusedElement();

    /// <summary>Shows the editor in a new headless window and lets it lay out.</summary>
    /// <param name="style">The editor style; the INI style by default, which the focus tests drive by key.</param>
    /// <param name="values">The file's values; <c>fullscreen = 1</c> and <c>screenwidth = 800</c> by default.</param>
    /// <returns>The harness.</returns>
    public static IniEditorHarness Show(IniEditorStyle? style = null, (string Section, string Key, string? Value)[]? values = null)
    {
        style  ??= IniEditorStyle.Ini;
        values ??= [ ("user", "fullscreen", "1"), ("user", "screenwidth", "800") ];

        IniEditorViewModel editor = new(Guid.CreateVersion7(), Substitute.For<IMediator>(), Substitute.For<IDialogService>(),
                                        IniEditorTestData.Result(values),
                                        NullLogger<IniEditorViewModel>.Instance, style);

        IniEditorView view = new() { DataContext = editor };

        Window window = new()
        {
            Content = view,
            Width   = 800,
            Height  = 600
        };

        window.Show();

        Dispatcher.UIThread.RunJobs();

        return new IniEditorHarness(window, view, editor);
    }

    /// <summary>Selects the section holding <paramref name="field" /> and lets the form lay out.</summary>
    /// <param name="field">The field whose section to show.</param>
    public void ShowSectionOf(IniFieldViewModel field)
    {
        Editor.SelectedSection = Editor.Sections.First(section => section.Fields.Contains(field));

        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The INI row of <paramref name="field" />: the outermost control in the form whose data context is the field.</summary>
    /// <param name="field">The field.</param>
    /// <returns>The row.</returns>
    public Control RowOf(IniFieldViewModel field)
        => View.GetVisualDescendants()
               .OfType<Control>()
               .First(control => ReferenceEquals(control.DataContext, field));

    /// <summary>The first control of type <typeparamref name="T" /> inside the row of <paramref name="field" />.</summary>
    /// <param name="field">The field.</param>
    /// <typeparam name="T">The control type.</typeparam>
    /// <returns>The control.</returns>
    public T Find<T>(IniFieldViewModel field)
        where T : Control
        => RowOf(field).GetVisualDescendants().OfType<T>().First();

    /// <summary>Presses Tab until <paramref name="target" /> has focus, failing after a bounded number of presses.</summary>
    /// <param name="target">The control to reach.</param>
    public void TabTo(Control target)
    {
        for (int presses = 0; presses < 20 && !ReferenceEquals(FocusedElement, target); presses++)
        {
            Press(Key.Tab);
        }

        FocusedElement.ShouldBeSameAs(target, "Tab never reached the target control.");
    }

    /// <summary>Presses and releases a key, then lets the dispatcher settle.</summary>
    /// <param name="key">The key.</param>
    /// <param name="modifiers">Any held modifiers.</param>
    public void Press(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Window.KeyPress(key, modifiers, PhysicalKey.None, keySymbol: null);
        Window.KeyRelease(key, modifiers, PhysicalKey.None, keySymbol: null);

        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Clicks the centre of <paramref name="target" /> with the left button, in whichever top level hosts it (a drop-down's items sit in their own pop-up).</summary>
    /// <param name="target">The control to click.</param>
    public void Click(Visual target)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(target).ShouldNotBeNull();

        Point centre = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), topLevel).ShouldNotBeNull();

        topLevel.MouseMove(centre);
        topLevel.MouseDown(centre, MouseButton.Left);
        topLevel.MouseUp(centre, MouseButton.Left);

        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Closes the window.</summary>
    public void Close()
    {
        Window.Close();

        Dispatcher.UIThread.RunJobs();
    }
}
