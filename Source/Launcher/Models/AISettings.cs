namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Strongly typed representation of the <c> [ai] </c> section of <c> zoo.ini </c>, covering AI-related gameplay settings.</summary>
public class AiSettings
{
    /// <summary>Maximum number of guests permitted in the zoo simultaneously. Valid range: <c> 1 </c>–<c> 10000 </c>.</summary>
    public int MaxGuests { get; set; } = 1_000;
}
