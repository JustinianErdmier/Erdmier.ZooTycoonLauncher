namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     View model for the Restore Previous INI dialogue: lists the installation's INI history newest first, shows the selected snapshot's differences against the current
///     values, and restores the chosen snapshot after a confirmation.
/// </summary>
public sealed partial class RestorePreviousIniDialogViewModel : ViewModelBase
{
    private const string NotSet = "(not set)";

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo(name: "en-GB");

    private readonly IDialogService _dialogs;

    private readonly ILogger<RestorePreviousIniDialogViewModel> _logger;

    private readonly IMediator _mediator;

    private readonly IIniEditorStyleState _styleState;

    private Guid _installationId;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">The dialogue service, used for the confirmation and error prompts.</param>
    /// <param name="styleState">The INI editor style, read when the difference rows are built.</param>
    /// <param name="logger">Logger for history-load and restore failures.</param>
    public RestorePreviousIniDialogViewModel(IMediator mediator, IDialogService dialogs, IIniEditorStyleState styleState, ILogger<RestorePreviousIniDialogViewModel> logger)
    {
        _mediator   = mediator;
        _dialogs    = dialogs;
        _styleState = styleState;
        _logger     = logger;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public RestorePreviousIniDialogViewModel()
        : this(null!, null!, new NullIniEditorStyleState(), NullLogger<RestorePreviousIniDialogViewModel>.Instance)
    { }

    /// <summary>Whether a user-initiated close (the title-bar button) is allowed: <see langword="false" /> while a restore is in flight. It is already <see langword="true" /> by the time <see cref="CloseRequested" /> is raised.</summary>
    public bool CanClose => !IsBusy;

    /// <summary>The differing settings of the selected snapshot, against the current values.</summary>
    [ ObservableProperty ]
    public partial IReadOnlyList<IniHistoryDiffRow> DiffRows { get; set; } = [ ];

    /// <summary>The load error, or <see langword="null" /> when the history loaded.</summary>
    [ ObservableProperty ]
    [ NotifyPropertyChangedFor(nameof(HasError)) ]
    public partial string? ErrorMessage { get; set; }

    /// <summary>Whether the history failed to load; the caption and the difference area are hidden then.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>Whether the selected snapshot differs from the current values in no setting.</summary>
    public bool HasNoDifferences => SelectedRow is not null && DiffRows.Count == 0;

    /// <summary>Whether the selected snapshot lacks keys that exist in the current file.</summary>
    public bool HasKeptKeys => SelectedRow is { Entry.Comparison.KeptKeys.Count: > 0 };

    /// <summary>Whether a restore is in progress.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(RestoreCommand)) ]
    [ NotifyCanExecuteChangedFor(nameof(CancelCommand)) ]
    [ NotifyPropertyChangedFor(nameof(CanClose)) ]
    public partial bool IsBusy { get; set; }

    /// <summary>The keys the selected snapshot lacks, which keep their current values; empty when there are none.</summary>
    public string KeptKeysText
        => SelectedRow is { Entry.Comparison.KeptKeys: { Count: > 0 } keys }
            ? "Not in this snapshot, so they keep their current values: " + string.Join(separator: ", ", keys.Select(key => IniDisplayText.KeyText(_styleState.Style, key)))
            : string.Empty;

    /// <summary>The caption above the difference grid, for example <c>2 changes</c>.</summary>
    public string ChangeCountText
        => DiffRows.Count == 1
            ? "1 change"
            : $"{DiffRows.Count} changes";

    /// <summary>The restored configuration after a successful restore; <see langword="null" /> otherwise.</summary>
    public IniConfigResult? Result { get; private set; }

    /// <summary>The history lines, newest first (the <c>Original</c> snapshot last).</summary>
    [ ObservableProperty ]
    public partial IReadOnlyList<IniHistoryRow> Rows { get; set; } = [ ];

    /// <summary>The selected history line.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(RestoreCommand)) ]
    public partial IniHistoryRow? SelectedRow { get; set; }

    /// <summary>Loads the history and selects the newest row. Must be awaited by the dialogue service before the window is shown.</summary>
    /// <param name="installationId">The installation whose history to show.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task InitialiseAsync(Guid installationId, CancellationToken cancellationToken = default)
    {
        _installationId = installationId;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_mediator is null)
        {
            return;
        }

        ErrorOr<IniHistoryResult> history;

        try
        {
            history = await _mediator.Send(new GetIniHistoryQuery(installationId), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, message: "Loading the INI history for installation {InstallationId} threw", installationId);

            ErrorMessage = $"Restore Previous INI could not load the history of zoo.ini: {ex.Message}";
            Rows         = [ ];

            return;
        }

        if (history.IsError)
        {
            _logger.LogWarning(message: "Loading the INI history for installation {InstallationId} failed: {Error}", installationId, history.FirstError.Description);

            ErrorMessage = history.FirstError.Description;
            Rows         = [ ];

            return;
        }

        ErrorMessage = null;
        Rows         = history.Value.Entries.Select(ToRow).ToList();
        SelectedRow  = Rows.Count > 0 ? Rows[0] : null;
    }

