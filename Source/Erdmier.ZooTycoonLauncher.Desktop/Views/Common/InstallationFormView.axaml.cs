namespace Erdmier.ZooTycoonLauncher.Desktop.Views.Common;

/// <summary>Code-behind for <c>InstallationFormView.axaml</c> — the shared Name / Folder / Default form hosted by the Add and Edit Installation dialogues.</summary>
public sealed partial class InstallationFormView : UserControl
{
    /// <summary>Initialises a new instance.</summary>
    public InstallationFormView() => AvaloniaXamlLoader.Load(this);
}
