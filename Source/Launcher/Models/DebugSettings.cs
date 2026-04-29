namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>Strongly typed representation of the <c> [debug] </c> section of <c> zoo.ini </c>, covering developer/diagnostic settings.</summary>
public class DebugSettings
{
    /// <summary>Display an FPS counter overlay during gameplay.</summary>
    public bool DrawFps { get; set; }

    /// <summary>Horizontal pixel position of the FPS counter overlay. Valid range: <c> 0 </c>–screen width.</summary>
    public int DrawFpsX { get; set; } = 720;

    /// <summary>Vertical pixel position of the FPS counter overlay. Valid range: <c> 0 </c>–screen height.</summary>
    public int DrawFpsY { get; set; } = 20;

    /// <summary>Logging verbosity cutoff level. Lower values are more verbose. Valid range: <c> 0 </c>–<c> 5 </c>.</summary>
    public int LogCutoff { get; set; } = 1;

    /// <summary>Write log output to a file on disk.</summary>
    public bool SendLogfile { get; set; } = true;

    /// <summary>Send log output to an attached debugger via <c> OutputDebugString </c>.</summary>
    public bool SendDebugger { get; set; } = true;
}
