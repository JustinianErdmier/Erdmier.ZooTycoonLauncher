namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Strongly typed representation of the <c> [advanced] </c> section of <c> zoo.ini </c>, controlling the performance/quality trade-off.</summary>
public class AdvancedSettings
{
    /// <summary>Reduce rendering quality during click operations.</summary>
    public bool Click { get; set; }

    /// <summary>Reduce rendering quality during drag operations.</summary>
    public bool Drag { get; set; }

    /// <summary>Overall quality preset. <c> 0 </c> = Total Quality, <c> 1 </c> = Quality, <c> 2 </c> = Balance, <c> 3 </c> = Speed, <c> 4 </c> = Paused.</summary>
    public int Level { get; set; } = 2;

    /// <summary>Load reduced-detail animation sets to improve performance.</summary>
    public bool LoadHalfAnims { get; set; }

    /// <summary>Reduce rendering quality during normal operation.</summary>
    public bool Normal { get; set; }

    /// <summary>Force 8-bit audio output. May improve compatibility on older hardware.</summary>
    public bool Use8BitSound { get; set; }
}
