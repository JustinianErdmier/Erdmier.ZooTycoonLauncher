namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>The answer to the "Do you want to save the changes to <c>zoo.ini</c>?" prompt.</summary>
public enum SaveChangesChoice
{
    /// <summary>Save, then continue.</summary>
    Yes,

    /// <summary>Discard the changes, then continue.</summary>
    No,

    /// <summary>Stay, keeping the changes.</summary>
    Cancel
}
