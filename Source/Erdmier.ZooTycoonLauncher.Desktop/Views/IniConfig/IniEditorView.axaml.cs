namespace Erdmier.ZooTycoonLauncher.Desktop.Views.IniConfig;

/// <summary>View for <see cref="IniEditorViewModel" />: the section list, the selected section's form, and the footer's Save, Reset and Restore defaults commands.</summary>
public sealed partial class IniEditorView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public IniEditorView() => AvaloniaXamlLoader.Load(this);
}
