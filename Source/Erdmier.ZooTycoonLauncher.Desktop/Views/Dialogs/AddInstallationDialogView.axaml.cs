namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>
///     The Win95-styled modal Add Installation dialogue. Closes itself when the view model raises <c>CloseRequested</c>, carrying the dispatched
///     <see cref="AddInstallationResult" /> or <see langword="null" /> on cancel.
/// </summary>
public sealed partial class AddInstallationDialogView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public AddInstallationDialogView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs eventArgs)
    {
        base.OnDataContextChanged(eventArgs);

        if (DataContext is not AddInstallationDialogViewModel viewModel)
        {
            return;
        }

        viewModel.CloseRequested -= OnCloseRequested;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, AddInstallationResult? result) => Close(result);
}
