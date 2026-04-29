using System.Threading.Tasks;

using Erdmier.ZooTycoonLauncher.Launcher.Models;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Reads <c> zoo.ini </c> from disk into a typed <see cref="ZooIniModel" /> and writes a <see cref="ZooIniModel" /> back to disk, preserving comments and key ordering on round-trip.</summary>
public interface IIniParserService
{
    /// <summary>Reads and parses <c> zoo.ini </c> from <paramref name="iniFilePath" />.</summary>
    /// <exception cref="System.IO.IOException"> Thrown if the file cannot be read. Callers (typically <see cref="StartupService" />) translate this into <see cref="StartupStatus.IniParseFailed" />. </exception>
    Task<ZooIniModel> ReadAsync(string iniFilePath);

    /// <summary>Writes <paramref name="model" /> back to <paramref name="iniFilePath" />, preserving the original layout if the model was previously read from disk.</summary>
    Task WriteAsync(string iniFilePath, ZooIniModel model);

    /// <summary>Returns a fresh <see cref="ZooIniModel" /> populated with the documented factory defaults.</summary>
    ZooIniModel GetDefaults();
}
