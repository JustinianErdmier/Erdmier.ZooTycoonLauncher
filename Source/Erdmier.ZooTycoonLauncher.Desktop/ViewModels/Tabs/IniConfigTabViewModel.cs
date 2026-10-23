namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;

/// <summary>
///     The INI Config tab (SDD §9.3.1). Hosts either the editor or a placeholder (loading / no INI / unreadable). Each activation reloads from disk unless edits are pending, so the
///     editor reflects anything the game wrote while the launcher stayed open; a failed load is retried on the next activation. When a reload fails while the editor already
///     holds pending edits, the editor stays on screen (never dropping unsaved work) and the failure is shown as an error dialogue instead.
/// </summary>
public sealed partial class IniConfigTabViewModel : ViewModelBase, IDisposable
{
    private readonly IDialogService _dialogs;

    private readonly ILogger<IniEditorViewModel> _editorLogger;

    private readonly bool _hasIni;

    private readonly IIniEditorStyleState _styleState;

    private readonly Guid _installationId;

    private readonly ILogger<IniConfigTabViewModel> _logger;

    private readonly IMediator? _mediator;

    private Task? _activation;

    private bool _hasLoadedStyle;

    private IniEditorViewModel? _editor;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installation">The open installation.</param>
    /// <param name="iniErrorMessage">The boot's INI synchronisation error, or <see langword="null" />.</param>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="dialogs">Chrome service for the editor's error dialogue.</param>
    /// <param name="logger">Logger for load failures.</param>
    /// <param name="editorLogger">Logger handed to the editor this tab creates.</param>
    /// <param name="placeholderLogger">Logger handed to the No INI placeholder's Create / Locate actions.</param>
    /// <param name="reopenOnIniTabAsync">Re-boots the open installation and reopens on this tab (called after a successful Create / Locate).</param>
    /// <param name="styleState">The shared INI editor style; the editor this tab creates follows it live.</param>
    /// <param name="writeAccess">The boot's write-access probe result; a read-only result shows the warning strip above the tab's content.</param>
    public IniConfigTabViewModel(InstallationSummary              installation,
                                 string?                          iniErrorMessage,
                                 IMediator                        mediator,
                                 IDialogService                   dialogs,
                                 ILogger<IniConfigTabViewModel>   logger,
                                 ILogger<IniEditorViewModel>      editorLogger,
                                 ILogger<IniPlaceholderViewModel> placeholderLogger,
                                 Func<CancellationToken, Task>    reopenOnIniTabAsync,
                                 IIniEditorStyleState             styleState,
                                 InstallationWriteAccess          writeAccess)
        : this(installation.Id,
               installation.Validity.HasIni,
               dialogs,
               styleState,
               logger,
               editorLogger,
               InitialContent(installation, iniErrorMessage, mediator, dialogs, reopenOnIniTabAsync, placeholderLogger))
    {
        _mediator = mediator;

        ReadOnlyWarning = IniReadOnlyText.For(writeAccess);
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniConfigTabViewModel()
        : this(Guid.Empty,
               hasIni: true,
               new NoOpDialogService(),
               new IniEditorStyleState(),
               NullLogger<IniConfigTabViewModel>.Instance,
               NullLogger<IniEditorViewModel>.Instance,
               new IniEditorViewModel())
    { }

    private IniConfigTabViewModel(Guid                           installationId,
                                  bool                           hasIni,
                                  IDialogService                 dialogs,
                                  IIniEditorStyleState           styleState,
                                  ILogger<IniConfigTabViewModel> logger,
                                  ILogger<IniEditorViewModel>    editorLogger,
                                  ViewModelBase                  content)
    {
        _installationId = installationId;
        _hasIni         = hasIni;
        _dialogs        = dialogs;
        _styleState     = styleState;
        _logger         = logger;
        _editorLogger   = editorLogger;

        Content = content;

        _styleState.StyleChanged += OnStyleChanged;
    }

    /// <summary>The editor or a placeholder.</summary>
    [ ObservableProperty ]
    public partial ViewModelBase Content { get; private set; }

    /// <summary>The read-only warning shown above the tab's content, or <see langword="null" /> when the installation is writable or its access is unknown.</summary>
    public string? ReadOnlyWarning { get; }

    /// <summary><see langword="true" /> when <see cref="ReadOnlyWarning" /> has text to show.</summary>
    public bool HasReadOnlyWarning => ReadOnlyWarning is not null;

    /// <summary>Whether the editor holds unsaved edits.</summary>
    public bool HasPendingChanges => _editor?.HasPendingChanges ?? false;

    /// <summary>
    ///     Loads (or reloads) from disk when the tab becomes selected. Skipped (a completed task is returned) when the installation has no INI, when edits are pending, or when
    ///     there is no mediator (the designer). Concurrent calls share one load, which no caller can cancel; each caller's token cancels only its own wait.
    /// </summary>
    /// <param name="cancellationToken">Cancels this caller's wait for the load, not the load itself.</param>
    /// <returns>A task that completes with the shared load, or a completed task when skipped.</returns>
    public Task ActivateAsync(CancellationToken cancellationToken)
    {
        if (!_hasIni
            || _mediator is null
            || HasPendingChanges)
        {
            return Task.CompletedTask;
        }

        if (_activation is not { IsCompleted: false })
        {
            // The load is shared, so it must not carry the first caller's token: that caller cancelling would fail every later caller's wait.
            _activation = LoadAsync(_mediator, CancellationToken.None);
        }

        return _activation.WaitAsync(cancellationToken);
    }

    /// <summary>Stops following the shared editor style, so a tab the launcher has navigated away from is no longer re-rendered by Options.</summary>
    public void Dispose() => _styleState.StyleChanged -= OnStyleChanged;

    /// <summary>Saves the editor's edits (used by the pending-changes guard).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when nothing is left unsaved.</returns>
    public Task<bool> SaveAsync(CancellationToken cancellationToken) => _editor?.TrySaveAsync(cancellationToken) ?? Task.FromResult(true);

    /// <summary>Discards the editor's edits (used by the pending-changes guard).</summary>
    public void DiscardChanges() => _editor?.DiscardChanges();

    private static ViewModelBase InitialContent(InstallationSummary              installation,
                                                string?                          iniErrorMessage,
                                                IMediator                        mediator,
                                                IDialogService                   dialogs,
                                                Func<CancellationToken, Task>    reopenOnIniTabAsync,
                                                ILogger<IniPlaceholderViewModel> placeholderLogger)
        => !installation.Validity.HasIni
               ? IniPlaceholderViewModel.NoIni(installation.Id, installation.Path, installation.Validity.HasExe, mediator, dialogs, reopenOnIniTabAsync, placeholderLogger)
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
                _logger.LogWarning(message: "The INI Config tab could not load zoo.ini for installation {InstallationId}: {Error}", _installationId, result.FirstError.Description);

                await ReportUnreadableAsync(placeholderMessage: result.FirstError.Description, dialogDescription: result.FirstError.Description);

                return;
            }

