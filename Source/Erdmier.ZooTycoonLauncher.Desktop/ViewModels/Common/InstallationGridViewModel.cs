using System.Collections.ObjectModel;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common;

/// <summary>
///     Shared view model for the installation <c>DataGrid</c> (SDD §9.4, §9.6). Owns the row collection, selected row, and data loading via
///     <see cref="GetAllInstallationsQuery" />. Subscribes to the installation change messages the Application handlers publish, so the grid refreshes
///     itself whoever made the change.
/// </summary>
public sealed partial class InstallationGridViewModel : ViewModelBase,
                                                        IDisposable,
                                                        IRecipient<InstallationAddedMessage>,
                                                        IRecipient<InstallationChangedMessage>,
                                                        IRecipient<InstallationDeletedMessage>,
                                                        IRecipient<DefaultInstallationChangedMessage>
{
    private readonly ILogger<InstallationGridViewModel> _logger;

    private readonly IMediator _mediator;

    private readonly IMessenger _messenger;

    private bool _disposed;

    private bool _isReloading;

    private bool _reloadPending;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher — used to issue <see cref="GetAllInstallationsQuery" />.</param>
    /// <param name="messenger">The CommunityToolkit messenger — used to subscribe to installation-change notifications.</param>
    /// <param name="logger">Logger for reload failures raised whilst handling installation change messages.</param>
    public InstallationGridViewModel(IMediator mediator, IMessenger messenger, ILogger<InstallationGridViewModel> logger)
    {
        _mediator  = mediator;
        _messenger = messenger;
        _logger    = logger;

        _messenger.RegisterAll(this);
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public InstallationGridViewModel()
        : this(null!, WeakReferenceMessenger.Default, NullLogger<InstallationGridViewModel>.Instance)
    { }

    /// <summary>The rows bound to the DataGrid. Rebuilt wholesale on every <see cref="LoadAsync" /> call.</summary>
    public ObservableCollection<InstallationGridRowModel> Rows { get; } = [];

    /// <summary>The currently selected row, or <see langword="null" /> when no row is selected.</summary>
    [ ObservableProperty ]
    [ NotifyPropertyChangedFor(nameof(HasSelection)) ]
    [ NotifyPropertyChangedFor(nameof(IsSelectionInvalid)) ]
    public partial InstallationGridRowModel? SelectedRow { get; set; }

    /// <summary><see langword="true" /> when a row is selected. Consumed by host VM <c>CanExecute</c> guards.</summary>
    public bool HasSelection => SelectedRow is not null;

    /// <summary><see langword="true" /> when the selected row's validity is not <c>Valid</c>. Guards the <c>Fix</c> command in the Installation Manager.</summary>
    public bool IsSelectionInvalid => SelectedRow?.ValidityColourToken == "Red";

    /// <summary>
    ///     Loads all registered installations from the database, projects them to <see cref="InstallationGridRowModel" />, sorts them (default first, then
    ///     alphabetical), and replaces <see cref="Rows" />. The previously selected row's identity is preserved across the reload when it still exists; otherwise
    ///     <see cref="SelectedRow" /> becomes <see langword="null" />.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_mediator is null)
        {
            return;
        }

        ErrorOr<IReadOnlyList<InstallationSummary>> result = await _mediator.Send(new GetAllInstallationsQuery(), cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to load the installation list: {Errors}",
                               string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}")));

            return;
        }

        Guid? selectedId = SelectedRow?.Id;

        IEnumerable<InstallationGridRowModel> sorted = result.Value
                                                             .Select(s => new InstallationGridRowModel(s.Id,
                                                                                                       s.Name,
                                                                                                       s.Path,
                                                                                                       s.Validity.DisplayName,
                                                                                                       s.Validity.ColourToken,
                                                                                                       s.IsDefault))
                                                             .Order(new InstallationGridRowComparer());

        Rows.Clear();

        foreach (InstallationGridRowModel row in sorted)
        {
            Rows.Add(row);
        }

        SelectedRow = Rows.FirstOrDefault(row => row.Id == selectedId);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;

        _messenger.UnregisterAll(this);
    }

    void IRecipient<InstallationAddedMessage>.Receive(InstallationAddedMessage message) => ScheduleReload();

    void IRecipient<InstallationChangedMessage>.Receive(InstallationChangedMessage message) => ScheduleReload();

    void IRecipient<InstallationDeletedMessage>.Receive(InstallationDeletedMessage message) => ScheduleReload();

    void IRecipient<DefaultInstallationChangedMessage>.Receive(DefaultInstallationChangedMessage message) => ScheduleReload();

    // Marshals the reload onto the UI thread (the publisher already sends on it, but Receive must not rely on that). A burst of messages — e.g. deleting the default
    // publishes InstallationDeletedMessage and DefaultInstallationChangedMessage together — is handed to ReloadCoalescedAsync, which never runs overlapping queries
    // on the shared DbContext.
    private void ScheduleReload()
        => Dispatcher.UIThread.Post(() =>
        {
            if (!_disposed)
            {
                _ = ReloadCoalescedAsync();
            }
        });

    // Runs only on the UI thread (see ScheduleReload), so the two flags need no locking. The gate is per grid instance: each InstallationGridViewModel serialises its
    // own reloads independently. A message that arrives whilst a reload is in flight marks one follow-up reload instead of starting a second, overlapping one — but
    // with SQLite's synchronous completion the first reload has usually already finished by the time a second message arrives, so back-to-back reloads are the common
    // case rather than true coalescing. The catch sits inside the loop so a failed attempt is logged, leaves the previous rows in place, and still honours a follow-up
    // reload requested whilst it was running. The explicit initial load is the hosts' InitialiseAsync (e.g. InstallationManagerDialogViewModel,
    // OpenGameInstallationViewModel), which calls LoadAsync directly and bypasses this gate.
    private async Task ReloadCoalescedAsync()
    {
        if (_isReloading)
        {
            _reloadPending = true;

            return;
        }

        _isReloading = true;

        try
        {
            do
            {
                _reloadPending = false;

                try
                {
                    await LoadAsync();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to reload the installation list.");
                }
            }
            while (_reloadPending && !_disposed);
        }
        finally
        {
            _isReloading = false;
        }
    }
}

// Sort: default row first, then alphabetical case-insensitive by Name.
file sealed class InstallationGridRowComparer : IComparer<InstallationGridRowModel>
{
    public int Compare(InstallationGridRowModel? x, InstallationGridRowModel? y)
    {
        if (x is null && y is null)
        {
            return 0;
        }

        if (x is null)
        {
            return 1;
        }

        if (y is null)
        {
            return -1;
        }

        if (x.IsDefault != y.IsDefault)
        {
            return x.IsDefault ? -1 : 1;
        }

        return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
    }
}
