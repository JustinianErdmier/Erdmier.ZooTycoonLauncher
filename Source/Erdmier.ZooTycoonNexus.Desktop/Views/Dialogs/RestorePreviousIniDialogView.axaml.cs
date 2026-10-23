namespace Erdmier.ZooTycoonNexus.Desktop.Views.Dialogs;

/// <summary>
///     The Win95-styled modal Restore Previous INI dialogue. Closes itself when the view model raises <c>CloseRequested</c>, and refuses a title-bar close whilst a
///     restore is in flight.
/// </summary>
public sealed partial class RestorePreviousIniDialogView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public RestorePreviousIniDialogView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnClosing(WindowClosingEventArgs eventArgs)
    {
        base.OnClosing(eventArgs);

        // A user-initiated close (the title-bar button) must not drop an in-flight restore; programmatic closes are never blocked.
        if (!eventArgs.IsProgrammatic && DataContext is RestorePreviousIniDialogViewModel { CanClose: false })
        {
            eventArgs.Cancel = true;
        }
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs eventArgs)
    {
        base.OnDataContextChanged(eventArgs);

        if (DataContext is not RestorePreviousIniDialogViewModel viewModel)
        {
            return;
        }

        viewModel.CloseRequested -= OnCloseRequested;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs eventArgs) => Close();
}
