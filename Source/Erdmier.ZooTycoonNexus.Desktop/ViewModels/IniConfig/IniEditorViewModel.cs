namespace Erdmier.ZooTycoonNexus.Desktop.ViewModels.IniConfig;

/// <summary>
///     The Game Config editor (SDD §9.3): a section list, the selected section's form, and a footer whose label shows the active row's help, then the unsaved-changes marker, then
///     the saved state. Save sends only the dirty rows; Reset reloads from disk.
/// </summary>
public sealed partial class IniEditorViewModel : ViewModelBase
{
    private const string ErrorTitle = "Cannot Save zoo.ini";

    private readonly IDialogService _dialogs;

    private readonly Guid _installationId;

    private readonly ILogger<IniEditorViewModel> _logger;

    private readonly IMediator? _mediator;

    private IniFieldViewModel? _focusedField;

    private IniFieldViewModel? _hoveredField;

    private Task<bool>? _pendingSave;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installationId">The installation being edited.</param>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">Chrome service for the error dialogue.</param>
    /// <param name="initial">The values to load.</param>
    /// <param name="logger">Logger for save, reload and restore failures.</param>
    /// <param name="style">The style to render in: the INI sections or the Friendly pages.</param>
    public IniEditorViewModel(Guid                        installationId,
                              IMediator                   mediator,
                              IDialogService              dialogs,
                              IniConfigResult             initial,
                              ILogger<IniEditorViewModel> logger,
                              IniEditorStyle              style)
        : this(installationId, dialogs, initial, logger, style)
        => _mediator = mediator;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniEditorViewModel()
        : this(Guid.Empty,
               new NoOpDialogService(),
               new IniConfigResult(new Dictionary<IniKeyId, string?>(), DateTime.UtcNow, HistoricalCount: 0),
               NullLogger<IniEditorViewModel>.Instance,
               IniEditorStyle.Ini)
    { }

    // Mediator-less core (the GeneralTabViewModel pattern): the designer leaves _mediator null, which disables Save, Reset and Restore defaults.
    private IniEditorViewModel(Guid installationId, IDialogService dialogs, IniConfigResult initial, ILogger<IniEditorViewModel> logger, IniEditorStyle style)
    {
        _installationId = installationId;
        _dialogs        = dialogs;
        _logger         = logger;

        Sections        = IniFieldFactory.CreateSections(style);
        SelectedSection = Sections[0];

        WireFields();

        Load(initial);
    }

    /// <summary>The seven INI sections (SDD §9.3 order) or the eight Friendly pages (spec §6.1 order).</summary>
    [ ObservableProperty ]
    public partial IReadOnlyList<IniSectionViewModel> Sections { get; private set; }

    /// <summary>The section shown in the form pane.</summary>
    [ ObservableProperty ]
    public partial IniSectionViewModel SelectedSection { get; set; }

