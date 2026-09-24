namespace Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;

/// <summary>User-facing messages shared by the installation management dialogues (Info, Edit, Delete, Fix).</summary>
public static class InstallationDialogMessages
{
    /// <summary>Shown when the installation was removed after the grid loaded (the handler returned <c>Installation.NotFound</c>).</summary>
    public const string InstallationMissing = "This installation no longer exists.";

    /// <summary>Shown when a dispatch throws unexpectedly; the exception itself is logged.</summary>
    public const string UnexpectedFailure = "Something went wrong — see the launcher log for details.";
}
