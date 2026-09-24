namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>The Win95-styled modal Edit Installation dialogue. Closes itself when the view model raises <c>CloseRequested</c>, returning whether the edit was saved.</summary>
public sealed partial class EditInstallationDialogView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public EditInstallationDialogView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs eventArgs)
    {
        base.OnDataContextChanged(eventArgs);

        if (DataContext is not EditInstallationDialogViewModel viewModel)
        {
            return;
        }

        viewModel.CloseRequested -= OnCloseRequested;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, bool saved) => Close(saved);
}