    /// <summary><see langword="true" /> while a save or reload is in flight; disables the pane and the footer commands.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    [ NotifyCanExecuteChangedFor(nameof(ResetCommand)) ]
    [ NotifyCanExecuteChangedFor(nameof(RestoreDefaultsCommand)) ]
    [ NotifyCanExecuteChangedFor(nameof(RestorePreviousCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary>The file's last-write time (UTC), shown localised in the footer.</summary>
    [ ObservableProperty ]
    public partial DateTime FileLastWriteUtc { get; set; }

    /// <summary>How many historical snapshots the installation has; Restore previous is available only when this is above zero.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(RestorePreviousCommand)) ]
    public partial int HistoricalCount { get; set; }

    /// <summary>The hovered or focused row's help, or <see langword="null" />.</summary>
    [ ObservableProperty ]
    public partial string? ActiveHelp { get; set; }

    /// <summary>Whether any row has an edit.</summary>
    public bool HasPendingChanges => Sections.Any(section => section.IsDirty);

    /// <summary>The footer label's text.</summary>
    public string FooterText
        => ActiveHelp
           ?? (HasPendingChanges
                   ? "● Unsaved changes"
                   : $"All changes saved · Last write: {FileLastWriteUtc.ToLocalTime().ToString(format: "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}");

    /// <summary>Whether the footer shows help (italic).</summary>
    public bool IsFooterHelp => ActiveHelp is not null;

    /// <summary>Whether the footer shows the unsaved-changes marker (maroon, bold).</summary>
    public bool IsFooterDirty => ActiveHelp is null && HasPendingChanges;

    /// <summary>Whether the footer shows the saved state (muted).</summary>
    public bool IsFooterSaved => ActiveHelp is null && !HasPendingChanges;

    private IEnumerable<IniFieldViewModel> AllFields => Sections.SelectMany(section => section.Fields);

    /// <summary>Loads values into every row (resetting edits) and updates the last-write time.</summary>
    /// <param name="result">The values from <c>GetIniConfigQuery</c> or <c>SaveIniCommand</c>.</param>
    public void Load(IniConfigResult result)
    {
        FileLastWriteUtc = result.FileLastWriteUtc;
        HistoricalCount  = result.HistoricalCount;

        foreach (IniFieldViewModel field in AllFields)
        {
            if (field.Id is { } id)
            {
                field.Load(result.Values.GetValueOrDefault(id));
            }
        }

        RaiseDirtyChanged();
    }

    /// <summary>
    ///     Re-renders the editor in another style without touching the file or sending a command. Each keyed row's baseline and pending edit are captured by key, the sections
    ///     are rebuilt from the new layout, the baselines are loaded and the edits re-applied, so the editor stays dirty exactly as before (spec §7). The first section or page
    ///     is then selected (spec C7), and any hover or focus held by the old rows is dropped.
    /// </summary>
    /// <param name="style">The style to render in.</param>
    public void ApplyStyle(IniEditorStyle style)
    {
        Dictionary<IniKeyId, (string? Baseline, string? Edit)> captured = [];

        foreach (IniKeyedFieldViewModel field in AllFields.OfType<IniKeyedFieldViewModel>())
        {
            if (field.Id is { } id)
            {
                captured[id] = (field.Baseline, field.TryGetEdit(out string? raw) ? raw : null);
            }
        }

        foreach (IniFieldViewModel field in AllFields)
        {
            field.PropertyChanged -= OnFieldPropertyChanged;
        }

        // The old rows leave the tree with the old sections; their hover and focus must not linger in the footer.
        ClearActiveRow();

        Sections        = IniFieldFactory.CreateSections(style);
        SelectedSection = Sections[0];

        WireFields();

        foreach (IniKeyedFieldViewModel field in AllFields.OfType<IniKeyedFieldViewModel>())
        {
            if (field.Id is not { } id
                || !captured.TryGetValue(id, out (string? Baseline, string? Edit) state))
            {
                continue;
            }

            field.Load(state.Baseline);

            if (state.Edit is { } edit)
            {
                field.ApplyEdit(edit);
            }
        }

        RaiseDirtyChanged();
    }

    /// <summary>Discards every edit, returning each row to its baseline.</summary>
    public void DiscardChanges()
    {
        foreach (IniFieldViewModel field in AllFields)
        {
            field.Reset();
        }

        RaiseDirtyChanged();
    }

    /// <summary>Saves the dirty rows. On failure shows the error dialogue and keeps the edits.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when nothing is left unsaved.</returns>
    public Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        if (!HasPendingChanges)
        {
            return Task.FromResult(true);
        }

        if (_pendingSave is { } inFlight)
        {
            // A second call (e.g. a guard's "Yes" arriving whilst Save is already dispatched) awaits the same save instead of sending a second SaveIniCommand; the
            // first call's outcome decides for both.
            return inFlight;
        }

        if (_mediator is not { } mediator
            || IsBusy)
        {
            // IsBusy without a _pendingSave means a reset is already in flight; a save cannot safely race it, so this call reports failure without touching the mediator.
            return Task.FromResult(false);
        }

        Task<bool> save = RunSaveAsync(mediator, cancellationToken);

        // RunSaveAsync clears _pendingSave in its finally block; a save that completed synchronously has already run that block, so recording it here would leave a
        // finished task behind for every later call to return.
        _pendingSave = save.IsCompleted ? null : save;

        return save;
    }

    private async Task<bool> RunSaveAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        Dictionary<IniKeyId, string> edits = [];

        foreach (IniFieldViewModel field in AllFields)
        {
            if (field.Id is { } id
                && field.TryGetEdit(out string? raw))
            {
                edits[id] = raw;
            }
        }

        IsBusy = true;

        try
        {
            ErrorOr<IniConfigResult> result = await mediator.Send(new SaveIniCommand(_installationId, edits), cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning(message: "Saving zoo.ini for installation {InstallationId} failed: {Error}", _installationId, result.FirstError.Description);

                await _dialogs.ShowErrorAsync(ErrorTitle, result.FirstError.Description);

                return false;
            }

            Load(result.Value);

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, message: "Saving zoo.ini for installation {InstallationId} threw", _installationId);

            await _dialogs.ShowErrorAsync(ErrorTitle, $"zoo.ini could not be saved: {ex.Message}");

            return false;
        }
        finally
        {
            IsBusy       = false;
            _pendingSave = null;
        }
    }

