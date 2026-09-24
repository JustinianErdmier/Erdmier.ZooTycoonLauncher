namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>
///     The INI Config editor (SDD §9.3): a section list, the selected section's form, and a footer whose label shows the active row's help, then the unsaved-changes marker, then
///     the saved state. Save sends only the dirty rows; Revert reloads from disk.
/// </summary>
public sealed partial class IniEditorViewModel : ViewModelBase
{
    private const string ErrorTitle = "Cannot Save zoo.ini";

    private readonly IDialogService _dialogs;

    private readonly Guid _installationId;

    private readonly IMediator? _mediator;

    private IniFieldViewModel? _helpField;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installationId">The installation being edited.</param>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">Chrome service for the error dialogue.</param>
    /// <param name="initial">The values to load.</param>
    public IniEditorViewModel(Guid installationId, IMediator mediator, IDialogService dialogs, IniConfigResult initial)
        : this(installationId, dialogs, initial)
        => _mediator = mediator;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniEditorViewModel()
        : this(Guid.Empty, new NoOpDialogService(), new IniConfigResult(new Dictionary<IniKeyId, string?>(), DateTime.UtcNow))
    { }

    // Mediator-less core (the GeneralTabViewModel pattern): the designer leaves _mediator null, which disables Save and Revert.
    private IniEditorViewModel(Guid installationId, IDialogService dialogs, IniConfigResult initial)
    {
        _installationId = installationId;
        _dialogs        = dialogs;

        Sections        = IniFieldFactory.CreateSections();
        SelectedSection = Sections[0];

        foreach (IniFieldViewModel field in AllFields)
        {
            field.PropertyChanged += OnFieldPropertyChanged;
        }

        Load(initial);
    }

    /// <summary>The seven sections, in SDD §9.3 order.</summary>
    public IReadOnlyList<IniSectionViewModel> Sections { get; }

    /// <summary>The section shown in the form pane.</summary>
    [ ObservableProperty ]
    public partial IniSectionViewModel SelectedSection { get; set; }

    /// <summary><see langword="true" /> while a save or reload is in flight; disables the pane and the footer commands.</summary>
    [ ObservableProperty ]
    [ NotifyCanExecuteChangedFor(nameof(SaveCommand)) ]
    [ NotifyCanExecuteChangedFor(nameof(RevertCommand)) ]
    public partial bool IsBusy { get; set; }

    /// <summary>The file's last-write time (UTC), shown localised in the footer.</summary>
    [ ObservableProperty ]
    public partial DateTime FileLastWriteUtc { get; set; }

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

        foreach (IniFieldViewModel field in AllFields)
        {
            if (field.Id is { } id)
            {
                field.Load(result.Values.GetValueOrDefault(id));
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
    public async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        if (!HasPendingChanges)
        {
            return true;
        }

        if (_mediator is null
            || IsBusy)
        {
            // IsBusy means a save or revert is already in flight (e.g. a close arriving whilst Save is dispatched); dispatching a second SaveIniCommand here could race the
            // first, so this call reports failure without touching the mediator.
            return false;
        }

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
            ErrorOr<IniConfigResult> result = await _mediator.Send(new SaveIniCommand(_installationId, edits), cancellationToken);

            if (result.IsError)
            {
                await _dialogs.ShowErrorAsync(ErrorTitle, result.FirstError.Description);

                return false;
            }

            Load(result.Value);

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _dialogs.ShowErrorAsync(ErrorTitle, $"zoo.ini could not be saved: {ex.Message}");

            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnActiveHelpChanged(string? value) => RaiseFooterChanged();

    // A keyboard section switch can move the visual tree's focus away from the hovered/focused row without it ever raising PointerExited (or losing focus in the way
    // OnFieldPropertyChanged expects), leaving the footer's help line stuck over "● Unsaved changes"; clearing it here whenever the section changes closes that gap.
    partial void OnSelectedSectionChanged(IniSectionViewModel value)
    {
        _helpField = null;
        ActiveHelp = null;
    }

    partial void OnFileLastWriteUtcChanged(DateTime value) => RaiseFooterChanged();

    [ RelayCommand(CanExecute = nameof(CanSaveOrRevert)) ]
    private Task SaveAsync(CancellationToken cancellationToken) => TrySaveAsync(cancellationToken);

    [ RelayCommand(CanExecute = nameof(CanSaveOrRevert)) ]
    private async Task RevertAsync(CancellationToken cancellationToken)
    {
        if (_mediator is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            ErrorOr<IniConfigResult> result = await _mediator.Send(new GetIniConfigQuery(_installationId), cancellationToken);

            if (result.IsError)
            {
                await _dialogs.ShowErrorAsync(title: "Cannot Reload zoo.ini", result.FirstError.Description);

                return;
            }

            Load(result.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _dialogs.ShowErrorAsync(title: "Cannot Reload zoo.ini", $"zoo.ini could not be reloaded: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSaveOrRevert() => HasPendingChanges && !IsBusy && _mediator is not null;

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

        if (eventArgs.PropertyName != nameof(IniFieldViewModel.IsHelpActive))
        {
            return;
        }

        if (field.IsHelpActive)
        {
            _helpField = field;
            ActiveHelp = field.Help;
        }
        else if (ReferenceEquals(_helpField, field))
        {
            _helpField = null;
            ActiveHelp = null;
        }
    }

    private void RaiseDirtyChanged()
    {
        OnPropertyChanged(nameof(HasPendingChanges));

        RaiseFooterChanged();

        SaveCommand.NotifyCanExecuteChanged();
        RevertCommand.NotifyCanExecuteChanged();
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

    public Task<SaveChangesChoice> ShowSaveChangesPromptAsync() => Task.FromResult(SaveChangesChoice.Cancel);

    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
}