    /// <summary>Closes the dialogue without restoring anything. Unavailable while a restore is in progress.</summary>
    [ RelayCommand(CanExecute = nameof(CanCancel)) ]
    private void Cancel()
    {
        // Execute() does not consult CanExecute, so the guard is repeated here.
        if (IsBusy)
        {
            return;
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanCancel() => !IsBusy;

    private bool CanRestore() => !IsBusy && SelectedRow is { Entry.Comparison.Rows.Count: > 0 };

    [ RelayCommand(CanExecute = nameof(CanRestore)) ]
    private async Task RestoreAsync()
    {
        IniHistoryRow? row = SelectedRow;

        // Execute() does not consult CanExecute, so the guard is repeated here.
        if (IsBusy || row is null)
        {
            return;
        }

        // Busy from the confirmation onwards, so a second press cannot stack a second prompt or send a second command.
        IsBusy = true;

        try
        {
            bool confirmed = await _dialogs.ShowConfirmationAsync(title: "Restore Previous INI", ConfirmationMessage(row));

            if (!confirmed)
            {
                return;
            }

            ErrorOr<IniConfigResult> restored = await _mediator.Send(new RestoreIniSnapshotCommand(_installationId, row.Entry.SnapshotId));

            if (restored.IsError)
            {
                _logger.LogWarning(message: "Restoring zoo.ini for installation {InstallationId} failed: {Error}", _installationId, restored.FirstError.Description);

                await _dialogs.ShowErrorAsync(title: "Cannot Restore zoo.ini", restored.FirstError.Description);

                return;
            }

            Result = restored.Value;

            // Release the busy flag first so CanClose is true when the window's Closing handler runs in response to CloseRequested.
            IsBusy = false;

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, message: "Restoring zoo.ini for installation {InstallationId} threw", _installationId);

            await _dialogs.ShowErrorAsync(title: "Cannot Restore zoo.ini", $"zoo.ini could not be restored: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string ConfirmationMessage(IniHistoryRow row)
    {
        string message = $"Restore zoo.ini to the version captured {row.CapturedText}? The current version is archived to history first, so this can be undone.";

        int kept = row.Entry.Comparison.KeptKeys.Count;

        return kept switch
        {
            0 => message,
            1 => message + " 1 setting is not in this snapshot and will keep its current value.",
            _ => message + $" {kept} settings are not in this snapshot and will keep their current values."
        };
    }

    private static IniHistoryRow ToRow(IniHistoryEntry entry)
        => new(entry,
               entry.CapturedUtc.ToLocalTime().ToString(format: "d MMM yyyy HH:mm", DisplayCulture),
               entry.Trigger.Name,
               entry.Comparison.Rows.Count.ToString(DisplayCulture));

    private static IniHistoryDiffRow ToDiffRow(IniEditorStyle style, IniDiffRow row)
    {
        // Friendly style names the whole key in one text; a key the Friendly layout does not place reads as in INI style.
        bool named = style == IniEditorStyle.Friendly && IniEditorLayouts.TryLocate(style, row.Id, out _);

        return new IniHistoryDiffRow(named ? string.Empty : $"[{row.Id.Section}]",
                                     named ? IniDisplayText.KeyText(style, row.Id) : row.Id.Key,
                                     row.Current is null ? NotSet : IniDisplayText.Format(style, row.Id, row.Current),
                                     IniDisplayText.Format(style, row.Id, row.Snapshot))
        {
            UsesSystemFace = named
        };
    }

    private IReadOnlyList<IniHistoryDiffRow> ToDiffRows(IniSnapshotComparison comparison)
    {
        IniEditorStyle style = _styleState.Style;

        // One row per diff row in both styles; Friendly follows the page order (keys the layout does not place keep the diff's order, last).
        IEnumerable<IniDiffRow> rows = style == IniEditorStyle.Friendly
            ? comparison.Rows.OrderBy(row => IniEditorLayouts.TryLocate(style, row.Id, out IniFieldLocation? location) ? location.Order : int.MaxValue)
            : comparison.Rows;

        return rows.Select(row => ToDiffRow(style, row)).ToList();
    }

    partial void OnSelectedRowChanged(IniHistoryRow? value)
    {
        DiffRows = value is null ? [ ] : ToDiffRows(value.Entry.Comparison);

        OnPropertyChanged(nameof(ChangeCountText));
        OnPropertyChanged(nameof(HasKeptKeys));
        OnPropertyChanged(nameof(HasNoDifferences));
        OnPropertyChanged(nameof(KeptKeysText));
    }

    /// <summary>Raised when the dialogue should close.</summary>
    public event EventHandler? CloseRequested;
}

file sealed class NullIniEditorStyleState : IIniEditorStyleState
{
    public IniEditorStyle Style { get; set; } = IniEditorStyle.Friendly;

    public int Version => 0;

    public event EventHandler? StyleChanged
    {
        add { }
        remove { }
    }
}