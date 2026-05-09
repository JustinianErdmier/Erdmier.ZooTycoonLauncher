using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Erdmier.ZooTycoonLauncher.Launcher.Models;
using Erdmier.ZooTycoonLauncher.Launcher.Services;

namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>
///     ViewModel for the INI Configurations tab. Exposes the editable keys of <see cref="ZooIniModel" /> as observable properties for two-way XAML binding, tracks an
///     <see cref="IsDirty" /> flag set by any edit, and persists changes to disk via <see cref="IIniParserService.WriteAsync" /> only when <see cref="SaveCommand" /> fires.
/// </summary>
/// <remarks>
///     <b>Tooltip strings</b> <br /><br /> Each tooltip = the prose from IniTooltips.axaml (Tt.* resource) + a "Default: value" line. The default value comes from a single
///     canonical `new ZooIniModel()` instance via ZooIniDefaults.KnownKeys → IniKeySpec.Read, so the submodel property initialisers (UserSettings.ScreenWidth = 800, etc.) remain the
///     single source of truth — the tooltips can never drift. <br /><br /> Three derived tooltips (ScreenMode, PlayMenuMusic, Language) don't map 1:1 to a single key, so they pass an
///     explicit human-readable default override.
/// </remarks>
public sealed partial class IniSettingsViewModel : ViewModelBase
{
    /// <summary>
    ///     Canonical defaults model. <c>new ZooIniModel()</c> picks up every submodel property initialiser (e.g. <c>UserSettings.ScreenWidth = 800</c>), so this single instance is
    ///     the source of truth for every tooltip's "Default:" line.
    /// </summary>
    private static readonly ZooIniModel DefaultsModel = new();

    private readonly IIniParserService _parser;

    private readonly IVersioningService _versioning;

    // Path to zoo.ini, supplied by MainWindowViewModel via ApplyModel. Null → save is impossible (game not located).
    private string? _iniPath;

    // Reference to the disk-of-record model. Preserved across edits, so the parser’s RawDocument is reused on save (comments + ordering survive).
    private ZooIniModel? _model;

    // Suppresses MarkDirty during ApplyModel so syncing from disk doesn't flip IsDirty to true.
    private bool _suppressDirtyTracking;

    public IniSettingsViewModel(IIniParserService parser, IVersioningService versioning)
    {
        _parser     = parser;
        _versioning = versioning;
    }

    /// <summary>Parameterless ctor used by the XAML designer only.</summary>
    public IniSettingsViewModel()
        : this(NullIniParserService.Instance, NullVersioningService.Instance)
    {
        ApplyModel(new ZooIniModel(), iniPath: null);
    }

    [ ObservableProperty ]
    public partial int CashIncrement { get; set; } = 5_000;

    public string CashIncrementTooltip { get; } = BuildTooltip(resourceKey: "Tt.CashIncrement", section: "UI", key: "MSCashIncrement");

    [ ObservableProperty ]
    public partial bool Click { get; set; }

    public string ClickTooltip { get; } = BuildTooltip(resourceKey: "Tt.Click", section: "advanced", key: "click");

    [ ObservableProperty ]
    public partial bool Drag { get; set; }

    public string DragTooltip { get; } = BuildTooltip(resourceKey: "Tt.Drag", section: "advanced", key: "drag");

    [ ObservableProperty ]
    public partial bool DrawFps { get; set; }

    public string DrawFpsTooltip { get; } = BuildTooltip(resourceKey: "Tt.DrawFps", section: "debug", key: "drawfps");

    [ ObservableProperty ]
    public partial int DrawFpsX { get; set; } = 720;

    public string DrawFpsXTooltip { get; } = BuildTooltip(resourceKey: "Tt.DrawFpsX", section: "debug", key: "drawfpsx");

    [ ObservableProperty ]
    public partial int DrawFpsY { get; set; } = 20;

    public string DrawFpsYTooltip { get; } = BuildTooltip(resourceKey: "Tt.DrawFpsY", section: "debug", key: "drawfpsy");

    [ ObservableProperty ]
    public partial int DrawRate { get; set; } = 60;

