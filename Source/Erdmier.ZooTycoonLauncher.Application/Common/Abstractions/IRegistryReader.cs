namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>Reads string values out of the Windows registry. Implementations are platform-specific.</summary>
/// <remarks>The interface lives here so <see cref="IInstallationLocator" /> implementations can be unit-tested against a fake reader without touching the real registry.</remarks>
public interface IRegistryReader
{
	/// <summary>Reads the string value at <paramref name="valueName" /> under <paramref name="keyPath" /> in <see cref="Microsoft.Win32.RegistryHive.LocalMachine" />.</summary>
	/// <param name="keyPath">The key path under HKLM (e.g. <c>SOFTWARE\Microsoft\Microsoft Games\Zoo Tycoon\1.0</c>).</param>
	/// <param name="valueName">The value name (e.g. <c>InstallPath</c>).</param>
	/// <returns>The value, or <see langword="null" /> when the key or value is absent.</returns>
	string? ReadLocalMachineString(string keyPath, string valueName);
}
