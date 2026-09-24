namespace Erdmier.ZooTycoonLauncher.Application.Common.Models;

/// <summary>One value row to change in the <c>Current</c> snapshot.</summary>
/// <param name="Id">The key.</param>
/// <param name="Value">The new raw value, or <see langword="null" /> when the key has left the file (the row is deleted).</param>
/// <param name="Source">How the value got there.</param>
public sealed record IniValueChange(IniKeyId Id, string? Value, IniValueSource Source);
