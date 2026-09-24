namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>The Win95-styled read-only Installation Info dialogue. Closes itself when the view model raises <c>CloseRequested</c>.</summary>
public sealed partial class InstallationInfoDialogView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public InstallationInfoDialogView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs eventArgs)
    {
        base.OnDataContextChanged(eventArgs);

        if (DataContext is not InstallationInfoDialogViewModel viewModel)
        {
            return;
        }

        viewModel.CloseRequested -= OnCloseRequested;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();
}