            if (_editor is null)
            {
                await EnsureStyleLoadedAsync(mediator, cancellationToken);

                _editor = new IniEditorViewModel(_installationId, mediator, _dialogs, result.Value, _editorLogger, _styleState.Style);

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
            _logger.LogError(ex, message: "The INI Config tab failed to load zoo.ini for installation {InstallationId}", _installationId);

            await ReportUnreadableAsync(placeholderMessage: ex.Message, dialogDescription: $"zoo.ini could not be reloaded: {ex.Message}");
        }
    }

    // The boot does not load the launcher options, so the first editor of this tab reads the stored style itself, once, before it is built. A failure keeps the style already in
    // force: the editor is still usable in either style, and the options dialogue reports its own errors.
    private async Task EnsureStyleLoadedAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        if (_hasLoadedStyle)
        {
            return;
        }

        _hasLoadedStyle = true;

        try
        {
            ErrorOr<LauncherOptions> options = await mediator.Send(new GetLauncherOptionsQuery(), cancellationToken);

            if (!options.IsError
                && options.Value is { } value)
            {
                _styleState.Style = value.EditorStyle;
            }
            else if (options.IsError)
            {
                _logger.LogWarning(message: "The INI Config tab could not read the editor style: {Error}", options.FirstError.Description);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, message: "The INI Config tab could not read the editor style");
        }
    }

    // A style change from Options re-renders the open editor in place: no command, no reload; the editor carries its pending edits across.
    private void OnStyleChanged(object? sender, EventArgs eventArgs) => _editor?.ApplyStyle(_styleState.Style);

    // A reload can fail while the editor already holds pending edits (the user started typing while the reload was in flight); dropping the editor for the placeholder would
    // silently discard that work, so the editor stays on screen and the failure is surfaced as a dialogue instead.
    private async Task ReportUnreadableAsync(string placeholderMessage, string dialogDescription)
    {
        if (_editor is { HasPendingChanges: true })
        {
            Content = _editor;

            await _dialogs.ShowErrorAsync(title: "Cannot Reload zoo.ini", dialogDescription);

            return;
        }

        Content = IniPlaceholderViewModel.Unreadable(placeholderMessage);
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

    public Task<string?> PickFileAsync(string? startPath, string title) => Task.FromResult<string?>(result: null);

    public Task<SaveChangesChoice> ShowSaveChangesPromptAsync() => Task.FromResult(SaveChangesChoice.Cancel);

    public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(false);

    public Task<IniConfigResult?> ShowRestorePreviousIniAsync(Guid installationId) => Task.FromResult<IniConfigResult?>(null);

    public Task ShowOptionsAsync() => Task.CompletedTask;

    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
}
