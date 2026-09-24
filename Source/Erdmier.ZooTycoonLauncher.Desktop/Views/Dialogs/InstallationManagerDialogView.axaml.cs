namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>The Win95-styled modal Installation Manager dialogue. Closes itself when the view model raises <c>CloseRequested</c>.</summary>
public sealed partial class InstallationManagerDialogView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public InstallationManagerDialogView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs eventArgs)
    {
        base.OnDataContextChanged(eventArgs);

        if (DataContext is not InstallationManagerDialogViewModel viewModel)
        {
            return;
        }

        viewModel.CloseRequested -= OnCloseRequested;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs eventArgs) => Close();
}
