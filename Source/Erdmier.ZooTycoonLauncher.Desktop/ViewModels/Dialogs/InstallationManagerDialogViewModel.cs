namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     View model for the Installation Manager modal (SDD §7.2.2, §9.4). Hosts <see cref="InstallationGridViewModel" /> and exposes the five management
///     commands. <c>Info</c>, <c>Edit</c>, <c>Delete</c>, and <c>Fix</c> are scaffolded stubs — each will be completed when its corresponding dialogue is
///     implemented.
/// </summary>
public sealed partial class InstallationManagerDialogViewModel : ViewModelBase, IDisposable
{
    private readonly IDialogService? _dialogs;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="grid">The installation grid view model. The manager takes ownership of it and disposes it alongside itself.</param>
    /// <param name="dialogs">The dialogue service — used here to open the Add Installation modal.</param>
    public InstallationManagerDialogViewModel(InstallationGridViewModel grid, IDialogService dialogs)
    {
        Grid     = grid;
        _dialogs = dialogs;

        Grid.PropertyChanged += OnGridPropertyChanged;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public InstallationManagerDialogViewModel()
        : this(new InstallationGridViewModel(), null!)
    { }

    /// <summary>The installation grid view model. Bound to <c>InstallationGridView.DataContext</c>.</summary>
    public InstallationGridViewModel Grid { get; }

    /// <summary>
    ///     <see langword="true" /> when the user has changed anything during this session of the dialogue (currently: at least one successful Add). Read by
    ///     <see cref="Composition.AvaloniaDialogService.ShowInstallationManagerAsync" /> once the dialogue closes, so callers only refresh when something actually changed.
    /// </summary>
    public bool HasChanges { get; private set; }

    /// <summary>Loads the installation list. Must be awaited by the dialogue service before the window is shown.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task InitialiseAsync(CancellationToken cancellationToken = default)
        => Grid.LoadAsync(cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        Grid.PropertyChanged -= OnGridPropertyChanged;
        Grid.Dispose();
    }

    [ RelayCommand ]
    private async Task AddAsync()
    {
        if (_dialogs is null)
        {
            return;
        }

        AddInstallationResult? result = await _dialogs.ShowAddInstallationAsync(prefilledPath: null);

        if (result is not null)
        {
            HasChanges = true;
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteSelectionCommand)) ]
    private Task InfoAsync(CancellationToken cancellationToken)
        => Task.CompletedTask; // TODO: SDD §7.2.4 — Installation Info dialogue not yet implemented.

    [ RelayCommand(CanExecute = nameof(CanExecuteSelectionCommand)) ]
    private Task EditAsync(CancellationToken cancellationToken)
        => Task.CompletedTask; // TODO: SDD §7.2.3 — Edit Installation dialogue not yet implemented.

    [ RelayCommand(CanExecute = nameof(CanExecuteSelectionCommand)) ]
    private Task DeleteAsync(CancellationToken cancellationToken)
        => Task.CompletedTask; // TODO: SDD §7.2.5 — Delete Installation confirmation not yet implemented.

    [ RelayCommand(CanExecute = nameof(CanExecuteFixCommand)) ]
    private Task FixAsync(CancellationToken cancellationToken)
        => Task.CompletedTask; // TODO: SDD §7.2.6 — Fix Installation dialogue not yet implemented.

    [ RelayCommand ]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private bool CanExecuteSelectionCommand() => Grid.HasSelection;

    private bool CanExecuteFixCommand() => Grid.IsSelectionInvalid;

    private void OnGridPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(InstallationGridViewModel.HasSelection)
                           or nameof(InstallationGridViewModel.IsSelectionInvalid))
        {
            InfoCommand.NotifyCanExecuteChanged();
            EditCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
            FixCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Raised when the dialogue should close.</summary>
    public event EventHandler? CloseRequested;
}
