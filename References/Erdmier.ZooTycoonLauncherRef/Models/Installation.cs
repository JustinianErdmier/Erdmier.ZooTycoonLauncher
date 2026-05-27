using System;

using JetBrains.Annotations;

namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>A registered Zoo Tycoon installation: a directory containing both <c>zoo.exe</c> and <c>zoo.ini</c>.</summary>
[ UsedImplicitly ]
public sealed class Installation
{
    /// <summary>Stable identifier. Never changes, even if the directory or name is updated.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>User-assigned friendly name. <see langword="null" /> means the UI falls back to <see cref="GameDirectory" />.</summary>
    public string? Name { get; set; }

    /// <summary>Absolute path to the directory containing <c>zoo.exe</c> and <c>zoo.ini</c>.</summary>
    public string GameDirectory { get; set; } = string.Empty;

    /// <summary>
    ///     <see langword="false" /> if the last validation attempt found <c>zoo.exe</c> missing. An installation must pass
    ///     validation to be added, but may become invalid afterwards (e.g. game uninstalled, drive disconnected).
    ///     Invalid installations are still retained in config until the user explicitly removes them.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>UTC timestamp of the last time this installation was opened in the launcher. <see langword="null" /> if never opened.</summary>
    public DateTime? LastOpened { get; set; }

    /// <summary>Display name used in all UI bindings. Falls back to <see cref="GameDirectory" /> when <see cref="Name" /> is <see langword="null" />.</summary>
    public string DisplayName => Name ?? GameDirectory;
}
