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

    /// <summary>
    ///     Opens a native folder picker rooted at the supplied path (or a sensible default when <see langword="null" />) and returns the chosen folder, or <see langword="null" />
    ///     when the user cancels.
    /// </summary>
    /// <param name="startPath">A directory to start the picker in, when present.</param>
    Task<string?> PickFolderAsync(string? startPath);
}
