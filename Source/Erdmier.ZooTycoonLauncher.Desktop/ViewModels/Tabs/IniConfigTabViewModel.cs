namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;

/// <summary>
///     The INI Config tab (SDD §9.3.1). Hosts either the editor or a placeholder (loading / no INI / unreadable). Each activation reloads from disk unless edits are pending, so the
///     editor reflects anything the game wrote while the launcher stayed open; a failed load is retried on the next activation.
/// </summary>
public sealed partial class IniConfigTabViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;

    private readonly bool _hasIni;

    private readonly Guid _installationId;

    private readonly IMediator? _mediator;

    private Task? _activation;

    private IniEditorViewModel? _editor;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The open installation.</param>
    /// <param name="iniErrorMessage">The boot's INI synchronisation error, or <see langword="null" />.</param>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">Chrome service for the editor's error dialogue.</param>
    public IniConfigTabViewModel(InstallationSummary installation, string? iniErrorMessage, IMediator mediator, IDialogService dialogs)
        : this(installation.Id, installation.Validity.HasIni, dialogs, InitialContent(installation.Validity.HasIni, iniErrorMessage))
        => _mediator = mediator;

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniConfigTabViewModel()
        : this(Guid.Empty, hasIni: true, new NoOpDialogService(), new IniEditorViewModel())
    { }

    private IniConfigTabViewModel(Guid installationId, bool hasIni, IDialogService dialogs, ViewModelBase content)
    {
        _installationId = installationId;
        _hasIni         = hasIni;
        _dialogs        = dialogs;

        Content = content;
    }

    /// <summary>The editor or a placeholder.</summary>
    [ ObservableProperty ]
    public partial ViewModelBase Content { get; set; }

    /// <summary>Whether the editor holds unsaved edits.</summary>
    public bool HasPendingChanges => _editor?.HasPendingChanges ?? false;

    /// <summary>Loads (or reloads) from disk when the tab becomes selected. Skipped when there is no INI or edits are pending; concurrent calls share one load.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The in-flight load, or a completed task when skipped.</returns>
    public Task ActivateAsync(CancellationToken cancellationToken)
    {
        if (!_hasIni
            || _mediator is null
            || HasPendingChanges)
        {
            return Task.CompletedTask;
        }

        if (_activation is { IsCompleted: false })
        {
            return _activation;
        }

        _activation = LoadAsync(_mediator, cancellationToken);

        return _activation;
    }

    /// <summary>Saves the editor's edits (used by the pending-changes guard).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when nothing is left unsaved.</returns>
    public Task<bool> SaveAsync(CancellationToken cancellationToken) => _editor?.TrySaveAsync(cancellationToken) ?? Task.FromResult(true);

    /// <summary>Discards the editor's edits (used by the pending-changes guard).</summary>
    public void DiscardChanges() => _editor?.DiscardChanges();

    private static ViewModelBase InitialContent(bool hasIni, string? iniErrorMessage)
        => !hasIni
               ? IniPlaceholderViewModel.NoIni()
               : iniErrorMessage is not null
                   ? IniPlaceholderViewModel.Unreadable(iniErrorMessage)
                   : IniPlaceholderViewModel.Loading();

    private async Task LoadAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        try
        {
            ErrorOr<IniConfigResult> result = await mediator.Send(new GetIniConfigQuery(_installationId), cancellationToken);

            if (result.IsError)
            {
                Content = IniPlaceholderViewModel.Unreadable(result.FirstError.Description);

                return;
            }

            if (_editor is null)
            {
                _editor = new IniEditorViewModel(_installationId, mediator, _dialogs, result.Value);

                _editor.PropertyChanged += OnEditorPropertyChanged;
            }
            else if (!_editor.HasPendingChanges)
            {
                // The user may have started editing while the reload was in flight; never overwrite those edits.
                _editor.Load(result.Value);
            }

            Content = _editor;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Content = IniPlaceholderViewModel.Unreadable(ex.Message);
        }
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(IniEditorViewModel.HasPendingChanges))
        {
            OnPropertyChanged(nameof(HasPendingChanges));
        }
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
