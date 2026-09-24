namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common;

/// <summary>
///     Shared view model for the installation form — Name, Folder (with Browse), Mark as default, and the inline error — hosted by the Add and Edit Installation
///     dialogues (SDD §7.2.1, §7.2.3, §9.5). The host sets the mode flags and owns Save; the form owns only the inputs and the folder picker.
/// </summary>
public sealed partial class InstallationFormViewModel : ViewModelBase
{
    private readonly IDialogService? _dialogs;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="dialogs">The dialogue service — used for the folder picker behind Browse.</param>
    public InstallationFormViewModel(IDialogService dialogs) => _dialogs = dialogs;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public InstallationFormViewModel()
        : this(null!)
    { }

    // TODO: Should probably be a list so we can display multiple errors at once.
    /// <summary>The most recent validation or dispatch error, or <see langword="null" /> when none. Shown under the inputs.</summary>
    [ ObservableProperty ]
    public partial string? ErrorMessage { get; set; }

    /// <summary><see langword="true" /> when the Browse button is shown (Add); <see langword="false" /> hides it (Edit).</summary>
    [ ObservableProperty ]
    public partial bool IsBrowseVisible { get; set; } = true;

    /// <summary>
    ///     <see langword="true" /> when Mark as default is ticked and cannot be changed — the first installation in Add (SDD §7.2.1), or the current default in Edit
    ///     (SDD §7.2.3). Set through <see cref="LockDefault" />.
    /// </summary>
    [ ObservableProperty ]
    public partial bool IsDefaultLocked { get; set; }

    /// <summary><see langword="true" /> when the Folder input is read-only (Edit — relocation happens through Fix).</summary>
    [ ObservableProperty ]
    public partial bool IsFolderReadOnly { get; set; }

    /// <summary>Whether the installation should be (or stay) the launcher default. Bound to the checkbox.</summary>
    [ ObservableProperty ]
    public partial bool MakeDefault { get; set; }

    /// <summary>The user-visible installation name. Bound to the Name input.</summary>
    [ ObservableProperty ]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>The installation folder (the one containing <c>zoo.exe</c>). Bound to the Folder input.</summary>
    [ ObservableProperty ]
    public partial string Path { get; set; } = string.Empty;

    /// <summary>Ticks Mark as default and prevents it being unticked.</summary>
    public void LockDefault()
    {
        MakeDefault     = true;
        IsDefaultLocked = true;
    }

    [ RelayCommand ]
    private async Task BrowseAsync()
    {
        if (_dialogs is null)
        {
            return;
        }

        string? chosen = await _dialogs.PickFolderAsync(string.IsNullOrWhiteSpace(Path) ? null : Path);

        if (!string.IsNullOrWhiteSpace(chosen))
        {
            Path = chosen;
        }
    }
}
