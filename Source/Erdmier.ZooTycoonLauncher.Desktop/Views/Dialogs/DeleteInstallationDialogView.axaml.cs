namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>The Win95-styled Delete Installation confirmation. Closes itself when the view model raises <c>CloseRequested</c>, returning whether the delete happened.</summary>
public sealed partial class DeleteInstallationDialogView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public DeleteInstallationDialogView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs eventArgs)
    {
        base.OnDataContextChanged(eventArgs);

        if (DataContext is not DeleteInstallationDialogViewModel viewModel)
        {
            return;
        }

        viewModel.CloseRequested -= OnCloseRequested;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, bool deleted) => Close(deleted);
}
