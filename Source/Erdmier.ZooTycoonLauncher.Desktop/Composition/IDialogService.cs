namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>Surface for opening Desktop-layer dialogues (modeless or modal). Grows with each new dialogue slice.</summary>
public interface IDialogService
{
    /// <summary>Opens the modeless launch-error window with the supplied message. Owned by <c>MainWindow</c> when available.</summary>
    /// <param name="message">The error message to display verbatim.</param>
    void ShowLaunchError(string message);

    /// <summary>Opens the modal Add Installation dialogue (SDD §7.2.1, §9.5). Returns the dispatched <see cref="AddInstallationResult" /> on Save, or <see langword="null" /> on Cancel.</summary>
    /// <param name="prefilledPath">A candidate path to pre-fill into the Folder input — typically <c>BootResult.LocatedCandidatePath</c>.</param>
    Task<AddInstallationResult?> ShowAddInstallationAsync(string? prefilledPath);

    /// <summary>Opens the modal Installation Manager dialogue (SDD §7.2.2, §9.4).</summary>
    /// <returns><see langword="true" /> when the user changed anything whilst the dialogue was open (for example, added an installation); otherwise <see langword="false" />.</returns>
    Task<bool> ShowInstallationManagerAsync();

    /// <summary>Opens the modal Edit Installation dialogue (SDD §7.2.3, §9.5) for the given installation.</summary>
    /// <param name="installationId">The installation to edit.</param>
    /// <returns><see langword="true" /> when the edit was saved; otherwise <see langword="false" />.</returns>
    Task<bool> ShowEditInstallationAsync(Guid installationId);

    /// <summary>Opens the read-only Installation Info dialogue (SDD §7.2.6, §9.5) for the given installation.</summary>
    /// <param name="installationId">The installation to describe.</param>
    Task ShowInstallationInfoAsync(Guid installationId);

    /// <summary>Opens the Delete Installation confirmation (SDD §7.2.4, §9.5) for the given installation.</summary>
    /// <param name="installationId">The installation to delete.</param>
    /// <returns><see langword="true" /> when the installation was deleted; otherwise <see langword="false" />.</returns>
    Task<bool> ShowDeleteInstallationAsync(Guid installationId);

    /// <summary>Opens the Fix Installation dialogue (SDD §7.2.5, §9.5) for the given installation.</summary>
    /// <param name="installationId">The installation to fix.</param>
    /// <returns><see langword="true" /> when anything was persisted (a relocation, or drift found by the re-probe); otherwise <see langword="false" />.</returns>
    Task<bool> ShowFixInstallationAsync(Guid installationId);

    /// <summary>
    ///     Opens a native folder picker rooted at the supplied path (or a sensible default when <see langword="null" />) and returns the chosen folder, or <see langword="null" />
    ///     when the user cancels.
    /// </summary>
    /// <param name="startPath">A directory to start the picker in, when present.</param>
    Task<string?> PickFolderAsync(string? startPath);
}
