namespace Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig.Fields;

/// <summary>View for <see cref="IniChoiceFieldViewModel" />: a combo of labelled values.</summary>
public sealed partial class IniChoiceFieldView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public IniChoiceFieldView() => AvaloniaXamlLoader.Load(this);

    // DropDownClosed is a CLR event, not a routed one, so the row cannot hear it: the value is set, so a pointer-focused field lets go.
    private void OnDropDownClosed(object? sender, EventArgs e) => IniFieldFocusRelease.AfterCommit(this);
}
