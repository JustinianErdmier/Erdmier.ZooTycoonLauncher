namespace Erdmier.ZooTycoonLauncher.Domain.Installations;

/// <summary>
/// Represents a Zoo Tycoon installation tracked by the launcher.
/// </summary>
/// <remarks>
/// Identity is the <see cref="Id" /> Guid (Version 7) set by the application layer at creation time.
/// <see cref="Path" /> is <see langword="init" />-only — relocating an installation rewrites the row
/// in place rather than as a separate entity.
/// </remarks>
public sealed class GameInstallation
{
    /// <summary>The installation's identifier, assigned at creation via <c>Guid.CreateVersion7()</c>.</summary>
    public Guid Id { get; init; }

    /// <summary>The user-visible name; unique (case-insensitive) within the <c>GameInstallations</c> table.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The fully qualified directory path containing <c>zoo.exe</c>; unique (case-insensitive).</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>Indicates whether <c>zoo.exe</c> was present on disk at last verification.</summary>
    public bool HasExe { get; set; }

    /// <summary>Indicates whether <c>zoo.ini</c> was present on disk at last verification.</summary>
    public bool HasIni { get; set; }

    /// <summary>UTC timestamp when this installation was added to the launcher.</summary>
    public DateTime AddedUtc { get; init; }

    /// <summary>UTC timestamp of the most recent modification to any non-identity column.</summary>
    public DateTime? ModifiedUtc { get; set; }

    /// <summary>UTC timestamp when the Launch Game button most recently kicked off a successful process start.</summary>
    public DateTime? LastPlayedUtc { get; set; }

    /// <summary>UTC timestamp when this installation most recently became the active installation in the main window.</summary>
    public DateTime? LastOpenedUtc { get; set; }

    /// <summary>Computes the current <see cref="InstallationValidity" /> from <see cref="HasExe" /> and <see cref="HasIni" />.</summary>
    public InstallationValidity Validity => InstallationValidity.From(HasExe, HasIni);
}
