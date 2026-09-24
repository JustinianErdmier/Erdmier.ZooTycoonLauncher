using System.ComponentModel;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;

/// <summary>
///     View model for the <c>OpenGameInstallation</c> state — shown when <c>LauncherSettings.LauncherStartupPreference</c> is <c>NoInstallation</c> (SDD §9.6). Hosts an
///     <see cref="InstallationGridViewModel" /> and exposes the picker action commands (<c>Open</c>, <c>Add</c>, <c>Info</c>, <c>Manage</c>).
/// </summary>
public sealed partial class OpenGameInstallationViewModel : ViewModelBase, IDisposable
{
    private readonly IDialogService? _dialogs;

    private readonly Func<Guid, CancellationToken, Task>? _openInstallationAsync;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="grid">The installation grid view model. The picker takes ownership of it and disposes it alongside itself.</param>
    /// <param name="dialogs">The dialogue service used to open the Add Installation modal and the Installation Manager.</param>
    /// <param name="openInstallationAsync">Callback that issues a pointed boot at the chosen installation (SDD §7.2.7), bypassing the startup preference.</param>
    public OpenGameInstallationViewModel(InstallationGridViewModel grid, IDialogService dialogs, Func<Guid, CancellationToken, Task> openInstallationAsync)
    {
        Grid                   = grid;
        _dialogs               = dialogs;
        _openInstallationAsync = openInstallationAsync;

        Grid.PropertyChanged += OnGridPropertyChanged;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public OpenGameInstallationViewModel()
        : this(new InstallationGridViewModel(), null!, null!)
    { }

    /// <summary>The installation grid view model. Bound to <c>InstallationGridView.DataContext</c>.</summary>
    public InstallationGridViewModel Grid { get; }

    /// <summary>Loads the picker's installation list. Must be awaited by <c>MainWindowViewModel</c> before the picker is shown as <c>ActiveContent</c>.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task InitialiseAsync(CancellationToken cancellationToken = default) => Grid.LoadAsync(cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        Grid.PropertyChanged -= OnGridPropertyChanged;
        Grid.Dispose();
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteSelectionCommand)) ]
    private async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (_openInstallationAsync is null
            || Grid.SelectedRow is null)
        {
            return;
        }

        await _openInstallationAsync(Grid.SelectedRow.Id, cancellationToken);
    }

    [ RelayCommand ]
    private async Task AddAsync(CancellationToken cancellationToken)
    {
        if (_dialogs is null)
        {
            return;
        }

        AddInstallationResult? result = await _dialogs.ShowAddInstallationAsync(prefilledPath: null);

        if (result is not null)
        {
            await Grid.LoadAsync(cancellationToken);
        }
    }

    [ RelayCommand(CanExecute = nameof(CanExecuteSelectionCommand)) ]
    private Task InfoAsync(CancellationToken cancellationToken)
        => Task.CompletedTask; // TODO: SDD §7.2.4 — Installation Info dialogue not yet implemented.

    [ RelayCommand ]
    private async Task ManageAsync(CancellationToken cancellationToken)
    {
        if (_dialogs is null)
        {
            return;
        }

        bool changed = await _dialogs.ShowInstallationManagerAsync();

        if (changed)
        {
            await Grid.LoadAsync(cancellationToken);
        }
    }

    private bool CanExecuteSelectionCommand() => Grid.HasSelection;

    private void OnGridPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(InstallationGridViewModel.HasSelection))
        {
            OpenCommand.NotifyCanExecuteChanged();
            InfoCommand.NotifyCanExecuteChanged();
        }
    }
}