    partial void OnActiveHelpChanged(string? value) => RaiseFooterChanged();

    // A keyboard section switch can move the visual tree's focus away from the hovered/focused row without it ever raising PointerExited (or losing focus in the way
    // OnFieldPropertyChanged expects), leaving the footer's help line stuck over "● Unsaved changes"; clearing it here whenever the section changes closes that gap.
    partial void OnSelectedSectionChanged(IniSectionViewModel value) => ClearActiveRow();

    private void WireFields()
    {
        foreach (IniFieldViewModel field in AllFields)
        {
            field.PropertyChanged += OnFieldPropertyChanged;
        }
    }

    private void ClearActiveRow()
    {
        // Otherwise the old row's flags stay true, and the first hover or focus on that row after switching back raises no change (its value already matches), so
        // the footer never picks the help back up.
        if (_hoveredField is not null)
        {
            _hoveredField.IsHovered = false;
        }

        if (_focusedField is not null)
        {
            _focusedField.IsFocused = false;
        }

        _hoveredField = null;
        _focusedField = null;
        ActiveHelp    = null;
    }

    partial void OnFileLastWriteUtcChanged(DateTime value) => RaiseFooterChanged();

    [ RelayCommand(CanExecute = nameof(CanSaveOrReset)) ]
    private Task SaveAsync(CancellationToken cancellationToken) => TrySaveAsync(cancellationToken);

    [ RelayCommand(CanExecute = nameof(CanSaveOrReset)) ]
    private Task ResetAsync(CancellationToken cancellationToken) => ReloadAsync(cancellationToken);

