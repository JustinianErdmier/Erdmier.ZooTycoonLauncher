namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;

/// <summary>The Win95-style save-changes prompt. Closes with the chosen <see cref="SaveChangesChoice" />; closing from the title bar yields no result (treated as Cancel).</summary>
public sealed partial class SaveChangesPromptView : ClassicWindow
{
    /// <summary>Initialises a new instance.</summary>
    public SaveChangesPromptView() => AvaloniaXamlLoader.Load(this);

    private void OnYesClick(object? sender, RoutedEventArgs e) => Close(SaveChangesChoice.Yes);

    private void OnNoClick(object? sender, RoutedEventArgs e) => Close(SaveChangesChoice.No);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(SaveChangesChoice.Cancel);
}