    public string DrawRateTooltip { get; } = BuildTooltip(resourceKey: "Tt.DrawRate", section: "user", key: "DrawRate");

    [ ObservableProperty ]
    public partial bool Fullscreen { get; set; } = true;

    [ ObservableProperty ]
    public partial int HelpType { get; set; } = 1;

    public string HelpTypeTooltip { get; } = BuildTooltip(resourceKey: "Tt.HelpType", section: "UI", key: "helpType");

    [ ObservableProperty ]
    public partial bool IsDirty { get; set; }

    [ ObservableProperty ]
    public partial int KeyScrollX { get; set; } = 64;

    public string KeyScrollXTooltip { get; } = BuildTooltip(resourceKey: "Tt.KeyScrollX", section: "UI", key: "keyScrollX");

    [ ObservableProperty ]
    public partial int KeyScrollY { get; set; } = 64;

    public string KeyScrollYTooltip { get; } = BuildTooltip(resourceKey: "Tt.KeyScrollY", section: "UI", key: "keyScrollY");

    // ── [language] (SDD §9.7) ─────────────────────────────────────────────────────────────────────────────────────────

    [ ObservableProperty ]
    public partial int Lang { get; set; } = 9;

    /// <summary>Hard-coded canonical list of common Windows LANGID/SUBLANGID combinations. Install-driven enumeration is a follow-up (see design §3.6).</summary>
    public IReadOnlyList<LanguageOption> LanguageOptions { get; } =
    [
        new(lang: 9, subLang: 1, displayName: "English (United States)"),
        new(lang: 9, subLang: 2, displayName: "English (United Kingdom)"),
        new(lang: 7, subLang: 1, displayName: "German (Germany)"),
        new(lang: 12, subLang: 1, displayName: "French (France)"),
        new(lang: 10, subLang: 3, displayName: "Spanish (Modern)"),
        new(lang: 16, subLang: 1, displayName: "Italian (Italy)"),
        new(lang: 17, subLang: 1, displayName: "Japanese"),
        new(lang: 22, subLang: 1, displayName: "Portuguese (Brazil)"),
        new(lang: 19, subLang: 1, displayName: "Dutch (Netherlands)"),
        new(lang: 29, subLang: 1, displayName: "Swedish (Sweden)")
    ];

    // Language: composite of [language]/lang + [language]/sublang. The default LangID 9 / SubLang 1 = English (United States).
    public string LanguageTooltip { get; } = BuildTooltipFromOverride(resourceKey: "Tt.Language", defaultDisplay: "English (United States)");

    [ ObservableProperty ]
    public partial int Level { get; set; } = 2;

    public string LevelTooltip { get; } = BuildTooltip(resourceKey: "Tt.Level", section: "advanced", key: "level");

    [ ObservableProperty ]
    public partial bool LoadHalfAnims { get; set; }

    public string LoadHalfAnimsTooltip { get; } = BuildTooltip(resourceKey: "Tt.LoadHalfAnims", section: "advanced", key: "loadHalfAnims");

    [ ObservableProperty ]
    public partial int LogCutoff { get; set; } = 1;

    public string LogCutoffTooltip { get; } = BuildTooltip(resourceKey: "Tt.LogCutoff", section: "debug", key: "logCutoff");

    [ ObservableProperty ]
    public partial int MapX { get; set; } = 75;

    public string MapXTooltip { get; } = BuildTooltip(resourceKey: "Tt.MapX", section: "Map", key: "mapX");

    [ ObservableProperty ]
    public partial int MapY { get; set; } = 75;

    public string MapYTooltip { get; } = BuildTooltip(resourceKey: "Tt.MapY", section: "Map", key: "mapY");

    [ ObservableProperty ]
    public partial int MaxCash { get; set; } = 500_000;

    public string MaxCashTooltip { get; } = BuildTooltip(resourceKey: "Tt.MaxCash", section: "UI", key: "MSMaxCash");

    [ ObservableProperty ]
    public partial int MaxGuests { get; set; } = 1_000;

    public string MaxGuestsTooltip { get; } = BuildTooltip(resourceKey: "Tt.MaxGuests", section: "ai", key: "maxGuests");

    [ ObservableProperty ]
    public partial string MenuMusic { get; set; } = "sounds/mainmenu.wav";

