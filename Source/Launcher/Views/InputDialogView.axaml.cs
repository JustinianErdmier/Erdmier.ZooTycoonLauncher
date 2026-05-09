using Avalonia.Controls;
using Avalonia.Interactivity;

using Classic.Avalonia.Theme;

namespace Erdmier.ZooTycoonLauncher.Launcher.Views;

/// <summary>A minimal single-line text-input dialog. No ViewModel — result is returned via <see cref="Window.ShowDialog{TResult}" />.</summary>
public partial class InputDialogView : ClassicWindow
{
    /// <summary>
    ///     Parameterless constructor required by the Avalonia runtime XAML loader and the designer.
    ///     Production code uses <see cref="InputDialogView(string, string)" /> instead.
    /// </summary>
    public InputDialogView() : this(prompt: string.Empty)
    { }

    /// <summary>Initialises a new instance of <see cref="InputDialogView" /> with a prompt and optional pre-filled text.</summary>
    /// <param name="prompt">Prompt label rendered above the input box.</param>
    /// <param name="defaultValue">Pre-filled text in the input box; the caret is positioned at the end.</param>
    public InputDialogView(string prompt, string defaultValue = "")
    {
        InitializeComponent();
        PromptText.Text     = prompt;
        InputBox.Text       = defaultValue;
        InputBox.CaretIndex = defaultValue.Length;
    }

    /// <summary>Closes the dialog returning the current input text.</summary>
    private void OnOkClick(object? sender, RoutedEventArgs e)
        => Close(InputBox.Text);

    /// <summary>Closes the dialog returning <see langword="null" /> to indicate cancellation.</summary>
    private void OnCancelClick(object? sender, RoutedEventArgs e)
        => Close((string?)null);
}
