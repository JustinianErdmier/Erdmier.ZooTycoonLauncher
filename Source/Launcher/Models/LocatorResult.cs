namespace Erdmier.ZooTycoonLauncher.Launcher.Models;

/// <summary>The outcome of an attempt to locate the Zoo Tycoon installation on disk.</summary>
/// <param name="ExeFound"> Whether <c> zoo.exe </c> was located. </param>
/// <param name="IniFound"> Whether <c> zoo.ini </c> was located. </param>
/// <param name="ExePath"> The full path to <c> zoo.exe </c>, or <see langword="null" /> if it was not found. </param>
/// <param name="IniPath"> The full path to <c> zoo.ini </c>, or <see langword="null" /> if it was not found. </param>
/// <param name="GameDirectory"> The directory containing the game files, or <see langword="null" /> if discovery failed. </param>
public record LocatorResult(bool    ExeFound,
                            bool    IniFound,
                            string? ExePath,
                            string? IniPath,
                            string? GameDirectory);