    [ ObservableProperty ]
    public partial int MenuMusicAttenuation { get; set; } = 1500;

    public string MenuMusicAttenuationTooltip { get; } = BuildTooltip(resourceKey: "Tt.MenuMusicAttenuation", section: "UI", key: "menuMusicAttenuation");

    public string MenuMusicTooltip { get; } = BuildTooltip(resourceKey: "Tt.MenuMusic", section: "UI", key: "menuMusic");

    [ ObservableProperty ]
    public partial bool MessageDisplay { get; set; } = true;

    public string MessageDisplayTooltip { get; } = BuildTooltip(resourceKey: "Tt.MessageDisplay", section: "UI", key: "MessageDisplay");

    [ ObservableProperty ]
    public partial int MinCash { get; set; } = 10_000;

    public string MinCashTooltip { get; } = BuildTooltip(resourceKey: "Tt.MinCash", section: "UI", key: "MSMinCash");

    [ ObservableProperty ]
    public partial int MinimumMessageInterval { get; set; } = 60;

    public string MinimumMessageIntervalTooltip { get; } = BuildTooltip(resourceKey: "Tt.MinimumMessageInterval", section: "UI", key: "minimumMessageInterval");

    [ ObservableProperty ]
    public partial int MouseScrollDelay { get; set; } = 1;

    public string MouseScrollDelayTooltip { get; } = BuildTooltip(resourceKey: "Tt.MouseScrollDelay", section: "UI", key: "mouseScrollDelay");

    [ ObservableProperty ]
    public partial int MouseScrollThreshold { get; set; } = 1;

    public string MouseScrollThresholdTooltip { get; } = BuildTooltip(resourceKey: "Tt.MouseScrollThreshold", section: "UI", key: "mouseScrollThreshold");

    [ ObservableProperty ]
    public partial int MouseScrollX { get; set; } = 27;

    public string MouseScrollXTooltip { get; } = BuildTooltip(resourceKey: "Tt.MouseScrollX", section: "UI", key: "mouseScrollX");

    [ ObservableProperty ]
    public partial int MouseScrollY { get; set; } = 27;

    public string MouseScrollYTooltip { get; } = BuildTooltip(resourceKey: "Tt.MouseScrollY", section: "UI", key: "mouseScrollY");

    [ ObservableProperty ]
    public partial int MovieVolume1 { get; set; } = -1000;

    public string MovieVolume1Tooltip { get; } = BuildTooltip(resourceKey: "Tt.MovieVolume1", section: "UI", key: "movievolume1");

    [ ObservableProperty ]
    public partial int MovieVolume2 { get; set; } = -1000;

    public string MovieVolume2Tooltip { get; } = BuildTooltip(resourceKey: "Tt.MovieVolume2", section: "UI", key: "movievolume2");

    [ ObservableProperty ]
    public partial bool NoMenuMusic { get; set; }

    [ ObservableProperty ]
    public partial bool Normal { get; set; }

    public string NormalTooltip { get; } = BuildTooltip(resourceKey: "Tt.Normal", section: "advanced", key: "normal");

    /// <summary>Friendly inverted view over <see cref="NoMenuMusic" />: <c>true</c> = play menu music. Bound from the Audio GroupBox checkbox.</summary>
    public bool PlayMenuMusic
    {
        get => !NoMenuMusic;

        set => NoMenuMusic = !value;
    }

    // PlayMenuMusic is the inverse of [UI]/noMenuMusic. NoMenuMusic defaults to false → PlayMenuMusic default = "On".
    public string PlayMenuMusicTooltip { get; } = BuildTooltipFromOverride(resourceKey: "Tt.PlayMenuMusic", defaultDisplay: "On");

    [ ObservableProperty ]
    public partial bool PlayMovie { get; set; }

    public string PlayMovieTooltip { get; } = BuildTooltip(resourceKey: "Tt.PlayMovie", section: "UI", key: "playMovie");

    [ ObservableProperty ]
    public partial bool PlaySecondMovie { get; set; }

    public string PlaySecondMovieTooltip { get; } = BuildTooltip(resourceKey: "Tt.PlaySecondMovie", section: "UI", key: "playSecondMovie");

