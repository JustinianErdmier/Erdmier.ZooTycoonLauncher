namespace Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig.Fields;

/// <summary>View for <see cref="IniLanguageFieldViewModel" />: the curated language combo.</summary>
public sealed partial class IniLanguageFieldView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public IniLanguageFieldView() => AvaloniaXamlLoader.Load(this);

    // DropDownClosed is a CLR event, not a routed one, so the row cannot hear it: the value is set, so a pointer-focused field lets go.
    // The list closes with the combo box refocusing itself as Unspecified, which leaves IsFocused alone, so a keyboard-focused combo keeps its focus.
    private void OnDropDownClosed(object? sender, EventArgs e) => IniFieldFocusRelease.AfterCommit(this);
}
