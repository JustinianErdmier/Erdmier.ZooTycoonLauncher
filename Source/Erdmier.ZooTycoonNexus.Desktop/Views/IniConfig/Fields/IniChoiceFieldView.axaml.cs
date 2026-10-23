namespace Erdmier.ZooTycoonNexus.Desktop.Views.IniConfig.Fields;

/// <summary>View for <see cref="IniChoiceFieldViewModel" />: a combo of labelled values.</summary>
public sealed partial class IniChoiceFieldView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public IniChoiceFieldView() => AvaloniaXamlLoader.Load(this);

    // DropDownClosed is a CLR event, not a routed one, so the row cannot hear it: the list closed (also on Esc or a click away, which is harmless), so a pointer-focused field lets go.
    // The list closes with the combo box refocusing itself as Unspecified, which leaves IsFocused alone, so a keyboard-focused combo keeps its focus.
    private void OnDropDownClosed(object? sender, EventArgs e) => IniFieldFocusRelease.AfterCommit(this);
}
