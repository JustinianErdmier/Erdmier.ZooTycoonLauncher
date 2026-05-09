namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>
///     Thin abstraction over <see cref="Microsoft.Win32.Registry" /> reads, introduced so that <see cref="FileLocatorService" /> remains testable without touching the real
///     registry.
/// </summary>
public interface IRegistryReader
{
    /// <summary>Reads a string value from <c> HKEY_LOCAL_MACHINE </c>, returning <see langword="null" /> if the subkey, value, or registry hive is unavailable.</summary>
    /// <param name="subKeyPath"> Backslash-separated path under <c> HKLM </c>, e.g. <c> "SOFTWARE\\Microsoft Games\\Zoo Tycoon\\1.0"</c>. </param>
    /// <param name="valueName"> Name of the value to read. </param>
    string? ReadHklmString(string subKeyPath, string valueName);
}
