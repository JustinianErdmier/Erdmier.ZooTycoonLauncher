namespace Erdmier.ZooTycoonLauncher.Launcher.ViewModels;

/// <summary>Flat label-value projection of a single <c> zoo.ini </c> key for verification display in the Home tab.</summary>
public sealed record IniDisplayEntry(string Label, string Value);
