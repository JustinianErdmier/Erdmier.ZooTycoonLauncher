namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>
///     View model for the read-only Installation Info modal (SDD §7.2.6, §9.5): name, path, status, default flag, and the added / last-opened / last-played timestamps
///     in the user's local time. History entries shows "—" until the INI Config slice writes snapshots.
/// </summary>
public sealed partial class InstallationInfoDialogViewModel : ViewModelBase
{
    private const string NotAvailable = "—";

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo(name: "en-GB");

    private readonly ILogger<InstallationInfoDialogViewModel> _logger;

    private readonly IMediator _mediator;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="mediator">The Mediator dispatcher.</param>
    /// <param name="logger">Logger for unexpected load failures.</param>
    public InstallationInfoDialogViewModel(IMediator mediator, ILogger<InstallationInfoDialogViewModel> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public InstallationInfoDialogViewModel()
        : this(null!, NullLogger<InstallationInfoDialogViewModel>.Instance)
    { }

    /// <summary>When the installation was added, in local time; "—" until loaded.</summary>
    [ ObservableProperty ]
    public partial string AddedText { get; set; } = NotAvailable;

    /// <summary>"Yes" when the installation is the launcher default, otherwise "No"; "—" until loaded.</summary>
    [ ObservableProperty ]
    public partial string DefaultText { get; set; } = NotAvailable;

    /// <summary>The load error, or <see langword="null" /> when the installation loaded.</summary>
    [ ObservableProperty ]
    public partial string? ErrorMessage { get; set; }

    /// <summary>Always "—": INI history is not recorded until the INI Config slice.</summary>
    public string HistoryEntriesText => NotAvailable;

    /// <summary>When the installation last became the active installation, in local time, or "—".</summary>
    [ ObservableProperty ]
    public partial string LastOpenedText { get; set; } = NotAvailable;

    /// <summary>When the game was last launched from this installation, in local time, or "—".</summary>
    [ ObservableProperty ]
    public partial string LastPlayedText { get; set; } = NotAvailable;

    /// <summary>The installation's name; "—" until loaded.</summary>
    [ ObservableProperty ]
    public partial string Name { get; set; } = NotAvailable;

    /// <summary>The installation's folder; "—" until loaded.</summary>
    [ ObservableProperty ]
    public partial string Path { get; set; } = NotAvailable;

    /// <summary>The validity colour token (<c>Green</c> / <c>Red</c>) for the Status value.</summary>
    [ ObservableProperty ]
    public partial string StatusColourToken { get; set; } = "Green";

    /// <summary>The validity display name (e.g. <c>Valid</c>, <c>Invalid — No EXE</c>); "—" until loaded.</summary>
    [ ObservableProperty ]
    public partial string StatusText { get; set; } = NotAvailable;

    /// <summary>Loads the installation. Must be awaited by the dialogue service before the window is shown.</summary>
    /// <param name="installationId">The installation to describe.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task InitialiseAsync(Guid installationId, CancellationToken cancellationToken = default)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_mediator is null)
        {
            return;
        }

        try
        {
            ErrorOr<InstallationSummary> result = await _mediator.Send(new GetInstallationByIdQuery(installationId), cancellationToken);

            if (result.IsError)
            {
                ErrorMessage = InstallationDialogMessages.InstallationMissing;

                return;
            }

            InstallationSummary summary = result.Value;

            Name              = summary.Name;
            Path              = summary.Path;
            StatusText        = summary.Validity.DisplayName;
            StatusColourToken = summary.Validity.ColourToken;
            DefaultText       = summary.IsDefault ? "Yes" : "No";
            AddedText         = FormatTimestamp(summary.AddedUtc);
            LastOpenedText    = FormatTimestamp(summary.LastOpenedUtc);
            LastPlayedText    = FormatTimestamp(summary.LastPlayedUtc);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to load installation {InstallationId} for the Info dialogue.", installationId);

            ErrorMessage = InstallationDialogMessages.UnexpectedFailure;
        }
    }

    [ RelayCommand ]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    // Storage is UTC; display is local time, en-GB (e.g. "12 Mar 2025 18:02") — SDD §7.2.6.
    private static string FormatTimestamp(DateTime? utc)
        => utc?.ToLocalTime()
               .ToString(format: "d MMM yyyy HH:mm", DisplayCulture)
           ?? NotAvailable;

    /// <summary>Raised when the dialogue should close.</summary>
    public event EventHandler? CloseRequested;
}
