namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>The Win95-styled modal Add Installation dialogue. Closes itself when the view model raises <c>CloseRequested</c>, carrying the dispatched <see cref="AddInstallationResult" /> or <see langword="null" /> on cancel.</summary>
public sealed partial class AddInstallationDialogView : Window
{
    /// <summary>Initialises a new instance.</summary>
    public AddInstallationDialogView() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is AddInstallationDialogViewModel vm)
        {
            vm.CloseRequested -= OnCloseRequested;
            vm.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, AddInstallationResult? result) => Close(result);
}
