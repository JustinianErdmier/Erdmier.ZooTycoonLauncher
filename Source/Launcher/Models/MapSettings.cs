namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Strongly typed representation of the <c> [Map] </c> section of <c> zoo.ini </c>, controlling default map dimensions for new zoos.</summary>
public class MapSettings
{
    /// <summary>Default new zoo width in tiles. Valid range: <c> 1 </c>–<c> 128 </c>.</summary>
    public int MapX { get; set; } = 75;

    /// <summary>Default new zoo height in tiles. Valid range: <c> 1 </c>–<c> 128 </c>.</summary>
    public int MapY { get; set; } = 75;
}
