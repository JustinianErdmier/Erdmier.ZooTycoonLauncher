namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>The Win95-styled Fix Installation dialogue. Closes itself when the view model raises <c>CloseRequested</c>.</summary>
public sealed partial class FixInstallationDialogView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public FixInstallationDialogView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs eventArgs)
    {
        base.OnDataContextChanged(eventArgs);

        if (DataContext is not FixInstallationDialogViewModel viewModel)
        {
            return;
        }

        viewModel.CloseRequested -= OnCloseRequested;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();
}
