using System;

using Microsoft.Win32;

namespace Erdmier.ZooTycoonLauncher.Launcher.Services;

/// <summary>Production implementation of <see cref="IRegistryReader" /> backed by <see cref="Microsoft.Win32.Registry" />. Available only on Windows.</summary>
public sealed class WindowsRegistryReader : IRegistryReader
{
    /// <inheritdoc />
    public string? ReadHklmString(string subKeyPath, string valueName)
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(subKeyPath);
            return key?.GetValue(valueName) as string;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