    [ ObservableProperty ]
    public partial int ScreenHeight { get; set; } = 600;

    public string ScreenHeightTooltip { get; } = BuildTooltip(resourceKey: "Tt.ScreenHeight", section: "user", key: "screenheight");

    /// <summary>Friendly drop-down view over <see cref="Fullscreen" />: 0 = Fullscreen, 1 = Windowed. Bound from the Display &amp; Performance "Screen mode" combo.</summary>
    public int ScreenModeIndex
    {
        get => Fullscreen ? 0 : 1;

        set => Fullscreen = value == 0;
    }

    public string ScreenModeTooltip { get; } = BuildTooltipFromOverride(resourceKey: "Tt.ScreenMode", defaultDisplay: "Fullscreen");

    [ ObservableProperty ]
    public partial int ScreenWidth { get; set; } = 800;

    public string ScreenWidthTooltip { get; } = BuildTooltip(resourceKey: "Tt.ScreenWidth", section: "user", key: "screenwidth");

    /// <summary>
    ///     Returns the option that matches the current <see cref="Lang" />/<see cref="SubLang" /> pair, or <see langword="null" /> if no match — in which case the drop-down shows
    ///     nothing selected and the underlying integer fields are preserved verbatim by the parser.
    /// </summary>
    public LanguageOption? SelectedLanguage
    {
        get => LanguageOptions.FirstOrDefault(o => o.Lang == Lang && o.SubLang == SubLang);

        set
        {
            if (value is null)
            {
                return;
            }

            Lang    = value.Lang;
            SubLang = value.SubLang;
        }
    }

    [ ObservableProperty ]
    public partial bool SendDebugger { get; set; } = true;

    public string SendDebuggerTooltip { get; } = BuildTooltip(resourceKey: "Tt.SendDebugger", section: "debug", key: "sendDebugger");

    [ ObservableProperty ]
    public partial bool SendLogfile { get; set; } = true;

    public string SendLogfileTooltip { get; } = BuildTooltip(resourceKey: "Tt.SendLogfile", section: "debug", key: "sendLogfile");

    [ ObservableProperty ]
    public partial int StartingCash { get; set; } = 70_000;

    public string StartingCashTooltip { get; } = BuildTooltip(resourceKey: "Tt.StartingCash", section: "UI", key: "MSStartingCash");

    [ ObservableProperty ]
    public partial string? StatusMessage { get; set; }

    [ ObservableProperty ]
    public partial int SubLang { get; set; } = 1;

    [ ObservableProperty ]
    public partial int TooltipDelay { get; set; } = 1;

    public string TooltipDelayTooltip { get; } = BuildTooltip(resourceKey: "Tt.TooltipDelay", section: "UI", key: "tooltipDelay");

    [ ObservableProperty ]
    public partial int TooltipDuration { get; set; } = 3_000;

    public string TooltipDurationTooltip { get; } = BuildTooltip(resourceKey: "Tt.TooltipDuration", section: "UI", key: "tooltipDuration");

    [ ObservableProperty ]
    public partial int UpdateRate { get; set; } = 15;

    public string UpdateRateTooltip { get; } = BuildTooltip(resourceKey: "Tt.UpdateRate", section: "user", key: "UpdateRate");

    [ ObservableProperty ]
    public partial bool Use8BitSound { get; set; }

    public string Use8BitSoundTooltip { get; } = BuildTooltip(resourceKey: "Tt.Use8BitSound", section: "advanced", key: "use8BitSound");

    // ── [UI] — interface (SDD §9.5) ───────────────────────────────────────────────────────────────────────────────────

    [ ObservableProperty ]
    public partial bool UseAlternateCursors { get; set; }

    public string UseAlternateCursorsTooltip { get; } = BuildTooltip(resourceKey: "Tt.UseAlternateCursors", section: "UI", key: "useAlternateCursors");

    [ ObservableProperty ]
    public partial int UserAttenuation { get; set; }

    public string UserAttenuationTooltip { get; } = BuildTooltip(resourceKey: "Tt.UserAttenuation", section: "UI", key: "userAttenuation");

