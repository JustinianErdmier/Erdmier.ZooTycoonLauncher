using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Validates and manages the list of registered Zoo Tycoon installations stored in <c>launcher.config</c>.</summary>
public interface IInstallationService
{
    /// <summary>Returns <see langword="true" /> if the given directory contains <c>zoo.exe</c>.</summary>
    /// <param name="gameDirectory">Absolute path to the directory to validate.</param>
    Task<bool> ValidateAsync(string gameDirectory);

    /// <summary>Re-validates all installations in the config and updates <see cref="Installation.IsValid" /> accordingly.</summary>
    Task RevalidateAllAsync();

    /// <summary>Returns all registered installations in config order.</summary>
    Task<IReadOnlyList<Installation>> GetAllAsync();

    /// <summary>Registers a new installation. Throws <see cref="InvalidOperationException" /> if the directory does not contain <c>zoo.exe</c>.</summary>
    /// <param name="gameDirectory">Absolute path to the directory containing <c>zoo.exe</c>.</param>
    /// <param name="name">Optional friendly name. When <see langword="null" /> the UI falls back to the directory path.</param>
    /// <returns>The newly registered <see cref="Installation" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="gameDirectory" /> does not contain <c>zoo.exe</c>.</exception>
    Task<Installation> AddAsync(string gameDirectory, string? name = null);

    /// <summary>Removes an installation by <see cref="Installation.Id" />. No-op if the Id is not found.</summary>
    /// <param name="id">The <see cref="Installation.Id" /> of the installation to remove.</param>
    Task RemoveAsync(Guid id);

    /// <summary>
    ///     Updates the name or game directory of an existing installation in place, preserving its
    ///     <see cref="Installation.Id" /> and <see cref="Installation.LastOpened" />.
    /// </summary>
    /// <param name="id">The <see cref="Installation.Id" /> of the installation to update.</param>
    /// <param name="name">New friendly name, or <see langword="null" /> to leave unchanged.</param>
    /// <param name="gameDirectory">New directory path, or <see langword="null" /> to leave unchanged.</param>
    Task UpdateAsync(Guid id, string? name = null, string? gameDirectory = null);

    /// <summary>Sets <see cref="LauncherConfig.LastOpenedInstallationId" /> to <paramref name="id" /> and saves the config.</summary>
    /// <param name="id">The <see cref="Installation.Id" /> to mark as last opened.</param>
    Task SetLastOpenedAsync(Guid id);

    /// <summary>
    ///     Runs auto-discovery (hard-coded install paths, then registry probes) and returns a
    ///     <see cref="LocatorResult" /> for the first valid directory found.
    ///     Returns a failure result when nothing is found.
    /// </summary>
    Task<LocatorResult> DiscoverAsync();
}
