namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common;

/// <summary>Implemented by main-window content that can hold unsaved edits, so the window can ask before closing or switching away from it (SDD §7.3.2).</summary>
public interface IPendingChangesGuard
{
    /// <summary>Whether unsaved edits exist.</summary>
    bool HasPendingChanges { get; }

    /// <summary>Asks the user what to do with unsaved edits, acting on the answer.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when the caller may proceed (no edits, saved, or discarded); <see langword="false" /> to stay.</returns>
    Task<bool> ConfirmLeaveAsync(CancellationToken cancellationToken);
}
