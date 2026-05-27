using Avalonia.Controls;
using Avalonia.Interactivity;

using Classic.Avalonia.Theme;

namespace Erdmier.ZooTycoonLauncher.Launcher.Views;

/// <summary>A minimal yes/no confirmation dialog. No ViewModel — result is returned via <see cref="Window.ShowDialog{TResult}" /> as a boxed <see cref="bool" />.</summary>
public partial class ConfirmDialogView : ClassicWindow
{
    /// <summary>
    ///     Parameterless constructor required by the Avalonia runtime XAML loader and the designer.
    ///     Production code uses <see cref="ConfirmDialogView(string)" /> instead.
    /// </summary>
    public ConfirmDialogView() : this(message: string.Empty)
    { }

    /// <summary>Initialises a new instance of <see cref="ConfirmDialogView" /> with the given message body.</summary>
    /// <param name="message">Body text shown above the Yes/No buttons.</param>
    public ConfirmDialogView(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }

    /// <summary>Closes the dialog returning <see langword="true" /> to indicate confirmation.</summary>
    private void OnYesClick(object? sender, RoutedEventArgs e) => Close((object?)true);

    /// <summary>Closes the dialog returning <see langword="false" /> to indicate cancellation.</summary>
    private void OnNoClick(object? sender, RoutedEventArgs e) => Close((object?)false);
}