    // Reloads the form from disk (Reset, and a cancelled Restore previous, which may have changed nothing but must not leave stale state).
    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        if (_mediator is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            await ReloadCoreAsync(_mediator, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // The reload itself, without touching IsBusy, so a caller that already holds the busy flag (Restore previous) can reuse it.
    private async Task ReloadCoreAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        try
        {
            ErrorOr<IniConfigResult> result = await mediator.Send(new GetIniConfigQuery(_installationId), cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning(message: "Reloading zoo.ini for installation {InstallationId} failed: {Error}", _installationId, result.FirstError.Description);

                await _dialogs.ShowErrorAsync(title: "Cannot Reload zoo.ini", result.FirstError.Description);

                return;
            }

            Load(result.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, message: "Reloading zoo.ini for installation {InstallationId} threw", _installationId);

            await _dialogs.ShowErrorAsync(title: "Cannot Reload zoo.ini", $"zoo.ini could not be reloaded: {ex.Message}");
        }
    }

    // Opens the Restore Previous INI dialogue behind the standard unsaved-changes prompt, then reloads the editor from whatever the dialogue left on disk.
    [ RelayCommand(CanExecute = nameof(CanRestorePrevious)) ]
    private async Task RestorePreviousAsync(CancellationToken cancellationToken)
    {
        if (HasPendingChanges)
        {
            switch (await _dialogs.ShowSaveChangesPromptAsync())
            {
                case SaveChangesChoice.Yes:
                    if (!await TrySaveAsync(cancellationToken))
                    {
                        return;
                    }

                    break;

                case SaveChangesChoice.No:
                    DiscardChanges();

                    break;

                default:
                    return;
            }
        }

        if (_mediator is not { } mediator)
        {
            return;
        }

        // Busy from here until the final load, so the form cannot be edited (and then overwritten by the load) while the history or the dialogue is open.
        IsBusy = true;

        try
        {
            IniConfigResult? restored = await _dialogs.ShowRestorePreviousIniAsync(_installationId);

            if (restored is not null)
            {
                Load(restored);

                return;
            }

            await ReloadCoreAsync(mediator, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, message: "Opening Restore Previous INI for installation {InstallationId} threw", _installationId);

            await _dialogs.ShowErrorAsync(title: "Cannot Restore zoo.ini", $"Restore Previous INI could not be opened: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRestorePrevious() => !IsBusy && _mediator is not null && HistoricalCount > 0;

    private bool CanSaveOrReset() => HasPendingChanges && !IsBusy && _mediator is not null;

    // Loads every setting's default into the form as unsaved edits; nothing is written until Save.
    [ RelayCommand(CanExecute = nameof(CanRestoreDefaults)) ]
    private void RestoreDefaults()
    {
        foreach (IniFieldViewModel field in AllFields)
        {
            field.ResetToDefault();
        }

        RaiseDirtyChanged();
    }

    private bool CanRestoreDefaults() => !IsBusy && _mediator is not null && AllFields.Any(field => !field.IsAtDefault);

    private void OnFieldPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (sender is not IniFieldViewModel field)
        {
            return;
        }

        if (eventArgs.PropertyName == nameof(IniFieldViewModel.IsDirty))
        {
            RaiseDirtyChanged();

            return;
        }

        if (eventArgs.PropertyName == nameof(IniFieldViewModel.IsHovered))
        {
            _hoveredField = field.IsHovered ? field : ReferenceEquals(_hoveredField, field) ? null : _hoveredField;
        }
        else if (eventArgs.PropertyName == nameof(IniFieldViewModel.IsFocused))
        {
            _focusedField = field.IsFocused ? field : ReferenceEquals(_focusedField, field) ? null : _focusedField;
        }
        else
        {
            return;
        }

        // The hovered row's help wins; with nothing hovered the focused row's shows; with neither the footer falls back to its dirty/saved state.
        ActiveHelp = (_hoveredField ?? _focusedField)?.Help;
    }

    private void RaiseDirtyChanged()
    {
        OnPropertyChanged(nameof(HasPendingChanges));

        RaiseFooterChanged();

        SaveCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();
        RestoreDefaultsCommand.NotifyCanExecuteChanged();
        RestorePreviousCommand.NotifyCanExecuteChanged();
    }

    private void RaiseFooterChanged()
    {
        OnPropertyChanged(nameof(FooterText));
        OnPropertyChanged(nameof(IsFooterHelp));
        OnPropertyChanged(nameof(IsFooterDirty));
        OnPropertyChanged(nameof(IsFooterSaved));
    }
}

file sealed class NoOpDialogService : IDialogService
{
    public void ShowLaunchError(string message)
    { }

    public Task<AddInstallationResult?> ShowAddInstallationAsync(string? prefilledPath) => Task.FromResult<AddInstallationResult?>(result: null);

    public Task<bool> ShowInstallationManagerAsync() => Task.FromResult(false);

    public Task<bool> ShowEditInstallationAsync(Guid installationId) => Task.FromResult(false);

    public Task ShowInstallationInfoAsync(Guid installationId) => Task.CompletedTask;

    public Task<bool> ShowDeleteInstallationAsync(Guid installationId) => Task.FromResult(false);

    public Task<bool> ShowFixInstallationAsync(Guid installationId) => Task.FromResult(false);

    public Task<string?> PickFolderAsync(string? startPath) => Task.FromResult<string?>(result: null);

    public Task<string?> PickFileAsync(string? startPath, string title) => Task.FromResult<string?>(result: null);

    public Task<SaveChangesChoice> ShowSaveChangesPromptAsync() => Task.FromResult(SaveChangesChoice.Cancel);

    public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(false);

    public Task<IniConfigResult?> ShowRestorePreviousIniAsync(Guid installationId) => Task.FromResult<IniConfigResult?>(null);

    public Task ShowOptionsAsync() => Task.CompletedTask;

    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
}
