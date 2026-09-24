using System.Collections.ObjectModel;

namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common;

/// <summary>
///     Shared view model for the installation <c>DataGrid</c> (SDD §9.4, §9.6). Owns the row collection, selected row, and data loading via
///     <see cref="GetAllInstallationsQuery" />. Subscribes to installation messenger messages so the grid refreshes automatically once the Application
///     handlers begin publishing them.
/// </summary>
public sealed partial class InstallationGridViewModel : ViewModelBase,
                                                        IDisposable,
                                                        IRecipient<InstallationAddedMessage>,
                                                        IRecipient<InstallationChangedMessage>,
                                                        IRecipient<InstallationDeletedMessage>,
                                                        IRecipient<DefaultInstallationChangedMessage>
{
    private readonly IMediator _mediator;

    private readonly IMessenger _messenger;

    private bool _disposed;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher — used to issue <see cref="GetAllInstallationsQuery" />.</param>
    /// <param name="messenger">The CommunityToolkit messenger — used to subscribe to installation-change notifications.</param>
    public InstallationGridViewModel(IMediator mediator, IMessenger messenger)
    {
        _mediator  = mediator;
        _messenger = messenger;

        _messenger.RegisterAll(this);
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public InstallationGridViewModel()
        : this(null!, WeakReferenceMessenger.Default)
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

    // Marshals the reload onto the UI thread rather than calling LoadAsync() directly, so a future off-thread publisher of these messages cannot mutate Rows off the UI
    // thread. Nothing publishes these messages yet, but Receive must not rely on a future publisher always raising on the UI thread.
    private void ScheduleReload()
        => Dispatcher.UIThread.Post(() =>
        {
            if (!_disposed)
            {
                _ = LoadAsync();
            }
        });
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
