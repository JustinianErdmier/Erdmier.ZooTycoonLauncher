namespace Erdmier.ZooTycoonNexus.Desktop.Composition;

/// <summary>The default <see cref="IIniEditorStyleState" />: an in-memory value starting at <see cref="IniEditorStyle.Friendly" />, the standard style.</summary>
public sealed class IniEditorStyleState : IIniEditorStyleState
{
    private IniEditorStyle _style = IniEditorStyle.Friendly;

    /// <inheritdoc />
    public int Version { get; private set; }

    /// <inheritdoc />
    public event EventHandler? StyleChanged;

    /// <inheritdoc />
    public IniEditorStyle Style
    {
        get => _style;
        set
        {
            Version++;

            if (_style == value)
            {
                return;
            }

            _style = value;

            StyleChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
