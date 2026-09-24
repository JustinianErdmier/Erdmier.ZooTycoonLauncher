namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.IniConfig;

/// <summary>The INI Config tab's non-editor sub-state: loading, no INI present, or unreadable INI (SDD §9.3.1).</summary>
public sealed class IniPlaceholderViewModel : ViewModelBase
{
    /// <summary>Initialises a new instance.</summary>
    /// <param name="headline">The bold headline.</param>
    /// <param name="body">The muted explanation (may be empty).</param>
    /// <param name="showWarningIcon">Whether to show the warning icon.</param>
    public IniPlaceholderViewModel(string headline, string body, bool showWarningIcon)
    {
        Headline        = headline;
        Body            = body;
        ShowWarningIcon = showWarningIcon;
    }

    /// <summary>Initialises a new instance for the XAML designer.</summary>
    public IniPlaceholderViewModel()
        : this(headline: "No INI present", body: "This installation has no zoo.ini on disk, so its settings cannot be edited.", showWarningIcon: true)
    { }

    /// <summary>The bold headline.</summary>
    public string Headline { get; }

    /// <summary>The muted explanation.</summary>
    public string Body { get; }

    /// <summary>Whether <see cref="Body" /> has text.</summary>
    public bool HasBody => Body.Length > 0;

    /// <summary>Whether to show the warning icon.</summary>
    public bool ShowWarningIcon { get; }

    /// <summary>Shown while the first load is in flight.</summary>
    /// <returns>The placeholder.</returns>
    public static IniPlaceholderViewModel Loading() => new(headline: "Loading zoo.ini…", body: string.Empty, showWarningIcon: false);

    /// <summary>Shown when the installation has no <c>zoo.ini</c>.</summary>
    /// <returns>The placeholder.</returns>
    public static IniPlaceholderViewModel NoIni()
        => new(headline: "No INI present", body: "This installation has no zoo.ini on disk, so its settings cannot be edited.", showWarningIcon: true);

    /// <summary>Shown when <c>zoo.ini</c> or its settings history could not be read.</summary>
    /// <param name="message">The error description.</param>
    /// <returns>The placeholder.</returns>
    public static IniPlaceholderViewModel Unreadable(string message) => new(headline: "zoo.ini could not be read", message, showWarningIcon: true);
}
