using System.Security;

using Microsoft.Win32;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Discovery;

/// <summary>
///     Windows-only implementation of <see cref="IRegistryReader" /> backed by <c>Microsoft.Win32.Registry</c>. Returns <see langword="null" /> for any missing key, missing
///     value, non-string value, or access error.
/// </summary>
public sealed class WindowsRegistryReader : IRegistryReader
{
    /// <inheritdoc />
    public string? ReadLocalMachineString(string keyPath, string valueName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath, writable: false);

            return key?.GetValue(valueName) as string;
        }
        catch (SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
