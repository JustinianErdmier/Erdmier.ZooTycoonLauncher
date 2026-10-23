namespace Erdmier.ZooTycoonNexus.Desktop.Views.Dialogs;

/// <summary>The Win95-style Yes / No confirmation prompt. Closes with <see langword="true" /> for Yes and <see langword="false" /> for No; closing from the title bar yields no result (treated as No).</summary>
public sealed partial class ConfirmationPromptView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public ConfirmationPromptView() => AvaloniaXamlLoader.Load(this);

    private void OnYesClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnNoClick(object? sender, RoutedEventArgs e) => Close(false);
}