    /// <summary>Replaces the working state with values from <paramref name="model" />, resets <see cref="IsDirty" />, and stashes <paramref name="iniPath" /> for the save path.</summary>
    public void ApplyModel(ZooIniModel? model, string? iniPath)
    {
        _model   = model;
        _iniPath = iniPath;

        if (model is null)
        {
            return;
        }

        _suppressDirtyTracking = true;

        try
        {
            SyncFromModel(model);
        }
        finally
        {
            _suppressDirtyTracking = false;
        }

        IsDirty = false;
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnFullscreenChanged(bool value)
    {
        MarkDirty();
        OnPropertyChanged(nameof(ScreenModeIndex));
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnScreenWidthChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnScreenHeightChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnUpdateRateChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDrawRateChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnLevelChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnLoadHalfAnimsChanged(bool value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDragChanged(bool value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnClickChanged(bool value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnNormalChanged(bool value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnNoMenuMusicChanged(bool value)
    {
        MarkDirty();
        OnPropertyChanged(nameof(PlayMenuMusic));
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMenuMusicChanged(string value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMenuMusicAttenuationChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnUserAttenuationChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnPlayMovieChanged(bool value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMovieVolume1Changed(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnPlaySecondMovieChanged(bool value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMovieVolume2Changed(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnUse8BitSoundChanged(bool value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnStartingCashChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnCashIncrementChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMinCashChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMaxCashChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMaxGuestsChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnUseAlternateCursorsChanged(bool value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnTooltipDelayChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnTooltipDurationChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMessageDisplayChanged(bool value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMouseScrollThresholdChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMouseScrollDelayChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMouseScrollXChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMouseScrollYChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnKeyScrollXChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnKeyScrollYChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMinimumMessageIntervalChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnHelpTypeChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMapXChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnMapYChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnLangChanged(int value)
    {
        MarkDirty();
        OnPropertyChanged(nameof(SelectedLanguage));
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSubLangChanged(int value)
    {
        MarkDirty();
        OnPropertyChanged(nameof(SelectedLanguage));
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDrawFpsChanged(bool value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDrawFpsXChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDrawFpsYChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnLogCutoffChanged(int value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSendLogfileChanged(bool value) => MarkDirty();

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSendDebuggerChanged(bool value) => MarkDirty();

    /// <summary>Writes the current working values back to <see cref="_model" /> and persists to disk. Bound to the Save button's Command.</summary>
    [ RelayCommand(CanExecute = nameof(CanSave)) ]
    private async Task SaveAsync()
    {
        if (_model is null
            || string.IsNullOrEmpty(_iniPath))
        {
            StatusMessage = "Cannot save: zoo.ini path is unknown.";

            return;
        }

        StatusMessage = "Saving…";

        try
        {
            // 1. Mutate the disk-of-record model in place so its RawDocument is reused (preserves comments + key ordering on a round-trip).
            ApplyToModel(_model);

            // 2. Snapshot zoo.ini → zoo.ini.undo BEFORE writing (SDD §8.2).
            await _versioning.CreateUndoSnapshotAsync(_iniPath);

            // 3. Atomically writes (the temp file and rename) — implemented inside IniParserService.WriteAsync() per SDD §11.
            await _parser.WriteAsync(_iniPath, _model);

            // 4. Verification re-read so any normalisation the parser applied is reflected into the UI.
            ZooIniModel reloaded = await _parser.ReadAsync(_iniPath);
            _model = reloaded;

            _suppressDirtyTracking = true;

            try
            {
                SyncFromModel(reloaded);
            }
            finally
            {
                _suppressDirtyTracking = false;
            }

            IsDirty       = false;
            StatusMessage = "Saved.";
        }
        catch (Exception ex)
        {
            // Per SDD §10: surface the error. IsDirty stays true, so the user can retry. Status-bar-only for this milestone (design §3.11).
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    private bool CanSave() => IsDirty && _model is not null && !string.IsNullOrEmpty(_iniPath);

    /// <summary>Re-reads <c>zoo.ini</c> from disk and applies it, throwing away any in-memory edits. Bound to the Discard button.</summary>
    /// <remarks>
    ///     Re-reads from disk rather than re-syncing from the cached <see cref="_model" /> because <see cref="SaveAsync" /> mutates that model in place before the file write
    ///     operation — after a failed save, the model holds the intended values, not the on-disk values. Rereading is the only way to guarantee Discard restores the user to the file's
    ///     actual state.
    /// </remarks>
    [ RelayCommand(CanExecute = nameof(CanDiscard)) ]
    private async Task DiscardAsync()
    {
        if (string.IsNullOrEmpty(_iniPath))
        {
            StatusMessage = "Cannot discard: zoo.ini path is unknown.";

            return;
        }

        try
        {
            ZooIniModel reloaded = await _parser.ReadAsync(_iniPath);

            // ApplyModel handles dirty-tracking suppression, _model rebind, and IsDirty = false in one go.
            ApplyModel(reloaded, _iniPath);

            StatusMessage = "Changes discarded.";
        }
        catch (Exception ex)
        {
            // IsDirty stays as-is, so the user can retry. Surface to the same status line as the save failures.
            StatusMessage = $"Discard failed: {ex.Message}";
        }
    }

    private bool CanDiscard() => IsDirty && !string.IsNullOrEmpty(_iniPath);

    /// <summary>
    ///     Restores <c>zoo.ini</c> from the <c>zoo.ini.undo</c> snapshot. Stubbed for this milestone — wired up so the button shows visibly disabled rather than executing a no-op
    ///     handler.
    /// </summary>
    /// <remarks>
    ///     TODO: hook up to <see cref="IVersioningService.RestoreUndoAsync" /> (currently throws <see cref="NotImplementedException" />) and gate <see cref="CanUndo" /> on
    ///     <see cref="IVersioningService.UndoSnapshotExists" />.
    /// </remarks>
    [ RelayCommand(CanExecute = nameof(CanUndo)) ]
    private void Undo()
    { }

    private bool CanUndo() => false;

    /// <summary>Re-evaluates Save and Discard CanExecute when <see cref="IsDirty" /> flips. Wired via the source generator's partial OnIsDirtyChanged.</summary>

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnIsDirtyChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
        DiscardCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Called by every observable-property setter to flip <see cref="IsDirty" /> on the first edit after a sync. Suppressed during <see cref="ApplyModel" />.</summary>
    private void MarkDirty()
    {
        if (_suppressDirtyTracking)
        {
            return;
        }

        IsDirty = true;
    }

    /// <summary>Copies values from <paramref name="model" /> into the working observable properties.</summary>
    private void SyncFromModel(ZooIniModel model)
    {
        // [user]
        Fullscreen   = model.User.Fullscreen;
        ScreenWidth  = model.User.ScreenWidth;
        ScreenHeight = model.User.ScreenHeight;
        UpdateRate   = model.User.UpdateRate;
        DrawRate     = model.User.DrawRate;

        // [advanced] — graphics
        Level         = model.Advanced.Level;
        LoadHalfAnims = model.Advanced.LoadHalfAnims;
        Drag          = model.Advanced.Drag;
        Click         = model.Advanced.Click;
        Normal        = model.Advanced.Normal;

        // [UI] + [advanced] — audio
        NoMenuMusic          = model.Ui.NoMenuMusic;
        MenuMusic            = model.Ui.MenuMusic;
        MenuMusicAttenuation = model.Ui.MenuMusicAttenuation;
        UserAttenuation      = model.Ui.UserAttenuation;
        PlayMovie            = model.Ui.PlayMovie;
        MovieVolume1         = model.Ui.MovieVolume1;
        PlaySecondMovie      = model.Ui.PlaySecondMovie;
        MovieVolume2         = model.Ui.MovieVolume2;
        Use8BitSound         = model.Advanced.Use8BitSound;

        // [UI] + [ai] — gameplay
        StartingCash  = model.Ui.StartingCash;
        CashIncrement = model.Ui.CashIncrement;
        MinCash       = model.Ui.MinCash;
        MaxCash       = model.Ui.MaxCash;
        MaxGuests     = model.Ai.MaxGuests;

        // [UI] — interface
        UseAlternateCursors    = model.Ui.UseAlternateCursors;
        TooltipDelay           = model.Ui.TooltipDelay;
        TooltipDuration        = model.Ui.TooltipDuration;
        MessageDisplay         = model.Ui.MessageDisplay;
        MouseScrollThreshold   = model.Ui.MouseScrollThreshold;
        MouseScrollDelay       = model.Ui.MouseScrollDelay;
        MouseScrollX           = model.Ui.MouseScrollX;
        MouseScrollY           = model.Ui.MouseScrollY;
        KeyScrollX             = model.Ui.KeyScrollX;
        KeyScrollY             = model.Ui.KeyScrollY;
        MinimumMessageInterval = model.Ui.MinimumMessageInterval;
        HelpType               = model.Ui.HelpType;

        // [Map]
        MapX = model.Map.MapX;
        MapY = model.Map.MapY;

        // [language]
        Lang    = model.Language.Lang;
        SubLang = model.Language.SubLang;

        // [debug]
        DrawFps      = model.Debug.DrawFps;
        DrawFpsX     = model.Debug.DrawFpsX;
        DrawFpsY     = model.Debug.DrawFpsY;
        LogCutoff    = model.Debug.LogCutoff;
        SendLogfile  = model.Debug.SendLogfile;
        SendDebugger = model.Debug.SendDebugger;
    }

    /// <summary>Copies the working observable properties back into <paramref name="model" />.</summary>
    private void ApplyToModel(ZooIniModel model)
    {
        // [user]
        model.User.Fullscreen   = Fullscreen;
        model.User.ScreenWidth  = ScreenWidth;
        model.User.ScreenHeight = ScreenHeight;
        model.User.UpdateRate   = UpdateRate;
        model.User.DrawRate     = DrawRate;

        // [advanced] — graphics
        model.Advanced.Level         = Level;
        model.Advanced.LoadHalfAnims = LoadHalfAnims;
        model.Advanced.Drag          = Drag;
        model.Advanced.Click         = Click;
        model.Advanced.Normal        = Normal;

        // [UI] + [advanced] — audio
        model.Ui.NoMenuMusic          = NoMenuMusic;
        model.Ui.MenuMusic            = MenuMusic;
        model.Ui.MenuMusicAttenuation = MenuMusicAttenuation;
        model.Ui.UserAttenuation      = UserAttenuation;
        model.Ui.PlayMovie            = PlayMovie;
        model.Ui.MovieVolume1         = MovieVolume1;
        model.Ui.PlaySecondMovie      = PlaySecondMovie;
        model.Ui.MovieVolume2         = MovieVolume2;
        model.Advanced.Use8BitSound   = Use8BitSound;

        // [UI] + [ai] — gameplay
        model.Ui.StartingCash  = StartingCash;
        model.Ui.CashIncrement = CashIncrement;
        model.Ui.MinCash       = MinCash;
        model.Ui.MaxCash       = MaxCash;
        model.Ai.MaxGuests     = MaxGuests;

        // [UI] — interface
        model.Ui.UseAlternateCursors    = UseAlternateCursors;
        model.Ui.TooltipDelay           = TooltipDelay;
        model.Ui.TooltipDuration        = TooltipDuration;
        model.Ui.MessageDisplay         = MessageDisplay;
        model.Ui.MouseScrollThreshold   = MouseScrollThreshold;
        model.Ui.MouseScrollDelay       = MouseScrollDelay;
        model.Ui.MouseScrollX           = MouseScrollX;
        model.Ui.MouseScrollY           = MouseScrollY;
        model.Ui.KeyScrollX             = KeyScrollX;
        model.Ui.KeyScrollY             = KeyScrollY;
        model.Ui.MinimumMessageInterval = MinimumMessageInterval;
        model.Ui.HelpType               = HelpType;

        // [Map]
        model.Map.MapX = MapX;
        model.Map.MapY = MapY;

        // [language]
        model.Language.Lang    = Lang;
        model.Language.SubLang = SubLang;

        // [debug]
        model.Debug.DrawFps      = DrawFps;
        model.Debug.DrawFpsX     = DrawFpsX;
        model.Debug.DrawFpsY     = DrawFpsY;
        model.Debug.LogCutoff    = LogCutoff;
        model.Debug.SendLogfile  = SendLogfile;
        model.Debug.SendDebugger = SendDebugger;
    }

    /// <summary>
    ///     Looks up the prose from the <c>Tt.*</c> resource, finds the matching <see cref="IniKeySpec" /> in <see cref="ZooIniDefaults.KnownKeys" />, reads the default value off
    ///     <see cref="DefaultsModel" />, and returns <c>"{prose}\n\nDefault: {value}"</c>.
    /// </summary>
    private static string BuildTooltip(string resourceKey, string section, string key)
    {
        string prose = LookupResourceString(resourceKey);

        IniKeySpec? spec = ZooIniDefaults.KnownKeys.FirstOrDefault(k => string.Equals(k.Section, section, StringComparison.OrdinalIgnoreCase)
                                                                        && string.Equals(k.Key, key, StringComparison.OrdinalIgnoreCase));

        if (spec is null)
        {
            return prose;
        }

        string defaultDisplay = FormatDefault(spec.Read(DefaultsModel), spec.Kind);

        return string.IsNullOrEmpty(defaultDisplay) ? prose : $"{prose}\n\nDefault: {defaultDisplay}";
    }

    /// <summary>Variant for derived tooltips (e.g. ScreenMode, PlayMenuMusic, Language) where the displayed default is composed by the VM, not read directly off a single key.</summary>
    private static string BuildTooltipFromOverride(string resourceKey, string defaultDisplay)
    {
        string prose = LookupResourceString(resourceKey);

        return string.IsNullOrEmpty(defaultDisplay) ? prose : $"{prose}\n\nDefault: {defaultDisplay}";
    }

    /// <summary>
    ///     Pulls a string out of the merged <c>Application.Resources</c> dictionary. Returns empty if Avalonia hasn't booted yet (designer pre-init) or the key is missing — the
    ///     tooltip silently degrades to "Default: …" alone, which is preferable to crashing the VM.
    /// </summary>
    private static string LookupResourceString(string resourceKey)
    {
        // ResourceDictionary.TryGetResource() (not Application.TryFindResource() — that doesn't exist in Avalonia 11). Pass theme: null to fall back through the merged dictionary
        // chain.
        if (Application.Current is { } app
            && app.Resources.TryGetResource(resourceKey, theme: null, out object? value)
            && value is string s)
        {
            return s;
        }

        return string.Empty;
    }

    /// <summary>
    ///     Translates an <see cref="IniKeySpec" />'s INI-string output into a human-friendly default-value display. Bools become "On"/"Off"; null/empty optional values become
    ///     "(none)" / "(empty)"; everything else is shown verbatim.
    /// </summary>
    private static string FormatDefault(string raw, IniSpecKind kind)
    {
        return kind switch
        {
            IniSpecKind.Bool                                                                  => raw == "1" ? "On" : "Off",
            IniSpecKind.NullableInt or IniSpecKind.NullableStr when string.IsNullOrEmpty(raw) => "(none)",
            IniSpecKind.Str when string.IsNullOrEmpty(raw)                                    => "(empty)",
            var _                                                                             => raw
        };
    }
}

file sealed class NullIniParserService : IIniParserService
{
    public static readonly NullIniParserService Instance = new();

    public Task<ZooIniModel> ReadAsync(string iniFilePath) => Task.FromResult(new ZooIniModel());

    public Task WriteAsync(string iniFilePath, ZooIniModel model) => Task.CompletedTask;
}

file sealed class NullVersioningService : IVersioningService
{
    public static readonly NullVersioningService Instance = new();

    public Task EnsureOriginalBackupAsync(string iniFilePath) => Task.CompletedTask;

    public Task CreateUndoSnapshotAsync(string iniFilePath) => Task.CompletedTask;

    public Task<bool> RestoreUndoAsync(string iniFilePath) => Task.FromResult(result: false);

    public Task<bool> RestoreOriginalAsync(string iniFilePath) => Task.FromResult(result: false);

    public bool UndoSnapshotExists(string iniFilePath) => false;

    public bool OriginalBackupExists(string iniFilePath) => false;
}
